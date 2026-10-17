using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace TrustEXE.Nucleo.Editor {
 public static class NucleoValidation {
  public static void Check(bool condition,string name){if(!condition)throw new Exception("FAIL: "+name);}
  public static void Run(){
   var s=new NucleoState();Check(!s.Decode(2)&&!s.Sync(0,2,1)&&!s.Vote(0,0),"stage gates");
   Check(!s.Energy(0,0,1)&&!s.Energy(1,0,9)&&s.Energy(0,0,10),"energy expiry and retry");
   for(int n=1;n<3;n++){Check(!s.Energy(0,n,11),"dual energy required");Check(s.Energy(1,n,12),"paired energy");}
   Check(s.stage==1&&!s.Decode(2),"both records required");s.inspected[0]=s.inspected[1]=true;
   Check(!s.Decode(0)&&s.sequence==0,"EVA stale order rejected");Check(s.Decode(2)&&s.Decode(0)&&s.Decode(1)&&s.stage==2,"true order accepted");
   Check(!s.Sync(0,0,20),"wrong sync target");Check(!s.Sync(0,2,20)&&!s.Sync(1,2,23)&&s.Sync(0,2,24),"sync deadline retry");
   s.Retry();Check(s.stage==2&&s.sequence==0&&s.powered[0],"stage retained on failure");
   foreach(int n in NucleoState.Order){Check(!s.Sync(0,n,30),"dual sync required");Check(s.Sync(1,n,31),"paired sync accepted");}
   Check(s.stage==3&&!s.Vote(0,0)&&!s.Vote(1,1)&&s.Vote(0,1),"agreement required");
   Check(JsonUtility.FromJson<NucleoState>(JsonUtility.ToJson(s)).completed,"JSON roundtrip");
   File.WriteAllText("Library/NucleoValidation.txt","PASS: stage gates, energy deadline, retries, both clues, stale EVA rejection, true sequence, sync deadline, checkpoint stage preservation, joint ending, JSON.");
  }
 }
 public sealed class NucleoRuntimeValidation:MonoBehaviour {
  NucleoGame g;
  IEnumerator Start(){g=GetComponent<NucleoGame>();g.TestMode=true;g.SetReady(0);g.SetReady(1);g.State=new NucleoState();
   var keyboard=InputSystem.AddDevice<Keyboard>();
   Vector2 a=g.Players[0].Position,b=g.Players[1].Position;
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.UpArrow));yield return new WaitForSeconds(.35f);
   InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
   NucleoValidation.Check(g.Players[0].Position.y>a.y+.2f&&g.Players[1].Position.y>b.y+.2f,"simultaneous keyboard movement");InputSystem.RemoveDevice(keyboard);
   g.Paused=true;float t=g.Clock;yield return new WaitForSeconds(.2f);NucleoValidation.Check(g.Clock==t,"pause clock");g.Paused=false;
   for(int n=0;n<3;n++){Use(0,n);Use(1,n);}NucleoValidation.Check(g.State.stage==1,"energy through proximity");
   Use(0,3);Use(1,3);NucleoValidation.Check(g.Notes[0]&&g.Notes[1],"independent records");g.Notes[0]=g.Notes[1]=false;
   Use(1,0);NucleoValidation.Check(g.State.sequence==0,"wrong sequence rejected");foreach(int n in NucleoState.Order)Use(1,n);NucleoValidation.Check(g.State.stage==2,"decode through proximity");
   g.Remaining=.001f;yield return null;yield return null;NucleoValidation.Check(g.State.stage==2&&g.State.strikes==1,"timeout recovery");
   g.Clock+=4;g.Integrity=4;g.Players[0].Teleport(NucleoGame.BeamPosition(0));g.Clock=6*Mathf.Ceil(g.Clock/6)+4.5f;yield return null;NucleoValidation.Check(g.Integrity==3,"active laser damages");yield return null;NucleoValidation.Check(g.Integrity==3,"damage immunity");for(int hit=0;hit<3;hit++){g.Clock+=3;g.Hit();}NucleoValidation.Check(g.State.stage==2&&g.Integrity==4&&g.State.sequence==0,"four hits recover current stage");
   foreach(int n in NucleoState.Order){Use(0,n);Use(1,n);}NucleoValidation.Check(g.State.stage==3,"sync through proximity");
   Physics2D.SyncTransforms();NucleoValidation.Check(Physics2D.Linecast(new Vector2(4,1.5f),new Vector2(2,1.5f)).collider,"authored boundary");
   Use(0,4);Use(1,5);NucleoValidation.Check(!g.State.completed,"mismatched votes do not finish");Use(1,4);NucleoValidation.Check(g.State.completed,"both choose ending");
   try{NucleoGame.SaveOverride=Path.GetFullPath("Library/NucleoValidationSave.json");g.TestMode=false;g.SaveCheckpoint();g.SaveCheckpoint();NucleoValidation.Check(g.SaveError==""&&g.Restore()&&g.State.completed,"atomic save restore");File.WriteAllText(NucleoGame.SavePath,"{\"version\":99}");NucleoValidation.Check(!g.Restore(),"invalid save rejection");g.SaveError="";}finally{g.TestMode=true;NucleoGame.SaveOverride=null;}
   File.WriteAllText("Library/NucleoRuntimeValidation.txt","PASS: simultaneous keyboard, pause, proximity interactions, records, wrong order, timeout recovery, real laser damage, immunity, four-hit recovery, all stages, boundary collision, joint ending, save/replace/restore, invalid save rejection. Player save untouched.");
   yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Library/NucleoVictory.png");
  }
  void Use(int p,int n){g.Players[p].Teleport(NucleoGame.Offset(p)+NucleoGame.Devices[n]+Vector2.down*.2f);Physics2D.SyncTransforms();g.Interact(p);}
 }
}

