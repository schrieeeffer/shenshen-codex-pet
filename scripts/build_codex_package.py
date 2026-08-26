from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "spritesheet-v2.png"
DEFAULT_OUTPUT = ROOT / "pet" / "codex" / "spritesheet.webp"
DEFAULT_WEB_OUTPUT = ROOT / "pet" / "web" / "spritesheet.webp"
EXPECTED_SIZE = (1536, 2288)
EXPECTED_WEB_SIZE = (1536, 1872)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build lossless Codex desktop/CLI and ChatGPT web pet assets."
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=DEFAULT_OUTPUT,
        help="Output WebP path (default: pet/codex/spritesheet.webp)",
    )
    parser.add_argument(
        "--web-output",
        type=Path,
        default=DEFAULT_WEB_OUTPUT,
        help="Web upload output (default: pet/web/spritesheet.webp)",
    )
    return parser.parse_args()


def save_and_verify(
    image: Image.Image,
    output: Path,
    expected_size: tuple[int, int],
    max_bytes: int | None = None,
) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    image.save(output, format="WEBP", lossless=True, quality=100, method=6, exact=True)

    with Image.open(output) as built:
        built_rgba = built.convert("RGBA")
        built_rgba.load()
        if built.format != "WEBP":
            raise ValueError(f"expected WEBP output, got {built.format}")
        if built_rgba.size != expected_size:
            raise ValueError(f"output size changed: {built_rgba.size}")
        if built_rgba.tobytes() != image.tobytes():
            raise ValueError(f"lossless WebP pixel verification failed: {output}")

    if max_bytes is not None and output.stat().st_size > max_bytes:
        raise ValueError(f"asset exceeds {max_bytes} bytes: {output}")


def main() -> int:
    args = parse_args()
    output = args.output.resolve()
    web_output = args.web_output.resolve()

    with Image.open(SOURCE) as opened:
        if opened.format != "PNG":
            raise ValueError(f"expected PNG source, got {opened.format}")
        image = opened.convert("RGBA")
        image.load()

    if image.size != EXPECTED_SIZE:
        raise ValueError(f"wrong atlas size: {image.size}, expected {EXPECTED_SIZE}")

    web_image = image.crop((0, 0, EXPECTED_WEB_SIZE[0], EXPECTED_WEB_SIZE[1]))
    if web_image.getchannel("A").getextrema()[0] == 255:
        raise ValueError("web pet must contain transparent pixels")

    save_and_verify(image, output, EXPECTED_SIZE)
    save_and_verify(web_image, web_output, EXPECTED_WEB_SIZE, 20 * 1024 * 1024)

    print(f"built Codex desktop/CLI: {output} ({output.stat().st_size} bytes)")
    print(f"built ChatGPT web: {web_output} ({web_output.stat().st_size} bytes)")
    print("OK: lossless desktop/CLI and web pet assets verified")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
