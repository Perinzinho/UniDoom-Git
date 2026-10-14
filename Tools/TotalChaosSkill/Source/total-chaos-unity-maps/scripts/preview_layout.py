#!/usr/bin/env python3
"""Renderiza um layout JSON como imagem top-down (norte para cima, igual à Unity).

Uso: python preview_layout.py layout.json [saida.png] [--cell 28]
"""
import sys

from PIL import Image, ImageDraw, ImageFont

from tc_layout import DOORS, Grid, key_color, load, walkable

BG = (16, 17, 16)
WALL = (74, 76, 70)
FLOOR = (122, 117, 104)
OUTDOOR = (88, 106, 114)
DOOR = (150, 100, 52)


def render(path, out, cell=28):
    data = load(path)
    g = Grid(data["grid"], data.get("autoShell", True))
    W, H = g.cols * cell, g.rows * cell
    legend_h = 54
    img = Image.new("RGB", (W, H + legend_h), BG)
    d = ImageDraw.Draw(img)
    try:
        font = ImageFont.load_default(size=max(10, cell // 2))
        small = ImageFont.load_default(size=11)
    except TypeError:  # Pillow antigo
        font = small = ImageFont.load_default()

    for x, r, ch in g.cells():
        x0, y0 = x * cell, r * cell
        box = (x0, y0, x0 + cell - 1, y0 + cell - 1)
        if ch == "#":
            d.rectangle(box, fill=WALL)
            continue
        if ch == " ":
            continue
        base = OUTDOOR if g.is_outdoor(x, r) else FLOOR
        d.rectangle(box, fill=base)
        cx, cy = x0 + cell / 2, y0 + cell / 2
        k = cell * 0.3
        if ch in DOORS:
            col = DOOR if ch == "D" else key_color(int(ch))
            d.rectangle((x0 + 2, y0 + 2, x0 + cell - 3, y0 + cell - 3), fill=col)
            if ch != "D":
                d.text((cx, cy), ch, fill=(255, 255, 255), font=font, anchor="mm")
        elif ch == "P":
            d.ellipse((cx - k, cy - k, cx + k, cy + k), fill=(70, 200, 90))
            d.text((cx, cy), "P", fill=(0, 0, 0), font=font, anchor="mm")
        elif ch == "E":
            d.ellipse((cx - k, cy - k, cx + k, cy + k), fill=(200, 50, 50))
            d.text((cx, cy), "E", fill=(255, 255, 255), font=font, anchor="mm")
        elif ch == "I":
            d.polygon([(cx, cy - k), (cx + k, cy), (cx, cy + k), (cx - k, cy)], fill=(230, 200, 60))
        elif ch in "abcdefghi":
            n = ord(ch) - 96
            d.rectangle((cx - k, cy - k, cx + k, cy + k), fill=key_color(n), outline=(255, 255, 255))
            d.text((cx, cy), str(n), fill=(255, 255, 255), font=small, anchor="mm")
        elif ch == "X":
            d.rectangle((x0 + 3, y0 + 3, x0 + cell - 4, y0 + cell - 4), fill=(190, 60, 190))
            d.text((cx, cy), "X", fill=(255, 255, 255), font=font, anchor="mm")
        elif ch == "L":
            d.ellipse((cx - k * 1.4, cy - k * 1.4, cx + k * 1.4, cy + k * 1.4), fill=(236, 190, 110))
            d.ellipse((cx - k * 0.6, cy - k * 0.6, cx + k * 0.6, cy + k * 0.6), fill=(255, 245, 200))
        elif ch == "C":
            d.rectangle((cx - k, cy - k, cx + k, cy + k), fill=(110, 82, 52))
        elif ch == "O":
            d.ellipse((cx - k, cy - k, cx + k, cy + k), fill=(90, 70, 60))

    ly = H + 8
    items = [("P inicio", (70, 200, 90)), ("E inimigo", (200, 50, 50)), ("I item", (230, 200, 60)),
             ("X saida", (190, 60, 190)), ("L luz", (236, 190, 110)), ("D porta", DOOR),
             ("1-9 trancada / a-i chave", key_color(1)), ("_ ao ar livre", OUTDOOR)]
    x = 8
    for i, (label, col) in enumerate(items):
        if i == 4:
            x, ly = 8, ly + 18
        d.rectangle((x, ly, x + 10, ly + 10), fill=col)
        d.text((x + 15, ly - 1), label, fill=(200, 200, 200), font=small)
        x += 15 + int(d.textlength(label, font=small)) + 14
    img.save(out)
    return out


if __name__ == "__main__":
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    cell = 28
    if "--cell" in sys.argv:
        cell = int(sys.argv[sys.argv.index("--cell") + 1])
        args = [a for a in args if a != str(cell)]
    if not args:
        print(__doc__)
        sys.exit(2)
    src = args[0]
    out = args[1] if len(args) > 1 else src.rsplit(".", 1)[0] + "_preview.png"
    print("Preview salvo em", render(src, out, cell))
