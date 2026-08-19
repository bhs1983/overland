# Overland Slice 0 — locked art contract

Original pixel assets, AI-drawn for Brandon Smith (studio Overland). No third-party packs. `LEGAL.md` is locked: not TES, not Zelda, not a port. Do not use Whimble/Whimsicle. Hero job is flue-walker. Filenames use `fluewalker_*` only.

This file is the **only** art contract. It supersedes the Checkpoint-2 16-color spec (`assets/ART_CP2_HISTORICAL.md`). `assets/v3/ART.md` is a pointer here.

Full unification plan (engine PRs, CheckArt lifecycle, risk): `docs/ART_UNIFICATION_PLAN.md`.

## Transitional vs locked

| | On disk today | Locked destination |
|---|---|---|
| Palette | **32 v3 colors** (`palette.json` + `palette.png`) | same — this file |
| `Palette.cs` hex | v3 table; `Iron` is `#3A4046`, not an AshDark alias | same |
| World tiles | CP2 16×16 under `assets/tiles/` | 32×32 individuals under `assets/environment/` |
| Items / UI | 16×16 | 32×32 |
| Hero / NPCs / enemies | v3 sizes already | same cells / pivots |
| `Tiles.Size` / camera | 16 / zoom 3 | **32 / zoom 2** (engine PR; do not flip here) |
| Packed `town_tiles.png` / `cold_tiles.png` / `props.png` / `enemies.png` | **absent — do not add** | still not Slice 0 on-disk |

Slice 0 on-disk world art is **individual PNGs**, one file per name. Packed sheets are an optional packer output **after** Slice 0, never a required game file.

## Cells

| Role | Cell (px) | Pivot (relative to cell) | Examples |
|---|---|---|---|
| Floor / wall tile | **32×32** | n/a (tile origin top-left) | brick, street, ash, flue wall |
| Prop (1-tile) | **32×32** | bottom-center **(16, 31)** | ash pile, chest, dead fan, items in-world |
| Prop (2-tile wide) | **64×32** or **64×48** | bottom-center of the combined box | stack mouth, iron door |
| Kiln prop | **64×64** | bottom-center **(32, 63)** | Kilnwalk kiln |
| Hero + NPCs | **32×48** | **(16, 47)** — soles on rows 46–47 | flue-walker, Tamsin, Holt, Wren, Rook |
| Small enemy | **32×32** | **(16, 31)** | Sootling, Brickleech |
| Claywalker | **32×40** | **(16, 39)** | already on disk; do not flatten to 32×32 |
| Clinker | **48×48** | **(24, 47)** | already on disk |
| Overfire | **64×64** | **(32, 63)** | already on disk |
| Item / UI icon | **32×32** after the UI PR; **16×16 on disk until then** | n/a (Control) | Crackiron, Folded Bellows, Stack Key, health pip, map nodes |
| VFX particle | **16×16** | center | spark, ember, smoke, ash_fall — **never ×2** |
| Light cookie | **32×32** | center | lantern, kiln, quench, overfire — grandfathered alpha |
| Parallax far | 480×96 until the sky PR; then **720×144** | n/a, repeat X | same screen width as today at zoom 2 |
| Parallax mid | 480×128 until the sky PR; then **720×192** | n/a, repeat X | same |
| FG prop | 32×64 / 64×32 / 32×32 | as today | **do not 2×**; already 32px family |

Gutters on *optional* packed sheets (post-Slice 0): **2px transparent** between cells, **0 on the outer edge**.

## Palette (locked 32)

Single source of truth:

- `res://assets/palette.json` — 32 entries, names + hex.
- `res://assets/palette.png` — 32×1 strip, index = x.
- `scripts/Util/Palette.cs` — hex literals must match the JSON byte-for-byte.

Transparent is not a palette color. RGB of every opaque pixel ∈ the 32. No gold. No green. No dither except ordered 2×2 of these 32 colors (parallax grain only).

| # | hex | name |
|---|-----|------|
| 0 | `#0A090B` | soot_black |
| 1 | `#1B1613` | deep_soot |
| 2 | `#3C2B21` | dark_brick |
| 3 | `#72402C` | fired_clay |
| 4 | `#B05C32` | kiln_terracotta |
| 5 | `#DC7A38` | kiln_orange |
| 6 | `#ECA45A` | ember |
| 7 | `#F2CA8C` | canvas_highlight |
| 8 | `#282A2E` | ash_dark |
| 9 | `#5C6064` | ash_grey |
| 10 | `#8E9397` | ash_light |
| 11 | `#C6C4BA` | canvas |
| 12 | `#EAE6DA` | wrap_bone |
| 13 | `#163848` | cold_draft_deep |
| 14 | `#3A6C7C` | cold_draft |
| 15 | `#7C9EAA` | cold_draft_light |
| 16 | `#110D0B` | soot_void |
| 17 | `#523628` | mid_brick |
| 18 | `#945032` | clay_mid |
| 19 | `#F4B464` | kiln_bloom |
| 20 | `#F8D8A4` | fire_lip |
| 21 | `#B6B2A8` | canvas_mid |
| 22 | `#9EA4A8` | ash_bright |
| 23 | `#224858` | cold_mid |
| 24 | `#8CB0B8` | bad_air |
| 25 | `#C86C38` | terracotta_hot |
| 26 | `#2C201A` | brick_shadow |
| 27 | `#726C64` | ash_warm |
| 28 | `#D4A072` | canvas_dust |
| 29 | `#18120E` | hair_deep |
| 30 | `#3A4046` | iron |
| 31 | `#6E7478` | iron_light |

`Palette.Iron` is **iron**, not an alias of ash_dark.

## Filter / mipmaps / alpha

For every game PNG:

- Filter = **Nearest** (`CanvasItem.TextureFilterEnum.Nearest`; `project.godot` `default_texture_filter=0`).
- Mipmaps = **Off**.
- Repeat = **Disabled**, except far/mid parallax strips.
- Alpha:
  - **Binary** (0 or 255) for tiles, props, characters, VFX, UI, FG, far/mid.
  - **Cookies (`light_*.png`) grandfathered:** any `a ∈ [0,255]` is legal; RGB still ∈ the 32.
- No JPEG. No magenta plates. `process/fix_alpha_border=true` on the `.import` sidecar.
- Canonical runtime load: `Assets.LoadPngNearest`. Godot import is a preview, not the source of truth.

## Camera and grid (locked destination)

| | Today | Locked |
|---|---|---|
| `Tiles.Size` | 16 | **32** |
| Camera zoom | 3 | **2** (integer nearest) |
| Viewport | 1280×720 | unchanged |
| Screen px / tile | 48 | **64** |
| Hero in tiles | 2 × 3 | **1 × 1.5** |

Zoom 3 + 32px is rejected (FOV too tight for Kilnwalk 20×15). Do not pick zoom 1.5.

`Tiles.Cell(x, y)` is the center of cell `(x,y)`. Slice 0 sockets are **exactly two consecutive floor cells = 64px** at the cell indices already authored. Do not recenter 14-wide rooms. No 3-tile mouth in Slice 0.

## Folder tree (destination)

```
res://assets/
  ART.md
  ART_CP2_HISTORICAL.md
  palette.json                    # 32 colors
  palette.png                     # 32×1
  characters/hero/                # hero_atlas.png + json
  characters/enemies/             # individuals only
  characters/npcs/                # tamsin holt wren rook
  environment/town/               # brick_floor_a.png … door.png
  environment/cold/               # ash_floor_a.png … quench_water_b.png
  environment/props/              # chest_closed.png, dead_fan_0.png, …
  environment/parallax/kilnwalk/
  environment/parallax/cold_stack/
  environment/parallax/shared/
  vfx/                            # spark.png, impacts.png, …
  ui/                             # crackiron, folded_bellows, stack_key, HUD icons
```

There is **no** `ui/items/` subdirectory. Until flatten, live v3 characters / parallax / VFX stay under `assets/v3/`. CP2 tiles stay under `assets/tiles/` and leftover 16px hero frames under `assets/sprites/hero/` (unused).

Do **not** add `assets/v3/environment/town_tiles.png` (or `cold_tiles.png` / `props.png`). QaCp3 currently asserts those packed sheets are absent.

## Pivot (sacred)

- Every hero frame is **32×48**. Do not trim the canvas down.
- Same pivot on every frame: **bottom-center of the boots**.
- Pivot pixel: **x=16, y=47**. Soles sit on row **46–47**. Re-plant; do not crop.
- Engine helper (later): `Centered = false`, `Offset = (-cellW/2, -(cellH-1))`.
- Mouth / iron / fan-east gates stay AABB-centered until native 64px art and `Scale (1,1)`.

Hero atlas is the live source: `characters/hero/hero_atlas.png` + `.json`. 69 `fluewalker_*` frames, 2px gutters, 10 columns. Dual export (strips + individuals) is optional packer output.

## Naming

- Hero files: `fluewalker_*` only. **Never** Whimble / Whimsicle / whimble-style.
- Creatures: `sootling`, `claywalker`, `brickleech`, `clinker`, `overfire` (+ `_pulse` / `_swipe` / future `_hurt` / `_cracked` / `_soft`).
- NPCs: `tamsin`, `holt`, `wren`, `rook`.
- Town files: `brick_floor_a`…`f`, `street_a`…`d`, `brick_wall`, `kiln`, `night_fire_0`…`3`, `stack_mouth_sealed`, `stack_mouth_open`, `door` (**32×32**).
- No TES/Zelda tokens in filenames.
- No readable text in any PNG (`fg_sign.png` already complies). Localization is labels/toasts only.

## Locked names

- Hero job: flue-walker (`fluewalker_*`).
- Sword Crackiron, tool Folded Bellows, key Stack Key.
- Creatures: Sootling, Claywalker, Brickleech, Clinker, Overfire.

## Silhouette / legal (accept on every new tile)

- Flue-walker: slim 21px square workman canvas coat, hanging sleeves, straight hem, cloth wrap over mouth and nose, short soot hair, **no cap**. Not an egg-coat. Not a pointed-cap hero. Not green. Not a tunic.
- Overfire: walking kiln-mouth; open-top chimney, brick kiln-box body, fire in the torso arch, pillar legs. Not a dragon. Swipe is a kiln-arch heat slash, **not** a fire sword.
- Chest: flat crate, iron hasp (iron/ash only). Not a rounded gold-lock box.
- Crackiron: thick short splitting iron, clay haft.
- Hop is a grounded dodge; boots stay on the bottom rows. Not a platformer jump.
- Dead fan / x-fan: blades in a circle or diagonal. Not a cross. Not a Triforce.
- Ember pips, not hearts. Iron hasp, not gold.
- Wren may be a little uncanny. Full glow-eyes fail tone.

## VFX off the body

Swing impact sparks are **not** baked into body frames. Body swings must read without the VFX sheet.
`vfx/impacts.png` (2px gutters) is: spark, spark_b, ember, ember_b, smoke, smoke_b, ash_fall.
Individuals stay in `vfx/` as well.

## Autotile / floor stamps

Floors are variant stamps (`brick_floor_a`…`f`, `ash_floor_a`… plus frost). 2×2 of any variant must be seamless. Walls are 1–3 individuals + `StaticBody2D`, not a 16-tile peering set in Slice 0. Do not autotile kilns, mouths, doors, chests, fans — those are prop nodes.

## Parallax

Orthographic 2D only. No 3D camera. No free camera. No day/night cycle.

| Layer | Rate | Contents |
|---|---|---|
| Far BG | 0.2× (lock 0.15–0.3) | looping, underscale, hazy |
| Mid BG | 0.5× (lock 0.4–0.6) | looping, muted + fog wash |
| Main | 1.0× | tiles, hero, interactables. **Hero stays on 1.0×.** |
| Foreground | 1.35× (lock 1.2–1.5) | Sparse props. Must not block doors or hits. |
| Particles | own speed | spark, ember, smoke, ash_fall; fog_wisp ~0.25× |

FG is sparse. Do not forest the plane. Cookies are 2D Light2D textures; RGB ∈ the 32; alpha grandfathered. No energy portals.

## Import

Engineer imports from disk. For every PNG: Filter = **Nearest**. Mipmaps = **Off**. Repeat = Enabled on far/mid strips only. Fix Alpha Border on.

## Filter (short)

Nearest-neighbor only. No anti-alias, no mipmaps, no bilinear, no gradients except dither of these 32 colors.
