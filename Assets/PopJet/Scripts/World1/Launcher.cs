using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Pháo giấy (GDD §6 #5): bóng chui vào thì bị giữ lại, nòng tự xoay qua lại; chạm màn hình để bắn bóng
    /// theo hướng nòng mà không tốn khí và không tính là cú chọc. Thay vì tốn khí, người chơi canh hướng.
    /// Nòng xoay theo scaled time. Collider phải là trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Launcher : MonoBehaviour
    {
        [Tooltip("Nòng pháo (hướng bắn = trục +Y local của nòng).")]
        [SerializeField] Transform barrel;
        [Tooltip("Góc quét hai bên so với hướng ban đầu của nòng (độ).")]
        [SerializeField] float sweepDegrees = 50f;
        [Tooltip("Tốc độ xoay (độ/giây). Starting value 60 (GDD §6).")]
        [SerializeField] float turnSpeed = 60f;
        [Tooltip("Vận tốc bóng khi bắn ra (đơn vị/giây). Khoảng 1.5 cú chọc (GDD §6). Starting value.")]
        [SerializeField] float launchSpeed = 6.5f;
        [Tooltip("Sau khi bắn, bao lâu thì pháo mới bắt lại được bóng (giây).")]
        [SerializeField] float recaptureDelay = 0.6f;
        [Tooltip("Chờ bao lâu sau khi bắt bóng mới cho bắn (tránh cú chạm vừa chọc bắn luôn).")]
        [SerializeField] float armDelay = 0.25f;

        BalloonController held;
        float baseAngle, sweepT, heldFor;
        // Thời gian vật lý (cộng trong FixedUpdate): luật bắt lại bóng không phụ thuộc khung hình,
        // để lời giải đã ghi phát lại ra y hệt (SolutionRecorder).
        float simTime, lastFire = -99f;

        public bool HasBalloon => held != null;
        public Vector2 Direction => barrel != null ? (Vector2)barrel.up : (Vector2)transform.up;
        public float LaunchSpeed => launchSpeed;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (barrel != null) baseAngle = barrel.eulerAngles.z;
        }

        void FixedUpdate() { simTime += Time.fixedDeltaTime; }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (held != null || simTime - lastFire < recaptureDelay) return;
            var b = other.GetComponentInParent<BalloonController>();
            if (b == null || !b.Hold(transform.position)) return;
            held = b;
            heldFor = 0f;
            Haptics.Light();
        }

        void Update()
        {
            if (barrel != null && sweepDegrees > 0f)
            {
                sweepT += Time.deltaTime * turnSpeed;
                float a = Mathf.PingPong(sweepT, sweepDegrees * 2f) - sweepDegrees;
                barrel.rotation = Quaternion.Euler(0f, 0f, baseAngle + a);
            }
            if (held == null) return;
            if (!held.IsHeld) { held = null; return; } // bóng nổ hoặc về đích trong lúc giữ
            heldFor += Time.deltaTime;
            if (heldFor < armDelay || TimeController.IsBlockingInput) return;
            PointerHelper.Poll();
            if (PointerHelper.Down && !PointerHelper.IsOverUI(PointerHelper.ScreenPos)) Fire();
        }

        /// <summary>Bắn bóng đang giữ theo hướng nòng.</summary>
        public void Fire() { FireVelocity(Direction * launchSpeed); }

        /// <summary>
        /// Bắn bóng đang giữ với vận tốc cho trước. Phát lại lời giải dùng đúng vận tốc đã ghi, vì nòng xoay theo
        /// khung hình nên hướng nòng lúc phát lại không trùng từng bit với lúc ghi.
        /// </summary>
        public void FireVelocity(Vector2 velocity)
        {
            if (held == null) return;
            held.Launch(velocity);
            held = null;
            lastFire = simTime;
            Haptics.Medium();
        }
    }
}
