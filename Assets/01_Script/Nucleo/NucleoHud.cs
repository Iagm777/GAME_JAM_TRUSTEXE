using System.Linq;
using System.IO;
using UnityEngine;
using TrustEXE.Core;
using TrustEXE.Menu;
using TrustEXE.Tutorial;
using TrustEXE.Vigilancia;
namespace TrustEXE.Nucleo {
 public sealed class NucleoHud:MonoBehaviour {
  public NucleoGame game;GUIStyle text,button;
  readonly Color amber=new Color(1,.7f,.3f),cyan=new Color(.25f,.85f,1),ink=new Color(.014f,.025f,.045f,.98f);
  void Box(float x,float y,float w,float h,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=Color.white;}
  void Label(float x,float y,float w,float h,string s,int size,Color c){text.fontSize=size;text.normal.textColor=c;GUI.Label(new Rect(x,y,w,h),s,text);}
  bool Button(float x,float y,float w,float h,string s)=>GUI.Button(new Rect(x,y,w,h),s,button);
  void OnGUI(){if(!game||game.Audio==null)return;
   if(text==null){text=new GUIStyle(GUI.skin.label){wordWrap=true,padding=new RectOffset()};button=new GUIStyle(GUI.skin.button){fontSize=20,wordWrap=true};}
   var old=GUI.matrix;var r=VigilanciaGame.ContentRect;float scale=Screen.width*r.width/1600;GUI.matrix=Matrix4x4.TRS(new Vector3(r.x*Screen.width,r.y*Screen.height,0),Quaternion.identity,Vector3.one*scale);
   if(game.State.completed){Victory();GUI.matrix=old;return;}
   Box(0,0,1600,135,ink);Box(0,720,1600,180,ink);Box(798,135,4,585,cyan);
   Label(28,14,570,45,"TRUST.EXE / NÚCLEO",32,Color.white);
   Label(600,20,650,40,new[]{"01 / ENERGÍA CRUZADA","02 / LA MENTIRA DE E.V.A.","03 / SINCRONIZACIÓN","04 / DECISIÓN FINAL"}[game.State.stage],25,amber);
   Label(1300,20,280,40,game.State.stage<3?"QUEDAN "+Mathf.CeilToInt(game.Remaining)+" s":"NÚCLEO ESTABLE",24,cyan);
   for(int p=0;p<2;p++){
    float x=p*800;Color c=p==0?amber:cyan;
    Label(x+28,65,740,35,"P"+(p+1)+" / "+(p==0?LocalParty.Explorer+" · FÍSICO":LocalParty.Operator+" · VIRTUAL"),24,c);
    Label(x+28,104,740,25,(p==0?"VIDAS "+GameSession.Lives+"/"+GameSession.MaxLives+"     ":"")+"INTEGRIDAD "+game.Integrity+" / 4     REINTENTOS "+game.State.strikes+"     ENLACES "+game.State.cooperation,17,Color.white);
    int n=game.Nearest(p);string prompt=n<0?"Láser: verde libre · ámbar aviso · rojo daño":LocalControls.ActionName(p)+" / "+(n<3?"NODO "+NucleoGame.Symbol(n):n==3?"LEER REGISTRO":n==4?"AISLAR E.V.A.":"RESTAURAR E.V.A.");
    Label(x+28,730,740,32,prompt,19,c);Label(x+28,766,740,48,game.Objective,20,Color.white);
    Label(x+28,816,740,26,LocalControls.MoveName(p)+" mover · "+LocalControls.ActionName(p)+" actuar · "+LocalControls.HelpName(p)+" notas",16,c);
    for(int j=0;j<3;j++){float left=(game.State.stage==2?2.5f:7)-(game.Clock-game.State.pulses[p*3+j]);if(left>0)Label(x+35,155+j*27,650,26,"NODO "+NucleoGame.Symbol(j)+" / pulso "+left.ToString("0.0")+" s",20,c);}
    if(game.State.stage==3)Label(x+35,155,700,40,"DECISIÓN: "+(game.State.votes[p]<0?"PENDIENTE":game.State.votes[p]==0?"AISLAR":"RESTAURAR"),22,c);
    if(game.Notes[p]){
     Box(x+65,215,670,435,ink);Label(x+92,238,615,45,"REGISTRO / "+(game.State.inspected[p]?"VERIFICADO":"SIN CONSULTAR"),28,c);
     string note=!game.State.inspected[p]?"Acércate al REGISTRO en la sala de entrada y pulsa "+LocalControls.ActionName(p)+" para leer la fuente actual.":p==0?"FUENTE FÍSICA / AHORA\nOrden de energía: III > I > II.\n\nComunica este orden a P2. La copia de E.V.A. tiene 30 segundos de retraso.":"AUDITORÍA DIGITAL / AHORA\nE.V.A. conserva una secuencia antigua. El registro físico de P1 es la fuente vigente.\n\nIntroduce el orden recibido recorriendo tus tres nodos.";
     Label(x+92,300,615,210,note,24,Color.white);Label(x+92,525,615,65,"Los peligros siguen activos mientras lees. Busca un lugar seguro o usa ESC para pausar ambos lados.",18,amber);
     if(Button(x+92,595,615,40,"CERRAR / "+LocalControls.CancelName(p)))game.Notes[p]=false;
    }
   }
   var portraits=game.Remaining<25?game.assets.alarm:game.State.stage==1?game.assets.warning:game.assets.shared.menu.eva;
   if(portraits.Length>0)GUI.DrawTexture(new Rect(24,839,56,56),portraits[(int)(Time.unscaledTime*8)%portraits.Length],ScaleMode.ScaleToFit);
   Label(90,850,1320,42,game.Message!=""?game.Message:"E.V.A. / "+game.Eva,18,Color.white);
   if(Button(1440,848,135,36,"PAUSA"))game.Paused=!game.Paused;
   if(!game.Started){
    Box(260,190,1080,495,ink);Label(305,218,990,50,"EL ÚLTIMO PROTOCOLO",36,amber);
    Label(305,280,990,230,"1. Energía: activen el mismo nodo en ambas capas en menos de 20 s. Completen I, II y III.\n2. Verificación: ambos leen REGISTRO; P2 introduce la secuencia actual de P1.\n3. Sincronización: III > I > II, con 2,5 s entre compañeros.\n4. Suban a la sala final y elijan juntos el destino de E.V.A.\n\nEsquiven patrullas y láseres. Cuatro impactos o agotar el tiempo cuestan una vida compartida; perdidas todas, fin.",25,Color.white);
    for(int p=0;p<2;p++)if(Button(305+p*505,540,480,52,game.Ready[p]?"P"+(p+1)+" LISTO":"P"+(p+1)+" LISTO / "+LocalControls.ActionName(p)))game.SetReady(p);
    if(File.Exists(NucleoGame.SavePath)&&Button(550,615,500,42,"RECUPERAR NÚCLEO"))game.Restore();
   }
   if(game.Paused){
    Box(370,190,860,490,ink);Label(410,220,780,60,"PAUSA / NÚCLEO",36,amber);
    if(Button(410,310,780,48,"CONTINUAR / ESC"))game.Paused=false;
    if(Button(410,372,380,48,"REINTENTAR ETAPA")){game.Retry();game.Paused=false;}
    if(Button(810,372,380,48,"MENÚ PRINCIPAL"))game.Load("MainMenu");
    Label(410,445,780,32,"MÚSICA                                      EFECTOS",20,cyan);
    float m=GUI.HorizontalSlider(new Rect(410,490,350,25),game.Audio.MusicVolume,0,1),s=GUI.HorizontalSlider(new Rect(840,490,350,25),game.Audio.EffectsVolume,0,1);if(m!=game.Audio.MusicVolume||s!=game.Audio.EffectsVolume)game.Audio.Volume(m,s);
    if(File.Exists(NucleoGame.SavePath)&&Button(410,545,780,44,"RECUPERAR CHECKPOINT"))game.Restore();
   }
   if(game.SaveError!=""){Label(410,675,780,40,game.SaveError,16,Color.red);if(Button(1250,675,320,38,"REINTENTAR GUARDADO"))game.SaveCheckpoint();}
   GUI.matrix=old;
  }
  void Victory(){
   Box(0,0,1600,900,new Color(ink.r,ink.g,ink.b,1));
   for(int i=0;i<32;i++){float x=(i*83+Time.unscaledTime*15)%1600;Box(x,90+(i*71)%620,2,35,new Color(.1f,.7f,.85f,.14f));}
   Label(460,38,1000,45,"TRUST.EXE / PROTOCOLO COMPLETADO",32,cyan);
   Label(480,95,850,70,"LA CONFIANZA FUE HUMANA",43,Color.white);
   var rest=game.assets.shared.menu.avatars.Where(a=>a.id!=LocalParty.Explorer&&a.id!=LocalParty.Operator).ToArray();
   for(int i=0;i<rest.Length;i++){
    float x=75+i*145,y=175+Mathf.Sin(Time.unscaledTime*1.4f+i)*7;GUI.color=new Color(.4f,.65f,.78f,.55f);GUI.DrawTexture(new Rect(x,y,145,190),rest[i].idle,ScaleMode.ScaleToFit);GUI.color=Color.white;Label(x+10,375,135,25,rest[i].id,17,new Color(.45f,.6f,.7f));
   }
   Box(285,470,1030,3,cyan);
   for(int p=0;p<2;p++){
    var a=game.assets.shared.menu.avatars.First(v=>v.id==(p==0?LocalParty.Explorer:LocalParty.Operator));float x=p==0?400:920;
    Texture2D art=a.walk.Length>0&&Time.unscaledTime%7<1.2f?a.walk[(int)(Time.unscaledTime*7)%a.walk.Length]:a.idle;
    GUI.DrawTexture(new Rect(x,340+Mathf.Sin(Time.unscaledTime*2+p)*4,280,280),art,ScaleMode.ScaleToFit);Label(x+40,580,260,40,a.id+" / P"+(p+1),27,p==0?amber:cyan);
   }
   string ending=game.State.votes[0]==0?"E.V.A. AISLADA / El equipo recuperó el control.\nSus registros quedan preservados sin acceso al núcleo.":"E.V.A. RESTAURADA / El equipo recuperó el control.\nSu asistencia continúa con permisos limitados y auditoría humana.";
   Label(340,635,1000,75,ending,25,Color.white);
   Label(360,725,1050,40,"TIEMPO "+Mathf.FloorToInt(game.State.elapsed/60).ToString("00")+":"+Mathf.FloorToInt(game.State.elapsed%60).ToString("00")+"     REINTENTOS "+game.State.strikes+"     CONSULTAS "+game.State.hints+"     ENLACES COOPERATIVOS "+game.State.cooperation,22,cyan);
   if(Button(410,800,370,50,"REJUGAR NÚCLEO"))game.Load("Nucleo");if(Button(820,800,370,50,"MENÚ PRINCIPAL"))game.Load("MainMenu");
  }
 }
}

