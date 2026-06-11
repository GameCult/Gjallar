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
    parser.add_argument("--padding-x", type=int, default=1, help="Horizontal cell padding in pixels.")
    parser.add_argument("--padding-y", type=int, default=1, help="Vertical cell padding in pixels.")
    parser.add_argument("--source-size", type=int, default=96, help="Large source font size used before downscaling into the target cell.")
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

    font = ImageFont.truetype(str(font_path), args.source_size)
    glyphs = [
        render_glyph(font, cp, width, height, args.padding_x, args.padding_y)
        for cp in codepoints
    ]

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
        f"wrote {out_path} from {font_path.name} at source {args.source_size}px "
        f"with {len(glyphs)} glyphs ({width}x{height})"
    )

def render_glyph(
    font: ImageFont.FreeTypeFont,
    codepoint: int,
    width: int,
    height: int,
    padding_x: int,
    padding_y: int,
) -> bytes:
    ch = chr(codepoint)
    work = Image.new("L", (font.size * 2, font.size * 2), 0)
    draw = ImageDraw.Draw(work)
    bbox = font.getbbox(ch)
    if bbox is not None:
        left, top, right, bottom = bbox
        x = max(0, (work.width - (right - left)) // 2 - left)
        y = max(0, (work.height - (bottom - top)) // 2 - top)
        draw.text((x, y), ch, fill=255, font=font)

    glyph_box = work.getbbox()
    if glyph_box is None:
        fitted = Image.new("L", (width, height), 0)
    else:
        cropped = work.crop(glyph_box)
        inner_width = max(1, width - padding_x * 2)
        inner_height = max(1, height - padding_y * 2)
        scale = min(inner_width / cropped.width, inner_height / cropped.height)
        scaled_width = max(1, min(inner_width, round(cropped.width * scale)))
        scaled_height = max(1, min(inner_height, round(cropped.height * scale)))
        resampled = cropped.resize((scaled_width, scaled_height), Image.Resampling.LANCZOS)
        fitted = Image.new("L", (width, height), 0)
        paste_x = max(0, (width - scaled_width) // 2)
        paste_y = max(0, (height - scaled_height) // 2)
        fitted.paste(resampled, (paste_x, paste_y))

    bytes_per_row = (width + 7) // 8
    packed = bytearray(bytes_per_row * height)
    pixels = fitted.load()
    for y in range(height):
        for x in range(width):
            if pixels[x, y] < 96:
                continue
            offset = y * bytes_per_row + x // 8
            packed[offset] |= 0x80 >> (x % 8)
    return bytes(packed)


if __name__ == "__main__":
    main()
