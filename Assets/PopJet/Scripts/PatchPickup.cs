using UnityEngine;
using UnityEngine.Events;

namespace PopJet
{
    /// <summary>
    /// Keo vá: chạm vào là vá ngay mọi lỗ rò, có thể hồi thêm một ít khí.
    /// Chỉ bị dùng khi thật sự có ích (có lỗ rò hoặc bóng chưa đầy khí) để người chơi không lỡ mất.
    /// Collider phải là trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PatchPickup : MonoBehaviour
    {
        [Tooltip("Lượng khí hồi khi ăn keo (0.3 = 30%, gấp 3 mức cũ 10%).")]
        [SerializeField, Range(0f, 1f)] float airRefill = 0.3f;
        [SerializeField] bool singleUse = true;
        [Tooltip("Sau bao nhiêu giây thì keo xuất hiện lại. 0 = không xuất hiện lại.")]
        [SerializeField] float respawnDelay = 0f;
        [SerializeField] UnityEvent onUsed;

        Collider2D col;
        Renderer[] renderers;

        void Awake()
        {
            col = GetComponent<Collider2D>();
            col.isTrigger = true;
            renderers = GetComponentsInChildren<Renderer>();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var balloon = other.GetComponentInParent<BalloonController>();
            if (balloon == null) return;

            bool useful = balloon.HasLeaks || balloon.Air01 < 0.999f;
            if (!useful) return;

            balloon.PatchAll();
            balloon.AddAir(airRefill);
            onUsed?.Invoke();

            if (singleUse)
            {
                SetActive(false);
                if (respawnDelay > 0f) Invoke(nameof(Restore), respawnDelay);
            }
        }

        void Restore() { SetActive(true); }

        void SetActive(bool value)
        {
            col.enabled = value;
            foreach (var r in renderers) r.enabled = value;
        }
    }
}
