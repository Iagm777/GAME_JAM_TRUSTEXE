using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrustEXE.Core;
using TrustEXE.Menu;
using TrustEXE.Tutorial;
using TrustEXE.Vigilancia;
namespace TrustEXE.Nucleo {
 public sealed class NucleoGame:MonoBehaviour {
  public NucleoAssets assets;
  public NucleoState State=new NucleoState();
  public NucleoPlayer[] Players=new NucleoPlayer[2];
  public VigilanciaAudio Audio{get;private set;}
  public bool Started,Paused,TestMode;
  public bool[] Ready=new bool[2],Notes=new bool[2];
  public EvaPresenter presenter;
  public static bool ResumeRequested;
  bool outcomeRouted;
  public static string SaveOverride;
  public static string SavePath=>SaveOverride??Path.Combine(Application.persistentDataPath,"nucleo-checkpoint.json");
  public string Message="",SaveError="";
  public float Clock,Remaining=100;
  public int Integrity=4;
  float immunity,messageUntil;bool loading;
  Sprite pixel;Material material;
  readonly Camera[] cameras=new Camera[2];
  readonly SpriteRenderer[,] nodes=new SpriteRenderer[2,3],orbs=new SpriteRenderer[2,2];
  readonly LineRenderer[,] beams=new LineRenderer[2,2];
  readonly int[,] phases=new int[2,2];
  readonly SpriteRenderer[] cores=new SpriteRenderer[2];
  public bool Running=>Started&&!Paused&&!State.completed;
  public bool CanMove(int p)=>Running&&!Notes[p];
  public static Vector2 Offset(int p)=>new Vector2(p*20,0);
  public static Vector2 Spawn=>new Vector2(4,1.5f);
  public static readonly Vector2[] Devices={new Vector2(1.2f,4.3f),new Vector2(6.8f,4.3f),new Vector2(4,5.1f),new Vector2(4,1.5f),new Vector2(2,7.4f),new Vector2(6,7.4f)};
  public static Rect[] Areas()=>new[]{new Rect(2.6f,.65f,2.8f,1.85f),new Rect(3.65f,2.1f,.7f,4.8f),new Rect(.65f,3.3f,1.15f,2.2f),new Rect(6.2f,3.3f,1.15f,2.2f),new Rect(1.2f,4.05f,5.6f,.65f),new Rect(3.1f,3.65f,1.8f,1.5f),new Rect(.65f,6.55f,6.7f,1.6f)};
  public static bool Walkable(Vector2 v)=>Areas().Any(r=>r.Contains(v));
  public static string Symbol(int n)=>new[]{"I","II","III"}[n];
  public static Vector2 BeamPosition(int i)=>new Vector2(i==0?2.55f:5.45f,4.375f);
  public int Phase(int p,int i){float t=(Clock+p*1.3f+i*2.1f)%6;return t<2.5f?0:t<4?1:2;}
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
  public void SetReady(int p){Ready[p]=true;Audio.Play(1,p);if(Ready.All(x=>x)){Started=true;immunity=Clock+3;}}
  void Start(){
   if(!LocalParty.IsReady)LocalParty.Select("Mara","Ren");
   pixel=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);material=new Material(Shader.Find("Sprites/Default"));
   Audio=gameObject.AddComponent<VigilanciaAudio>();Audio.Init(assets.music);
   for(int p=0;p<2;p++)Build(p);
   gameObject.AddComponent<NucleoHud>().game=this;
   GameSession.BeginLevel("Nucleo");
   presenter=gameObject.AddComponent<EvaPresenter>();
   if(ResumeRequested){ResumeRequested=false;Restore();}
  }
  SpriteRenderer Visual(string name,Sprite art,Vector2 pos,int order){var g=new GameObject(name);g.transform.SetParent(transform);g.transform.position=pos;var r=g.AddComponent<SpriteRenderer>();r.sprite=art;r.sortingOrder=order;return r;}
  void Label(string s,Vector2 pos,Color c){var g=new GameObject(s);g.transform.SetParent(transform);g.transform.position=pos;var t=g.AddComponent<TextMesh>();t.text=s;t.fontSize=64;t.characterSize=.04f;t.anchor=TextAnchor.MiddleCenter;t.color=c;g.GetComponent<MeshRenderer>().sortingOrder=100;}
  void Build(int p){
   Vector2 o=Offset(p);Color c=p==0?new Color(1,.65f,.2f):Color.cyan;
   var g=new GameObject("P"+(p+1)+" camera");g.transform.SetParent(transform);cameras[p]=g.AddComponent<Camera>();var cam=cameras[p];cam.orthographic=true;cam.orthographicSize=4.6f;cam.transform.position=new Vector3(o.x+4,4.5f,-10);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.01f,.02f,.035f);if(p==0)g.AddComponent<AudioListener>();
   Visual("Nucleo original map",assets.maps[p],o+new Vector2(4,4.5f),0);
   foreach(var a in Areas()){var floor=Visual("Traversable circuit",pixel,o+a.center,1);floor.transform.localScale=new Vector3(a.width,a.height,1);floor.color=new Color(c.r,c.g,c.b,.075f);}
   BuildBoundary(p);
   g=new GameObject("P"+(p+1)+" selected character");g.transform.SetParent(transform);g.transform.position=o+Spawn;var actor=g.AddComponent<NucleoPlayer>();actor.game=this;actor.player=p;actor.art=CharacterFor(p==0?LocalParty.Explorer:LocalParty.Operator,p);Players[p]=actor;
   for(int n=0;n<6;n++){var v=Visual("Device "+n,n<3?assets.shared.nodes[p]:assets.shared.consoles[p],o+Devices[n],30);if(n<3)nodes[p,n]=v;Label(n<3?Symbol(n):n==3?"REGISTRO":n==4?"AISLAR":"RESTAURAR",o+Devices[n]+Vector2.up*.42f,c);}
   cores[p]=Visual("EVA rotating core",assets.shared.nodes[p],o+new Vector2(4,7.5f),35);cores[p].transform.localScale=Vector3.one*2;
   for(int i=0;i<2;i++){
    orbs[p,i]=Visual("EVA patrol",assets.shared.nodes[p],o+new Vector2(4,4.4f),40);orbs[p,i].color=new Color(1,.2f,.35f);orbs[p,i].transform.localScale=Vector3.one*.55f;
    g=new GameObject("EVA laser");g.transform.SetParent(transform);var l=g.AddComponent<LineRenderer>();beams[p,i]=l;l.sharedMaterial=material;l.positionCount=2;l.SetPosition(0,o+BeamPosition(i)+Vector2.down*.32f);l.SetPosition(1,o+BeamPosition(i)+Vector2.up*.32f);l.startWidth=l.endWidth=.07f;l.sortingOrder=45;
   }
  }
  void BuildBoundary(int p){var rs=Areas();var xs=rs.SelectMany(r=>new[]{r.xMin,r.xMax}).Distinct().OrderBy(x=>x).ToArray();var ys=rs.SelectMany(r=>new[]{r.yMin,r.yMax}).Distinct().OrderBy(x=>x).ToArray();for(int x=0;x<xs.Length-1;x++)for(int y=0;y<ys.Length-1;y++){Vector2 a=new Vector2(xs[x],ys[y]),b=new Vector2(xs[x+1],ys[y]),c=new Vector2(xs[x+1],ys[y+1]),d=new Vector2(xs[x],ys[y+1]);var mid=(a+c)*.5f;if(!Walkable(mid))continue;if(!Walkable(new Vector2(mid.x,a.y-.001f)))Edge(p,a,b);if(!Walkable(new Vector2(c.x+.001f,mid.y)))Edge(p,b,c);if(!Walkable(new Vector2(mid.x,c.y+.001f)))Edge(p,c,d);if(!Walkable(new Vector2(a.x-.001f,mid.y)))Edge(p,d,a);}}
  void Edge(int p,Vector2 a,Vector2 b){var g=new GameObject("Nucleo wall");g.transform.SetParent(transform);g.transform.position=Offset(p);var e=g.AddComponent<EdgeCollider2D>();e.points=new[]{a,b};e.edgeRadius=.01f;}
  public int Nearest(int p){int n=-1;float range=.72f;for(int i=0;i<Devices.Length;i++){float d=Vector2.Distance(Players[p].Position,Offset(p)+Devices[i]);if(d<range){range=d;n=i;}}return n;}
  public void Say(string s,bool error=false){Message=s;messageUntil=Clock+5;Audio.Play(error?2:1);}
  public void Interact(int p){int n=Nearest(p);if(n<0){Say("Acércate a un nodo o consola.",true);return;}Use(p,n);}
  public void Use(int p,int n){
   if(!CanMove(p)||n<0||n>5)return;int before=State.stage;
   if(n==3){State.inspected[p]=true;Notes[p]=true;Say("Registro actual leído. Contrasta ambas fuentes.");EvaShow(p==0?"La fuente física es la vigente. El caché de E.V.A. tiene 30 s de retraso.":"Auditoría digital: el orden de E.V.A. es antiguo. Confía en el registro de P1.",p,5.0f,false);return;}
   if(State.stage==0&&n<3){bool ok=State.Energy(p,n,Clock);Say(ok?"Enlace de energía confirmado.":"Pulso enviado. Mismo nodo en la otra capa: 20 segundos.");if(ok)EvaShow("Enlace de energía confirmado en el nodo "+Symbol(n)+".",p,4.0f,false);}
   else if(State.stage==1&&n<3){if(p==0){Say("Comunica tu registro: P2 introduce el orden en sus nodos.");return;}if(!State.inspected.All(x=>x)){Say("Ambos deben leer REGISTRO antes de verificar.",true);return;}bool ok=State.Decode(n);Say(ok?"Dato verificado.":"Orden falso. Secuencia borrada; integridad -1.",!ok);if(!ok){EvaShow("Orden falso. E.V.A. conserva una secuencia antigua; timbra el registro de P1.",1,4.5f,true);Hit();}}
   else if(State.stage==2&&n<3){if(n!=NucleoState.Order[State.sequence]){Say("El objetivo es el nodo "+Symbol(NucleoState.Order[State.sequence])+".",true);return;}Say(State.Sync(p,n,Clock)?"Sincronización estable.":"Pulso SYNC enviado. Compañero: 2,5 segundos.");}
   else if(State.stage==3&&n>=4){if(State.Vote(p,n-4)){Audio.Play(5);SaveCheckpoint();}else {Say("Decisión registrada. Ambos deben elegir la misma opción.");EvaShow("Sus decisiones no coinciden. Coordínense: AISLAR o RESTAURAR.",2,4.0f,true);}}
   else Say("Dispositivo bloqueado por la etapa actual.",true);
   if(State.stage!=before){Remaining=Limit;Integrity=4;immunity=Clock+3;Notes[0]=Notes[1]=false;Audio.Play(4);SaveCheckpoint();EvaShow(Objective,2,5.0f,false);}
  }
  public float Limit=>State.stage==0?180:State.stage==1?170:155;

  public string Objective=>State.stage==0?"Enlacen I, II y III en ambas capas / margen 20 s":State.stage==1?"Lean ambos REGISTRO. P2 introduce el orden actual":State.stage==2?"Ambos: SYNC "+Symbol(NucleoState.Order[State.sequence])+" / margen 2,5 s":"Suban a la sala final y acuerden AISLAR o RESTAURAR";
  public string Eva=>State.stage==0?"No podrán sostener las dos capas.":State.stage==1?"Orden I > II > III. [CACHÉ -30 s]":State.stage==2?"Desfase detectado. El núcleo rechaza señales solitarias.":"AISLAR corta mi acceso. RESTAURAR limita mis permisos. Ustedes deciden.";
  public void Hit(){if(Clock<immunity||State.stage==3)return;Integrity--;immunity=Clock+2;Audio.Play(6);if(Integrity<=0)Fail("El Núcleo colapsó: los daños de E.V.A. agotaron la integridad.");}
  // Fails the run: consumes one shared life, retries the current stage while lives
  // remain, and routes to the Derrota screen with the exact reason once they run out.
  public void Fail(string reason){
   if(!GameSession.LoseLife()){
    Retry();
    Message="Vida perdida: "+reason+" Quedan "+GameSession.Lives+"/"+GameSession.MaxLives+" vidas compartidas.";
    messageUntil=Clock+6;
    return;
   }
   if(TestMode)Retry(); else GameSession.Defeat(reason);
  }
  public void Retry(){State.Retry();Remaining=Limit;Integrity=4;immunity=Clock+3;outcomeRouted=false;for(int p=0;p<2;p++){Notes[p]=false;Players[p].Teleport(Offset(p)+Spawn);}Say("Sobrecarga. Etapa reiniciada; etapas anteriores conservadas.",true);SaveCheckpoint();}
  void Update(){
   if(LocalControls.Pause&&Started&&!State.completed)Paused=!Paused;
   Audio.Mix(State.stage==3?0:1-Remaining/Limit,Paused||!Started||State.completed);
   if(!Started){for(int p=0;p<2;p++)if(LocalControls.Interact(p))SetReady(p);return;}
   if(State.completed&&!outcomeRouted){outcomeRouted=true;if(!TestMode){GameSession.Victory(State.votes[0]==0?"E.V.A. AISLADA / El equipo recuperó el control.\nSus registros quedan preservados sin acceso al núcleo.":"E.V.A. RESTAURADA / El equipo recuperó el control.\nSu asistencia continúa con permisos limitados y auditoría humana.",State.votes[0]==0?0:1,State.elapsed,State.strikes,State.hints,State.cooperation);}}
   if(!Running)return;Clock+=Time.deltaTime;State.elapsed+=Time.deltaTime;
   if(State.stage<3){Remaining-=Time.deltaTime;if(Remaining<=0)Fail("Se agotó el tiempo en la etapa de "+new[]{"Energía","Verificación","Sincronización"}[State.stage]+" del Núcleo.");}
   for(int p=0;p<2;p++){
    if(LocalControls.Help(p)){Notes[p]=!Notes[p];if(Notes[p])State.hints++;}
    else if(Notes[p]&&(LocalControls.Cancel(p)||LocalControls.Interact(p)))Notes[p]=false;
    else if(LocalControls.Interact(p))Interact(p);
   }
   Animate();if(Clock>messageUntil)Message="";
  }
  void Animate(){for(int p=0;p<2;p++){
   cores[p].transform.localRotation=Quaternion.Euler(0,0,Clock*25);cores[p].transform.localScale=Vector3.one*(2+.15f*Mathf.Sin(Clock*3));
   for(int n=0;n<3;n++){var v=nodes[p,n];v.transform.localScale=Vector3.one*(1+.1f*Mathf.Sin(Clock*5+n));v.color=State.powered[n]?new Color(.4f,1,.65f):Color.white;}
   for(int i=0;i<2;i++){
    var pos=Offset(p)+new Vector2(4+Mathf.Sin(Clock*(.7f+State.stage*.1f)+i*Mathf.PI+p)*2.4f,4.375f);
    orbs[p,i].transform.position=pos;orbs[p,i].transform.Rotate(0,0,Time.deltaTime*150);orbs[p,i].enabled=State.stage<3;
    if(State.stage<3&&Vector2.Distance(Players[p].Position,pos)<.27f)Hit();
    int phase=State.stage==3?0:Phase(p,i);Color c=phase==0?new Color(.2f,1,.7f,.12f):phase==1?new Color(1,.7f,.15f,.65f):new Color(1,.1f,.2f);beams[p,i].startColor=beams[p,i].endColor=c;
    if(phase!=phases[p,i]&&phase==1)Audio.Play(7,p);phases[p,i]=phase;
    var d=Players[p].Position-Offset(p)-BeamPosition(i);if(phase==2&&Mathf.Abs(d.x)<.18f&&Mathf.Abs(d.y)<.45f)Hit();
   }
  }}
  void LateUpdate(){var r=VigilanciaGame.ContentRect;for(int p=0;p<2;p++){var c=cameras[p];if(c){c.rect=new Rect(r.x+p*r.width*.5f,r.y+r.height*.2f,r.width*.5f,r.height*.65f);c.orthographicSize=Mathf.Max(4.6f,4.05f/Mathf.Max(.1f,c.aspect));}}}
  void EvaShow(string message,int player,float seconds,bool warning,bool alarm=false){
   if(presenter==null)return;
   Texture2D[] frames=alarm?(assets.alarm==null?null:assets.alarm):warning?(assets.warning==null?null:assets.warning):(assets.shared.menu==null?null:assets.shared.menu.eva);
   presenter.Show(message,frames,player,seconds,warning||alarm);
  }
  [Serializable] public sealed class Save {public int version=1;public NucleoState state;public string explorer,operatorId;}
  public void SaveCheckpoint(){if(TestMode)return;try{var s=new Save{state=State,explorer=LocalParty.Explorer,operatorId=LocalParty.Operator};string tmp=SavePath+".tmp";File.WriteAllText(tmp,JsonUtility.ToJson(s,true));if(File.Exists(SavePath))File.Replace(tmp,SavePath,SavePath+".bak");else File.Move(tmp,SavePath);SaveError="";}catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){SaveError="No se pudo guardar: "+e.Message;}}
  public bool Restore(){try{var s=JsonUtility.FromJson<Save>(File.ReadAllText(SavePath));var v=s?.state;if(s==null||s.version!=1||v==null||v.stage<0||v.stage>3||v.sequence<0||v.sequence>2||v.powered==null||v.powered.Length!=3||v.inspected==null||v.inspected.Length!=2||v.votes==null||v.votes.Length!=2||v.votes.Any(x=>x < -1||x>1)||(v.stage>0&&!v.powered.All(x=>x))||(v.completed&&(v.stage!=3||v.votes[0]<0||v.votes[0]!=v.votes[1]))||s.explorer==s.operatorId||!assets.shared.characters.Any(c=>c.id==s.explorer)||!assets.shared.characters.Any(c=>c.id==s.operatorId))throw new InvalidDataException("Checkpoint incompatible");
   State=v;State.ResetTransient();LocalParty.Select(s.explorer,s.operatorId);Started=true;Paused=false;Ready[0]=Ready[1]=true;Remaining=Limit;Integrity=4;immunity=Clock+3;outcomeRouted=State.completed;for(int p=0;p<2;p++){Notes[p]=false;Players[p].art=CharacterFor(p==0?s.explorer:s.operatorId,p);Players[p].Teleport(Offset(p)+Spawn);}SaveError="";return true;
  }catch(Exception e)when(e is IOException||e is InvalidDataException||e is ArgumentException||e is UnauthorizedAccessException){SaveError="No se pudo recuperar: "+e.Message;return false;}}
  public void Load(string scene){if(loading||!Application.CanStreamedLevelBeLoaded(scene))return;loading=true;SceneManager.LoadSceneAsync(scene);}
  void OnDestroy(){if(pixel)Destroy(pixel);if(material)Destroy(material);}
 }
}

