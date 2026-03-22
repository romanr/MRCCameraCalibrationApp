/// <summary>
/// Utility class for parsing and validating MRC (Mixed Reality Capture) camera calibration XML data.
///
/// PURPOSE — WHY THE FILE EXISTS
/// ==============================
/// Mixed Reality Capture (MRC) lets VR game developers overlay live camera footage from a physical
/// camera on top of the rendered VR scene.  For this overlay to be geometrically correct the runtime
/// must know:
///   • where the physical camera is in the real world relative to the VR headset (position + orientation),
///   • how the camera images map to pixels (focal lengths and principal point in the camera matrix).
///
/// Oculus / Meta stores this data in a file called <c>mrc.xml</c> in OpenCV FileStorage XML format.
/// Each VR application reads the file from its own sandboxed folder at start-up:
///   <c>Android/data/{package}/files/mrc.xml</c>
///
/// LIFECYCLE OF mrc.xml IN THIS APP
/// ==================================
/// 1. A PC calibration tool (Oculus PC MRC or Reality Mixer) connects to this app over TCP.
/// 2. The calibration XML string is received as a network payload (<c>CALIBRATION_DATA</c> message).
/// 3. The XML is validated and written to <c>Application.persistentDataPath/mrc.xml</c>
///    (the app's own private storage) as the authoritative copy.
/// 4. <c>UpdateCalibrationFiles</c> then copies the file to every installed non-system VR app's
///    <c>Android/data/{package}/files/mrc.xml</c> so they pick it up on next launch.
/// 5. A fallback copy is also saved to <c>/sdcard/MRC/mrc.xml</c> for devices where Android 12+
///    scoped storage prevents direct writes to other apps' data folders.
/// 6. When this app reconnects to the PC tool it re-sends the saved XML so the UI stays in sync.
/// 7. On non-Android (PC/Editor) builds the data is pushed directly to the OVR runtime via the
///    <c>OVRPlugin.SetExternalCameraProperties</c> API instead of being written to a file.
///
/// TESTING
/// ========
/// All XML parsing logic is isolated in this file so it can be exercised without a running Unity
/// scene or Android device.  See <c>Assets/Tests/Editor/CalibrationXmlParserTests.cs</c> for the
/// NUnit EditMode tests.  Run them via Unity &gt; Window &gt; General &gt; Test Runner &gt; EditMode.
/// </summary>

using System;
using System.Globalization;
using System.Xml;

/// <summary>
/// Parses and validates MRC camera calibration XML strings.
/// This class has no Unity or Android dependencies and is safe to unit-test in the Editor.
/// </summary>
public static class CalibrationXmlParser
{
    /// <summary>
    /// All camera calibration parameters extracted from a single <c>mrc.xml</c> document.
    /// </summary>
    public struct CalibrationData
    {
        /// <summary>
        /// Zero-based camera index on the device (typically 0 for the first physical camera).
        /// </summary>
        public uint CameraId;

        /// <summary>
        /// Friendly name used by the OVR runtime to identify the camera (e.g. "ExternalCamera0").
        /// </summary>
        public string CameraName;

        /// <summary>
        /// Translation vector [x, y, z] in metres describing the camera position relative to the
        /// VR headset tracking origin.
        /// </summary>
        public float[] Translation;

        /// <summary>
        /// Rotation quaternion [x, y, z, w] describing the camera orientation relative to the
        /// VR headset tracking origin.
        /// </summary>
        public float[] Rotation;

        /// <summary>
        /// 3×3 camera intrinsic matrix stored row-major: [fx, 0, cx, 0, fy, cy, 0, 0, 1].
        /// <list type="bullet">
        ///   <item><description>fx, fy — focal lengths in pixels.</description></item>
        ///   <item><description>cx, cy — principal point (optical axis) in pixels.</description></item>
        /// </list>
        /// </summary>
        public float[] CameraMatrix;

        /// <summary>Sensor width in pixels.</summary>
        public int ImageWidth;

        /// <summary>Sensor height in pixels.</summary>
        public int ImageHeight;
    }

    /// <summary>
    /// Parses an <c>mrc.xml</c> string into a <see cref="CalibrationData"/> value.
    /// </summary>
    /// <param name="xmlData">Raw XML text received from the PC calibration tool.</param>
    /// <param name="result">Populated on success; default on failure.</param>
    /// <param name="errorMessage">Human-readable error description on failure; <c>null</c> on success.</param>
    /// <returns><c>true</c> when the XML is valid and all required fields are present and parseable.</returns>
    public static bool TryParse(string xmlData, out CalibrationData result, out string errorMessage)
    {
        result = default;
        errorMessage = null;

        if (string.IsNullOrEmpty(xmlData))
        {
            errorMessage = "XML data is null or empty.";
            return false;
        }

        XmlDocument doc = new XmlDocument();
        try
        {
            doc.LoadXml(xmlData);
        }
        catch (XmlException ex)
        {
            errorMessage = $"Invalid XML: {ex.Message}";
            return false;
        }

        return TryParse(doc, out result, out errorMessage);
    }

    /// <summary>
    /// Parses an already-loaded <see cref="XmlDocument"/> into a <see cref="CalibrationData"/> value.
    /// </summary>
    /// <param name="doc">XML document produced by the PC calibration tool.</param>
    /// <param name="result">Populated on success; default on failure.</param>
    /// <param name="errorMessage">Human-readable error description on failure; <c>null</c> on success.</param>
    /// <returns><c>true</c> when all required fields are present and parseable.</returns>
    public static bool TryParse(XmlDocument doc, out CalibrationData result, out string errorMessage)
    {
        result = default;
        errorMessage = null;

        XmlNode root = doc.SelectSingleNode("opencv_storage");
        if (root == null)
        {
            errorMessage = "Missing <opencv_storage> root element.";
            return false;
        }

        // camera_id
        XmlNode cameraIdNode = root.SelectSingleNode("camera_id");
        if (cameraIdNode == null)
        {
            errorMessage = "Missing <camera_id> element.";
            return false;
        }
        if (!uint.TryParse(cameraIdNode.InnerText.Trim(), out uint cameraId))
        {
            errorMessage = $"<camera_id> value '{cameraIdNode.InnerText.Trim()}' is not a valid non-negative integer.";
            return false;
        }

        // camera_name
        XmlNode cameraNameNode = root["camera_name"];
        if (cameraNameNode == null)
        {
            errorMessage = "Missing <camera_name> element.";
            return false;
        }

        // translation [x, y, z]
        if (!TryParseFloatArray(root, "translation", 3, out float[] translation, out errorMessage))
            return false;

        // rotation [x, y, z, w]
        if (!TryParseFloatArray(root, "rotation", 4, out float[] rotation, out errorMessage))
            return false;

        // camera_matrix (3×3 = 9 values, row-major)
        if (!TryParseFloatArray(root, "camera_matrix", 9, out float[] cameraMatrix, out errorMessage))
            return false;

        // image dimensions
        if (!TryParseIntElement(root, "image_width", out int imageWidth, out errorMessage))
            return false;
        if (!TryParseIntElement(root, "image_height", out int imageHeight, out errorMessage))
            return false;

        result = new CalibrationData
        {
            CameraId = cameraId,
            CameraName = cameraNameNode.InnerText.Trim(),
            Translation = translation,
            Rotation = rotation,
            CameraMatrix = cameraMatrix,
            ImageWidth = imageWidth,
            ImageHeight = imageHeight,
        };
        return true;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static bool TryParseFloatArray(XmlNode root, string elementName, int expectedCount,
        out float[] values, out string errorMessage)
    {
        values = null;

        XmlNode element = root[elementName];
        if (element == null)
        {
            errorMessage = $"Missing <{elementName}> element.";
            return false;
        }

        XmlNode dataNode = element["data"];
        if (dataNode == null)
        {
            errorMessage = $"Missing <data> child element inside <{elementName}>.";
            return false;
        }

        string[] tokens = dataNode.InnerText.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length != expectedCount)
        {
            errorMessage = $"<{elementName}/data> contains {tokens.Length} value(s), expected {expectedCount}.";
            return false;
        }

        values = new float[expectedCount];
        for (int i = 0; i < expectedCount; i++)
        {
            if (!float.TryParse(tokens[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
            {
                errorMessage = $"Could not parse float value '{tokens[i]}' at index {i} in <{elementName}/data>.";
                return false;
            }
        }

        errorMessage = null;
        return true;
    }

    private static bool TryParseIntElement(XmlNode root, string elementName, out int value, out string errorMessage)
    {
        value = 0;

        XmlNode element = root[elementName];
        if (element == null)
        {
            errorMessage = $"Missing <{elementName}> element.";
            return false;
        }

        if (!int.TryParse(element.InnerText.Trim(), out value))
        {
            errorMessage = $"<{elementName}> value '{element.InnerText.Trim()}' is not a valid integer.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
