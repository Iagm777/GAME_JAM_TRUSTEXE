using UnityEngine;

namespace TrustEXE.Tutorial
{
    public sealed class TutorialAtmosphere : MonoBehaviour
    {
        TutorialGame game;
        readonly Transform[] sparks=new Transform[36];
        readonly Vector3[] origins=new Vector3[36];
        Sprite sprite;
        float time;
        public void Initialize(TutorialGame owner)
        {
            game=owner;
            sprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            var random=new System.Random(48);
            for(int i=0;i<sparks.Length;i++)
            {
                var go=new GameObject("Ambient mote "+i);go.transform.SetParent(transform);
                origins[i]=new Vector3((i%2)*20+1+(float)random.NextDouble()*6,1+(float)random.NextDouble()*7,0);
                sparks[i]=go.transform;go.transform.localScale=Vector3.one*(i%3==0?.035f:.018f);
                var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=12;
                r.color=i%2==0?new Color(1,.65f,.22f,.45f):new Color(.3f,.85f,1,.5f);
            }
        }
        void Update()
        {
            if(!game||game.Paused)return;time+=Time.deltaTime;
            for(int i=0;i<sparks.Length;i++)sparks[i].position=origins[i]+new Vector3(Mathf.Sin(time*.4f+i)*.14f,Mathf.Sin(time*.7f+i)*.25f,0);
        }
        void OnDestroy(){if(sprite)Destroy(sprite);}
    }
}
