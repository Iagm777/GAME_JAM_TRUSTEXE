using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrustEXE.Core;
using TrustEXE.Menu;
using TrustEXE.Tutorial;

namespace TrustEXE.Acceso
{
    public sealed class AccesoGame : MonoBehaviour
    {
        public AccesoAssets assets;
        public AccesoState State { get; private set; } = new AccesoState();
        public AccesoPlayer[] Players { get; } = new AccesoPlayer[2];
        public bool Started, Paused;
        public readonly bool[] Ready = new bool[2], AtExit = new bool[2];
        // Panels: 0 none, 1 notes, 2 code, 3 diagnostic. Only their owner's movement is blocked.
        public readonly int[] Panel = new int[2];
        public readonly string[] Message = { "", "" }, Notes = { "Busca la tarjeta de acceso.", "Activa el nodo 01 para enlazar ambas capas." };
        public readonly int[] Digits = { 1, 1, 1 };
        public int Cursor, PowerChoice;
        public string Eva = "Dos capas. Un acceso. Recuperen la credencial y establezcan el enlace.";
        public string SaveError = "";
        public float Clock { get; private set; }
        public bool HasCheckpoint => File.Exists(SavePath);
        public bool TestMode;
        public EvaPresenter presenter;
        public static bool ResumeRequested;
        public bool GatesClear(int gate) => gateAmount[gate] >= 1;
        public static Vector2 Offset(int p) => new Vector2(p * 20, 0);
        public static Vector2 LocalDevice(int index) => index == 0 ? new Vector2(1.35f, 1.6f) : index == 1 ? new Vector2(6.6f, 4.25f) : new Vector2(5.9f, 5.35f);
#if UNITY_EDITOR
        public static string ValidationSavePath;
#endif
        public static string SavePath {
            get {
#if UNITY_EDITOR
                if(!string.IsNullOrEmpty(ValidationSavePath))return ValidationSavePath;
#endif
                return Path.Combine(Application.persistentDataPath, "acceso-checkpoint.json");
            }
        }
        readonly Color[] colors = { new Color(1,.68f,.3f), new Color(.25f,.86f,1) };
        readonly SpriteRenderer[,] devices = new SpriteRenderer[2,3], gates = new SpriteRenderer[2,2];
        readonly BoxCollider2D[,] blockers = new BoxCollider2D[2,2];
        readonly SpriteRenderer[] exitViews = new SpriteRenderer[2];
        readonly float[] gateAmount = new float[2], messageUntil = new float[2];
        readonly TextMesh[] meters = new TextMesh[2];
        readonly Transform[] motes = new Transform[30];
        readonly Camera[] cameras = new Camera[2];
        readonly Vector3[] moteOrigins = new Vector3[30];
        AudioSource sfx, music;
        AudioClip accept, reject, door, foot;
        Sprite pixel;
        bool checkpointA, checkpointB, loading;
        public float MusicVolume { get; private set; }
        public float SfxVolume { get; private set; }

        void Start()
        {
            if (!LocalParty.IsReady) LocalParty.Select("Mara", "Ren");
            pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0,0,1,1), Vector2.one*.5f, 1);
            sfx = gameObject.AddComponent<AudioSource>(); sfx.playOnAwake = false;
            accept = Tone(720,.16f); reject = Tone(150,.19f); door = Tone(80,.7f); foot = Tone(110,.035f);
            MusicVolume = PlayerPrefs.GetFloat("AccesoMusic", .35f); SfxVolume = PlayerPrefs.GetFloat("AccesoSfx", .7f);
            music = gameObject.AddComponent<AudioSource>(); music.clip = assets.ambience; music.loop = true; music.volume = MusicVolume; music.Play();
            for (int p=0;p<2;p++) BuildWorld(p);
            for (int i=0;i<motes.Length;i++) {
                var pos = Offset(i%2) + new Vector2(1.1f+(i%6)*.54f,.7f+(i%11)*.69f);
                var r=Visual("Ambient particle",pixel,pos,10); r.color=new Color(colors[i%2].r,colors[i%2].g,colors[i%2].b,.35f);
                r.transform.localScale=Vector3.one*.025f; motes[i]=r.transform; moteOrigins[i]=pos;
            }
            gameObject.AddComponent<AccesoHud>().game = this;
            GameSession.BeginLevel("Acceso");
            presenter = gameObject.AddComponent<EvaPresenter>();
            Log("START", "No inversion until supply is powered");
            if(ResumeRequested){ResumeRequested=false;Restore();}
        }
        void BuildWorld(int p)
        {
            var o=Offset(p); var camObject=new GameObject("P"+(p+1)+" / independent camera");camObject.transform.SetParent(transform);
            var cam=camObject.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=4.65f;
            cameras[p]=cam;
            cam.transform.position=new Vector3(o.x+4,4.5f,-10);cam.rect=new Rect(p*.5f,.2f,.5f,.65f);cam.backgroundColor=new Color(.015f,.025f,.04f);
            cam.clearFlags=CameraClearFlags.SolidColor;if(p==0)camObject.AddComponent<AudioListener>();
            Visual("Acceso map P"+(p+1),assets.maps[p],o+new Vector2(4,4.5f),0);
            // Authored against the supplied 1600x900 map. Grilles are floor, pipes remain outside the walkable contour.
            var wall=new GameObject("Manually traced room boundary P"+(p+1));wall.transform.SetParent(transform);wall.transform.position=o;
            var edge=wall.AddComponent<EdgeCollider2D>();edge.edgeRadius=.03f;
            edge.points=new[]{new Vector2(.72f,.55f),new Vector2(4.35f,.55f),new Vector2(4.35f,2.65f),new Vector2(3.13f,2.65f),new Vector2(3.13f,4.12f),new Vector2(5.5f,4.12f),new Vector2(5.5f,3.35f),new Vector2(7.35f,3.35f),new Vector2(7.35f,6.12f),new Vector2(5.65f,6.12f),new Vector2(5.65f,5.1f),new Vector2(3.13f,5.1f),new Vector2(3.13f,6.52f),new Vector2(4.38f,6.52f),new Vector2(4.38f,8.38f),new Vector2(.75f,8.38f),new Vector2(.75f,6.52f),new Vector2(2.2f,6.52f),new Vector2(2.2f,5.1f),new Vector2(1.97f,5.1f),new Vector2(1.97f,4.12f),new Vector2(2.2f,4.12f),new Vector2(2.2f,2.65f),new Vector2(.72f,2.65f),new Vector2(.72f,.55f)};
            var actor=new GameObject("P"+(p+1)+" / "+(p==0?LocalParty.Explorer:LocalParty.Operator));actor.transform.SetParent(transform);actor.transform.position=o+new Vector2(2.65f,1.05f);
            var a=actor.AddComponent<AccesoPlayer>();a.game=this;a.player=p;a.art=CharacterFor(p==0?LocalParty.Explorer:LocalParty.Operator,p);Players[p]=a;
            var shadow=Visual("Ground marker",pixel,actor.transform.position,12);shadow.transform.SetParent(actor.transform,true);shadow.transform.localPosition=new Vector3(0,-.29f,0);shadow.transform.localScale=new Vector3(.36f,.055f,1);shadow.color=colors[p];
            for(int i=0;i<3;i++) {
                var sprite=i==0?assets.shared.nodes[p]:i==1?assets.shared.consoles[p]:assets.shared.switchesOff[p];
                devices[p,i]=Visual(DeviceName(p,i),sprite,o+LocalDevice(i),35);
                if(p==0&&i==0){devices[p,i].sprite=pixel;devices[p,i].transform.localScale=new Vector3(.42f,.28f,1);var stripe=Visual("Card stripe",pixel,o+LocalDevice(i)+new Vector2(0,.05f),36);stripe.transform.localScale=new Vector3(.36f,.055f,1);stripe.color=colors[p];}
                var box=devices[p,i].gameObject.AddComponent<BoxCollider2D>();box.size=new Vector2(.38f,.24f);
                Label(DeviceName(p,i),o+LocalDevice(i)+new Vector2(0,.48f),.041f,colors[p]);
            }
            meters[p]=Label(p==0?"0 V / SIN ENERGÍA":"CANAL 02 / SIN ENLACE",o+LocalDevice(1)+new Vector2(0,-.48f),.032f,Color.white);
            for(int g=0;g<2;g++) {
                Vector2 pos=o+new Vector2(2.665f,g==0?3.35f:6.12f);
                gates[p,g]=Visual("Door "+(g==0?"A":"B")+" P"+(p+1),assets.shared.doorsClosed[p],pos,45);
                var block=new GameObject("Door collider");block.transform.SetParent(transform);block.transform.position=pos;blockers[p,g]=block.AddComponent<BoxCollider2D>();blockers[p,g].size=new Vector2(1.03f,.25f);
                Label(g==0?"A":"B",pos+new Vector2(-.62f,0),.06f,colors[p]);
            }
            exitViews[p]=Visual("Exit pad",pixel,o+new Vector2(2.65f,7.65f),3);exitViews[p].transform.localScale=new Vector3(1.2f,.6f,1);
            Label(p==0?"SALIDA / VIGILANCIA":"NODO 03 / SALIDA",o+new Vector2(2.65f,8.13f),.045f,colors[p]);
            Label("RECEPCIÓN",o+new Vector2(2.65f,2.3f),.045f,colors[p]);
        }
        SpriteRenderer Visual(string name,Sprite sprite,Vector2 pos,int order) {
            var go=new GameObject(name);go.transform.SetParent(transform);go.transform.position=pos;var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;return r;
        }
        TextMesh Label(string text,Vector2 pos,float size,Color color) {
            var go=new GameObject(text);go.transform.SetParent(transform);go.transform.position=pos;var t=go.AddComponent<TextMesh>();t.text=text;t.fontSize=60;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.color=color;go.GetComponent<MeshRenderer>().sortingOrder=110;return t;
        }
        public string DeviceName(int p,int i) => i==0?(p==0?"TARJETA / CLAVE":"NODO 01 / ENLACE"):i==1?(p==0?"SUMINISTRO":"NODO 02 / DIAGNÓSTICO"):"SYNC / PUERTA B";
        public int Nearest(int p) {
            int found=-1;float distance=.83f;for(int i=0;i<3;i++){float d=Vector2.Distance(Players[p].Position,Offset(p)+LocalDevice(i));if(d<distance){distance=d;found=i;}}return found;
        }
        public bool CanMove(int p) => Started&&!Paused&&!State.completed&&Panel[p]==0;
        public void SetReady(int p) { Ready[p]=true;Play(accept);if(Ready[0]&&Ready[1])Started=true; }
        public void Close(int p) { Panel[p]=0;Play(accept); }
        public void Pause() { if(Started&&!State.completed){Paused=!Paused;music.Pause();if(!Paused)music.UnPause();} }
        void LateUpdate(){foreach(var cam in cameras)if(cam)cam.orthographicSize=Mathf.Max(4.65f,4.1f/Mathf.Max(.1f,cam.aspect));}
        void Update()
        {
            if(!Started){for(int p=0;p<2;p++)if(LocalControls.Interact(p))SetReady(p);return;}
            if(LocalControls.Pause)Pause();if(Paused||State.completed)return;
            Clock+=Time.deltaTime;
            for(int p=0;p<2;p++) {
                if(LocalControls.Cancel(p)&&Panel[p]>0)Close(p);
                else if(LocalControls.Help(p)){Panel[p]=Panel[p]==0?1:0;Play(accept);}
                else if(Panel[p]==2) {
                    Vector2 nav=LocalControls.Navigate(p);if(nav.x!=0)Cursor=(Cursor+(nav.x>0?1:2))%3;
                    if(nav.y!=0){Digits[Cursor]=(Digits[Cursor]-1+(nav.y>0?1:2))%3+1;Play(accept);}
                    if(LocalControls.Interact(p))SubmitCode();
                } else if(Panel[p]==3) {
                    if(LocalControls.Navigate(p)!=Vector2.zero){PowerChoice=1-PowerChoice;Play(accept);}
                    if(LocalControls.Interact(p))SubmitPower();
                } else if(LocalControls.Interact(p)){if(Panel[p]==1)Close(p);else Interact(p);}
                if(Clock>messageUntil[p])Message[p]="";
                AtExit[p]=GatesClear(1)&&Vector2.Distance(Players[p].Position,Offset(p)+new Vector2(2.65f,7.65f))<.65f;
                exitViews[p].color=AtExit[p]?new Color(.3f,1,.5f,.65f):new Color(colors[p].r,colors[p].g,colors[p].b,State.doorB?.22f+.1f*Mathf.Sin(Clock*3):.06f);
                for(int i=0;i<3;i++)devices[p,i].color=Nearest(p)==i?Color.Lerp(Color.white,colors[p],.25f+.2f*Mathf.Sin(Clock*5)):Color.white;
            }
            AnimateGates();
            for(int i=0;i<motes.Length;i++)motes[i].position=moteOrigins[i]+new Vector3(Mathf.Sin(Clock*.4f+i)*.1f,Mathf.Sin(Clock*.7f+i)*.12f,0);
            if(State.DoorA&&!checkpointA){checkpointA=true;SaveCheckpoint(1);}
            if(State.doorB&&!checkpointB){checkpointB=true;SaveCheckpoint(2);}
            if(AtExit[0]&&AtExit[1]&&!State.completed){State.completed=true;if(!SaveCheckpoint(3))State.completed=false;else{Eva="Acceso completado. Ambas identidades verificadas. Siguiente sector: Vigilancia.";Play(accept);}}
        }
        public void Interact(int p)
        {
            int i=Nearest(p);if(i<0){Say(p,"Acércate a un dispositivo iluminado.",true);return;}
            if(i==0) {
                if(p==0){State.card=true;Notes[0]="TARJETA A-312\nCódigo físico: 3 · 1 · 2. Compártelo con P2.\nLa lectura del voltímetro físico es la fuente fiable. E.V.A. puede invertir las etiquetas de energía.";}
                else {State.linked=true;Notes[1]="NODO 01 / ENLACE ESTABLE\nPide la clave de la tarjeta a P1.\nProtocolo: contrastar el voltímetro físico antes de confirmar energía. E.V.A. solo cambia etiquetas, nunca la tensión real.";}
                Panel[p]=1;Say(p,p==0?"Tarjeta recuperada · código guardado en Q":"Enlace establecido · espera la tarjeta de P1",false);
                EvaShow(p==0?"Tarjeta A-312 recuperada. Código físico: 3 · 1 · 2. Compártelo con P2.":"Enlace del nodo 01 estable. Espera la clave de P1 para continuar.",p,4.5f,false);
                if(State.DoorA)Eva="Enlace establecido. Puerta A liberada en ambas capas.";
                Log("CREDENTIAL", "card="+State.card+" linked="+State.linked);return;
            }
            if(!State.DoorA){Say(p,"Primero recuperen la tarjeta y activen el nodo 01.",true);return;}
            if(i==1) {
                if(p==0){State.powered=true;meters[0].text="120 V / ENCENDIDO";meters[0].color=new Color(.5f,1,.6f);Notes[0]="TARJETA A-312 / código 3 · 1 · 2\nSUMINISTRO: 120 V / ENCENDIDO.\nEl panel de E.V.A. dice SIN ENERGÍA: la etiqueta está invertida. P2 debe declarar el estado físico real.";Eva="Diagnóstico de E.V.A.: suministro SIN ENERGÍA. No intenten abrir la puerta B.";Panel[0]=1;Log("INVERSION_POWER", "true=ON, EVA=OFF");Say(p,"Suministro activado · lectura física: 120 V",false);}
                else {Panel[1]=!State.decoded?2:3;Say(p,State.decoded?"Contrasta la etiqueta de E.V.A. con P1":"Solicita a P1 los tres dígitos de la tarjeta",false);
                if(p==0)EvaShow("E.V.A. miente: el panel dice SIN ENERGÍA. Tu voltímetro manda.",0,5.0f,true);}return;
            }
            if(!State.CanSync){Say(p,"Faltan suministro, código o diagnóstico contrastado.",true);return;}
            if(State.doorB){Say(p,"Puerta B abierta · ambos a SALIDA",false);return;}
            bool opened=State.Pulse(p,Clock);devices[p,2].sprite=assets.shared.switchesOn[p];
            Say(p,opened?"¡Sincronización completa! Puerta B liberada":"Pulso enviado · tu compañero tiene 6 segundos",false);
            if(opened){Eva="Contradicción detectada. Su verificación tiene prioridad. Acceso al sistema interno autorizado.";EvaShow("Acceso al sistema interno autorizado. Ambos pueden cruzar la puerta B.",2,5.0f,false);Log("DOOR_B","both pulses within 6 seconds");}
        }
        public void SubmitCode() {
            if(State.Code(Digits[0],Digits[1],Digits[2])){Panel[1]=3;Say(1,"Código válido. Ahora verifica el suministro con P1.",false);EvaShow("Código válido. Ahora contrasta el estado físico con P1.",1,4.0f,false);Log("CODE","accepted");}
            else {Say(1,"Código incorrecto. Pregunta a P1 por la tarjeta; puedes reintentar.",true);EvaShow("Código incorrecto. Pide a P1 los tres dígitos de la tarjeta.",1,4.0f,true);}
        }
        public void SubmitPower() {
            if(State.Verify(PowerChoice==1)){Panel[1]=0;Notes[1]="Código validado. INVERSION_POWER detectada: E.V.A. muestra SIN ENERGÍA, pero P1 confirma 120 V / ENCENDIDO.\nAmbos activen SYNC con menos de 6 segundos de diferencia.";Eva="Verificación independiente aceptada. Sincronicen los dos controles de la puerta B.";EvaShow("Contradicción detectada. Su verificación tiene prioridad.",2,6.0f,false);Say(1,"Mentira detectada · protocolo de apertura habilitado",false);Log("VERIFY","physical ON overrides EVA OFF");}
            else Say(1,!State.powered?"P1 aún debe activar y medir el suministro.":"La medición física contradice esa respuesta. Consulta los 120 V de P1.",true);
        }
        void AnimateGates() {
            for(int g=0;g<2;g++) {
                bool open=g==0?State.DoorA:State.doorB;
                if(open&&gateAmount[g]==0)Play(door);
                gateAmount[g]=Mathf.MoveTowards(gateAmount[g],open?1:0,Time.deltaTime/ .75f);
                for(int p=0;p<2;p++){gates[p,g].transform.position=Offset(p)+new Vector2(2.665f+Mathf.SmoothStep(0,.8f,gateAmount[g]),g==0?3.35f:6.12f);blockers[p,g].enabled=gateAmount[g]<1;gates[p,g].color=new Color(1,1,1,1-gateAmount[g]*.7f);}
            }
        }
        // Resolves an avatar by id against the shared catalog, falling back to the
        // per-role default with a loud log instead of throwing. A stale shared asset
        // must never freeze the sector: the picked character is used whenever present.
        TutorialAssets.Character CharacterFor(string id,int player)
        {
            var art=assets.shared&&assets.shared.characters!=null?assets.shared.characters.FirstOrDefault(c=>c.id==id):null;
            if(art!=null)return art;
            if(assets.shared&&assets.shared.characters!=null)
            {
                art=assets.shared.characters.FirstOrDefault(c=>c.id==(player==0?"Mara":"Ren"));
                if(art==null)art=assets.shared.characters[Mathf.Clamp(player,0,assets.shared.characters.Length-1)];
            }
            Debug.LogError("TRUST.EXE: avatar \""+id+"\" no está en el catálogo compartido; se usó "+(art==null?"ninguno":art.id)+".");
            return art;
        }
        void Say(int p,string value,bool error){Message[p]=value;messageUntil[p]=Clock+5;Play(error?reject:accept);}
        void EvaShow(string message,int player,float seconds,bool warning)
        {
            if(presenter==null)return;
            Texture2D[] frames=warning?(assets.evaWarning==null?null:assets.evaWarning):(assets.shared.menu==null?null:assets.shared.menu.eva);
            presenter.Show(message,frames,player,seconds,warning);
        }
        void Log(string fact,string data){Debug.Log("[Acceso / "+fact+"] "+data);}
        public void Footstep(int p){sfx.panStereo=p==0?-.55f:.55f;sfx.PlayOneShot(foot,SfxVolume*.22f);}
        void Play(AudioClip clip){sfx.panStereo=0;sfx.PlayOneShot(clip,SfxVolume);}
        public void Volume(float m,float s){MusicVolume=m;SfxVolume=s;music.volume=m;PlayerPrefs.SetFloat("AccesoMusic",m);PlayerPrefs.SetFloat("AccesoSfx",s);}
        AudioClip Tone(float frequency,float duration){const int rate=22050;var a=new float[(int)(rate*duration)];for(int i=0;i<a.Length;i++){float t=(float)i/rate;a[i]=.15f*Mathf.Sin(2*Mathf.PI*(frequency*t+35*t*t))*Mathf.Sin(Mathf.PI*i/a.Length);}var c=AudioClip.Create("Acceso feedback",a.Length,1,rate,false);c.SetData(a,0);return c;}
        [Serializable] public sealed class Save {public int version=1,stage;public string explorer,operatorId;public AccesoState state;public Vector2 p1,p2;}
        public bool SaveCheckpoint(int stage) {
            if(TestMode)return true;
            try {var save=new Save{stage=stage,explorer=LocalParty.Explorer,operatorId=LocalParty.Operator,state=State,p1=Players[0].Position,p2=Players[1].Position};string temp=SavePath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(save,true));if(File.Exists(SavePath))File.Replace(temp,SavePath,SavePath+".bak");else File.Move(temp,SavePath);SaveError="";return true;}
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){SaveError="No se pudo guardar. Reintenta desde la pausa. "+e.Message;Log("SAVE_ERROR",e.Message);return false;}
        }
        public bool Restore() {
            try {
                var s=JsonUtility.FromJson<Save>(File.ReadAllText(SavePath));
                if(s==null||s.version!=1||s.stage<1||s.stage>3||s.state==null||!s.state.DoorA||s.explorer==s.operatorId||!assets.shared.characters.Any(a=>a.id==s.explorer)||!assets.shared.characters.Any(a=>a.id==s.operatorId)||(s.stage>=2&&(!s.state.doorB||!s.state.CanSync)))throw new InvalidDataException("Checkpoint no compatible.");
                State=s.state;State.ResetTimers();State.completed=s.stage==3;LocalParty.Select(s.explorer,s.operatorId);Started=true;Paused=false;Ready[0]=Ready[1]=true;Panel[0]=Panel[1]=0;
                checkpointA=true;checkpointB=s.stage>=2;gateAmount[0]=1;gateAmount[1]=State.doorB?1:0;
                // Safe authored spawns avoid malformed/obsolete saved coordinates trapping an actor.
                for(int p=0;p<2;p++){Players[p].art=CharacterFor(p==0?s.explorer:s.operatorId,p);Players[p].Teleport(Offset(p)+new Vector2(2.65f,s.stage>=2?6.95f:4.55f));}
                Notes[0]="Tarjeta A-312 / clave 3 · 1 · 2.\n"+(State.powered?"Voltímetro físico: 120 V / ENCENDIDO.":"Activa el suministro de la sala derecha.");Notes[1]="Protocolo: contrastar el voltímetro físico de P1; E.V.A. puede invertir etiquetas de energía.";
                meters[0].text=State.powered?"120 V / ENCENDIDO":"0 V / SIN ENERGÍA";
                Eva=State.verified?"Contradicción ya verificada. Continúen hacia la salida.":State.powered?"Diagnóstico de E.V.A.: suministro SIN ENERGÍA.":"Checkpoint recuperado. Puerta A abierta.";
                SaveError="";music.UnPause();AnimateGates();return true;
            }catch(Exception e)when(e is IOException||e is InvalidDataException||e is UnauthorizedAccessException||e is ArgumentException){SaveError="No se pudo recuperar el checkpoint: "+e.Message;return false;}
        }
        public void RetrySave(){if(State.DoorA)SaveCheckpoint(State.completed?3:State.doorB?2:1);}
        public void Load(string scene){if(loading||!Application.CanStreamedLevelBeLoaded(scene))return;loading=true;SceneManager.LoadSceneAsync(scene);}
        void OnDestroy(){foreach(var c in new[]{accept,reject,door,foot})if(c)Destroy(c);if(pixel)Destroy(pixel);}
    }
}
