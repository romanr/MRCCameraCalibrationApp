#!/usr/bin/env python3
"""
Increment the patch level of bundleVersion and AndroidBundleVersionCode
inside ProjectSettings/ProjectSettings.asset.

bundleVersion format handled:
  "1.0"     -> "1.0.1"
  "1.0.4"   -> "1.0.5"

AndroidBundleVersionCode is incremented by 1 in every case.
Both fields are required; the script exits with an error if either is missing.
"""
import re
import sys

SETTINGS_FILE = "ProjectSettings/ProjectSettings.asset"

with open(SETTINGS_FILE, "r") as f:
    content = f.read()


# --- bundleVersion -----------------------------------------------------------
def _bump_bundle_version(m: re.Match) -> str:
    parts = m.group(2).split(".")
    if len(parts) == 2:
        parts.append("1")
    else:
        parts[2] = str(int(parts[2]) + 1)
    return m.group(1) + ".".join(parts)


if not re.search(r"bundleVersion: \d+\.\d+(?:\.\d+)?", content):
    print("ERROR: could not find bundleVersion in " + SETTINGS_FILE, file=sys.stderr)
    sys.exit(1)

content, n = re.subn(
    r"(bundleVersion: )(\d+\.\d+(?:\.\d+)?)", _bump_bundle_version, content
)
assert n == 1, f"Expected exactly one bundleVersion replacement, got {n}"

# Read back the new version for output.
new_version = re.search(r"bundleVersion: (\d+\.\d+(?:\.\d+)?)", content).group(1)

# --- AndroidBundleVersionCode ------------------------------------------------
if not re.search(r"AndroidBundleVersionCode: \d+", content):
    print(
        "ERROR: could not find AndroidBundleVersionCode in " + SETTINGS_FILE,
        file=sys.stderr,
    )
    sys.exit(1)

content, n = re.subn(
    r"(AndroidBundleVersionCode: )(\d+)",
    lambda m: m.group(1) + str(int(m.group(2)) + 1),
    content,
)
assert n == 1, f"Expected exactly one AndroidBundleVersionCode replacement, got {n}"

with open(SETTINGS_FILE, "w") as f:
    f.write(content)

print(new_version)
