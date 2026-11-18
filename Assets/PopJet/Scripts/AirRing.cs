using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Vòng khí quanh bóng, chỉ hiện khi đang ngắm (GDD §3.2). Đọc theo chiều kim đồng hồ từ đỉnh:
    /// phần xanh = khí còn lại, vạch tối = ranh giới mỗi cú chọc (ventAirCost),
    /// đoạn nhấp nháy = phần khí cú chọc này sẽ tiêu. Cú này làm hết khí thì cả vòng chuyển màu cam.
    /// Viền tối phía dưới giữ tương phản trên mọi nền. Chạy bằng unscaled time vì thế giới đang dừng.
    /// </summary>
    public class AirRing : MonoBehaviour
    {
        [SerializeField] BalloonInput input;
        [SerializeField] BalloonController balloon;
        [SerializeField] Material material;
        [Tooltip("Khoảng cách từ mặt bóng tới vòng (đơn vị world).")]
        [SerializeField] float gap = 0.16f;
        [SerializeField] float width = 0.07f;
        [SerializeField] float blinkHz = 2.5f;
        [SerializeField] Color fullColor = new Color(0.114f, 0.62f, 0.459f);   // #1D9E75
        [SerializeField] Color warnColor = new Color(1f, 0.541f, 0f);          // #FF8A00
        [SerializeField] Color emptyColor = new Color(0.85f, 0.82f, 0.78f);    // #D8D2C8
        [SerializeField] Color outlineColor = new Color(0.17f, 0.17f, 0.23f, 0.55f);
        [SerializeField] int sortingOrder = 17;

        const int MaxTicks = 12;

        LineRenderer under, empty, fill, cost;
        readonly LineRenderer[] ticks = new LineRenderer[MaxTicks];
        bool shown;

        void LateUpdate()
        {
            bool show = input != null && balloon != null && input.IsAiming && balloon.CanAim;
            if (!show)
            {
                if (shown) SetShown(false);
                return;
            }
            EnsureBuilt();
            if (!shown) SetShown(true);

            Vector2 c = balloon.Center;
            float r = balloon.Radius + gap;
            float air = balloon.Air01;
            float step = Mathf.Max(0.01f, balloon.VentAirCost);
            bool warn = balloon.IsLastPoke;
            Color main = warn ? warnColor : fullColor;

            Arc(under, c, r, 0f, 1f);
            Arc(empty, c, r, 0f, 1f);
            fill.startColor = fill.endColor = main;
            Arc(fill, c, r, 0f, air);

            if (input.HasAimDirection)
            {
                float blink = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * blinkHz);
                Color cc = Color.Lerp(Color.white, main, 0.35f);
                cc.a = Mathf.Lerp(0.35f, 1f, blink);
                cost.startColor = cost.endColor = cc;
                Arc(cost, c, r, Mathf.Max(0f, air - step), air);
            }
            else cost.enabled = false;

            for (int i = 0; i < MaxTicks; i++)
            {
                float a = (i + 1) * step;
                bool on = a < 0.999f;
                ticks[i].enabled = on;
                if (!on) continue;
                Vector2 dir = Dir(a);
                ticks[i].SetPosition(0, c + dir * (r - width * 0.9f));
                ticks[i].SetPosition(1, c + dir * (r + width * 0.9f));
            }
        }

        static Vector2 Dir(float amount01)
        {
            float deg = 90f - amount01 * 360f; // bắt đầu ở đỉnh, đi theo chiều kim đồng hồ
            float rad = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        static void Arc(LineRenderer lr, Vector2 c, float r, float from01, float to01)
        {
            float span = to01 - from01;
            if (span <= 0.002f) { lr.enabled = false; return; }
            lr.enabled = true;
            int n = Mathf.Max(2, Mathf.CeilToInt(span * 72f) + 1);
            lr.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Lerp(from01, to01, i / (float)(n - 1));
                lr.SetPosition(i, c + Dir(a) * r);
            }
        }

        void SetShown(bool value)
        {
            shown = value;
            if (under == null) return;
            under.enabled = empty.enabled = fill.enabled = cost.enabled = value;
            foreach (var t in ticks) t.enabled = value;
        }

        void EnsureBuilt()
        {
            if (under != null) return;
            under = NewLine("Outline", width * 1.7f, outlineColor, sortingOrder);
            empty = NewLine("Empty", width, emptyColor, sortingOrder + 1);
            fill = NewLine("Fill", width, fullColor, sortingOrder + 2);
            cost = NewLine("Cost", width, Color.white, sortingOrder + 3);
            for (int i = 0; i < MaxTicks; i++)
            {
                var t = NewLine("Tick_" + i, width * 0.45f, new Color(outlineColor.r, outlineColor.g, outlineColor.b, 1f), sortingOrder + 4);
                t.positionCount = 2;
                ticks[i] = t;
            }
        }

        LineRenderer NewLine(string name, float w, Color c, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.widthMultiplier = w;
            lr.startColor = lr.endColor = c;
            lr.numCapVertices = 2;
            lr.sortingOrder = order;
            if (material != null) lr.sharedMaterial = material;
            lr.enabled = false;
            return lr;
        }
    }
}
