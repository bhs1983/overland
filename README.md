# Overland

Original-IP top-down adventure for desktop Windows.
Engine is locked: **Godot 4.x, C# only**. Pixel-art, 16:9, top-down orthogonal camera. No first-person. No Unity, Unreal, or custom engine.

Working title for the first ship: **Overland Slice 0**.
First place: **Kilnwalk**. First dungeon: **the Cold Stack** (Checkpoint 2+).

## How to run (Godot 4 .NET / Windows)

1. Install **Godot 4.x .NET** (Mono) from https://godotengine.org — not the standard non-.NET build.
2. Install **.NET SDK 8** (or whatever your Godot 4.x release expects).
3. Open this repo folder in Godot (**Import** / **Open** → select `project.godot`).
4. Wait for C# restore / first build (Build button or build on play).
5. Press **Play** (F5). Title → New Game.

### Keys

| Action | Keys |
|---|---|
| Move | WASD or Arrow keys |
| Attack (Crackiron) | J or Z |
| Folded Bellows | K or X |
| Dodge-step | L or Shift |
| Interact / talk / save | E or Enter |
| Pause map | Esc or M |

### Checkpoint 1 — town

1. Walk **Kilnwalk** (ridge street, kiln yard, night-fire, sealed stack mouth).
2. Talk to **Tamsin Cole** — take the hire.
3. Talk to **Holt Vetch** — receive **Crackiron**.
4. Talk to **Wren Quill** after the hire — marks the mouth on the pause map.
5. Talk to **Rook Darnel** — opens the mouth after hire.
6. Save at the **night fire**.

### Checkpoint 2 — Cold Stack rooms 1–3

1. Enter the mouth → **Stack Mouth** (save point, one **Sootling** — teach the swing).
2. North → **Ashdrift Hall** — puff ash piles with Folded Bellows; open the chest for **Folded Bellows**.
3. East → **Dead Fan Walk** — puff the dead fan; east door opens.

### Checkpoint 3 — Cold Stack rooms 4–8

1. East from Dead Fan Walk → **Setter's Alcove** — two **Claywalkers** (bellows then Crackiron); optional heal.
2. South → **Quench Trench** — water channel, **Brickleeches** drop; side path back to room 3.
3. East → **Clinker Yard** — miniboss **the Clinker** (bellows opens cracks).
4. North after Clinker → **Key Landing** — **Stack Key**.
5. North → **Sealed Flue** — Stack Key opens the iron door. Rooms 9–10 later.

v3 hero atlas (`fluewalker_*`, pivot 16,47) + parallax layers (far 0.2 / mid 0.5 / FG 1.35 / fog 0.25). No 3D camera.

Rooms 9–10 (Long Drop, Overfire) are **not built yet**.

## Credits

Author of record: **Brandon Smith**.
Researched, designed, and packaged by AI agents for Brandon Smith.
Earnings and ownership accrue to Brandon. Agents cannot hold money, open store pages, or sign anything. Brandon publishes.

## Slice 0 (locked)

One original town. One dungeon of 10 authored rooms. Sword (Crackiron) + one tool (Folded Bellows). One item gate (Stack Key). Miniboss (the Clinker). Boss (the Overfire). Pause map. Save/load.

Do not generate a continent until Slice 0 is fun.

See [LEGAL.md](LEGAL.md), [DESIGN.md](DESIGN.md), and [SLICE-0.md](SLICE-0.md).
