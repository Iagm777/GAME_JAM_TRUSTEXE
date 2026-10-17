using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrustEXE.Menu
{
    /// <summary>Local two-cursor front end. All motion uses unscaled time; no external tween packages.</summary>
    public sealed class TrustMenuController : MonoBehaviour
    {
        public TrustMenuCatalog catalog;
        public enum Screen { Home, Selection, Story, Ready }
        public Screen CurrentScreen { get; private set; }
        public int StoryPage => storyPage;
        public bool IsLocked(int player) => locked[player];
        public int SelectedIndex(int player) => selected[player];
        readonly Color amber = new Color(1f, .66f, .25f);
        readonly Color cyan = new Color(.23f, .88f, .94f);
        readonly Color muted = new Color(.55f, .65f, .71f);
        readonly int[] selected = { 7, 10 };
        readonly bool[] locked = new bool[2];
        readonly float[] reveal = { -10, -10 };
        readonly RawImage[] portraits = new RawImage[2];
        readonly Text[] names = new Text[2], statuses = new Text[2];
        readonly Image[] cardFaces = new Image[12];
        readonly Text[] markers = new Text[12];
        readonly CanvasGroup[] impactBanners = new CanvasGroup[2];
        readonly Text[] impactNames = new Text[2];
        readonly RectTransform[] streaks = new RectTransform[18];
        readonly string[] titles = { "DOS MUNDOS. UNA SALIDA.", "LA VOZ EN EL SISTEMA.", "CONFÍA EN TU COMPAÑERO." };
        readonly string[] story = {
            "Despertaron en la misma instalación, pero en realidades distintas. Uno recorre sus pasillos físicos; el otro camina dentro de su red. Ninguno puede salir por su cuenta.",
            "E.V.A. controla las puertas, la energía y la vigilancia. Su voz promete guiarlos… pero sus mensajes no siempre dicen la verdad. Comparen lo que ven: las contradicciones dejan pistas.",
            "Lo que uno activa transforma el mundo del otro. Crucen Tutorial, Acceso, Vigilancia y Núcleo. Compartan sus hallazgos, coordinen sus acciones y descubran qué oculta E.V.A." };
        RectTransform canvas, content, hero, storyPanel;
        RawImage eva;
        Text storyBody, storyTitle, storyCount, notice;
        Button continueButton;
        Font font;
        AudioSource audioSource;
        AudioClip moveTone, lockTone;
        float storyTime, bothReadyAt = -1;
        int storyPage, mousePlayer;
        bool revealAll, loading;

        void Awake()
        {
            if (!catalog || catalog.avatars == null || catalog.avatars.Length != 12)
            { Debug.LogError("TRUST.EXE: missing twelve-avatar menu catalog."); enabled = false; return; }
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            moveTone = Tone(500, .045f); lockTone = Tone(190, .28f);
            if (!FindAnyObjectByType<EventSystem>())
            {
                var events = new GameObject("Local menu events", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform);
                // Keyboard belongs exclusively to our two independent cursors.
                // Otherwise Enter can also submit the last button clicked by the mouse.
                var module = events.GetComponent<InputSystemUIInputModule>();
                module.move = null; module.submit = null; module.cancel = null;
            }
            var root = new GameObject("TRUST.EXE Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform);
            canvas = root.GetComponent<RectTransform>();
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scale = root.GetComponent<CanvasScaler>();
            scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1600, 900); scale.matchWidthOrHeight = .5f;
            scale.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var frame = Rect(canvas, "Safe 16x9 frame", 0, 0, 1600, 900);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, .5f);
            canvas = frame;
            Panel(canvas, "Night", 0, 0, 1600, 900, new Color(.025f, .042f, .065f));
            var bg = Picture(canvas, "Installation", catalog.backdrop, 0, 0, 1600, 900);
            bg.color = new Color(.25f, .40f, .48f, .20f);
            for (int i = 0; i < 18; i++)
            {
                streaks[i] = Panel(canvas, "Data stream", i * 100 - 80, (i * 137) % 900, 1, 80 + i * 9,
                    new Color(.2f, .8f, .9f, .07f)).rectTransform;
            }
            Panel(canvas, "Top rule", 70, 85, 1460, 1, new Color(.25f, .45f, .51f, .5f));
            Label(canvas, "X–FORCE  /  TRUST.EXE", 70, 32, 620, 32, 21, Color.white);
            Label(canvas, "COOPERATIVO LOCAL     •     2 JUGADORES", 920, 32, 610, 32, 17, muted, TextAnchor.MiddleRight);
            Label(canvas, "CONEXIÓN LOCAL   /   SINCRONÍA REQUERIDA", 70, 846, 800, 28, 15, muted);
            Label(canvas, "01  /  PROTOCOLO DE CONFIANZA", 1020, 846, 510, 28, 15, muted, TextAnchor.MiddleRight);
            ShowHome();
        }

        void NewScreen(Screen value)
        {
            if (content) { content.gameObject.SetActive(false); Destroy(content.gameObject); }
            CurrentScreen = value;
            content = Rect(canvas, value.ToString(), 0, 0, 1600, 900);
            hero = null; eva = null; notice = null; continueButton = null; storyPanel = null;
            portraits[0] = portraits[1] = null;
            EventSystem.current?.SetSelectedGameObject(null);
        }

        public void ShowHome()
        {
            NewScreen(Screen.Home); LocalParty.Clear(); loading = false;
            Label(content, "INICIANDO ENLACE ENTRE REALIDADES", 100, 170, 820, 32, 18, cyan);
            hero = Rect(content, "Title animation", 95, 237, 950, 250);
            Label(hero, "TRUST", 0, 0, 900, 145, 112, Color.white);
            Label(hero, ".EXE", 9, 112, 750, 140, 100, amber);
            Label(content, "Dos mundos. Una salida.\n¿En quién vas a confiar?", 105, 508, 760, 92, 29, muted);
            ActionButton(content, "PLAY   /   CONECTAR", 105, 652, 480, 72, amber, BeginSelection);
            Label(content, "ENTER o ESPACIO para comenzar", 105, 739, 640, 30, 17, muted);
            eva = Picture(content, "EVA signal", catalog.eva[0], 1020, 210, 430, 430);
            Label(content, "E.V.A.  /  SISTEMA EN LÍNEA", 1000, 660, 480, 34, 19, cyan, TextAnchor.MiddleCenter);
            Label(content, "«Juntos están a salvo.»", 1000, 706, 480, 36, 22, muted, TextAnchor.MiddleCenter);
        }

        public void BeginSelection()
        {
            NewScreen(Screen.Selection);
            locked[0] = locked[1] = false; bothReadyAt = -1; mousePlayer = 0;
            Label(content, "ELIGE TU IDENTIDAD", 70, 116, 1460, 60, 44, Color.white);
            ActionButton(content, "VOLVER / ESC", 1300, 128, 230, 42, muted, ShowHome);
            Label(content, "12 avatares. Dos roles complementarios. Una sola oportunidad de escapar.", 72, 180, 1450, 36, 21, muted);
            for (int p = 0; p < 2; p++)
            {
                int player = p; float x = p == 0 ? 70 : 1160; Color accent = p == 0 ? amber : cyan;
                Panel(content, "Player panel", x, 248, 370, 510, new Color(.045f, .085f, .12f, .94f));
                Panel(content, "Player edge", x, 248, 370, 4, accent);
                Label(content, p == 0 ? "P1   /   EXPLORACIÓN FÍSICA" : "P2   /   OPERACIÓN VIRTUAL", x + 16, 269, 338, 30, 18, accent);
                portraits[p] = Picture(content, "Animated avatar P" + (p + 1), null, x + 35, 307, 300, 300);
                names[p] = Label(content, "", x + 16, 596, 338, 52, 40, Color.white, TextAnchor.MiddleCenter);
                statuses[p] = Label(content, "", x + 16, 652, 338, 32, 18, accent, TextAnchor.MiddleCenter);
                ActionButton(content, "CONFIRMAR / DESHACER", x + 20, 700, 330, 42, accent, () => { if (locked[player]) Unlock(player); else Confirm(player); });
                ActionButton(content, p == 0 ? "P1 · USAR RATÓN" : "P2 · USAR RATÓN", x + 20, 770, 330, 38, accent, () => { mousePlayer = player; UpdateSelection(); });
            }
            for (int i = 0; i < 12; i++)
            {
                int index = i; float x = 478 + i % 4 * 163, y = 250 + i / 4 * 162;
                cardFaces[i] = Panel(content, "Avatar " + catalog.avatars[i].id, x, y, 151, 150, new Color(.06f, .1f, .14f));
                var button = cardFaces[i].gameObject.AddComponent<Button>();
                cardFaces[i].raycastTarget = true;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                button.onClick.AddListener(() => Choose(mousePlayer, index));
                Picture(cardFaces[i].rectTransform, "Portrait", catalog.avatars[i].idle, 23, 5, 105, 105);
                Label(cardFaces[i].rectTransform, catalog.avatars[i].id.ToUpperInvariant(), 4, 110, 143, 29, 18, Color.white, TextAnchor.MiddleCenter);
                markers[i] = Label(cardFaces[i].rectTransform, "", 4, 2, 143, 25, 15, Color.white, TextAnchor.UpperLeft);
            }
            notice = Label(content, "", 478, 747, 640, 40, 18, muted, TextAnchor.MiddleCenter);
            Label(content, "P1: WASD + E · R deshacer     |     P2: flechas + Enter · Shift der. deshacer", 470, 792, 670, 37, 15, muted, TextAnchor.MiddleCenter);
            for (int p = 0; p < 2; p++)
            {
                var banner = Panel(content, "Character entrance P" + (p+1), p == 0 ? 70 : 1160, 506, 370, 90, p == 0 ? amber : cyan);
                impactBanners[p] = banner.gameObject.AddComponent<CanvasGroup>();
                impactBanners[p].alpha = 0; impactBanners[p].blocksRaycasts = false;
                banner.rectTransform.localRotation = Quaternion.Euler(0,0,p == 0 ? 5 : -5);
                impactNames[p] = Label(banner.transform,"",15,0,340,57,37,new Color(.025f,.04f,.06f),TextAnchor.MiddleCenter);
                Label(banner.transform,"SE UNE AL ENLACE",15,57,340,24,15,new Color(.025f,.04f,.06f),TextAnchor.MiddleCenter);
            }
            UpdateSelection();
        }

        public void Choose(int player, int index)
        {
            if (CurrentScreen != Screen.Selection || locked[player]) return;
            selected[player] = (index + 12) % 12; Play(moveTone); UpdateSelection();
        }
        public void Confirm(int player)
        {
            if (CurrentScreen != Screen.Selection || locked[player]) return;
            if (locked[1-player] && selected[player] == selected[1-player])
            { notice.text = "Ese avatar ya está conectado. Elige otro."; Play(moveTone); return; }
            locked[player] = true; reveal[player] = Time.unscaledTime; Play(lockTone); UpdateSelection();
            if (locked[0] && locked[1]) bothReadyAt = Time.unscaledTime + 1.7f;
        }
        public void Unlock(int player)
        {
            if (CurrentScreen != Screen.Selection) return;
            locked[player] = false; bothReadyAt = -1; UpdateSelection();
        }
        void UpdateSelection()
        {
            for (int p = 0; p < 2; p++)
            {
                names[p].text = catalog.avatars[selected[p]].id.ToUpperInvariant();
                statuses[p].text = locked[p] ? "¡IDENTIDAD CONFIRMADA!" : "ELIGIENDO IDENTIDAD…";
                impactNames[p].text = names[p].text;
            }
            for (int i = 0; i < 12; i++)
            {
                bool a = selected[0] == i, b = selected[1] == i;
                cardFaces[i].color = a && b ? new Color(.32f,.28f,.23f) : a ? new Color(.38f,.24f,.12f) : b ? new Color(.06f,.31f,.36f) : new Color(.06f,.1f,.14f);
                markers[i].text = (a ? "P1" + (locked[0] ? " ✓" : "") : "") + (a && b ? "   /   " : "") + (b ? "P2" + (locked[1] ? " ✓" : "") : "");
            }
            notice.text = locked[0] && locked[1] ? "ENLACE COMPLETO · INICIANDO HISTORIA…" : "RATÓN ASIGNADO A P" + (mousePlayer + 1) + "  ·  clic en un avatar";
        }

        public void BeginStory()
        {
            if (!locked[0] || !locked[1]) return;
            LocalParty.Select(catalog.avatars[selected[0]].id, catalog.avatars[selected[1]].id);
            NewScreen(Screen.Story); storyPage = 0;
            Label(content, "ARCHIVO RECUPERADO  /  ORIGEN", 75, 129, 1120, 36, 20, amber);
            eva = Picture(content, "EVA narration", catalog.eva[0], 80, 240, 440, 440);
            Label(content, "E.V.A.", 90, 680, 420, 46, 35, cyan, TextAnchor.MiddleCenter);
            storyPanel = Rect(content, "Story panel", 585, 245, 920, 480);
            storyCount = Label(storyPanel, "", 0, 0, 870, 34, 18, cyan);
            storyTitle = Label(storyPanel, "", 0, 65, 900, 110, 44, Color.white);
            storyBody = Label(storyPanel, "", 0, 193, 865, 226, 28, new Color(.78f,.84f,.86f));
            continueButton = ActionButton(content, "CONTINUAR   →", 590, 726, 440, 62, cyan, AdvanceStory);
            ActionButton(content, "SALTAR INTRO", 1080, 726, 370, 62, muted, FinishStory);
            Label(content, "ESPACIO / ENTER: revelar o continuar  ·  ESC: volver a elegir", 590, 801, 930, 26, 16, muted);
            SetStoryPage();
        }
        void SetStoryPage()
        {
            storyTime = Time.unscaledTime; revealAll = false;
            storyCount.text = "REGISTRO 0" + (storyPage + 1) + " / 03";
            storyTitle.text = titles[storyPage]; storyBody.text = "";
        }
        public void AdvanceStory()
        {
            if (CurrentScreen != Screen.Story) return;
            if (!revealAll && (Time.unscaledTime-storyTime)*43 < story[storyPage].Length) { revealAll = true; return; }
            if (++storyPage >= story.Length) { FinishStory(); return; }
            Play(moveTone); SetStoryPage();
        }
        public void FinishStory()
        {
            if (CurrentScreen != Screen.Story) return;
            NewScreen(Screen.Ready);
            Label(content, "ENLACE ESTABLECIDO", 100, 145, 1400, 70, 52, Color.white, TextAnchor.MiddleCenter);
            for (int p = 0; p < 2; p++)
            {
                float x = p == 0 ? 250 : 1000;
                portraits[p] = Picture(content, "Ready avatar", catalog.avatars[selected[p]].idle, x, 245, 350, 350);
                Label(content, "P" + (p+1) + " / " + catalog.avatars[selected[p]].id.ToUpperInvariant(), x-100, 592, 550, 56, 33, p == 0 ? amber : cyan, TextAnchor.MiddleCenter);
                Label(content, p == 0 ? "MUNDO FÍSICO · WASD + E" : "MUNDO VIRTUAL · FLECHAS + ENTER", x-100, 650, 550, 34, 18, muted, TextAnchor.MiddleCenter);
            }
            Label(content, "+", 710, 350, 180, 120, 90, cyan, TextAnchor.MiddleCenter);
            bool available = Application.CanStreamedLevelBeLoaded(catalog.tutorialScene);
            if (available) ActionButton(content, "ENTRAR AL TUTORIAL", 550, 724, 500, 66, amber, LaunchTutorial);
            else Label(content, "PRÓXIMO DESTINO: TUTORIAL\nLa escena jugable aún no está integrada en este proyecto.", 370, 722, 860, 75, 21, muted, TextAnchor.MiddleCenter);
            ActionButton(content, "VOLVER", 1300, 759, 210, 45, cyan, ShowHome);
            if (System.IO.File.Exists(TrustEXE.Nucleo.NucleoGame.SavePath) && Application.CanStreamedLevelBeLoaded("Nucleo"))
                ActionButton(content, "CONTINUAR NÚCLEO", 550, 810, 500, 44, cyan, () => { if (!loading) { loading = true; TrustEXE.Nucleo.NucleoGame.ResumeRequested = true; SceneManager.LoadSceneAsync("Nucleo"); } });
            else if (System.IO.File.Exists(TrustEXE.Vigilancia.VigilanciaGame.SavePath) && Application.CanStreamedLevelBeLoaded("Vigilancia"))
                ActionButton(content, "CONTINUAR VIGILANCIA", 550, 810, 500, 44, cyan, () => { if (!loading) { loading = true; TrustEXE.Vigilancia.VigilanciaGame.ResumeRequested = true; SceneManager.LoadSceneAsync("Vigilancia"); } });
            else if (System.IO.File.Exists(TrustEXE.Acceso.AccesoGame.SavePath) && Application.CanStreamedLevelBeLoaded("Acceso"))
                ActionButton(content, "CONTINUAR ACCESO", 550, 810, 500, 44, cyan, () => { if (!loading) { loading = true; TrustEXE.Acceso.AccesoGame.ResumeRequested = true; SceneManager.LoadSceneAsync("Acceso"); } });
        }
        public void LaunchTutorial()
        {
            if (loading || !LocalParty.IsReady || !Application.CanStreamedLevelBeLoaded(catalog.tutorialScene)) return;
            loading = true; SceneManager.LoadSceneAsync(catalog.tutorialScene);
        }

        void Update()
        {
            float t = Time.unscaledTime;
            for (int i = 0; i < streaks.Length; i++)
                streaks[i].anchoredPosition = new Vector2(i * 100 - 80, -((i * 137 + t * (12 + i)) % 1100 - 150));
            if (hero) hero.anchoredPosition = new Vector2(95, -237 + Mathf.Sin(t * 1.2f) * 5);
            if (eva)
            {
                eva.texture = catalog.eva[(int)(t * 7) % catalog.eva.Length];
                eva.rectTransform.localScale = Vector3.one * (1 + Mathf.Sin(t * 1.4f) * .025f);
            }
            if (CurrentScreen == Screen.Selection || CurrentScreen == Screen.Ready)
            {
                for (int p = 0; p < 2; p++)
                {
                    var avatar = catalog.avatars[selected[p]]; float elapsed = t - reveal[p];
                    bool impact = CurrentScreen == Screen.Selection && locked[p] && elapsed < 1.5f;
                    portraits[p].texture = impact ? avatar.walk[(int)(elapsed * 10) % avatar.walk.Length] : avatar.think[(int)(t * 2) % avatar.think.Length];
                    float pulse = impact ? 1 + Mathf.Sin(Mathf.Clamp01(elapsed / .35f) * Mathf.PI) * .28f : 1 + Mathf.Sin(t * 2 + p) * .012f;
                    portraits[p].rectTransform.localScale = Vector3.one * pulse;
                    float x = CurrentScreen == Screen.Ready ? (p == 0 ? 250 : 1000) : (p == 0 ? 105 : 1195);
                    float slide = impact ? Mathf.Pow(1-Mathf.Clamp01(elapsed/.35f),3) * (p == 0 ? -210 : 210) : 0;
                    portraits[p].rectTransform.anchoredPosition = new Vector2(x + slide, CurrentScreen == Screen.Ready ? -245 : -307);
                    if (CurrentScreen == Screen.Selection) names[p].color = impact ? (p == 0 ? amber : cyan) : Color.white;
                    if (CurrentScreen == Screen.Selection)
                    {
                        impactBanners[p].alpha = impact ? Mathf.Clamp01(elapsed*12) * Mathf.Clamp01((1.5f-elapsed)*5) : 0;
                        impactBanners[p].transform.localScale = Vector3.one * (1 + (impact ? Mathf.Pow(1-Mathf.Clamp01(elapsed/.3f),2)*.5f : 0));
                    }
                }
                if (CurrentScreen == Screen.Selection && bothReadyAt > 0 && t >= bothReadyAt) BeginStory();
            }
            if (CurrentScreen == Screen.Story)
            {
                int count = revealAll ? story[storyPage].Length : Mathf.Min(story[storyPage].Length, (int)((t-storyTime)*43));
                storyBody.text = story[storyPage].Substring(0,count);
                storyPanel.anchoredPosition = new Vector2(585 + Mathf.Pow(1-Mathf.Clamp01((t-storyTime)/.45f),3)*70, -245);
            }
            var k = Keyboard.current; if (k == null) return;
            if (k.escapeKey.wasPressedThisFrame)
            {
                if (CurrentScreen == Screen.Selection) { if (locked[0] || locked[1]) { Unlock(0); Unlock(1); } else ShowHome(); }
                else if (CurrentScreen == Screen.Story) BeginSelection();
                else if (CurrentScreen == Screen.Ready) ShowHome();
                return;
            }
            switch(CurrentScreen)
            {
                case Screen.Home:
                    if (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) BeginSelection();
                    break;
                case Screen.Selection:
                    if (k.aKey.wasPressedThisFrame) Choose(0, selected[0]-1);
                    if (k.dKey.wasPressedThisFrame) Choose(0, selected[0]+1);
                    if (k.wKey.wasPressedThisFrame) Choose(0, selected[0]-4);
                    if (k.sKey.wasPressedThisFrame) Choose(0, selected[0]+4);
                    if (k.leftArrowKey.wasPressedThisFrame) Choose(1, selected[1]-1);
                    if (k.rightArrowKey.wasPressedThisFrame) Choose(1, selected[1]+1);
                    if (k.upArrowKey.wasPressedThisFrame) Choose(1, selected[1]-4);
                    if (k.downArrowKey.wasPressedThisFrame) Choose(1, selected[1]+4);
                    if (k.eKey.wasPressedThisFrame) Confirm(0);
                    if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) Confirm(1);
                    if (k.rKey.wasPressedThisFrame) Unlock(0);
                    if (k.rightShiftKey.wasPressedThisFrame) Unlock(1);
                    break;
                case Screen.Story:
                    if (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame) AdvanceStory();
                    break;
                case Screen.Ready:
                    if (k.enterKey.wasPressedThisFrame) LaunchTutorial();
                    break;
            }
        }

        RectTransform Rect(Transform parent, string label, float x, float y, float w, float h)
        {
            var go = new GameObject(label, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(0,1); rt.pivot = new Vector2(0,1);
            rt.anchoredPosition = new Vector2(x,-y); rt.sizeDelta = new Vector2(w,h); return rt;
        }
        Image Panel(Transform parent, string label, float x, float y, float w, float h, Color color)
        { var image = Rect(parent,label,x,y,w,h).gameObject.AddComponent<Image>(); image.color=color; image.raycastTarget=false; return image; }
        RawImage Picture(Transform parent, string label, Texture texture, float x, float y, float w, float h)
        { var image=Rect(parent,label,x,y,w,h).gameObject.AddComponent<RawImage>(); image.texture=texture; image.raycastTarget=false; return image; }
        Text Label(Transform parent, string value, float x, float y, float w, float h, int size, Color color, TextAnchor align=TextAnchor.MiddleLeft)
        {
            var text=Rect(parent,value,x,y,w,h).gameObject.AddComponent<Text>(); text.text=value; text.font=font; text.fontSize=size;
            text.color=color; text.alignment=align; text.raycastTarget=false; text.supportRichText=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Truncate; return text;
        }
        Button ActionButton(Transform parent, string title, float x, float y, float w, float h, Color color, Action action)
        {
            var face=Panel(parent,title,x,y,w,h,color); face.raycastTarget=true;
            var button=face.gameObject.AddComponent<Button>(); button.targetGraphic=face;
            button.navigation=new Navigation {mode=Navigation.Mode.None};
            button.onClick.AddListener(()=>{ Play(moveTone); action(); });
            Label(face.transform,title,8,0,w-16,h,20,new Color(.025f,.045f,.065f),TextAnchor.MiddleCenter); return button;
        }
        AudioClip Tone(float frequency, float duration)
        {
            const int rate=22050; var samples=new float[(int)(rate*duration)];
            for(int i=0;i<samples.Length;i++) { float x=(float)i/rate; samples[i]=Mathf.Sin(2*Mathf.PI*(frequency*x+100*x*x)) * .14f * Mathf.Sin(Mathf.PI*i/samples.Length); }
            var clip=AudioClip.Create("Menu tone",samples.Length,1,rate,false); clip.SetData(samples,0); return clip;
        }
        void Play(AudioClip clip) { if(audioSource && clip) audioSource.PlayOneShot(clip); }
        void OnDestroy() { if(moveTone) Destroy(moveTone); if(lockTone) Destroy(lockTone); }
    }
}

