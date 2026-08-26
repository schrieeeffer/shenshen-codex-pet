from __future__ import annotations

import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Any, Iterator


ROOT = Path(__file__).resolve().parents[1]
TARGET_DIRECTORIES = (ROOT / "metadata", ROOT / "qa")
REPOSITORY_PREFIXES = (
    "assets/",
    "metadata/",
    "pet/",
    "previews/",
    "qa/",
    "references/",
    "source/",
)
WINDOWS_ABSOLUTE = re.compile(r"^[A-Za-z]:[\\/]")
SEMANTIC_VERSION = re.compile(r"^\d+\.\d+\.\d+$")


def validate_release_contract(errors: list[str]) -> str:
    release_path = ROOT / "metadata" / "release.json"
    project_path = ROOT / "src" / "ShenshenPet.Windows" / "ShenshenPet.Windows.csproj"
    app_manifest_path = ROOT / "src" / "ShenshenPet.Windows" / "app.manifest"

    release = json.loads(release_path.read_text(encoding="utf-8"))
    version = str(release.get("package_version", ""))
    if not SEMANTIC_VERSION.fullmatch(version):
        errors.append(f"metadata/release.json: invalid package_version: {version}")
        return version

    project = ET.parse(project_path).getroot()
    project_version = project.findtext(".//Version")
    if project_version != version:
        errors.append(
            f"Windows project version {project_version} differs from release {version}"
        )

    app_manifest = ET.parse(app_manifest_path).getroot()
    identity = next(
        (element for element in app_manifest.iter() if element.tag.endswith("assemblyIdentity")),
        None,
    )
    expected_windows_version = f"{version}.0"
    if identity is None or identity.get("version") != expected_windows_version:
        errors.append(
            f"app manifest version must be {expected_windows_version}"
        )

    if (release.get("web_width"), release.get("web_height"), release.get("web_rows")) != (
        1536,
        1872,
        9,
    ):
        errors.append("release metadata has the wrong ChatGPT web geometry")

    notes_path = ROOT / "docs" / "releases" / f"v{version}.md"
    if not notes_path.is_file():
        errors.append(f"missing release notes: {notes_path.relative_to(ROOT)}")

    changelog = (ROOT / "CHANGELOG.md").read_text(encoding="utf-8")
    if f"## [{version}]" not in changelog:
        errors.append(f"CHANGELOG.md has no section for {version}")

    return version


def walk_strings(value: Any, key_path: str = "$") -> Iterator[tuple[str, str]]:
    if isinstance(value, dict):
        for key, child in value.items():
            yield from walk_strings(child, f"{key_path}.{key}")
    elif isinstance(value, list):
        for index, child in enumerate(value):
            yield from walk_strings(child, f"{key_path}[{index}]")
    elif isinstance(value, str):
        yield key_path, value


def main() -> int:
    errors: list[str] = []
    parsed = 0

    for directory in TARGET_DIRECTORIES:
        for path in sorted(directory.glob("*.json")):
            parsed += 1
            try:
                data = json.loads(path.read_text(encoding="utf-8"))
            except (OSError, json.JSONDecodeError) as error:
                errors.append(f"{path.relative_to(ROOT)}: invalid JSON: {error}")
                continue

            for key_path, value in walk_strings(data):
                normalized = value.replace("\\", "/")
                if "/workspace/scratch/" in normalized or WINDOWS_ABSOLUTE.match(value):
                    errors.append(
                        f"{path.relative_to(ROOT)} {key_path}: machine-specific path: {value}"
                    )
                    continue

                if normalized.startswith("archive://") or normalized.startswith(("http://", "https://")):
                    continue

                if normalized.startswith(REPOSITORY_PREFIXES):
                    candidate = ROOT / normalized
                    if not candidate.exists():
                        errors.append(
                            f"{path.relative_to(ROOT)} {key_path}: missing repository path: {value}"
                        )

    release_version = validate_release_contract(errors)

    if errors:
        for error in errors:
            print(f"ERROR: {error}")
        return 1

    print(
        f"OK: parsed {parsed} metadata/QA JSON files; release v{release_version} "
        "has portable paths and aligned version metadata"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
