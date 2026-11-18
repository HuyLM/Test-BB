using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Cảm giác "nảy và mềm" khi bóng va tường:
    /// - Độ nảy thật đến từ PhysicsMaterial2D trên collider của bóng (bounciness cao, friction thấp).
    /// - Script này chỉ lo phần "mềm": bóp dẹt sprite theo pháp tuyến va chạm rồi rung nhẹ về hình tròn
    ///   (lò xo tắt dần), cộng tiếng "bộp" có âm lượng theo lực va.
    /// Chỉ biến dạng object con `visual`, không đụng vào scale gốc (BalloonController dùng scale gốc để thể hiện lượng khí).
    /// Mọi giá trị số là starting value - cần playtest.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BalloonBounce : MonoBehaviour
    {
        [Tooltip("Object con chứa SpriteRenderer của bóng.")]
        [SerializeField] Transform visual;

        [Header("Squash (starting values)")]
        [Tooltip("Độ dẹt tối đa theo hướng va (0.35 = dẹt 35%).")]
        [SerializeField, Range(0f, 0.6f)] float maxSquash = 0.35f;
        [Tooltip("Va chạm với tốc độ (theo pháp tuyến) từ mức này trở lên thì dẹt tối đa.")]
        [SerializeField] float fullImpactSpeed = 2.5f;
        [Tooltip("Dưới tốc độ này thì bỏ qua (chạm nhẹ, trượt dọc tường).")]
        [SerializeField] float minImpactSpeed = 0.4f;
        [Tooltip("Tỉ lệ phình ra ở trục vuông góc khi bị dẹt (giữ cảm giác thể tích).")]
        [SerializeField, Range(0f, 1f)] float bulge = 0.6f;
        [Tooltip("Độ cứng lò xo hồi hình. Thấp = mềm, chậm.")]
        [SerializeField] float stiffness = 180f;
        [Tooltip("Giảm chấn. Thấp = rung nhiều lần (jelly), cao = về thẳng.")]
        [SerializeField] float springDamping = 9f;

        [Header("Audio")]
        [SerializeField] AudioSource source;
        [Tooltip("Nên có từ 3 clip trở lên để tránh lặp.")]
        [SerializeField] AudioClip[] bounceClips;
        [SerializeField] Vector2 pitchRange = new Vector2(0.9f, 1.1f);
        [Tooltip("Thời gian tối thiểu giữa 2 tiếng nảy, tránh spam khi lăn dọc tường.")]
        [SerializeField] float soundCooldown = 0.08f;

        float squash;      // < 0 = dẹt theo axis
        float velocity;
        Vector2 axis = Vector2.right;
        float lastSoundTime = -99f;

        void Awake()
        {
            if (source == null) source = GetComponent<AudioSource>();
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            if (c.contactCount == 0) return;
            Vector2 n = c.GetContact(0).normal;
            float speed = Mathf.Abs(Vector2.Dot(c.relativeVelocity, n));
            if (speed < minImpactSpeed) return;

            float k = Mathf.Clamp01(speed / fullImpactSpeed);
            axis = n;
            squash = Mathf.Min(squash, -k * maxSquash);
            velocity = 0f;

            PlayBounce(k);
        }

        void LateUpdate()
        {
            if (visual == null) return;

            // Lò xo tắt dần về 0 (semi-implicit Euler, chặn dt để không nổ khi lag).
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
            velocity += (-stiffness * squash - springDamping * velocity) * dt;
            squash += velocity * dt;
            if (Mathf.Abs(squash) < 0.0005f && Mathf.Abs(velocity) < 0.005f) { squash = 0f; velocity = 0f; }

            // Trục X local của visual = pháp tuyến va chạm. Sprite tròn nên xoay không lộ.
            visual.rotation = Quaternion.FromToRotation(Vector3.right, axis);
            visual.localScale = new Vector3(1f + squash, 1f - squash * bulge, 1f);
        }

        void PlayBounce(float strength)
        {
            if (source == null || bounceClips == null || bounceClips.Length == 0) return;
            if (Time.time - lastSoundTime < soundCooldown) return;
            lastSoundTime = Time.time;
            var clip = bounceClips[Random.Range(0, bounceClips.Length)];
            if (clip == null) return;
            source.pitch = Random.Range(pitchRange.x, pitchRange.y);
            source.PlayOneShot(clip, Mathf.Lerp(0.3f, 1f, strength));
        }
    }
}
