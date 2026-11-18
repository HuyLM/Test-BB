using UnityEngine;
using UnityEngine.UI;

namespace PopJet
{
    /// <summary>
    /// HUD tối thiểu của Giai đoạn 1 (GDD §10.3): nút Retry, số cú chọc, chữ lý do thua hiện ngay chỗ nổ,
    /// nhãn "Stuck?" và nút Retry nhấp nháy khi bóng bị kẹt. Thanh khí là AirBarUI riêng.
    /// Mọi hoạt ảnh chạy bằng unscaled time. Chữ lấy từ GameText (tiếng Anh).
    /// Dùng UI Text thường cho prototype; Giai đoạn 2 đổi sang TextMeshPro.
    /// </summary>
    public class PopJetHud : MonoBehaviour
    {
        [SerializeField] BalloonController balloon;
        [SerializeField] LevelManager level;
        [SerializeField] Button retryButton;
        [SerializeField] Text retryLabel;
        [SerializeField] Text pokeLabel;
        [SerializeField] Text failLabel;
        [SerializeField] Text stuckLabel;
        [Tooltip("Chữ lý do thua hiện bao lâu (giây). Starting value.")]
        [SerializeField] float failLabelSeconds = 1.2f;
        [Tooltip("Chữ lý do thua nằm cao hơn chỗ nổ bao nhiêu (đơn vị canvas).")]
        [SerializeField] float failLabelOffset = 150f;

        Camera cam;
        RectTransform failRect, retryRect;
        float failShownAt = -1f;
        bool stuck;

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
        }

        void OnDisable()
        {
            if (balloon != null)
            {
                balloon.Poked -= SetPokes;
                balloon.BecameEmpty -= OnEmpty;
            }
            if (level != null) level.StuckChanged -= OnStuck;
        }

        void OnRetry()
        {
            if (level != null) level.Restart();
        }

        void SetPokes(int count)
        {
            if (pokeLabel != null) pokeLabel.text = GameText.Pokes(count);
        }

        void OnStuck(bool value)
        {
            stuck = value;
            if (stuckLabel != null) stuckLabel.gameObject.SetActive(value);
            if (!value && retryRect != null) retryRect.localScale = Vector3.one;
        }

        void OnEmpty()
        {
            OnStuck(false);
            if (failLabel == null) return;
            failLabel.text = GameText.Fail(balloon.LastFailReason);
            failLabel.gameObject.SetActive(true);
            failShownAt = Time.unscaledTime;
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
                if (t > failLabelSeconds)
                {
                    failLabel.gameObject.SetActive(false);
                    failShownAt = -1f;
                }
            }
            if (stuck && retryRect != null)
                retryRect.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(now * 6f));
        }
    }
}
