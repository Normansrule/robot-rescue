using System.Collections.Generic;
using RobotRescue.Bots;
using RobotRescue.Campaign;
using RobotRescue.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static RobotRescue.EditorTools.ChamberKit;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// Menu: Robot Rescue > Build Everything. Rebuilds every scene, the main menu and the campaign asset, and puts
    /// Build Settings in play order. Also callable headless:
    /// <c>Unity -batchmode -quit -projectPath . -executeMethod RobotRescue.EditorTools.ProjectBuilder.BuildEverything</c>
    /// </summary>
    public static class ProjectBuilder
    {
        public const string MenuScene = Root + "/Scenes/MainMenu.unity";
        const string CampaignPath = Root + "/Resources/Campaign.asset";

        public struct Level
        {
            public string path, title;
            public bool big;
            public Level(string path, string title, bool big = false) { this.path = path; this.title = title; this.big = big; }
            public string Scene => System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public static readonly (string title, string location, Level[] levels)[] Chapters =
        {
            ("Bureau HQ", "Robot Rescue Bureau Station, in orbit", new[]
            {
                new Level(Root + "/Scenes/TestChambers/Test01_StackEmUp.unity", "Training 1: Stack 'Em Up"),
                new Level(Root + "/Scenes/TestChambers/Test02_ConveyorCaper.unity", "Training 2: Conveyor Caper"),
                new Level(Root + "/Scenes/TestChambers/Test03_SwingShift.unity", "Training 3: Swing Shift"),
                new Level(Chapter1Builder.ScenePath, "Coffee Crisis", true),
            }),
            ("Nonna Lucia's Trattoria", "Brightwater Reach, galley platform", new[]
            {
                new Level(Root + "/Scenes/Ch2_Call01_PastaEmergency.unity", "Call 2-1: Pasta Emergency"),
                new Level(Chapter2Builder.WhiskScene, "Call 2-2: Whisk Business"),
                new Level(Chapter2Builder.BossScene, "Pasta Panic", true),
            }),
            ("Brightwater Park", "Brightwater Reach, garden platform", new[]
            {
                new Level(Chapter3Builder.LooseScene, "Call 3-1: Loose Screws"),
                new Level(Chapter3Builder.OakScene, "Up a Tree", true),
            }),
            ("School Science Fair", "Brightwater Reach, school platform", new[]
            {
                new Level(Chapter4Builder.HumScene, "Call 4-1: Catch the Hum"),
                new Level(Chapter4Builder.VolcanoScene, "Slightly Too Big", true),
            }),
            ("Docks & Lighthouse", "Brightwater Reach, harbour", new[]
            {
                new Level(Chapter5Builder.DocksScene, "Call 5-1: Heavy Lifting"),
                new Level(Chapter5Builder.LighthouseScene, "Lights Out", true),
            }),
            ("Funland Pier", "Brightwater Reach, pier", new[]
            {
                new Level(Chapter6Builder.CarouselScene, "Call 6-1: Carousel Chaos"),
                new Level(Chapter6Builder.WheelScene, "Stuck at the Top", true),
            }),
            ("The Old Rig", "Brightwater Reach, the half-sunk rig", new[]
            {
                new Level(Chapter7Builder.DescentScene, "Call 7-1: Going Down"),
                new Level(Chapter7Builder.ZeroScene, "Unit Zero", true),
            }),
            ("Founders' Festival", "Brightwater Reach, summit platform", new[]
            {
                new Level(Chapter8Builder.StageScene, "Call 8-1: Stage Fright"),
                new Level(Chapter8Builder.SongScene, "The Founders' Song", true),
            }),
        };

        /// <summary>Stand-alone calls outside the chapter order: always open, never unlock campaign levels, not in the world.</summary>
        public static readonly Level[] SideCalls =
        {
            new Level(PumpHouseBuilder.ScenePath, "Side Call: The Pump House"),
        };

        [MenuItem("Robot Rescue/Build Everything", priority = 0)]
        public static void BuildEverything()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            TestChamberBuilder.BuildAll();
            Chapter1Builder.Build();
            CookingChamberBuilder.Build();
            Chapter2Builder.BuildWhiskBusiness();
            Chapter2Builder.BuildPastaPanic();
            Chapter3Builder.BuildLooseScrews();
            Chapter3Builder.BuildUpATree();
            Chapter4Builder.BuildCatchTheHum();
            Chapter4Builder.BuildSlightlyTooBig();
            Chapter5Builder.BuildHeavyLifting();
            Chapter5Builder.BuildLightsOut();
            Chapter6Builder.BuildCarouselChaos();
            Chapter6Builder.BuildStuckAtTheTop();
            Chapter7Builder.BuildGoingDown();
            Chapter7Builder.BuildUnitZero();
            Chapter8Builder.BuildStageFright();
            Chapter8Builder.BuildFoundersSong();
            PumpHouseBuilder.Build();
            BuildMenuScene();
            BuildCampaign();
            AssetDatabase.SaveAssets();
            WorldBuilder.Build();
            ApplyBuildSettings();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(System.IO.File.Exists(WorldBuilder.ScenePath) ? WorldBuilder.ScenePath : MenuScene);
            Debug.Log("[Robot Rescue] Built everything. Open World and press Play.");
        }

        [MenuItem("Robot Rescue/Main Menu Scene", priority = 1)]
        public static void BuildMenuOnly()
        {
            BuildMenuScene();
            BuildCampaign();
            ApplyBuildSettings();
        }

        static void BuildCampaign()
        {
            EnsureFolder(Root + "/Resources");
            var c = LoadOrCreate<CampaignDefinition>(CampaignPath);
            c.mainMenuScene = System.IO.Path.GetFileNameWithoutExtension(MenuScene);
            var chapters = new List<ChapterEntry>();
            foreach (var (title, location, levels) in Chapters)
            {
                var entries = new List<LevelEntry>();
                foreach (var l in levels) entries.Add(new LevelEntry { scene = l.Scene, title = l.title, isBigRescue = l.big });
                chapters.Add(new ChapterEntry { title = title, location = location, levels = entries.ToArray() });
            }
            c.chapters = chapters.ToArray();
            var side = new List<LevelEntry>();
            foreach (var l in SideCalls) side.Add(new LevelEntry { scene = l.Scene, title = l.title });
            c.sideCalls = side.ToArray();
            EditorUtility.SetDirty(c);
        }

        /// <summary>The open world first (the game boots into it), then the old menu and every level on its own.</summary>
        public static void ApplyBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var world in WorldBuilder.ScenePaths)
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(world)) scenes.Add(new EditorBuildSettingsScene(world, true));
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScene)) scenes.Add(new EditorBuildSettingsScene(MenuScene, true));
            foreach (var chapter in Chapters)
                foreach (var l in chapter.levels)
                    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(l.path)) scenes.Add(new EditorBuildSettingsScene(l.path, true));
            foreach (var l in SideCalls)
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(l.path)) scenes.Add(new EditorBuildSettingsScene(l.path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>A sunny little diorama: PIP waving among the props, with a slowly orbiting camera.</summary>
        static void BuildMenuScene()
        {
            if (!BeginScene(out var scene)) return;
            var root = new GameObject("Diorama").transform;
            Box("Ground", root, new Vector3(0, -0.25f, 0), new Vector3(14f, 0.5f, 14f), Mat("Floor_Cream", new Color(0.98f, 0.93f, 0.82f)));
            Cylinder("Rug", root, new Vector3(0, 0.01f, 0), 3.2f, 0.02f, Mat("Rug_Coral", new Color(1f, 0.6f, 0.55f)));

            Drone("Drone_A", root, new Vector3(-2.2f, 0.5f, 1.2f), new Color(0.55f, 0.8f, 1f));
            Drone("Drone_B", root, new Vector3(-2.2f, 1.5f, 1.2f), new Color(1f, 0.7f, 0.4f));
            Drone("Drone_C", root, new Vector3(2.4f, 0.5f, 0.6f), new Color(0.75f, 0.55f, 1f));
            Hook("Hook", root, new Vector3(1.2f, 4.2f, 2.5f));
            var tools = ToolKit();
            ToolPedestal("WhiskStand", root, new Vector3(2.6f, 0f, -1.8f), tools.whisk, null, null);
            Box("Crate", root, new Vector3(-2.6f, 0.5f, -2f), Vector3.one, Mat("Crate_Orange", new Color(1f, 0.62f, 0.25f)));

            // PIP, as a statue: build the real bot, then strip everything that would make it playable.
            var holder = new GameObject("tmpCam").AddComponent<Camera>().gameObject.AddComponent<BotCamera>();
            var bot = BuildBot(holder, new Vector3(0f, 0f, 0f), 200f);
            var pip = bot.gameObject;
            // Keep the face and voice (they work on their own); drop everything that needs a controller.
            Object.DestroyImmediate(pip.GetComponent<BotPersonality>());
            Object.DestroyImmediate(pip.GetComponent<BotAnimator>());
            Object.DestroyImmediate(pip.GetComponent<BotParts>());
            Object.DestroyImmediate(pip.GetComponent<BotBattery>());
            Object.DestroyImmediate(bot.GetComponent<ToolArm>());
            Object.DestroyImmediate(bot.GetComponent<Grabber>());
            Object.DestroyImmediate(bot.GetComponent<TetherCable>());
            Object.DestroyImmediate(bot);
            Object.DestroyImmediate(pip.GetComponent<BotInput>());
            Object.DestroyImmediate(holder.gameObject);
            var charger = GameObject.Find("HomeCharger");
            if (charger)
            {
                charger.transform.SetParent(root, true);
                // In the menu the charger is only PIP's stand; its repair-sign bulb read as a second antenna.
                foreach (var t in charger.GetComponentsInChildren<Transform>(true))
                    if (t.name == "SignPost" || t.name == "SignBulb") t.gameObject.SetActive(false);
            }
            pip.GetComponent<Rigidbody>().isKinematic = true;
            pip.AddComponent<MenuIdle>();

            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            var c = cam.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.Skybox;
            c.fieldOfView = 50f;
            EnablePost(c);
            cam.AddComponent<AudioListener>();
            var orbit = cam.AddComponent<MenuCamera>();
            orbit.pivot = new Vector3(0f, 0f, 0f);
            orbit.radius = 8f;
            orbit.height = 3.2f;
            orbit.lookHeight = 1.6f;
            orbit.Place(20f);

            new GameObject("MainMenu").AddComponent<MainMenu>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MenuScene);
            AssetDatabase.SaveAssets();
        }
    }
}
