using UnityEngine;
using UnityEngine.InputSystem;

namespace TrustEXE.Tutorial
{
    /// <summary>Two stable gamepad slots plus two independent halves of one keyboard.</summary>
    public static class LocalControls
    {
        static readonly Gamepad[] pads = new Gamepad[2];
        static readonly float[] nextNavigation = new float[2];
        static readonly Vector2[] lastNavigation = new Vector2[2];
        static int sampledFrame = -1;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { pads[0]=pads[1]=null; sampledFrame=-1; nextNavigation[0]=nextNavigation[1]=0; lastNavigation[0]=lastNavigation[1]=Vector2.zero; }
        public static Gamepad Pad(int player)
        {
            if (sampledFrame != Time.frameCount)
            {
                sampledFrame = Time.frameCount;
                for (int p=0;p<2;p++) if (pads[p]!=null && !pads[p].added) pads[p]=null;
                foreach(var pad in Gamepad.all)
                {
                    if (pad==pads[0] || pad==pads[1]) continue;
                    if(pads[0]==null) pads[0]=pad; else if(pads[1]==null) pads[1]=pad;
                }
            }
            return pads[player];
        }
        public static Vector2 Move(int p)
        {
            var k=Keyboard.current; Vector2 value=Vector2.zero;
            if(k!=null) value=p==0
                ? new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0))
                : new Vector2((k.rightArrowKey.isPressed?1:0)-(k.leftArrowKey.isPressed?1:0),(k.upArrowKey.isPressed?1:0)-(k.downArrowKey.isPressed?1:0));
            var pad=Pad(p);
            if(pad!=null)
            {
                var stick=pad.leftStick.ReadValue(); if(stick.magnitude<.22f) stick=Vector2.zero;
                var gamepad=stick+pad.dpad.ReadValue(); if(gamepad.sqrMagnitude>value.sqrMagnitude) value=gamepad;
            }
            return Vector2.ClampMagnitude(value,1);
        }
        public static Vector2 Navigate(int p)
        {
            var v=Move(p); if(v.sqrMagnitude<.1f) {lastNavigation[p]=Vector2.zero;return Vector2.zero;}
            var direction=Mathf.Abs(v.x)>Mathf.Abs(v.y)?new Vector2(Mathf.Sign(v.x),0):new Vector2(0,Mathf.Sign(v.y));
            if(direction!=lastNavigation[p]) {lastNavigation[p]=direction; nextNavigation[p]=Time.unscaledTime+.32f;return direction;}
            if(Time.unscaledTime>=nextNavigation[p]) {nextNavigation[p]=Time.unscaledTime+.16f;return direction;}
            return Vector2.zero;
        }
        public static bool Interact(int p)
        {
            var k=Keyboard.current;
            return (Pad(p)?.buttonSouth.wasPressedThisFrame??false) || (k!=null && (p==0?k.eKey.wasPressedThisFrame:k.enterKey.wasPressedThisFrame||k.numpadEnterKey.wasPressedThisFrame));
        }
        public static bool Cancel(int p)
        { var k=Keyboard.current;return (Pad(p)?.buttonEast.wasPressedThisFrame??false)||(k!=null&&(p==0?k.rKey.wasPressedThisFrame:k.rightShiftKey.wasPressedThisFrame)); }
        public static bool Help(int p)
        { var k=Keyboard.current;return (Pad(p)?.buttonWest.wasPressedThisFrame??false)||(k!=null&&(p==0?k.qKey.wasPressedThisFrame:k.rightCtrlKey.wasPressedThisFrame)); }
        public static bool Pause => (Keyboard.current?.escapeKey.wasPressedThisFrame??false)||(Pad(0)?.startButton.wasPressedThisFrame??false)||(Pad(1)?.startButton.wasPressedThisFrame??false);
        public static string ActionName(int p) => Pad(p)!=null?"BOTÓN INFERIOR":p==0?"E":"ENTER";
        public static string CancelName(int p) => Pad(p)!=null?"BOTÓN DERECHO":p==0?"R":"SHIFT DER.";
        public static string HelpName(int p) => Pad(p)!=null?"BOTÓN IZQUIERDO":p==0?"Q":"CTRL DER.";
        public static string MoveName(int p) => Pad(p)!=null?"STICK IZQUIERDO / CRUCETA":p==0?"W A S D":"FLECHAS";
        public static string DeviceName(int p) => Pad(p)!=null?"MANDO "+(p+1):"TECLADO · P"+(p+1);
    }
}
