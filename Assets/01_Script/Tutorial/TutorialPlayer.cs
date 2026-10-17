using UnityEngine;

namespace TrustEXE.Tutorial
{
    [RequireComponent(typeof(Rigidbody2D),typeof(CircleCollider2D),typeof(SpriteRenderer))]
    public sealed class TutorialPlayer : MonoBehaviour
    {
        public int player;
        public TutorialAssets.Character art;
        public TutorialGame game;
        public float DistanceWalked {get;private set;}
        public Vector2 Position => body.position;
        Rigidbody2D body;
        SpriteRenderer visual;
        Vector2 input,previous;
        float animationTime;
        void Awake()
        {
            body=GetComponent<Rigidbody2D>();body.gravityScale=0;body.constraints=RigidbodyConstraints2D.FreezeRotation;
            body.interpolation=RigidbodyInterpolation2D.Interpolate;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            GetComponent<CircleCollider2D>().radius=.16f;
            visual=GetComponent<SpriteRenderer>();previous=body.position;
        }
        void Update()
        {
            input=game.CanMove(player)?LocalControls.Move(player):Vector2.zero;
            DistanceWalked+=Vector2.Distance(previous,body.position);previous=body.position;
            if(art==null)return;
            animationTime+=Time.deltaTime;
            var frames=Mathf.Abs(input.x)>Mathf.Abs(input.y)?(input.x<0?art.left:art.right):(input.y>0?art.up:art.down);
            visual.sprite=input.sqrMagnitude>.01f&&frames!=null&&frames.Length>0?frames[(int)(animationTime*9)%frames.Length]:art.idle;
            visual.sortingOrder=50-Mathf.RoundToInt(body.position.y*2);
        }
        void FixedUpdate() {body.linearVelocity=game.CanMove(player)?input*2.15f:Vector2.zero;}
        public void Teleport(Vector2 position) {body.position=position;previous=position;body.linearVelocity=Vector2.zero;}
    }
}
