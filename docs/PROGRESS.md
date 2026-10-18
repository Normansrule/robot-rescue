# Progress log


## Comfort rules: reserve battery and lost parts that come home (2026-10-18)

- **Reserve battery**: at 0 % the bot no longer stops dead. It walks at 45 % speed, can still carry things, reattach
  parts and plug into a green charger post, but can't climb, use a tool or power a socket. DISPATCH warns it's on
  reserve; the HUD shows RESERVE and the tow countdown. 75 s away from a charger and the tow truck comes as before.
- **Lost parts**: a bobbing marker and thin light beam (in the part's own colour) show where it landed. Parts that hit
  water float for a moment, and any part you haven't collected after 45 s (or that falls out of the world) hops home
  and clicks back on by itself.
- The Pump House self-test gained 3 checks for these (24 in total); the last PC run (21/21) predates them.
- README rebuilt around an animated progress board, campaign map, build log and a level-by-level progress tracker,
  using the Play Mode captures from the dev PC.
- Compiles in the cloud check; not yet run in Unity.


## Side call: The Pump House, a three-room rescue slice (2026-10-18)

From Alek's inspiration pack (Portal 2 for rule clarity, Tinykin / I Am Future stills for scale and warm rooms).
Three rooms: charge a **power cell** from your Tether and carry the power past the cable's reach; hold a gate open with a
**counterweight pan**; lower a drawbridge with either a full cell in the winch or enough weight (the cell counts), then
carry Pebble home and watch the pumps start. New: `PowerCell`, `CellCradle`, `CounterweightLift`, `WeighZone`,
`PuzzleReset` (red reset pads), `LayeredHints` (H / D-pad down), side calls on the menu, latched doors, no idle battery
drain. Scripted self-test: Robot Rescue > Side Calls > Run Pump House Self-Test (passed 21/21 on the dev PC, 2026-10-18, after Build Everything came out clean with 20 scenes). Docs in `docs/slices/pump-house/`.
- Not yet verified in Unity: needs a PC run (build the scene, run the self-test, play it).

## Milestone 7 (part 4): Chapter 8, Founders' Festival, the finale (2026-10-14)

- **Call 8-1: Stage Fright**: two drones on the speaker plates, the curtain motor screw, and a flagpole climb to free
  the banner; the stage lights come up when all three are done. Task step zones can now power objects
  (`TaskStepTrigger.poweredObjects`).
- **Big Rescue: The Founders' Song**: power three pylons (Magnet the battery, carry a drone, climb the bell tower and
  plug in), then hold the link (new `AnchorLink` boss meter). Every beat of the song boosts it; it fades if a pylon
  drops. Fireworks when the Reach is anchored. Unit Zero watches from the crowd.
- The last two Hum story beats; the festival landmark is replaced by real platforms, so the whole Reach is connected.
- **The ending**: after the finale the credits roll (new `EndCredits`), then back to the title.
- Not yet verified in Unity.

## Milestone 7 (part 3): Chapter 7, The Old Rig (2026-10-14)

- **Call 7-1: Going Down**: swing across a flooded hall on crane hooks, open Outpost Zero's door (two hinge screws
  and a drone on the emergency power plate); a gangway drops for the way back.
- **Big Rescue: Unit Zero**: Zero's battery drains, faster with every hum (new `ZeroRescue` boss meter, which can
  fail). Fix his charging line up the pipe tower, plug in to charge him, then carry him (new `ZeroBot`, only movable
  once charged) across the catwalk to the rescue lift.
- Two more Hum story beats; the Old Rig landmark is replaced by real platforms.
- The main menu hides the charger's sign bulb, which looked like a second antenna on PIP.
- Not yet verified in Unity.

## Milestone 7 (part 2): Chapter 6, Funland Pier (2026-10-14)

- **Call 6-1: Carousel Chaos**: climb, Tether-pull and Magnet three carousel animals back onto their poles, then
  screw the motor tight; the carousel turns (new `Spinner`, also usable for fans and windmills).
- **Big Rescue: Stuck at the Top**: a Ferris wheel with Grandpa Sal and the twins stuck at the top (new
  `FerrisRescue` boss meter). Tighten the hub up the service tower, Magnet the counterweight onto the brake, plug in at
  the control booth and keep it turning; each hum jams it. The Old Rig is visible from the tower.
- Signs now use a depth-tested shader (`RobotRescue/SignText`), so text no longer shows through walls.
- Two more Hum story beats; the pier landmark is replaced by real platforms.
- Not yet verified in Unity.

## Customizable bots + design direction (2026-10-12)

- **Build your own rescue bot**: *Customize bots* on the main menu and pause menu opens the Bureau workshop. Pick a
  name, body (Bean, Boxy, Barrel, Pear), head (Monitor, Dome, Wide-screen, Retro TV), feet, antenna topper, chest
  badge, shell / trim / joint colours, LED eyes, resting smile and voice. Saved per player (P1 to P4) and shown live.
- **Helper Drone crew livery**: shared paint (or each their own), a topper and LED colour.
- DISPATCH calls your bot by the name you gave it.
- Cosmetics never change colliders or reach, and tool heads keep their tool colours (`BotLook`, `BotLookStore`,
  `BotCustomizer`, `DroneCustomizer`, `BotWorkshop`, builder variants in `ChamberKit.Looks.cs`).
- New [DESIGN.md](DESIGN.md) and an updated [STORY.md](STORY.md) adopting the expanded design guide: the "rescuers need
  rescuing" theme, a friendly Bureau (a failed handover, not a cover-up), the Hum's stages, memory bolts as keepsakes,
  and a handover finale instead of a boss fight.
- Needs a rebuild (`rr levels` / Build Everything) so every scene's bots get the variants. Not yet verified in Unity.

## Milestone 7 (part 1): Chapter 5, Docks & Lighthouse (2026-10-12)

- **The Magnet** (fourth tool): hold Use next to a heavy metal crate and it floats in front of PIP (`MagnetBlock`).
  The Tool Arm now remembers which tools you've collected between sessions.
- **Call 5-1: Heavy Lifting**: float two crates onto the ballast plates of a listing barge (new `Hinge`), and screw its
  mast light back on (`PowerToggle`).
- **Big Rescue: Lights Out**: fuel the generator, climb the lighthouse, fix the lens and plug in to be the beacon
  (`BeaconRescue` boss meter). Captain Mo's boat only comes in while the light is on; each hum flickers it in morse.
- Two more Hum story beats; the lighthouse landmark is replaced by real platforms on the bridge chain.
- Not yet verified in Unity.

## Milestone 4b (part 2): Chapter 4, School Science Fair (2026-10-12)

- **Call 4-1: Catch the Hum**: fix the stand of Priya's seismograph, power it with a drone on the stage plate, then
  wait for the hum; it scribbles the S.O.S. onto its paper drum (new `Seismograph`, listening to `HumTremor.Pulsed`).
- **Big Rescue: Slightly Too Big**: Theo's volcano fills the gym with foam (`FoamFlood` boss meter). Three valve screws
  (one up a scaffold, screwed while climbing) and two drone-weighted drain plates slow it; all five drain it. Every hum
  makes it surge; reaching the top is a restart.
- Reusable round people (`ChamberKit.Person`): Priya, Theo and Ms. Okafor. Two more Hum story beats.
- The school landmark is replaced by the real thing on the Brightwater bridge chain.
- Not yet verified in Unity.

## Milestone 4b (part 1): Chapter 3, Brightwater Park (2026-10-11)

**New gameplay**
- **Screwdriver** for the Tool Arm (unlocked on a stand in Call 3-1). Loose screws glow; hold Use until they sink in.
  Screws power things like plates and sockets do, so they can open bridges, start pumps or stand ladders up.
- **Hinges**: drawbridges and ladders that swing into place (and stay) once powered.
- **Mittens the cat**: hops to the next branch up whenever PIP gets close, until she's stuck on the top one. Pick her
  up (or, frowned upon, throw her) and set her down in her basket.
- **The Hum, felt**: in the oak garden a meter shows the next hum building. When it hits, the camera rattles, the tree
  shakes, loose things hop, Mittens yowls, and a climbing bot loses grip.
- **Tools are remembered** between sessions, so a call that needs the Whisk or Screwdriver works even if you skip
  straight to it.

**New levels**: Call 3-1: Loose Screws and Big Rescue: Up a Tree, both on the Brightwater Reach bridge chain after
Pasta Panic (the park landmark is replaced by the real thing). New DISPATCH lines, two more Hum story beats.

- Not yet verified in Unity: needs `rr levels` (Build Everything) on the dev PC.

## Milestone 4: cute 3D, personalities, open world, battery and parts (2026-10-11)

- Shaders: `RobotRescue/Toon` (ramp shading, rim, outline pass, SSAO-aware), `RobotRescue/BotFace` (SDF LED face with
  moods), `RobotRescue/Sky`. Builders fall back to URP Lit if a shader fails to compile.
- Rounded-box meshes for every prop (MeshKit), post-processing profile (bloom, tonemapping, vignette), fog.
- PIP rebuilt as a jointed toy: wobbly head, antenna, arms, feet (BotAnimator), face moods (BotFace), babble voice
  (Babble), reactions (BotPersonality). Drones get names, temperaments, faces and voices (DronePersonality).
- Battery (BotBattery), service bays (ChargePad), charger posts (ChargerSocket), drone charge sharing, tow when flat.
- Breakable arms, feet and antenna (BotParts, BotPartPickup) with gameplay effects.
- Climbing with stamina (ClimbSurface), wave emote, golden bolts (Collectible).
- Open world (WorldBuilder > World.unity): all seven calls stitched onto one island hill with gates, porches,
  walkways, terrain, landmarks for chapters 3-8, title flyover. Areas switch their systems on as PIP walks in.
- Setting (docs/STORY.md): the Bureau is a space station hub; mission pods (MissionPod) fly to destinations.
  WorldBuilder now builds two worlds: World_Station (Chapter 1, decks over a planet) and World_Brightwater
  (Chapter 2, an ocean rig city with chapters 3-8 visible on unconnected platforms).
- Mystery: the Hum (DISPATCH lines after each call) and 12 memory-bolt logs from Unit Zero.
- Not yet verified in Unity: needs `rr levels` (Build Everything) on the dev PC.

## Milestone 3: Chapters 1–2 vertical slice (2026-10-11)

**New gameplay**
- **Tool Arm** on PIP: hold F / B to use the current tool, Tab / Y to swap. Tool heads are data (`ToolDefinition`):
  Grabber, Whisk, Screwdriver, Magnet. Walk into a `ToolPickup` pedestal to unlock one.
- **Whisk + mixing bowls**: drop ingredients in a bowl, set it down, whisk until the progress bar fills.
- **Recipes**: Spaghetti (boil pasta), Minestrone (boil tomato + carrot), Tomato Sauce (whisk tomato), Pesto (whisk basil).
  Pots now accept several recipes and burn if left boiling too long.
- **Orders**: an `OrderBoard` spawns tickets with patience timers; serve the right dish at the hatch. Expired tickets are
  strikes; "Hero moments" for saves in the last seconds.
- **Kitchen chaos**: fuses blow (every cable pops out), pots rattle off the stove, the pantry conveyor reverses.
- **Ingredient dispensers**, a trash bin for burnt food, and Mr. Grumbleton the critic, whose face reddens as tickets expire.
- **Coffee machine boss**: pressure rises over time; three vents (socket, one-drone plate, two-drone plate) bleed it off.
- **Campaign**: main menu (Continue, Choose a call, Controls), unlocks saved between sessions, Enter / A goes to the next
  call, pause menu (Esc / Start).
- **HUD**: tool bar, order tickets with patience bars, boss meters, world-space progress bars, chaos alerts, title cards,
  win / lose screens.

**New levels** (all generated by builders): Big Rescue: Coffee Crisis, Call 2-2: Whisk Business, Big Rescue: Pasta Panic,
main menu diorama.

**Tooling**
- `Robot Rescue > Build Everything`: every scene + campaign + Build Settings in one click.
- `Robot Rescue > Capture README Media`: renders a screenshot and an orbiting GIF per level (own GIF encoder, no plugins).
- `Robot Rescue > Build Game`: Windows / Linux / Mac / WebGL into `Builds/`.
- `tools/rr`: Ubuntu / WSL helper (publish to GitHub, releases, Pages, progress, open Unity, play builds).
- Optional GitHub Actions workflow for cloud builds.

**Fixes**: `EventBus` reset on play moved out of the generic class (Unity can't run `RuntimeInitializeOnLoadMethod` in
generic types); world signs no longer read mirrored.

## Milestone 2: puzzle toolkit (2026-10-11)
Helper Drones, conveyors (PIP rides them), colour-coded power wires, AND/OR power gates, step signals, and three test
chambers: Stack 'Em Up, Conveyor Caper, Swing Shift.

## Milestone 1: core bot (2026-10-10)
PIP's movement with squash and stretch, the Tether Cable (power / pull / swing), push / carry / throw, DISPATCH subtitles,
and the first cooking chamber, Call 2-1: Pasta Emergency.
