using UnityEngine;
using UnityEngine.UI;

namespace PopJet
{
    /// <summary>
    /// Chỉ báo đích ngoài màn hình (GDD §10.3 "↗ đích"): khi đích nằm ngoài khung camera thì hiện mũi tên ở mép
    /// vùng an toàn của HUD, chĩa về phía đích, nhún nhẹ. Đích trong khung, đã về đích hoặc đang mở màn hình thì ẩn.
    /// Dựng lúc chạy trong SafeArea. Chạy bằng unscaled time (vẫn chỉ đúng khi thế giới dừng lúc ngắm).
    /// </summary>
    public class GoalIndicator : MonoBehaviour
    {
        [SerializeField] BalloonController balloon;
        [SerializeField] GameScreens screens;
        [Tooltip("Vùng an toàn của HUD: mũi tên nằm trong này.")]
        [SerializeField] RectTransform safeArea;
        [Tooltip("Sprite tam giác, đỉnh hướng +Y.")]
        [SerializeField] Sprite arrowSprite;
        [SerializeField] Color color = new Color(0.3f, 0.85f, 0.45f, 0.95f);
        [Tooltip("Mũi tên cách mép vùng an toàn bao nhiêu (đơn vị canvas). Phía trên chừa chỗ cho thanh trên của HUD.")]
        [SerializeField] float edgeMargin = 90f;
        [SerializeField] float topMargin = 260f;
        [SerializeField] float bottomMargin = 230f;

        Camera cam;
        Transform goal;
        RectTransform arrow;
        Image image;

        /// <summary>true = mũi tên đang hiện (test đọc).</summary>
        public bool IsShown => image != null && image.enabled;

        void Start()
        {
            cam = Camera.main;
            var g = FindAnyObjectByType<GoalZone>();
            if (g != null) goal = g.transform;
            if (safeArea == null || arrowSprite == null) return;
            image = UiKit.Image("GoalArrow", safeArea, arrowSprite, color);
            arrow = image.rectTransform;
            arrow.sizeDelta = new Vector2(70f, 84f);
            arrow.anchorMin = arrow.anchorMax = new Vector2(0.5f, 0.5f);
            image.enabled = false;
        }

        void LateUpdate()
        {
            if (image == null) return;
            bool show = goal != null && cam != null && balloon != null &&
                        balloon.State != BalloonState.Goal && balloon.State != BalloonState.Empty &&
                        !(screens != null && (screens.IsScreenOpen || screens.IsHintPlaying));
            Vector3 vp = show ? cam.WorldToViewportPoint(goal.position) : Vector3.zero;
            if (show) show = vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f;
            if (image.enabled != show) image.enabled = show;
            if (!show) return;

            // Hướng từ tâm màn hình tới đích, cắt vào khung vùng an toàn trừ lề.
            Rect r = safeArea.rect;
            Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            if (dir.sqrMagnitude < 1e-6f) dir = Vector2.up;
            dir.Normalize();
            float halfW = r.width * 0.5f - edgeMargin;
            float top = r.height * 0.5f - topMargin, bottom = -(r.height * 0.5f - bottomMargin);
            float tx = Mathf.Abs(dir.x) > 1e-4f ? halfW / Mathf.Abs(dir.x) : float.MaxValue;
            float ty = dir.y > 1e-4f ? top / dir.y : dir.y < -1e-4f ? bottom / dir.y : float.MaxValue;
            float t = Mathf.Min(tx, ty);
            float bob = Mathf.Sin(Time.unscaledTime * 5f) * 10f;
            arrow.anchoredPosition = dir * (t - bob); // neo ở giữa vùng an toàn
            arrow.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
        }
    }
}
