using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Mèo ngủ (GDD §5.2 #20, nhóm D): bóng vào bán kính thì mèo dậy, sau alertSeconds thì vồ.
    /// Lúc vồ mà bóng còn trong tầm thì nổ (CatAttack). Báo trước: "Zzz" khi ngủ, tai giật khi bóng gần mép bán kính,
    /// đổi màu và phồng lên khi dậy. Chạy theo scaled time (dừng khi ngắm).
    /// </summary>
    public class Cat : MonoBehaviour
    {
        enum Mode { Sleep, Alert, Pounce, Rest }

        [Tooltip("Bán kính đánh thức, tính tới mặt bóng (GDD §5.2: 1.8).")]
        [SerializeField] float wakeRadius = 1.8f;
        [Tooltip("Tầm vồ: lúc vồ mà mặt bóng gần hơn mức này thì nổ.")]
        [SerializeField] float reach = 1.9f;
        [Tooltip("Dậy bao lâu thì vồ (giây). GDD §5.1 nhóm D: báo trước ít nhất 0.6.")]
        [SerializeField] float alertSeconds = 0.6f;
        [SerializeField] float pounceSeconds = 0.25f;
        [Tooltip("Vồ xong nghỉ bao lâu rồi ngủ lại.")]
        [SerializeField] float restSeconds = 2f;
        [SerializeField] Transform body;
        [SerializeField] Transform earL;
        [SerializeField] Transform earR;
        [SerializeField] Transform zzz;
        [SerializeField] SpriteRenderer bodyRenderer;
        [SerializeField] Color sleepColor = new Color(0.55f, 0.5f, 0.6f);
        [SerializeField] Color alertColor = new Color(1f, 0.6f, 0.1f);

        Mode mode;
        float t;
        BalloonController balloon;
        Vector3 bodyHome, zzzHome;

        void Awake()
        {
            if (body != null) bodyHome = body.localPosition;
            if (zzz != null) zzzHome = zzz.localPosition;
        }

        void Start() { balloon = FindAnyObjectByType<BalloonController>(); }

        void Update()
        {
            if (balloon == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            t += dt;
            Vector2 toBalloon = balloon.Center - (Vector2)transform.position;
            float d = toBalloon.magnitude - balloon.Radius;
            bool alive = balloon.State != BalloonState.Empty && balloon.State != BalloonState.Goal;

            switch (mode)
            {
                case Mode.Sleep:
                    if (zzz != null)
                    {
                        zzz.gameObject.SetActive(true);
                        zzz.localPosition = zzzHome + Vector3.up * Mathf.Repeat(t * 0.3f, 0.4f);
                    }
                    // Tai giật khi bóng tới gần mép bán kính.
                    float twitch = alive && d < wakeRadius + 0.6f ? Mathf.Sin(t * 40f) * 12f : 0f;
                    if (earL != null) earL.localRotation = Quaternion.Euler(0f, 0f, twitch);
                    if (earR != null) earR.localRotation = Quaternion.Euler(0f, 0f, -twitch);
                    if (bodyRenderer != null) bodyRenderer.color = sleepColor;
                    if (alive && balloon.Started && d < wakeRadius) Enter(Mode.Alert);
                    break;
                case Mode.Alert:
                    if (zzz != null) zzz.gameObject.SetActive(false);
                    if (bodyRenderer != null) bodyRenderer.color = Color.Lerp(sleepColor, alertColor, Mathf.PingPong(t * 8f, 1f));
                    if (body != null) body.localScale = Vector3.one * (1f + 0.15f * (t / alertSeconds));
                    if (t >= alertSeconds) Enter(Mode.Pounce);
                    break;
                case Mode.Pounce:
                    if (bodyRenderer != null) bodyRenderer.color = alertColor;
                    if (body != null)
                    {
                        float k = Mathf.Sin(Mathf.Clamp01(t / pounceSeconds) * Mathf.PI);
                        Vector2 dir = toBalloon.sqrMagnitude > 1e-4f ? toBalloon.normalized : Vector2.up;
                        body.localPosition = bodyHome + (Vector3)(dir * (Mathf.Min(reach, toBalloon.magnitude) * 0.6f * k));
                    }
                    if (t >= pounceSeconds * 0.5f && alive && d < reach) balloon.Pop(FailReason.CatAttack);
                    if (t >= pounceSeconds) Enter(Mode.Rest);
                    break;
                case Mode.Rest:
                    if (body != null)
                    {
                        body.localPosition = bodyHome;
                        body.localScale = Vector3.one;
                    }
                    if (bodyRenderer != null) bodyRenderer.color = sleepColor;
                    if (t >= restSeconds) Enter(Mode.Sleep);
                    break;
            }
        }

        void Enter(Mode m) { mode = m; t = 0f; }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, wakeRadius);
        }
    }
}
