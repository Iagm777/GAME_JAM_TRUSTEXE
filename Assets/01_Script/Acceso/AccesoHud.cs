using UnityEngine;
using TrustEXE.Menu;
using TrustEXE.Tutorial;

namespace TrustEXE.Acceso
{
    public sealed class AccesoHud : MonoBehaviour
    {
        public AccesoGame game;
        GUIStyle text,button;
        readonly Color amber=new Color(1,.72f,.38f),cyan=new Color(.3f,.87f,1),ink=new Color(.018f,.032f,.05f,.98f);
        void Box(float x,float y,float w,float h,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=Color.white;}
        void Label(float x,float y,float w,float h,string s,int size,Color c,bool bold=false){text.fontSize=size;text.normal.textColor=c;text.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;GUI.Label(new Rect(x,y,w,h),s,text);}
        bool Button(float x,float y,float w,float h,string s)=>GUI.Button(new Rect(x,y,w,h),s,button);
        void Panel(float x,string title,Color color){Box(x+65,205,670,450,ink);Box(x+65,205,670,3,color);Label(x+95,232,610,48,title,27,color,true);}
        void OnGUI()
        {
            if(!game||game.assets==null)return;
            if(text==null){text=new GUIStyle(GUI.skin.label){wordWrap=true,padding=new RectOffset(0,0,0,0)};button=new GUIStyle(GUI.skin.button){fontSize=19,wordWrap=true};}
            var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1600f,Screen.height/900f,1));
            Box(0,0,1600,135,ink);Box(0,720,1600,180,ink);Box(798,135,4,585,cyan);
            Label(26,14,410,42,"TRUST.EXE / ACCESO",30,Color.white,true);
            Label(520,21,600,36,game.State.doorB?"03 / RUTA AL SISTEMA INTERNO":game.State.DoorA?"02 / NO CONFÍES SIN VERIFICAR":"01 / DOS IDENTIDADES, UNA LLAVE",21,amber,true);
            Label(1260,23,310,32,"SECTOR 01     "+Mathf.FloorToInt(game.Clock/60).ToString("00")+":"+Mathf.FloorToInt(game.Clock%60).ToString("00"),20,cyan);
            for(int p=0;p<2;p++) {
                float x=p*800;var c=p==0?amber:cyan;
                Box(x+26,65,748,2,c);
                Label(x+28,80,740,30,"P"+(p+1)+" / "+(p==0?LocalParty.Explorer+" · MUNDO FÍSICO":LocalParty.Operator+" · MUNDO VIRTUAL"),22,c,true);
                int nearest=game.Nearest(p);
                string prompt=game.AtExit[p]?"EN SALIDA / ESPERANDO AL COMPAÑERO":nearest>=0?LocalControls.ActionName(p)+"  /  "+game.DeviceName(p,nearest):"EXPLORA / ACÉRCATE A UN DISPOSITIVO";
                Label(x+27,727,748,32,game.Message[p]!=""?game.Message[p]:prompt,18,c,true);
                Label(x+27,764,748,32,game.State.Objective(p),18,Color.white);
                Label(x+27,797,748,27,LocalControls.MoveName(p)+" mover   "+LocalControls.ActionName(p)+" actuar   "+LocalControls.HelpName(p)+" pista   "+LocalControls.CancelName(p)+" cerrar",15,new Color(.7f,.77f,.83f));
                float pulse=p==0?game.State.pulseP1:game.State.pulseP2;
                if(game.State.CanSync&&!game.State.doorB&&game.Clock-pulse<6)Label(x+120,675,560,34,"PULSO ACTIVO / "+(6-game.Clock+pulse).ToString("0.0")+" s",23,c,true);
                if(game.Panel[p]==1){Panel(x,"ARCHIVO PERSONAL / P"+(p+1),c);Label(x+95,300,610,230,game.Notes[p],25,Color.white);if(Button(x+95,575,610,48,"CERRAR / "+LocalControls.ActionName(p)+" o "+LocalControls.CancelName(p)))game.Close(p);}
                if(game.Panel[p]==2){Panel(x,"NODO 02 / CÓDIGO DE ACCESO",c);Label(x+95,294,610,65,"Pide a P1 los dígitos de la tarjeta.\nIzquierda / derecha: casilla · Arriba / abajo: valor",21,Color.white);
                    for(int d=0;d<3;d++){Box(x+165+d*150,390,120,95,d==game.Cursor?new Color(.08f,.35f,.42f):new Color(.08f,.12f,.17f));Label(x+206+d*150,408,80,65,game.Digits[d].ToString(),48,c,true);}
                    if(Button(x+95,575,610,48,"VALIDAR / ENTER"))game.SubmitCode();}
                if(game.Panel[p]==3){Panel(x,"NODO 02 / CONTRASTE DE FUENTES",c);Label(x+95,292,610,95,"E.V.A.: «SIN ENERGÍA»\nNo es una medición. Pregunta a P1 qué indica el voltímetro físico.",24,Color.white);
                    for(int v=0;v<2;v++){Box(x+95,412+v*59,610,48,game.PowerChoice==v?new Color(.08f,.35f,.42f):new Color(.08f,.12f,.17f));Label(x+115,421+v*59,570,35,(game.PowerChoice==v?">  ":"    ")+(v==0?"0 V / APAGADO":"120 V / ENCENDIDO"),24,c,true);}
                    if(Button(x+95,575,610,48,"CONFIRMAR ESTADO REAL / ENTER"))game.SubmitPower();}
            }
            Box(24,836,1552,51,new Color(.065f,.095f,.13f));
            var portraits=game.State.powered&&!game.State.verified?game.assets.evaWarning:game.assets.shared.menu.eva;
            if(portraits!=null&&portraits.Length>0)GUI.DrawTexture(new Rect(28,828,64,64),portraits[(int)(Time.unscaledTime*7)%portraits.Length],ScaleMode.ScaleToFit);
            Label(102,843,1335,42,"E.V.A. / "+game.Eva,18,Color.white);
            if(Button(1460,843,100,34,"PAUSA"))game.Pause();
            if(!game.Started) {
                for(int p=0;p<2;p++){float x=p*800;var c=p==0?amber:cyan;Panel(x,p==0?"RECUPERA EL ACCESO":"RECONSTRUYE LA VERDAD",c);Label(x+95,301,610,175,p==0?"Busca la tarjeta, alimenta el sistema y comparte tus mediciones.\n\nTu compañero necesita información del mundo físico.":"Enlaza la red, introduce la clave y verifica los diagnósticos.\n\nE.V.A. puede mentir. Contrasta sus etiquetas con P1.",25,Color.white);Label(x+95,492,610,45,LocalControls.MoveName(p)+" mover · "+LocalControls.ActionName(p)+" interactuar",21,c);if(Button(x+95,565,610,48,game.Ready[p]?"LISTO / ESPERANDO AL COMPAÑERO":"ESTOY LISTO / "+LocalControls.ActionName(p)))game.SetReady(p);}
                if(game.HasCheckpoint&&Button(520,665,560,42,"CONTINUAR DESDE EL CHECKPOINT DE ACCESO"))game.Restore();
            }
            if(game.Paused||game.State.completed){Box(375,175,850,515,ink);Label(420,210,760,55,game.State.completed?"ACCESO RESTABLECIDO":"PAUSA / AMBAS CAPAS",37,amber,true);Label(420,278,760,75,game.State.completed?"Ambos llegaron a la salida. Checkpoint guardado.\nHan detectado la primera inversión de E.V.A.":"Continúa o recupera el último checkpoint conjunto.",24,Color.white);
                if(!game.State.completed&&Button(420,363,760,43,"CONTINUAR / ESC"))game.Pause();
                Label(420,426,200,30,"MÚSICA / EFECTOS",18,cyan);float m=GUI.HorizontalSlider(new Rect(655,435,235,25),game.MusicVolume,0,1),s=GUI.HorizontalSlider(new Rect(935,435,235,25),game.SfxVolume,0,1);if(m!=game.MusicVolume||s!=game.SfxVolume)game.Volume(m,s);
                if(game.HasCheckpoint&&Button(420,478,370,45,"RECUPERAR CHECKPOINT"))game.Restore();if(Button(810,478,370,45,"REINICIAR ACCESO"))game.Load("Acceso");
                if(Button(420,540,370,45,"MENÚ PRINCIPAL"))game.Load("MainMenu");
                if(game.State.completed&&Application.CanStreamedLevelBeLoaded("Vigilancia")){if(Button(810,540,370,45,"ENTRAR A VIGILANCIA"))game.Load("Vigilancia");}
                else Label(810,543,370,70,"Vigilancia aún no está creada en este proyecto.",19,Color.gray);
                if(game.SaveError!=""&&Button(420,610,760,40,"REINTENTAR GUARDADO"))game.RetrySave();
            }
            if(game.SaveError!="")Label(420,695,760,55,game.SaveError,17,new Color(1,.5f,.4f));
            GUI.matrix=old;
        }
    }
}
