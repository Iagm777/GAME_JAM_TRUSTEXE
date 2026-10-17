using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrustEXE.Core;
using TrustEXE.Tutorial;

namespace TrustEXE.Result.Editor
{
    [InitializeOnLoad]
    public static class ResultSetup
    {
        const string ScenePath = "Assets/00_Scenes/Resultado.unity";
        const string SharedPath = "Assets/06_Data/Tutorial/TutorialAssets.asset";
        static ResultSetup() { EditorApplication.update += Poll; }
        static void Poll()
        {
            const string path = "Library/TrustResultRequest.txt";
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(path)) return;
            string cmd;
            try { cmd = File.ReadAllText(path).Trim(); File.Delete(path); } catch (IOException) { return; }
            try
            {
                if (cmd == "setup") Build();
                else if (cmd == "validate") Validate();
                else if (cmd == "open") { if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save current scene first."); EditorSceneManager.OpenScene(ScenePath); }
                else if (cmd == "play") { EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath); EditorApplication.isPlaying = true; }
                else if (cmd == "stop") EditorApplication.isPlaying = false;
                else if (cmd == "capture") ScreenCapture.CaptureScreenshot("Library/ResultPreview.png");
                else if (cmd == "build") { var report = UnityEditor.BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(), "Builds/Result/TRUST_EXE.exe", BuildTarget.StandaloneWindows64, BuildOptions.None); File.WriteAllText("Library/ResultBuild.txt", report.summary.result + " errors=" + report.summary.totalErrors); if (report.summary.totalErrors > 0) throw new Exception("Build errors: " + report.summary.totalErrors); }
                File.WriteAllText("Library/TrustResultResult.txt", "OK: " + cmd);
            }
            catch (Exception e) { File.WriteAllText("Library/TrustResultResult.txt", e.ToString()); Debug.LogException(e); }
        }

        [MenuItem("TRUST.EXE/Resultado/Preparar escena de resultado")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var shared = AssetDatabase.LoadAssetAtPath<TutorialAssets>(SharedPath);
            if (!shared) throw new FileNotFoundException("Tutorial assets are required first.", SharedPath);
            if (!File.Exists(ScenePath))
            {
                var original = SceneManager.GetActiveScene();
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                var root = new GameObject("TRUST.EXE / Resultado");
                root.AddComponent<TrustEXE.Result.ResultScene>().shared = shared;
                EditorSceneManager.SaveScene(scene, ScenePath);
                EditorSceneManager.CloseScene(scene, true);
                if (original.IsValid()) SceneManager.SetActiveScene(original);
            }
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("TRUST.EXE: Resultado listo (Victoria / Derrota).");
        }

        /// <summary>Pure-state validation of the shared-lives and outcome contract.</summary>
        public static void Validate()
        {
            GameSession.SuppressSceneLoad = true;
            try
            {
                GameSession.Lives = 3; GameSession.Level = "";
                Check(!GameSession.LoseLife() && GameSession.Lives == 2, "lose life decrements");
                Check(!GameSession.LoseLife() && GameSession.Lives == 1, "two lives left");
                Check(GameSession.LoseLife() && GameSession.Lives == 0, "third life empties pool");
                Check(GameSession.LoseLife() && GameSession.Lives == 0, "pool stays at zero after empty");
                GameSession.BeginLevel("Tutorial");
                GameSession.Lives = 1;
                GameSession.BeginLevel("Tutorial");
                Check(GameSession.Lives == 1, "same level keeps the pool");
                GameSession.BeginLevel("Acceso");
                Check(GameSession.Lives == GameSession.MaxLives, "new level restores the pool");
                GameSession.Defeat("Prueba de causa");
                Check(GameSession.Defeated && !GameSession.Victorious && GameSession.FailureReason == "Prueba de causa", "defeat records reason");
                var level = GameSession.Level;
                GameSession.RetryLevel();
                Check(GameSession.Lives == GameSession.MaxLives && !GameSession.Defeated && GameSession.Level == "", "retry restores pool and clears flags");
                Check(level == "Acceso", "retry keeps the failed level name");
                var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
                Check(scene != null, "Resultado scene exists");
                File.WriteAllText("Library/TrustResultValidation.txt", "PASS: shared lives per level, defeat reason, retry restore, scene wiring.\n" + DateTime.UtcNow.ToString("O"));
            }
            catch (Exception e) { File.WriteAllText("Library/TrustResultValidation.txt", "FAIL: " + e); Debug.LogException(e); }
            finally { GameSession.Reset(); }
        }
        static void Check(bool value, string label) { if (!value) throw new Exception("Result validation: " + label); }
    }
}