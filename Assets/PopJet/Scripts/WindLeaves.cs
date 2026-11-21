using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Lá giấy bay theo gió quạt (GDD §8.4 màn 1-7): cho thấy vùng gió và hướng gió mà không cần chữ.
    /// Gắn cùng object với Fan (BoxCollider2D trigger là vùng gió, +X local là hướng gió). Lá sinh lúc chạy, trôi dọc
    /// +X, lắc nhẹ, ra khỏi vùng thì quay lại đầu quạt. Quạt tắt thì lá chậm dần và mờ đi.
    /// Chỉ là hình, không có collider. Scaled time: đứng yên khi thế giới dừng.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class WindLeaves : MonoBehaviour
    {
        [SerializeField] Sprite leafSprite;
        [SerializeField] Material material;
        [SerializeField, Min(1)] int count = 6;
        [Tooltip("Tốc độ trôi (đơn vị world/giây). Starting value.")]
        [SerializeField] float speed = 1.6f;
        [Tooltip("Cỡ ảnh lá (đơn vị world). Hình lá chiếm khoảng 2/3 bề ngang ảnh.")]
        [SerializeField] float size = 0.3f;
        [SerializeField] Color color = new Color(0.56f, 0.78f, 0.52f, 0.85f);
        [SerializeField] int sortingOrder = 2;

        Transform[] leaves;
        SpriteRenderer[] renderers;
        float[] x, lane, phase, pace;
        float halfLength, halfWidth, flow = 1f;
        Fan fan;

        void Awake()
        {
            fan = GetComponent<Fan>();
            var box = GetComponent<BoxCollider2D>();
            halfLength = box.size.x * 0.5f;
            halfWidth = box.size.y * 0.5f;
            if (leafSprite == null) return;

            leaves = new Transform[count];
            renderers = new SpriteRenderer[count];
            x = new float[count];
            lane = new float[count];
            phase = new float[count];
            pace = new float[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Leaf_" + i);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = leafSprite;
                sr.color = color;
                sr.sortingOrder = sortingOrder;
                if (material != null) sr.sharedMaterial = material;
                leaves[i] = go.transform;
                renderers[i] = sr;
                // Rải đều dọc vùng gió, xen kẽ hai bên, nhịp hơi khác nhau (giá trị cố định, không random).
                x[i] = -halfLength + (i + 0.5f) * (2f * halfLength / count);
                lane[i] = ((i * 0.618f) % 1f - 0.5f) * 1.4f * halfWidth;
                phase[i] = i * 1.7f;
                pace[i] = 0.8f + 0.4f * ((i * 0.37f) % 1f);
            }
            Place();
        }

        void Update()
        {
            if (leaves == null) return;
            float dt = Time.deltaTime;
            bool on = fan == null || fan.IsOn;
            flow = Mathf.MoveTowards(flow, on ? 1f : 0f, dt * 1.5f);
            for (int i = 0; i < leaves.Length; i++)
            {
                x[i] += speed * pace[i] * flow * dt;
                if (x[i] > halfLength) x[i] -= 2f * halfLength;
                phase[i] += dt * 3f * pace[i];
            }
            Place();
        }

        void Place()
        {
            float scale = size / Mathf.Max(0.01f, leafSprite.bounds.size.x);
            for (int i = 0; i < leaves.Length; i++)
            {
                float y = Mathf.Clamp(lane[i] + Mathf.Sin(phase[i]) * 0.18f, -halfWidth * 0.9f, halfWidth * 0.9f);
                leaves[i].localPosition = new Vector3(x[i], y, 0f);
                leaves[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase[i] * 1.3f) * 35f);
                leaves[i].localScale = Vector3.one * scale;
                // Mờ ở hai đầu vùng gió để lá không hiện/biến đột ngột.
                float edge = Mathf.Clamp01((halfLength - Mathf.Abs(x[i])) / 0.4f);
                var c = color;
                c.a *= edge * Mathf.Lerp(0.35f, 1f, flow);
                renderers[i].color = c;
            }
        }
    }
}
