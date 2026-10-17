using UnityEngine;
using TrustEXE.Tutorial;

namespace TrustEXE.Acceso
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public sealed class AccesoPlayer : MonoBehaviour
    {
        public AccesoGame game;
        public int player;
        public TutorialAssets.Character art;
        public Vector2 Position => body.position;
        Rigidbody2D body;
        SpriteRenderer view;
        Vector2 input;
        float clock, step;
        void Awake()
        {
            body = GetComponent<Rigidbody2D>(); body.gravityScale = 0; body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = GetComponent<CircleCollider2D>(); col.radius = .15f;
            view = GetComponent<SpriteRenderer>();
        }
        void Update()
        {
            input = game.CanMove(player) ? LocalControls.Move(player) : Vector2.zero;
            if (!game.Paused) clock += Time.deltaTime;
            var frames = Mathf.Abs(input.x) > Mathf.Abs(input.y) ? (input.x < 0 ? art.left : art.right) : (input.y > 0 ? art.up : art.down);
            view.sprite = input.sqrMagnitude > .01f ? frames[(int)(clock * 9) % frames.Length] : art.idle;
            view.sortingOrder = 80 - Mathf.RoundToInt(Position.y * 3);
            if (body.linearVelocity.sqrMagnitude > .3f && game.CanMove(player)) { step += Time.deltaTime; if (step > .32f) { step = 0; game.Footstep(player); } }
        }
        void FixedUpdate() { body.linearVelocity = game.CanMove(player) ? input * 2.35f : Vector2.zero; }
        public void Teleport(Vector2 pos) { body.position = pos; body.linearVelocity = Vector2.zero; }
    }
}
