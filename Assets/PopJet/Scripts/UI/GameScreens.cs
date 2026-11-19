using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PopJet
{
    /// <summary>
    /// Phần 2.4 + 2.5 của GDD: HUD đầy đủ và mọi màn hình, dựng bằng code lúc chạy (UiKit).
    /// - HUD: nút Tạm dừng (trên trái), ô Sao (sáng khi nhặt), nút Xem màn (dưới trái), dòng gợi ý tutorial,
    ///   bảng 3 mục tiêu đầu màn (§10.3), camera giới thiệu (LevelCameraDirector).
    /// - Màn hình: Tạm dừng, Kết quả, Trang chủ, Bản đồ, Cài đặt (§10.2, §10.4, §10.5).
    /// Mọi màn hình nằm dưới "Screens" của canvas HUD, nền tối chặn chạm xuống màn chơi (BalloonInput bỏ qua
    /// chạm trên UI). Trang chủ/Bản đồ/Cài đặt dừng thế giới bằng lý do Menu, Tạm dừng bằng lý do Pause.
    /// Mọi hoạt ảnh chạy bằng unscaled time.
    /// </summary>
    public class GameScreens : MonoBehaviour
    {
        [SerializeField] BalloonController balloon;
        [SerializeField] LevelManager level;
        [SerializeField] LevelSession session;
        [SerializeField] LevelLoader loader;
        [SerializeField] LevelCameraDirector director;
        [Tooltip("Vùng an toàn của HUD (SafeAreaFitter). Nút HUD mới đặt trong này.")]
        [SerializeField] RectTransform safeArea;
        [SerializeField] TMP_FontAsset font;
        [Tooltip("Material TMP có viền cho chữ nằm trên màn chơi.")]
        [SerializeField] Material outlineMaterial;
        [SerializeField] Sprite roundRect;
        [SerializeField] Sprite starSprite;
        [Tooltip("Về đích bao lâu thì hiện màn Kết quả (giây, unscaled). Starting value.")]
        [SerializeField] float resultDelay = 0.8f;
        [Tooltip("Bảng mục tiêu đầu màn hiện bao lâu (giây). GDD §10.3.")]
        [SerializeField] float objectivesSeconds = 2f;
        [Tooltip("Dòng gợi ý tutorial hiện tối đa bao lâu (giây); chọc lần đầu thì mờ đi sớm hơn.")]
        [SerializeField] float hintSeconds = 6f;

        // HUD
        Button pauseButton, lookButton;
        Image starChip;
        TextMeshProUGUI hintLabel;
        RectTransform objectives;
        float hintShownAt = -1f;

        // Màn hình
        RectTransform screens;
        RectTransform pauseScreen, resultScreen, homeScreen, mapScreen, settingsScreen;
        readonly Stack<RectTransform> backStack = new Stack<RectTransform>();
        RectTransform current;

        // Kết quả
        Image[] resultStars = new Image[3];
        TextMeshProUGUI resultPokes, resultCoins, resultCrown, resultTitle;
        Button resultNext;

        // Trang chủ / Bản đồ / Cài đặt
        TextMeshProUGUI homeCoins;
        RectTransform mapContent;
        ScrollRect mapScroll;
        readonly List<(TextMeshProUGUI label, System.Func<string> text)> toggleLabels = new List<(TextMeshProUGUI, System.Func<string>)>();

        bool introRunning, introSkip;

        public bool IsScreenOpen => current != null;
        public string CurrentScreen => current != null ? current.name : null;

        void Awake()
        {
            if (font != null) UiKit.Font = font;
            if (outlineMaterial != null) UiKit.OutlineMaterial = outlineMaterial;
            if (roundRect != null) UiKit.RoundRect = roundRect;
            if (safeArea == null) safeArea = (RectTransform)transform;
            BuildHud();
            screens = UiKit.Stretch(UiKit.Rect("Screens", transform));
            pauseScreen = BuildPause();
            resultScreen = BuildResult();
            homeScreen = BuildHome();
            mapScreen = BuildMap();
            settingsScreen = BuildSettings();
        }

        void OnEnable()
        {
            if (balloon != null) balloon.Poked += OnPoked;
            if (session != null)
            {
                session.StarCollected += OnStar;
                session.Completed += OnCompleted;
            }
        }

        void OnDisable()
        {
            if (balloon != null) balloon.Poked -= OnPoked;
            if (session != null)
            {
                session.StarCollected -= OnStar;
                session.Completed -= OnCompleted;
            }
        }

        void Start()
        {
            GameSettings.Apply();
            starChip.gameObject.SetActive(session != null && session.LevelHasStar);
            var catalog = loader != null ? loader.Catalog : null;
            if (GameFlow.ConsumeShowHome(catalog))
            {
                Open(homeScreen, false);
                return;
            }
            BeginLevel();
        }

        // ---------------------------------------------------------------- Đầu màn

        void BeginLevel()
        {
            int index = loader != null ? loader.Index : 0;
            if (!GameFlow.ConsumeIntro(index))
            {
                ShowHint();
                return;
            }
            objectives.gameObject.SetActive(true);
            if (director != null)
            {
                introRunning = true;
                introSkip = false;
                StartCoroutine(director.Intro(() => introSkip, () => { introRunning = false; ShowHint(); }));
            }
            else ShowHint();
            StartCoroutine(HideObjectives());
        }

        IEnumerator HideObjectives()
        {
            float t = 0f;
            while (t < objectivesSeconds && !introSkip) { t += Time.unscaledDeltaTime; yield return null; }
            objectives.gameObject.SetActive(false);
        }

        void ShowHint()
        {
            string hint = loader != null && loader.Level != null ? loader.Level.Hint : null;
            if (string.IsNullOrEmpty(hint)) return;
            hintLabel.text = hint;
            hintLabel.gameObject.SetActive(true);
            hintLabel.alpha = 1f;
            hintShownAt = Time.unscaledTime;
        }

        void Update()
        {
            if (introRunning && !introSkip && current == null)
            {
                // Chạm lần đầu thì bỏ qua giới thiệu (chạm này không bắt đầu ngắm: thế giới còn đang dừng vì Intro).
                PointerHelper.Poll();
                if (PointerHelper.Down) introSkip = true;
            }
            if (hintShownAt >= 0f)
            {
                float t = Time.unscaledTime - hintShownAt;
                if (t > hintSeconds) hintLabel.alpha = Mathf.MoveTowards(hintLabel.alpha, 0f, Time.unscaledDeltaTime * 2f);
                if (hintLabel.alpha <= 0f) { hintLabel.gameObject.SetActive(false); hintShownAt = -1f; }
            }
            if (lookButton != null)
                UiKit.ButtonLabel(lookButton).text = director != null && director.IsLooking ? GameText.Back : GameText.Look;
        }

        void OnPoked(int count)
        {
            // Cú chọc đầu: gợi ý đã làm xong việc, mờ dần sau 1.5 giây.
            if (count == 1 && hintShownAt >= 0f) hintShownAt = Mathf.Min(hintShownAt, Time.unscaledTime - hintSeconds + 1.5f);
        }

        void OnStar() { if (starChip != null) starChip.color = UiKit.Gold; }

        void OnCompleted(LevelResult r) { StartCoroutine(ShowResultLater(r)); }

        IEnumerator ShowResultLater(LevelResult r)
        {
            yield return new WaitForSecondsRealtime(resultDelay);
            if (director != null) director.SetLook(false);
            FillResult(r);
            Open(resultScreen, false);
        }

        // ---------------------------------------------------------------- Điều hướng

        void Open(RectTransform screen, bool pushCurrent)
        {
            if (pushCurrent && current != null) backStack.Push(current);
            else backStack.Clear();
            if (current != null) current.gameObject.SetActive(false);
            current = screen;
            if (screen == mapScreen) RefreshMap();
            if (screen == homeScreen) homeCoins.text = GameText.Coins + " " + SaveSystem.Data.coins;
            RefreshToggles();
            screen.gameObject.SetActive(true);
            ApplyTimeReasons();
        }

        void CloseAll()
        {
            backStack.Clear();
            if (current != null) current.gameObject.SetActive(false);
            current = null;
            ApplyTimeReasons();
        }

        void Back()
        {
            if (current != null) current.gameObject.SetActive(false);
            current = backStack.Count > 0 ? backStack.Pop() : null;
            if (current != null)
            {
                if (current == mapScreen) RefreshMap();
                RefreshToggles();
                current.gameObject.SetActive(true);
            }
            ApplyTimeReasons();
        }

        void ApplyTimeReasons()
        {
            bool pause = current == pauseScreen || (current == settingsScreen && backStack.Contains(pauseScreen))
                         || (current == mapScreen && backStack.Contains(pauseScreen));
            bool menu = current == homeScreen || current == mapScreen || current == settingsScreen;
            TimeController.Set(TimeReason.Pause, pause);
            TimeController.Set(TimeReason.Menu, menu && !pause);
            SaveSystem.SaveIfDirty();
        }

        // Các thao tác công khai (nút bấm, smoke test gọi bằng reflection).
        public void Pause()
        {
            if (current != null || (balloon != null && balloon.State == BalloonState.Goal)) return;
            if (director != null) director.SetLook(false);
            Open(pauseScreen, false);
        }

        public void Resume() { CloseAll(); }

        public void Retry()
        {
            CloseAll();
            if (level != null) level.Restart();
        }

        public void Next()
        {
            if (loader == null || !loader.HasNext) { Open(mapScreen, true); return; }
            CloseAll();
            if (level != null) level.LoadNext();
            else loader.LoadNext();
        }

        public void OpenMap() { Open(mapScreen, true); }
        public void OpenSettings() { Open(settingsScreen, true); }

        public void PlayFromHome()
        {
            int target = GameFlow.NextLevelToPlay(loader != null ? loader.Catalog : null);
            GameFlow.ForgetIntro();
            if (loader != null && target != loader.Index) { CloseAll(); loader.LoadLevel(target); return; }
            CloseAll();
            BeginLevel();
        }

        public void PlayLevel(int index)
        {
            if (loader == null || !loader.Catalog.IsUnlocked(index)) return;
            GameFlow.ForgetIntro();
            CloseAll();
            loader.LoadLevel(index);
        }

        public void GoHome()
        {
            CloseAll();
            GameFlow.RequestHome();
            if (level != null) level.Restart();
        }

        // ---------------------------------------------------------------- HUD

        void BuildHud()
        {
            // Nút Tạm dừng góc trên trái (§10.3), cùng cỡ với nút Retry: 132 = 44 pt.
            pauseButton = UiKit.Button("PauseButton", safeArea, "II", UiKit.Neutral, Pause, 56f);
            UiKit.Place((RectTransform)pauseButton.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -32f), new Vector2(132f, 132f));

            // Ô Sao: mờ = chưa nhặt, vàng = đã nhặt. Ẩn nếu màn không có Sao.
            starChip = UiKit.Image("StarChip", safeArea, starSprite, UiKit.StarOff);
            UiKit.Place(starChip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(560f, -48f), new Vector2(96f, 96f));

            // Nút Xem màn góc dưới trái (§10.3).
            lookButton = UiKit.Button("LookButton", safeArea, GameText.Look, UiKit.Neutral, () => { if (director != null && current == null && !introRunning) director.ToggleLook(); }, 48f);
            UiKit.Place((RectTransform)lookButton.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(32f, 48f), new Vector2(220f, 132f));

            hintLabel = UiKit.Label("HintLabel", safeArea, "", 52f, Color.white, TextAlignmentOptions.Center, true);
            UiKit.Place(hintLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(1000f, 90f));
            hintLabel.gameObject.SetActive(false);

            // Bảng 3 mục tiêu đầu màn: không chặn chạm (không phải popup).
            objectives = UiKit.Rect("Objectives", safeArea);
            UiKit.Place(objectives, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(820f, 420f));
            var bg = objectives.gameObject.AddComponent<Image>();
            bg.sprite = UiKit.RoundRect;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(UiKit.Ink.r, UiKit.Ink.g, UiKit.Ink.b, 0.78f);
            bg.raycastTarget = false;
            UiKit.Column(objectives, 12f, 36);
            objectives.gameObject.AddComponent<PopupAnim>();
            var title = UiKit.Label("Title", objectives, "", 72f, UiKit.Gold);
            UiKit.Size(title, 96f);
            UiKit.Size(UiKit.Label("Goal", objectives, GameText.GoalReach, 54f, Color.white), 74f);
            UiKit.Size(UiKit.Label("Star", objectives, GameText.GoalStar, 54f, Color.white), 74f);
            UiKit.Size(UiKit.Label("Par", objectives, "", 54f, Color.white), 74f);
            objectives.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            // Điền chữ bảng mục tiêu khi đã có dữ liệu màn (LevelLoader chạy trước, LevelSession.Start sau Awake).
            if (objectives != null && objectives.gameObject.activeSelf)
            {
                var def = loader != null ? loader.Level : null;
                objectives.Find("Title").GetComponent<TextMeshProUGUI>().text =
                    def != null ? def.Id + "  " + def.Title : "";
                objectives.Find("Star").gameObject.SetActive(session != null && session.LevelHasStar);
                objectives.Find("Par").GetComponent<TextMeshProUGUI>().text = GameText.GoalPar(session != null ? session.Par : 3);
            }
        }

        // ---------------------------------------------------------------- Tạm dừng (§10.4)

        RectTransform BuildPause()
        {
            var root = UiKit.Overlay("Pause", screens, UiKit.Dim);
            var card = UiKit.Card("Card", root, new Vector2(820f, 1180f));
            UiKit.Column(card, 28f, 56);
            UiKit.Size(UiKit.Label("Title", card, GameText.Paused, 96f, UiKit.Ink), 130f);
            UiKit.Size(UiKit.Button("Resume", card, GameText.Resume, UiKit.Primary, Resume), 150f);
            UiKit.Size(UiKit.Button("Retry", card, GameText.Retry, UiKit.Neutral, Retry), 132f);
            UiKit.Size(UiKit.Button("Map", card, GameText.Map, UiKit.Neutral, OpenMap), 132f);
            AddToggle(card, () => GameText.Sound + ": " + OnOff(SaveSystem.Data.settings.SoundOn),
                () => GameSettings.SetSound(!SaveSystem.Data.settings.SoundOn));
            AddToggle(card, () => GameText.Music + ": " + OnOff(SaveSystem.Data.settings.MusicOn),
                () => GameSettings.SetMusic(!SaveSystem.Data.settings.MusicOn));
            UiKit.Size(UiKit.Button("Settings", card, GameText.Settings, UiKit.Neutral, OpenSettings), 132f);
            root.gameObject.SetActive(false);
            return root;
        }

        // ---------------------------------------------------------------- Kết quả (§10.4)

        RectTransform BuildResult()
        {
            var root = UiKit.Overlay("Result", screens, UiKit.Dim);
            var card = UiKit.Card("Card", root, new Vector2(880f, 1220f));
            UiKit.Column(card, 22f, 56);
            resultTitle = UiKit.Label("Title", card, GameText.LevelComplete, 84f, UiKit.Ink);
            UiKit.Size(resultTitle, 120f);

            var row = UiKit.Rect("Stars", card);
            UiKit.Size(row, 230f);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = false;
            for (int i = 0; i < 3; i++)
            {
                resultStars[i] = UiKit.Image("Star" + i, row, starSprite, UiKit.StarOff);
                resultStars[i].rectTransform.sizeDelta = Vector2.one * (i == 1 ? 210f : 170f);
                resultStars[i].color = new Color(0f, 0f, 0f, 0.15f);
            }
            var caption = UiKit.Label("StarNames", card, "Goal        Star        Par", 40f, new Color(0.4f, 0.4f, 0.45f));
            UiKit.Size(caption, 50f);

            resultPokes = UiKit.Label("Pokes", card, "", 60f, UiKit.Ink);
            UiKit.Size(resultPokes, 80f);
            resultCoins = UiKit.Label("Coins", card, "", 54f, new Color(0.75f, 0.55f, 0.05f));
            UiKit.Size(resultCoins, 72f);
            resultCrown = UiKit.Label("Crown", card, GameText.Crown, 60f, UiKit.Orange);
            UiKit.Size(resultCrown, 72f);

            resultNext = UiKit.Button("Next", card, GameText.Next, UiKit.Primary, Next, 72f);
            UiKit.Size(resultNext, 170f);
            var bottom = UiKit.Rect("Row", card);
            UiKit.Size(bottom, 132f);
            var hb = bottom.gameObject.AddComponent<HorizontalLayoutGroup>();
            hb.spacing = 24f;
            hb.childControlWidth = hb.childControlHeight = true;
            hb.childForceExpandWidth = hb.childForceExpandHeight = true;
            UiKit.Button("Retry", bottom, GameText.Retry, UiKit.Neutral, Retry);
            UiKit.Button("Map", bottom, GameText.Map, UiKit.Neutral, OpenMap);
            root.gameObject.SetActive(false);
            return root;
        }

        void FillResult(LevelResult r)
        {
            bool[] got = { r.goal, r.star, r.underPar };
            for (int i = 0; i < 3; i++) StartCoroutine(PuffStar(resultStars[i], got[i], 0.15f + i * 0.25f));
            resultPokes.text = GameText.Pokes(r.pokes, r.par);
            resultPokes.color = r.underPar ? UiKit.Ink : UiKit.Orange;
            resultCoins.text = GameText.CoinsEarned(r.coinsEarned) + "   (" + SaveSystem.Data.coins + ")";
            resultCrown.gameObject.SetActive(r.crown);
            bool hasNext = loader != null && loader.HasNext;
            UiKit.ButtonLabel(resultNext).text = hasNext ? GameText.Next : GameText.Map;
            resultTitle.text = hasNext ? GameText.LevelComplete : GameText.AllDone;
        }

        /// <summary>Sao lần lượt "phồng" lên (§10.4).</summary>
        IEnumerator PuffStar(Image star, bool on, float delay)
        {
            star.color = new Color(0f, 0f, 0f, 0.15f);
            star.transform.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(delay);
            if (!on) yield break;
            star.color = UiKit.Gold;
            if (on) Haptics.Light();
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                float s = t < 0.18f ? Mathf.Lerp(0.2f, 1.25f, t / 0.18f) : Mathf.Lerp(1.25f, 1f, (t - 0.18f) / 0.12f);
                star.transform.localScale = Vector3.one * s;
                yield return null;
            }
            star.transform.localScale = Vector3.one;
        }

        // ---------------------------------------------------------------- Trang chủ (§10.2)

        RectTransform BuildHome()
        {
            var root = UiKit.Overlay("Home", screens, new Color(0.16f, 0.2f, 0.28f, 0.88f));
            var title = UiKit.Label("Title", root, GameText.GameTitle, 170f, Color.white, TextAlignmentOptions.Center, true);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 240f));
            homeCoins = UiKit.Label("Coins", root, "", 56f, UiKit.Gold, TextAlignmentOptions.Center, true);
            UiKit.Place(homeCoins.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 80f));

            var col = UiKit.Rect("Buttons", root);
            UiKit.Place(col, new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(760f, 640f));
            var v = UiKit.Column(col, 32f, 0);
            v.childAlignment = TextAnchor.LowerCenter;
            UiKit.Size(UiKit.Button("Play", col, GameText.Play, UiKit.Primary, PlayFromHome, 96f), 220f);
            UiKit.Size(UiKit.Button("Map", col, GameText.Map, UiKit.Neutral, OpenMap), 140f);
            UiKit.Size(UiKit.Button("Settings", col, GameText.Settings, UiKit.Neutral, OpenSettings), 140f);
            root.gameObject.SetActive(false);
            return root;
        }

        // ---------------------------------------------------------------- Bản đồ (§10.5)

        RectTransform BuildMap()
        {
            var root = UiKit.Overlay("Map", screens, new Color(0.16f, 0.2f, 0.28f, 0.96f));
            var title = UiKit.Label("Title", root, GameText.Map, 90f, Color.white, TextAlignmentOptions.Center, true);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(900f, 130f));
            var back = UiKit.Button("Back", root, GameText.Back, UiKit.Neutral, Back, 52f);
            UiKit.Place((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -60f), new Vector2(240f, 132f));

            var view = UiKit.Rect("Viewport", root);
            view.anchorMin = new Vector2(0f, 0f);
            view.anchorMax = new Vector2(1f, 1f);
            view.offsetMin = new Vector2(40f, 60f);
            view.offsetMax = new Vector2(-40f, -230f);
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0.001f); // nhận kéo cuộn

            mapContent = UiKit.Rect("Content", view);
            mapContent.anchorMin = new Vector2(0f, 1f);
            mapContent.anchorMax = new Vector2(1f, 1f);
            mapContent.pivot = new Vector2(0.5f, 1f);
            mapContent.sizeDelta = Vector2.zero;
            var v = UiKit.Column(mapContent, 24f, 12);
            v.childForceExpandWidth = true;
            var fit = mapContent.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            mapScroll = root.gameObject.AddComponent<ScrollRect>();
            mapScroll.viewport = view;
            mapScroll.content = mapContent;
            mapScroll.horizontal = false;
            mapScroll.movementType = ScrollRect.MovementType.Clamped;
            mapScroll.scrollSensitivity = 40f;
            root.gameObject.SetActive(false);
            return root;
        }

        void RefreshMap()
        {
            for (int i = mapContent.childCount - 1; i >= 0; i--) Destroy(mapContent.GetChild(i).gameObject);
            var catalog = loader != null ? loader.Catalog : null;
            if (catalog == null) return;

            int world = -1;
            RectTransform grid = null;
            for (int i = 0; i < catalog.Count; i++)
            {
                var def = catalog[i];
                if (def == null) continue;
                if (def.World != world)
                {
                    world = def.World;
                    var header = UiKit.Label("World" + world, mapContent, world + ". " + GameText.World(world), 64f, UiKit.Gold,
                        TextAlignmentOptions.Left, true);
                    UiKit.Size(header, 90f);
                    grid = UiKit.Rect("Grid" + world, mapContent);
                    var g = grid.gameObject.AddComponent<GridLayoutGroup>();
                    g.cellSize = new Vector2(180f, 210f);
                    g.spacing = new Vector2(16f, 20f);
                    g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    g.constraintCount = 5;
                    g.childAlignment = TextAnchor.UpperCenter;
                    var f = grid.gameObject.AddComponent<ContentSizeFitter>();
                    f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
                MapButton(grid, catalog, i);
            }
        }

        void MapButton(RectTransform grid, LevelCatalog catalog, int index)
        {
            var def = catalog[index];
            bool unlocked = catalog.IsUnlocked(index);
            var rec = SaveSystem.GetRecord(def.Id);
            bool isCurrent = loader != null && loader.Index == index;
            int i = index;
            var b = UiKit.Button("Level_" + def.Id, grid, def.Id, isCurrent ? UiKit.Primary : unlocked ? UiKit.Paper : UiKit.Neutral,
                () => PlayLevel(i), 52f);
            b.interactable = unlocked;
            var label = UiKit.ButtonLabel(b);
            label.rectTransform.offsetMin = new Vector2(0f, 60f);
            if (!unlocked) label.color = new Color(0.45f, 0.45f, 0.5f);

            int stars = rec != null ? rec.Stars : 0;
            for (int s = 0; s < 3; s++)
            {
                var img = UiKit.Image("Star" + s, b.transform, starSprite, s < stars ? UiKit.Gold : new Color(0f, 0f, 0f, 0.15f));
                UiKit.Place(img.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2((s - 1) * 50f, 18f), new Vector2(48f, 48f));
            }
            // Vương miện: viền cam quanh nút (font chưa chắc có ký tự vương miện).
            if (rec != null && rec.crown)
            {
                var o = b.gameObject.AddComponent<Outline>();
                o.effectColor = UiKit.Orange;
                o.effectDistance = new Vector2(6f, -6f);
            }
        }

        // ---------------------------------------------------------------- Cài đặt (§10.4, §10.7)

        RectTransform BuildSettings()
        {
            var root = UiKit.Overlay("Settings", screens, UiKit.Dim);
            var card = UiKit.Card("Card", root, new Vector2(820f, 1000f));
            UiKit.Column(card, 28f, 56);
            UiKit.Size(UiKit.Label("Title", card, GameText.Settings, 90f, UiKit.Ink), 130f);
            AddToggle(card, () => GameText.Sound + ": " + OnOff(SaveSystem.Data.settings.SoundOn),
                () => GameSettings.SetSound(!SaveSystem.Data.settings.SoundOn));
            AddToggle(card, () => GameText.Music + ": " + OnOff(SaveSystem.Data.settings.MusicOn),
                () => GameSettings.SetMusic(!SaveSystem.Data.settings.MusicOn));
            AddToggle(card, () => GameText.Haptics + ": " + OnOff(Haptics.Enabled), () => Haptics.Enabled = !Haptics.Enabled);
            AddToggle(card, () => GameText.ReduceMotion + ": " + OnOff(SaveSystem.Data.settings.reduceMotion),
                () => GameSettings.SetReduceMotion(!SaveSystem.Data.settings.reduceMotion));
            UiKit.Size(UiKit.Button("Back", card, GameText.Back, UiKit.Primary, Back), 140f);
            root.gameObject.SetActive(false);
            return root;
        }

        void AddToggle(RectTransform parent, System.Func<string> text, System.Action toggle)
        {
            Button b = null;
            b = UiKit.Button("Toggle", parent, text(), UiKit.Paper, () => { toggle(); RefreshToggles(); }, 52f);
            UiKit.Size(b, 120f);
            toggleLabels.Add((UiKit.ButtonLabel(b), text));
        }

        void RefreshToggles()
        {
            foreach (var (label, text) in toggleLabels) label.text = text();
        }

        static string OnOff(bool on) => on ? GameText.On : GameText.Off;
    }
}
