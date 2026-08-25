from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
REQUEST = ROOT / "metadata" / "pet-request.json"


def main() -> int:
    request = json.loads(REQUEST.read_text(encoding="utf-8"))
    guides = request.get("layout_guides", [])
    if not guides:
        raise ValueError("metadata/pet-request.json contains no layout guides")

    for guide in guides:
        output = ROOT / guide["path"]
        output.parent.mkdir(parents=True, exist_ok=True)
        width = int(guide["width"])
        height = int(guide["height"])
        frames = int(guide["frames"])
        cell_width = int(guide["cell_width"])
        cell_height = int(guide["cell_height"])
        margin_x = int(guide["safe_margin_x"])
        margin_y = int(guide["safe_margin_y"])

        if width != frames * cell_width or height != cell_height:
            raise ValueError(f"invalid guide geometry for {guide['state']}")

        image = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        draw = ImageDraw.Draw(image)
        for column in range(frames):
            left = column * cell_width
            right = left + cell_width - 1
            draw.rectangle((left, 0, right, cell_height - 1), outline=(50, 150, 255, 190), width=1)
            draw.rectangle(
                (
                    left + margin_x,
                    margin_y,
                    right - margin_x,
                    cell_height - margin_y - 1,
                ),
                outline=(255, 176, 32, 150),
                width=1,
            )
            draw.line(
                (left + margin_x, cell_height - margin_y - 1, right - margin_x, cell_height - margin_y - 1),
                fill=(255, 96, 96, 180),
                width=1,
            )
            draw.text((left + 5, 5), f"{guide['state']} {column}", fill=(255, 255, 255, 210))

        image.save(output, format="PNG", optimize=True)
        print(f"built: {output.relative_to(ROOT)}")

    print(f"OK: built {len(guides)} reproducible layout guides")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
