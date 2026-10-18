using System.Collections;
using System.Collections.Generic;
using System.Text;
using RobotRescue.Bots;
using RobotRescue.Narrative;
using RobotRescue.Puzzles;
using RobotRescue.Tasks;
using RobotRescue.Tasks.Rescue;
using UnityEngine;

namespace RobotRescue.Dev
{
    /// <summary>
    /// Plays The Pump House by script in Play Mode and checks every rule the slice promises: wrong-order mistakes don't
    /// progress, power transfer works, the counterweight holds, resets clear only their room, both answers to the last
    /// room work, the rescue changes the world, and standing still doesn't drain the battery. It moves objects directly
    /// (it can't press keys), so it checks the puzzle logic and physics triggers, not the controls or the camera.
    /// Run it from Robot Rescue > Side Calls > Run Pump House Self-Test. Results go to the Console, the screen and
    /// docs/slices/pump-house/selftest-last.txt.
    /// </summary>
    public class PumpHouseSelfTest : MonoBehaviour
    {
        public static string ReportPath => System.IO.Path.Combine(Application.dataPath, "../docs/slices/pump-house/selftest-last.txt");

        /// <summary>Set when the run ends: true if every check passed.</summary>
        public static bool? Passed { get; private set; }

        readonly List<(bool ok, string what)> results = new List<(bool, string)>();
        bool done;

        IEnumerator Start()
        {
            Passed = null;
            yield return new WaitForSeconds(0.5f);

            var room1 = Find("PumpHouse/Room1_ChargeRoom");
            var room2 = Find("PumpHouse/Room2_GateRoom");
            var room3 = Find("PumpHouse/Room3_PumpHall");
            var runner = FindAnyObjectByType<TaskRunner>();
            var bot = FindAnyObjectByType<BotController>();
            if (!room1 || !room2 || !room3 || !runner || !bot)
            {
                Check(false, "scene has the three rooms, a task runner and a bot (open Side_PumpHouse first)");
                Finish();
                yield break;
            }

            var cellA = Get<PowerCell>(room1, "PowerCell_A");
            var chargeA = Get<CellCradle>(room1, "ChargeCradle/CradleZone");
            var doorCradle = Get<CellCradle>(room1, "DoorCradle/CradleZone");
            var socketA = Get<PowerSocket>(room1, "ChargeSocket/Socket");
            var door1 = Get<Door>(room1, "Door_GateRoom");
            var reset1 = Get<PuzzleReset>(room1, "ResetPad/ResetZone");

            var gate = room2.GetComponent<CounterweightLift>();
            var crateA = Find("PumpHouse/Room2_GateRoom/Crate_R2_Floor");
            var crateB = Find("PumpHouse/Room2_GateRoom/Crate_R2_Shelf");
            var reset2 = Get<PuzzleReset>(room2, "ResetPad/ResetZone");

            var bridge = room3.GetComponent<CounterweightLift>();
            var cellB = Get<PowerCell>(room3, "PowerCell_B");
            var chargeB = Get<CellCradle>(room3, "ChargeCradle/CradleZone");
            var socketB = Get<PowerSocket>(room3, "ChargeSocket/Socket");
            var winch = Get<CellCradle>(room3, "WinchCradle/CradleZone");
            var crateC = Find("PumpHouse/Room3_PumpHall/Crate_R3_A");
            var crateD = Find("PumpHouse/Room3_PumpHall/Crate_R3_B");
            var pebble = Find("PumpHouse/Room3_PumpHall/Pebble");
            var gauge = Get<RescueDropZone>(room3, "TideGauge_Drop");
            var exit = Get<Door>(room3, "ExitDoor");
            var pumps = room3.transform.Find("PumpWheel")?.GetComponent<Spinner>();
            var reset3 = Get<PuzzleReset>(room3, "ResetPad/ResetZone");
            var hints1 = Get<LayeredHints>(room1, "Hints");

            // ---- start state
            Check(!door1.IsOpen && gate.Open01 < 0.01f && bridge.Open01 < 0.01f && runner.task.steps.Length == 5 && !runner.IsComplete,
                "clean start: door shut, gate down, bridge up, 5 steps open");

            // ---- the battery doesn't drain while standing still
            var battery = bot.GetComponent<BotBattery>();
            bot.RespawnPoint = new Vector3(-2f, 0.1f, 9f);   // off the home charger, so charging can't hide a drain
            bot.Respawn(false);
            yield return new WaitForSeconds(0.5f);
            float before = battery.Charge01;
            yield return new WaitForSeconds(2f);
            Check(Mathf.Approximately(before, battery.Charge01), $"idle bot keeps its charge ({before:0.000} -> {battery.Charge01:0.000})");

            // ---- a flat battery means reserve power, not a dead end
            battery.Drain(1f);
            Check(battery.OnReserve && !battery.IsDead && !battery.CanDoPoweredWork && Mathf.Approximately(battery.SpeedScale, battery.reserveSpeed),
                $"flat battery: on reserve, still walking at {battery.SpeedScale:0.00}x, no tools or powering");
            battery.Fill();
            Check(!battery.OnReserve && battery.CanDoPoweredWork, "charged again: off reserve, tools and climbing back");

            // ---- a lost part shows a marker and comes home by itself
            var parts = bot.GetComponent<BotParts>();
            if (parts)
            {
                parts.Break(BotPart.Antenna, Vector3.up * 2f);
                yield return null;
                var lost = FindAnyObjectByType<BotPartPickup>();
                bool marked = lost && lost.Marker;
                if (lost) lost.ReturnHome();
                yield return Until(() => parts.Has(BotPart.Antenna), 3f);
                Check(marked && parts.Has(BotPart.Antenna), "lost antenna: marker shown, service tray sends it home and it clicks back on");
            }
            else Check(false, "the bot has BotParts");

            // ---- hints come in layers, goal first
            string h1 = hints1.RequestHint(), h2 = hints1.RequestHint();
            Check(h1 == "r1_hint_1" && h2 == "r1_hint_2", $"hints arrive goal first, then the system ({h1}, {h2})");

            // ---- room 1: an empty cell in the door cradle does nothing (wrong order)
            Put(cellA.gameObject, Above(doorCradle));
            yield return new WaitForSeconds(1.2f);
            Check(doorCradle.Cell == cellA && !doorCradle.IsOutputOn && !door1.IsOpen, "empty cell in the door cradle: cradle sees it, door stays shut");

            // ---- room 1: charge it at the station (as if the bot were plugged in), then carry it over
            Put(cellA.gameObject, Above(chargeA));
            yield return new WaitForSeconds(0.8f);
            PowerUtil.Send(socketA.poweredObjects, true);
            yield return Until(() => cellA.IsFull, 6f);
            PowerUtil.Send(socketA.poweredObjects, false);
            Check(cellA.IsFull && runner.IsStepDone("cell_charged"), "plugging in fills the cell and completes 'charge a cell'");

            Put(cellA.gameObject, Above(doorCradle));
            yield return Until(() => door1.IsOpen, 3f);
            Check(doorCradle.IsOutputOn && door1.IsOpen && runner.IsStepDone("door1"), "full cell in the door cradle powers the door");

            Put(cellA.gameObject, new Vector3(0f, 0.6f, 9f));
            yield return new WaitForSeconds(1f);
            Check(!doorCradle.IsOutputOn && door1.IsOpen, "taking the cell back out: door stays open (latched, no softlock)");

            reset1.ResetRoom();
            yield return new WaitForSeconds(0.5f);
            Check(Near(cellA.transform.position, new Vector3(-5.6f, 0.55f, 6f), 0.6f) && cellA.Charge01 < 0.01f && door1.IsOpen && runner.IsStepDone("door1"),
                "room 1 reset: cell home and empty, door and finished steps untouched");

            // ---- room 2: one crate moves the gate a little, two hold it open
            var pan2 = room2.transform.Find("Pan").position;
            Put(crateA, pan2 + Vector3.up * 0.6f);
            yield return new WaitForSeconds(3f);
            Check(gate.Open01 > 0.1f && gate.Open01 < 0.4f && !gate.IsOpen, $"one crate: gate moves a bit but not enough ({gate.Open01:0.00})");
            Put(crateB, pan2 + new Vector3(0.5f, 1.5f, 0.5f));
            yield return Until(() => gate.IsOpen, 6f);
            Check(gate.IsOpen && runner.IsStepDone("gate"), "two crates: gate fully open, 'gate' step done");
            yield return new WaitForSeconds(2f);
            Check(gate.IsOpen, "gate stays open with nobody touching anything (held state)");

            // ---- room 2: a crate held by nobody but sitting under the gate stops it closing
            Put(crateB, new Vector3(0f, 0.5f, 27.3f));
            yield return new WaitForSeconds(2f);
            Check(gate.Open01 > 0.9f, $"something under the gate: it won't come down ({gate.Open01:0.00})");

            reset2.ResetRoom();
            yield return Until(() => gate.Open01 < 0.05f, 8f);
            Check(gate.Open01 < 0.05f && runner.IsStepDone("gate") && door1.IsOpen, "room 2 reset: crates home, gate closes, other rooms untouched");

            // ---- room 3, answer 1: weight. Two crates aren't enough; the cell is heavy too.
            var pan3 = room3.transform.Find("Pan").position;
            Put(crateC, pan3 + new Vector3(-0.5f, 0.6f, -0.5f));
            Put(crateD, pan3 + new Vector3(0.5f, 0.6f, 0.5f));
            yield return new WaitForSeconds(4f);
            Check(!bridge.IsOpen && bridge.Open01 > 0.2f && bridge.Open01 < 0.6f, $"two crates: bridge comes partway down ({bridge.Open01:0.00})");
            Put(cellB.gameObject, pan3 + new Vector3(0.5f, 0.8f, -0.5f));
            yield return Until(() => bridge.IsOpen, 8f);
            Check(bridge.IsOpen && runner.IsStepDone("bridge"), "two crates + the (empty) cell: bridge fully down");

            reset3.ResetRoom();
            yield return Until(() => bridge.Open01 < 0.05f, 8f);
            Check(bridge.Open01 < 0.05f && cellB.Charge01 < 0.01f, "room 3 reset: bridge back up, cell home");

            // ---- room 3, answer 2: power. Charge the cell, set it in the winch cradle.
            yield return new WaitForSeconds(0.5f);
            PowerUtil.Send(socketB.poweredObjects, true);
            yield return Until(() => cellB.IsFull, 6f);
            PowerUtil.Send(socketB.poweredObjects, false);
            Put(cellB.gameObject, Above(winch));
            yield return Until(() => bridge.IsOpen, 8f);
            Check(winch.IsOutputOn && bridge.MotorOn && bridge.IsOpen, "full cell in the winch cradle: bridge fully down (second answer)");

            // ---- the rescue changes the world
            Check(!exit.IsOpen && (!pumps || !pumps.Powered), "before the rescue: exit shut, pumps still");
            Put(pebble, gauge.transform.position);
            yield return Until(() => gauge.Done, 3f);
            yield return new WaitForSeconds(0.5f);
            Check(gauge.Done && runner.IsStepDone("pebble") && runner.IsComplete, "Pebble on the tide gauge: last step done, task complete");
            Check(exit.IsOpen && (pumps && pumps.Powered), "after the rescue: exit opens, pumps run");

            reset3.ResetRoom();
            yield return new WaitForSeconds(0.5f);
            Check(gauge.Done && Near(pebble.transform.position, gauge.seat.position, 0.3f), "room 3 reset after the rescue: Pebble stays home");

            Finish();
        }

        // ------------------------------------------------------------------ helpers

        void Check(bool ok, string what)
        {
            results.Add((ok, what));
            if (ok) Debug.Log("[SelfTest] PASS " + what);
            else Debug.LogError("[SelfTest] FAIL " + what);
        }

        void Finish()
        {
            done = true;
            int fails = 0;
            var sb = new StringBuilder();
            sb.AppendLine("The Pump House self-test");
            sb.AppendLine($"Unity {Application.unityVersion} · {SystemInfo.operatingSystem} · {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            foreach (var (ok, what) in results)
            {
                if (!ok) fails++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + what);
            }
            sb.AppendLine(fails == 0 ? $"ALL {results.Count} CHECKS PASSED" : $"{fails} OF {results.Count} CHECKS FAILED");
            Passed = fails == 0 && results.Count > 0;
            Debug.Log("[SelfTest] " + (Passed == true ? "ALL PASSED" : $"{fails} FAILED"));
#if UNITY_EDITOR
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ReportPath));
                System.IO.File.WriteAllText(ReportPath, sb.ToString());
            }
            catch (System.Exception e) { Debug.LogWarning("[SelfTest] couldn't write the report: " + e.Message); }
#endif
        }

        static IEnumerator Until(System.Func<bool> condition, float timeout)
        {
            float end = Time.time + timeout;
            while (!condition() && Time.time < end) yield return null;
        }

        static bool Near(Vector3 a, Vector3 b, float d) => (a - b).sqrMagnitude <= d * d;

        static Vector3 Above(CellCradle c) => c.transform.position + Vector3.up * 0.1f;

        static GameObject Find(string path) => GameObject.Find(path);

        static T Get<T>(GameObject room, string path) where T : Component
        {
            var t = room.transform.Find(path);
            return t ? t.GetComponent<T>() : null;
        }

        /// <summary>Teleports a physics object (dropping it if held), the way a player setting it down would leave it.</summary>
        static void Put(GameObject go, Vector3 pos)
        {
            if (!go) return;
            if (go.TryGetComponent(out Carryable c) && c.Holder) c.Holder.Drop();
            var rb = go.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = pos;
                rb.rotation = Quaternion.identity;
            }
            go.transform.SetPositionAndRotation(pos, Quaternion.identity);
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true };
            float y = 10f;
            GUI.Label(new Rect(10f, y, 900f, 24f), done ? (Passed == true ? "<b><color=#7dff9a>SELF-TEST: ALL PASSED</color></b>" : "<b><color=#ff7a6e>SELF-TEST: FAILURES</color></b>") : "<b>SELF-TEST RUNNING…</b>", style);
            foreach (var (ok, what) in results)
            {
                y += 20f;
                GUI.Label(new Rect(10f, y, 1200f, 22f), (ok ? "<color=#7dff9a>PASS</color>  " : "<color=#ff7a6e>FAIL</color>  ") + what, style);
            }
        }
    }
}
