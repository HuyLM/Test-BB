using System.Collections.Generic;
using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Quỹ đạo dự đoán khi đang ngắm (GDD §3.2): chuỗi chấm theo đường bay dự kiến của cú chọc,
    /// tính cả vận tốc hiện tại, các jet đang chạy, trọng lực sau lần chọc đầu và damping.
    /// Dừng ở va chạm đầu tiên (vẽ bóng mờ ở đó) hoặc sau previewSeconds.
    /// Cú chọc này làm bóng hết khí thì đổi sang màu cảnh báo.
    /// Chỉ dùng truy vấn vật lý (CircleCast) nên vẫn chạy khi thế giới đang dừng (timeScale = 0).
    /// Không tính vật cản đang di chuyển. Mọi giá trị số là starting value.
    /// </summary>
    public class TrajectoryPreview : MonoBehaviour
    {
        [SerializeField] BalloonInput input;
        [SerializeField] BalloonController balloon;
        [SerializeField] Sprite dotSprite;
        [Tooltip("Sprite vòng tròn rỗng (1 đơn vị) cho bóng mờ ở điểm va chạm.")]
        [SerializeField] Sprite ghostSprite;
        [SerializeField] Material material;
        [Tooltip("Số giây mô phỏng tối đa. GDD §3.2: 0.6 giây. Người mới đoán sai nhiều thì tăng lên 1.0; quá dễ thì giảm 0.4.")]
        [SerializeField] float previewSeconds = 0.6f;
        [SerializeField] float dotInterval = 0.05f;
        [SerializeField] float dotSize = 0.09f;
        [SerializeField] Color color = new Color(0.114f, 0.62f, 0.459f);   // #1D9E75
        [SerializeField] Color warnColor = new Color(1f, 0.541f, 0f);      // #FF8A00
        [SerializeField] int sortingOrder = 16;

        const float Step = 0.02f; // khớp Fixed Timestep mặc định
        const int MaxDots = 24;

        struct SimJet
        {
            public Vector2 dir;
            public float thrust, duration, elapsed, airCost;
        }

        readonly List<JetInfo> jets = new List<JetInfo>();
        readonly List<SimJet> sim = new List<SimJet>();
        readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        readonly List<SpriteRenderer> rims = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> cores = new List<SpriteRenderer>();
        SpriteRenderer ghost;
        bool visible;

        void LateUpdate()
        {
            bool show = input != null && balloon != null && input.HasAimDirection && balloon.CanAim;
            if (!show)
            {
                if (visible) Show(0, false, color);
                return;
            }
            Simulate(input.FlyDirection);
        }

        void Simulate(Vector2 flyDir)
        {
            EnsurePool();
            var body = balloon.Body;
            Vector2 p = body.position;
            Vector2 v = balloon.Velocity;
            float radius = balloon.Radius;
            float mass = Mathf.Max(0.0001f, body.mass);
            float damping = balloon.Damping;
            Vector2 gravity = Physics2D.gravity * balloon.PlayGravityScale; // lần chọc đầu bật trọng lực này
            float air = balloon.Air01;

            sim.Clear();
            balloon.GetJets(jets);
            foreach (var j in jets)
                sim.Add(new SimJet { dir = j.dir, thrust = j.thrust, duration = j.duration, elapsed = j.elapsed, airCost = j.airCost });
            sim.Add(new SimJet
            {
                dir = flyDir.normalized,
                thrust = balloon.VentThrust,
                duration = balloon.VentDuration,
                airCost = balloon.VentAirCost
            });

            var filter = new ContactFilter2D();
            filter.useTriggers = false;
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(balloon.gameObject.layer));

            int steps = Mathf.CeilToInt(previewSeconds / Step);
            float t = 0f, nextDot = dotInterval;
            int dotCount = 0;
            bool hit = false, popped = false;

            for (int s = 0; s < steps; s++)
            {
                // Giống BalloonController.FixedUpdate: lực giảm tuyến tính, khí thấp thì lực yếu đi.
                float thrustScale = Mathf.Lerp(balloon.MinThrustWhenLow, 1f, Mathf.Clamp01(air));
                Vector2 force = Vector2.zero;
                for (int i = 0; i < sim.Count; i++)
                {
                    var j = sim[i];
                    if (j.elapsed >= j.duration) continue;
                    float k = 1f - j.elapsed / j.duration;
                    force += j.dir * (j.thrust * k * thrustScale);
                    air -= j.airCost * 2f * k * Step / j.duration;
                    j.elapsed += Step;
                    sim[i] = j;
                }

                // Cùng thứ tự tích phân với Box2D: cộng lực + trọng lực, rồi hãm theo damping.
                v += (force / mass + gravity) * Step;
                v *= 1f / (1f + Step * damping);

                Vector2 delta = v * Step;
                float dist = delta.magnitude;
                if (dist > 1e-5f && Cast(p, radius, delta / dist, dist, filter, body, out Vector2 centroid))
                {
                    p = centroid;
                    hit = true;
                    break;
                }
                p += delta;
                t += Step;
                if (air <= 0f) { popped = true; break; }
                if (t >= nextDot && dotCount < MaxDots)
                {
                    PlaceDot(dotCount, p, dotCount / (float)MaxDots);
                    dotCount++;
                    nextDot += dotInterval;
                }
            }

            Color c = (popped || balloon.IsLastPoke) ? warnColor : color;
            if (hit || popped)
            {
                ghost.transform.position = p;
                ghost.transform.localScale = Vector3.one * (radius * 2f);
            }
            Show(dotCount, hit || popped, c);
        }

        bool Cast(Vector2 origin, float radius, Vector2 dir, float dist, ContactFilter2D filter, Rigidbody2D self, out Vector2 centroid)
        {
            centroid = origin;
            // Bán kính nhỏ hơn 5% để bóng đang tựa tường không bị tính là "đã va" ngay từ đầu.
            int n = Physics2D.CircleCast(origin, radius * 0.95f, dir, filter, hits, dist);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                if (h.collider == null || h.collider.attachedRigidbody == self) continue;
                if (h.distance <= 0f) continue;
                if (h.distance < best)
                {
                    best = h.distance;
                    centroid = h.centroid;
                    found = true;
                }
            }
            return found;
        }

        void PlaceDot(int index, Vector2 pos, float along)
        {
            float size = dotSize * Mathf.Lerp(1f, 0.6f, along);
            var rim = rims[index];
            rim.transform.position = pos;
            rim.transform.localScale = Vector3.one * (size * 1.45f);
            cores[index].transform.localScale = Vector3.one * (1f / 1.45f);
        }

        void Show(int dotCount, bool showGhost, Color c)
        {
            visible = dotCount > 0 || showGhost;
            for (int i = 0; i < rims.Count; i++)
            {
                bool on = i < dotCount;
                rims[i].enabled = on;
                cores[i].enabled = on;
                if (on) cores[i].color = c;
            }
            if (ghost != null)
            {
                ghost.enabled = showGhost;
                ghost.color = new Color(c.r, c.g, c.b, 0.85f);
            }
        }

        void EnsurePool()
        {
            if (ghost != null) return;
            for (int i = 0; i < MaxDots; i++)
            {
                var rim = NewRenderer("Dot_" + i, transform, dotSprite, Color.white, sortingOrder);
                var core = NewRenderer("Core", rim.transform, dotSprite, color, sortingOrder + 1);
                rims.Add(rim);
                cores.Add(core);
            }
            ghost = NewRenderer("Ghost", transform, ghostSprite != null ? ghostSprite : dotSprite, color, sortingOrder);
            Show(0, false, color);
        }

        SpriteRenderer NewRenderer(string name, Transform parent, Sprite sprite, Color c, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = c;
            sr.sortingOrder = order;
            if (material != null) sr.sharedMaterial = material;
            sr.enabled = false;
            return sr;
        }
    }
}
