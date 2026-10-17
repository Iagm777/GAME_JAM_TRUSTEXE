using System;
namespace TrustEXE.Nucleo {
 [Serializable] public sealed class NucleoState {
  public int stage, sequence, strikes, hints, cooperation;
  public float elapsed;
  public bool completed;
  public bool[] powered=new bool[3], inspected=new bool[2];
  public int[] votes={-1,-1};
  [NonSerialized] public float[] pulses={-100,-100,-100,-100,-100,-100};
  public static readonly int[] Order={2,0,1};
  public bool Energy(int p,int node,float clock){
   if(stage!=0||p<0||p>1||node<0||node>2||powered[node])return false;
   pulses[p*3+node]=clock;
   if(clock-pulses[(1-p)*3+node]>7)return false;
   powered[node]=true;cooperation++;
   if(Array.TrueForAll(powered,x=>x)){stage=1;ResetTransient();}return true;
  }
  public bool Decode(int node){
   if(stage!=1||!inspected[0]||!inspected[1])return false;
   if(node!=Order[sequence]){sequence=0;return false;}
   if(++sequence==3){stage=2;sequence=0;ResetTransient();}return true;
  }
  public bool Sync(int p,int node,float clock){
   if(stage!=2||node!=Order[sequence])return false;
   pulses[p*3+node]=clock;
   if(clock-pulses[(1-p)*3+node]>2.5f)return false;
   cooperation++;sequence++;ResetTransient();
   if(sequence==3){stage=3;sequence=0;}return true;
  }
  public bool Vote(int p,int choice){if(stage!=3||choice<0||choice>1)return false;votes[p]=choice;completed=votes[0]>=0&&votes[0]==votes[1];return completed;}
  public void ResetTransient(){pulses=new[]{-100f,-100f,-100f,-100f,-100f,-100f};}
  public void Retry(){strikes++;sequence=0;votes=new[]{-1,-1};ResetTransient();if(stage==0)powered=new bool[3];if(stage==1)inspected=new bool[2];}
 }
}
