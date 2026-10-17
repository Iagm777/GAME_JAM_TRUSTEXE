using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrustEXE.Menu.Editor
{
    [InitializeOnLoad]
    public static class TrustMenuSetup
    {
        public const string ScenePath = "Assets/00_Scenes/MainMenu.unity";
        const string CatalogPath = "Assets/01_Script/Menu/MenuCatalog.asset";
        static readonly string[] Names = { "Axi", "Bruno", "Byte", "Elian", "Kai", "KBot", "Lyra", "Mara", "Noah", "Nova", "Ren", "Zoro" };
        static TrustMenuSetup()
        {
            EditorApplication.delayCall += () => {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !File.Exists(ScenePath)) Build();
            };
            EditorApplication.update += ProcessValidationRequest;
        }

        [MenuItem("TRUST.EXE/Preparar menu y catalogo")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var catalog = AssetDatabase.LoadAssetAtPath<TrustMenuCatalog>(CatalogPath);
            if (!catalog) { catalog = ScriptableObject.CreateInstance<TrustMenuCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.avatars = Names.Select(name => new TrustMenuCatalog.Avatar {
                id = name,
                idle = Texture(name, "Idle"),
                walk = Enumerable.Range(0,3).Select(i => Texture(name,"Walk_Down_" + i)).ToArray(),
                think = Enumerable.Range(0,2).Select(i => Texture(name,"Think_" + i)).ToArray()
            }).ToArray();
            catalog.eva = Enumerable.Range(0,4).Select(i => RequiredTexture("Assets/03_Sprites/Character/EVA/EVA/EVA_Neutral_"+i+".png")).ToArray();
            catalog.backdrop = RequiredTexture("Assets/03_Sprites/Fondos/Tutorial.jpeg");
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            if (!File.Exists(ScenePath))
            {
                var original = SceneManager.GetActiveScene();
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                var cameraObject = new GameObject("Menu camera", typeof(Camera), typeof(AudioListener));
                var camera = cameraObject.GetComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.025f,.042f,.065f); camera.orthographic = true;
                var root = new GameObject("TRUST.EXE Menu", typeof(TrustMenuController));
                root.GetComponent<TrustMenuController>().catalog = catalog;
                EditorSceneManager.SaveScene(scene, ScenePath);
                EditorSceneManager.CloseScene(scene, true);
                if (original.IsValid()) SceneManager.SetActiveScene(original);
            }
            var otherScenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath && File.Exists(s.path));
            EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene(ScenePath,true)}.Concat(otherScenes).ToArray();
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("TRUST.EXE: MainMenu listo. Play abre el menu. 12 avatares y 4 frames de E.V.A. verificados.");
        }
        static Texture2D Texture(string name, string pose) => RequiredTexture("Assets/03_Sprites/Character/"+name+"/"+name+"_"+pose+".png");
        static Texture2D RequiredTexture(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!texture) throw new FileNotFoundException("Falta recurso de menu", path);
            return texture;
        }
        [MenuItem("TRUST.EXE/Abrir MainMenu")]
        public static void Open()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        // Local, explicit validation hook; no network and no arbitrary code execution.
        // Write 'validate' to Library/TrustMenuRequest.txt while in Play Mode.
        static void ProcessValidationRequest()
        {
            const string request = "Library/TrustMenuRequest.txt";
            if (!File.Exists(request) || EditorApplication.isCompiling) return;
            string command;
            // The external writer may still own the file for a single editor tick.
            try { command = File.ReadAllText(request).Trim(); File.Delete(request); }
            catch (IOException) { return; }
            if (command == "build") { Build(); return; }
            if (command != "validate") return;
            try
            {
                if (!EditorApplication.isPlaying) throw new Exception("Enter Play Mode first.");
                var menu = UnityEngine.Object.FindAnyObjectByType<TrustMenuController>();
                Assert(menu != null, "Menu exists");
                Assert(menu.catalog.avatars.Length == 12, "Twelve characters");
                Assert(menu.catalog.avatars.All(a => a.idle && a.walk.All(t=>t) && a.think.All(t=>t)), "All animation frames assigned");
                menu.BeginSelection();
                for (int i=0;i<12;i++) { menu.Choose(0,i); Assert(menu.SelectedIndex(0)==i,"Avatar navigation " + i); }
                menu.Choose(0,0); menu.Choose(1,0); menu.Confirm(0); menu.Confirm(1);
                Assert(menu.IsLocked(0) && !menu.IsLocked(1), "Duplicate avatar blocked");
                menu.Choose(0,2); Assert(menu.SelectedIndex(0)==0, "Confirmed avatar cannot move");
                menu.Unlock(0); Assert(!menu.IsLocked(0), "Undo confirmation");
                menu.Choose(0,-1); Assert(menu.SelectedIndex(0)==11,"Navigation wraps");
                menu.Choose(0,7); menu.Choose(1,10); menu.Confirm(0); menu.Confirm(1); menu.BeginStory();
                Assert(LocalParty.Explorer=="Mara" && LocalParty.Operator=="Ren", "Session identities preserved");
                menu.Choose(0,11); menu.Choose(1,9);
                Assert(LocalParty.Explorer=="Zoro" && LocalParty.Operator=="Nova", "any selection propagates to the session before the ready screen");
                menu.Unlock(0);menu.Unlock(1);menu.Choose(0,7);menu.Choose(1,10);menu.Confirm(0);menu.Confirm(1);
                Assert(menu.CurrentScreen==TrustMenuController.Screen.Story,"Story after both confirmations");
                menu.AdvanceStory(); Assert(menu.StoryPage==0,"First press reveals text");
                menu.AdvanceStory(); Assert(menu.StoryPage==1,"Second press advances");
                menu.AdvanceStory(); menu.AdvanceStory(); menu.AdvanceStory(); menu.AdvanceStory();
                Assert(menu.CurrentScreen==TrustMenuController.Screen.Ready,"Full story reaches ready screen");
                menu.ShowHome(); Assert(!LocalParty.IsReady,"New session clears identities");
                menu.BeginSelection(); menu.Confirm(0); menu.Confirm(1); menu.BeginStory(); menu.FinishStory();
                Assert(menu.CurrentScreen==TrustMenuController.Screen.Ready,"Skip reaches ready screen");
                menu.ShowHome();
                File.WriteAllText("Library/TrustMenuValidation.txt", "PASS: catalog, 12 avatars, navigation, duplicate prevention, lock, undo, wrap, party, story reveal/advance, full story, skip, session reset.\n"+DateTime.UtcNow.ToString("O"));
                Debug.Log("TRUST.EXE menu validation PASS");
            }
            catch(Exception exception)
            { File.WriteAllText("Library/TrustMenuValidation.txt", "FAIL: "+exception); Debug.LogException(exception); }
        }
        static void Assert(bool value,string label) { if(!value) throw new Exception("Menu validation: "+label); }
    }
}
