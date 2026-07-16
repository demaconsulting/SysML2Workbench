"""Build a Windows .ico containing only the practical app-icon sizes
(title bar, taskbar, Explorer small/medium/large views, Alt-Tab), as
uncompressed 32bpp BMP (DIB) frames per Microsoft's documented recommendation
(PNG compression is only recommended for 256x256 "jumbo" icons - not needed
here since this file is used solely as an exe/window/installer icon, not as
a general-purpose image; the source Icon.png is used directly wherever a
compressed bitmap is wanted).
"""
import struct
from PIL import Image

SRC = "Icon.png"
BMP_SIZES = [16, 24, 32, 48, 64]


def resized_rgba(img, size):
    return img.resize((size, size), Image.LANCZOS).convert("RGBA")


def bmp_frame(img_rgba):
    """Build the DIB (BITMAPINFOHEADER + XOR + AND mask) bytes for one ICO frame."""
    w, h = img_rgba.size
    pixels = img_rgba.load()

    # XOR data: BGRA, bottom-to-top row order, 32bpp needs no row padding.
    xor = bytearray()
    for y in range(h - 1, -1, -1):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            xor += bytes((b, g, r, a))

    # AND mask: 1bpp, bottom-to-top, rows padded to a 4-byte boundary.
    # All-zero (fully "opaque") since the 32bpp XOR alpha channel carries
    # real transparency information for any decoder that honors it.
    row_bytes = ((w + 31) // 32) * 4
    and_mask = bytearray(row_bytes * h)

    header = struct.pack(
        "<IiiHHIIiiII",
        40,  # biSize
        w,  # biWidth
        h * 2,  # biHeight (doubled: XOR + AND)
        1,  # biPlanes
        32,  # biBitCount
        0,  # biCompression (BI_RGB)
        0,  # biSizeImage
        0, 0,  # biXPelsPerMeter, biYPelsPerMeter
        0,  # biClrUsed
        0,  # biClrImportant
    )
    return bytes(header) + bytes(xor) + bytes(and_mask)


def png_frame(img_rgba):
    """Re-encode as a strict truecolor+alpha (color type 6) PNG, never palette-indexed."""
    import io
    buf = io.BytesIO()
    img_rgba.save(buf, format="PNG")
    return buf.getvalue()


def main():
    master = Image.open(SRC).convert("RGBA")

    frames = []  # (width_for_dir, height_for_dir, data, is_png)
    for s in BMP_SIZES:
        data = bmp_frame(resized_rgba(master, s))
        frames.append((s, s, data, False))

    with open("Icon.ico", "wb") as out:
        out.write(struct.pack("<HHH", 0, 1, len(frames)))
        header_size = 6
        entry_size = 16
        offset = header_size + entry_size * len(frames)
        entries = []
        for w, h, data, _ in frames:
            entries.append(struct.pack("<BBBBHHII", w, h, 0, 0, 1, 32, len(data), offset))
            offset += len(data)
        for e in entries:
            out.write(e)
        for _, _, data, _ in frames:
            out.write(data)

    print(f"Wrote Icon.ico with {len(frames)} BMP frames")


if __name__ == "__main__":
    main()
