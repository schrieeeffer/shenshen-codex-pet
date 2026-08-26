from __future__ import annotations

import argparse
import json
import shutil
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
ATLAS_PATH = ROOT / "assets" / "spritesheet-v2.png"
MANIFEST_PATH = ROOT / "pet" / "pet.manifest.json"


def assert_safe_output(output: Path) -> Path:
    resolved = output.resolve()
    allowed_roots = ((ROOT / "build").resolve(), (ROOT / "dist" / "release-staging").resolve())
    if not any(resolved.is_relative_to(root) for root in allowed_roots):
        raise ValueError(f"runtime-frame output must stay in build/ or dist/release-staging/: {resolved}")
    return resolved


def main() -> int:
    parser = argparse.ArgumentParser(description="Build lazy-load runtime PNG frames for the Windows pet.")
    parser.add_argument("--output", type=Path, default=ROOT / "build" / "runtime-frames")
    args = parser.parse_args()
    output = assert_safe_output(args.output)

    manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    atlas_definition = manifest["atlas"]
    cell_width = int(atlas_definition["cellWidth"])
    cell_height = int(atlas_definition["cellHeight"])
    cells = {
        (int(animation["row"]), column)
        for animation in manifest["animations"]
        for column in range(int(animation["frameCount"]))
    }
    cells.update(
        (int(direction["row"]), int(direction["column"]))
        for direction in manifest["lookDirections"]
    )

    if output.exists():
        shutil.rmtree(output)
    output.mkdir(parents=True)

    with Image.open(ATLAS_PATH) as opened:
        atlas = opened.convert("RGBA")
        if atlas.size != (int(atlas_definition["width"]), int(atlas_definition["height"])):
            raise ValueError("source atlas geometry differs from the manifest")

        for row, column in sorted(cells):
            left = column * cell_width
            top = row * cell_height
            frame = atlas.crop((left, top, left + cell_width, top + cell_height))
            frame.save(output / f"{row}-{column}.png", format="PNG", optimize=True, compress_level=9)

    generated = sorted(output.glob("*.png"))
    if len(generated) != len(cells):
        raise RuntimeError(f"expected {len(cells)} runtime frames, built {len(generated)}")
    print(f"OK: built {len(generated)} lazy-load runtime frames in {output.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
