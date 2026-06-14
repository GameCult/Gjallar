from __future__ import annotations

import argparse
import struct
from pathlib import Path


PSF2_MAGIC = 0x864AB572
UNICODE_TABLE_FLAG = 0x01


def main() -> None:
    parser = argparse.ArgumentParser(description="Build a fixed-cell PSF2 font from authored Shinonome bitmap sources.")
    parser.add_argument("--latin", required=True, help="Path to Shinonome latin1 font_src.bit.")
    parser.add_argument("--kanji", required=True, help="Path to Shinonome kanjic font_src.bit.")
    parser.add_argument("--out", required=True, help="Output PSF2 path.")
    parser.add_argument("--cell-width", type=int, required=True, help="Fixed cell width in pixels.")
    parser.add_argument("--cell-height", type=int, required=True, help="Fixed cell height in pixels.")
    args = parser.parse_args()

    latin_font = parse_bit_font(Path(args.latin))
    kanji_font = parse_bit_font(Path(args.kanji))

    glyph_map: dict[int, Glyph] = {}
    for codepoint, glyph in latin_font.glyphs.items():
        glyph_map[codepoint] = pad_glyph(glyph, args.cell_width, args.cell_height)
    for codepoint, glyph in kanji_font.glyphs.items():
        glyph_map[codepoint] = pad_glyph(glyph, args.cell_width, args.cell_height)

    codepoints = sorted(glyph_map)
    bytes_per_row = (args.cell_width + 7) // 8
    char_size = bytes_per_row * args.cell_height
    header = struct.pack(
        "<IIIIIIII",
        PSF2_MAGIC,
        0,
        32,
        UNICODE_TABLE_FLAG,
        len(codepoints),
        char_size,
        args.cell_height,
        args.cell_width,
    )
    unicode_table = b"".join(chr(codepoint).encode("utf-8") + b"\xFF" for codepoint in codepoints)

    out_path = Path(args.out)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with out_path.open("wb") as handle:
        handle.write(header)
        for codepoint in codepoints:
            handle.write(pack_glyph(glyph_map[codepoint], args.cell_width, args.cell_height))
        handle.write(unicode_table)

    print(
        f"wrote {out_path} with {len(codepoints)} glyphs "
        f"from {Path(args.latin).name} + {Path(args.kanji).name} "
        f"into {args.cell_width}x{args.cell_height} cells"
    )


class Glyph:
    def __init__(self, width: int, height: int, rows: list[int]) -> None:
        self.width = width
        self.height = height
        self.rows = rows


class BitFont:
    def __init__(self, width: int, height: int, glyphs: dict[int, Glyph]) -> None:
        self.width = width
        self.height = height
        self.glyphs = glyphs


def parse_bit_font(path: Path) -> BitFont:
    width = 0
    height = 0
    encoding: int | None = None
    bitmap_lines: list[str] = []
    in_bitmap = False
    glyphs: dict[int, Glyph] = {}

    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.rstrip("\n")
        if line.startswith("FONTBOUNDINGBOX "):
            _, w, h, *_ = line.split()
            width = int(w)
            height = int(h)
        elif line.startswith("ENCODING "):
            encoding = int(line.split()[1])
        elif line == "BITMAP":
            in_bitmap = True
            bitmap_lines = []
        elif line.startswith("ENDCHAR"):
            if encoding is None:
                raise ValueError(f"missing ENCODING before ENDCHAR in {path}")
            glyphs.update(decode_glyph(encoding, width, height, bitmap_lines))
            encoding = None
            in_bitmap = False
            bitmap_lines = []
        elif in_bitmap:
            bitmap_lines.append(line)

    if width <= 0 or height <= 0:
        raise ValueError(f"missing FONTBOUNDINGBOX in {path}")
    return BitFont(width, height, glyphs)


def decode_glyph(encoding: int, width: int, height: int, bitmap_lines: list[str]) -> dict[int, Glyph]:
    glyph = Glyph(width, height, [parse_row(line, width) for line in bitmap_lines[:height]])
    codepoint = decode_encoding(encoding)
    return {} if codepoint is None else {codepoint: glyph}


def decode_encoding(encoding: int) -> int | None:
    if 0 <= encoding <= 0xFF:
        return encoding

    row = (encoding >> 8) & 0xFF
    cell = encoding & 0xFF
    try:
        text = bytes([row + 0x80, cell + 0x80]).decode("euc_jp")
    except UnicodeDecodeError:
        return None
    return ord(text) if len(text) == 1 else None


def parse_row(line: str, width: int) -> int:
    bits = 0
    for index, ch in enumerate(line[:width]):
        if ch not in {".", " "}:
            bits |= 1 << (width - index - 1)
    return bits


def pad_glyph(glyph: Glyph, cell_width: int, cell_height: int) -> Glyph:
    if glyph.width > cell_width or glyph.height > cell_height:
        raise ValueError(
            f"glyph {glyph.width}x{glyph.height} does not fit inside target cell {cell_width}x{cell_height}"
        )

    offset_x = max(0, (cell_width - glyph.width) // 2)
    offset_y = max(0, cell_height - glyph.height)
    padded = [0 for _ in range(cell_height)]
    for row_index, row_bits in enumerate(glyph.rows):
        padded[offset_y + row_index] = row_bits << (cell_width - glyph.width - offset_x)
    return Glyph(cell_width, cell_height, padded)


def pack_glyph(glyph: Glyph, width: int, height: int) -> bytes:
    bytes_per_row = (width + 7) // 8
    packed = bytearray(bytes_per_row * height)
    for y in range(height):
        row_bits = glyph.rows[y]
        for x in range(width):
            if row_bits & (1 << (width - x - 1)):
                packed[y * bytes_per_row + x // 8] |= 0x80 >> (x % 8)
    return bytes(packed)


if __name__ == "__main__":
    main()
