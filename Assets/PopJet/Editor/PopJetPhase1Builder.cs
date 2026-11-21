using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace PopJet.EditorTools
{
    /// <summary>
    /// Giai đoạn 1 của GDD (§16.1): thiết lập mobile, quỹ đạo dự đoán, vòng khí, HUD (Retry, thanh khí,
    /// số cú chọc, lý do thua, kẹt), mặt bóng, dây bóng, dòng khí rò, rung máy, âm "thời gian dừng".
    /// - Build Sample Scene tự gọi SetupPhase1.
    /// - Menu "Add Phase 1 Features To Open Scene" áp dụng cho scene đang mở, không dựng lại (giữ chỉnh tay).
    /// Chạy nhiều lần không bị nhân đôi component hay HUD.
    /// </summary>
    public static partial class PopJetSceneBuilder
    {
        const string HapticsPluginPath = Root + "/Plugins/iOS/PopJetHaptics.mm";
        static Sprite ring, smile, roundRect;

        [MenuItem("Tools/PopJet/Apply Mobile Player Settings")]
        public static void ApplyMobilePlayerSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            AssetDatabase.SaveAssets();
            Debug.Log("[PopJet] Player Settings: Default Orientation = Portrait.");
        }

        [MenuItem("Tools/PopJet/Add Phase 1 Features To Open Scene")]
        public static void AddPhase1ToOpenScene()
        {
            var balloon = Object.FindAnyObjectByType<BalloonController>();
            var input = Object.FindAnyObjectByType<BalloonInput>();
            var level = Object.FindAnyObjectByType<LevelManager>();
            if (balloon == null || input == null || level == null)
            {
                Debug.LogError("[PopJet] Scene đang mở cần có BalloonController, BalloonInput và LevelManager.");
                return;
            }
            SetupPhase1(balloon, input, level, Object.FindAnyObjectByType<CinemachineCamera>());
            var scene = balloon.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PopJet] Đã thêm tính năng Giai đoạn 1 vào " + scene.path);
        }

        static void SetupPhase1(BalloonController balloon, BalloonInput input, LevelManager level, CinemachineCamera vcam)
        {
            EnsurePhase1Assets();
            ApplyMobilePlayerSettings();
            ConfigureHapticsPlugin();

            var balloonGo = balloon.gameObject;
            var visual = balloonGo.transform.Find("Visual");

            if (vcam != null)
            {
                var fit = GetOrAdd<CameraAspectFit>(vcam.gameObject);
                SetFloat(fit, "referenceOrthoSize", vcam.Lens.OrthographicSize > 0f ? vcam.Lens.OrthographicSize : FollowOrthoSize);
            }

            // Mũi tên thẳng cũ được thay bằng quỹ đạo dự đoán.
            var inputSo = new SerializedObject(input);
            var arrowProp = inputSo.FindProperty("flightArrow");
            if (arrowProp.objectReferenceValue is LineRenderer arrow)
            {
                arrow.gameObject.SetActive(false);
                arrowProp.objectReferenceValue = null;
                inputSo.ApplyModifiedPropertiesWithoutUndo();
            }

            SetRefs(GetOrAdd<TrajectoryPreview>(input.gameObject),
                ("input", input), ("balloon", balloon), ("dotSprite", circle), ("ghostSprite", ring), ("material", spriteMat));
            SetRefs(GetOrAdd<AirRing>(input.gameObject),
                ("input", input), ("balloon", balloon), ("material", spriteMat));

            SetRefs(GetOrAdd<BalloonFace>(balloonGo),
                ("visual", visual), ("input", input), ("circleSprite", circle), ("smileSprite", smile),
                ("squareSprite", square), ("material", spriteMat));
            SetRefs(GetOrAdd<BalloonString>(balloonGo), ("material", spriteMat));
            SetRefs(GetOrAdd<LeakStreamFX>(balloonGo),
                ("streamPrefab", MakeStreamPrefab()), ("holeSprite", circle), ("tapeSprite", square),
                ("spriteMaterial", spriteMat), ("hissLoop", MakeLoopNoiseClip("leak_loop", 1f, 31, 0.35f)));
            SetRefs(GetOrAdd<BalloonHaptics>(balloonGo), ("input", input));

            BuildMusic(input);
            BuildHud(balloon, level);
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        // ---------------- Asset ----------------

        static void EnsurePhase1Assets()
        {
            EnsureDir(ArtDir); EnsureDir(SfxDir); EnsureDir(PrefabDir);
            if (circle == null) circle = MakeSprite("Circle", 256, (x, y, s) => Disc(x, y, s, 0.5f));
            if (square == null) square = MakeSprite("Square", 32, (x, y, s) => 1f);
            if (dot == null) dot = MakeSprite("SoftDot", 64, (x, y, s) => SoftDot(x, y, s));
            ring = MakeSprite("Ring", 128, RingShape);
            smile = MakeSprite("Smile", 128, SmileShape);
            roundRect = MakeSprite("RoundRect", 64, RoundRectShape);
            SetSpriteBorder($"{ArtDir}/RoundRect.png", new Vector4(24f, 24f, 24f, 24f));
            roundRect = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/RoundRect.png");

            if (spriteMat == null)
                spriteMat = LoadMat("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat")
                            ?? AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            if (particleMat == null)
                particleMat = LoadMat("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat")
                              ?? spriteMat;
        }

        /// <summary>Vòng tròn rỗng, dùng cho bóng mờ ở điểm va chạm của quỹ đạo dự đoán.</summary>
        static float RingShape(float x, float y, int s)
        {
            float d = new Vector2(x / s - 0.5f, y / s - 0.5f).magnitude;
            return (0.035f - Mathf.Abs(d - 0.45f)) * s;
        }

        /// <summary>Nét cong hình chữ U cho miệng cười (lật dọc thành miệng mếu).</summary>
        static float SmileShape(float x, float y, int s)
        {
            float u = x / s, v = y / s;
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.9f));
            float stroke = (0.07f - Mathf.Abs(d - 0.5f)) * s;
            float cap = (0.6f - v) * s;
            return Mathf.Min(stroke, cap);
        }

        /// <summary>Hình chữ nhật bo góc 24 px, cắt 9-slice cho nút và ô thanh khí.</summary>
        static float RoundRectShape(float x, float y, int s)
        {
            const float r = 24f;
            float half = s * 0.5f;
            float dx = Mathf.Max(Mathf.Abs(x - half) - (half - r), 0f);
            float dy = Mathf.Max(Mathf.Abs(y - half) - (half - r), 0f);
            return r - Mathf.Sqrt(dx * dx + dy * dy);
        }

        static void SetSpriteBorder(string path, Vector4 border)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null || imp.spriteBorder == border) return;
            imp.spriteBorder = border;
            imp.SaveAndReimport();
        }

        static void ConfigureHapticsPlugin()
        {
            if (!(AssetImporter.GetAtPath(HapticsPluginPath) is PluginImporter imp)) return;
            imp.SetCompatibleWithAnyPlatform(false);
            imp.SetCompatibleWithEditor(false);
            imp.SetCompatibleWithPlatform(BuildTarget.Android, false);
            imp.SetCompatibleWithPlatform(BuildTarget.iOS, true);
            imp.SaveAndReimport();
        }

        static ParticleSystem MakeStreamPrefab()
        {
            string path = $"{PrefabDir}/LeakStream.prefab";
            var go = new GameObject("LeakStream");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.13f);
            main.startColor = new Color(1f, 1f, 1f, 0.8f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            var em = ps.emission;
            em.rateOverTime = 30f; // LeakStreamFX chỉnh theo lực còn lại của lỗ rò

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.02f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // phát theo +Y local

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 1.8f));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{ArtDir}/PopJetParticle.mat") ?? particleMat;
            r.sortingOrder = 29;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<ParticleSystem>();
        }

        static AudioClip MakeLoopNoiseClip(string name, float seconds, int seed, float volume)
        {
            // Tiếng "xì" đều, không có envelope để loop liền mạch.
            var rnd = new System.Random(seed);
            int n = (int)(seconds * Rate);
            var data = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float white = (float)(rnd.NextDouble() * 2 - 1);
                lp += (white - lp) * 0.5f;
                data[i] = lp * volume;
            }
            return WriteWav(name, data);
        }

        static AudioClip MakeMusicLoop(string name)
        {
            // Placeholder: vòng lặp 8 giây, 120 BPM, nốt móc đơn trên âm giai ngũ cung Đô trưởng + bass.
            float[] scale = { 261.63f, 293.66f, 329.63f, 392.00f, 440.00f, 523.25f };
            int[] pattern = { 0, 2, 4, 2, 3, 2, 1, 2, 0, 2, 4, 5, 4, 3, 2, 1, 0, 2, 4, 2, 3, 4, 5, 4, 3, 2, 1, 2, 0, 1, 2, 0 };
            float[] bass = { 130.81f, 98.00f, 110.00f, 87.31f };
            const float noteLen = 0.25f, barLen = 2f, seconds = 8f;
            int n = (int)(seconds * Rate);
            var data = new float[n];
            const float twoPi = Mathf.PI * 2f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                int ni = (int)(t / noteLen);
                float nt = t - ni * noteLen;
                float f = scale[pattern[ni % pattern.Length]];
                float env = Mathf.Exp(-nt * 9f) * Mathf.Min(1f, nt * 200f) * Mathf.Min(1f, (noteLen - nt) * 200f);
                float lead = (Mathf.Sin(twoPi * f * t) + 0.3f * Mathf.Sin(twoPi * 2f * f * t)) * env * 0.18f;

                int bi = (int)(t / barLen);
                float bt = t - bi * barLen;
                float benv = Mathf.Exp(-bt * 1.2f) * Mathf.Min(1f, bt * 50f) * Mathf.Min(1f, (barLen - bt) * 20f);
                float b = Mathf.Sin(twoPi * bass[bi % bass.Length] * t) * benv * 0.16f;
                data[i] = lead + b;
            }
            return WriteWav(name, data);
        }

        // ---------------- Nhạc + "thời gian dừng" ----------------

        static void BuildMusic(BalloonInput input)
        {
            var existing = Object.FindAnyObjectByType<TimeFreezeAudio>();
            if (existing != null) { SetRefs(existing, ("input", input)); return; }

            var go = new GameObject("Music");
            var src = go.AddComponent<AudioSource>();
            src.clip = MakeMusicLoop("music_house");
            src.loop = true;
            src.playOnAwake = true;
            src.volume = 0.3f; // starting value
            go.AddComponent<AudioLowPassFilter>();

            var sfxGo = new GameObject("FreezeSfx");
            sfxGo.transform.SetParent(go.transform, false);
            var sfx = sfxGo.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            var freeze = go.AddComponent<TimeFreezeAudio>();
            SetRefs(freeze, ("input", input), ("sfxSource", sfx),
                ("tickClip", MakeToneClip("aim_tick", 0.05f, 1600f, 1200f)),
                ("whooshClip", MakeNoiseClip("whoosh", 0.28f, 41, 0.45f)));
        }

        // ---------------- HUD (UGUI, Canvas Scaler 1080x1920, theo safe area) ----------------

        static readonly Color Ink = new Color(0.17f, 0.17f, 0.23f);

        static void BuildHud(BalloonController balloon, LevelManager level)
        {
            var existing = Object.FindAnyObjectByType<PopJetHud>();
            if (existing != null) { SetRefs(existing, ("balloon", balloon), ("level", level)); return; }
            EnsureEventSystem();
            var font = EnsureTmpFont();

            var canvasGo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // starting value: theo chiều ngang (GDD §10.6)

            var safe = UiRect("SafeArea", canvasGo.transform);
            safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // Nút Retry góc trên phải: cao 132 = 44 pt ở màn 1080 px mật độ 3x (Apple HIG).
            var retry = UiRect("RetryButton", safe);
            Place(retry, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -32f), new Vector2(240f, 132f));
            var retryImg = retry.gameObject.AddComponent<Image>();
            retryImg.sprite = roundRect;
            retryImg.type = Image.Type.Sliced;
            var btn = retry.gameObject.AddComponent<Button>();
            btn.targetGraphic = retryImg;
            var retryText = UiText("Label", retry, font, 48, Ink, TextAlignmentOptions.Center, false);
            var rt = retryText.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            retryText.text = GameText.Retry;

            var stuck = UiText("StuckLabel", safe, font, 40, Color.white, TextAlignmentOptions.Right, true);
            Place(stuck.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -176f), new Vector2(360f, 60f));
            stuck.text = GameText.Stuck;
            stuck.gameObject.SetActive(false);

            // Bên phải nút Tạm dừng (GameScreens dựng nút đó ở góc trên trái, rộng 132).
            var pokes = UiText("PokeLabel", safe, font, 44, Color.white, TextAlignmentOptions.Left, true);
            Place(pokes.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(192f, -52f), new Vector2(360f, 90f));
            pokes.text = GameText.Pokes(0);

            var airLabel = UiText("AirLabel", safe, font, 36, Color.white, TextAlignmentOptions.Right, true);
            Place(airLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(-276f, -190f), new Vector2(140f, 44f));
            airLabel.text = GameText.Air;

            var bar = UiRect("AirBar", safe);
            Place(bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(520f, 44f));
            SetRefs(bar.gameObject.AddComponent<AirBarUI>(), ("balloon", balloon), ("slotSprite", roundRect));

            var fail = UiText("FailLabel", safe, font, 72, Color.white, TextAlignmentOptions.Center, true);
            Place(fail.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 120f));
            fail.gameObject.SetActive(false);

            var hud = canvasGo.AddComponent<PopJetHud>();
            SetRefs(hud, ("balloon", balloon), ("level", level), ("retryButton", btn), ("retryLabel", retryText),
                ("pokeLabel", pokes), ("failLabel", fail), ("stuckLabel", stuck));
        }

        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            // Project chỉ bật Input System mới: phải dùng InputSystemUIInputModule, không dùng StandaloneInputModule.
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        static RectTransform UiRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static TextMeshProUGUI UiText(string name, Transform parent, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align, bool outline)
        {
            var rt = UiRect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            // Viền chữ: dùng chung một material preset (không tạo material instance trong scene).
            if (outline && font != null) t.fontSharedMaterial = TmpOutlineMaterial(font);
            return t;
        }

        // ---------------- TextMeshPro (phần 2.4) ----------------

        static Material TmpOutlineMaterial(TMP_FontAsset font)
        {
            string path = ArtDir + "/TMP_Outline.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(font.material);
                AssetDatabase.CreateAsset(m, path);
            }
            m.CopyPropertiesFromMaterial(font.material);
            m.SetTexture("_MainTex", font.material.GetTexture("_MainTex"));
            m.EnableKeyword("OUTLINE_ON");
            m.SetFloat("_OutlineWidth", 0.22f);
            m.SetColor("_OutlineColor", new Color(Ink.r, Ink.g, Ink.b, 0.9f));
            EditorUtility.SetDirty(m);
            return m;
        }

        const string TmpFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        /// <summary>Import TMP Essential Resources nếu chưa có (font SDF mặc định + TMP Settings), trả về font mặc định.</summary>
        static TMP_FontAsset EnsureTmpFont()
        {
            // TMP Essentials vẫn cần (TMP Settings, shader SDF); chữ trong game dùng Nunito nếu có file TTF.
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
            if (font != null) return BodyFont() ?? font;
            var pkg = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
            string unitypackage = pkg != null ? pkg.resolvedPath + "/Package Resources/TMP Essential Resources.unitypackage" : null;
            if (unitypackage == null || !File.Exists(unitypackage))
            {
                Debug.LogError("[PopJet] Không tìm thấy TMP Essential Resources.unitypackage trong com.unity.ugui.");
                return null;
            }
            AssetDatabase.ImportPackage(unitypackage, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
            if (font == null) Debug.LogError("[PopJet] Đã import TMP Essentials nhưng chưa thấy " + TmpFontPath);
            return BodyFont() ?? font;
        }
    }
}
