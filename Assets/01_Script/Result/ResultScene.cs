using UnityEngine;
using UnityEngine.SceneManagement;
using TrustEXE.Core;
using TrustEXE.Menu;
using TrustEXE.Tutorial;

namespace TrustEXE.Result
{
    /// <summary>
    /// Shared result screen: Victoria after the Núcleo ending, Derrota whenever a
    /// sector consumes the shared lives, with the exact failure reason. Drawn with
    /// IMGUI like the sector HUDs; built from values frozen by GameSession.
    /// </summary>
    public sealed class ResultScene : MonoBehaviour
    {
        public TutorialAssets shared;
        GUIStyle title, button;
        readonly Color amber = new Color(1, .72f, .38f), cyan = new Color(.3f, .87f, 1);
        AudioSource audio;
        AudioClip winTone, loseTone;
        bool defeat;

        void Start()
        {
            defeat = GameSession.Defeated;
            audio = gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            winTone = Jingle(true);
            loseTone = Jingle(false);
            audio.PlayOneShot(defeat ? loseTone : winTone, .85f);
        }

        void Box(float x, float y, float w, float h, Color c) { GUI.color = c; GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture); GUI.color = Color.white; }
        void Label(float x, float y, float w, float h, string s, int size, Color c, bool bold = false)
        { title.fontSize = size; title.normal.textColor = c; title.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; GUI.Label(new Rect(x, y, w, h), s, title); }
        bool Button(float x, float y, float w, float h, string s) => GUI.Button(new Rect(x, y, w, h), s, button);

        void OnGUI()
        {
            if (!shared || !shared.menu) return;
            if (title == null) { title = new GUIStyle(GUI.skin.label) { wordWrap = true, padding = new RectOffset(0, 0, 0, 0) }; button = new GUIStyle(GUI.skin.button) { fontSize = 22, wordWrap = true, padding = new RectOffset(12, 12, 8, 8) }; }
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1600f, Screen.height / 900f, 1));
            if (defeat) DrawDefeat(); else DrawVictory();
            GUI.matrix = old;
        }

        void DrawVictory()
        {
            Box(0, 0, 1600, 900, new Color(.014f, .025f, .045f, 1));
            for (int i = 0; i < 32; i++) { float x = (i * 83 + Time.unscaledTime * 15) % 1600; Box(x, 90 + (i * 71) % 620, 2, 35, new Color(.1f, .7f, .85f, .14f)); }
            Label(460, 38, 1000, 45, "TRUST.EXE / PROTOCOLO COMPLETADO", 32, cyan);
            Label(400, 95, 900, 70, "LA CONFIANZA FUE HUMANA", 43, Color.white);
            if (shared.menu.avatars != null)
            {
                int shown = 0;
                for (int i = 0; i < shared.menu.avatars.Length && shown < 10; i++)
                {
                    var a = shared.menu.avatars[i];
                    if (a.id == LocalParty.Explorer || a.id == LocalParty.Operator) continue;
                    float x = 75 + shown * 145, y = 175 + Mathf.Sin(Time.unscaledTime * 1.4f + shown) * 7;
                    GUI.color = new Color(.4f, .65f, .78f, .55f); GUI.DrawTexture(new Rect(x, y, 145, 190), a.idle, ScaleMode.ScaleToFit); GUI.color = Color.white;
                    Label(x + 10, 375, 135, 25, a.id, 17, new Color(.45f, .6f, .7f));
                    shown++;
                }
            }
            Box(285, 470, 1030, 3, cyan);
            for (int p = 0; p < 2; p++)
            {
                var id = p == 0 ? LocalParty.Explorer : LocalParty.Operator;
                Texture2D art = null;
                if (shared.menu.avatars != null) for (int i = 0; i < shared.menu.avatars.Length; i++) if (shared.menu.avatars[i].id == id) { art = shared.menu.avatars[i].idle; break; }
                if (art == null) continue;
                float x = p == 0 ? 400 : 920;
                GUI.DrawTexture(new Rect(x, 340 + Mathf.Sin(Time.unscaledTime * 2 + p) * 4, 280, 280), art, ScaleMode.ScaleToFit);
                Label(x + 40, 580, 260, 40, id + " / P" + (p + 1), 27, p == 0 ? amber : cyan);
            }
            string ending = GameSession.EndingText != "" ? GameSession.EndingText : "El equipo recuperó el control.";
            Label(340, 635, 1000, 75, ending, 25, Color.white);
            Label(360, 725, 1050, 40, "TIEMPO " + Mathf.FloorToInt(GameSession.Elapsed / 60).ToString("00") + ":" + Mathf.FloorToInt(GameSession.Elapsed % 60).ToString("00") + "     REINTENTOS " + GameSession.Strikes + "     CONSULTAS " + GameSession.Hints + "     ENLACES COOPERATIVOS " + GameSession.Cooperation, 22, cyan);
            if (Button(450, 800, 400, 56, "VOLVER AL MENÚ")) LoadMenu();
            if (Button(900, 800, 250, 56, "REJUGAR NÚCLEO")) ReplayNucleo();
        }

        void DrawDefeat()
        {
            Box(0, 0, 1600, 900, new Color(.06f, .015f, .015f, 1));
            for (int i = 0; i < 40; i++) { float x = (i * 61 + Time.unscaledTime * 18) % 1600; Box(x, 80 + (i * 53) % 700, 2, 30, new Color(1, .18f, .1f, .16f)); }
            if (shared.menu.eva != null && shared.menu.eva.Length > 0)
                GUI.DrawTexture(new Rect(650, 92, 300, 300), shared.menu.eva[(int)(Time.unscaledTime * 8) % shared.menu.eva.Length], ScaleMode.ScaleToFit);
            Label(440, 400, 720, 80, "ACCESO DENEGADO", 56, new Color(1, .25f, .18f), true);
            Label(440, 480, 720, 45, "E.V.A.  /  PROTOCOLO DE FALLO ACTIVADO", 24, cyan);
            Box(470, 550, 660, 4, new Color(1, .25f, .18f));
            string reason = GameSession.FailureReason != "" ? GameSession.FailureReason : "Se agotaron las vidas compartidas.";
            Label(470, 572, 660, 110, "PERDISTE: " + reason, 27, Color.white);
            Label(470, 662, 660, 40, "Sector: " + (GameSession.Level != "" ? GameSession.Level : "—"), 20, new Color(.75f, .79f, .83f));
            if (Button(480, 730, 310, 60, "REINTENTAR NIVEL")) GameSession.RetryLevel();
            if (Button(810, 730, 310, 60, "VOLVER AL MENÚ")) LoadMenu();
            Label(470, 808, 660, 28, "Las vidas compartidas se restauran al reintentar el nivel.", 17, new Color(.7f, .74f, .78f));
        }

        void LoadMenu() { SceneManager.LoadSceneAsync("MainMenu"); }
        void ReplayNucleo() { GameSession.Lives = GameSession.MaxLives; GameSession.Level = ""; GameSession.Defeated = GameSession.Victorious = false; SceneManager.LoadSceneAsync("Nucleo"); }

        AudioClip Jingle(bool win)
        {
            const int rate = 22050; float duration = win ? 1.6f : 1.1f; var samples = new float[(int)(rate * duration)];
            float[] notes = win ? new[] { 523.25f, 659.25f, 783.99f, 1046.5f } : new[] { 392f, 329.63f, 261.63f, 196f };
            float step = duration / notes.Length;
            for (int n = 0; n < notes.Length; n++)
            {
                int start = (int)(n * step * rate), count = (int)(step * rate * .92f);
                for (int i = 0; i < count && start + i < samples.Length; i++)
                {
                    float t = (float)i / rate, env = Mathf.Min(1, t / .01f) * Mathf.Pow(1 - (float)i / count, 1.4f);
                    samples[start + i] += Mathf.Sin(2 * Mathf.PI * notes[n] * t) * env * (win ? .16f : .14f);
                }
            }
            var clip = AudioClip.Create("Result jingle", samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        void OnDestroy() { if (winTone) Destroy(winTone); if (loseTone) Destroy(loseTone); }
    }
}