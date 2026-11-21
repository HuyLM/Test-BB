using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Mèo ngủ (GDD §5.2 #20, nhóm D): bóng vào bán kính thì mèo dậy, sau alertSeconds thì vồ.
    /// Lúc vồ mà bóng còn trong tầm thì nổ (CatAttack). Báo trước: "Zzz" khi ngủ, tai giật khi bóng gần mép bán kính,
    /// đổi màu và phồng lên khi dậy.
    /// Luật (dậy, vồ, nổ) chạy trong FixedUpdate theo vị trí vật lý của bóng, nên không phụ thuộc khung hình và lời giải
    /// đã ghi phát lại ra y hệt. Phần nhìn chạy trong Update. Cả hai dừng khi thế giới dừng (lúc ngắm).
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
        [Tooltip("Art AI (tuỳ chọn): hình mèo ngủ / mèo thức gán vào bodyRenderer theo trạng thái. Chỉ là phần nhìn.")]
        [SerializeField] Sprite sleepSprite;
        [SerializeField] Sprite awakeSprite;

        Mode mode;
        float t;            // thời gian trong trạng thái hiện tại (bước vật lý)
        Mode shownMode;
        float vt;           // thời gian trong trạng thái hiện tại theo khung hình (chỉ cho phần nhìn)
        BalloonController balloon;
        Vector3 bodyHome, zzzHome;

        void Awake()
        {
            if (body != null) bodyHome = body.localPosition;
            if (zzz != null) zzzHome = zzz.localPosition;
        }

        void Start() { balloon = FindAnyObjectByType<BalloonController>(); }

        /// <summary>Khoảng cách từ mèo tới mặt bóng, theo vị trí vật lý (không nội suy).</summary>
        float Gap(out Vector2 toBalloon)
        {
            toBalloon = balloon.Body.position - (Vector2)transform.position;
            return toBalloon.magnitude - balloon.Radius;
        }

        bool BalloonAlive => balloon.State != BalloonState.Empty && balloon.State != BalloonState.Goal;

        void FixedUpdate()
        {
            if (balloon == null) return;
            t += Time.fixedDeltaTime;
            float d = Gap(out _);
            bool alive = BalloonAlive;
            switch (mode)
            {
                case Mode.Sleep:
                    if (alive && balloon.Started && d < wakeRadius) Enter(Mode.Alert);
                    break;
                case Mode.Alert:
                    if (t >= alertSeconds) Enter(Mode.Pounce);
                    break;
                case Mode.Pounce:
                    SetLook(true);
                    if (t >= pounceSeconds * 0.5f && alive && d < reach) balloon.Pop(FailReason.CatAttack);
                    if (t >= pounceSeconds) Enter(Mode.Rest);
                    break;
                case Mode.Rest:
                    if (t >= restSeconds) Enter(Mode.Sleep);
                    break;
            }
        }

        void Update()
        {
            if (balloon == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (shownMode != mode) { shownMode = mode; vt = 0f; }
            vt += dt;
            float d = Gap(out Vector2 toBalloon);
            bool alive = BalloonAlive;

            switch (mode)
            {
                case Mode.Sleep:
                    if (zzz != null)
                    {
                        zzz.gameObject.SetActive(true);
                        zzz.localPosition = zzzHome + Vector3.up * Mathf.Repeat(vt * 0.3f, 0.4f);
                    }
                    if (body != null)
                    {
                        body.localPosition = bodyHome;
                        body.localScale = Vector3.one;
                    }
                    // Tai giật khi bóng tới gần mép bán kính.
                    float twitch = alive && d < wakeRadius + 0.6f ? Mathf.Sin(vt * 40f) * 12f : 0f;
                    if (earL != null) earL.localRotation = Quaternion.Euler(0f, 0f, twitch);
                    if (earR != null) earR.localRotation = Quaternion.Euler(0f, 0f, -twitch);
                    if (bodyRenderer != null) bodyRenderer.color = sleepColor;
                    SetLook(false);
                    break;
                case Mode.Alert:
                    SetLook(true);
                    if (zzz != null) zzz.gameObject.SetActive(false);
                    if (bodyRenderer != null) bodyRenderer.color = Color.Lerp(sleepColor, alertColor, Mathf.PingPong(vt * 8f, 1f));
                    if (body != null) body.localScale = Vector3.one * (1f + 0.15f * Mathf.Clamp01(vt / alertSeconds));
                    break;
                case Mode.Pounce:
                    SetLook(true);
                    if (bodyRenderer != null) bodyRenderer.color = alertColor;
                    if (body != null)
                    {
                        float k = Mathf.Sin(Mathf.Clamp01(vt / pounceSeconds) * Mathf.PI);
                        Vector2 dir = toBalloon.sqrMagnitude > 1e-4f ? toBalloon.normalized : Vector2.up;
                        body.localPosition = bodyHome + (Vector3)(dir * (Mathf.Min(reach, toBalloon.magnitude) * 0.6f * k));
                    }
                    break;
                case Mode.Rest:
                    if (body != null)
                    {
                        body.localPosition = bodyHome;
                        body.localScale = Vector3.one;
                    }
                    if (bodyRenderer != null) bodyRenderer.color = sleepColor;
                    SetLook(false);
                    break;
            }
        }

        void SetLook(bool awake)
        {
            if (bodyRenderer == null) return;
            var s = awake ? awakeSprite : sleepSprite;
            if (s != null) bodyRenderer.sprite = s;
        }

        void Enter(Mode m) { mode = m; t = 0f; }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, wakeRadius);
        }
    }
}
