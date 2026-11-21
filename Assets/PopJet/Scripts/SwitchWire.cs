using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Dây mờ nối công tắc với cửa (GDD §8.4 màn 1-8): báo "nút này mở cửa kia" mà không cần chữ.
    /// Chưa bấm: một vệt sáng chạy dọc dây từ công tắc tới cửa. Bấm rồi: cả dây sáng lên rồi mờ dần.
    /// Các chấm dọc dây do builder dựng sẵn trong prefab màn. Chỉ là hình. Scaled time: đứng yên khi thế giới dừng.
    /// </summary>
    public class SwitchWire : MonoBehaviour
    {
        [SerializeField] ButtonSwitch source;
        [Tooltip("Các chấm dọc dây, theo thứ tự từ công tắc tới cửa.")]
        [SerializeField] SpriteRenderer[] dots = new SpriteRenderer[0];
        [SerializeField] Color idleColor = new Color(1f, 0.85f, 0.35f, 0.22f);
        [SerializeField] Color pulseColor = new Color(1f, 0.9f, 0.45f, 0.75f);
        [SerializeField] Color litColor = new Color(1f, 0.9f, 0.45f, 0.95f);
        [Tooltip("Vệt sáng chạy qua bao nhiêu chấm mỗi giây.")]
        [SerializeField] float pulseSpeed = 8f;

        float t, litT = -1f;

        /// <summary>true = công tắc đã bấm, dây đã sáng.</summary>
        public bool IsLit => litT >= 0f;

        void Update()
        {
            if (dots.Length == 0) return;
            float dt = Time.deltaTime;
            if (source != null && source.IsPressed && litT < 0f) litT = 0f;
            if (litT >= 0f)
            {
                litT += dt;
                // Sáng hết rồi mờ dần về mức nhạt hơn lúc chưa bấm.
                float k = Mathf.Clamp01((litT - 0.4f) / 1.2f);
                var c = Color.Lerp(litColor, new Color(litColor.r, litColor.g, litColor.b, idleColor.a * 0.5f), k);
                foreach (var d in dots) if (d != null) d.color = c;
                return;
            }
            t += dt * pulseSpeed;
            float head = Mathf.Repeat(t, dots.Length + 6f);
            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] == null) continue;
                float d = head - i;
                float glow = d >= 0f && d < 4f ? 1f - d / 4f : 0f;
                dots[i].color = Color.Lerp(idleColor, pulseColor, glow);
            }
        }
    }
}
