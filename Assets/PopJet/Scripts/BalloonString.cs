using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Dây bóng (GDD §4.2): dây mềm mô phỏng verlet, vẽ bằng LineRenderer, chỉ để nhìn (không va chạm).
    /// Dây kéo ngược hướng bay nên cho thấy bóng đang đi đâu, nhanh hay chậm.
    /// Chạy theo scaled time nên đứng sững khi thế giới dừng. Bóng vỡ thì dây rơi xuống rồi mờ dần.
    /// Gắn vào object gốc của bóng (gốc không xoay vì freezeRotation). Mọi giá trị số là starting value.
    /// </summary>
    [RequireComponent(typeof(BalloonController), typeof(CircleCollider2D))]
    public class BalloonString : MonoBehaviour
    {
        [SerializeField] Material material;
        [SerializeField] Color color = new Color(0.42f, 0.36f, 0.31f);
        [SerializeField] float length = 0.9f;
        [SerializeField, Range(3, 16)] int segments = 8;
        [SerializeField] float width = 0.025f;
        [SerializeField] float gravity = 6f;
        [Tooltip("Giữ lại bao nhiêu vận tốc mỗi bước (thấp = dây ít đung đưa).")]
        [SerializeField, Range(0.8f, 1f)] float keepVelocity = 0.97f;
        [SerializeField] float fadeSeconds = 1f;
        [SerializeField] int sortingOrder = 9;

        BalloonController balloon;
        CircleCollider2D circle;
        LineRenderer line;
        Vector2[] pos, prev;
        bool attached = true;
        float fallTime;

        void Awake()
        {
            balloon = GetComponent<BalloonController>();
            circle = GetComponent<CircleCollider2D>();

            var go = new GameObject("String");
            go.transform.SetParent(transform, false);
            line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = segments + 1;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.startColor = line.endColor = color;
            line.sortingOrder = sortingOrder;
            if (material != null) line.sharedMaterial = material;

            pos = new Vector2[segments + 1];
            prev = new Vector2[segments + 1];
            Vector2 a = Anchor;
            float seg = length / segments;
            for (int i = 0; i <= segments; i++) pos[i] = prev[i] = a + Vector2.down * (seg * i);
            Draw();
        }

        void OnEnable() { balloon.BecameEmpty += OnEmpty; }
        void OnDisable() { balloon.BecameEmpty -= OnEmpty; }

        void OnEmpty() { if (balloon.Burst) attached = false; } // vỡ thì dây rơi tự do

        /// <summary>Núm buộc dây ở đáy bóng; co lên theo lượng khí vì gốc bóng scale theo khí.</summary>
        Vector2 Anchor => transform.TransformPoint(new Vector2(circle.offset.x, circle.offset.y - circle.radius - 0.04f));

        void LateUpdate()
        {
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
            if (dt <= 0f) return; // thế giới đang dừng: dây đứng yên

            float seg = length / segments;
            Vector2 g = Vector2.down * (gravity * dt * dt);
            int start = attached ? 1 : 0;
            if (attached) pos[0] = prev[0] = Anchor;

            for (int i = start; i <= segments; i++)
            {
                Vector2 v = (pos[i] - prev[i]) * keepVelocity;
                prev[i] = pos[i];
                pos[i] += v + g;
            }

            for (int it = 0; it < 6; it++)
            {
                for (int i = 0; i < segments; i++)
                {
                    Vector2 d = pos[i + 1] - pos[i];
                    float l = d.magnitude;
                    if (l < 1e-6f) continue;
                    Vector2 corr = d * ((l - seg) / l);
                    if (i == 0 && attached) pos[i + 1] -= corr;
                    else
                    {
                        pos[i] += corr * 0.5f;
                        pos[i + 1] -= corr * 0.5f;
                    }
                }
            }

            if (!attached)
            {
                fallTime += dt;
                float a = Mathf.Clamp01(1f - fallTime / Mathf.Max(0.01f, fadeSeconds));
                var c = new Color(color.r, color.g, color.b, color.a * a);
                line.startColor = line.endColor = c;
                if (a <= 0f) { line.enabled = false; enabled = false; return; }
            }
            Draw();
        }

        void Draw()
        {
            for (int i = 0; i <= segments; i++) line.SetPosition(i, pos[i]);
        }
    }
}
