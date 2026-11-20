using UnityEngine;
using UnityEngine.Events;

namespace PopJet
{
    /// <summary>
    /// Keo vá: chạm vào là vá ngay mọi lỗ rò. Chỉ vá, KHÔNG bơm thêm khí (hồi khí là việc của trạm bơm, GDD §6 #8).
    /// Chỉ bị dùng khi bóng đang có lỗ rò, để người chơi không lỡ mất keo lúc chưa cần.
    /// Collider phải là trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PatchPickup : MonoBehaviour
    {
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

            if (!balloon.HasLeaks) return;

            balloon.PatchAll();
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
