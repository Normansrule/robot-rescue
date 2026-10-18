using RobotRescue.Dev;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// Menu: Robot Rescue > Side Calls > Run Pump House Self-Test. Opens (or builds) the Pump House, drops the
    /// <see cref="PumpHouseSelfTest"/> in and presses Play. The test object is removed again when you stop.
    /// Headless: <c>Unity -batchmode -projectPath . -executeMethod RobotRescue.EditorTools.PumpHouseSelfTestRunner.RunFromCommandLine</c>
    /// (no -quit; it exits with code 0 when every check passes, 1 otherwise).
    /// </summary>
    public static class PumpHouseSelfTestRunner
    {
        const string TestObject = "__PumpHouseSelfTest";
        const string BatchKey = "RobotRescue.PumpHouseSelfTest.Batch";

        [MenuItem("Robot Rescue/Side Calls/Run Pump House Self-Test")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) return;
            if (!System.IO.File.Exists(PumpHouseBuilder.ScenePath)) PumpHouseBuilder.Build();
            else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(PumpHouseBuilder.ScenePath);
            else return;
            new GameObject(TestObject).AddComponent<PumpHouseSelfTest>();
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.EnterPlaymode();
        }

        public static void RunFromCommandLine()
        {
            SessionState.SetBool(BatchKey, true);
            PumpHouseBuilder.Build();
            // This project enters Play Mode without a domain reload, so WatchAfterReload won't run again; wait from here.
            EditorApplication.update -= WaitForResult;
            EditorApplication.update += WaitForResult;
            Run();
        }

        [InitializeOnLoadMethod]
        static void WatchAfterReload()
        {
            // Entering Play Mode reloads scripts; pick the batch run back up on the other side.
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (SessionState.GetBool(BatchKey, false)) EditorApplication.update += WaitForResult;
        }

        static void WaitForResult()
        {
            if (!EditorApplication.isPlaying || PumpHouseSelfTest.Passed == null) return;
            EditorApplication.update -= WaitForResult;
            bool ok = PumpHouseSelfTest.Passed == true;
            SessionState.EraseBool(BatchKey);
            EditorApplication.ExitPlaymode();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            var go = GameObject.Find(TestObject);
            if (go) Object.DestroyImmediate(go);
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path == PumpHouseBuilder.ScenePath) EditorSceneManager.SaveScene(scene);
        }
    }
}
