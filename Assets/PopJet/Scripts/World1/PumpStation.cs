using UnityEngine;
using UnityEngine.Events;

namespace PopJet
{
    /// <summary>
    /// Trạm bơm (GDD §6 #8): bóng ở trong vùng thì khí hồi dần. Mỗi trạm có tổng mức hồi tối đa để không
    /// "cày" được khí vô hạn (§6, §15). Keo vá chỉ vá, việc hồi khí là của trạm bơm.
    /// Khi không có cú chọc nào đang chạy, trạm kéo nhẹ bóng về tâm vùng để bóng đứng lại bơm được;
    /// cú chọc mạnh hơn lực kéo nhiều nên người chơi luôn thoát ra được.
    /// Chạy trong bước vật lý nên tự dừng khi thế giới dừng (lúc ngắm, tạm dừng).
    /// Collider (vùng bơm) phải là trigger; tâm vùng = tâm collider.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PumpStation : MonoBehaviour
    {
        [Tooltip("Khí hồi mỗi giây khi bóng ở trong vùng (0..1 = cả bình). Starting value theo GDD §6 #8.")]
        [SerializeField] float airPerSecond = 0.25f;
        [Tooltip("Tổng khí trạm này hồi được trong cả màn. Hết thì trạm tắt. Starting value theo GDD §6 #8.")]
        [SerializeField] float capacity = 0.5f;
        [Tooltip("Lực lò xo kéo bóng về tâm vùng (nhân khoảng cách). 0 = không giữ. Starting value.")]
        [SerializeField] float holdStrength = 4f;
        [Tooltip("Con hiển thị lượng khí còn lại của trạm: scale X từ 1 (đầy) về 0 (hết).")]
        [SerializeField] Transform gauge;
        [Tooltip("Vùng bơm nhìn thấy được; mờ đi khi trạm hết khí.")]
        [SerializeField] SpriteRenderer zone;
        [SerializeField] UnityEvent onDepleted;

        Collider2D col;
        float remaining;
        float lastPumpStep = -1f;
        Color zoneColor;

        /// <summary>Khí trạm còn hồi được (0..capacity).</summary>
        public float Remaining => remaining;
        public float Capacity => capacity;
        /// <summary>true = bước vật lý vừa rồi trạm có bơm khí vào bóng.</summary>
        public bool IsPumping => Time.fixedTime - lastPumpStep <= Time.fixedDeltaTime * 1.5f;

        void Awake()
        {
            col = GetComponent<Collider2D>();
            col.isTrigger = true;
            remaining = capacity;
            if (zone != null) zoneColor = zone.color;
            UpdateVisuals();
        }

        void OnTriggerStay2D(Collider2D other)
        {
            var balloon = other.GetComponentInParent<BalloonController>();
            if (balloon == null) return;
            // Bóng có thể có nhiều collider: mỗi bước vật lý chỉ bơm một lần.
            if (Mathf.Approximately(lastPumpStep, Time.fixedTime)) return;
            if (remaining <= 0f || balloon.IsHeld) return;
            var state = balloon.State;
            if (state == BalloonState.Empty || state == BalloonState.Goal) return;

            float dt = Time.fixedDeltaTime;
            float amount = Mathf.Min(airPerSecond * dt, remaining, 1f - balloon.Air01);
            if (amount > 0f)
            {
                balloon.AddAir(amount);
                remaining -= amount;
                lastPumpStep = Time.fixedTime;
                if (remaining <= 1e-4f)
                {
                    remaining = 0f;
                    onDepleted?.Invoke();
                }
                UpdateVisuals();
            }

            // Giữ bóng lại trong vùng khi đang bơm và người chơi không chọc.
            if (amount > 0f && holdStrength > 0f && balloon.VentCount == 0)
            {
                var rb = balloon.Body;
                Vector2 toCenter = (Vector2)col.bounds.center - rb.position;
                rb.AddForce(toCenter * holdStrength * rb.mass);
            }
        }

        void UpdateVisuals()
        {
            float k = capacity > 0f ? remaining / capacity : 0f;
            if (gauge != null)
            {
                var s = gauge.localScale;
                gauge.localScale = new Vector3(k, s.y, s.z);
            }
            if (zone != null)
                zone.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, zoneColor.a * (0.25f + 0.75f * k));
        }
    }
}
