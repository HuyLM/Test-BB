using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Vật nhóm D đứng yên (GDD §5.1, §15.3): chạm vào là nổ ngay qua BalloonController.Pop(reason).
    /// Dùng cho lửa nến (Burned), miệng máy hút bụi (SuckedIn). Collider thường hoặc trigger đều được.
    /// Tùy chọn chập chờn: object con "flicker" co giãn và lắc nhẹ theo scaled time (luôn chuyển động = báo nguy).
    /// </summary>
    public class HardHazard : MonoBehaviour
    {
        [SerializeField] FailReason reason = FailReason.Burned;
        [Tooltip("Object con chập chờn (ngọn lửa). Để trống = không có hoạt ảnh.")]
        [SerializeField] Transform flicker;
        [SerializeField] float flickerAmount = 0.12f;
        [SerializeField] float flickerSpeed = 11f;

        Vector3 flickerScale;
        float seed;

        void Awake()
        {
            if (flicker != null) flickerScale = flicker.localScale;
            seed = Random.value * 10f;
        }

        void Update()
        {
            if (flicker == null) return;
            float t = Time.time * flickerSpeed + seed;
            float n = Mathf.PerlinNoise(t, seed) - 0.5f;
            flicker.localScale = new Vector3(flickerScale.x * (1f - n * flickerAmount), flickerScale.y * (1f + n * flickerAmount * 2f), 1f);
            flicker.localRotation = Quaternion.Euler(0f, 0f, n * 8f);
        }

        void OnCollisionEnter2D(Collision2D c) { Hit(c.collider); }
        void OnTriggerEnter2D(Collider2D other) { Hit(other); }
        void OnTriggerStay2D(Collider2D other) { Hit(other); }

        void Hit(Collider2D other)
        {
            var balloon = other.GetComponentInParent<BalloonController>();
            if (balloon != null) balloon.Pop(reason);
        }
    }
}
