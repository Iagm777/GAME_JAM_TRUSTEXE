using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrustEXE.Tutorial;
namespace TrustEXE.Vigilancia.Editor {
 [InitializeOnLoad] public static class VigilanciaSetup {
  const string ScenePath="Assets/00_Scenes/Vigilancia.unity",DataPath="Assets/06_Data/Vigilancia/VigilanciaAssets.asset",AudioPath="Assets/04_Audio/Vigilancia/Surveillance_Grid.wav";
  static VigilanciaSetup(){EditorApplication.update+=Poll;}
  static void Poll(){const string path="Library/TrustVigilanciaRequest.txt";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(path))return;
   string cmd=File.ReadAllText(path).Trim();File.Delete(path);try{
    if(cmd=="setup")Build();
    else if(cmd=="open"){if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Save current scene and stop Play Mode first.");EditorSceneManager.OpenScene(ScenePath);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);}
    else if(cmd=="play"){EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);EditorApplication.isPlaying=true;}
    else if(cmd=="stop")EditorApplication.isPlaying=false;
    else if(cmd=="validate")VigilanciaValidation.Run();
    else if(cmd=="runtime")UnityEngine.Object.FindAnyObjectByType<VigilanciaGame>().gameObject.AddComponent<VigilanciaRuntimeValidation>();
    else if(cmd=="capture")ScreenCapture.CaptureScreenshot("Library/VigilanciaPreview.png");
    else if(cmd=="ready"){var g=UnityEngine.Object.FindAnyObjectByType<VigilanciaGame>();g.TestMode=true;g.SetReady(0);g.SetReady(1);}
    else if(cmd=="build"){var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),"Builds/Vigilancia/TRUST_EXE.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);File.WriteAllText("Library/VigilanciaBuild.txt",report.summary.result+" errors="+report.summary.totalErrors);if(report.summary.totalErrors>0)throw new Exception("Build failed");}
    File.WriteAllText("Library/TrustVigilanciaResult.txt","OK: "+cmd);
   }catch(Exception e){File.WriteAllText("Library/TrustVigilanciaResult.txt",e.ToString());Debug.LogException(e);}
  }
  [MenuItem("TRUST.EXE/Vigilancia/Preparar escenario")]
  public static void Build(){
   if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first");
   Directory.CreateDirectory("Assets/06_Data/Vigilancia");Directory.CreateDirectory("Assets/02_Prefabs/Vigilancia");Directory.CreateDirectory(Path.GetDirectoryName(AudioPath));AssetDatabase.Refresh();
   var data=AssetDatabase.LoadAssetAtPath<VigilanciaAssets>(DataPath);
   if(!data){
    data=ScriptableObject.CreateInstance<VigilanciaAssets>();AssetDatabase.CreateAsset(data,DataPath);data.shared=AssetDatabase.LoadAssetAtPath<TutorialAssets>("Assets/06_Data/Tutorial/TutorialAssets.asset");
    var map=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/03_Sprites/Fondos/Vigilancia.jpeg");data.maps=new Sprite[2];for(int p=0;p<2;p++){var s=Sprite.Create(map,new Rect(p*map.width/2,0,map.width/2,map.height),Vector2.one*.5f,map.height/9f);s.name="Vigilancia_P"+p;AssetDatabase.AddObjectToAsset(s,data);data.maps[p]=s;}
    data.cameras=new[]{"Ambar","Cian"}.Select(c=>Art(data,"Lamina_02/Camaras/L02_Camaras_"+c+"_Frontal_Normal.png",.55f)).ToArray();
    data.alarmCameras=new[]{"Ambar","Cian"}.Select(c=>Art(data,"Lamina_02/Camaras/L02_Camaras_"+c+"_Frontal_Alerta.png",.55f)).ToArray();
    data.evaAlarm=AssetDatabase.FindAssets("EVA_Alarm t:Texture2D").Select(AssetDatabase.GUIDToAssetPath).OrderBy(s=>s).Select(AssetDatabase.LoadAssetAtPath<Texture2D>).ToArray();
    MakeMusic();AssetDatabase.ImportAsset(AudioPath);data.music=AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();
   }
   if(!File.Exists(ScenePath)){var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);var root=new GameObject("TRUST.EXE / Vigilancia");root.AddComponent<VigilanciaGame>().assets=data;PrefabUtility.SaveAsPrefabAsset(root,"Assets/02_Prefabs/Vigilancia/PF_Vigilancia.prefab");EditorSceneManager.SaveScene(scene,ScenePath);EditorSceneManager.CloseScene(scene,true);if(original.IsValid())SceneManager.SetActiveScene(original);}
   EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();AssetDatabase.SaveAssets();
  }
  static Sprite Art(VigilanciaAssets data,string path,float width){var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/03_Sprites/Objetos_Laboratorio/PNG_Transparentes/"+path);if(!texture)throw new FileNotFoundException(path);var s=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f,texture.width/width);s.name=Path.GetFileNameWithoutExtension(path);AssetDatabase.AddObjectToAsset(s,data);return s;}
  // Original 8-bar, 96 BPM electronic loop; integer-cycle pads and wrapped percussion tails eliminate seams.
  static void MakeMusic(){
   const int rate=22050;const double duration=20,beat=.625;float[] samples=new float[(int)(rate*duration)];var random=new System.Random(2026);
   Action<double,double,double,double,int> note=(start,length,midi,gain,kind)=>{double f=440*Math.Pow(2,(midi-69)/12);for(int i=0;i<(int)(length*rate);i++){double t=(double)i/rate,env=Math.Min(1,t/.012)*Math.Exp(-t/(length*.25));double value=kind==2?(random.NextDouble()*2-1)*Math.Exp(-t*40):kind==3?Math.Sin(2*Math.PI*(52*t+2*(1-Math.Exp(-t*30)))):Math.Sin(2*Math.PI*f*t)+.18*Math.Sin(4*Math.PI*f*t);samples[((int)(start*rate)+i)%samples.Length]+=(float)(value*gain*env);}};
   int[] roots={40,40,43,43,36,36,38,38};int[] arp={0,7,12,15,12,7,19,7};
   for(int b=0;b<32;b++){int root=roots[b/4];note(b*beat,.55,root,.16,0);note(b*beat,.22,0,.18,3);note(b*beat,.06,0,.045,2);note(b*beat+.3125,.05,0,.025,2);if(b%2==1)note(b*beat,.12,0,.08,2);for(int a=0;a<2;a++)note(b*beat+a*.3125,.48,root+24+arp[(b*2+a)%8],.045,1);}
   for(int i=0;i<samples.Length;i++){double t=(double)i/rate;samples[i]+=(float)(.025*Math.Sin(2*Math.PI*82.4*t)+.018*Math.Sin(2*Math.PI*123.5*t));}
   using(var w=new BinaryWriter(File.Create(AudioPath))){int bytes=samples.Length*2;w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+bytes);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(bytes);foreach(float s in samples)w.Write((short)(Mathf.Clamp(s,-.9f,.9f)*32767));}
  }
 }
}
