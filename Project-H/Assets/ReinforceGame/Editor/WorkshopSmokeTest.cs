using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ReinforceGame.Editor
{
    // Invoke with Unity -batchmode -projectPath ... -executeMethod ReinforceGame.Editor.WorkshopSmokeTest.Run
    // No -quit: the runtime probe exits after the assertions and screenshot complete.
    [InitializeOnLoad]
    public static class WorkshopSmokeTest
    {
        static double start;
        static WorkshopSmokeTest()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("ReinforceSmoke", false))
                {
                    new GameObject("Runtime smoke assertions").AddComponent<WorkshopSmokeProbe>();
                    start = EditorApplication.timeSinceStartup;
                    EditorApplication.update += Timeout;
                }
            };
        }
        static void Timeout()
        {
            if (EditorApplication.timeSinceStartup - start < 90) return;
            File.WriteAllText("Assets/ReinforceGame/Validation/smoke-failure.txt", "Runtime smoke test timed out.");
            EditorApplication.Exit(1);
        }
        public static void Run()
        {
            WorkshopGenerator.Generate();
            WorkshopGenerator.OpenBootstrap();
            SessionState.SetBool("ReinforceSmoke", true);
            EditorApplication.isPlaying = true;
        }
    }
}
