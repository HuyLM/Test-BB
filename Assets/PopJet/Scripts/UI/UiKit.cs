using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PopJet
{
    /// <summary>
    /// Bộ dựng UI bằng code lúc chạy (UGUI + TextMeshPro) cho HUD đầy đủ và các màn hình (GDD §10).
    /// Dựng bằng code để PopJetSceneBuilder không phải nối hàng trăm tham chiếu bằng chuỗi tên field.
    /// Màu và cỡ chữ là starting value theo phong cách "bơm hơi trên giấy" (§10.1).
    /// </summary>
    public static class UiKit
    {
        public static readonly Color Ink = new Color(0.17f, 0.17f, 0.23f);
        public static readonly Color Paper = new Color(0.98f, 0.96f, 0.91f);
        public static readonly Color Dim = new Color(0.05f, 0.06f, 0.1f, 0.72f);
        public static readonly Color Primary = new Color(0.98f, 0.45f, 0.32f);
        public static readonly Color Secondary = new Color(0.36f, 0.62f, 0.92f);
        public static readonly Color Neutral = new Color(0.85f, 0.85f, 0.88f);
        public static readonly Color Gold = new Color(1f, 0.824f, 0.247f);
        public static readonly Color StarOff = new Color(1f, 1f, 1f, 0.18f);
        public static readonly Color Orange = new Color(1f, 0.541f, 0f);

        /// <summary>Font TMP dùng chung. Để trống thì lấy font mặc định trong TMP Settings.</summary>
        public static TMP_FontAsset Font;
        /// <summary>Material TMP có viền (preset dùng chung, builder tạo ở Art/TMP_Outline.mat).</summary>
        public static Material OutlineMaterial;
        /// <summary>Sprite bo góc (9-slice) cho nút và bảng.</summary>
        public static Sprite RoundRect;

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color, bool raycast = false)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border.sqrMagnitude > 0f) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool outline = false)
        {
            var t = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null) t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            // Dùng material preset có sẵn viền: đặt outlineWidth lúc chạy tạo material instance và lỗi khi chữ chưa vẽ lần nào.
            if (outline && OutlineMaterial != null) t.fontSharedMaterial = OutlineMaterial;
            return t;
        }

        /// <summary>Nút viên thuốc phồng (§10.1): nền bo góc, chữ ở giữa, nhấn thì xẹp, thả thì bật lại.</summary>
        public static Button Button(string name, Transform parent, string text, Color color, Action onClick, float fontSize = 64f)
        {
            var img = Image(name, parent, RoundRect, color, true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            img.gameObject.AddComponent<PuffyButton>();
            var label = Label("Label", img.transform, text, fontSize, Ink);
            Stretch(label.rectTransform);
            return btn;
        }

        public static TextMeshProUGUI ButtonLabel(Button b) => b.GetComponentInChildren<TextMeshProUGUI>(true);

        /// <summary>Lớp nền tối phủ toàn màn hình, chặn chạm xuống màn chơi.</summary>
        public static RectTransform Overlay(string name, Transform parent, Color dim)
        {
            var img = Image(name, parent, null, dim, true);
            Stretch(img.rectTransform);
            return img.rectTransform;
        }

        /// <summary>Tấm thiệp giấy ở giữa màn hình (§10.1), hiện ra bằng hoạt ảnh "bơm phồng".</summary>
        public static RectTransform Card(string name, Transform parent, Vector2 size)
        {
            var img = Image(name, parent, RoundRect, Paper, true);
            Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            img.gameObject.AddComponent<PopupAnim>();
            return img.rectTransform;
        }

        public static VerticalLayoutGroup Column(RectTransform rt, float spacing, int padding)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static LayoutElement Size(Component c, float height, float width = -1f)
        {
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            if (width > 0f) { le.preferredWidth = width; le.minWidth = width; }
            return le;
        }
    }

    /// <summary>Nhấn thì xẹp xuống 0.92, thả ra bật lại vọt quá một chút (§10.1). Chạy bằng unscaled time.</summary>
    public class PuffyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        float scale = 1f, velocity, target = 1f;

        public void OnPointerDown(PointerEventData e)
        {
            target = 0.92f;
            Haptics.Selection();
        }

        public void OnPointerUp(PointerEventData e) { target = 1f; velocity += 3f; }

        void OnDisable() { scale = target = 1f; velocity = 0f; transform.localScale = Vector3.one; }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            // Lò xo tắt dần: k = 400, c = 22 (starting value).
            velocity += ((target - scale) * 400f - velocity * 22f) * dt;
            scale += velocity * dt;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    /// <summary>Bơm phồng khi hiện: 0 → 1.08 → 1 trong 0.25 giây (§10.1). Unscaled time.</summary>
    public class PopupAnim : MonoBehaviour
    {
        float t;

        void OnEnable() { t = 0f; transform.localScale = Vector3.one * 0.01f; }

        void Update()
        {
            if (t >= 0.25f) return;
            t += Time.unscaledDeltaTime;
            float s = t < 0.16f ? Mathf.Lerp(0.01f, 1.08f, t / 0.16f) : Mathf.Lerp(1.08f, 1f, (t - 0.16f) / 0.09f);
            if (t >= 0.25f) s = 1f;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
