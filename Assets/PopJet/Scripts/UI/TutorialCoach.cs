using TMPro;
using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Gợi ý bằng hình cho các màn tutorial (GDD §8.4), theo cờ TutorialCue của LevelDefinition:
    /// - DragHand (1-1): bàn tay mờ nhấp nháy trên bóng; khi đang giữ mà chưa kéo ra thì bàn tay ma kéo xuống
    ///   (kim chĩa xuống = bóng bay lên tới đích). Hết sau cú chọc đầu tiên.
    /// - StarChip (1-6): ô Sao trên HUD nhấp nháy tới khi nhặt được Sao.
    /// - ParChip (1-9): phần Par trên HUD vàng và nhún tới cú chọc đầu tiên, sau đó vàng yên. Dòng chữ giải thích Par
    ///   do GameScreens hiện.
    /// Mọi màn: đã kéo kim ra rồi đưa ngón tay về vùng hủy thì hiện "Release in the middle to cancel" ngay trên bóng,
    /// tới khi người chơi hủy thành công một lần (lưu SaveData.tutorial.cancelLearned).
    /// Lá giấy trong gió quạt (1-7) và dây nối công tắc với cửa (1-8) là đồ trong màn: WindLeaves, SwitchWire.
    /// Chạy bằng unscaled time vì thế giới dừng trong lúc ngắm.
    /// </summary>
    public class TutorialCoach : MonoBehaviour
    {
        [SerializeField] BalloonController balloon;
        [SerializeField] BalloonInput input;
        [SerializeField] LevelLoader loader;
        [SerializeField] LevelSession session;
        [SerializeField] PopJetHud hud;
        [SerializeField] GameScreens screens;
        [Tooltip("Vùng an toàn của HUD: dòng gợi ý hủy nằm trong này.")]
        [SerializeField] RectTransform safeArea;
        [Tooltip("Sprite bàn tay chỉ ngón trỏ lên, đầu ngón ở mép trên giữa ảnh.")]
        [SerializeField] Sprite handSprite;
        [SerializeField] Material material;
        [Tooltip("Cao của bàn tay (đơn vị world).")]
        [SerializeField] float handSize = 1.1f;
        [Tooltip("Bàn tay ma kéo xa mặt bóng bao nhiêu (đơn vị world).")]
        [SerializeField] float dragDistance = 1.1f;
        [Tooltip("Dòng gợi ý hủy nằm cao hơn tâm bóng bao nhiêu (đơn vị canvas).")]
        [SerializeField] float tipOffset = 190f;

        SpriteRenderer hand, handShadow;
        TextMeshProUGUI cancelTip;
        float tipLinger;
        Camera cam;
        TutorialCue cues;
        bool parPulseDone;

        void Awake()
        {
            cam = Camera.main;
            if (handSprite == null) return;
            handShadow = NewSprite("HandShadow", new Color(0.17f, 0.17f, 0.23f, 0.45f), 40);
            hand = NewSprite("Hand", Color.white, 41);
            handShadow.transform.SetParent(hand.transform, false);
            handShadow.transform.localPosition = new Vector3(0.04f, -0.04f, 0f) / handSize;
        }

        SpriteRenderer NewSprite(string name, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = handSprite;
            sr.color = color;
            sr.sortingOrder = order;
            if (material != null) sr.sharedMaterial = material;
            sr.enabled = false;
            return sr;
        }

        void OnEnable()
        {
            if (input != null) input.Canceled += OnCanceled;
            if (balloon != null) balloon.Poked += OnPoked;
        }

        void OnDisable()
        {
            if (input != null) input.Canceled -= OnCanceled;
            if (balloon != null) balloon.Poked -= OnPoked;
        }

        void Start()
        {
            var def = loader != null ? loader.Level : null;
            cues = def != null ? def.Tutorial : TutorialCue.None;
            if ((cues & TutorialCue.ParChip) != 0 && hud != null)
                hud.SetParHighlight(true, balloon == null || balloon.PokeCount == 0);
            // Dòng gợi ý hủy dựng lúc chạy, sau Awake của GameScreens (đã gán font cho UiKit).
            if (safeArea != null)
            {
                cancelTip = UiKit.Label("CancelTip", safeArea, GameText.CancelTip, 46f, Color.white, TextAlignmentOptions.Center, true);
                UiKit.Place(cancelTip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 70f));
                cancelTip.gameObject.SetActive(false);
            }
        }

        void OnPoked(int count)
        {
            if ((cues & TutorialCue.ParChip) != 0 && !parPulseDone && hud != null)
            {
                parPulseDone = true;
                hud.SetParHighlight(true, false);
            }
        }

        void OnCanceled()
        {
            var t = SaveSystem.Data.tutorial;
            if (t.cancelLearned) return;
            t.cancelLearned = true;
            SaveSystem.MarkDirty();
            tipLinger = 0f;
        }

        void LateUpdate()
        {
            UpdateHand();
            UpdateStarChip();
            UpdateCancelTip();
        }

        // ---------------- 1-1: bàn tay

        void UpdateHand()
        {
            if (hand == null) return;
            bool show = (cues & TutorialCue.DragHand) != 0 && balloon != null && input != null &&
                        balloon.PokeCount == 0 && balloon.CanAim && !TimeController.IsBlockingInput &&
                        !input.HasAimDirection && !(screens != null && screens.IsScreenOpen);
            hand.enabled = handShadow.enabled = show;
            if (!show) return;

            float now = Time.unscaledTime;
            Vector2 c = balloon.Center;
            float scale = handSize / Mathf.Max(0.01f, handSprite.bounds.size.y);
            float alpha;
            Vector2 tip;
            if (!input.IsAiming)
            {
                // Nhấp nháy ngay trên bóng: "giữ vào đây".
                float k = 0.5f + 0.5f * Mathf.Sin(now * Mathf.PI * 2f * 1.4f);
                tip = c;
                scale *= Mathf.Lerp(1f, 0.88f, k);
                alpha = Mathf.Lerp(0.45f, 0.9f, k);
            }
            else
            {
                // Đang giữ mà chưa kéo: bàn tay ma kéo xuống rồi lặp lại.
                float cycle = Mathf.Repeat(now, 1.5f);
                float move = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(cycle / 0.9f));
                tip = c + Vector2.down * ((balloon.Radius + dragDistance) * move);
                alpha = cycle < 1.2f ? 0.8f : Mathf.Lerp(0.8f, 0f, (cycle - 1.2f) / 0.3f);
            }
            // Đầu ngón trỏ ở mép trên giữa ảnh: đặt mép đó vào điểm chạm.
            float h = handSprite.bounds.size.y * scale;
            hand.transform.position = new Vector3(tip.x, tip.y - h * 0.45f, 0f);
            hand.transform.localScale = Vector3.one * scale;
            hand.color = new Color(1f, 1f, 1f, alpha);
            handShadow.color = new Color(0.17f, 0.17f, 0.23f, 0.45f * alpha);
        }

        // ---------------- 1-6: ô Sao

        void UpdateStarChip()
        {
            if ((cues & TutorialCue.StarChip) == 0 || screens == null || screens.StarChip == null) return;
            var chip = screens.StarChip;
            if (session != null && session.StarCollectedThisRun)
            {
                chip.rectTransform.localScale = Vector3.one;
                return;
            }
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * 1.2f);
            chip.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.3f, k);
            chip.color = Color.Lerp(UiKit.StarOff, new Color(1f, 0.92f, 0.6f, 0.95f), k);
        }

        // ---------------- Mọi màn: cách hủy

        void UpdateCancelTip()
        {
            if (cancelTip == null) return;
            bool want = input != null && !SaveSystem.Data.tutorial.cancelLearned && input.InCancelZoneAfterAiming;
            if (want) tipLinger = 0.6f;
            else tipLinger -= Time.unscaledDeltaTime;
            bool show = tipLinger > 0f;
            if (cancelTip.gameObject.activeSelf != show) cancelTip.gameObject.SetActive(show);
            if (!show || cam == null || balloon == null) return;

            var parent = cancelTip.rectTransform.parent as RectTransform;
            Vector2 screen = cam.WorldToScreenPoint(balloon.Center);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out Vector2 local)) return;
            local.y += tipOffset;
            Rect r = parent.rect;
            float halfW = cancelTip.preferredWidth * 0.5f + 20f;
            local.x = Mathf.Clamp(local.x, r.xMin + halfW, Mathf.Max(r.xMin + halfW, r.xMax - halfW));
            local.y = Mathf.Clamp(local.y, r.yMin + 60f, r.yMax - 60f);
            cancelTip.rectTransform.localPosition = local;
            cancelTip.alpha = Mathf.Clamp01(tipLinger / 0.3f);
        }
    }
}
