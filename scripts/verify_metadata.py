from __future__ import annotations

import json
import re
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

    if errors:
        for error in errors:
            print(f"ERROR: {error}")
        return 1

    print(f"OK: parsed {parsed} metadata/QA JSON files with portable path references")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
