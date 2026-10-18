# Test report: The Pump House

**Build revision:** cloud copy, 2026-10-18, applied to the dev PC the same day (not yet committed there).
**Declared target:** Windows desktop (Unity 6000.6.5f1, URP), keyboard + mouse and Xbox-style gamepad, solo.

## Implemented, partial, planned

| | Feature |
|---|---|
| **Implemented (in code, compile-checked in the cloud)** | Three rooms; power cell + charging / output cradles; counterweight gate and drawbridge with gauge and safety zones; winch motor; Pebble rescue with a world change (pumps, lamps, beacons, puddles, exit); per-room reset pads; layered hints on H / D-pad down; side-call save that can't touch campaign unlocks; latched room doors; no idle battery drain; scripted self-test |
| **Partial** | Visual pass: primitives and toon materials in the warm-workshop palette, no bespoke models, sound or animation for the new machines. Hints are spoken by DISPATCH only (no on-screen hint button). |
| **Planned, not built** | Co-op roles for the slice; mid-room save/load (only completion is saved); before/after postcard; gamepad prompt icons; stitching the Pump House into the Brightwater world. |

## What was actually run

| Check | Where | Result |
|---|---|---|
| Whole runtime (`Scripts/**`) compiles | Cloud container, .NET 8 against stand-in Unity APIs (`.cloud-compile/full`) | **Pass**, 0 errors |
| Every editor builder (`Editor/*.cs`, not `Editor/Media`) compiles | Same | **Pass**, 0 errors |
| Compiles in Unity 6000.6.5f1; `Build Everything` | Dev PC, Windows 11 | **Pass**, clean, 20 scenes |
| Pump House self-test, headless (`-executeMethod ...PumpHouseSelfTestRunner.RunFromCommandLine`) | Dev PC, Play Mode | **Pass, 21/21** (full log in `selftest-last.txt` next to this file). First run hung at exit: the project enters Play Mode without a domain reload, so the runner never started waiting for the result. Fixed in `PumpHouseSelfTestRunner.RunFromCommandLine`. |
| A person plays it | Dev PC | **Not run yet** |

The stand-in APIs accept more than Unity does, so the cloud pass catches C# mistakes, not misuse of Unity APIs.
No performance, frame-rate or controller claims are made; nothing has been measured.

## Automated self-test (what it checks)

The log on the PC lists 21 PASS lines; some items below are split into two lines there.

It moves objects directly (it can't press keys), so it tests rules, triggers and state, not controls or camera.

1. Clean start: door shut, gate down, bridge up, 5 steps open.
2. Standing still off a charger doesn't drain the battery.
3. Hints come goal first, then system.
4. Empty cell in the door cradle: cradle sees it, door stays shut (wrong order is safe).
5. "Plugging in" at the station fills the cell; step `cell_charged` completes.
6. Full cell in the door cradle opens the door; step `door1`.
7. Taking the cell out again: door stays open (no softlock).
8. Room 1 reset: cell home and empty; door and finished steps untouched.
9. One crate: gate moves 10-40 % and doesn't open. Two crates: open, step `gate`, and it stays open untouched.
10. Something under the gate stops it closing.
11. Room 2 reset: crates home, gate closes, other rooms untouched.
12. Two crates: bridge partway. Plus the empty cell: bridge fully down, step `bridge` (answer B).
13. Room 3 reset: bridge back up, cell home and empty.
14. Charged cell in the winch cradle: bridge fully down by motor (answer A).
15. Before the rescue the exit is shut and pumps still; Pebble on the gauge completes the task, opens the exit, runs pumps.
16. Room 3 reset after the rescue: Pebble stays home.

## Manual playtest script (for the first person to play it)

Record: tester, Windows version, input device, build revision, what happened.

1. From the main menu: Choose a call → Side calls → *Side Call: The Pump House*. Read DISPATCH's intro.
2. Without hints, can you say what the door needs? Note how long until you plug in at the station.
3. Put the empty cell in the door cradle first on purpose. Does the amber lamp and line explain it?
4. Room 2: stand in the pan yourself. Does the gate's partial lift read as "more weight"?
5. Tether-pull the shelf crate. Throw a crate into a corner, then use the reset pad.
6. Room 3: solve it one way, then press Backspace and solve it the other way.
7. Fall in the sump on purpose. Where do you come back, and where do the objects go?
8. Ask the tester to explain why their solution worked. Note the words they use.
9. After the exit flag: is the side call ticked on the menu? Is the campaign's Continue unchanged?
10. Repeat 1-3 with a gamepad.

## Known issues and risks (unverified until the PC run)

- Moving a kinematic gate/bridge with physics objects resting on it hasn't been tuned; objects might jitter on the bridge as it lowers.
- The Pebble drone keeps its Helper Drone behaviour (it shares charge when carried); intended to read as friendly, untested.
- Puddles "drain" by sinking 0.2 m slowly; they may z-fight with the floor at the start.
- The reset pad and hint volumes are big triggers; they're ignored by every raycast in the project, but a stray interaction is possible.
- Battery change is game-wide: every level now drains only while moving or doing powered work.
