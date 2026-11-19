using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Chạy qua lại (hoặc một chiều) giữa vị trí ban đầu và vị trí ban đầu + offset (GDD §5.2 #2, §15.3).
    /// Rigidbody2D kinematic, MovePosition trong FixedUpdate nên tự dừng khi thế giới dừng (timeScale = 0).
    /// Tốc độ giữ ≤ 30% tốc độ tối đa của bóng (§5.2). Đường ray vẽ bằng chấm (sprite dotSprite) lúc chạy.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PathMover : MonoBehaviour
    {
        [Tooltip("Điểm cuối, tính từ vị trí ban đầu (đơn vị world).")]
        [SerializeField] Vector2 offset = new Vector2(3f, 0f);
        [Tooltip("Đơn vị/giây. Starting value 1.0 (GDD §5.2).")]
        [SerializeField] float speed = 1f;
        [Tooltip("true = qua lại mãi. false = chạy tới điểm cuối rồi dừng (máy hút bụi đuổi theo).")]
        [SerializeField] bool pingPong = true;
        [Tooltip("Chỉ chạy sau khi người chơi chọc lần đầu (GDD §8.3 luật 8: mở màn luôn an toàn).")]
        [SerializeField] bool waitForFirstPoke = false;
        [Tooltip("Pha ban đầu 0..1 dọc đường đi.")]
        [SerializeField, Range(0f, 1f)] float startPhase = 0f;
        [Tooltip("Sprite chấm vẽ đường ray. Để trống = không vẽ.")]
        [SerializeField] Sprite dotSprite;
        [SerializeField] float dotSpacing = 0.35f;

        Rigidbody2D rb;
        Vector2 a, b;
        float s; // quãng đường đã đi (0..length, pingpong dùng 0..2*length)
        BalloonController balloon;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            a = transform.position;
            b = a + offset;
            s = startPhase * offset.magnitude;
            rb.position = Vector2.Lerp(a, b, startPhase);
            if (dotSprite != null) DrawRail();
        }

        void Start()
        {
            if (waitForFirstPoke) balloon = FindAnyObjectByType<BalloonController>();
        }

        void DrawRail()
        {
            var parent = transform.parent;
            float len = offset.magnitude;
            int n = Mathf.Max(2, Mathf.RoundToInt(len / dotSpacing) + 1);
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("RailDot");
                go.transform.SetParent(parent, false);
                go.transform.position = Vector2.Lerp(a, b, i / (float)(n - 1));
                go.transform.localScale = Vector3.one * 0.12f;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = dotSprite;
                sr.color = new Color(1f, 1f, 1f, 0.35f);
                sr.sortingOrder = -1;
            }
        }

        void FixedUpdate()
        {
            if (balloon != null && !balloon.Started) return;
            float len = offset.magnitude;
            if (len < 1e-4f) return;
            s += speed * Time.fixedDeltaTime;
            float d = pingPong ? Mathf.PingPong(s, len) : Mathf.Min(s, len);
            rb.MovePosition(Vector2.Lerp(a, b, d / len));
        }

        void OnDrawGizmosSelected()
        {
            Vector3 p = Application.isPlaying ? (Vector3)a : transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(p, p + (Vector3)offset);
        }
    }
}
