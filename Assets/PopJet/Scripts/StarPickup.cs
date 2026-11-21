using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Sao của màn (mục tiêu ★ thứ hai, GDD §3.4): bóng chạm vào là nhặt, báo cho LevelSession.
    /// Mỗi màn nên có đúng 1 Sao, đặt lệch khỏi đường đi ngắn nhất. Collider phải là trigger.
    /// Xoay nhẹ theo scaled time (đứng yên khi thế giới dừng), nhặt xong thì phồng lên rồi biến mất.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class StarPickup : MonoBehaviour
    {
        [Tooltip("Object con chứa hình Sao. Có thì chỉ hình xoay, collider đứng yên (vật lý không phụ thuộc khung hình, " +
                 "lời giải phát lại ra y hệt). Để trống thì xoay cả object.")]
        [SerializeField] Transform visual;
        [SerializeField] float spinDegreesPerSecond = 40f;
        [SerializeField] float popSeconds = 0.2f;

        Collider2D col;
        Renderer[] renderers;
        Vector3 baseScale;
        bool taken;
        float takenTime;

        void Awake()
        {
            col = GetComponent<Collider2D>();
            col.isTrigger = true;
            renderers = GetComponentsInChildren<Renderer>();
            baseScale = transform.localScale;
            if (visual == null) visual = transform;
        }

        /// <summary>Đã nhặt ở lượt trước cột mốc (chơi lại từ cột mốc): ẩn luôn, không báo lại.</summary>
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
            takenTime = 0f;
            col.enabled = false;
            var session = LevelSession.Current;
            if (session != null) session.CollectStar(this);
        }

        void Update()
        {
            if (!taken)
            {
                visual.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime);
                return;
            }
            takenTime += Time.deltaTime;
            float t = takenTime / Mathf.Max(0.01f, popSeconds);
            transform.localScale = baseScale * (1f + 0.6f * t);
            if (t >= 1f)
            {
                foreach (var r in renderers) r.enabled = false;
                enabled = false;
            }
        }
    }
}
