# Overland Slice 0 — v3 Tiny Engineer import

Brandon owns it. Original, AI-drawn. Studio Overland. No third-party packs.

Do not use Whimble/Whimsicle. Hero job is flue-walker. Filenames use `fluewalker_*` only.

## Pivot (sacred)
- Every hero frame is **32×48**. Do not trim the canvas down.
- Same pivot on every frame: **bottom-center of the boots**.
- Pivot pixel: **x=16, y=47** (row 47 on the 32×48 cell).
- Soles sit on row **46–47**. Re-plant; do not crop.

## Dual export
Per-action strips **and** `characters/hero/hero_atlas.png`.
- Strips and atlas use **2px transparent padding between cells** (0 on the outer edge).
- Nearest only. No bilinear. No mipmaps.
- `hero_atlas.json` lists each frame’s x,y,w,h and pivot `{x:16,y:47}` relative to the frame.

## Folder layout (new; old `sprites/` + `tiles/` stay for CP2)
```
res://assets/v3/palette.png
res://assets/v3/palette.json
res://assets/v3/characters/hero/          strips + atlas + fluewalker_* individuals
res://assets/v3/characters/enemies/       individuals + enemies.png
res://assets/v3/environment/town_tiles.png
res://assets/v3/environment/cold_tiles.png
res://assets/v3/environment/props.png
res://assets/v3/environment/parallax/  far/mid/fg + light cookies
res://assets/v3/vfx/                      individuals + impacts.png
res://assets/v3/ui/                       existing ui (unchanged path)
```
CP2 still reads `sprites/` and `tiles/`. Leave those copies in place.

## Import
Engineer atlas-imports after CP2 PASS. Until then, keep using the 0-gutter `sprites/hero/*.png` strips.

For every PNG:
- Filter = **Nearest**
- Mipmaps = **Off**
- Repeat = Disabled (unless tiling a floor)
- Fix Alpha Border on so nearest upscale does not fringe

## Sizes
- Tiles / props: 32×32
- Hero cell: 32×48, pivot (16, 47)
- Sootling: 32×32
- Claywalker: 32×40
- Brickleech: 32×32
- Clinker: 48×48
- Overfire: 64×64
- Items / UI: 32×32
- VFX particles: 16×16

## Hero files (1-indexed, fluewalker_* only)
- Individuals: `fluewalker_idle_{dir}_01..04`, `fluewalker_walk_{dir}_01..06`,
  `fluewalker_swing_{dir}_01..04` (sparks stripped), `fluewalker_hurt_{dir}`,
  `fluewalker_hop_{dir}_01..02`, `fluewalker_victory_01`.
- Strips (2px gutters): `fluewalker_idle.png`, `fluewalker_walk.png`,
  `fluewalker_swing.png`, `fluewalker_hurt.png`, `fluewalker_hop.png`,
  `fluewalker_victory.png`. Order inside each strip: down, up, left, right.
- Atlas: `hero_atlas.png` + `hero_atlas.json`.

## VFX off the body
Swing impact sparks are **not** baked into body frames. Body swings must read without the VFX sheet.
`vfx/impacts.png` (2px gutters) is: spark, spark_b, ember, ember_b, smoke, smoke_b, ash_fall.
Individuals stay in `vfx/` as well.

## Locked names
- Hero job: flue-walker (`fluewalker_*`). Do not use Whimble/Whimsicle.
- Sword Crackiron, tool Folded Bellows, key Stack Key.
- Creatures: Sootling, Claywalker, Brickleech, Clinker, Overfire.

## Palette (locked 32)

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

Transparent sprite background is not a palette color.
Export is indexed / nearest to this table. No gold. No green cap.

## Environment sheets (2px gutters, nearest)
- `environment/town_tiles.png` — brick floor/wall variants, kiln, night_fire, street, mouths, door
- `environment/cold_tiles.png` — ash floor, frost_ash, flue brick, iron doors, quench_water, ledge, cracked_brick
- `environment/props.png` — barrel, bench, clay_stack, chest, dead_fan, x_fan, pipes, ash_pile

## Silhouette notes
- Flue-walker: slim 21px square workman canvas coat, hanging sleeves, straight hem, cloth wrap over mouth and nose, short soot hair, **no cap**. Not an egg-coat. Not a pointed-cap hero. Not green. Not a tunic.
- Overfire: walking kiln-mouth; open-top chimney, brick kiln-box body, fire in the torso arch, pillar legs. Not a dragon.
- Chest: flat crate, iron hasp (iron/ash only). Not a rounded gold-lock box.
- Crackiron: thick short splitting iron, clay haft.
- Hop is a grounded dodge; boots stay on the bottom rows. Not a platformer jump.
- X-fan: diagonal blades. Not a cross.
- Ember pips, not hearts. Iron hasp, not gold.

## Filter
Nearest-neighbor only. No anti-alias, no mipmaps, no bilinear, no gradients except dither of these 32 colors.

## Parallax (greenlit after CP2 PASS)

Orthographic 2D only. No 3D camera. No free camera. No day/night cycle.

### Scroll rates
| Layer | Rate | Contents |
|---|---|---|
| Far BG | 0.2× (lock 0.15–0.3) | `kilnwalk/far_bg.png` `cold_stack/far_bg.png` — looping, underscale, hazy |
| Mid BG | 0.5× (lock 0.4–0.6) | `kilnwalk/mid_bg.png` `cold_stack/mid_bg.png` — looping, muted + fog wash |
| Main | 1.0× | Existing town/cold tiles, hero, interactables. **Hero and gameplay-critical stay on 1.0×.** Do not move them. |
| Foreground | 1.35× (lock 1.2–1.5) | Sparse props. Must not block doors or hits. Stronger contrast. |
| Particles | own speed | Reuse `vfx/` spark, ember, smoke, ash_fall. `shared/fog_wisp.png` ~0.25× slow drift. |

### Files
```
environment/parallax/kilnwalk/
  far_bg.png          480×96   tile X
  mid_bg.png          480×128  tile X (transparent sky)
  fg_lamp.png         32×64    transparent
  fg_sign.png         32×32    transparent, no readable text
  fg_overhang.png     64×32    transparent, walk under
  tall_chimney_top.png 32×32   main-plane crown
  light_lantern.png   32×32    2D light cookie
  light_kiln.png      32×32    2D light cookie
environment/parallax/cold_stack/
  far_bg.png          480×96   tile X
  mid_bg.png          480×128  tile X (transparent sky)
  fg_pipe.png         32×64    transparent, walk behind
  fg_overhang.png     64×32    transparent, walk under
  tall_flue_top.png   32×32    main-plane crown
  light_quench.png    32×32    2D light cookie (cold-draft)
  light_overfire.png  32×32    softer BG heat cookie (kiln_orange, not gold)
environment/parallax/shared/
  fog_wisp.png        48×16    cold-draft / ash only
```

### FG
Sparse. A few lamp posts, hanging signs, overhangs, large pipes. Do not block doors or hits. Do not forest the plane.

### Tall props (main plane, not a 3D camera)
`tall_chimney_top` / `tall_flue_top` are extra sprites for chimney / flue / tall-lamp crowns.
Engineer: place with a slight independent Y-offset (about −4 to −8 px) so the crown reads above the body. Still 1.0× scroll. Not a 3D camera.

### 2D lights
Godot Light2D cookies: `light_lantern` (work-lantern) and `light_kiln` (kiln mouth) on Kilnwalk; `light_quench` + `light_overfire` on Cold Stack.
Distance falloff is in the cookie (stepped palette + alpha). Strongest on the 1.0× layer; use the overfire cookie softer / larger on mid BG. No energy portals. No day/night.

### Import
Engineer imports from disk. No GitHub from Art.
For every PNG: Filter = **Nearest**. Mipmaps = **Off**. Repeat = Enabled on far/mid strips only. Fix Alpha Border on.

### Filter
Nearest-neighbor only. No anti-alias, no mipmaps, no bilinear, no gradients except dither of the locked 32 colors (light cookies may step alpha).
