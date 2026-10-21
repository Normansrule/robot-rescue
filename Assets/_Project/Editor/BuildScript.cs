using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// One-click player builds into Builds/RobotRescue-&lt;Platform&gt;/ at the project root.
    /// Menu: Robot Rescue > Build Game > ... ; headless:
    /// <c>Unity -batchmode -quit -projectPath . -executeMethod RobotRescue.EditorTools.BuildScript.BuildAll</c>
    /// Platforms whose Unity module isn't installed are skipped with a warning (add them in Unity Hub > Installs >
    /// Add modules). The version comes from the RR_VERSION environment variable, else Player Settings.
    /// </summary>
    public static class BuildScript
    {
        public enum Platform { Windows, Linux, Mac, WebGL }

        public static string BuildRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds"));

        [MenuItem("Robot Rescue/Build Game/Windows", priority = 40)] public static void BuildWindows() => Run(Platform.Windows);
        [MenuItem("Robot Rescue/Build Game/Linux", priority = 41)] public static void BuildLinux() => Run(Platform.Linux);
        [MenuItem("Robot Rescue/Build Game/Mac", priority = 42)] public static void BuildMac() => Run(Platform.Mac);
        [MenuItem("Robot Rescue/Build Game/Browser (WebGL)", priority = 43)] public static void BuildWebGL() => Run(Platform.WebGL);
        [MenuItem("Robot Rescue/Build Game/All Installed Platforms", priority = 60)]
        public static void BuildAll() => Run(Platform.Windows, Platform.Linux, Platform.Mac, Platform.WebGL);

        /// <summary>For -executeMethod with "-rrPlatforms Windows,WebGL" on the command line.</summary>
        public static void BuildFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-rrPlatforms");
            if (i < 0 || i + 1 >= args.Length) { BuildAll(); return; }
            var list = args[i + 1].Split(',').Select(s => (Platform)Enum.Parse(typeof(Platform), s.Trim(), true)).ToArray();
            Run(list);
        }

        static (BuildTarget target, BuildTargetGroup group, string exe) Info(Platform p)
        {
            switch (p)
            {
                case Platform.Windows: return (BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, "RobotRescue.exe");
                case Platform.Linux: return (BuildTarget.StandaloneLinux64, BuildTargetGroup.Standalone, "RobotRescue.x86_64");
                case Platform.Mac: return (BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone, "RobotRescue.app");
                default: return (BuildTarget.WebGL, BuildTargetGroup.WebGL, "");
            }
        }

        static void Run(params Platform[] platforms)
        {
            ProjectBuilder.ApplyBuildSettings();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) { Fail("No scenes in Build Settings. Run Robot Rescue > Build Everything first."); return; }

            PlayerSettings.companyName = "Robot Rescue Bureau";
            PlayerSettings.productName = "Robot Rescue";
            string version = Environment.GetEnvironmentVariable("RR_VERSION");
            if (!string.IsNullOrEmpty(version)) PlayerSettings.bundleVersion = version.TrimStart('v');
            // Uncompressed WebGL runs on GitHub Pages and any plain static server without special headers.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            // Our own page (Assets/WebGLTemplates/RobotRescue): the game's colours, a charging-battery loader, controls.
            PlayerSettings.WebGL.template = "PROJECT:RobotRescue";

            var log = new List<string> { $"Robot Rescue {PlayerSettings.bundleVersion}  ·  {DateTime.Now:yyyy-MM-dd HH:mm}" };
            bool failed = false;
            foreach (var p in platforms)
            {
                var (target, group, exe) = Info(p);
                if (!BuildPipeline.IsBuildTargetSupported(group, target))
                {
                    string msg = $"{p}: SKIPPED (Unity's {p} Build Support module isn't installed; add it in Unity Hub)";
                    Debug.LogWarning("[Build] " + msg);
                    log.Add(msg);
                    continue;
                }
                string dir = Path.Combine(BuildRoot, "RobotRescue-" + p);
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = exe.Length > 0 ? Path.Combine(dir, exe) : dir,
                    target = target,
                    targetGroup = group,
                    options = BuildOptions.None,
                });
                var s = report.summary;
                string line = $"{p}: {s.result}  ({s.totalSize / (1024f * 1024f):0.0} MB, {s.totalTime.TotalSeconds:0}s)  ->  {dir}";
                log.Add(line);
                if (s.result == BuildResult.Succeeded) Debug.Log("[Build] " + line);
                else { Debug.LogError("[Build] " + line); failed = true; }
            }
            Directory.CreateDirectory(BuildRoot);
            File.WriteAllLines(Path.Combine(BuildRoot, "build-report.txt"), log);
            if (failed) Fail("At least one build failed; see the log above.");
        }

        static void Fail(string message)
        {
            Debug.LogError("[Build] " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
