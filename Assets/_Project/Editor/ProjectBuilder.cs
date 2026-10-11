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
            ("Bureau HQ", "Robot Rescue Bureau, Brightwater", new[]
            {
                new Level(Root + "/Scenes/TestChambers/Test01_StackEmUp.unity", "Training 1: Stack 'Em Up"),
                new Level(Root + "/Scenes/TestChambers/Test02_ConveyorCaper.unity", "Training 2: Conveyor Caper"),
                new Level(Root + "/Scenes/TestChambers/Test03_SwingShift.unity", "Training 3: Swing Shift"),
                new Level(Chapter1Builder.ScenePath, "Coffee Crisis", true),
            }),
            ("Nonna Lucia's Trattoria", "Brightwater harbour", new[]
            {
                new Level(Root + "/Scenes/Ch2_Call01_PastaEmergency.unity", "Call 2-1: Pasta Emergency"),
                new Level(Chapter2Builder.WhiskScene, "Call 2-2: Whisk Business"),
                new Level(Chapter2Builder.BossScene, "Pasta Panic", true),
            }),
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
            BuildMenuScene();
            BuildCampaign();
            ApplyBuildSettings();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(MenuScene);
            Debug.Log("[Robot Rescue] Built everything. Open MainMenu and press Play.");
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
            EditorUtility.SetDirty(c);
        }

        /// <summary>Main menu first, then every level in play order (only scenes that exist).</summary>
        public static void ApplyBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScene)) scenes.Add(new EditorBuildSettingsScene(MenuScene, true));
            foreach (var chapter in Chapters)
                foreach (var l in chapter.levels)
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
            Object.DestroyImmediate(bot.GetComponent<ToolArm>());
            Object.DestroyImmediate(bot.GetComponent<Grabber>());
            Object.DestroyImmediate(bot.GetComponent<TetherCable>());
            Object.DestroyImmediate(bot);
            Object.DestroyImmediate(pip.GetComponent<BotInput>());
            Object.DestroyImmediate(holder.gameObject);
            pip.GetComponent<Rigidbody>().isKinematic = true;
            pip.AddComponent<MenuIdle>();

            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            var c = cam.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.62f, 0.83f, 1f);
            c.fieldOfView = 50f;
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
