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
        static Sprite ArtSprite(string name, float ppu = 0f, bool fullRect = false)
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
                         || settings.spriteMeshType != (fullRect ? SpriteMeshType.FullRect : SpriteMeshType.Tight)
                         || imp.wrapMode != (fullRect ? TextureWrapMode.Repeat : TextureWrapMode.Clamp);
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
                settings.spriteMeshType = fullRect ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
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
            return AddSprite(go, sprite, Color.white, order);
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
    }
}
