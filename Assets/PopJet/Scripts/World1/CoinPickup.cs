using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Xu (GDD §7.1, §12.2): vừa là tiền vừa là "vệt bánh mì" gợi ý đường đi.
    /// Chỉ lần nhặt đầu của mỗi xu được tính tiền (LevelSession kiểm tra theo id); xu đã sở hữu hiện mờ
    /// nhưng vẫn nhặt được để giữ cảm giác. Xu chỉ cộng vào ví khi về đích. Collider phải là trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CoinPickup : MonoBehaviour
    {
        [Tooltip("Mã xu, duy nhất trong một màn. Builder đánh số 0, 1, 2...")]
        [SerializeField] int id;
        [Tooltip("Object con chứa hình xu. Có thì chỉ hình nhún, collider đứng yên (vật lý không phụ thuộc khung hình, " +
                 "lời giải phát lại ra y hệt). Để trống thì nhún cả object.")]
        [SerializeField] Transform visual;
        [SerializeField] float bobAmplitude = 0.06f;
        [SerializeField] float popSeconds = 0.15f;

        Collider2D col;
        SpriteRenderer[] renderers;
        Transform bob;
        Vector3 basePos, baseScale;
        bool taken;
        float t;

        public int Id => id;

        void Awake()
        {
            col = GetComponent<Collider2D>();
            col.isTrigger = true;
            renderers = GetComponentsInChildren<SpriteRenderer>();
            bob = visual != null ? visual : transform;
            basePos = bob.localPosition;
            baseScale = transform.localScale;
        }

        void Start()
        {
            var s = LevelSession.Current;
            if (s != null && s.CoinAlreadyOwned(id))
                foreach (var r in renderers) { var c = r.color; c.a *= 0.35f; r.color = c; }
        }

        /// <summary>Đã nhặt ở lượt trước cột mốc (chơi lại từ cột mốc): ẩn luôn, không tính lại.</summary>
        public void HideCollected()
        {
            taken = true;
            col.enabled = false;
            foreach (var r in renderers) r.enabled = false;
            enabled = false;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (taken || other.GetComponentInParent<BalloonController>() == null) return;
            taken = true;
            t = 0f;
            col.enabled = false;
            var s = LevelSession.Current;
            if (s != null) s.CollectCoin(id);
        }

        void Update()
        {
            t += Time.deltaTime; // scaled: đứng yên khi thế giới dừng
            if (!taken)
            {
                // bobAmplitude tính theo đơn vị world: hình con nằm trong object đã scale nên chia lại.
                float amp = bob == transform ? bobAmplitude : bobAmplitude / Mathf.Max(0.01f, transform.lossyScale.y);
                bob.localPosition = basePos + Vector3.up * (Mathf.Sin((t + id * 0.37f) * 3f) * amp);
                return;
            }
            float k = t / Mathf.Max(0.01f, popSeconds);
            transform.localScale = baseScale * (1f + 0.8f * k);
            if (k >= 1f)
            {
                foreach (var r in renderers) r.enabled = false;
                enabled = false;
            }
        }
    }
}
