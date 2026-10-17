using System;
using System.IO;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace TrustEXE.Vigilancia.Editor {
 public static class VigilanciaValidation {
  public static void Check(bool condition,string name){if(!condition)throw new Exception("FAIL: "+name);}
  public static void Run(){var s=new VigilanciaState();Check(!s.Switch(1),"clue and relays required");s.clue=true;s.relays[0]=s.relays[1]=s.relays[2]=true;Check(!s.Switch(0)&&s.sequence==0,"cached EVA order rejected");Check(s.Switch(1)&&s.Switch(0)&&s.Switch(2),"physical order accepted");Check(!s.Pulse(0,1)&&!s.Pulse(1,8),"expired pulse rejected");Check(s.Pulse(0,9),"sync can be retried");s.ResetTimers();Check(s.pulse0==-100&&s.pulse1==-100,"timer reset");Check(JsonUtility.FromJson<VigilanciaState>(JsonUtility.ToJson(s)).lift,"JSON roundtrip");
   var a=AssetDatabase.LoadAssetAtPath<VigilanciaAssets>("Assets/06_Data/Vigilancia/VigilanciaAssets.asset");Check(a&&a.maps.Length==2&&a.music&&a.cameras.Length==2&&a.shared.characters.Length==12,"assets bound");
   foreach(var c in a.shared.characters)Check(c.idle&&c.down.Length==3&&c.up.Length==3&&c.left.Length==3&&c.right.Length==3,"animated "+c.id);
   for(int p=0;p<2;p++)for(int i=0;i<8;i++)Check(VigilanciaGame.Walkable(VigilanciaGame.Device(p,i)),"device on walkable floor");
   File.WriteAllText("Library/VigilanciaValidation.txt","PASS: state prerequisites, stale clue, correct order, expired sync, retry, serialization, assets, 12 animations, walkable devices.");
  }
 }
 public sealed class VigilanciaRuntimeValidation:MonoBehaviour {
  VigilanciaGame g;
  IEnumerator Start(){g=GetComponent<VigilanciaGame>();g.TestMode=true;g.SetReady(0);g.SetReady(1);yield return null;
   var keyboard=Keyboard.current;var a=g.Players[0].Position;var b=g.Players[1].Position;
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.UpArrow));yield return new WaitForSeconds(.4f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForFixedUpdate();
   VigilanciaValidation.Check(g.Players[0].Position.y>a.y+.6f&&g.Players[1].Position.y>b.y+.6f,"simultaneous input");
   g.Notes[0]=true;a=g.Players[0].Position;b=g.Players[1].Position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.UpArrow));yield return new WaitForSeconds(.3f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForFixedUpdate();VigilanciaValidation.Check(Vector2.Distance(a,g.Players[0].Position)<.06f&&g.Players[1].Position.y>b.y+.4f,"independent notes");g.Notes[0]=false;
   float clock=g.Clock;g.Paused=true;yield return new WaitForSeconds(.2f);VigilanciaValidation.Check(g.Clock==clock,"pause freezes hazards");g.Paused=false;
   Use(0,0);VigilanciaValidation.Check(!g.State.relays[0],"active camera blocks relay");Use(0,3);g.Notes[0]=false;
   for(int i=0;i<3;i++){Use(1,i);Use(0,i);VigilanciaValidation.Check(g.State.relays[i],"cross world camera relay "+i);}
   Use(1,4);VigilanciaValidation.Check(g.State.sequence==0,"wrong order retry");Use(1,5);Use(1,4);Use(1,6);VigilanciaValidation.Check(g.State.sequence==3,"three switches complete");
   int alarms=g.Alarms;g.Clock+=20;float deadline=g.Clock+3;
   while(g.Alarms==alarms&&g.Clock<deadline){float angle=(-135+Mathf.Sin(g.Clock*.65f)*55)*Mathf.Deg2Rad;g.Players[0].Teleport(VigilanciaGame.Rooms[0]+new Vector2(.65f,.7f)+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*.8f);Physics2D.SyncTransforms();yield return null;}
   VigilanciaValidation.Check(g.Alarms==alarms+1&&g.State.sequence==3&&g.State.relays[0]&&g.Players[0].Position==VigilanciaGame.Spawn,"real sensor detection, alarm retains progress and safe spawn");
   Use(0,7);Use(1,7);yield return new WaitForSeconds(1.7f);VigilanciaValidation.Check(g.State.lift,"lift animation completes");
   Physics2D.SyncTransforms();VigilanciaValidation.Check(Physics2D.Linecast(new Vector2(4,.7f),new Vector2(3,.7f)).collider,"boundary blocks");
   for(int p=0;p<2;p++)for(int i=0;i<3;i++){var pos=VigilanciaGame.LaserPosition(p,i);VigilanciaValidation.Check(!Physics2D.Linecast(pos+Vector2.left*.2f,pos+Vector2.right*.2f).collider,"relay removes barrier");}
   g.Players[0].Teleport(new Vector2(4,4.6f));yield return new WaitForFixedUpdate();yield return null;VigilanciaValidation.Check(!g.State.completed,"both exits required");g.Players[1].Teleport(new Vector2(24,4.6f));yield return new WaitForFixedUpdate();yield return null;VigilanciaValidation.Check(g.State.completed,"joint exit");
   try{VigilanciaGame.SaveOverride=Path.GetFullPath("Library/VigilanciaValidationSave.json");g.TestMode=false;g.SaveCheckpoint();g.SaveCheckpoint();VigilanciaValidation.Check(g.SaveError==""&&g.Restore()&&g.State.completed,"atomic save restore");File.WriteAllText(VigilanciaGame.SavePath,"{\"version\":99}");VigilanciaValidation.Check(!g.Restore(),"corrupt save rejected");g.SaveError="";}finally{g.TestMode=true;VigilanciaGame.SaveOverride=null;}
   File.WriteAllText("Library/VigilanciaRuntimeValidation.txt","PASS: real simultaneous keyboard input; independent notes; pause; camera relay prerequisite; all switches; wrong order retry; alarm recovery; wall and open laser collisions; dual exit; atomic save/replace/restore; invalid save rejected. Player save untouched.");
  }
  void Use(int p,int i){g.Players[p].Teleport(VigilanciaGame.Offset(p)+VigilanciaGame.Device(p,i)+new Vector2(0,-.2f));Physics2D.SyncTransforms();g.Interact(p);}
 }
}
