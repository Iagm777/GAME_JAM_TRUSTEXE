using UnityEngine;
using TrustEXE.Tutorial;
namespace TrustEXE.Nucleo {
 [RequireComponent(typeof(Rigidbody2D),typeof(CircleCollider2D),typeof(SpriteRenderer))]
 public sealed class NucleoPlayer:MonoBehaviour {
  public NucleoGame game; public int player; public TutorialAssets.Character art;
  Rigidbody2D body;SpriteRenderer view;Vector2 input;float clock,step;
  public Vector2 Position=>body.position;
  void Awake(){body=GetComponent<Rigidbody2D>();body.gravityScale=0;body.constraints=RigidbodyConstraints2D.FreezeRotation;body.interpolation=RigidbodyInterpolation2D.Interpolate;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;GetComponent<CircleCollider2D>().radius=.13f;view=GetComponent<SpriteRenderer>();}
  void Update(){
   input=game.CanMove(player)?LocalControls.Move(player):Vector2.zero;
   if(game.Running)clock+=Time.deltaTime;
   var frames=Mathf.Abs(input.x)>Mathf.Abs(input.y)?(input.x<0?art.left:art.right):(input.y>0?art.up:art.down);
   view.sprite=input.sqrMagnitude>.01f?frames[(int)(clock*9)%frames.Length]:art.idle;
   view.sortingOrder=80-Mathf.RoundToInt(Position.y*3);
   if(game.CanMove(player)&&body.linearVelocity.sqrMagnitude>.3f){step+=Time.deltaTime;if(step>.31f){step=0;game.Audio.Play(0,player);}}
  }
  void FixedUpdate(){body.linearVelocity=game.CanMove(player)?Vector2.MoveTowards(body.linearVelocity,input*2.45f,Time.fixedDeltaTime*28):Vector2.zero;}
  public void Teleport(Vector2 p){body.position=p;body.linearVelocity=Vector2.zero;}
 }
}

