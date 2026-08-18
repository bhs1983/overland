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
| Dodge-step | L or Shift |
| Interact / talk / save | E or Enter |
| Pause map | Esc or M |

### Checkpoint 1 — how to play (town QA)

1. Walk **Kilnwalk** (ridge street, kiln yard, night-fire, sealed stack mouth).
2. Talk to **Tamsin Cole** — take the hire (exact line from SLICE-0.md).
3. Talk to **Holt Vetch** — receive **Crackiron**; swing with J/Z.
4. Talk to **Wren Quill** after the hire — she marks the mouth on the pause map.
5. Talk to **Rook Darnel** — mouth stays sealed until hire; after hire he opens it.
6. Save at the **night fire** (interact). Load from title or pause.

Dungeon rooms are **not** in this PR (Checkpoint 2).

## Credits

Author of record: **Brandon Smith**.
Researched, designed, and packaged by AI agents for Brandon Smith.
Earnings and ownership accrue to Brandon. Agents cannot hold money, open store pages, or sign anything. Brandon publishes.

## Slice 0 (locked)

One original town. One dungeon of 10 authored rooms. Sword (Crackiron) + one tool (Folded Bellows). One item gate (Stack Key). Miniboss (the Clinker). Boss (the Overfire). Pause map. Save/load.

Do not generate a continent until Slice 0 is fun.

See [LEGAL.md](LEGAL.md), [DESIGN.md](DESIGN.md), and [SLICE-0.md](SLICE-0.md).
