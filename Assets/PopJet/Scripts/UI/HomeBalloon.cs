using UnityEngine;
using UnityEngine.EventSystems;

namespace PopJet
{
    /// <summary>
    /// Sân chơi ở Trang chủ (GDD §10.2): Pốp lơ lửng giữa màn hình, chạm vào thì "chọc thử": bóng vọt về phía
    /// ngược chỗ chạm, xẹp nhẹ, rồi lò xo kéo về chỗ cũ. Dạy động từ chính của game ngay từ Trang chủ.
    /// Chỉ là UI (không vật lý, không tốn gì). Unscaled time vì Trang chủ dừng thế giới.
    /// </summary>
    public class HomeBalloon : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] float kick = 900f;
        [SerializeField] float stiffness = 60f;
        [SerializeField] float damping = 7f;

        RectTransform rt;
        Vector2 home, offset, velocity;
        float squash, t;

        /// <summary>Số lần đã chọc (test đọc).</summary>
        public int Pokes { get; private set; }

        void Awake()
        {
            rt = (RectTransform)transform;
            home = rt.anchoredPosition;
        }

        void OnEnable()
        {
            offset = velocity = Vector2.zero;
            squash = 0f;
        }

        public void OnPointerDown(PointerEventData e)
        {
            Vector2 local = Vector2.zero;
            if (rt.parent is RectTransform parent)
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out local);
            Poke(local);
        }

        /// <summary>Chọc tại điểm local (tính từ tâm bóng): bóng bay ngược hướng đó.</summary>
        public void Poke(Vector2 local)
        {
            Vector2 dir = local.sqrMagnitude > 1f ? -local.normalized : Vector2.up;
            velocity += dir * kick;
            squash = 0.25f;
            Pokes++;
            Haptics.Light();
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            t += dt;
            velocity += (-offset * stiffness - velocity * damping) * dt;
            offset += velocity * dt;
            squash = Mathf.MoveTowards(squash, 0f, dt * 1.5f);
            rt.anchoredPosition = home + offset + Vector2.up * Mathf.Sin(t * 1.6f) * 14f;
            float s = 1f - squash * Mathf.Abs(Mathf.Sin(t * 30f)) * 0.5f;
            rt.localScale = new Vector3(2f - s, s, 1f);
        }
    }
}
