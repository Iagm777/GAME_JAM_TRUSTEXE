using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrustEXE.Tutorial;
namespace TrustEXE.Nucleo.Editor {
 [InitializeOnLoad] public static class NucleoSetup {
  const string ScenePath="Assets/00_Scenes/Nucleo.unity",DataPath="Assets/06_Data/Nucleo/NucleoAssets.asset",AudioPath="Assets/04_Audio/Nucleo/Core_Protocol.wav";
  static NucleoSetup(){EditorApplication.update+=Poll;}
  static void Poll(){const string path="Library/TrustNucleoRequest.txt";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(path))return;
   string cmd=File.ReadAllText(path).Trim();File.Delete(path);try{
    if(cmd=="setup")Build();
    else if(cmd=="play"){EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);EditorApplication.isPaused=false;Time.timeScale=1;Application.runInBackground=true;EditorApplication.isPlaying=true;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Show();}
    else if(cmd=="stop")EditorApplication.isPlaying=false;
    else if(cmd=="validate")NucleoValidation.Run();
    else if(cmd=="runtime")UnityEngine.Object.FindAnyObjectByType<NucleoGame>().gameObject.AddComponent<NucleoRuntimeValidation>();
    else if(cmd=="status"){var g=UnityEngine.Object.FindAnyObjectByType<NucleoGame>();File.WriteAllText("Library/NucleoStatus.txt","playing="+EditorApplication.isPlaying+" paused="+EditorApplication.isPaused+" scale="+Time.timeScale+" frame="+Time.frameCount+" active="+g.gameObject.activeInHierarchy+" enabled="+g.enabled+" audio="+(g.Audio!=null)+" players="+(g.Players[0]!=null));}
    else if(cmd=="capture")ScreenCapture.CaptureScreenshot("Library/NucleoPreview.png");
    else if(cmd=="ready"){var g=UnityEngine.Object.FindAnyObjectByType<NucleoGame>();g.TestMode=true;g.SetReady(0);g.SetReady(1);}
    else if(cmd=="menu")EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/00_Scenes/MainMenu.unity");
    else if(cmd=="build"){var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),"Builds/Nucleo/TRUST_EXE.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);File.WriteAllText("Library/NucleoBuild.txt",report.summary.result+" errors="+report.summary.totalErrors);if(report.summary.totalErrors>0)throw new Exception("Build failed");}
    File.WriteAllText("Library/TrustNucleoResult.txt","OK: "+cmd);
   }catch(Exception e){File.WriteAllText("Library/TrustNucleoResult.txt",e.ToString());Debug.LogException(e);}
  }
  [MenuItem("TRUST.EXE/Nucleo/Preparar escenario final")]
  public static void Build(){
   if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first");
   Directory.CreateDirectory("Assets/06_Data/Nucleo");Directory.CreateDirectory("Assets/02_Prefabs/Nucleo");Directory.CreateDirectory(Path.GetDirectoryName(AudioPath));AssetDatabase.Refresh();
   var data=AssetDatabase.LoadAssetAtPath<NucleoAssets>(DataPath);
   if(!data){
    data=ScriptableObject.CreateInstance<NucleoAssets>();AssetDatabase.CreateAsset(data,DataPath);data.shared=AssetDatabase.LoadAssetAtPath<TutorialAssets>("Assets/06_Data/Tutorial/TutorialAssets.asset");
    var map=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/03_Sprites/Fondos/Nucleo.jpeg");data.maps=new Sprite[2];for(int p=0;p<2;p++){var s=Sprite.Create(map,new Rect(p*map.width/2,0,map.width/2,map.height),Vector2.one*.5f,map.height/9f);s.name="Nucleo_P"+p;AssetDatabase.AddObjectToAsset(s,data);data.maps[p]=s;}
    data.alarm=Frames("EVA_Alarm");data.warning=Frames("EVA_Warning");MakeMusic();AssetDatabase.ImportAsset(AudioPath);data.music=AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();
   }
   if(!File.Exists(ScenePath)){var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);var root=new GameObject("TRUST.EXE / Nucleo final");root.AddComponent<NucleoGame>().assets=data;PrefabUtility.SaveAsPrefabAsset(root,"Assets/02_Prefabs/Nucleo/PF_Nucleo.prefab");EditorSceneManager.SaveScene(scene,ScenePath);EditorSceneManager.CloseScene(scene,true);if(original.IsValid())SceneManager.SetActiveScene(original);}
   EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();AssetDatabase.SaveAssets();
  }
  static Texture2D[] Frames(string filter)=>AssetDatabase.FindAssets(filter+" t:Texture2D").Select(AssetDatabase.GUIDToAssetPath).OrderBy(x=>x).Select(AssetDatabase.LoadAssetAtPath<Texture2D>).ToArray();
  static void MakeMusic(){
   const int rate=22050;const double beat=.4;float[] samples=new float[rate*16];var rng=new System.Random(303);
   for(int b=0;b<40;b++){
    int root=new[]{38,38,34,36}[b/10];
    for(int a=0;a<4;a++){
     double start=b*beat+a*.1,f=440*Math.Pow(2,(root+24+new[]{0,7,12,15}[a]-69)/12.0);
     for(int j=0;j<rate*.19;j++){double t=(double)j/rate;samples[((int)(start*rate)+j)%samples.Length]+=(float)(Math.Sin(2*Math.PI*f*t)*Math.Sin(Math.PI*t/.19)*.055);}
    }
    for(int j=0;j<rate*.35;j++){double t=(double)j/rate,env=Math.Exp(-t*18);double kick=Math.Sin(2*Math.PI*(48*t+2*(1-Math.Exp(-t*30))));samples[(b*(int)(rate*beat)+j)%samples.Length]+=(float)(kick*env*.22+(rng.NextDouble()*2-1)*Math.Exp(-t*75)*.055);}
   }
   using(var w=new BinaryWriter(File.Create(AudioPath))){int bytes=samples.Length*2;w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+bytes);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(bytes);foreach(float s in samples)w.Write((short)(Mathf.Clamp(s,-.9f,.9f)*32767));}
  }
 }
}


