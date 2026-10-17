using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrustEXE.Menu;

namespace TrustEXE.Tutorial
{
    public sealed class TutorialGame : MonoBehaviour
    {
        public TutorialAssets assets;
        public TutorialProgress Progress {get;private set;} = new TutorialProgress();
        public TutorialPlayer[] Players {get;private set;} = new TutorialPlayer[2];
        public bool Started {get;private set;}
        public bool Paused {get;private set;}
        public bool[] IntroReady {get;} = new bool[2];
        public string EvaMessage {get;private set;} = "Bienvenidos. Este entrenamiento es seguro. Mis indicaciones serán fiables: aprendan a escucharse y a trabajar juntos.";
        public string SaveError {get;private set;}
        public bool GatePassable {get;private set;}
        public bool[] Reading {get;} = new bool[2];
        public string[] Notes {get;} = {"Aún no has inspeccionado el terminal.","Aún no has activado el nodo."};
        public string[] Feedback {get;} = {"", ""};
        public string[] Prompt {get;} = {"", ""};
        public string[] Popup {get;} = {"", ""};
        public struct Device
        {
            public int player, value;
            public Vector2 position;
            public string title;
            public SpriteRenderer view;
        }
        readonly Device[,] devices=new Device[2,3];
        readonly BoxCollider2D[] gates=new BoxCollider2D[2];
        readonly Transform[] gateViews=new Transform[2];
        readonly SpriteRenderer[] exitLights=new SpriteRenderer[2];
        readonly float[] feedbackUntil=new float[2];
        readonly Color[] accents={new Color(1,.66f,.25f),new Color(.2f,.85f,1)};
        TutorialHud hud;
        AudioSource sound;
        AudioSource music;
        AudioClip chime, errorTone;
        Sprite white;
        bool attemptedSave,loading;
        Lesson lastLesson;
        public static Vector2 Offset(int player) => new Vector2(player*20,0);

        void Start()
        {
            if(!LocalParty.IsReady) LocalParty.Select("Mara","Ren");
            white=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
            sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;
            chime=Tone(680,.18f);errorTone=Tone(180,.12f);
            music=gameObject.AddComponent<AudioSource>();music.clip=assets.music;music.loop=true;music.volume=PlayerPrefs.GetFloat("TutorialVolume",.55f);music.Play();
            for(int p=0;p<2;p++) BuildWorld(p);
            hud=gameObject.AddComponent<TutorialHud>();hud.Initialize(this);
            gameObject.AddComponent<TutorialAtmosphere>().Initialize(this);
        }
        void BuildWorld(int p)
        {
            Vector2 offset=Offset(p);
            var cameraObject=new GameObject("P"+(p+1)+" camera");cameraObject.transform.SetParent(transform);
            var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=4.7f;
            camera.transform.position=new Vector3(offset.x+4,4.5f,-10);
            camera.rect=new Rect(p*.5f,.20f,.5f,.65f);camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=p==0?new Color(.035f,.028f,.02f):new Color(.015f,.035f,.06f);
            if(p==0)cameraObject.AddComponent<AudioListener>();
            Visual("Map P"+(p+1),assets.maps[p],offset+new Vector2(4,4.5f),0);
            // Manually traced navigable silhouette, not inferred from image pixels.
            var walls=new GameObject("Room and corridor walls P"+(p+1));walls.transform.SetParent(transform);walls.transform.position=offset;
            var edge=walls.AddComponent<EdgeCollider2D>();edge.edgeRadius=.025f;
            edge.points=new[]{new Vector2(1,.9f),new Vector2(6.9f,.9f),new Vector2(6.9f,4.2f),new Vector2(4.45f,4.2f),new Vector2(4.45f,7),new Vector2(5.9f,7),new Vector2(5.9f,8.45f),new Vector2(2.1f,8.45f),new Vector2(2.1f,7),new Vector2(3.55f,7),new Vector2(3.55f,4.2f),new Vector2(1,4.2f),new Vector2(1,.9f)};
            var actor=new GameObject("P"+(p+1)+" "+(p==0?LocalParty.Explorer:LocalParty.Operator));actor.transform.SetParent(transform);actor.transform.position=offset+new Vector2(3.8f,1.8f);
            Players[p]=actor.AddComponent<TutorialPlayer>();Players[p].player=p;Players[p].game=this;
            string identity=p==0?LocalParty.Explorer:LocalParty.Operator;
            Players[p].art=assets.characters.FirstOrDefault(a=>a.id==identity)??assets.characters[p];
            var shadow=Visual("Player ground marker",white,offset+new Vector2(3.8f,1.8f),3);
            shadow.transform.SetParent(actor.transform,true);shadow.transform.localPosition=new Vector3(0,-.25f,.1f);
            shadow.transform.localScale=new Vector3(.42f,.10f,1);shadow.color=new Color(accents[p].r,accents[p].g,accents[p].b,.7f);
            MakeDevice(p,0,0,new Vector2(2.2f,3.35f),p==0?"TERMINAL":"NODO",p==0?assets.consoles[p]:assets.nodes[p]);
            MakeDevice(p,1,1,new Vector2(5.55f,1.65f),p==0?"CÍRCULO":"1",assets.switchesOff[p]);
            MakeDevice(p,2,2,new Vector2(5.55f,3.2f),p==0?"TRIÁNGULO":"2",assets.switchesOff[p]);
            var gate=Visual("Door P"+(p+1),assets.doorsClosed[p],offset+new Vector2(4,4.6f),25);
            gateViews[p]=gate.transform;
            var gateCollider=new GameObject("Door blocker P"+(p+1));gateCollider.transform.SetParent(transform);gateCollider.transform.position=offset+new Vector2(4,4.6f);
            gates[p]=gateCollider.AddComponent<BoxCollider2D>();gates[p].size=new Vector2(.9f,.28f);
            var beam=Visual("Gate beam",white,offset+new Vector2(4,4.6f),24);beam.transform.SetParent(gate.transform,true);beam.transform.localScale=new Vector3(.9f,.09f,1);beam.color=accents[p];
            exitLights[p]=Visual("Exit zone",white,offset+new Vector2(4,7.75f),2);exitLights[p].transform.localScale=new Vector3(1.4f,.75f,1);exitLights[p].color=new Color(.15f,.25f,.28f,.5f);
            WorldText("SALIDA",offset+new Vector2(4,8.15f),.055f,Color.white);
            WorldText("P"+(p+1),offset+new Vector2(3.8f,1.3f),.05f,accents[p]);
        }
        void MakeDevice(int p,int index,int value,Vector2 local,string title,Sprite sprite)
        {
            Vector2 position=Offset(p)+local;
            var view=Visual(title+" P"+(p+1),sprite,position,20);
            var collider=view.gameObject.AddComponent<BoxCollider2D>();collider.size=new Vector2(.46f,.35f);
            devices[p,index]=new Device {player=p,value=value,position=position,title=title,view=view};
            WorldText(title,position+new Vector2(0,.57f),.048f,accents[p]);
        }
        SpriteRenderer Visual(string label,Sprite sprite,Vector2 position,int order)
        {var go=new GameObject(label);go.transform.SetParent(transform);go.transform.position=position;var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;return r;}
        void WorldText(string value,Vector2 position,float size,Color color)
        {
            var go=new GameObject(value);go.transform.SetParent(transform);go.transform.position=position;
            var text=go.AddComponent<TextMesh>();text.text=value;text.fontSize=60;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.color=color;
            go.GetComponent<MeshRenderer>().sortingOrder=100;
        }
        public bool CanMove(int p) => Started&&!Paused&&!Reading[p]&&!Progress.completed;
        public void SetMusicVolume(float value) {if(music)music.volume=value;}
        public void Ready(int p) {if(IntroReady[p])return;IntroReady[p]=true;Play(false);if(IntroReady[0]&&IntroReady[1]) {Started=true;EvaMessage="Primera lección: muévanse por sus salas. Cada persona controla a su propio personaje.";}}
        public void TogglePause() {if(!Started||Progress.completed)return;Paused=!Paused;hud.ShowPause(Paused);}
        public void ShowHelp(int p)
        {
            Reading[p]=true;
            Popup[p]="CONTROLES DE P"+(p+1)+"\n\n"+LocalControls.MoveName(p)+" — moverse\n"+LocalControls.ActionName(p)+" — inspeccionar / activar\n"+LocalControls.HelpName(p)+" — recordar pista y controles\n"+LocalControls.CancelName(p)+" — cerrar este panel\n\nTU PISTA: "+Notes[p]+"\n\nAcércate a menos de un paso del objeto. La interacción solo afecta a tu lado; tu compañero puede seguir jugando.";
        }
        public void ClosePopup(int p) {Reading[p]=false;}
        public int NearestDevice(int p)
        {
            int result=-1;float closest=.85f;
            for(int i=0;i<3;i++) {float d=Vector2.Distance(Players[p].Position,devices[p,i].position);if(d<closest){closest=d;result=i;}}
            return result;
        }
        void Update()
        {
            if(!hud)return;
            if(!Started)
            {
                for(int p=0;p<2;p++)if(LocalControls.Interact(p))Ready(p);
                hud.Refresh();return;
            }
            if(LocalControls.Pause)TogglePause();
            if(Paused) {if(LocalControls.Interact(0)||LocalControls.Interact(1))TogglePause();hud.Refresh();return;}
            if(Progress.completed){hud.Refresh();return;}
            for(int p=0;p<2;p++)
            {
                if(Players[p].DistanceWalked>.9f)Progress.moved[p]=true;
                if(LocalControls.Help(p)) {if(Reading[p])ClosePopup(p);else ShowHelp(p);}
                else if(Reading[p]) {if(LocalControls.Interact(p)||LocalControls.Cancel(p))ClosePopup(p);}
                else if(LocalControls.Interact(p))
                {
                    if(SaveError!=null&&Progress.CanComplete)SaveCheckpoint();
                    else Interact(p);
                }
                if(Time.unscaledTime>feedbackUntil[p])Feedback[p]="";
                int nearest=NearestDevice(p);
                Prompt[p]=nearest<0?"ACÉRCATE A UN OBJETO":LocalControls.ActionName(p)+"  ·  "+devices[p,nearest].title;
                Progress.atExit[p]=GatePassable&&Vector2.Distance(Players[p].Position,Offset(p)+new Vector2(4,7.75f))<.65f;
                exitLights[p].color=Progress.atExit[p]?new Color(.3f,1,.55f,.8f):GatePassable?new Color(accents[p].r,accents[p].g,accents[p].b,.35f+.12f*Mathf.Sin(Time.time*3)):new Color(.15f,.25f,.28f,.5f);
            }
            if(Progress.CanComplete&&!attemptedSave)SaveCheckpoint();
            if(lastLesson!=Progress.Lesson)
            {
                lastLesson=Progress.Lesson;Play(false);
                if(lastLesson==Lesson.Clues)EvaMessage="Bien. P1: inspecciona el terminal. P2: activa el nodo. Después cuenten en voz alta lo que descubrieron.";
                if(lastLesson==Lesson.Sequence)EvaMessage="Las dos pistas son correctas. Primero actúa el mundo físico; después, el virtual. Un error reinicia la secuencia, no sus pistas.";
                if(lastLesson==Lesson.Exit)EvaMessage="Secuencia correcta. He abierto ambas puertas. Reúnanse en las zonas SALIDA: guardaré solo cuando lleguen los dos.";
            }
            hud.Refresh();
        }
        public void Interact(int p)
        {
            int nearest=NearestDevice(p);
            if(nearest<0){Say(p,"Acércate al terminal, nodo o interruptor antes de interactuar.",true);return;}
            if(Progress.Lesson==Lesson.Movement){Say(p,"Primero caminen ambos un poco para practicar los controles.",true);return;}
            var d=devices[p,nearest];
            if(nearest==0)
            {
                Progress.ReadClue(p);Reading[p]=true;
                Notes[p]=p==0?"El símbolo del terminal es TRIÁNGULO. Díselo a P2. Activa primero el interruptor físico con ese símbolo.":"Clave parcial: CÍRCULO → 1; TRIÁNGULO → 2. Pregunta a P1 su símbolo. Después de P1, activa el número correspondiente.";
                Popup[p]=(p==0?"TERMINAL FÍSICO / PISTA RECUPERADA":"NODO VIRTUAL / CLAVE PARCIAL")+"\n\n"+Notes[p]+"\n\nEl orden importa: P1 primero → P2 después.\n\nComparte tu pista en voz alta. Puedes volver a consultarla con "+LocalControls.HelpName(p)+".";
                d.view.color=new Color(.65f,1,.78f);Play(false);return;
            }
            if(!Progress.terminalRead||!Progress.nodeRead){Say(p,"Falta información. P1 debe leer el terminal y P2 activar el nodo.",true);return;}
            if(Progress.DoorOpen){Say(p,"Puerta abierta. Cruza el pasillo hacia SALIDA.",false);return;}
            bool correct=Progress.PressSwitch(p,d.value);
            for(int player=0;player<2;player++) for(int i=1;i<3;i++)devices[player,i].view.sprite=assets.switchesOff[player];
            if(Progress.sequence>=1)devices[0,2].view.sprite=assets.switchesOn[0];
            if(Progress.sequence==2)devices[1,2].view.sprite=assets.switchesOn[1];
            if(!correct)
            {
                Say(0,"Secuencia reiniciada. Revisa el símbolo y activa P1 primero.",true);
                Say(1,"Espera a P1; después pulsa el número que corresponde a su símbolo.",false);
                EvaMessage="No hay penalización. Sus pistas siguen disponibles. Recuerden: físico primero, virtual después.";
            }
            else {Say(p,Progress.DoorOpen?"¡ENLACE COMPLETO! Abriendo puertas…":"Paso 1 correcto. Ahora le toca a P2.",false);if(Progress.DoorOpen)StartCoroutine(OpenGates());}
        }
        IEnumerator OpenGates()
        {
            float t=0;
            while(t<.7f)
            {
                if(!Paused)t+=Time.deltaTime;
                for(int p=0;p<2;p++)gateViews[p].position=Offset(p)+new Vector2(4+Mathf.SmoothStep(0,.85f,t/.7f),4.6f);
                yield return null;
            }
            for(int p=0;p<2;p++){gates[p].enabled=false;gateViews[p].GetComponent<SpriteRenderer>().sprite=assets.doorsOpen[p];}
            GatePassable=true;
        }
        void Say(int p,string message,bool error) {Feedback[p]=message;feedbackUntil[p]=Time.unscaledTime+5;Play(error);}
        public void SaveCheckpoint()
        {
            if(!Progress.CanComplete)return;attemptedSave=true;
            try
            {
                Progress.completed=true;
                TutorialCheckpoint.Write(new TutorialSave {explorer=LocalParty.Explorer,operatorId=LocalParty.Operator,explorerPosition=Players[0].Position,operatorPosition=Players[1].Position,progress=Progress,savedUtc=DateTime.UtcNow.ToString("O")});
                SaveError=null;EvaMessage="Checkpoint guardado. Han aprendido a explorar, compartir pistas y coordinar acciones. El siguiente sector es Acceso.";
                Play(false);hud.ShowComplete();
            }
            catch(Exception e) when(e is System.IO.IOException||e is UnauthorizedAccessException)
            {Progress.completed=false;SaveError="No se pudo guardar. Permanezcan en SALIDA y pulsen interactuar para reintentar.";EvaMessage=SaveError;Debug.LogWarning("TRUST.EXE checkpoint: "+e.Message);}
        }
        public bool RestoreCheckpoint()
        {
            if(!TutorialCheckpoint.TryRead(out var save))return false;
            if(!assets.characters.Any(a=>a.id==save.explorer)||!assets.characters.Any(a=>a.id==save.operatorId))return false;
            LocalParty.Select(save.explorer,save.operatorId);Progress=save.progress;Started=true;Paused=false;GatePassable=true;
            for(int p=0;p<2;p++)
            {
                Players[p].art=assets.characters.First(a=>a.id==(p==0?save.explorer:save.operatorId));
                // Checkpoint positions are deterministic and validated, never trust arbitrary JSON coordinates.
                Players[p].Teleport(Offset(p)+new Vector2(4,7.75f));gates[p].enabled=false;
                gateViews[p].position=Offset(p)+new Vector2(4.85f,4.6f);
                gateViews[p].GetComponent<SpriteRenderer>().sprite=assets.doorsOpen[p];
            }
            EvaMessage="Checkpoint recuperado: ambos completaron el Tutorial.";hud.ShowComplete();return true;
        }
        public void Restart(){if(!loading){loading=true;SceneManager.LoadSceneAsync("Tutorial");}}
        public void MainMenu(){if(!loading){loading=true;SceneManager.LoadSceneAsync("MainMenu");}}
        public void NextSector(){if(!loading&&Application.CanStreamedLevelBeLoaded("Acceso")){loading=true;SceneManager.LoadSceneAsync("Acceso");}}
        AudioClip Tone(float frequency,float duration)
        {const int rate=22050;var samples=new float[(int)(rate*duration)];for(int i=0;i<samples.Length;i++)samples[i]=.12f*Mathf.Sin(2*Mathf.PI*frequency*i/rate)*Mathf.Sin(Mathf.PI*i/samples.Length);var clip=AudioClip.Create("Tutorial feedback",samples.Length,1,rate,false);clip.SetData(samples,0);return clip;}
        void Play(bool error){sound.PlayOneShot(error?errorTone:chime);}
        void OnDestroy(){if(white)Destroy(white);if(chime)Destroy(chime);if(errorTone)Destroy(errorTone);}
    }
}
