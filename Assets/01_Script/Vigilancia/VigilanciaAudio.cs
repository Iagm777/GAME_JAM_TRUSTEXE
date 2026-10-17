using UnityEngine;
namespace TrustEXE.Vigilancia {
 public sealed class VigilanciaAudio:MonoBehaviour {
  AudioSource music,alarm; readonly AudioSource[] voices=new AudioSource[2];
  readonly AudioClip[] clips=new AudioClip[8];
  public float MusicVolume{get;private set;} public float EffectsVolume{get;private set;}
  public void Init(AudioClip track){
   MusicVolume=PlayerPrefs.GetFloat("VigilanciaMusic",.38f);EffectsVolume=PlayerPrefs.GetFloat("VigilanciaEffects",.65f);
   music=gameObject.AddComponent<AudioSource>();music.clip=track;music.loop=true;music.volume=MusicVolume;music.Play();
   for(int p=0;p<2;p++){voices[p]=gameObject.AddComponent<AudioSource>();voices[p].panStereo=p==0?-.5f:.5f;voices[p].playOnAwake=false;}
   for(int k=0;k<8;k++)clips[k]=Make(k);
   alarm=gameObject.AddComponent<AudioSource>();alarm.clip=clips[6];alarm.loop=true;alarm.volume=0;alarm.Play();
  }
  // 0 step, 1 accepted, 2 denied, 3 camera shutdown, 4 relay, 5 lift, 6 alarm, 7 laser warning.
  public void Play(int kind,int player=0){voices[player].PlayOneShot(clips[kind],EffectsVolume*(kind==0?.22f:1));}
  public void Mix(float danger,bool paused){alarm.volume=paused?0:EffectsVolume*danger*.4f;music.volume=MusicVolume*(paused?.25f:1-danger*.3f);}
  public void Volume(float m,float s){MusicVolume=m;EffectsVolume=s;PlayerPrefs.SetFloat("VigilanciaMusic",m);PlayerPrefs.SetFloat("VigilanciaEffects",s);}
  static AudioClip Make(int k){
   int rate=22050;float duration=k==0?.07f:k==5?1.5f:k==6?1f:k==3?.6f:.25f;
   float[] data=new float[(int)(rate*duration)];var rng=new System.Random(40+k);
   for(int i=0;i<data.Length;i++){
    float t=(float)i/rate,env=Mathf.Sin(Mathf.PI*i/data.Length),f=k==0?90:k==1?880:k==2?135:k==3?700-950*t:k==4?480+1200*t:k==5?65+70*t:k==6?560+180*Mathf.Sin(t*Mathf.PI*2):1200;
    float v=Mathf.Sin(2*Mathf.PI*f*t);
    if(k==0||k==5)v=.4f*v+.2f*(float)(rng.NextDouble()*2-1);
    data[i]=v*env*.23f;
   }
   var c=AudioClip.Create("Vigilancia SFX "+k,data.Length,1,rate,false);c.SetData(data,0);return c;
  }
  void OnDestroy(){foreach(var c in clips)if(c)Destroy(c);}
 }
}
