using UnityEngine;
using TrustEXE.Core;
using TrustEXE.Menu;

namespace TrustEXE.Tutorial
{
    // Each panel belongs to one player. IMGUI never consumes keyboard movement.
    public sealed class TutorialHud : MonoBehaviour
    {
        TutorialGame game;
        readonly Color amber = new Color(1,.70f,.33f), cyan = new Color(.32f,.87f,1);
        GUIStyle label, title, button;
        float volume;
        bool hasCheckpoint;
        public void Initialize(TutorialGame owner) { game=owner; volume=PlayerPrefs.GetFloat("TutorialVolume",.55f);hasCheckpoint=TutorialCheckpoint.TryRead(out _); }
        public void Refresh() {}
        public void ShowPause(bool value) {}
        public void ShowComplete() {}
        void Box(Rect r, Color c) { GUI.color=c; GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white; }
        void Text(Rect r,string value,int size,Color color,bool bold=false)
        {label.fontSize=size;label.normal.textColor=color;label.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;GUI.Label(r,value,label);}
        bool Button(Rect r,string value) => GUI.Button(r,value,button);
        void OnGUI()
        {
            if(!game||game.assets==null)return;
            if(label==null) {label=new GUIStyle(GUI.skin.label){wordWrap=true,padding=new RectOffset(0,0,0,0)};title=new GUIStyle(label);button=new GUIStyle(GUI.skin.button){fontSize=20,wordWrap=true,padding=new RectOffset(12,12,8,8)};}
            var old=GUI.matrix;
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1600f,Screen.height/900f,1));
            Box(new Rect(0,0,1600,135),new Color(.025f,.035f,.055f));
            Box(new Rect(0,720,1600,180),new Color(.025f,.035f,.055f));
            Box(new Rect(797,135,6,585),new Color(.60f,.72f,.76f));
            Text(new Rect(28,16,420,45),"TRUST.EXE  /  SECTOR 00",29,Color.white,true);
            Text(new Rect(1170,22,400,35),"TUTORIAL · ENLACE DE CONFIANZA",18,cyan,true);
            string[] lessons={"01  EXPLORA","02  COMPARTE","03  SINCRONIZA","04  ESCAPA","ENLACE COMPLETO"};
            Text(new Rect(560,20,530,40),lessons[(int)game.Progress.Lesson],25,amber,true);
            Text(new Rect(1102,22,130,32),"VIDAS "+GameSession.Lives+"/"+GameSession.MaxLives,18,cyan,true);
            for(int p=0;p<2;p++)
            {
                float x=p*800;var c=p==0?amber:cyan;
                Box(new Rect(x+24,73,752,2),c);
                Text(new Rect(x+28,88,720,36),"P"+(p+1)+"   "+(p==0?LocalParty.Explorer+" / MUNDO FÍSICO":LocalParty.Operator+" / MUNDO VIRTUAL"),23,c,true);
                Text(new Rect(x+28,728,740,34),game.Feedback[p]!=""?game.Feedback[p]:game.Prompt[p],19,c,true);
                Text(new Rect(x+28,769,740,33),LocalControls.MoveName(p)+"  MOVER    "+LocalControls.ActionName(p)+"  ACTUAR    "+LocalControls.HelpName(p)+"  PISTA",17,Color.white);
                if(game.Progress.DoorOpen)Text(new Rect(x+28,688,740,30),game.Progress.atExit[p]?"EN SALIDA · ESPERANDO AL COMPAÑERO":"SIGUE EL PASILLO HASTA SALIDA",17,c,true);
                if(game.Reading[p])
                {
                    Box(new Rect(x+66,206,668,440),new Color(.025f,.045f,.07f,.98f));
                    Box(new Rect(x+66,206,668,4),c);
                    Text(new Rect(x+94,237,612,310),game.Popup[p],23,Color.white);
                    if(Button(new Rect(x+94,568,612,48),"CERRAR · "+LocalControls.ActionName(p)+" / "+LocalControls.CancelName(p)))game.ClosePopup(p);
                }
            }
            Box(new Rect(24,813,1552,65),new Color(.075f,.10f,.13f));
            if(game.assets.menu&&game.assets.menu.eva.Length>0)GUI.DrawTexture(new Rect(28,808,72,72),game.assets.menu.eva[(int)(Time.unscaledTime*7)%game.assets.menu.eva.Length],ScaleMode.ScaleToFit);
            Text(new Rect(110,824,1340,48),"E.V.A.  /  "+game.EvaMessage,19,Color.white);
            if(Button(new Rect(1460,828,102,36),"PAUSA"))game.TogglePause();
            if(!game.Started)
            {
                if(hasCheckpoint&&Button(new Rect(530,654,540,48),"RECUPERAR CHECKPOINT COMPLETADO"))game.RestoreCheckpoint();
                for(int p=0;p<2;p++)
                {
                    float x=p*800;var c=p==0?amber:cyan;
                    Box(new Rect(x+70,212,660,430),new Color(.025f,.04f,.06f,.97f));
                    Text(new Rect(x+105,244,590,45),p==0?"EXPLORA LO REAL":"DESCIFRA LO VIRTUAL",31,c,true);
                    Text(new Rect(x+105,310,590,160),"Dos personas. Un teclado.\n\nRecorre tu sala, inspecciona tu "+(p==0?"terminal":"nodo")+" y comparte la pista en voz alta. Las dos acciones abren el camino.",24,Color.white);
                    Text(new Rect(x+105,478,590,50),LocalControls.MoveName(p)+" para caminar · "+LocalControls.ActionName(p)+" para interactuar",21,c);
                    if(Button(new Rect(x+105,553,590,52),game.IntroReady[p]?"LISTO · ESPERANDO AL COMPAÑERO":"ESTOY LISTO · "+LocalControls.ActionName(p)))game.Ready(p);
                }
            }
            if(game.Paused||game.Progress.completed)
            {
                Box(new Rect(370,188,860,490),new Color(.025f,.04f,.06f,.99f));
                Text(new Rect(418,223,764,58),game.Progress.completed?"¡UN GRAN EQUIPO!":"PAUSA",40,amber,true);
                Text(new Rect(418,294,764,70),game.Progress.completed?"Tutorial completado por ambos. Checkpoint guardado.\nContinúen juntos al sector Acceso.":"Tomen un respiro. Los dos mundos están en pausa.",24,Color.white);
                if(!game.Progress.completed&&Button(new Rect(418,374,764,46),"CONTINUAR · ESC / E / ENTER"))game.TogglePause();
                Text(new Rect(418,437,250,30),"VOLUMEN DE MÚSICA",18,cyan);
                float next=GUI.HorizontalSlider(new Rect(690,449,490,24),volume,0,1);
                if(next!=volume){volume=next;game.SetMusicVolume(volume);PlayerPrefs.SetFloat("TutorialVolume",volume);}
                if(Button(new Rect(418,500,370,48),"REPETIR TUTORIAL"))game.Restart();
                if(Button(new Rect(812,500,370,48),"VOLVER AL MENÚ"))game.MainMenu();
                if(game.Progress.completed&&Application.CanStreamedLevelBeLoaded("Acceso")&&Button(new Rect(418,568,764,48),"ENTRAR A ACCESO"))game.NextSector();
                Text(new Rect(418,623,764,25),"ESC pausa · Los paneles de pistas solo detienen a su jugador",17,Color.gray);
            }
            GUI.matrix=old;
        }
    }
}
