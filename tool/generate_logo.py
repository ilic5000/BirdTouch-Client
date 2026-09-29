"""Draws the BirdTouch logo (purple tile, white gull, "BT") at high resolution.

Recreates the original 164 px logo of the Xamarin app so launcher icons are sharp.
Output goes to assets/branding/. Run from the repository root:

    python tool/generate_logo.py      (needs Pillow and the Rockwell Bold font, Windows: ROCKB.TTF)

Then regenerate the launcher icons: dart run flutter_launcher_icons
"""

import math
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

SIZE = 1024
OUT = Path(__file__).resolve().parent.parent / "assets" / "branding"
FONT = "C:/Windows/Fonts/ROCKB.TTF"
INNER = (0x7E, 0x57, 0xC2)  # centre of the tile
OUTER = (0x4A, 0x25, 0x9E)  # edges of the tile


def gradient_tile(size: int) -> Image.Image:
    """Radial purple gradient, lighter in the upper middle like the original."""
    img = Image.new("RGB", (size, size))
    px = img.load()
    cx, cy, max_d = size * 0.5, size * 0.38, size * 0.85
    for y in range(size):
        for x in range(size):
            t = min(1.0, math.hypot(x - cx, y - cy) / max_d)
            t = t**1.3
            px[x, y] = tuple(round(INNER[i] + (OUTER[i] - INNER[i]) * t) for i in range(3))
    return img


def bezier(p0, p1, p2, steps=64):
    return [
        (
            (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t**2 * p2[0],
            (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t**2 * p2[1],
        )
        for t in (i / steps for i in range(steps + 1))
    ]


def wing(start, control, end, width_start, width_end):
    """A tapered stroke along a quadratic curve, as a polygon."""
    pts = bezier(start, control, end)
    left, right = [], []
    for i, (x, y) in enumerate(pts):
        nx_, ny_ = pts[min(i + 1, len(pts) - 1)]
        px_, py_ = pts[max(i - 1, 0)]
        dx, dy = nx_ - px_, ny_ - py_
        length = math.hypot(dx, dy) or 1
        t = i / (len(pts) - 1)
        w = width_start + (width_end - width_start) * t
        ox, oy = -dy / length * w / 2, dx / length * w / 2
        left.append((x + ox, y + oy))
        right.append((x - ox, y - oy))
    return left + right[::-1]


def mark(size: int, scale: float = 1.0) -> Image.Image:
    """White gull and "BT" on a transparent layer. `scale` shrinks it around the centre."""
    s = size / 1024 * scale
    off = size * (1 - scale) / 2
    P = lambda x, y: (off + x * s, off + y * s)  # noqa: E731

    layer = Image.new("L", (size, size), 0)
    d = ImageDraw.Draw(layer)
    # Gull: a short left wing and a long right wing sweeping up and to the right.
    d.polygon(wing(P(240, 345), P(320, 175), P(470, 350), 18 * s, 56 * s), fill=255)
    d.polygon(wing(P(470, 350), P(530, 110), P(850, 170), 56 * s, 12 * s), fill=255)
    d.ellipse([*P(442, 322), *P(498, 378)], fill=255)

    font = ImageFont.truetype(FONT, round(430 * s))
    text = "BT"
    bbox = d.textbbox((0, 0), text, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    x = off + 512 * s - tw / 2 - bbox[0]
    y = off + 660 * s - th / 2 - bbox[1]
    d.text((x, y), text, font=font, fill=255)
    return layer


def with_shadow(base: Image.Image, mask: Image.Image) -> Image.Image:
    shadow = mask.filter(ImageFilter.GaussianBlur(base.width / 90))
    shadow = ImageChops.offset(shadow, 0, round(base.width / 110))
    base = base.convert("RGBA")
    base.paste((30, 10, 60, 255), (0, 0), shadow.point(lambda v: v * 0.45))
    base.paste((255, 255, 255, 255), (0, 0), mask)
    return base


def rounded(img: Image.Image, radius_ratio=0.2) -> Image.Image:
    m = Image.new("L", img.size, 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, img.width - 1, img.height - 1], round(img.width * radius_ratio), fill=255)
    img = img.convert("RGBA")
    img.putalpha(m)
    return img


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    tile = gradient_tile(SIZE)

    # iOS / legacy Android icon: full square, no transparency (the OS rounds the corners).
    full = with_shadow(tile, mark(SIZE)).convert("RGB")
    full.save(OUT / "icon.png")

    # In-app logo with rounded corners, like the original.
    rounded(full).resize((512, 512), Image.LANCZOS).save(OUT / "logo.png")

    # Android adaptive icon foreground: the mark inside the 66 % safe zone, on transparency.
    fg = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    fg.paste((255, 255, 255, 255), (0, 0), mark(SIZE, scale=0.62))
    fg.save(OUT / "icon_foreground.png")

    # Monochrome mark (Android 13 themed icons, and for use on coloured backgrounds).
    mono = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    mono.paste((255, 255, 255, 255), (0, 0), mark(SIZE))
    mono.resize((256, 256), Image.LANCZOS).save(OUT / "mark_white.png")


if __name__ == "__main__":
    main()
