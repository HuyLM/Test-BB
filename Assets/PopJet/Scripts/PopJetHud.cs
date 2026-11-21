using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PopJet
{
    /// <summary>
    /// HUD tối thiểu của Giai đoạn 1 (GDD §10.3): nút Retry, số cú chọc, chữ lý do thua hiện ngay chỗ nổ,
    /// nhãn "Stuck?" và nút Retry nhấp nháy khi bóng bị kẹt. Thanh khí là AirBarUI riêng.
    /// Mọi hoạt ảnh chạy bằng unscaled time. Chữ lấy từ GameText (tiếng Anh).
    /// Chữ dùng TextMeshPro (phần 2.4). Nút Tạm dừng, ô Sao, Xem màn và các màn hình nằm ở GameScreens.
    /// </summary>
    public class PopJetHud : MonoBehaviour
    {
        [SerializeField] BalloonController balloon;
        [SerializeField] LevelManager level;
        [Tooltip("Để trống thì số cú chọc hiện không kèm Par và không báo sao.")]
        [SerializeField] LevelSession session;
        [SerializeField] Button retryButton;
        [SerializeField] TMP_Text retryLabel;
        [SerializeField] TMP_Text pokeLabel;
        [SerializeField] TMP_Text failLabel;
        [SerializeField] TMP_Text stuckLabel;
        [Tooltip("Chữ lý do thua hiện bao lâu (giây). Starting value.")]
        [SerializeField] float failLabelSeconds = 1.2f;
        [Tooltip("Chữ lý do thua nằm cao hơn chỗ nổ bao nhiêu (đơn vị canvas).")]
        [SerializeField] float failLabelOffset = 150f;

        Camera cam;
        RectTransform failRect, retryRect;
        float failShownAt = -1f;
        float messageSeconds;
        bool stuck;
        bool parGold, parPulse;

        void Awake()
        {
            cam = Camera.main;
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetry);
                retryRect = (RectTransform)retryButton.transform;
            }
            if (retryLabel != null) retryLabel.text = GameText.Retry;
            if (stuckLabel != null)
            {
                stuckLabel.text = GameText.Stuck;
                stuckLabel.gameObject.SetActive(false);
            }
            if (failLabel != null)
            {
                failRect = failLabel.rectTransform;
                failLabel.gameObject.SetActive(false);
            }
            SetPokes(0);
        }

        void OnEnable()
        {
            if (balloon != null)
            {
                balloon.Poked += SetPokes;
                balloon.BecameEmpty += OnEmpty;
            }
            if (level != null) level.StuckChanged += OnStuck;
            if (session != null)
            {
                session.StarCollected += OnStar;
                session.Completed += OnCompleted;
            }
            Checkpoint.Activated += OnCheckpoint;
        }

        void OnDisable()
        {
            if (balloon != null)
            {
                balloon.Poked -= SetPokes;
                balloon.BecameEmpty -= OnEmpty;
            }
            if (level != null) level.StuckChanged -= OnStuck;
            if (session != null)
            {
                session.StarCollected -= OnStar;
                session.Completed -= OnCompleted;
            }
            Checkpoint.Activated -= OnCheckpoint;
        }

        void Start()
        {
            // Par của màn chỉ có sau khi LevelLoader chạy; chơi lại từ cột mốc thì đã có sẵn vài cú chọc.
            SetPokes(balloon != null ? balloon.PokeCount : 0);
        }

        /// <summary>
        /// Làm nổi phần Par trên nhãn số cú chọc (tutorial màn 1-9, GDD §8.4): gold = chữ Par màu vàng,
        /// pulse = nhãn nhún nhẹ.
        /// </summary>
        public void SetParHighlight(bool gold, bool pulse)
        {
            parGold = gold;
            parPulse = pulse;
            if (!pulse && pokeLabel != null) pokeLabel.rectTransform.localScale = Vector3.one;
            SetPokes(balloon != null ? balloon.PokeCount : 0);
        }

        void OnCheckpoint(Checkpoint checkpoint) { ShowMessage(GameText.Checkpoint, 0.9f); }

        void OnRetry()
        {
            if (level != null) level.Restart();
        }

        void SetPokes(int count)
        {
            if (pokeLabel == null) return;
            if (session == null) pokeLabel.text = GameText.Pokes(count);
            else if (parGold) pokeLabel.text = GameText.Pokes(count) + " / <color=#FFD23F>Par " + session.Par + "</color>";
            else pokeLabel.text = GameText.Pokes(count, session.Par);
            // Vượt Par thì chữ chuyển cam: sao Par của lượt này đã mất.
            pokeLabel.color = session != null && count > session.Par ? new Color(1f, 0.541f, 0f) : Color.white;
        }

        void OnStar() { ShowMessage(GameText.StarCollected, 0.8f); }

        void OnCompleted(LevelResult r) { ShowMessage(GameText.Result(r.Stars, r.crown), 1.2f); }

        void OnStuck(bool value)
        {
            stuck = value;
            if (stuckLabel != null) stuckLabel.gameObject.SetActive(value);
            if (!value && retryRect != null) retryRect.localScale = Vector3.one;
        }

        void OnEmpty()
        {
            OnStuck(false);
            ShowMessage(GameText.Fail(balloon.LastFailReason), failLabelSeconds);
        }

        /// <summary>Hiện chữ ngắn (lý do thua, nhặt Sao, số sao) ngay phía trên bóng, bật lên rồi tự ẩn.</summary>
        void ShowMessage(string text, float seconds)
        {
            if (failLabel == null) return;
            failLabel.text = text;
            failLabel.gameObject.SetActive(true);
            failShownAt = Time.unscaledTime;
            messageSeconds = seconds;
            PlaceFailLabel();
        }

        void PlaceFailLabel()
        {
            var parent = failRect.parent as RectTransform;
            if (cam == null || parent == null) return;
            Vector2 screen = cam.WorldToScreenPoint(balloon.Center);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out Vector2 local)) return;

            local.y += failLabelOffset;
            Rect r = parent.rect;
            Vector2 half = failRect.rect.size * 0.5f;
            local.x = Mathf.Clamp(local.x, r.xMin + half.x, Mathf.Max(r.xMin + half.x, r.xMax - half.x));
            local.y = Mathf.Clamp(local.y, r.yMin + half.y, Mathf.Max(r.yMin + half.y, r.yMax - half.y));
            failRect.localPosition = local;
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (failShownAt >= 0f && failRect != null)
            {
                // Bật lên: 0.3 -> 1.15 -> 1 rồi giữ, hết giờ thì ẩn.
                float t = now - failShownAt;
                float s = t < 0.12f ? Mathf.Lerp(0.3f, 1.15f, t / 0.12f)
                        : t < 0.25f ? Mathf.Lerp(1.15f, 1f, (t - 0.12f) / 0.13f) : 1f;
                failRect.localScale = Vector3.one * s;
                if (t > messageSeconds)
                {
                    failLabel.gameObject.SetActive(false);
                    failShownAt = -1f;
                }
            }
            if (stuck && retryRect != null)
                retryRect.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(now * 6f));
            if (parPulse && pokeLabel != null)
                pokeLabel.rectTransform.localScale = Vector3.one * (1f + 0.07f * Mathf.Sin(now * 5f));
        }
    }
}
