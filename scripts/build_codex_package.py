from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "spritesheet-v2.png"
DEFAULT_OUTPUT = ROOT / "pet" / "codex" / "spritesheet.webp"
EXPECTED_SIZE = (1536, 2288)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build the lossless Codex v2 WebP package asset."
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=DEFAULT_OUTPUT,
        help="Output WebP path (default: pet/codex/spritesheet.webp)",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    output = args.output.resolve()
    output.parent.mkdir(parents=True, exist_ok=True)

    with Image.open(SOURCE) as opened:
        if opened.format != "PNG":
            raise ValueError(f"expected PNG source, got {opened.format}")
        image = opened.convert("RGBA")
        image.load()

    if image.size != EXPECTED_SIZE:
        raise ValueError(f"wrong atlas size: {image.size}, expected {EXPECTED_SIZE}")

    image.save(output, format="WEBP", lossless=True, quality=100, method=6, exact=True)

    with Image.open(output) as built:
        built_rgba = built.convert("RGBA")
        built_rgba.load()
        if built.format != "WEBP":
            raise ValueError(f"expected WEBP output, got {built.format}")
        if built_rgba.size != image.size:
            raise ValueError(f"output size changed: {built_rgba.size}")
        if built_rgba.tobytes() != image.tobytes():
            raise ValueError("lossless WebP pixel verification failed")

    print(f"built: {output}")
    print(f"size: {output.stat().st_size} bytes")
    print("OK: lossless Codex v2 WebP verified")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
