# Robot Rescue: design direction

This is the working design doc. It adopts the direction from Alek's *Expanded Design Guide* (2026-10-12) where it fits
the game we've already built, with one big change: **the heroes are customizable bots, not one fixed character.**
The story bible is [STORY.md](STORY.md); what's built is in [PROGRESS.md](PROGRESS.md).

The guide's reference images (Tinykin, I Am Future) are press material for study only. Nothing from them goes into
the project.

## The promise

> Rescuers need rescuing, too. You don't have to do everything yourself to be useful.

A cozy community-rescue adventure. Every repair teaches you something, every rescue leaves the city a little more
connected, and in the end the people you helped help you bring Unit Zero home. Errands, physics puzzles, tools and the
mystery are one game, not four.

Pillars:
1. **Help people, not machines.** A fix matters because of who it's for (Nonna Lucia's kitchen, Rosa's cat, Captain Mo).
2. **Readable physical puzzles.** Portal 2 clarity with friendlier rules: consistent mechanisms, combinations of things
   you already know, no trick solutions.
3. **Funny, never punishing.** Bots fall apart and click back together. Mistakes are recoverable, locally.
4. **Better together, fine alone.** Friends get real jobs; every required puzzle has a solo answer.
5. **Your bot.** You build your rookie, name it, and DISPATCH talks to it by name.

## Customizable bots (built)

Every player is a Bureau rookie built from the same kit Unit Zero was. "PIP" is just the default name and look.

| Option | Choices | Notes |
|---|---|---|
| Name | up to 12 characters | DISPATCH's lines say your bot's name instead of "PIP" |
| Body | Bean, Boxy, Barrel, Pear | Same collider and reach on every body |
| Head | Monitor, Dome, Wide-screen, Retro TV | Every head frames the same LED face |
| Feet | Boots, Rollers, Clogs | Feet still pop off on a hard landing |
| Antenna | Bulb, Sparkle, Propeller (spins), Sprout, Bow | |
| Badge | Bureau, Heart, Bolt, Flower, Plain | |
| Shell / Trim / Joints | 10 / 10 / 5 curated colours | Trim also colours the Tether cable and antenna glow |
| Eyes | 7 LED colours | |
| Smile | Cheery, Calm, Focused, Dreamy, Beaming | Resting expression; moods still play on top |
| Voice | Low, Medium, High, Squeaky | Pitch of the babble voice |

**Helper Drones** share a crew livery: one paint for the whole crew (or each keeps its own colour), a topper (Knob, Bow,
Propeller, Sprout) and LED colour.

**Where:** *Customize bots* on the main menu (the bot on the rug updates live) and in the pause menu (your own bot
updates live). Tabs P1 to P4 hold a saved bot for each player, ready for local co-op; P2 to P4 start as DOT, BIX and LOU.

**Rules that keep it readable** (from the guide's cosmetics advice):
- Cosmetics never change the collider, reach, carry strength or any puzzle behaviour.
- The Tool Arm head keeps its tool's colour on every bot, so you can always tell Grabber from Whisk at a glance.
- Every colour is from a curated palette, so no combination is unreadable against the toon outlines.
- Shapes vary inside the same footprint: a Boxy bot and a Bean bot fit through the same gaps.

**How it's built:** `BotLook` / `DroneLivery` (indices into `BotCatalog`), saved by `BotLookStore` in PlayerPrefs
(`rr.look.p1`..`p4`, `rr.look.drones`), applied by `BotCustomizer` / `DroneCustomizer`. The builders make every
variant once as an inactive child (`ChamberKit.Looks.cs`). Changes raise `BotLookChanged` on the EventBus.

**Next for customization:** unlockable cosmetics as rescue rewards (shell panels, decals, pod interiors, Bureau desk
decorations), an in-world Paint Bay at the Bureau, and gamepad navigation in the workshop.

## Mechanics rules

Things the guide asks for, checked against what we have.

| Rule | Status |
|---|---|
| **Battery is planning, not a tax.** Standing still, reading, talking and thinking shouldn't drain it; show the cost of powered work before you commit. | Done (2026-10-18): `BotBattery` only drains while moving and for powered work. |
| **Low-power reserve.** A flat bot can still walk slowly, pick things up, reattach parts and get to safety. Never "need power to reach the only power". | Done (2026-10-18): at 0 % `BotBattery` goes on reserve (45 % speed, carrying and charger sockets still work; no climbing, tools or powering). The tow comes after 75 s away from a charger. |
| **Lost parts are comedy.** Visible marker, floats near water, returns to a service tray if it falls somewhere unreachable. | Done (2026-10-18): `BotPartPickup` shows a bobbing marker and beam, floats where it hits water, and hops home on its own after 45 s or if it falls out of the world. |
| **Puzzle-local resets**, not whole-mission restarts; ask the group before resetting someone else's work. | Done for the Pump House (`PuzzleReset` pads, 2026-10-18); roll out to campaign rooms next. Co-op "ask first" not built. |
| **Hints in layers:** goal, relevant system, relationship, then action. | Done for the Pump House (`LayeredHints`, H / D-pad down, 2026-10-18); write layers for campaign rooms next. |
| **Accessibility:** sound puzzles have visual twins (the Hum already shows as a meter and camera shake), tool targets differ by shape not just colour, assisted swinging, reduced camera motion, remapping, readable text. | Partly. |
| **Magnet rule:** ferrous things are attracted; repulsion only on clearly marked pads. | Matches `MagnetBlock`. |
| **Water is states, not simulation:** a few clear fill levels and valve states. | Matches `FoamFlood`. |
| **No grading the main campaign on speed.** Optional efficiency challenges after a rescue. | Order patience timers in the kitchen are the exception; keep them gentle. |

## Co-op

- Helper Drones and friends are partners, not button-holders: real tool access, clear roles, able to start useful
  actions. Friend-controlled drones hover low and follow the ground unless a puzzle allows flight.
- Prototype 2-player first (Netcode for GameObjects), then 4. Test shared-object authority, latency and rope recovery
  early; a Tether that behaves locally won't automatically behave online.
- When a player drops, anything they held locks or settles safely. Late joiners get the current puzzle state. Shared
  collectibles count once.
- Each player brings their own customized bot (the P1 to P4 slots are the local half of this).

## Tools: several uses each

| Tool | Core idea | Uses we have | Uses to add |
|---|---|---|---|
| Tether | connect | power, pull, swing | anchor-and-brace, cable geometry around obstacles, winching |
| Whisk | controlled rotation | mixing bowls | adapters that turn pumps, winches, brushes (introduce before a critical use) |
| Screwdriver | fasten / release / adjust | screw sockets that power things | ordered assembly (support first, then unscrew), alignment |
| Magnet | attract ferrous things | float crates | fetch through grates, sort mixed batches, work through glass |

## Puzzle families

The guide lists 18. We already have power routing, counterweights (pressure plates), conveyors, rotary (whisk),
fastening, magnets, liquid levels (foam), sound pattern (seismograph), recipes and animal trust (Mittens). Next to add,
in order: **fault diagnosis** (is it power, a blockage or a missing part?), **light and mirrors** (the lighthouse),
**sequencing with latches and stored power** (every solo answer to a 2-player puzzle), and finally **safe system
handover** (prove the replacement works before disconnecting the old one), which is the finale's whole idea.

## Places

The core campaign stays coastal so we reuse water, rigs, foliage and machinery. The guide proposes 12 side-call
locations and 4 later destinations; treat them as a menu, not a launch list. Strongest fits for Brightwater Reach side
calls: **Sunspoke Tram Depot**, **Coral Clinic** (wildlife pool), **Seaglass Sorting Yard** (magnet sorting),
**Bellbuoy Music Pier** (the Hum as music), **Driftwood Theater** (pulleys and counterweights). Later pods: Snowbell
Polar Pier, Dustflower Solar Farm, Orchard Ring Greenhouse, Cloudstep Lift Village, alongside the existing Moon Mine and
Jungle Lab ideas.

Pod travel stays a short skippable arrival with a good landing, never a flight sim.

Between rescues: photograph recovering wildlife, deliver repaired radios, plant seedlings, fill a service notebook,
collect before-and-after postcards of each place.

## Visual direction

**Sunlit handcrafted stylized 3D** (the guide's option A), which is where our toon shader already sits: rounded
machinery, painted metal, rubber joints, broad colours, warm light, restrained wear. Low-poly painterly (C) for
production economy; miniature diorama (D) only for the Bureau mission map and close-up repair views. One look across
every chapter.

- Lighting: readable sun and sky, baked static light, probes for moving bots, few extra dynamic lights.
- Water: looks separate from puzzle behaviour (animated surface, foam at structures, buoyancy only where it matters).
- Fog: distant landmarks fade, nearby interactive things stay crisp.
- Post: gentle bloom, AO and grading. No heavy depth of field, motion blur or chromatic aberration in play.
- Zero looks tired and sympathetic, never like a boss. Same sockets and face language as the rookies, older proportions,
  lots of patch repairs.

Rendering tests to do: park arrival, kitchen clutter, lighthouse in fog, Old Rig with Zero, the Bureau after several
missions. Render the same park view in two styles before committing to more art.

## Interface

**Live Wire.** The Tether Cable is the interface: things are off until current reaches them, and powering on is how
something says it's ready. One system across three places: the game (`Scripts/UI/UiTheme.cs`, `MenuFocus.cs`), the
play page around the browser build (`Assets/WebGLTemplates/RobotRescue`) and the website (`site/`). Same tokens in C#
and CSS; change one, change the others.

- **Colour has jobs.** Tether cyan is live current: focus, keys, things that are on (`#2FE0FF` dark, `#008AB0` light,
  `#00718F` when it's small text on light). Bureau orange `#FF8C26` is PIP and the one main action on a screen
  (Continue, Resume, Play). Unpowered things are slate (`#24335F` dark, `#CFC8B8` light). Green, amber and red only
  mean charged, low and lost. Backgrounds: sea-navy `#0A1430` at night, workshop white `#F6F3EC` by day.
- **Light and dark.** The website follows the system setting, with a toggle saved as `rr-theme` (shared with the play
  page). The game has **Light mode** on the Controls page (PlayerPrefs `rr.ui.light`); screens that cache styles rebuild
  when `UiTheme.Version` changes. The game frame and end credits stay dark in both.
- **Type.** Unbounded Bold for headings 24 and up, Figtree for the rest (Bold for buttons and labels), loaded from
  `Resources/Fonts` and `site/fonts`. Game scale on a 1080-pixel canvas: 76 / 44 / 24 / 18 / 16 / 14, never below 80%.
  Sentence case everywhere.
- **Contrast** (checked): body text about 15:1 in both modes, secondary text 5.8:1 or better, small cyan and orange text
  4.5:1 or better, the light-mode title orange (`#CC6210`) above 3:1 as large text, and focus rings and selected states
  above 3:1.
- **Spacing.** A 4-pixel base used in steps of 8, 12, 16, 24, 32, 48, 64 and 96 on the web; 8 / 16 / 24 in the game.
- **Cables.** On the website one main line runs down the left edge and each section is a socket on it; scrolling
  powers it. In the game, cards (pause, win, lose, flat battery) and the Calls and Controls pages hang on a short lead
  from the top of the screen and power on when they open (`UiTheme.PowerOn`): a 0.15-second spark, then a border glow
  that settles to a steady live edge. The battery shows current running in along a lead while it charges.
- **Motion is physics, and only when something changes.** Verlet rope for hanging cables, damped springs for doors,
  landings and the game's focus plug (stiffness 600, damping 40), a real pendulum for the swing. Arrivals use a strong
  ease-out; frequent things (menu buttons) don't animate. Every web animation loop sleeps when nothing moves or it's off
  screen. Warnings breathe about once a second instead of flashing.
- **Reduced motion.** `prefers-reduced-motion` on the web and **Calm motion** in the game: cables hang still, power just
  switches on, the playground jumps to each result, no sparks or tints. Every state change still shows.
- **Keyboard and gamepad.** Main and pause menus: arrows / D-pad (directional pad) move the plug, Enter / A presses,
  Esc / B goes back. Website: everything is reachable with Tab, swatches are a radio group (arrow keys), and the
  playground's 1 / 2 / 3 / R keys only work while it has focus, so single-key shortcuts never fire elsewhere. The game's
  call list and workshop still need a mouse; that's the next interface job.
- **States are written, not decorated.** Loading says what's happening ("Charging your bot 42%"), empty says why
  ("Not filmed yet"), errors say what to do ("Check your connection, then try again") and keep whatever did load.
  On phones the play page asks before a 50 MB download, because the game needs a keyboard or gamepad.
- **Don't:** stock gradients, glassy cards in identical grids, filler icons, placeholder copy, decorative loops. README
  art loops only where a character is alive (PIP blinking, the pod flying).

## What to build next

0. Done 2026-10-18: the atlas's three-room rescue slice, **The Pump House** (side call). Plan, test report and
   manifest in [slices/pump-house](slices/pump-house/SLICE_PLAN.md). New rules: portable power cells and counterweights.

1. The guide's "one complete park rescue" test, using Chapter 3: Bureau briefing, pod arrival, a readable fault,
   Tether, one more tool, a recoverable mistake, an optional memory bolt, and a clear before-and-after. Play it solo and
   with two players.
2. Done 2026-10-18: the battery reserve and lost-part rules above.
3. The lighthouse as the second major test (multi-tool reasoning, fog, the Hum becoming an SOS).
4. Customization rewards from rescues.
