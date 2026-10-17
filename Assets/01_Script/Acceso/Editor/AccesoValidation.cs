using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TrustEXE.Acceso.Editor
{
    public static class AccesoValidation
    {
        public static void Check(bool value,string name){if(!value)throw new Exception("FAIL: "+name);}
        public static void Run(){
            var s=new AccesoState();Check(!s.DoorA&&!s.CanSync,"initial locks");s.card=true;Check(!s.DoorA,"P1 cannot open alone");s.linked=true;Check(s.DoorA,"joint credentials");
            Check(!s.Code(1,2,3)&&!s.decoded,"wrong code retry");Check(s.Code(3,1,2),"physical code");Check(!s.Verify(true),"cannot verify unpowered supply");s.powered=true;Check(!s.Verify(false)&&!s.verified,"EVA lie rejected");Check(s.Verify(true)&&s.CanSync,"physical truth accepted");
            Check(!s.Pulse(0,1),"one pulse insufficient");Check(!s.Pulse(1,8),"expired pulse rejected");Check(s.Pulse(0,9)&&s.doorB,"retry and reverse order accepted");
            s.ResetTimers();Check(s.pulseP1==-100&&s.pulseP2==-100,"checkpoint clears timers");
            var copy=JsonUtility.FromJson<AccesoState>(JsonUtility.ToJson(s));Check(copy.CanSync&&copy.doorB,"JSON roundtrip");
            var a=AssetDatabase.LoadAssetAtPath<AccesoAssets>("Assets/06_Data/Acceso/AccesoAssets.asset");Check(a&&a.maps.Length==2&&a.ambience&&a.shared.characters.Length==12,"scene assets");
            foreach(var c in a.shared.characters)Check(c.idle&&c.up.Length==3&&c.down.Length==3&&c.left.Length==3&&c.right.Length==3,"animation "+c.id);
            File.WriteAllText("Library/AccesoValidation.txt","PASS: 13 state/asset checks; all 12 characters have directional animation sets.");
        }
    }
    // Executes in Play Mode, uses the same public interactions as the keyboard, and never writes a player's save.
    public sealed class AccesoRuntimeValidation : MonoBehaviour
    {
        AccesoGame game;
        IEnumerator Start(){
            game=GetComponent<AccesoGame>();game.TestMode=true;game.SetReady(0);game.SetReady(1);
            yield return null;
            var keyboard=Keyboard.current;
            Vector2 start0=game.Players[0].Position,start1=game.Players[1].Position;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D,Key.RightArrow));yield return new WaitForSeconds(.35f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForFixedUpdate();
            AccesoValidation.Check(game.Players[0].Position.x>start0.x+.5f&&game.Players[1].Position.x>start1.x+.5f,"simultaneous keyboard movement");
            Use(0,0);Use(1,0);game.Panel[0]=game.Panel[1]=0;
            yield return new WaitForSeconds(1);
            AccesoValidation.Check(game.GatesClear(0),"door A collider opens");
            // Collider casts verify both door states and the room contour without bypassing physics.
            Physics2D.SyncTransforms();
            AccesoValidation.Check(!Physics2D.Linecast(new Vector2(2.65f,3.05f),new Vector2(2.65f,3.65f)).collider,"door A passable");
            AccesoValidation.Check(Physics2D.Linecast(new Vector2(2.65f,5.8f),new Vector2(2.65f,6.4f)).collider,"door B still blocks");
            AccesoValidation.Check(Physics2D.Linecast(new Vector2(2.65f,1),new Vector2(2.65f,-1)).collider,"outer wall blocks");
            Use(0,1);game.Panel[0]=0;Use(1,1);game.Digits[0]=3;game.Digits[1]=1;game.Digits[2]=2;game.SubmitCode();game.PowerChoice=0;game.SubmitPower();AccesoValidation.Check(!game.State.verified,"false diagnosis refused");game.PowerChoice=1;game.SubmitPower();
            game.Panel[0]=1;AccesoValidation.Check(!game.CanMove(0)&&game.CanMove(1),"panel ownership");
            start0=game.Players[0].Position;start1=game.Players[1].Position;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D,Key.RightArrow));yield return new WaitForSeconds(.2f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForFixedUpdate();
            AccesoValidation.Check(Vector2.Distance(start0,game.Players[0].Position)<.08f&&game.Players[1].Position.x>start1.x+.2f,"panel blocks only its owner during real input");game.Panel[0]=0;
            game.Pause();AccesoValidation.Check(!game.CanMove(0)&&!game.CanMove(1),"global pause");game.Pause();
            Use(0,2);Use(1,2);yield return new WaitForSeconds(1);
            AccesoValidation.Check(game.GatesClear(1),"door B opens");Physics2D.SyncTransforms();AccesoValidation.Check(!Physics2D.Linecast(new Vector2(2.65f,5.8f),new Vector2(2.65f,6.4f)).collider,"door B passable");
            game.Players[0].Teleport(new Vector2(2.65f,7.65f));yield return new WaitForFixedUpdate();yield return null;AccesoValidation.Check(!game.State.completed,"one exit insufficient");
            game.Players[1].Teleport(new Vector2(22.65f,7.65f));yield return new WaitForFixedUpdate();yield return null;AccesoValidation.Check(game.State.completed,"joint exit completes");
            try {
                AccesoGame.ValidationSavePath=Path.GetFullPath("Library/AccesoValidationSave.json");game.TestMode=false;
                AccesoValidation.Check(game.SaveCheckpoint(3)&&game.SaveCheckpoint(3),"save create and atomic replacement");
                AccesoValidation.Check(game.Restore()&&game.State.completed&&game.State.CanSync,"full checkpoint restore");
                File.WriteAllText(AccesoGame.SavePath,"{\"version\":99}");AccesoValidation.Check(!game.Restore(),"invalid checkpoint refused");
                game.SaveError="";
            } finally { game.TestMode=true;AccesoGame.ValidationSavePath=null; }
            File.WriteAllText("Library/AccesoRuntimeValidation.txt","PASS: simultaneous keyboard movement; full puzzle; collisions; independent panels; pause; retry; joint exit; atomic checkpoint write/replace/restore; invalid save rejection. User checkpoint untouched.");
        }
        void Use(int p,int index){game.Players[p].Teleport(AccesoGame.Offset(p)+AccesoGame.LocalDevice(index)+new Vector2(0,-.55f));Physics2D.SyncTransforms();game.Interact(p);}
    }
}
