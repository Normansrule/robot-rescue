# Slice plan: The Pump House

A small, original, playable rescue-puzzle slice, built from the *Game Builder's Inspiration Atlas* route for Robot Rescue
("three-room rescue puzzle"). Side call, always open from **Choose a call > Side calls**. Scene:
`Assets/_Project/Scenes/Side_PumpHouse.unity`, built by `Robot Rescue > Side Calls > The Pump House`
(also part of `Build Everything`).

## Reference stack (one gameplay, one visual, nothing else)

| Role | Reference | What we studied | Original adaptation | Status of the observation |
|---|---|---|---|---|
| Gameplay anchor | Portal 2, mechanism demonstrations ([thinkwithportals.com/media.php](https://www.thinkwithportals.com/media.php)) and the atlas card | One device, one visible rule: input, what the device does, the result you can see. Teach a rule safely, then combine two. | No portals, no test-chamber look, no voice. Our rules are a **portable power cell** (power you can carry further than the cable reaches) and a **counterweight pan** (a mechanism that holds its state without you). | The atlas's principle and card. **The videos were not watched in this session**; no timestamps or specifics are claimed. |
| Visual anchor | Tinykin and I Am Future images on the supplied contact sheet | Small character in a big warm room; readable helpers in a group; exposed components with task framing; warm interior light. | Taller-than-needed rooms (7 m, 10 m hall) around a 1.2 m bot, warm boards with cream wainscot under soft teal, brass pipes, lamp bulbs, a charge station with its coils showing, a cell whose charge bars face both ways. Pebble is a readable focal helper with a name tag and LED face. | Observed from the contact sheet stills only (composition, palette, scale). Stills say nothing about motion or rules. **No reference image is in the project.** |
| Supporting | none | | | |

## Player promise

> I'm a small rescuer who works out how a place works, fixes it, and brings someone home.

## The loop

Arrive in a room → read the machine (lamps, wires, needle, bars) → try something → it partly works and shows why →
succeed, or reset just this room → see the place change → walk on.

## Rooms

1. **Charge Room: power transfer.** The door is fed by a cradle next to it, 12 m from the only socket (the Tether's
   power leash is 7 m). Plug in at the charge station to fill a power cell (four bars), unplug, carry the cell to the
   door cradle. Wrong order (empty cell in the door cradle) shows an amber lamp and DISPATCH explains.
2. **Gate Room: a held state.** A gate on a pulley; weight in a floor pan lifts it. Height follows (weight/needed)²,
   so one crate visibly lifts it a little and the needle shows half; two crates (one Tether-pulled from a high shelf)
   open it fully, and it stays open with nobody touching anything. Standing in the pan yourself doesn't help: you're
   not at the gate. It never closes on something underneath it.
3. **Pump Hall: both rules, two valid answers.** A drawbridge across the sump, also on a pan, plus a winch whose
   cradle is a motor when it holds a full cell.
   - Answer A (power): charge the hall's cell, carry it 11 m to the winch cradle.
   - Answer B (weight): the two crates are 3 of the 4.5 needed; the small surprise is that the cell is heavy too.
   Cross, pick up Pebble (who can't be Tether-yanked across; someone has to walk over), carry Pebble home to the tide
   gauge by the exit. The pumps spin, warm lamps come on, red beacons go off, puddles drain, the exit opens.

## State (authoritative)

| Thing | States | Who owns it |
|---|---|---|
| Power cell | charge 0..1 (full at 1); held / resting | `PowerCell` |
| Cradle | empty · cell not full (amber) · charging (blue pulse) · powering (green) | `CellCradle` |
| Gate / bridge | Open01 0..1, from pan weight or motor; open at ≥ 0.99; never closes on something in its safety zone | `CounterweightLift` + `WeighZone` |
| Room doors | closed · open (latched once powered) | `Door.latch` |
| Mission | 5 steps in `TaskRunner`: cell_charged, door1, gate, bridge, pebble | `TaskRunner`, raised by the pieces above |
| Rescue | Pebble home or not | `RescueDropZone` |
| Progress | side call complete (PlayerPrefs `rr.side.<scene>`), never touches the campaign unlock count | `CampaignProgress` |

The HUD checklist, DISPATCH lines, lamps, needle and doors all read from these components; nothing is a canned animation.

## Camera and controls

Existing third-person `BotCamera`. Keyboard + mouse and gamepad, same as the campaign. New: **H / D-pad down** for a
hint. **Reset a room**: stand on its red pad (circular-arrow mark) for 1.2 s.

## Success, failure, recovery

- Success: Pebble on the tide gauge (task complete), then the exit flag.
- Failure modes and recovery: fall in the sump → back to the hall checkpoint, objects to their start; objects thrown
  into corners → reset pad; wrong order → the machine shows why and nothing breaks; battery flat → existing tow to the
  last charger (service bays in rooms 2 and 3).
- Restart: Backspace / Select reloads the scene (existing).

## Also closed from DESIGN.md's gaps

- Battery no longer drains while standing still (only moving and powered work cost charge).
- Puzzle-local resets.
- Layered hints (goal → system → relationship → action).

## Scope cuts (deliberate)

No co-op roles (solo only; local co-op isn't in the game yet), no mid-room save, no new art assets (all primitives and
shaders already in the project), no audio beyond existing DISPATCH babble, not stitched into the open world.
