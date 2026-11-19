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
        [SerializeField] float bobAmplitude = 0.06f;
        [SerializeField] float popSeconds = 0.15f;

        Collider2D col;
        SpriteRenderer[] renderers;
        Vector3 basePos, baseScale;
        bool taken;
        float t;

        public int Id => id;

        void Awake()
        {
            col = GetComponent<Collider2D>();
            col.isTrigger = true;
            renderers = GetComponentsInChildren<SpriteRenderer>();
            basePos = transform.localPosition;
            baseScale = transform.localScale;
        }

        void Start()
        {
            var s = LevelSession.Current;
            if (s != null && s.CoinAlreadyOwned(id))
                foreach (var r in renderers) { var c = r.color; c.a *= 0.35f; r.color = c; }
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
                transform.localPosition = basePos + Vector3.up * (Mathf.Sin((t + id * 0.37f) * 3f) * bobAmplitude);
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
