using UnityEngine;
using UnityEngine.UI;

namespace TrustEXE.Core
{
    /// <summary>
    /// Radiocast banner: E.V.A. physically appears, animated, on the side of the
    /// player who just received a clue, warning or instruction. It renders as a
    /// screen-space overlay above both views (like the menu interface) and never
    /// consumes keyboard input, so local co-op controls keep working.
    /// </summary>
    public sealed class EvaPresenter : MonoBehaviour
    {
        public bool Active => content != null;

        RawImage portrait;
        Text body, stamp;
        RectTransform content;
        AudioSource voice;
        AudioClip chime, alarmTone;
        Font font;
        Texture2D[] frames;
        float startedAt, duration;
        int side;
        bool warned;

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            chime = Tone(640, .34f, true);
            alarmTone = Tone(170, .55f, false);
        }

        /// <summary>
        /// Shows E.V.A. animated with a caption. player 0/1 anchors the panel on
        /// that player's half; player 2 centers it for joint messages.
        /// </summary>
        public void Show(string message, Texture2D[] portraitFrames, int player, float seconds, bool warning)
        {
            if (portraitFrames == null || portraitFrames.Length == 0) return;
            if (content != null) Dismiss();
            frames = portraitFrames;
            side = Mathf.Clamp(player, 0, 2);
            warned = warning;
            duration = Mathf.Max(.6f, seconds);
            startedAt = Time.unscaledTime;
            voice.PlayOneShot(warning ? alarmTone : chime);
            Build(message);
        }

        public void Dismiss()
        {
            if (content != null) { Destroy(content.gameObject); content = null; portrait = null; body = null; stamp = null; }
        }

        void Build(string message)
        {
            var root = new GameObject("EVA radiocast", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform);
            var canvas = root.GetComponent<RectTransform>();
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scale = root.GetComponent<CanvasScaler>();
            scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1600, 900); scale.matchWidthOrHeight = .5f;
            scale.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var frame = Rect(canvas, "Safe frame", 0, 0, 1600, 900);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, .5f);
            canvas = frame;

            float cx = side == 0 ? 400 : side == 1 ? 1200 : 800; // player half or centered
            var panel = Panel(canvas, "EVA panel", cx - 350, 150, 700, 330, new Color(.03f, .06f, .1f, .96f));
            Panel(panel.rectTransform, "EVA edge", 0, 0, 700, 3, warned ? new Color(1, .2f, .12f) : new Color(.25f, .87f, 1));
            portrait = Picture(panel.rectTransform, "EVA portrait", frames[0], 30, 46, 190, 190);
            stamp = Label(panel.rectTransform, warned ? "E.V.A.  /  AVISO" : "E.V.A.  /  SEÑAL", 250, 46, 420, 36, 22, new Color(.25f, .87f, 1), TextAnchor.UpperLeft);
            body = Label(panel.rectTransform, message, 250, 96, 420, 200, 24, Color.white, TextAnchor.UpperLeft);
            content = panel.rectTransform;

            var dim = Panel(canvas, "EVA backdrop", 0, 0, 1600, 900, new Color(.01f, .02f, .04f, .08f));
            dim.raycastTarget = false;
        }

        void Update()
        {
            if (content == null) return;
            float t = Time.unscaledTime - startedAt;
            if (t < 0) return;
            if (frames != null && frames.Length > 0)
                portrait.texture = frames[(int)(Time.unscaledTime * 8) % frames.Length];
            float slide = Mathf.Pow(1 - Mathf.Clamp01(t / .3f), 3) * (side == 0 ? -60 : side == 1 ? 60 : 0);
            content.anchoredPosition = new Vector2(side == 0 ? 400 : side == 1 ? 1200 : 800, -150 + slide);
            content.localScale = Vector3.one * (1 + Mathf.Clamp01(1f - t / .3f) * .18f);
            if (t >= duration) Dismiss();
        }

        RectTransform Rect(Transform parent, string label, float x, float y, float w, float h)
        {
            var go = new GameObject(label, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h); return rt;
        }
        Image Panel(Transform parent, string label, float x, float y, float w, float h, Color color)
        { var image = Rect(parent, label, x, y, w, h).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image; }
        RawImage Picture(Transform parent, string label, Texture2D texture, float x, float y, float w, float h)
        { var image = Rect(parent, label, x, y, w, h).gameObject.AddComponent<RawImage>(); image.texture = texture; image.raycastTarget = false; return image; }
        Text Label(Transform parent, string value, float x, float y, float w, float h, int size, Color color, TextAnchor align = TextAnchor.UpperLeft)
        {
            var text = Rect(parent, value, x, y, w, h).gameObject.AddComponent<Text>();
            text.text = value; text.font = font; text.fontSize = size;
            text.color = color; text.alignment = align; text.raycastTarget = false; text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; return text;
        }

        AudioClip Tone(float frequency, float duration, bool up)
        {
            const int rate = 22050; var samples = new float[(int)(rate * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float x = (float)i / rate;
                float f = up ? frequency + 140 * x : frequency - 220 * Mathf.Clamp01(x / .4f);
                samples[i] = Mathf.Sin(2 * Mathf.PI * f * x) * .14f * Mathf.Sin(Mathf.PI * i / samples.Length);
            }
            var clip = AudioClip.Create("EVA tone", samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        void OnDestroy() { Dismiss(); if (chime) Destroy(chime); if (alarmTone) Destroy(alarmTone); }
    }
}