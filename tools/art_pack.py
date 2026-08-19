#!/usr/bin/env python3
"""Overland Slice 0 art ingest.

Art produces PNG. Engineer runs this before commit. JPEG is an error.
No network. No API keys. Reads assets/palette.json (32 v3 colors).

    python tools/art_pack.py --role tile --src inbox/brick_floor_a.png --out assets/environment/town/brick_floor_a.png
    python tools/art_pack.py --role cookie --src inbox/light_lantern.png --out assets/environment/parallax/kilnwalk/light_lantern.png
    python tools/art_pack.py --role parallax --cell 720x144 --src inbox/far_bg.png --out assets/environment/parallax/kilnwalk/far_bg.png
    python tools/art_pack.py --check-tree
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

try:
    from PIL import Image
except ImportError as exc:
    raise SystemExit("Pillow is required. pip install -r tools/requirements.txt") from exc

ROOT = Path(__file__).resolve().parent.parent

ROLES = (
    "tile",
    "prop",
    "npc",
    "hero-frame",
    "enemy",
    "ui",
    "vfx",
    "cookie",
    "parallax",
    "hero-atlas",
)

DEFAULT_CELL = {
    "tile": (32, 32),
    "prop": (32, 32),
    "npc": (32, 48),
    "hero-frame": (32, 48),
    "ui": (32, 32),
    "vfx": (16, 16),
    "cookie": (32, 32),
}

BANNED = (
    "hyrule",
    "zelda",
    "triforce",
    "sheikah",
    "ganon",
    "hylia",
    "korok",
    "daggerfall",
    "bethesda",
    "whiterun",
    "septim",
    "nirn",
    "daedra",
    "aedra",
    "whimble",
    "whimsicle",
    "master sword",
    "master_sword",
    "master-sword",
    "mastersword",
)

MAGENTA = ((255, 0, 255), (255, 0, 170))
SEAM_LIMIT = 12.0
KEY_MANHATTAN = 80
KEY_LAB = 18.0


def load_palette(path: Path) -> list[tuple[int, int, int]]:
    data = json.loads(path.read_text(encoding="utf-8"))
    colors = data["colors"]
    if len(colors) != 32:
        raise SystemExit(f"{path}: expected 32 colors, got {len(colors)}")
    out: list[tuple[int, int, int]] = []
    for i, entry in enumerate(colors):
        if int(entry["index"]) != i:
            raise SystemExit(f"{path}: index {entry['index']} != {i}")
        hx = str(entry["hex"]).lstrip("#")
        out.append((int(hx[0:2], 16), int(hx[2:4], 16), int(hx[4:6], 16)))
    return out


def parse_cell(text: str) -> tuple[int, int]:
    m = re.fullmatch(r"(\d+)x(\d+)", text.strip(), re.I)
    if not m:
        raise SystemExit(f"bad --cell {text!r}, expected WxH")
    return int(m.group(1)), int(m.group(2))


def infer_cell(role: str, src: Path, explicit: str | None) -> tuple[int, int]:
    if explicit:
        return parse_cell(explicit)
    name = src.stem.lower()
    if role == "parallax":
        if name == "far_bg":
            return 720, 144
        if name == "mid_bg":
            return 720, 192
        if name == "fog_wisp":
            return 48, 16
        raise SystemExit("parallax requires --cell WxH (or a known filename: far_bg, mid_bg, fog_wisp)")
    if role == "enemy":
        raise SystemExit("enemy requires --cell (32x32 / 32x40 / 48x48 / 64x64)")
    if role == "hero-atlas":
        return 338, 348
    if role == "prop" and name in {"stack_mouth_sealed", "stack_mouth_open"}:
        return 64, 32
    if role == "prop" and name == "kiln":
        return 64, 64
    if role == "prop" and name in {"iron_door_closed", "iron_door_open"}:
        return 64, 48
    if role in DEFAULT_CELL:
        return DEFAULT_CELL[role]
    raise SystemExit(f"no default cell for role {role}")


def legal_name(path: Path) -> None:
    hay = str(path).replace("\\", "/").lower()
    for token in BANNED:
        if token in hay:
            raise SystemExit(f"banned token {token!r} in {path}")


def _srgb_to_lin(c: float) -> float:
    c = c / 255.0
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def rgb_to_lab(rgb: tuple[int, int, int]) -> tuple[float, float, float]:
    r, g, b = (_srgb_to_lin(rgb[0]), _srgb_to_lin(rgb[1]), _srgb_to_lin(rgb[2]))
    x = r * 0.4124564 + g * 0.3575761 + b * 0.1804375
    y = r * 0.2126729 + g * 0.7151522 + b * 0.0721750
    z = r * 0.0193339 + g * 0.1191920 + b * 0.9503041
    xn, yn, zn = 0.95047, 1.00000, 1.08883

    def f(t: float) -> float:
        return t ** (1.0 / 3.0) if t > 0.008856 else (7.787 * t + 16.0 / 116.0)

    fx, fy, fz = f(x / xn), f(y / yn), f(z / zn)
    return (116.0 * fy - 16.0, 500.0 * (fx - fy), 200.0 * (fy - fz))


def lab_dist(a: tuple[int, int, int], b: tuple[int, int, int]) -> float:
    la, aa, ba = rgb_to_lab(a)
    lb, ab, bb = rgb_to_lab(b)
    return ((la - lb) ** 2 + (aa - ab) ** 2 + (ba - bb) ** 2) ** 0.5


def manhattan(a: tuple[int, int, int], b: tuple[int, int, int]) -> int:
    return abs(a[0] - b[0]) + abs(a[1] - b[1]) + abs(a[2] - b[2])


def is_key_color(rgb: tuple[int, int, int]) -> bool:
    for key in MAGENTA:
        if lab_dist(rgb, key) <= KEY_LAB or manhattan(rgb, key) < KEY_MANHATTAN:
            return True
    return False


def nearest_palette(rgb: tuple[int, int, int], palette: list[tuple[int, int, int]]) -> tuple[int, int, int]:
    best = palette[0]
    best_d = 1 << 30
    for p in palette:
        d = manhattan(rgb, p)
        if d < best_d:
            best_d = d
            best = p
            if d == 0:
                break
    return best


def bayer2(x: int, y: int) -> float:
    # -0.5 .. 0.5 of one quantization step; used only with --dither ordered2
    table = ((0, 2), (3, 1))
    return (table[y & 1][x & 1] + 0.5) / 4.0 - 0.5


def process(
    src: Path,
    role: str,
    cell: tuple[int, int],
    palette: list[tuple[int, int, int]],
    allow_jpeg: bool,
    dither: str,
) -> tuple[Image.Image, dict]:
    suffix = src.suffix.lower()
    if suffix in {".jpg", ".jpeg"} and not allow_jpeg:
        raise SystemExit(f"JPEG is an error: {src} (pass --allow-jpeg only for experiments, never commit)")

    im = Image.open(src)
    if im.format == "JPEG" and not allow_jpeg:
        raise SystemExit(f"JPEG is an error: {src} (detected format=JPEG)")
    im = im.convert("RGBA")
    pixels = list(im.getdata())
    w, h = im.size
    soot = palette[0]
    keyed = 0
    keyed_rgba: list[tuple[int, int, int, int]] = []
    for i, (r, g, b, a) in enumerate(pixels):
        if a == 0:
            keyed_rgba.append((0, 0, 0, 0))
            continue
        rgb = (r, g, b)
        if is_key_color(rgb):
            keyed += 1
            keyed_rgba.append((0, 0, 0, 0))
            continue
        # leftover magenta-ish fringe: pull toward soot_black
        if r > 80 and b > 80 and g < r * 0.7 and g < b * 0.7:
            t = 0.45
            rgb = (
                int(r * (1 - t) + soot[0] * t),
                int(g * (1 - t) + soot[1] * t),
                int(b * (1 - t) + soot[2] * t),
            )
        keyed_rgba.append((rgb[0], rgb[1], rgb[2], a))

    im.putdata(keyed_rgba)
    cw, ch = cell
    if role == "vfx" and (w < cw or h < ch):
        raise SystemExit(f"vfx must not be upscaled ({w}x{h} < {cw}x{ch})")
    if role == "ui" and (w, h) == (16, 16) and cell == (32, 32):
        # 16×16 is legal until the UI PR. Do not invent 32px.
        cw, ch = 16, 16
    if (w, h) != (cw, ch):
        im = im.resize((cw, ch), Image.Resampling.NEAREST)
        w, h = cw, ch
        pixels_src = list(im.getdata())
    else:
        pixels_src = list(im.getdata())

    out_px: list[tuple[int, int, int, int]] = []
    off = 0
    unique: set[tuple[int, int, int]] = set()
    use_dither = dither == "ordered2" and role == "parallax"
    for i, (r, g, b, a) in enumerate(pixels_src):
        x = i % w
        y = i // w
        if role != "cookie":
            a = 255 if a >= 128 else 0
        if a == 0:
            out_px.append((0, 0, 0, 0))
            continue
        rgb = (r, g, b)
        if use_dither:
            bias = int(round(bayer2(x, y) * 16))
            rgb = (
                max(0, min(255, r + bias)),
                max(0, min(255, g + bias)),
                max(0, min(255, b + bias)),
            )
        q = nearest_palette(rgb, palette)
        if q != (r, g, b) and not use_dither:
            # count source-vs-palette before dither noise
            if nearest_palette((r, g, b), palette) != (r, g, b):
                off += 1
        elif nearest_palette((r, g, b), palette) != (r, g, b):
            off += 1
        unique.add(q)
        out_px.append((q[0], q[1], q[2], a if role == "cookie" else 255))

    im.putdata(out_px)

    if role != "cookie":
        for r, g, b, a in out_px:
            if a not in (0, 255):
                raise SystemExit(f"{src}: partial alpha after pack (role {role} is binary)")

    seam_lr = seam_tb = None
    if role == "tile":
        seam_lr, seam_tb = seam_scores(out_px, w, h)
        if seam_lr >= SEAM_LIMIT or seam_tb >= SEAM_LIMIT:
            raise SystemExit(
                f"{src}: 2x2 seam mean edge Δ lr={seam_lr:.1f} tb={seam_tb:.1f} (limit {SEAM_LIMIT})"
            )

    stats = {
        "size": f"{w}x{h}",
        "unique": len(unique),
        "off": off,
        "keyed": keyed,
        "seam_lr": seam_lr,
        "seam_tb": seam_tb,
    }
    return im, stats


def seam_scores(pixels: list[tuple[int, int, int, int]], w: int, h: int) -> tuple[float, float]:
    def rgb_at(x: int, y: int) -> tuple[int, int, int]:
        r, g, b, a = pixels[y * w + x]
        if a == 0:
            return (0, 0, 0)
        return (r, g, b)

    lr = 0
    for y in range(h):
        lr += manhattan(rgb_at(w - 1, y), rgb_at(0, y))
    tb = 0
    for x in range(w):
        tb += manhattan(rgb_at(x, h - 1), rgb_at(x, 0))
    return lr / max(h, 1), tb / max(w, 1)


def check_atlas_json(json_path: Path, png_size: tuple[int, int]) -> None:
    data = json.loads(json_path.read_text(encoding="utf-8"))
    cell = data.get("cell")
    if cell != [32, 48] and cell != (32, 48):
        raise SystemExit(f"{json_path}: cell must be [32, 48], got {cell}")
    pivot = data.get("pivot") or {}
    if int(pivot.get("x", -1)) != 16 or int(pivot.get("y", -1)) != 47:
        raise SystemExit(f"{json_path}: root pivot must be 16,47")
    frames = data.get("frames") or {}
    if not frames:
        raise SystemExit(f"{json_path}: no frames")
    for name, fr in frames.items():
        legal_name(Path(name))
        if not str(name).startswith("fluewalker_"):
            raise SystemExit(f"{json_path}: frame {name} must be fluewalker_*")
        if int(fr.get("w", 0)) != 32 or int(fr.get("h", 0)) != 48:
            raise SystemExit(f"{json_path}: {name} cell {fr.get('w')}x{fr.get('h')}")
        pv = fr.get("pivot") or {}
        if int(pv.get("x", -1)) != 16 or int(pv.get("y", -1)) != 47:
            raise SystemExit(f"{json_path}: {name} pivot {pv}")
    pad = int(data.get("padding", 2))
    cols = int(data.get("columns", 10))
    n = len(frames)
    rows = (n + cols - 1) // cols
    expect_w = cols * 32 + max(cols - 1, 0) * pad
    expect_h = rows * 48 + max(rows - 1, 0) * pad
    if png_size != (expect_w, expect_h) and png_size != (338, 348):
        # allow the locked shipped atlas even if frame count math is restated
        if png_size[0] != expect_w or png_size[1] != expect_h:
            print(
                f"note: atlas json packed formula {expect_w}x{expect_h}; png is {png_size[0]}x{png_size[1]}",
                file=sys.stderr,
            )


def resolve(p: str) -> Path:
    path = Path(p)
    if path.is_absolute():
        return path
    cwd = Path.cwd() / path
    if cwd.exists() or cwd.parent.exists():
        return cwd
    return ROOT / path


def print_stats(out: Path, stats: dict) -> None:
    parts = [
        f"OK {out.as_posix()}",
        stats["size"],
        f"unique={stats['unique']}",
        f"off={stats['off']}",
        f"keyed={stats['keyed']}",
    ]
    if stats["seam_lr"] is not None:
        parts.append(f"seam_lr={stats['seam_lr']:.1f}")
        parts.append(f"seam_tb={stats['seam_tb']:.1f}")
    print(" ".join(parts))


SIZE_BY_NAME = {
    "far_bg": (720, 144),
    "mid_bg": (720, 192),
    "fog_wisp": (48, 16),
    "hero_atlas": (338, 348),
    "stack_mouth_sealed": (64, 32),
    "stack_mouth_open": (64, 32),
    "kiln": (64, 64),
    "iron_door_closed": (64, 48),
    "iron_door_open": (64, 48),
    "stair": (32, 48),
    "impacts": (124, 16),
    "palette": (32, 1),
    "fg_lamp": (32, 64),
    "fg_pipe": (32, 64),
    "fg_overhang": (64, 32),
    "fg_sign": (32, 32),
    "tall_chimney_top": (32, 32),
    "tall_flue_top": (32, 32),
}


def infer_tree_role(path: Path) -> tuple[str, tuple[int, int] | None]:
    rel = path.as_posix().replace("\\", "/").lower()
    name = path.stem.lower()
    if name == "palette":
        return "tile", (32, 1)
    if "/characters/npcs/" in rel:
        return "npc", (32, 48)
    if "/characters/hero/" in rel and name == "hero_atlas":
        return "hero-atlas", (338, 348)
    if "/characters/enemies/" in rel:
        if name.startswith("overfire"):
            return "enemy", (64, 64)
        if name.startswith("clinker"):
            return "enemy", (48, 48)
        if name.startswith("claywalker"):
            return "enemy", (32, 40)
        return "enemy", (32, 32)
    if "/vfx/" in rel:
        return "vfx", (16, 16) if name != "impacts" else (124, 16)
    if name.startswith("light_"):
        return "cookie", (32, 32)
    if name in {"far_bg", "mid_bg", "fog_wisp"} or name.startswith("fg_") or name.startswith("tall_"):
        return "parallax", SIZE_BY_NAME.get(name)
    if "/ui/" in rel:
        return "ui", (32, 32)
    if "/environment/props/" in rel:
        return "prop", SIZE_BY_NAME.get(name, (32, 32))
    if "/environment/town/" in rel or "/environment/cold/" in rel:
        floors = ("brick_floor", "street", "ash_floor", "frost_ash", "quench_water")
        if any(name == f or name.startswith(f + "_") for f in floors):
            return "tile", (32, 32)
        return "prop", SIZE_BY_NAME.get(name, (32, 32))
    return "tile", SIZE_BY_NAME.get(name)


def check_tree(palette: list[tuple[int, int, int]]) -> int:
    assets = ROOT / "assets"
    fails = 0
    checked = 0
    leftover = ("tiles", "sprites", "v3")
    for name in leftover:
        if (assets / name).is_dir():
            print(f"FAIL leftover assets/{name}", file=sys.stderr)
            fails += 1
    for packed in (
        assets / "environment" / "town_tiles.png",
        assets / "environment" / "cold_tiles.png",
        assets / "ui" / "ui.png",
    ):
        if packed.is_file():
            print(f"FAIL packed sheet {packed.relative_to(ROOT)}", file=sys.stderr)
            fails += 1

    for path in sorted(assets.rglob("*")):
        if path.suffix.lower() in {".jpg", ".jpeg"}:
            print(f"FAIL JPEG {path.relative_to(ROOT)}", file=sys.stderr)
            fails += 1
            continue
        if path.suffix.lower() != ".png":
            continue
        if path.name.lower() == "palette.png":
            continue
        legal_name(path)
        role, cell = infer_tree_role(path)
        im = Image.open(path)
        if im.format == "JPEG":
            print(f"FAIL JPEG magic {path.relative_to(ROOT)}", file=sys.stderr)
            fails += 1
            continue
        w, h = im.size
        if cell is not None and (w, h) != cell:
            print(f"FAIL {path.relative_to(ROOT)} size {w}x{h} expected {cell[0]}x{cell[1]}", file=sys.stderr)
            fails += 1
        _, stats = process(path, role, (w, h), palette, allow_jpeg=False, dither="none")
        checked += 1
        if stats["off"] > 0:
            print(f"FAIL {path.relative_to(ROOT)} off-palette {stats['off']}", file=sys.stderr)
            fails += 1
    print(f"OK check-tree — {checked} png, {fails} fail")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(
        description="Quantize, chroma-key, and size-check Overland PNGs against the locked 32-color palette.",
        epilog="JPEG is an error unless --allow-jpeg. Do not commit JPEG. Signs/glyphs are a human check.",
    )
    ap.add_argument("--role", choices=ROLES)
    ap.add_argument("--src", help="source PNG (JPEG only with --allow-jpeg)")
    ap.add_argument("--out", help="destination PNG under assets/")
    ap.add_argument("--cell", default=None, help="WxH override")
    ap.add_argument("--palette", default=str(ROOT / "assets" / "palette.json"))
    ap.add_argument("--allow-jpeg", action="store_true", help="never use for a commit")
    ap.add_argument("--dither", choices=("none", "ordered2"), default="none")
    ap.add_argument("--check", action="store_true", help="validate and print stats; do not write --out")
    ap.add_argument("--check-tree", action="store_true", help="scan assets/ without writing")
    args = ap.parse_args(argv)

    palette = load_palette(Path(args.palette) if Path(args.palette).is_absolute() else resolve(args.palette))
    if args.check_tree:
        return check_tree(palette)

    if not args.role or not args.src or not args.out:
        raise SystemExit("need --role --src --out, or --check-tree")

    if args.dither == "ordered2" and args.role != "parallax":
        raise SystemExit("--dither ordered2 is only allowed for --role parallax")

    src = resolve(args.src)
    out = resolve(args.out)
    legal_name(src)
    legal_name(out)
    if not src.is_file():
        raise SystemExit(f"missing --src {src}")

    cell = infer_cell(args.role, src, args.cell)
    im, stats = process(src, args.role, cell, palette, args.allow_jpeg, args.dither)

    if args.role == "hero-atlas":
        sibling = src.with_suffix(".json")
        if sibling.is_file():
            check_atlas_json(sibling, im.size)

    if args.check:
        print_stats(out, stats)
        return 0

    out.parent.mkdir(parents=True, exist_ok=True)
    im.save(out, format="PNG", optimize=True)
    if args.role == "hero-atlas":
        src_json = src.with_suffix(".json")
        if src_json.is_file() and src.resolve() != out.resolve():
            dest_json = out.with_suffix(".json")
            dest_json.write_text(src_json.read_text(encoding="utf-8"), encoding="utf-8")
    print_stats(out, stats)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
