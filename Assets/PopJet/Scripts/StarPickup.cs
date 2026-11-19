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
                transform.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime);
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
