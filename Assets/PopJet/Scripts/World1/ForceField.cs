using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Trường lực hướng tâm (GDD §5.2 #12, §15.3): hút bóng về tâm, càng gần càng mạnh. Máy hút bụi dùng
    /// trường này kèm HardHazard (SuckedIn) ở miệng. Collider trigger xác định vùng hút.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ForceField : MonoBehaviour
    {
        [Tooltip("Lực tối đa ở tâm. Starting value 5 (GDD §5.2).")]
        [SerializeField] float maxForce = 5f;
        [Tooltip("Lực ở mép vùng, tính theo tỉ lệ của maxForce.")]
        [SerializeField, Range(0f, 1f)] float edgeFactor = 0.15f;
        [Tooltip("Bán kính dùng để tính lực giảm dần (đơn vị world).")]
        [SerializeField] float radius = 3f;
        [Tooltip("Âm = đẩy ra xa thay vì hút vào.")]
        [SerializeField] float sign = 1f;

        void Awake() { GetComponent<Collider2D>().isTrigger = true; }

        void OnTriggerStay2D(Collider2D other)
        {
            var balloon = other.GetComponentInParent<BalloonController>();
            // Chưa chọc lần đầu thì không hút: mở màn luôn an toàn (GDD §8.3 luật 8).
            if (balloon == null || !balloon.Started) return;
            var rb = other.attachedRigidbody;
            if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) return;
            Vector2 to = (Vector2)transform.position - rb.position;
            float d = to.magnitude;
            if (d < 1e-3f) return;
            float k = Mathf.Lerp(1f, edgeFactor, Mathf.Clamp01(d / Mathf.Max(0.01f, radius)));
            rb.AddForce(to / d * (maxForce * k * sign), ForceMode2D.Force);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
