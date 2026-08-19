# Overland Slice 0 — map

**Status: LOCKED** (Orchestrator + Director, 2026-08-18). Room list does not reopen.

One town. One dungeon. Ten authored rooms. Sword + one tool + one item gate. One miniboss. One boss.
Side Flue is a heal sink only. No extra side-dungeon paths. No continent. No runtime generator.

## Town — Kilnwalk

A ridge street, a kiln yard, a night-fire, the sealed stack mouth. Walk, talk, take the hire, get the sword, enter the dungeon. That is the whole town.

### NPCs

| Name | Role |
|---|---|
| **Tamsin Cole** | Kilnhand steward. Gives the hire: go into the Cold Stack, find why the draft reversed, shut it. |
| **Holt Vetch** | Retired setter. Gives **Crackiron**. Points at the sealed mouth. |
| **Wren Quill** | Keeps the night fire. Marks the Cold Stack on the pause map after you accept. |
| **Rook Darnel** | Door-warden. Unlocks the stack mouth after the hire is taken. |

No other named townsfolk in Slice 0. Hero is a flue-walker. Not “Whimble.” Names stay locked.

## Dungeon — The Cold Stack (10 rooms)

Same ten rooms. Environment talks: ash patterns, broken fans, residual heat, reversed draft. Each room shows a piece of why the stack failed. No new rooms.

| # | Room | Play | Environment talks (why it failed) |
|---|---|---|---|
| 1 | **Stack Mouth** | Entrance. One Sootling. Teach the swing. | Soot streaks on the ceiling run **down** toward the yard, not up the chimney. First read: draft reversed. |
| 2 | **Ashdrift Hall** | Ash piles block the way. Chest: **Folded Bellows**. | Ash is banked against the inner door, shoved **in**, not out. The stack has been coughing toward town. |
| 3 | **Dead Fan Walk** | Tool gate. Puff the bellows into the dead fan. East door opens. | Fan blades seized with clinker. Forcing air the old way is the only spin they have left. This is why the draft died. |
| 4 | **Setter's Alcove** | Two Claywalkers. Soften crust with bellows, then hit. Optional heal. | Half-set bricks. Tools dropped mid-work. Floor tiles still warm. The last crew left when the firing went wrong. |
| 5 | **Quench Trench** | Water channel. Brickleeches drop from the walls. Side path back to 3. | Quench water never dumped. They tried to kill the heat from here and failed. Same trench, not a second dungeon. |
| 6 | **Clinker Yard** | Miniboss: **the Clinker**. | A whole charge fused into one slag body. The failed firing, standing. |
| 7 | **Key Landing** | After the Clinker: **Stack Key**. | A setter’s ring left on a ledge above warped brick. Heat came from below. The key is a tool, not a relic. |
| 8 | **Sealed Flue** | Item gate. Stack Key opens the iron door. No other key in the slice. | Iron door bolted from this side after the air turned. Soot handprints point **down**. They sealed it to keep the reverse draft off Kilnwalk. |
| 9 | **Long Drop** | Last pack of Sootlings. Prep. | Vertical flue. Ash hangs on the upper lip; brick below is clean. Air is sucking down toward room 10. |
| 10 | **Overfire Chamber** | Boss: **the Overfire**. Stair back to Kilnwalk. | Residual heat that learned to keep itself. It is pulling air to feed. This is the reverse draft. |

Optional 11th room if Engineer needs a heal sink: **Side Flue**, off room 5, one heal, one Brickleech. Cool dead pocket. No story branch. Do not add a 12th.

## Gates

1. **Hire gate (town):** Rook will not open the mouth until Tamsin gives the hire.
2. **Tool gate (room 3):** Folded Bellows → dead fan → east door.
3. **Item gate (room 8):** Stack Key → iron door → Long Drop / boss.

## Combat cast (Slice 0 only)

| Name | Role |
|---|---|
| **Sootling** | Small ash-clump. 1 hit. Rushes. Bellows staggers it. |
| **Claywalker** | Slow. Crust soaks one sword hit until bellows softens it. |
| **Brickleech** | Wall cling. Drops when you pass. 1–2 hits. |
| **The Clinker** | Miniboss. Fused slag. Slow, armored. Bellows opens the cracks; Crackiron hits the cracks. |
| **The Overfire** | Boss. A walking bad firing. Telegraphed heat pulse (step out), close swipe (hit after). Bellows can shove the pulse ash, not skip the fight. |

## Quest text (keep this short in-game)

- Tamsin: “Draft’s running the wrong way. Walk the Cold Stack. Shut what you find. Coin when you come back up.”
- Holt: “Take Crackiron. It splits cooled clay. It’ll split what’s down there too.”
- Wren: “I marked the mouth. Don’t linger in the long heat.”
- Rook: “Mouth’s open. I lock it behind you if the air turns.”

No extra NPC lines. Rooms do the talking.

Boss down: Tamsin pays the hire. Slice ends. Save still works.

## Save / pause

Save at the night fire and at Stack Mouth. Pause map shows Kilnwalk plus rooms 1–10 as you enter them. No world map.

## Hold (do not spec)

Post-accept only: treat these 10 rooms as high-quality modules with consistent door / height / theme connection points so a later prefab-module + connection-grammar assembler can reuse them. Do not design that system now. No BSP. No WFC. No runtime generator in Slice 0.

## Accept checklist

- [x] Walk Kilnwalk
- [x] Enter the Cold Stack
- [x] Get Folded Bellows
- [x] Open the Dead Fan Walk gate
- [x] Beat the Clinker, get Stack Key
- [x] Open Sealed Flue
- [x] Beat the Overfire
- [x] Pause map
- [x] Save / load
- [x] Original names only
- [ ] QA PASS (headless rooms 1–10 green; live play still needed)
