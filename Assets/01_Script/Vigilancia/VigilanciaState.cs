using System;
namespace TrustEXE.Vigilancia {
 [Serializable] public sealed class VigilanciaState {
  public bool clue, lift, completed;
  public bool[] relays=new bool[3];
  public int sequence;
  public float pulse0=-100,pulse1=-100;
  public static readonly int[] Order={1,0,2};
  public bool Switch(int index) {
   if(!clue||index<0||index>2||!relays[index]||sequence>=3)return false;
   if(index!=Order[sequence]){sequence=0;return false;}
   sequence++;return true;
  }
  public bool Pulse(int p,float time){
   if(sequence!=3)return false;
   if(p==0)pulse0=time;else pulse1=time;
   lift=Math.Abs(pulse0-pulse1)<=5&&time-Math.Min(pulse0,pulse1)<=5;return lift;
  }
  public void ResetTimers(){pulse0=pulse1=-100;}
 }
}
