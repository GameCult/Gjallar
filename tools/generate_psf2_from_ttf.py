from __future__ import annotations

import argparse
import struct
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


PSF2_MAGIC = 0x864AB572
UNICODE_TABLE_FLAG = 0x01


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate a fixed-cell PSF2 font from a TTF/OTF font.")
    parser.add_argument("--font", required=True, help="Source TTF/OTF path.")
    parser.add_argument("--out", required=True, help="Output PSF2 path.")
    parser.add_argument("--width", type=int, default=8, help="Cell width in pixels.")
    parser.add_argument("--height", type=int, default=16, help="Cell height in pixels.")
    args = parser.parse_args()

    font_path = Path(args.font)
    out_path = Path(args.out)
    width = args.width
    height = args.height

    codepoints = [0x0000]
    codepoints.extend(range(0x0020, 0x007F))
    codepoints.extend(range(0x00A0, 0x0100))
    codepoints.extend(range(0x2000, 0x2070))
    codepoints.extend(range(0x3000, 0x3040))
    codepoints.extend(range(0x3040, 0x30A0))
    codepoints.extend(range(0x30A0, 0x3100))
    codepoints.extend(range(0xFF00, 0xFFEF + 1))

    font_size = choose_font_size(font_path, width, height)
    font = ImageFont.truetype(str(font_path), font_size)
    glyphs = [render_glyph(font, cp, width, height) for cp in codepoints]

    bytes_per_row = (width + 7) // 8
    char_size = bytes_per_row * height
    header = struct.pack(
        "<IIIIIIII",
        PSF2_MAGIC,
        0,
        32,
        UNICODE_TABLE_FLAG,
        len(glyphs),
        char_size,
        height,
        width,
    )
    unicode_table = b"".join(
        chr(cp).encode("utf-8", "strict") + b"\xFF"
        for cp in codepoints
    )

    out_path.parent.mkdir(parents=True, exist_ok=True)
    with out_path.open("wb") as handle:
        handle.write(header)
        for glyph in glyphs:
            handle.write(glyph)
        handle.write(unicode_table)

    print(
        f"wrote {out_path} from {font_path.name} at {font_size}px "
        f"with {len(glyphs)} glyphs ({width}x{height})"
    )


def choose_font_size(font_path: Path, width: int, height: int) -> int:
    samples = [
        "A", "M", "0", "?", "あ", "ん", "ア", "ン", "メ", "タ", "め",
    ]
    for size in range(height * 2, 1, -1):
        font = ImageFont.truetype(str(font_path), size)
        if all(fits(font, sample, width, height) for sample in samples):
            return size
    raise RuntimeError(f"Could not fit representative glyphs from {font_path} into {width}x{height}.")


def fits(font: ImageFont.FreeTypeFont, text: str, width: int, height: int) -> bool:
    bbox = font.getbbox(text)
    if bbox is None:
        return True
    left, top, right, bottom = bbox
    return (right - left) <= width and (bottom - top) <= height


def render_glyph(font: ImageFont.FreeTypeFont, codepoint: int, width: int, height: int) -> bytes:
    ch = chr(codepoint)
    image = Image.new("L", (width, height), 0)
    draw = ImageDraw.Draw(image)
    bbox = font.getbbox(ch)
    if bbox is not None:
        left, top, right, bottom = bbox
        glyph_width = max(0, right - left)
        glyph_height = max(0, bottom - top)
        x = max(0, (width - glyph_width) // 2 - left)
        y = max(0, (height - glyph_height) // 2 - top)
        draw.text((x, y), ch, fill=255, font=font)

    bytes_per_row = (width + 7) // 8
    packed = bytearray(bytes_per_row * height)
    pixels = image.load()
    for y in range(height):
        for x in range(width):
            if pixels[x, y] < 96:
                continue
            offset = y * bytes_per_row + x // 8
            packed[offset] |= 0x80 >> (x % 8)
    return bytes(packed)


if __name__ == "__main__":
    main()
