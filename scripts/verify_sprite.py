from __future__ import annotations

import argparse
import hashlib
import json
import sys
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
ATLAS = ROOT / "assets" / "spritesheet-v2.png"
MANIFEST = ROOT / "pet" / "pet.manifest.json"
CODEX_MANIFEST = ROOT / "pet" / "codex" / "pet.json"
CODEX_ATLAS = ROOT / "pet" / "codex" / "spritesheet.webp"
WEB_ATLAS = ROOT / "pet" / "web" / "spritesheet.webp"
EXPECTED_SIZE = (1536, 2288)
EXPECTED_WEB_SIZE = (1536, 1872)
CELL_SIZE = (192, 208)
FRAMES_PER_ROW = (6, 8, 8, 4, 5, 8, 6, 6, 6, 8, 8)
EXPECTED_SHA256 = "0dd1c39f8333f73da19389b4cda3f62c5658468e93caa5401add19bca33b5f30"
EXPECTED_STATES = (
    "idle",
    "running-right",
    "running-left",
    "waving",
    "jumping",
    "failed",
    "waiting",
    "running",
    "review",
)


def visible_pixels(cell: Image.Image) -> int:
    return sum(cell.getchannel("A").histogram()[17:])


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Verify the packaged sprite atlases.")
    parser.add_argument(
        "--allow-reencoded",
        action="store_true",
        help="Allow the source PNG bytes to differ while keeping structural checks strict.",
    )
    return parser.parse_args()


def validate_occupancy(
    image: Image.Image,
    label: str,
    frames_per_row: tuple[int, ...] = FRAMES_PER_ROW,
) -> list[str]:
    errors: list[str] = []
    cell_width, cell_height = CELL_SIZE
    for row, used_frames in enumerate(frames_per_row):
        for column in range(8):
            left = column * cell_width
            top = row * cell_height
            cell = image.crop((left, top, left + cell_width, top + cell_height))
            pixels = visible_pixels(cell)
            if column < used_frames and pixels == 0:
                errors.append(f"{label}: empty required frame: row {row}, column {column}")
            if column >= used_frames and pixels != 0:
                errors.append(f"{label}: artwork in unused frame: row {row}, column {column}")
    return errors


def validate_manifest() -> list[str]:
    errors: list[str] = []
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    atlas = manifest.get("atlas", {})
    if (atlas.get("width"), atlas.get("height")) != EXPECTED_SIZE:
        errors.append("pet manifest atlas dimensions do not match the v2 contract")
    if (atlas.get("cellWidth"), atlas.get("cellHeight")) != CELL_SIZE:
        errors.append("pet manifest cell dimensions do not match the v2 contract")
    if (atlas.get("columns"), atlas.get("rows")) != (8, 11):
        errors.append("pet manifest grid does not match the v2 contract")

    animations = manifest.get("animations", [])
    state_ids = tuple(animation.get("id") for animation in animations)
    if state_ids != EXPECTED_STATES:
        errors.append(f"wrong canonical animation order/names: {state_ids}")
    for row, animation in enumerate(animations):
        if animation.get("row") != row:
            errors.append(f"animation {animation.get('id')} has wrong row")
        if animation.get("frameCount") != FRAMES_PER_ROW[row]:
            errors.append(f"animation {animation.get('id')} has wrong frame count")
        durations = animation.get("frameDurationsMs", [])
        if len(durations) != animation.get("frameCount"):
            errors.append(f"animation {animation.get('id')} has wrong duration count")

    directions = manifest.get("lookDirections", [])
    if len(directions) != 16:
        errors.append("pet manifest must define exactly 16 look directions")

    codex = json.loads(CODEX_MANIFEST.read_text(encoding="utf-8"))
    if codex.get("id") != manifest.get("id"):
        errors.append("Codex pet id differs from the shared manifest")
    if codex.get("spriteVersionNumber") != 2:
        errors.append("Codex pet must declare spriteVersionNumber: 2")
    if codex.get("spritesheetPath") != "spritesheet.webp":
        errors.append("Codex pet must point to spritesheet.webp")
    return errors


def main() -> int:
    args = parse_args()
    errors: list[str] = []
    data = ATLAS.read_bytes()
    digest = hashlib.sha256(data).hexdigest()

    with Image.open(ATLAS) as opened:
        if opened.format != "PNG":
            errors.append(f"wrong source format: {opened.format}, expected PNG")
        if opened.mode != "RGBA":
            errors.append(f"wrong source mode: {opened.mode}, expected RGBA")
        image = opened.convert("RGBA")
        image.load()

    if image.size != EXPECTED_SIZE:
        errors.append(f"wrong atlas size: {image.size}, expected {EXPECTED_SIZE}")

    errors.extend(validate_occupancy(image, "source PNG"))
    errors.extend(validate_manifest())

    if CODEX_ATLAS.exists():
        with Image.open(CODEX_ATLAS) as opened:
            if opened.format != "WEBP":
                errors.append(f"wrong Codex format: {opened.format}, expected WEBP")
            codex_image = opened.convert("RGBA")
            codex_image.load()
        if codex_image.size != EXPECTED_SIZE:
            errors.append(f"wrong Codex atlas size: {codex_image.size}")
        elif codex_image.tobytes() != image.tobytes():
            errors.append("Codex WebP pixels differ from the source PNG")
        errors.extend(validate_occupancy(codex_image, "Codex WebP"))
    else:
        errors.append("missing Codex atlas: pet/codex/spritesheet.webp")

    if WEB_ATLAS.exists():
        with Image.open(WEB_ATLAS) as opened:
            if opened.format != "WEBP":
                errors.append(f"wrong web format: {opened.format}, expected WEBP")
            web_image = opened.convert("RGBA")
            web_image.load()
        if web_image.size != EXPECTED_WEB_SIZE:
            errors.append(f"wrong web atlas size: {web_image.size}")
        else:
            source_web = image.crop((0, 0, EXPECTED_WEB_SIZE[0], EXPECTED_WEB_SIZE[1]))
            if web_image.tobytes() != source_web.tobytes():
                errors.append("web WebP pixels differ from the first nine source rows")
        if WEB_ATLAS.stat().st_size > 20 * 1024 * 1024:
            errors.append("web WebP exceeds the 20 MiB upload limit")
        if web_image.getchannel("A").getextrema()[0] == 255:
            errors.append("web WebP does not contain transparent pixels")
        errors.extend(validate_occupancy(web_image, "web WebP", FRAMES_PER_ROW[:9]))
    else:
        errors.append("missing web atlas: pet/web/spritesheet.webp")

    print(f"file: {ATLAS}")
    print(f"size: {image.width}x{image.height}")
    print(f"sha256: {digest}")
    print(f"expected sha256: {EXPECTED_SHA256}")

    if digest != EXPECTED_SHA256 and not args.allow_reencoded:
        errors.append("source PNG SHA-256 differs from the immutable packaged release")
    elif digest != EXPECTED_SHA256:
        print("note: source bytes differ; accepted because --allow-reencoded was supplied")

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print("OK: source atlas, shared manifest, Codex package, and web upload passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
