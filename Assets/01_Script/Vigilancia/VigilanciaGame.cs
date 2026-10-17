using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrustEXE.Core;
using TrustEXE.Menu;
using TrustEXE.Tutorial;
namespace TrustEXE.Vigilancia {
 public sealed class VigilanciaGame:MonoBehaviour {
  public VigilanciaAssets assets;
  public VigilanciaState State=new VigilanciaState();
  public VigilanciaPlayer[] Players=new VigilanciaPlayer[2];
  public VigilanciaAudio Audio{get;private set;}
  public bool Started,Paused,TestMode; public static bool ResumeRequested;
  public bool[] Ready=new bool[2],Notes=new bool[2],AtExit=new bool[2];
  public EvaPresenter presenter;
  public float Clock,Alert; public int Alarms;
  public string[] Message={"",""};float[] messageUntil=new float[2];
  public string SaveError="";
  public bool Running=>Started&&!Paused&&!State.completed;
  public bool CanMove(int p)=>Running&&!Notes[p];
  public static string SaveOverride;
  public static string SavePath=>SaveOverride??Path.Combine(Application.persistentDataPath,"vigilancia-checkpoint.json");
  public bool HasCheckpoint=>File.Exists(SavePath);
  public static Vector2 Offset(int p)=>new Vector2(p*20,0);
  public static readonly Vector2[] Rooms={new Vector2(2,7.25f),new Vector2(6,2.2f),new Vector2(6,7.25f),new Vector2(2,2.2f)};
  public static Vector2 Spawn=>new Vector2(4,.85f);
  public static Vector2 Device(int p,int i){
   if(i<4)return Rooms[i]+new Vector2(p==0?-.35f:.35f,0);
   if(i<7)return Rooms[i-4]+new Vector2(-.5f,-.55f);
   return new Vector2(p==0?3.45f:4.55f,4.55f);
  }
  readonly float[] disabledUntil=new float[4];
  readonly SpriteRenderer[,] devices=new SpriteRenderer[2,8];
  readonly SpriteRenderer[] sensors=new SpriteRenderer[4];
  readonly Mesh[] cones=new Mesh[4];readonly MeshRenderer[] coneViews=new MeshRenderer[4];
  readonly Vector3[] coneVertices=new Vector3[18];readonly Color[] coneColors=new Color[18];readonly int[] coneTriangles=new int[48];
  readonly LineRenderer[,] lasers=new LineRenderer[2,3];readonly BoxCollider2D[,] laserBlocks=new BoxCollider2D[2,3];
  readonly int[,] lastLaserPhase=new int[2,3];
  readonly Camera[] cameras=new Camera[2];readonly SpriteRenderer[] liftViews=new SpriteRenderer[2];
  readonly Vector2[] sensorPositions=new Vector2[4];
  readonly Color amber=new Color(1,.65f,.24f),cyan=new Color(.2f,.8f,1);
  Sprite pixel;Material lineMaterial;float immunity,liftProgress;int checkpoint;bool loading;
  public float CameraRemaining(int i)=>Mathf.Max(0,disabledUntil[i]-Clock);
  public bool CameraDisabled(int i)=>CameraRemaining(i)>0||State.lift;
  public string Eva=>State.lift?"Ascensor autorizado. Ambas identidades deben ocupar la plataforma.":State.sequence==3?"Integridad restaurada. Sincronicen los controles centrales en 5 segundos.":"CACHÉ -30 s / Orden recomendado: I > II > III. [Información sin sincronizar]";
  void Start(){
   if(!LocalParty.IsReady)LocalParty.Select("Mara","Ren");
   pixel=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
   lineMaterial=new Material(Shader.Find("Sprites/Default"));
   Audio=gameObject.AddComponent<VigilanciaAudio>();Audio.Init(assets.music);
   for(int j=0;j<16;j++){coneTriangles[j*3]=0;coneTriangles[j*3+1]=j+1;coneTriangles[j*3+2]=j+2;}
   for(int p=0;p<2;p++)BuildWorld(p);
   for(int i=0;i<4;i++){
    sensorPositions[i]=Rooms[i]+new Vector2(.65f,.7f);
    sensors[i]=Visual("CAM "+(i+1),assets.cameras[0],sensorPositions[i],40);
    var go=new GameObject("Visible sensor cone "+i);go.transform.SetParent(transform);cones[i]=new Mesh();go.AddComponent<MeshFilter>().mesh=cones[i];coneViews[i]=go.AddComponent<MeshRenderer>();coneViews[i].sharedMaterial=lineMaterial;coneViews[i].sortingOrder=8;
   }
   gameObject.AddComponent<VigilanciaHud>().game=this;
   GameSession.BeginLevel("Vigilancia");
   presenter=gameObject.AddComponent<EvaPresenter>();
   if(ResumeRequested){ResumeRequested=false;Restore();}
  }
  SpriteRenderer Visual(string name,Sprite sprite,Vector2 pos,int order){var go=new GameObject(name);go.transform.SetParent(transform);go.transform.position=pos;var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;return r;}
  void Slab(string name,Vector2 pos,Vector2 size,Color color,int order){var r=Visual(name,pixel,pos,order);r.transform.localScale=new Vector3(size.x,size.y,1);r.color=color;}
  void Label(string value,Vector2 pos,float size,Color color){var go=new GameObject(value);go.transform.SetParent(transform);go.transform.position=pos;var t=go.AddComponent<TextMesh>();t.text=value;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.color=color;go.GetComponent<MeshRenderer>().sortingOrder=100;}
  public static Rect[] WalkAreas()=>new[]{
   new Rect(.75f,1.2f,2.4f,1.95f),new Rect(4.8f,1.2f,2.55f,1.95f),new Rect(.75f,6.25f,2.4f,2.05f),new Rect(4.8f,6.25f,2.55f,2.05f),
   new Rect(3.65f,.5f,.7f,8),new Rect(2,1.9f,4,.65f),new Rect(2,6.9f,4,.65f),new Rect(.8f,4.3f,6.5f,.65f),new Rect(3.2f,4.05f,1.6f,1.15f)};
  public static bool Walkable(Vector2 v)=>WalkAreas().Any(r=>r.Contains(v));
  void BuildWorld(int p){
   Vector2 o=Offset(p);Color c=p==0?amber:cyan;
   var camGo=new GameObject("P"+(p+1)+" independent camera");camGo.transform.SetParent(transform);var cam=camGo.AddComponent<Camera>();cameras[p]=cam;cam.orthographic=true;cam.orthographicSize=4.6f;cam.rect=new Rect(p*.5f,.2f,.5f,.65f);cam.transform.position=new Vector3(o.x+4,4.5f,-10);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.01f,.02f,.035f);if(p==0)camGo.AddComponent<AudioListener>();
   Visual("Vigilancia supplied map",assets.maps[p],o+new Vector2(4,4.5f),0);
   // Four visible bridges replace painted wall sections; their traversable bounds match WalkAreas exactly.
   foreach(float y in new[]{2.225f,7.225f})foreach(float x in new[]{3.35f,4.6f}){
    Slab("Bridge floor",o+new Vector2(x,y),new Vector2(1.05f,.65f),new Color(.055f,.08f,.1f),2);
    foreach(float dy in new[]{-.31f,.31f})Slab("Bridge rail",o+new Vector2(x,y+dy),new Vector2(1.05f,.025f),c,3);
    for(int i=0;i<6;i++)Slab("Bridge tread",o+new Vector2(x-.43f+i*.17f,y),new Vector2(.022f,.48f),new Color(c.r,c.g,c.b,.28f),3);
   }
   BuildBoundary(p);
   var go=new GameObject("P"+(p+1)+" animated avatar");go.transform.SetParent(transform);go.transform.position=o+Spawn;
   var actor=go.AddComponent<VigilanciaPlayer>();actor.game=this;actor.player=p;actor.art=CharacterFor(p==0?LocalParty.Explorer:LocalParty.Operator,p);Players[p]=actor;
   for(int i=0;i<8;i++){
    if(p==0&&i>=4&&i<7)continue;
    Sprite sprite=i==7?assets.shared.consoles[p]:i>=4?assets.shared.switchesOff[p]:i==3&&p==0?assets.shared.consoles[p]:assets.shared.nodes[p];
    devices[p,i]=Visual(DeviceName(p,i),sprite,o+Device(p,i),30);
    Label(i==7?"SYNC":i>=4?"SW "+Roman(i-4):p==0?(i==3?"REGISTRO":"RELÉ "+Roman(i)):"CAM "+(i+1),o+Device(p,i)+new Vector2(0,.38f),.035f,c);
   }
   for(int i=0;i<3;i++){
    var pos=LaserPosition(p,i);var beam=new GameObject("Pulsed laser "+i);beam.transform.SetParent(transform);var l=beam.AddComponent<LineRenderer>();lasers[p,i]=l;l.sharedMaterial=lineMaterial;l.positionCount=2;l.SetPosition(0,pos+Vector2.down*.3f);l.SetPosition(1,pos+Vector2.up*.3f);l.startWidth=l.endWidth=.055f;l.sortingOrder=42;
    var block=new GameObject("Laser collider "+i);block.transform.SetParent(transform);block.transform.position=pos;laserBlocks[p,i]=block.AddComponent<BoxCollider2D>();laserBlocks[p,i].size=new Vector2(.075f,.58f);
   }
   liftViews[p]=Visual("Lift pad",pixel,o+new Vector2(4,4.6f),4);liftViews[p].transform.localScale=new Vector3(.65f,.65f,1);liftViews[p].color=new Color(c.r,c.g,c.b,.12f);
   Label("ASCENSOR",o+new Vector2(4,5.07f),.035f,c);
   foreach(int i in Enumerable.Range(0,4))Label(i==3?"ARCHIVO / IV":"SEGURIDAD / "+Roman(i),o+Rooms[i]+new Vector2(0,-.78f),.03f,c);
  }
  public static string Roman(int i)=>new[]{"I","II","III","IV"}[i];
  public static Vector2 LaserPosition(int p,int i)=>Offset(p)+new Vector2(i==0?3.3f:4.65f,i==1?2.225f:7.225f);
  void BuildBoundary(int p){
   var rects=WalkAreas();var xs=rects.SelectMany(r=>new[]{r.xMin,r.xMax}).Distinct().OrderBy(x=>x).ToArray();var ys=rects.SelectMany(r=>new[]{r.yMin,r.yMax}).Distinct().OrderBy(y=>y).ToArray();
   for(int x=0;x<xs.Length-1;x++)for(int y=0;y<ys.Length-1;y++){
    var center=new Vector2((xs[x]+xs[x+1])/2,(ys[y]+ys[y+1])/2);if(!Walkable(center))continue;
    Vector2 a=new Vector2(xs[x],ys[y]),b=new Vector2(xs[x+1],ys[y]),c=new Vector2(xs[x+1],ys[y+1]),d=new Vector2(xs[x],ys[y+1]);
    if(!Walkable(new Vector2(center.x,a.y-.001f)))Edge(p,a,b);
    if(!Walkable(new Vector2(c.x+.001f,center.y)))Edge(p,b,c);
    if(!Walkable(new Vector2(center.x,c.y+.001f)))Edge(p,c,d);
    if(!Walkable(new Vector2(a.x-.001f,center.y)))Edge(p,d,a);
   }
  }
  void Edge(int p,Vector2 a,Vector2 b){var go=new GameObject("Authored wall");go.transform.SetParent(transform);go.transform.position=Offset(p);var e=go.AddComponent<EdgeCollider2D>();e.edgeRadius=.015f;e.points=new[]{a,b};}
  public int Nearest(int p){if(!Players[p])return -1;int best=-1;float range=.7f;for(int i=0;i<8;i++){if(p==0&&i>=4&&i<7)continue;float d=Vector2.Distance(Players[p].Position,Offset(p)+Device(p,i));if(d<range){range=d;best=i;}}return best;}
  public string DeviceName(int p,int i)=>i==7?"SINCRONIZAR ASCENSOR":i>=4?"INTERRUPTOR "+Roman(i-4):p==0?(i==3?"LEER REGISTRO FÍSICO":"SELLAR RELÉ "+Roman(i)):"ANULAR CÁMARA "+(i+1)+" / 14 s";
  // Resolves an avatar by id against the shared catalog, falling back to the
  // per-role default with a loud log instead of throwing (stale shared data must
  // never freeze the sector or swap the picked character silently).
  TutorialAssets.Character CharacterFor(string id,int player){
   var art=assets.shared&&assets.shared.characters!=null?assets.shared.characters.FirstOrDefault(c=>c.id==id):null;
   if(art!=null)return art;
   if(assets.shared&&assets.shared.characters!=null){
    art=assets.shared.characters.FirstOrDefault(c=>c.id==(player==0?"Mara":"Ren"));
    if(art==null)art=assets.shared.characters[Mathf.Clamp(player,0,assets.shared.characters.Length-1)];
   }
   Debug.LogError("TRUST.EXE: avatar \""+id+"\" no está en el catálogo compartido; se usó "+(art==null?"ninguno":art.id)+".");
   return art;
  }
  public void SetReady(int p){Ready[p]=true;Audio.Play(1,p);if(Ready[0]&&Ready[1]){Started=true;immunity=Clock+4;}}
  public void Say(int p,string s,bool error=false){Message[p]=s;messageUntil[p]=Clock+6;Audio.Play(error?2:1,p);}
  public void Interact(int p){int i=Nearest(p);if(i<0){Say(p,"Acércate a un dispositivo.",true);return;}Use(p,i);}
  public void Use(int p,int i){
   if(!CanMove(p)||i<0||i>7)return;
   if(i==7){if(State.sequence<3){Say(p,"Valida los tres interruptores antes de sincronizar.",true);return;}if(State.lift)return;if(State.Pulse(p,Clock)){Audio.Play(5,p);SaveCheckpoint();EvaShow("Ascensor autorizado. Ambas identidades deben ocupar la plataforma.",2,5.0f,false);}else Say(p,"Pulso activo: tu compañero dispone de 5 s.");return;}
   if(p==1&&i<4){disabledUntil[i]=Clock+14;Audio.Play(3,p);Say(p,"Cámara "+(i+1)+" anulada durante 14 s. Avisa a P1.");EvaShow("Cámara "+(i+1)+" anulada durante 14 segundos. P1 puede sellar su relé.",1,3.0f,false);return;}
   if(p==0&&i==3){State.clue=true;Notes[0]=true;Say(p,"Registro actual guardado: II > I > III. Consulta con Q.");EvaShow("Registro físico: II > I > III. La copia de E.V.A. tiene 30 s de retraso.",0,5.0f,false);SaveCheckpoint();return;}
   if(p==0&&i<3){
    if(State.relays[i]){Say(p,"Relé ya sellado. P2 puede activar su interruptor.");return;}
    if(!CameraDisabled(i)){Say(p,"Cámara activa: pide a P2 que anule CAM "+(i+1)+".",true);return;}
    State.relays[i]=true;Audio.Play(4,p);Say(p,"Relé "+Roman(i)+" sellado. Barrera retirada en ambas capas.");EvaShow("Relé "+Roman(i)+" sellado. Barrera retirada en ambas capas.",2,4.0f,false);SaveCheckpoint();return;
   }
   if(p==1&&i>=4&&i<7){
    if(State.sequence==3){Say(p,"Secuencia completa. Ambos al control SYNC.");return;}
    int n=i-4;
    if(!State.clue||!State.relays[n]){Say(p,!State.clue?"P1 debe consultar el registro físico.":"P1 debe sellar el relé "+Roman(n)+".",true);return;}
    bool accepted=State.Switch(n);Say(p,accepted?"Interruptor aceptado / "+State.sequence+" de 3":"Orden incorrecto: secuencia reiniciada. Contrasta el registro actual de P1.",!accepted);
    if(!accepted)EvaShow("Orden incorrecto: secuencia reiniciada. Consulta el registro ACTUAL de P1.",1,4.5f,true);
    if(!accepted){Alert=Mathf.Min(.8f,Alert+.3f);Debug.Log("[Vigilancia] EVA cached order rejected; physical order II I III");}
    SaveCheckpoint();
   }
  }
  public string Objective(int p){if(State.lift)return "Ambos al ascensor central";if(State.sequence==3)return "Activen ambos SYNC con menos de 5 s de diferencia";if(!State.clue)return p==0?"Consulta REGISTRO en la sala IV (abajo izquierda)":"Anula cámaras desde sus nodos para ayudar a P1";return p==0?"Pide anulación de cámaras y sella los tres relés":"Activa interruptores siguiendo el registro ACTUAL de P1";}
  public static Rect ContentRect { get {float aspect=(float)Screen.width/Screen.height,target=16f/9;return aspect>target?new Rect((1-target/aspect)*.5f,0,target/aspect,1):new Rect(0,(1-aspect/target)*.5f,1,aspect/target);} }
  void LateUpdate(){var r=ContentRect;for(int p=0;p<2;p++){var c=cameras[p];if(c){c.rect=new Rect(r.x+p*r.width*.5f,r.y+r.height*.2f,r.width*.5f,r.height*.65f);c.orthographicSize=Mathf.Max(4.6f,4.05f/Mathf.Max(.1f,c.aspect));}}}
  void Update(){
   if(!Started){for(int p=0;p<2;p++)if(LocalControls.Interact(p))SetReady(p);UpdateHazards(false);return;}
   if(LocalControls.Pause)Paused=!Paused;
   Audio.Mix(Alert,Paused||State.completed);if(!Running)return;
   Clock+=Time.deltaTime;
   for(int p=0;p<2;p++){
    if(LocalControls.Help(p))Notes[p]=!Notes[p];else if(Notes[p]&&(LocalControls.Cancel(p)||LocalControls.Interact(p)))Notes[p]=false;else if(LocalControls.Interact(p))Interact(p);
    if(Clock>messageUntil[p])Message[p]="";
    for(int i=0;i<8;i++)if(devices[p,i]){bool on=i<3?State.relays[i]:i>=4&&i<7?Enumerable.Range(0,State.sequence).Any(n=>VigilanciaState.Order[n]==i-4):i==7?State.lift:State.clue;devices[p,i].color=on?new Color(.5f,1,.7f):Nearest(p)==i?Color.Lerp(Color.white,p==0?amber:cyan,.3f+.15f*Mathf.Sin(Clock*5)):Color.white;if(i>=4&&i<7)devices[p,i].sprite=on?assets.shared.switchesOn[p]:assets.shared.switchesOff[p];}
   }
   UpdateHazards(true);
   liftProgress=Mathf.MoveTowards(liftProgress,State.lift?1:0,Time.deltaTime*.65f);
   for(int p=0;p<2;p++){AtExit[p]=liftProgress>=1&&Vector2.Distance(Players[p].Position,Offset(p)+new Vector2(4,4.6f))<.4f;liftViews[p].color=new Color(AtExit[p]?.3f:.15f,State.lift?1:.4f,.75f,.15f+.4f*liftProgress);liftViews[p].transform.localRotation=Quaternion.Euler(0,0,State.lift?Mathf.Sin(Clock)*5:0);}
   if(AtExit[0]&&AtExit[1]){State.completed=true;Audio.Play(5);SaveCheckpoint();}
  }
  public int LaserPhase(int p,int i)=>State.relays[i]||State.lift?0:(Clock+i*1.7f+p*1.1f)%8<3?0:(Clock+i*1.7f+p*1.1f)%8<5?1:2;
  void UpdateHazards(bool detect){
   bool seen=false;
   for(int i=0;i<4;i++){
    float angle=-135+Mathf.Sin(Clock*.65f+i)*55;Vector2 direction=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
    bool off=CameraDisabled(i);sensors[i].sprite=Alert>.25f?assets.alarmCameras[0]:assets.cameras[0];sensors[i].color=off?new Color(.3f,.6f,.7f,.6f):Color.white;sensors[i].transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Clock*.65f+i)*12);
    coneVertices[0]=sensorPositions[i];Color color=Alert>.2f?new Color(1,.15f,.08f,.24f):new Color(1,.65f,.15f,.18f);coneColors[0]=color;
    for(int j=0;j<=16;j++){float a=(angle-27+54*j/16f)*Mathf.Deg2Rad;Vector2 ray=new Vector2(Mathf.Cos(a),Mathf.Sin(a));float distance=2.1f;var hit=Physics2D.Raycast(sensorPositions[i],ray,distance);if(hit.collider&&!(hit.collider.GetComponent<VigilanciaPlayer>()))distance=hit.distance;coneVertices[j+1]=sensorPositions[i]+ray*distance;coneColors[j+1]=color;}
    cones[i].Clear();cones[i].vertices=coneVertices;cones[i].triangles=coneTriangles;cones[i].colors=coneColors;cones[i].RecalculateBounds();coneViews[i].enabled=!off;
    Vector2 delta=Players[0].Position-sensorPositions[i];
    if(detect&&!off&&Clock>immunity&&delta.magnitude<2.1f&&Vector2.Angle(delta,direction)<27){var hit=Physics2D.Raycast(sensorPositions[i],delta.normalized,delta.magnitude+.05f);if(hit.collider&&hit.collider.GetComponent<VigilanciaPlayer>()==Players[0])seen=true;}
   }
   for(int p=0;p<2;p++)for(int i=0;i<3;i++){
    int phase=LaserPhase(p,i);var color=phase==2?new Color(1,.15f,.12f):phase==1?new Color(1,.7f,.15f,.5f+.4f*Mathf.Sin(Clock*14)):new Color(.2f,.8f,.9f,.1f);
    lasers[p,i].startColor=lasers[p,i].endColor=color;
    // Never enable a solid beam through an avatar already crossing it.
    var delta=Players[p].Position-LaserPosition(p,i);bool overlaps=Mathf.Abs(delta.x)<.2f&&Mathf.Abs(delta.y)<.45f;
    laserBlocks[p,i].enabled=phase==2&&!overlaps;
    if(detect&&phase!=lastLaserPhase[p,i]&&phase==1)Audio.Play(7,p);lastLaserPhase[p,i]=phase;
    if(detect&&phase==2&&overlaps&&Clock>immunity)seen=true;
   }
   if(!detect)return;
   Alert=Mathf.MoveTowards(Alert,seen?1:0,Time.deltaTime*(seen?.52f:.22f));
   if(Alert>=1)TriggerAlarm();
  }
  public void TriggerAlarm(){
   Alarms++;Alert=0;immunity=Clock+5;State.ResetTimers();
   for(int p=0;p<2;p++){Notes[p]=false;Players[p].Teleport(Offset(p)+Spawn);Say(p,"Alarma: regreso seguro. Relés y secuencia conservados.",true);}
   Audio.Play(6);
   EvaShow("E.V.A. los ha registrado: pierden una vida compartida.",2,5.0f,true);
   if(GameSession.LoseLife()&&!TestMode){GameSession.Defeat("E.V.A. los detectó y agotó sus vidas compartidas en Vigilancia.");}
  }
  void EvaShow(string message,int player,float seconds,bool warning){
   if(presenter==null)return;
   Texture2D[] frames=warning?(assets.evaAlarm==null?null:assets.evaAlarm):(assets.shared.menu==null?null:assets.shared.menu.eva);
   presenter.Show(message,frames,player,seconds,warning);
  }
  [Serializable] public sealed class Save {public int version=1;public VigilanciaState state;public string explorer,operatorId;public int alarms;}
  public void SaveCheckpoint(){checkpoint++;if(TestMode)return;try{var data=new Save{state=State,explorer=LocalParty.Explorer,operatorId=LocalParty.Operator,alarms=Alarms};string tmp=SavePath+".tmp";File.WriteAllText(tmp,JsonUtility.ToJson(data,true));if(File.Exists(SavePath))File.Replace(tmp,SavePath,SavePath+".bak");else File.Move(tmp,SavePath);SaveError="";}catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){SaveError="No se pudo guardar: "+e.Message;}}
  public bool Restore(){try{var s=JsonUtility.FromJson<Save>(File.ReadAllText(SavePath));if(s==null||s.version!=1||s.state==null||s.state.relays==null||s.state.relays.Length!=3||s.state.sequence<0||s.state.sequence>3||s.explorer==s.operatorId||!assets.shared.characters.Any(c=>c.id==s.explorer)||!assets.shared.characters.Any(c=>c.id==s.operatorId)||(s.state.sequence>0&&(!s.state.clue||Enumerable.Range(0,s.state.sequence).Any(i=>!s.state.relays[VigilanciaState.Order[i]])))||(s.state.lift&&s.state.sequence!=3)||(s.state.completed&&!s.state.lift))throw new InvalidDataException("Checkpoint incompatible");
   State=s.state;State.ResetTimers();LocalParty.Select(s.explorer,s.operatorId);Alarms=s.alarms;Started=true;Ready[0]=Ready[1]=true;Paused=false;Alert=0;immunity=Clock+5;Array.Clear(disabledUntil,0,4);liftProgress=State.lift?1:0;
   for(int p=0;p<2;p++){Notes[p]=false;Players[p].art=CharacterFor(p==0?s.explorer:s.operatorId,p);Players[p].Teleport(Offset(p)+Spawn);}SaveError="";return true;
  }catch(Exception e)when(e is IOException||e is InvalidDataException||e is UnauthorizedAccessException||e is ArgumentException){SaveError="No se pudo recuperar: "+e.Message;return false;}}
  public void Load(string scene){if(loading||!Application.CanStreamedLevelBeLoaded(scene))return;loading=true;SceneManager.LoadSceneAsync(scene);}
  void OnDestroy(){if(pixel)Destroy(pixel);if(lineMaterial)Destroy(lineMaterial);foreach(var mesh in cones)if(mesh)Destroy(mesh);}
 }
}
