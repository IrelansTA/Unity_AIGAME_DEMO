#!/usr/bin/env python3
"""Audit and normalize transparent Unity sprite PNGs without touching .meta files."""

from __future__ import annotations

import argparse
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--folder", required=True, help="Folder containing PNG frames.")
    parser.add_argument("--files", nargs="+", required=True, help="PNG filenames, in audit order.")
    parser.add_argument("--anchor-x", type=float, default=95.5)
    parser.add_argument("--ground-bottom", type=int, default=149)
    parser.add_argument("--canvas", type=int, default=192)
    parser.add_argument("--audit-out", help="Optional path for a checkerboard audit sheet.")
    parser.add_argument("--write", action="store_true", help="Rewrite PNG files in place.")
    parser.add_argument(
        "--freeze",
        action="append",
        default=[],
        help="Freeze group as source.png:dst1.png,dst2.png. Can be repeated.",
    )
    return parser.parse_args()


def scrub_transparency(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    px = image.load()
    width, height = image.size
    for y in range(height):
        for x in range(width):
            r, g, b, a = px[x, y]
            magenta = r > 150 and b > 150 and g < 130 and (r - g) > 60 and (b - g) > 60
            if a <= 8 or magenta:
                px[x, y] = (0, 0, 0, 0)
    return image


def anchored(image: Image.Image, canvas: int, anchor_x: float, ground_bottom: int) -> Image.Image:
    image = scrub_transparency(image)
    box = image.getbbox()
    output = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    if box is None:
        return output

    crop = image.crop(box)
    width, height = crop.size
    left = int(round(anchor_x - (width - 1) / 2.0))
    top = ground_bottom + 1 - height
    output.alpha_composite(crop, (left, top))
    return output


def apply_freeze(folder: Path, specs: list[str], canvas: int, anchor_x: float, ground_bottom: int) -> None:
    for spec in specs:
        source_name, targets = spec.split(":", 1)
        source = anchored(Image.open(folder / source_name), canvas, anchor_x, ground_bottom)
        for target_name in targets.split(","):
            source.save(folder / target_name)


def print_row(name: str, image: Image.Image) -> None:
    box = image.getbbox()
    digest = hashlib.sha256(image.tobytes()).hexdigest()[:12]
    if box is None:
        print(f"{name} hash={digest} empty")
        return

    width = box[2] - box[0]
    height = box[3] - box[1]
    center_x = (box[0] + box[2] - 1) / 2.0
    bottom = box[3] - 1
    print(f"{name} hash={digest} box={box} size={width}x{height} cx={center_x:.1f} bottom={bottom}")


def write_audit_sheet(folder: Path, names: list[str], canvas: int, anchor_x: float, ground_bottom: int, out: Path) -> None:
    cols = 8
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * canvas, rows * canvas), (62, 62, 62, 255))
    for index, name in enumerate(names):
        image = Image.open(folder / name).convert("RGBA")
        bg = Image.new("RGBA", (canvas, canvas), (88, 88, 88, 255))
        draw = ImageDraw.Draw(bg)
        step = 24
        for y in range(0, canvas, step):
            for x in range(0, canvas, step):
                fill = (116, 116, 116, 255) if ((x // step + y // step) % 2) == 0 else (74, 74, 74, 255)
                draw.rectangle((x, y, x + step - 1, y + step - 1), fill=fill)
        bg.alpha_composite(image)
        draw.line((0, ground_bottom, canvas, ground_bottom), fill=(0, 255, 0, 255))
        draw.line((int(anchor_x), 0, int(anchor_x), canvas), fill=(0, 160, 255, 255))
        draw.text((4, 4), name.replace(".png", ""), fill=(255, 255, 255, 255))
        sheet.alpha_composite(bg, ((index % cols) * canvas, (index // cols) * canvas))
    sheet.save(out)


def main() -> None:
    args = parse_args()
    folder = Path(args.folder)

    if args.write:
        apply_freeze(folder, args.freeze, args.canvas, args.anchor_x, args.ground_bottom)
        for name in args.files:
            path = folder / name
            anchored(Image.open(path), args.canvas, args.anchor_x, args.ground_bottom).save(path)

    for name in args.files:
        print_row(name, Image.open(folder / name).convert("RGBA"))

    if args.audit_out:
        write_audit_sheet(folder, args.files, args.canvas, args.anchor_x, args.ground_bottom, Path(args.audit_out))
        print(f"audit={args.audit_out}")


if __name__ == "__main__":
    main()
