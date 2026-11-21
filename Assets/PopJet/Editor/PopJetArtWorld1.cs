using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PopJet.EditorTools
{
    /// <summary>
    /// Art Thế giới 1 tạo bằng AI (GDD §9.6). Sprite thành phẩm nằm ở Art/World1 (builder KHÔNG ghi đè thư mục này).
    /// Có file thì builder dùng sprite thật, thiếu file thì giữ placeholder hình học như cũ.
    /// Collider giữ nguyên kích thước gameplay; sprite chỉ là phần nhìn.
    /// </summary>
    public static partial class PopJetSceneBuilder
    {
        const string ArtWorld1Dir = ArtDir + "/World1";

        static readonly Dictionary<string, Sprite> artCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Nạp sprite Art/World1/{name}.png. ppu = 0: cạnh dài nhất = 1 đơn vị world.
        /// fullRect = true cho sprite dùng drawMode Tiled.
        /// </summary>
        static Sprite ArtSprite(string name, float ppu = 0f, bool fullRect = false, Vector2? pivot = null, Vector4 border = default)
        {
            if (artCache.TryGetValue(name, out var cached) && cached != null) return cached;
            string path = $"{ArtWorld1Dir}/{name}.png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return null;

            imp.GetSourceTextureWidthAndHeight(out int w, out int h);
            float wantPpu = ppu > 0f ? ppu : Mathf.Max(w, h);
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            bool dirty = imp.textureType != TextureImporterType.Sprite
                         || imp.spriteImportMode != SpriteImportMode.Single
                         || !Mathf.Approximately(imp.spritePixelsPerUnit, wantPpu)
                         || imp.mipmapEnabled
                         || settings.spriteMeshType != SpriteMeshType.FullRect
                         || imp.wrapMode != (fullRect ? TextureWrapMode.Repeat : TextureWrapMode.Clamp)
                         || imp.spriteBorder != border
                         || (pivot.HasValue ? (settings.spriteAlignment != (int)SpriteAlignment.Custom || settings.spritePivot != pivot.Value)
                                            : settings.spriteAlignment != (int)SpriteAlignment.Center);
            if (dirty)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = wantPpu;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.filterMode = FilterMode.Bilinear;
                imp.wrapMode = fullRect ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                imp.ReadTextureSettings(settings);
                // Luôn Full Rect: shader viền cần phần lề trong suốt quanh hình (mesh Tight cắt mất viền).
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = pivot.HasValue ? (int)SpriteAlignment.Custom : (int)SpriteAlignment.Center;
                if (pivot.HasValue) settings.spritePivot = pivot.Value;
                settings.spriteBorder = border;
                imp.SetTextureSettings(settings);
                imp.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            artCache[name] = sprite;
            return sprite;
        }

        /// <summary>Thêm con "Art" hiển thị sprite thật, cao height đơn vị world (giữ tỉ lệ ảnh).</summary>
        static SpriteRenderer AddArt(Transform parent, Sprite sprite, float height, int order, Vector2 localPos, float rotZ = 0f)
        {
            var go = new GameObject("Art");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            go.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
            return Outlined(AddSprite(go, sprite, Color.white, order));
        }

        /// <summary>
        /// Nền phòng khách (lớp xa, không va chạm): 1 ảnh có sàn ở đáy màn, phần cao hơn lát bằng mảnh tường sọc.
        /// Mép trên sàn gỗ trong ảnh nằm ở 12.4% chiều cao tính từ đáy; đặt trùng mặt sàn y = -5.
        /// </summary>
        static void RoomBackground(Transform level, float height)
        {
            var bg = ArtSprite("BG_Room_Far");
            if (bg == null) return;
            const float floorTop = 0.124f;  // tỉ lệ từ đáy ảnh tới mép trên sàn
            const float windowBand = 0.25f; // 25% trên cùng của ảnh có cửa sổ + khung tranh
            // Phủ ít nhất tường bao (rộng 6.2) và một màn chuẩn (cao 10 + tường trên 0.3) tính từ mặt sàn.
            float scale = Mathf.Max(6.2f / bg.bounds.size.x, (Mathf.Min(height, 10f) + 0.3f) / ((1f - floorTop) * bg.bounds.size.y));
            float bgH = bg.bounds.size.y * scale;
            float width = bg.bounds.size.x * scale;
            float bottom = -5f - floorTop * bgH;

            var root = new GameObject("Background");
            root.transform.SetParent(level, false);
            var far = new GameObject("Room_Far");
            far.transform.SetParent(root.transform, false);
            far.transform.localPosition = new Vector3(0f, bottom + bgH * 0.5f, 0f);
            far.transform.localScale = Vector3.one * scale;
            AddSprite(far, bg, Color.white, -20);

            // Lớp giữa: sofa, đèn, kệ sách đứng trên mặt sàn, rộng bằng tường bao; hơi nhạt để không lấn vật chơi.
            var midArt = ArtSprite("BG_Room_Mid");
            if (midArt != null)
            {
                float ms = 6.2f / midArt.bounds.size.x;
                var mid = new GameObject("Room_Mid");
                mid.transform.SetParent(root.transform, false);
                mid.transform.localPosition = new Vector3(0f, -5.02f + midArt.bounds.size.y * ms * 0.5f, 0f);
                mid.transform.localScale = Vector3.one * ms;
                AddSprite(mid, midArt, new Color(0.93f, 0.93f, 0.93f), -18);
            }

            float top = -5f + height + 0.3f;
            float bgTop = bottom + bgH;
            if (top <= bgTop) return;
            var tileSprite = ArtSprite("BG_Room_Wall", fullRect: true);
            if (tileSprite == null) return;
            // Màn cao hơn 1 màn hình: lát tường sọc lên trên, chồng xuống che dải cửa sổ (cửa sổ giữa vách trông lạ).
            float from = bgTop - windowBand * bgH;
            var tile = new GameObject("Room_Wall");
            tile.transform.SetParent(root.transform, false);
            tile.transform.localPosition = new Vector3(0f, (from + top) * 0.5f, 0f);
            tile.transform.localScale = Vector3.one * (width / tileSprite.bounds.size.x);
            var sr = AddSprite(tile, tileSprite, Color.white, -19);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(tileSprite.bounds.size.x, (top - from) / tile.transform.localScale.x);
        }

        /// <summary>
        /// Gắn art vào vật có scale không đều (nút, kim...): kích thước tính theo đơn vị world
        /// dọc trục x/y của ảnh, xoay rotZ (0 hoặc ±90 độ), lệch worldOffset so với tâm vật.
        /// </summary>
        static SpriteRenderer AddArtWorld(Transform parent, Sprite sprite, Vector2 worldSize, float rotZ, int order, Vector2 worldOffset)
        {
            var go = new GameObject("Art");
            go.transform.SetParent(parent, false);
            go.transform.position = parent.position + (Vector3)worldOffset;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            Vector3 ls = parent.lossyScale;
            bool swap = Mathf.Abs(Mathf.DeltaAngle(rotZ, 90f)) < 1f || Mathf.Abs(Mathf.DeltaAngle(rotZ, -90f)) < 1f;
            float px = swap ? ls.y : ls.x, py = swap ? ls.x : ls.y;
            go.transform.localScale = new Vector3(worldSize.x / sprite.bounds.size.x / px, worldSize.y / sprite.bounds.size.y / py, 1f);
            return Outlined(AddSprite(go, sprite, Color.white, order));
        }

        /// <summary>
        /// Tường/kệ: lát dải bìa các-tông (Wall_Tile) dọc cạnh dài, đúng 1 hàng theo bề dày.
        /// Collider giữ nguyên, sprite vuông gốc bị tắt.
        /// </summary>
        static void WallArt(GameObject wall, Vector2 size, Color tint)
        {
            var tile = ArtSprite("Wall_Tile", fullRect: true);
            if (tile == null || size.x <= 0f || size.y <= 0f) return;
            wall.GetComponent<SpriteRenderer>().enabled = false;
            bool vertical = size.y > size.x;
            float len = vertical ? size.y : size.x, thick = vertical ? size.x : size.y;
            float s = thick / tile.bounds.size.y; // scale world đều của dải
            var go = new GameObject("Art");
            go.transform.SetParent(wall.transform, false);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, vertical ? 90f : 0f);
            go.transform.localScale = vertical ? new Vector3(s / size.y, s / size.x, 1f) : new Vector3(s / size.x, s / size.y, 1f);
            var sr = AddSprite(go, tile, tint, 0);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(len / s, tile.bounds.size.y);
        }

        /// <summary>Cửa trượt: tấm cửa vòm (Door) xoay nằm ngang nếu cửa nằm ngang, 9-slice để kéo dài không méo khung.</summary>
        static void DoorArt(GameObject door, Vector2 size)
        {
            var art = ArtSprite("Door", fullRect: true, border: new Vector4(40f, 40f, 40f, 120f));
            if (art == null) return;
            door.GetComponent<SpriteRenderer>().enabled = false;
            bool horizontal = size.x > size.y;
            float thick = horizontal ? size.y : size.x, len = horizontal ? size.x : size.y;
            float s = thick / art.bounds.size.x;
            var go = new GameObject("Art");
            go.transform.SetParent(door.transform, false);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, horizontal ? -90f : 0f);
            go.transform.localScale = horizontal ? new Vector3(s / size.y, s / size.x, 1f) : new Vector3(s / size.x, s / size.y, 1f);
            var sr = Outlined(AddSprite(go, art, Color.white, 2));
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(thick / s, len / s);
        }

        /// <summary>
        /// Art cho vật trong scene chung (không nằm trong prefab màn): thân bóng, mặt bóng, kim, icon HUD, vân giấy.
        /// </summary>
        static void ApplySceneArt(BalloonController balloon, BalloonInput input, GameScreens screens, Transform hud)
        {
            // Thân bóng: pivot là tâm hình tròn (không tính núm), đường kính 421 px = 1 đơn vị = collider.
            var body = ArtSprite("Balloon_Body", 421f, pivot: new Vector2(225f / 453f, (512f - 224.5f) / 512f));
            var visual = balloon.transform.Find("Visual");
            if (body != null && visual != null)
            {
                var bodySr = visual.GetComponent<SpriteRenderer>();
                bodySr.sprite = body;
                Outlined(bodySr);
            }

            string[] faceNames = { "00_Neutral", "01_Happy", "02_Laugh", "03_Scared", "04_Dizzy", "05_Sleepy",
                                   "06_Determined", "07_Surprised", "08_Sad", "09_Wink", "10_Worried", "11_Excited" };
            var faces = new List<Object>();
            foreach (var n in faceNames)
            {
                var f = ArtSprite("Faces/Face_" + n);
                if (f != null) faces.Add(f);
            }
            var face = balloon.GetComponent<BalloonFace>();
            if (face != null) SetArray(face, "faceSprites", faces.Count == 12 ? faces.ToArray() : new Object[0]);

            // Kim: ảnh nằm ngang mũi chĩa trái, xoay -90 cho mũi chĩa +Y (BalloonInput quay +Y vào bóng).
            var needleArt = ArtSprite("Needle");
            var needle = new SerializedObject(input).FindProperty("needle").objectReferenceValue as Transform;
            if (needleArt != null && needle != null && needle.Find("Art") == null)
            {
                needle.GetComponent<SpriteRenderer>().enabled = false;
                const float len = 0.95f;
                AddArtWorld(needle, needleArt, new Vector2(len, len * needleArt.bounds.size.y / needleArt.bounds.size.x), -90f, 20, Vector2.zero);
            }

            if (screens != null)
                SetRefs(screens, ("iconPause", ArtSprite("Icons/Icon_Pause")), ("iconRestart", ArtSprite("Icons/Icon_Restart")),
                    ("iconLook", ArtSprite("Icons/Icon_Look")), ("iconMap", ArtSprite("Icons/Icon_Map")),
                    ("iconSettings", ArtSprite("Icons/Icon_Settings")));

            // Vân giấy phủ nhẹ cả màn chơi, nằm dưới mọi UI (con đầu tiên của canvas HUD).
            var grain = ArtSprite("Paper_Grain_Overlay", fullRect: true);
            if (hud != null && grain != null)
            {
                var old = hud.Find("PaperGrain");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var go = new GameObject("PaperGrain", typeof(RectTransform));
                go.layer = 5;
                go.transform.SetParent(hud, false);
                go.transform.SetAsFirstSibling();
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var raw = go.AddComponent<UnityEngine.UI.RawImage>();
                raw.texture = grain.texture;
                raw.uvRect = new Rect(0f, 0f, 2f, 3.6f);
                raw.raycastTarget = false;
            }
        }
    }
}
