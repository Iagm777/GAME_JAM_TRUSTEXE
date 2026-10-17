using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TrustEXE.Tutorial.Editor
{
    public static class TutorialValidation
    {
        public static void Run()
        {
            var game=UnityEngine.Object.FindAnyObjectByType<TutorialGame>();
            if(!game)throw new Exception("Open Tutorial in Play Mode first.");
            game.StartCoroutine(Check(game));
        }
        static void Require(bool test,string label){if(!test)throw new Exception("FAIL: "+label);Debug.Log("Tutorial test PASS: "+label);}
        static IEnumerator Check(TutorialGame game)
        {
            var routine=Steps(game);
            while(true)
            {
                object current;
                try{if(!routine.MoveNext())break;current=routine.Current;}
                catch(Exception e){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());File.WriteAllText("Library/TrustTutorialValidation.txt",e.ToString());Debug.LogException(e);yield break;}
                yield return current;
            }
            File.WriteAllText("Library/TrustTutorialValidation.txt","PASS: assets; simultaneous keyboard movement; physical collisions; independent popup; global pause; incorrect order reset; clues retained; door animation and collider release; dual exit; checkpoint roundtrip.\n"+DateTime.UtcNow.ToString("O"));
        }
        static IEnumerator Steps(TutorialGame g)
        {
            Require(g.assets.characters.Length==12&&g.assets.music,"catalog + music");
            foreach(var c in g.assets.characters)Require(c.idle&&c.down.Length==3&&c.up.Length==3&&c.left.Length==3&&c.right.Length==3,"animation "+c.id);
            g.Ready(0);g.Ready(1);var k=Keyboard.current;
            var a=g.Players[0].Position;var b=g.Players[1].Position;
            InputSystem.QueueStateEvent(k,new KeyboardState(Key.A,Key.LeftArrow));yield return new WaitForSeconds(.6f);
            InputSystem.QueueStateEvent(k,new KeyboardState());yield return null;
            Require(g.Players[0].Position.x<a.x-.7f&&g.Players[1].Position.x<b.x-.7f,"two players move on same keyboard");
            g.ShowHelp(0);a=g.Players[0].Position;b=g.Players[1].Position;
            InputSystem.QueueStateEvent(k,new KeyboardState(Key.D,Key.RightArrow));yield return new WaitForSeconds(.4f);
            InputSystem.QueueStateEvent(k,new KeyboardState());yield return null;
            Require(Vector2.Distance(a,g.Players[0].Position)<.08f&&g.Players[1].Position.x>b.x+.4f,"popup only blocks owner");g.ClosePopup(0);
            g.TogglePause();a=g.Players[0].Position;b=g.Players[1].Position;
            InputSystem.QueueStateEvent(k,new KeyboardState(Key.D,Key.RightArrow));yield return new WaitForSeconds(.3f);
            InputSystem.QueueStateEvent(k,new KeyboardState());yield return null;
            Require(Vector2.Distance(a,g.Players[0].Position)<.08f&&Vector2.Distance(b,g.Players[1].Position)<.08f,"pause stops both");g.TogglePause();
            for(int p=0;p<2;p++)g.Players[p].Teleport(TutorialGame.Offset(p)+new Vector2(1.3f,2));
            InputSystem.QueueStateEvent(k,new KeyboardState(Key.A,Key.LeftArrow));yield return new WaitForSeconds(.5f);
            InputSystem.QueueStateEvent(k,new KeyboardState());yield return null;
            Require(g.Players[0].Position.x>1&&g.Players[1].Position.x>21,"room wall collisions");
            for(int p=0;p<2;p++){g.Players[p].Teleport(TutorialGame.Offset(p)+new Vector2(2.2f,2.8f));g.Interact(p);g.ClosePopup(p);}
            Require(g.Progress.terminalRead&&g.Progress.nodeRead,"both clues obtained through devices");
            g.Players[1].Teleport(TutorialGame.Offset(1)+new Vector2(5.1f,3.2f));g.Interact(1);
            Require(g.Progress.sequence==0&&g.Progress.terminalRead&&g.Progress.nodeRead,"wrong order resets only sequence");
            g.Players[0].Teleport(new Vector2(5.1f,3.2f));g.Interact(0);g.Interact(1);
            Require(g.Progress.DoorOpen&&!g.GatePassable,"door waits for animation");yield return new WaitForSeconds(.9f);
            Require(g.GatePassable,"door animation finished");
            for(int p=0;p<2;p++)g.Players[p].Teleport(TutorialGame.Offset(p)+new Vector2(4,4.1f));
            InputSystem.QueueStateEvent(k,new KeyboardState(Key.W,Key.UpArrow));yield return new WaitForSeconds(.65f);
            InputSystem.QueueStateEvent(k,new KeyboardState());yield return null;
            Require(g.Players[0].Position.y>5&&g.Players[1].Position.y>5,"both physically cross open door");
            // Isolate save test from the player's existing save.
            string savePath=TutorialCheckpoint.FilePath;string backup=File.Exists(savePath)?File.ReadAllText(savePath):null;
            try
            {
                g.Players[0].Teleport(new Vector2(4,7.75f));yield return null;Require(!g.Progress.completed,"one exit is insufficient");
                g.Players[1].Teleport(TutorialGame.Offset(1)+new Vector2(4,7.75f));yield return null;yield return null;
                Require(g.Progress.completed&&TutorialCheckpoint.TryRead(out var saved)&&saved.progress.completed,"dual exit saves valid checkpoint");
                Require(g.RestoreCheckpoint(),"checkpoint restore");
            }
            finally {if(backup!=null)File.WriteAllText(savePath,backup);else if(File.Exists(savePath))File.Delete(savePath);}
        }
    }
}
