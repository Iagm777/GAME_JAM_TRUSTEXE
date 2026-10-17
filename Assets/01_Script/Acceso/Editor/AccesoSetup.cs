using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrustEXE.Tutorial;

namespace TrustEXE.Acceso.Editor
{
    [InitializeOnLoad]
    public static class AccesoSetup
    {
        const string ScenePath="Assets/00_Scenes/Acceso.unity",DataPath="Assets/06_Data/Acceso/AccesoAssets.asset";
        static AccesoSetup(){EditorApplication.update+=Poll;}
        static void Poll(){
            const string request="Library/TrustAccesoRequest.txt";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
            string cmd=File.ReadAllText(request).Trim();File.Delete(request);
            try{
                if(cmd=="setup")Build();
                else if(cmd=="play"){EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);EditorApplication.isPlaying=true;}
                else if(cmd=="stop")EditorApplication.isPlaying=false;
                else if(cmd=="validate")AccesoValidation.Run();
                else if(cmd=="runtime")UnityEngine.Object.FindAnyObjectByType<AccesoGame>().gameObject.AddComponent<AccesoRuntimeValidation>();
                else if(cmd=="capture")ScreenCapture.CaptureScreenshot("Library/AccesoPreview.png");
                else if(cmd=="open"){if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save current scene before opening Acceso.");EditorSceneManager.OpenScene(ScenePath);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);}
                else if(cmd=="build"){var report=UnityEditor.BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),"Builds/Acceso/TRUST_EXE.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);if(report.summary.totalErrors>0)throw new Exception("Build errors: "+report.summary.totalErrors);}
                File.WriteAllText("Library/TrustAccesoResult.txt","OK: "+cmd);
            }catch(Exception e){File.WriteAllText("Library/TrustAccesoResult.txt",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("TRUST.EXE/Acceso/Preparar escena")]
        public static void Build(){
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            Directory.CreateDirectory("Assets/06_Data/Acceso");Directory.CreateDirectory("Assets/02_Prefabs/Acceso");AssetDatabase.Refresh();
            var data=AssetDatabase.LoadAssetAtPath<AccesoAssets>(DataPath);
            if(!data){
                data=ScriptableObject.CreateInstance<AccesoAssets>();AssetDatabase.CreateAsset(data,DataPath);
                data.shared=AssetDatabase.LoadAssetAtPath<TutorialAssets>("Assets/06_Data/Tutorial/TutorialAssets.asset");
                if(!data.shared)throw new Exception("Tutorial assets are required.");
                var map=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/03_Sprites/Fondos/Acceso.jpeg");data.maps=new Sprite[2];
                for(int p=0;p<2;p++){var sprite=Sprite.Create(map,new Rect(p*map.width/2,0,map.width/2,map.height),Vector2.one*.5f,map.height/9f);sprite.name="Acceso_P"+(p+1);AssetDatabase.AddObjectToAsset(sprite,data);data.maps[p]=sprite;}
                data.evaWarning=AssetDatabase.FindAssets("EVA_Warning t:Texture2D").Select(AssetDatabase.GUIDToAssetPath).OrderBy(x=>x).Select(AssetDatabase.LoadAssetAtPath<Texture2D>).ToArray();
                data.ambience=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/04_Audio/Acceso/Access_Ambience.wav");
                EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();
            }
            if(!File.Exists(ScenePath)){
                var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
                var root=new GameObject("TRUST.EXE / Acceso");root.AddComponent<AccesoGame>().assets=data;
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/02_Prefabs/Acceso/PF_Acceso.prefab");EditorSceneManager.SaveScene(scene,ScenePath);EditorSceneManager.CloseScene(scene,true);if(original.IsValid())SceneManager.SetActiveScene(original);
            }
            EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath&&s.path!="Assets/00_Scenes/Tutorial.unity").Concat(new[]{new EditorBuildSettingsScene("Assets/00_Scenes/Tutorial.unity",true),new EditorBuildSettingsScene(ScenePath,true)}).ToArray();AssetDatabase.SaveAssets();
        }
    }
}
