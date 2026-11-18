using UnityEngine;
using UnityEngine.UI;

namespace PopJet
{
    /// <summary>
    /// Thanh khí chia ô trên HUD (GDD §10.3): mỗi ô = 1 cú chọc (ventAirCost); ô cuối ngắn hơn
    /// nếu 1 không chia hết cho ventAirCost. Các ô tự sinh lúc chạy bên trong RectTransform này.
    /// Khí chỉ còn đủ 1 cú chọc thì thanh chuyển màu cam.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class AirBarUI : MonoBehaviour
    {
        [SerializeField] BalloonController balloon;
        [Tooltip("Sprite bo góc (9-slice) cho từng ô. Để trống thì vẽ hình chữ nhật.")]
        [SerializeField] Sprite slotSprite;
        [SerializeField] float gap = 8f;
        [SerializeField] Color fullColor = new Color(0.114f, 0.62f, 0.459f);   // #1D9E75
        [SerializeField] Color lowColor = new Color(1f, 0.541f, 0f);           // #FF8A00
        [SerializeField] Color emptyColor = new Color(0.17f, 0.17f, 0.23f, 0.6f);

        RectTransform[] fills;
        Image[] fillImages;
        float[] slotStart, slotSize;
        float builtCost = -1f;

        void LateUpdate()
        {
            if (balloon == null) return;
            if (!Mathf.Approximately(builtCost, balloon.VentAirCost) && !Build()) return;

            float air = balloon.Air01;
            Color c = balloon.IsLastPoke ? lowColor : fullColor;
            for (int i = 0; i < fills.Length; i++)
            {
                float f = Mathf.Clamp01((air - slotStart[i]) / slotSize[i]);
                fills[i].anchorMax = new Vector2(f, 1f);
                fillImages[i].enabled = f > 0.001f;
                fillImages[i].color = c;
            }
        }

        bool Build()
        {
            var rt = (RectTransform)transform;
            float width = rt.rect.width;
            if (width <= 0f) return false;

            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

            builtCost = balloon.VentAirCost;
            float cost = Mathf.Max(0.01f, builtCost);
            int n = Mathf.CeilToInt(1f / cost - 0.0001f);
            float unit = (width - gap * (n - 1)) * cost; // bề rộng của một ô đầy

            fills = new RectTransform[n];
            fillImages = new Image[n];
            slotStart = new float[n];
            slotSize = new float[n];
            float x = 0f;
            for (int i = 0; i < n; i++)
            {
                slotStart[i] = i * cost;
                slotSize[i] = Mathf.Min(cost, 1f - i * cost);
                float w = unit * slotSize[i] / cost;

                var bg = NewImage("Slot_" + i, transform, emptyColor);
                var bgRect = bg.rectTransform;
                bgRect.anchorMin = new Vector2(0f, 0f);
                bgRect.anchorMax = new Vector2(0f, 1f);
                bgRect.pivot = new Vector2(0f, 0.5f);
                bgRect.anchoredPosition = new Vector2(x, 0f);
                bgRect.sizeDelta = new Vector2(w, 0f);

                var fill = NewImage("Fill", bg.transform, fullColor);
                var fillRect = fill.rectTransform;
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
                fills[i] = fillRect;
                fillImages[i] = fill;
                x += w + gap;
            }
            return true;
        }

        Image NewImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = slotSprite;
            img.type = slotSprite != null && slotSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
    }
}
