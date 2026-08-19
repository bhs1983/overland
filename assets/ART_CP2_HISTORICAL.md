# Overland Slice 0 — Tiny Engineer import (historical, Checkpoint 2)

**Superseded.** This is the 16-color / 16px Checkpoint-2 spec. The locked contract is `assets/ART.md`. Do not author new pixels against this file.

Original pixel assets, AI-drawn for Brandon Smith (studio Overland). No third-party packs.

## Sizes
- Tiles: 16×16
- Hero: 16×24 (5 frames)
- Small enemies: 16×16 (Sootling, Brickleech)
- Claywalker: 16×20
- Clinker: 24×24
- Overfire: 32×32
- Items / UI: 16×16

## Godot import
Path: `res://assets/...`

For every PNG:
- Filter = **Nearest**
- Mipmaps = **Off**
- Repeat = Disabled (unless tiling a floor)
- Fix alpha border (premult / Fix Alpha Border on) so nearest upscale does not fringe

Suggested layout:
```
res://assets/palette.png
res://assets/palette.json
res://assets/tiles/town.png
res://assets/tiles/town/*.png
res://assets/tiles/cold_stack.png
res://assets/tiles/cold_stack/*.png
res://assets/sprites/hero.png
res://assets/sprites/hero/*.png
res://assets/sprites/items.png
res://assets/sprites/items/*.png
res://assets/sprites/enemies.png
res://assets/sprites/enemies/*.png
res://assets/ui/ui.png
res://assets/ui/*.png
```

## Palette (historical 16)

| # | hex | name |
|---|-----|------|
| 0 | `#0B0A0C` | soot_black |
| 1 | `#1C1714` | deep_soot |
| 2 | `#3A2A22` | dark_brick |
| 3 | `#6B3A28` | fired_clay |
| 4 | `#A85A32` | kiln_terracotta |
| 5 | `#D4783A` | kiln_orange |
| 6 | `#E8A05A` | ember |
| 7 | `#F0C98A` | canvas_highlight |
| 8 | `#2A2C30` | ash_dark |
| 9 | `#5A5E62` | ash_grey |
| 10 | `#8B9094` | ash_light |
| 11 | `#C4C2BA` | canvas |
| 12 | `#E8E4D8` | wrap_bone |
| 13 | `#1A3A48` | cold_draft_deep |
| 14 | `#3D6A78` | cold_draft |
| 15 | `#7A9AA4` | cold_draft_light |

Transparent sprite background is not a palette color.

## Sheets
- `tiles/town.png` — 8×16×16 row: brick_floor, brick_wall, kiln, night_fire, street, stack_mouth_sealed, stack_mouth_open, door
- `tiles/cold_stack.png` — 10×16×16 row: ash_floor, flue_wall, iron_door_closed, iron_door_open, dead_fan, ash_pile, quench_water, chest, ledge, cracked_brick
- `sprites/hero.png` — 5×16×24: idle_down, idle_up, idle_left, idle_right, swing_down
- `sprites/items.png` — 3×16×16: crackiron, folded_bellows, stack_key
- `sprites/enemies.png` — packed row (see individuals for true sizes)
- `ui/ui.png` — 6×16×16: health_pip, map_node_town, map_node_room, map_node_here, save_mark, pause_frame

## Silhouette notes
- Flue-walker: canvas coat, cloth wrap over mouth and nose, short soot hair, **no cap**, wide shoulders. Not a pointed-cap hero.
- Overfire: walking kiln-mouth; square stack, brick body, fire in the arch. Not a dragon.
- Chest: flat iron-banded crate, square hasp. Not a rounded gold-lock chest.

## Filter
Nearest-neighbor only. No anti-alias, no mipmaps, no gradients except dither of these 16 colors.
