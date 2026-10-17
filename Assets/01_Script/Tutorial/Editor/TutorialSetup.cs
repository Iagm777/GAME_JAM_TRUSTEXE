using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrustEXE.Menu;

namespace TrustEXE.Tutorial.Editor
{
    [InitializeOnLoad]
    public static class TutorialSetup
    {
        const string ScenePath="Assets/00_Scenes/Tutorial.unity";
        const string DataPath="Assets/06_Data/Tutorial/TutorialAssets.asset";
        const string AudioPath="Assets/04_Audio/Tutorial/Music/Trust_Swing_Loop.wav";
        static TutorialSetup(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string path="Library/TrustTutorialRequest.txt";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(path))return;
            string command;
            try {command=File.ReadAllText(path).Trim();File.Delete(path);}catch(IOException){return;}
            try
            {
                if(command=="setup")Build();
                if(command=="refresh")AssetDatabase.Refresh();
                if(command=="play") {EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);EditorApplication.isPlaying=true;}
                if(command=="stop")EditorApplication.isPlaying=false;
                if(command=="validate")TutorialValidation.Run();
                if(command=="capture")ScreenCapture.CaptureScreenshot("Library/TutorialPreview.png");
                if(command=="ready") {var g=UnityEngine.Object.FindAnyObjectByType<TutorialGame>();g.Ready(0);g.Ready(1);}
                if(command=="menu") {EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/00_Scenes/MainMenu.unity");AssetDatabase.SaveAssets();}
                if(command=="build")BuildWindows();
                File.WriteAllText("Library/TrustTutorialResult.txt","OK: "+command);
            }
            catch(Exception e){File.WriteAllText("Library/TrustTutorialResult.txt",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("TRUST.EXE/Tutorial/Preparar tutorial completo")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before setup.");
            Directory.CreateDirectory("Assets/06_Data/Tutorial");Directory.CreateDirectory(Path.GetDirectoryName(AudioPath));
            AssetDatabase.Refresh();
            var data=AssetDatabase.LoadAssetAtPath<TutorialAssets>(DataPath);
            if(data) {
                if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
                AssetDatabase.SaveAssets();return;
            }
            data=ScriptableObject.CreateInstance<TutorialAssets>();AssetDatabase.CreateAsset(data,DataPath);
            data.menu=AssetDatabase.LoadAssetAtPath<TrustMenuCatalog>("Assets/01_Script/Menu/MenuCatalog.asset");
            var map=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/03_Sprites/Fondos/Tutorial.jpeg");
            data.maps=new Sprite[2];
            for(int p=0;p<2;p++) {var s=UnityEngine.Sprite.Create(map,new Rect(p*map.width/2,0,map.width/2,map.height),Vector2.one*.5f,map.height/9f);s.name="Tutorial_Map_"+p;AssetDatabase.AddObjectToAsset(s,data);data.maps[p]=s;}
            data.characters=data.menu.avatars.Select(a=>new TutorialAssets.Character {
                id=a.id,idle=CharacterSprite(data,a.id,"Idle"),
                down=Frames(data,a.id,"Down"),up=Frames(data,a.id,"Up"),left=Frames(data,a.id,"Left"),right=Frames(data,a.id,"Right")
            }).ToArray();
            data.consoles=Pair(data,"Consolas","Consolas","Encendida",.76f);
            data.nodes=Pair(data,"Nodos","Nodos","Encendido",.65f);
            data.switchesOff=Pair(data,"Interruptores","Interruptores","Apagado",.52f);
            data.switchesOn=Pair(data,"Interruptores","Interruptores","Encendido",.52f);
            data.doorsClosed=Pair(data,"Puertas","Puertas","Cerrada",1.12f);
            data.doorsOpen=Pair(data,"Puertas","Puertas","Abierta",1.12f);
            MakeMusic();AssetDatabase.ImportAsset(AudioPath);data.music=AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);
            EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();
            var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            var root=new GameObject("TRUST.EXE - Cooperative Tutorial");root.AddComponent<TutorialGame>().assets=data;
            // Save a reusable bootstrap prefab; worlds and colliders are authored in TutorialGame.
            Directory.CreateDirectory("Assets/02_Prefabs/Tutorial");AssetDatabase.Refresh();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/02_Prefabs/Tutorial/PF_Tutorial.prefab");
            EditorSceneManager.SaveScene(scene,ScenePath);EditorSceneManager.CloseScene(scene,true);if(original.IsValid())SceneManager.SetActiveScene(original);
            EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/00_Scenes/MainMenu.unity");
            Debug.Log("TRUST.EXE Tutorial ready: scene, 12 animated characters, split maps, devices, original music.");
        }
        static Sprite[] Frames(TutorialAssets data,string id,string direction)=>Enumerable.Range(0,3).Select(i=>CharacterSprite(data,id,"Walk_"+direction+"_"+i)).ToArray();
        static Sprite CharacterSprite(TutorialAssets data,string id,string pose)
        {
            string path="Assets/03_Sprites/Character/"+id+"/"+id+"_"+pose+".png";
            if(!File.Exists(path)&&id=="Nova")path=pose=="Walk_Left_2"?"Assets/03_Sprites/Character/Nova/Nova_Walk_Left_0_otro_pie.png":"Assets/03_Sprites/Character/Nova/Nova_Walk_Right_2_otro_pie.png";
            return Sprite(data,path,.96f);
        }
        static Sprite[] Pair(TutorialAssets data,string folder,string label,string state,float width)=>new[]{"Ambar","Cian"}.Select(c=>Sprite(data,"Assets/03_Sprites/Objetos_Laboratorio/PNG_Transparentes/Lamina_01/"+folder+"/L01_"+label+"_"+c+"_"+state+".png",width)).ToArray();
        static Sprite Sprite(TutorialAssets data,string path,float width)
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!texture)throw new FileNotFoundException("Missing art",path);
            var sprite=UnityEngine.Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f,texture.width/width);
            sprite.name=Path.GetFileNameWithoutExtension(path);AssetDatabase.AddObjectToAsset(sprite,data);return sprite;
        }
        // Original 16-bar electro-swing sketch, 120 BPM: walking bass, swung keys, brushed percussion.
        // Rendered to WAV so the source asset can be auditioned or replaced in the project.
        static void MakeMusic()
        {
            const int rate=22050;const double beat=.5;var samples=new float[rate*32];var rng=new System.Random(19);
            int[] roots={48,48,53,48,55,53,48,55};int[] melody={72,75,79,82,79,75,74,72,67,70,74,77,74,70,67,70};
            Action<double,double,double,double,int> note=(start,duration,midi,gain,kind)=>{
                int begin=(int)(start*rate),count=(int)(duration*rate);double f=440*Math.Pow(2,(midi-69)/12);
                for(int i=0;i<count&&begin+i<samples.Length;i++) {double t=(double)i/rate;double env=Math.Min(1,t/.006)*Math.Pow(1-(double)i/count,2);
                    double v=kind==2?(rng.NextDouble()*2-1)*Math.Exp(-t*45):Math.Sin(2*Math.PI*f*t)+.22*Math.Sin(4*Math.PI*f*t);
                    samples[begin+i]+=(float)(v*gain*env);
                }
            };
            for(int b=0;b<64;b++)
            {
                int root=roots[(b/8)%roots.Length];int[] walk={0,7,12,7};
                note(b*beat,.4,root-12+walk[b%4],.16,0);
                note(b*beat+.333,.15,melody[b%melody.Length]+((b/16)%2==1?-12:0),.10,1);
                if(b%2==1){note(b*beat,.18,root+12,.055,1);note(b*beat,.18,root+15,.055,1);note(b*beat,.18,root+19,.055,1);note(b*beat,.10,0,.09,2);}
                note(b*beat,.045,0,.035,2);note(b*beat+.333,.035,0,.025,2);
            }
            using(var stream=new BinaryWriter(File.Create(AudioPath)))
            {
                int length=samples.Length*2;stream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));stream.Write(36+length);stream.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));stream.Write(16);stream.Write((short)1);stream.Write((short)1);stream.Write(rate);stream.Write(rate*2);stream.Write((short)2);stream.Write((short)16);stream.Write(System.Text.Encoding.ASCII.GetBytes("data"));stream.Write(length);
                foreach(float sample in samples)stream.Write((short)(Mathf.Clamp(sample,-.95f,.95f)*32767));
            }
        }
        [MenuItem("TRUST.EXE/Tutorial/Build Windows")]
        public static void BuildWindows()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            Directory.CreateDirectory("Builds/Tutorial");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/00_Scenes/MainMenu.unity",true),new EditorBuildSettingsScene(ScenePath,true)};
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/00_Scenes/MainMenu.unity");
            AssetDatabase.SaveAssets();
            var result=BuildPipeline.BuildPlayer(new[]{"Assets/00_Scenes/MainMenu.unity",ScenePath},"Builds/Tutorial/TRUST_EXE.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
            File.WriteAllText("Library/TrustTutorialBuild.txt",result.summary.result+" errors="+result.summary.totalErrors);
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed.");
        }
    }
}

