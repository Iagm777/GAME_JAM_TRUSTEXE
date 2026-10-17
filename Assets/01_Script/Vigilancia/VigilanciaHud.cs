using UnityEngine;
using TrustEXE.Tutorial;
using TrustEXE.Menu;
namespace TrustEXE.Vigilancia {
 public sealed class VigilanciaHud:MonoBehaviour {
  public VigilanciaGame game;GUIStyle text,button;
  readonly Color amber=new Color(1,.72f,.38f),cyan=new Color(.3f,.87f,1),ink=new Color(.018f,.032f,.05f,.98f);
  void Box(float x,float y,float w,float h,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=Color.white;}
  void Label(float x,float y,float w,float h,string s,int size,Color c,bool bold=false){text.fontSize=size;text.normal.textColor=c;text.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;GUI.Label(new Rect(x,y,w,h),s,text);}
  bool Button(float x,float y,float w,float h,string s)=>GUI.Button(new Rect(x,y,w,h),s,button);
  void Card(float x,string title,Color color){Box(x+65,194,670,470,ink);Box(x+65,194,670,3,color);Label(x+95,218,610,50,title,28,color,true);}
  void OnGUI(){
   if(!game||game.Audio==null)return;
   if(text==null){text=new GUIStyle(GUI.skin.label){wordWrap=true,padding=new RectOffset()};button=new GUIStyle(GUI.skin.button){fontSize=19,wordWrap=true};}
   var old=GUI.matrix;var content=VigilanciaGame.ContentRect;float scale=Screen.width*content.width/1600f;GUI.matrix=Matrix4x4.TRS(new Vector3(content.x*Screen.width,content.y*Screen.height,0),Quaternion.identity,new Vector3(scale,scale,1));
   Box(0,0,1600,135,ink);Box(0,720,1600,180,ink);Box(798,135,4,585,cyan);
   Label(26,12,540,45,"TRUST.EXE / VIGILANCIA",29,Color.white,true);
   Label(610,21,600,35,game.State.lift?"03 / ASCENSOR RESTAURADO":game.State.sequence==3?"02 / SINCRONIZACIÓN FINAL":"01 / OJOS EN AMBAS CAPAS",21,amber,true);
   Label(1300,23,270,35,"SECTOR 02   "+Mathf.FloorToInt(game.Clock/60).ToString("00")+":"+Mathf.FloorToInt(game.Clock%60).ToString("00"),20,cyan);
   for(int p=0;p<2;p++){
    float x=p*800;Color c=p==0?amber:cyan;
    Box(x+26,62,748,2,c);Label(x+28,73,735,32,"P"+(p+1)+" / "+(p==0?LocalParty.Explorer+" · FÍSICO":LocalParty.Operator+" · VIRTUAL"),21,c,true);
    string status=p==0?"DETECCIÓN "+Mathf.RoundToInt(game.Alert*100)+"%    ALARMAS "+game.Alarms:"SECUENCIA "+game.State.sequence+" / 3    RELÉS "+(game.State.relays[0]?"I ":"")+(game.State.relays[1]?"II ":"")+(game.State.relays[2]?"III":"");
    Label(x+28,107,740,24,status,15,Color.white);
    if(p==0){Box(400,114,370,5,new Color(.15f,.2f,.25f));Box(400,114,370*game.Alert,5,new Color(1,.25f,.12f));}
    int n=game.Nearest(p);string prompt=game.AtExit[p]?"EN ASCENSOR / ESPERANDO AL COMPAÑERO":n>=0?LocalControls.ActionName(p)+" / "+game.DeviceName(p,n):"LÁSER: TENUE = LIBRE · ÁMBAR = AVISO · ROJO = BLOQUEADO";
    Label(x+27,727,746,36,game.Message[p]!=""?game.Message[p]:prompt,17,c,true);
    Label(x+27,766,746,38,game.Objective(p),18,Color.white);
    Label(x+27,805,746,26,LocalControls.MoveName(p)+" mover   "+LocalControls.ActionName(p)+" actuar   "+LocalControls.HelpName(p)+" notas   ESC pausa",15,new Color(.65f,.75f,.8f));
    if(p==1)for(int i=0;i<4;i++)if(game.CameraRemaining(i)>0)Label(x+24+(i%2)*385,144+(i/2)*27,370,27,"CAM "+(i+1)+" ANULADA / "+game.CameraRemaining(i).ToString("0.0")+" s",17,cyan,true);
    float pulse=p==0?game.State.pulse0:game.State.pulse1;if(!game.State.lift&&game.Clock-pulse<5)Label(x+170,681,500,30,"SYNC ACTIVO / "+(5-game.Clock+pulse).ToString("0.0")+" s",22,c,true);
    if(game.Notes[p]){Card(x,p==0?"REGISTRO FÍSICO / FUENTE ACTUAL":"PROTOCOLO / VERIFICAR LA FECHA",c);
     string note=p==0?(game.State.clue?"Registro actual: II > I > III.\n\nCada cámara necesita anulación de P2 antes de sellar su relé. Los relés sellados desactivan las barreras de ambas capas.\n\nComunica el orden del registro a tu compañero.":"Busca REGISTRO en la sala IV, abajo a la izquierda.\n\nLos conos de cámara elevan la detección. Retírate para reducirla. P2 puede anular cada cámara durante 14 segundos."):"E.V.A. utiliza una copia con 30 s de retraso. Su orden NO es el registro actual. Pide a P1 que consulte el registro físico.\n\nCAM anula una cámara; SW introduce un paso del orden. Un error reinicia solo la secuencia.\n\nTras validar tres pasos, ambos deben pulsar SYNC.";
     Label(x+95,288,610,270,note,24,Color.white);if(Button(x+95,585,610,46,"CERRAR / "+LocalControls.CancelName(p)))game.Notes[p]=false;
    }
   }
   Box(24,840,1552,48,new Color(.065f,.095f,.13f));
   var portraits=game.Alert>.4f?game.assets.evaAlarm:game.assets.shared.menu.eva;
   if(portraits!=null&&portraits.Length>0)GUI.DrawTexture(new Rect(28,830,64,64),portraits[(int)(Time.unscaledTime*7)%portraits.Length],ScaleMode.ScaleToFit);
   Label(102,846,1320,40,"E.V.A. / "+game.Eva,17,Color.white);if(Button(1460,845,100,34,"PAUSA"))game.Paused=!game.Paused;
   if(!game.Started){for(int p=0;p<2;p++){float x=p*800;Color c=p==0?amber:cyan;Card(x,p==0?"EVITA LA VIGILANCIA":"CONTROLA SUS OJOS",c);Label(x+95,290,610,260,p==0?"1. Lee el registro físico en la sala IV.\n2. Pide a P2 que anule cada cámara.\n3. Sella los relés y comparte el orden.\n4. Sincronicen el ascensor.\n\nEvita conos y láseres. La alarma conserva tus avances.":"1. Recorre los cuatro nodos CAM.\n2. Anula cámaras durante 14 segundos.\n3. Espera los relés de P1 y acciona los tres SW en el orden del registro físico.\n4. Sincroniza con P1.\n\nE.V.A. muestra datos antiguos: revisa la fecha.",24,Color.white);if(Button(x+95,585,610,46,game.Ready[p]?"LISTO / ESPERANDO":"ESTOY LISTO / "+LocalControls.ActionName(p)))game.SetReady(p);}
    if(game.HasCheckpoint&&Button(535,672,530,38,"RECUPERAR CHECKPOINT DE VIGILANCIA"))game.Restore();
   }
   if(game.Paused||game.State.completed){Box(365,176,870,530,ink);Label(410,207,780,55,game.State.completed?"VIGILANCIA SUPERADA":"PAUSA / AMBAS CAPAS",35,amber,true);Label(410,275,780,83,game.State.completed?"Ascensor restaurado y ambas identidades verificadas.\n"+(Application.CanStreamedLevelBeLoaded("Nucleo")?"El Núcleo está listo para entrar.":"El siguiente sector, Núcleo, aún no está implementado."):"El tiempo, los sensores y los controles están detenidos.",23,Color.white);
    if(!game.State.completed&&Button(410,365,780,42,"CONTINUAR / ESC"))game.Paused=false;
    Label(410,425,220,28,"MÚSICA / EFECTOS",18,cyan);float m=GUI.HorizontalSlider(new Rect(650,434,240,25),game.Audio.MusicVolume,0,1),s=GUI.HorizontalSlider(new Rect(930,434,240,25),game.Audio.EffectsVolume,0,1);if(m!=game.Audio.MusicVolume||s!=game.Audio.EffectsVolume)game.Audio.Volume(m,s);
    if(game.HasCheckpoint&&Button(410,475,380,45,"RECUPERAR CHECKPOINT"))game.Restore();if(Button(810,475,380,45,"REINICIAR VIGILANCIA"))game.Load("Vigilancia");
    if(Button(410,535,380,45,"MENÚ PRINCIPAL"))game.Load("MainMenu");if(game.State.completed&&Application.CanStreamedLevelBeLoaded("Nucleo")&&Button(810,535,380,45,"ENTRAR AL NÚCLEO"))game.Load("Nucleo");
    if(game.SaveError!=""&&Button(410,600,780,42,"REINTENTAR GUARDADO"))game.SaveCheckpoint();
   }
   if(game.SaveError!="")Label(400,674,800,45,game.SaveError,17,new Color(1,.4f,.3f));
   GUI.matrix=old;
  }
 }
}
