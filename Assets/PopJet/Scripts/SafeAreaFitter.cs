using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Co RectTransform vào vùng an toàn của màn hình (tai thỏ, Dynamic Island, thanh home).
    /// Gắn vào object con của Canvas chứa toàn bộ HUD.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform rect;
        Rect lastArea;
        Vector2Int lastScreen;

        void Awake()
        {
            rect = (RectTransform)transform;
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != lastArea || Screen.width != lastScreen.x || Screen.height != lastScreen.y) Apply();
        }

        void Apply()
        {
            lastArea = Screen.safeArea;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 min = lastArea.position;
            Vector2 max = lastArea.position + lastArea.size;
            min.x /= Screen.width; max.x /= Screen.width;
            min.y /= Screen.height; max.y /= Screen.height;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
