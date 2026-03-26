// These are EditMode tests for CalibrationXmlParser.
// Run them via Unity > Window > General > Test Runner > EditMode.
//
// They exercise the XML parsing and validation logic that lives in
// Assets/Scripts/CalibrationXmlParser.cs without requiring an Android device,
// a running scene, or any Oculus SDK calls.

using NUnit.Framework;

[TestFixture]
public class CalibrationXmlParserTests
{
    // -------------------------------------------------------------------------
    // A complete, valid mrc.xml document used as the baseline across tests.
    // The format mirrors what the Oculus PC MRC calibration tool produces.
    // -------------------------------------------------------------------------
    private const string ValidXml =
        "<?xml version=\"1.0\"?>" +
        "<opencv_storage>" +
            "<camera_id>0</camera_id>" +
            "<camera_name>ExternalCamera0</camera_name>" +
            "<translation type_id=\"opencv-matrix\">" +
                "<rows>3</rows><cols>1</cols><dt>f</dt>" +
                "<data>0.5 1.0 -0.5</data>" +
            "</translation>" +
            "<rotation type_id=\"opencv-matrix\">" +
                "<rows>4</rows><cols>1</cols><dt>f</dt>" +
                "<data>0.0 0.0 0.0 1.0</data>" +
            "</rotation>" +
            "<camera_matrix type_id=\"opencv-matrix\">" +
                "<rows>3</rows><cols>3</cols><dt>f</dt>" +
                "<data>800 0 960 0 800 540 0 0 1</data>" +
            "</camera_matrix>" +
            "<image_width>1920</image_width>" +
            "<image_height>1080</image_height>" +
        "</opencv_storage>";

    // =========================================================================
    // Happy-path tests
    // =========================================================================

    [Test]
    public void TryParse_ValidXml_ReturnsTrue()
    {
        bool ok = CalibrationXmlParser.TryParse(ValidXml, out _, out string error);
        Assert.IsTrue(ok, $"Expected success but got error: {error}");
    }

    [Test]
    public void TryParse_ValidXml_ErrorIsNull()
    {
        CalibrationXmlParser.TryParse(ValidXml, out _, out string error);
        Assert.IsNull(error);
    }

    [Test]
    public void TryParse_ValidXml_CameraIdCorrect()
    {
        CalibrationXmlParser.TryParse(ValidXml, out var data, out _);
        Assert.AreEqual(0u, data.CameraId);
    }

    [Test]
    public void TryParse_ValidXml_CameraNameCorrect()
    {
        CalibrationXmlParser.TryParse(ValidXml, out var data, out _);
        Assert.AreEqual("ExternalCamera0", data.CameraName);
    }

    [Test]
    public void TryParse_ValidXml_TranslationCorrect()
    {
        CalibrationXmlParser.TryParse(ValidXml, out var data, out _);
        Assert.AreEqual(3, data.Translation.Length);
        Assert.AreEqual(0.5f,  data.Translation[0], 1e-6f);
        Assert.AreEqual(1.0f,  data.Translation[1], 1e-6f);
        Assert.AreEqual(-0.5f, data.Translation[2], 1e-6f);
    }

    [Test]
    public void TryParse_ValidXml_RotationCorrect()
    {
        CalibrationXmlParser.TryParse(ValidXml, out var data, out _);
        Assert.AreEqual(4, data.Rotation.Length);
        Assert.AreEqual(0f, data.Rotation[0], 1e-6f);
        Assert.AreEqual(0f, data.Rotation[1], 1e-6f);
        Assert.AreEqual(0f, data.Rotation[2], 1e-6f);
        Assert.AreEqual(1f, data.Rotation[3], 1e-6f);
    }

    [Test]
    public void TryParse_ValidXml_CameraMatrixCorrect()
    {
        CalibrationXmlParser.TryParse(ValidXml, out var data, out _);
        Assert.AreEqual(9, data.CameraMatrix.Length);
        // fx at [0,0] maps to index 0
        Assert.AreEqual(800f, data.CameraMatrix[0], 1e-6f);
        // cx at [0,2] maps to index 2
        Assert.AreEqual(960f, data.CameraMatrix[2], 1e-6f);
        // fy at [1,1] maps to index 4
        Assert.AreEqual(800f, data.CameraMatrix[4], 1e-6f);
        // cy at [1,2] maps to index 5
        Assert.AreEqual(540f, data.CameraMatrix[5], 1e-6f);
    }

    [Test]
    public void TryParse_ValidXml_ImageDimensionsCorrect()
    {
        CalibrationXmlParser.TryParse(ValidXml, out var data, out _);
        Assert.AreEqual(1920, data.ImageWidth);
        Assert.AreEqual(1080, data.ImageHeight);
    }

    [Test]
    public void TryParse_CameraIdNonZero_Accepted()
    {
        string xml = ValidXml.Replace("<camera_id>0</camera_id>", "<camera_id>2</camera_id>");
        bool ok = CalibrationXmlParser.TryParse(xml, out var data, out string error);
        Assert.IsTrue(ok, $"Expected success but got error: {error}");
        Assert.AreEqual(2u, data.CameraId);
    }

    [Test]
    public void TryParse_FloatValuesWithDecimalPoints_ParsedCorrectly()
    {
        // Replace the translation values with fractional numbers.
        string xml = ValidXml.Replace(
            "<data>0.5 1.0 -0.5</data>",
            "<data>0.123456 -1.5 2.999</data>");
        bool ok = CalibrationXmlParser.TryParse(xml, out var data, out string error);
        Assert.IsTrue(ok, $"Expected success but got error: {error}");
        Assert.AreEqual(0.123456f, data.Translation[0], 1e-5f);
        Assert.AreEqual(-1.5f,    data.Translation[1], 1e-6f);
        Assert.AreEqual(2.999f,   data.Translation[2], 1e-5f);
    }

    // =========================================================================
    // Input-validation failure tests
    // =========================================================================

    [Test]
    public void TryParse_NullInput_ReturnsFalse()
    {
        bool ok = CalibrationXmlParser.TryParse(null, out _, out string error);
        Assert.IsFalse(ok);
        Assert.IsNotNull(error);
    }

    [Test]
    public void TryParse_EmptyString_ReturnsFalse()
    {
        bool ok = CalibrationXmlParser.TryParse("", out _, out string error);
        Assert.IsFalse(ok);
        Assert.IsNotNull(error);
    }

    [Test]
    public void TryParse_MalformedXml_ReturnsFalse()
    {
        bool ok = CalibrationXmlParser.TryParse("<not closed", out _, out string error);
        Assert.IsFalse(ok);
        Assert.IsNotNull(error);
    }

    [Test]
    public void TryParse_WrongRootElement_ReturnsFalse()
    {
        string xml = ValidXml.Replace("opencv_storage", "some_other_root");
        bool ok = CalibrationXmlParser.TryParse(xml, out _, out string error);
        Assert.IsFalse(ok);
        StringAssert.Contains("opencv_storage", error);
    }

    [Test]
    public void TryParse_MissingCameraId_ReturnsFalse()
    {
        string xml = ValidXml.Replace("<camera_id>0</camera_id>", "");
        bool ok = CalibrationXmlParser.TryParse(xml, out _, out string error);
        Assert.IsFalse(ok);
        StringAssert.Contains("camera_id", error);
    }

    [Test]
    public void TryParse_NegativeCameraId_ReturnsFalse()
    {
        string xml = ValidXml.Replace("<camera_id>0</camera_id>", "<camera_id>-1</camera_id>");
        bool ok = CalibrationXmlParser.TryParse(xml, out _, out string error);
        Assert.IsFalse(ok);
        StringAssert.Contains("camera_id", error);
    }

    [Test]
    public void TryParse_NonNumericCameraId_ReturnsFalse()
    {
        string xml = ValidXml.Replace("<camera_id>0</camera_id>", "<camera_id>abc</camera_id>");
        bool ok = CalibrationXmlParser.TryParse(xml, out _, out string error);
        Assert.IsFalse(ok);
        StringAssert.Contains("camera_id", error);
    }

    [Test]
    public void TryParse_MissingCameraName_ReturnsFalse()
    {
        string xml = ValidXml.Replace("<camera_name>ExternalCamera0</camera_name>", "");
        bool ok = CalibrationXmlParser.TryParse(xml, out _, out string error);
        Assert.IsFalse(ok);
        StringAssert.Contains("camera_name", error);
    }

    [Test]
    public void TryParse_MissingTranslation_ReturnsFalse()
    {
        // Remove entire <translation> block
        int start = ValidXml.IndexOf("<translation");
        int end   = ValidXml.IndexOf("</translation>") + "</translation>".Length;
        string xml = ValidXml.Remove(start, end - start);
        bool ok = CalibrationXmlParser.TryParse(xml, out _, out string error);
        Assert.IsFalse(ok);
        StringAssert.Contains("translation", error);
    }

    [Test]
    public void TryParse_TranslationWrongCount_ReturnsFalse()
    {
        // Only 2 values instead of the expected 3
        string xml = ValidXml.Replace("<data>0.5 1.0 -0.5</data>", "<data>0.5 1.0</data>");
        bool ok = CalibrationXmlParser.TryParse(xml, out _, out string error);
        Assert.IsFalse(ok);
        StringAssert.Contains("translation", error);
    }

    [Test]
    public void TryParse_RotationWrongCount_ReturnsFalse()
    {
        // Only 3 values instead of the expected 4
        string xml = ValidXml.Replace("<data>0.0 0.0 0.0 1.0</data>", "<data>0.0 0.0 0.0</data>");
        bool ok = CalibrationXmlParser.TryParse(xml, out _, out string error);
        Assert.IsFalse(ok);
        StringAssert.Contains("rotation", error);
    }

    [Test]
    public void TryParse_CameraMatrixWrongCount_ReturnsFalse()
    {
        // Only 8 values instead of the expected 9
        string xml = ValidXml.Replace(
            "<data>800 0 960 0 800 540 0 0 1</data>",
            "<data>800 0 960 0 800 540 0 0</data>");
        bool ok = CalibrationXmlParser.TryParse(xml, out _, out string error);
        Assert.IsFalse(ok);
        StringAssert.Contains("camera_matrix", error);
    }

    [Test]
    public void TryParse_MissingImageWidth_ReturnsFalse()
    {
        string xml = ValidXml.Replace("<image_width>1920</image_width>", "");
        bool ok = CalibrationXmlParser.TryParse(xml, out _, out string error);
        Assert.IsFalse(ok);
        StringAssert.Contains("image_width", error);
    }

    [Test]
    public void TryParse_MissingImageHeight_ReturnsFalse()
    {
        string xml = ValidXml.Replace("<image_height>1080</image_height>", "");
        bool ok = CalibrationXmlParser.TryParse(xml, out _, out string error);
        Assert.IsFalse(ok);
        StringAssert.Contains("image_height", error);
    }

    [Test]
    public void TryParse_NonNumericImageWidth_ReturnsFalse()
    {
        string xml = ValidXml.Replace("<image_width>1920</image_width>", "<image_width>wide</image_width>");
        bool ok = CalibrationXmlParser.TryParse(xml, out _, out string error);
        Assert.IsFalse(ok);
        StringAssert.Contains("image_width", error);
    }

    [Test]
    public void TryParse_NonNumericFloatInTranslation_ReturnsFalse()
    {
        // Ensure that when truly non-numeric tokens are present the parse fails.
        string xml2 = ValidXml.Replace("<data>0.5 1.0 -0.5</data>", "<data>0.5 abc -0.5</data>");
        bool ok = CalibrationXmlParser.TryParse(xml2, out _, out string error);
        Assert.IsFalse(ok);
        StringAssert.Contains("translation", error);
    }
}
