using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Gai: không làm nổ bóng, chỉ tạo thêm một lỗ rò tại điểm chạm.
    /// Chạm lại sẽ tạo lỗ mới (BalloonController tự chống spam bằng cooldown và giới hạn số lỗ).
    /// Hoạt động với cả collider thường (chặn bóng) lẫn trigger (bóng xuyên qua).
    /// </summary>
    public class Spike : MonoBehaviour
    {
        void OnCollisionEnter2D(Collision2D c) { HandleCollision(c); }
        void OnCollisionStay2D(Collision2D c) { HandleCollision(c); }

        void OnTriggerEnter2D(Collider2D other) { HandleTrigger(other); }
        void OnTriggerStay2D(Collider2D other) { HandleTrigger(other); }

        void HandleCollision(Collision2D c)
        {
            var balloon = c.collider.GetComponentInParent<BalloonController>();
            if (balloon == null || c.contactCount == 0) return;
            balloon.AddLeak(c.GetContact(0).point);
        }

        void HandleTrigger(Collider2D other)
        {
            var balloon = other.GetComponentInParent<BalloonController>();
            if (balloon == null) return;
            balloon.AddLeak(other.ClosestPoint(transform.position));
        }
    }
}
