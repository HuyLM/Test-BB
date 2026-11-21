using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PopJet
{
    /// <summary>
    /// Chuyển màn hình (GDD §10.1): một quả bóng to phình kín màn hình, đổi màn sau lưng nó, rồi bóng nổ thành confetti
    /// để lộ màn mới (khoảng 0.5 giây). Chơi lại thì không dùng (giữ nhịp nhanh).
    /// Giảm chuyển động (§10.7): thay bằng mờ dần, không phình, không confetti.
    /// Canvas riêng DontDestroyOnLoad để sống qua lần load scene. Unscaled time.
    /// </summary>
    public class ScreenTransition : MonoBehaviour
    {
        /// <summary>Tắt thì đổi màn ngay (smoke test tắt để không phải chờ).</summary>
        public static bool Enabled = true;
        static Sprite circle;
        static ScreenTransition instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Enabled = true; instance = null; }

        public static bool IsRunning => instance != null && instance.running;

        /// <summary>GameScreens truyền sprite tròn (bóng, confetti).</summary>
        public static void Configure(Sprite circleSprite) { if (circleSprite != null) circle = circleSprite; }

        /// <summary>Phủ màn hình, gọi change() (thường là load scene), rồi mở ra.</summary>
        public static void Play(Action change)
        {
            if (!Enabled || IsRunning) { change?.Invoke(); return; }
            if (instance == null) Build();
            instance.StartCoroutine(instance.Run(change));
        }

        static readonly Color[] Party =
        {
            new Color(1f, 0.824f, 0.247f), new Color(0.81f, 0.91f, 0.87f), new Color(0.95f, 0.77f, 0.69f),
            new Color(0.93f, 0.33f, 0.3f), new Color(0.36f, 0.62f, 0.92f),
        };
        const int ConfettiCount = 28;

        bool running;
        Image cover, fade;
        RectTransform root;
        Image[] confetti;
        Vector2[] vel;

        static void Build()
        {
            var go = new GameObject("[ScreenTransition]", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(go);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;
            instance = go.AddComponent<ScreenTransition>();
            instance.root = (RectTransform)go.transform;
            instance.fade = UiKit.Image("Fade", go.transform, null, new Color(0.16f, 0.2f, 0.28f, 0f), true);
            UiKit.Stretch(instance.fade.rectTransform);
            instance.cover = UiKit.Image("Cover", go.transform, circle, new Color(0.93f, 0.33f, 0.3f), true);
            UiKit.Place(instance.cover.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * 3200f);
            var shine = UiKit.Image("Shine", instance.cover.transform, circle, new Color(1f, 1f, 1f, 0.35f));
            UiKit.Place(shine.rectTransform, new Vector2(0.32f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 320f));
            instance.confetti = new Image[ConfettiCount];
            instance.vel = new Vector2[ConfettiCount];
            for (int i = 0; i < ConfettiCount; i++)
            {
                var c = UiKit.Image("Confetti_" + i, go.transform, i % 3 == 0 ? circle : null, Party[i % Party.Length]);
                UiKit.Place(c.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, i % 3 == 0 ? new Vector2(34f, 34f) : new Vector2(22f, 44f));
                instance.confetti[i] = c;
            }
            instance.HideAll();
        }

        void HideAll()
        {
            cover.gameObject.SetActive(false);
            fade.gameObject.SetActive(false);
            foreach (var c in confetti) c.gameObject.SetActive(false);
        }

        IEnumerator Run(Action change)
        {
            running = true;
            bool calm = GameSettings.ReduceMotion;
            float t = 0f;
            if (calm)
            {
                fade.gameObject.SetActive(true);
                while (t < 0.15f) { t += Time.unscaledDeltaTime; fade.color = WithA(fade.color, t / 0.15f); yield return null; }
            }
            else
            {
                GameAudio.UiPop();
                cover.gameObject.SetActive(true);
                while (t < 0.25f)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / 0.25f);
                    cover.rectTransform.localScale = Vector3.one * Mathf.Max(0.02f, k * k * (3f - 2f * k) + Mathf.Sin(k * Mathf.PI) * 0.05f);
                    yield return null;
                }
            }
            change?.Invoke();
            yield return null; // scene mới Awake/Start
            yield return null;
            t = 0f;
            if (calm)
            {
                while (t < 0.15f) { t += Time.unscaledDeltaTime; fade.color = WithA(fade.color, 1f - t / 0.15f); yield return null; }
            }
            else
            {
                cover.gameObject.SetActive(false);
                GameAudio.Pop();
                for (int i = 0; i < confetti.Length; i++)
                {
                    float a = i * Mathf.PI * 2f / confetti.Length + (i % 2) * 0.3f;
                    vel[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (1100f + (i * 37 % 7) * 120f);
                    confetti[i].rectTransform.anchoredPosition = Vector2.zero;
                    confetti[i].gameObject.SetActive(true);
                }
                while (t < 0.5f)
                {
                    float dt = Time.unscaledDeltaTime;
                    t += dt;
                    for (int i = 0; i < confetti.Length; i++)
                    {
                        vel[i] += Vector2.down * 2600f * dt;
                        vel[i] *= 1f - 2f * dt;
                        var r = confetti[i].rectTransform;
                        r.anchoredPosition += vel[i] * dt;
                        r.localRotation = Quaternion.Euler(0f, 0f, t * 720f * (i % 2 == 0 ? 1f : -1f));
                        confetti[i].color = WithA(confetti[i].color, 1f - t / 0.5f);
                    }
                    yield return null;
                }
            }
            HideAll();
            foreach (var c in confetti) c.color = WithA(c.color, 1f);
            running = false;
        }

        static Color WithA(Color c, float a) { c.a = Mathf.Clamp01(a); return c; }
    }
}
