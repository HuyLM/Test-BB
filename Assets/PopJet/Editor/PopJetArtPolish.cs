using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.U2D;

namespace PopJet.EditorTools
{
    /// <summary>
    /// Hoàn thiện art (GDD §9.6, §10.1): viền mực chung bằng shader, Sprite Atlas cho art Thế giới 1,
    /// font Baloo 2 (tiêu đề) và Nunito (chữ thường) dạng TextMeshPro.
    /// </summary>
    public static partial class PopJetSceneBuilder
    {
        const string OutlineShaderName = "PopJet/SpriteOutline";
        const string FontDir = "Assets/PopJet/Fonts";
        const string AtlasPath = ArtDir + "/World1.spriteatlasv2";

        static Material outlineMat;

        /// <summary>Material viền #2B2B3A (Art/SpriteOutline.mat). Thiếu shader thì trả null và giữ material sprite thường.</summary>
        static Material OutlineMaterial()
        {
            if (outlineMat != null) return outlineMat;
            var shader = Shader.Find(OutlineShaderName);
            if (shader == null) return null;
            string path = ArtDir + "/SpriteOutline.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            m.SetColor("_OutlineColor", new Color(0.169f, 0.169f, 0.227f, 1f)); // #2B2B3A
            m.SetFloat("_OutlinePixels", 3f);
            EditorUtility.SetDirty(m);
            outlineMat = m;
            return m;
        }

        /// <summary>Gắn viền cho sprite art của vật tương tác (nền, tường, mặt bóng không có viền).</summary>
        static SpriteRenderer Outlined(SpriteRenderer sr)
        {
            var m = OutlineMaterial();
            if (sr != null && m != null) sr.sharedMaterial = m;
            return sr;
        }

        /// <summary>
        /// Gom art Thế giới 1 vào một Sprite Atlas V2 (ít draw call trên mobile). Bỏ ra: ảnh lát (Tiled / RawImage lặp),
        /// nền xa quá lớn và hai tấm gốc chưa cắt. Padding 16 lớn hơn độ dày viền tính theo texel.
        /// </summary>
        static void BuildWorld1Atlas()
        {
            var skip = new HashSet<string> { "Wall_Tile", "BG_Room_Wall", "BG_Room_Far", "Paper_Grain", "Paper_Grain_Overlay", "Balloon_Faces", "Icons_HUD" };
            var objects = new List<Object>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { ArtWorld1Dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (skip.Contains(Path.GetFileNameWithoutExtension(path))) continue;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) objects.Add(sprite);
            }
            if (objects.Count == 0) return;

            var atlas = new SpriteAtlasAsset();
            atlas.Add(objects.ToArray());
            SpriteAtlasAsset.Save(atlas, AtlasPath);
            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(AtlasPath) is SpriteAtlasImporter imp)
            {
                imp.includeInBuild = true;
                imp.packingSettings = new SpriteAtlasPackingSettings
                {
                    blockOffset = 1,
                    padding = 16,
                    enableRotation = false,
                    enableTightPacking = false,
                    enableAlphaDilation = true,
                };
                imp.textureSettings = new SpriteAtlasTextureSettings
                {
                    readable = false,
                    generateMipMaps = false,
                    sRGB = true,
                    filterMode = FilterMode.Bilinear,
                };
                var platform = imp.GetPlatformSettings("DefaultTexturePlatform");
                platform.maxTextureSize = 2048;
                platform.textureCompression = TextureImporterCompression.Compressed;
                imp.SetPlatformSettings(platform);
                imp.SaveAndReimport();
            }
        }

        // ------------------------------------------------------------------ Font

        /// <summary>Bảng chữ Latin cơ bản (game chỉ có tiếng Anh, §20) + vài dấu hay dùng; thiếu ký tự thì font động tự thêm.</summary>
        static string FontChars()
        {
            var sb = new System.Text.StringBuilder();
            for (char c = (char)32; c < 127; c++) sb.Append(c);
            sb.Append("‘’“”…–—•×·");
            return sb.ToString();
        }

        /// <summary>
        /// Tạo (một lần) font asset TMP động từ file TTF trong Fonts/, nạp sẵn bảng chữ Latin.
        /// Trả null nếu thiếu file TTF (builder dùng LiberationSans như cũ).
        /// </summary>
        static TMP_FontAsset PopJetFont(string ttf, string assetName)
        {
            string path = $"{FontDir}/{assetName}.asset";
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (fa != null) return fa;
            var src = AssetDatabase.LoadAssetAtPath<Font>($"{FontDir}/{ttf}");
            if (src == null) return null;

            fa = TMP_FontAsset.CreateFontAsset(src, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fa == null) { Debug.LogError("[PopJet] Không tạo được font asset từ " + ttf); return null; }
            fa.name = assetName;
            AssetDatabase.CreateAsset(fa, path);
            var tex = fa.atlasTextures[0];
            tex.name = assetName + " Atlas";
            AssetDatabase.AddObjectToAsset(tex, fa);
            var mat = fa.material;
            mat.name = assetName + " Material";
            AssetDatabase.AddObjectToAsset(mat, fa);
            fa.TryAddCharacters(FontChars(), out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.LogWarning($"[PopJet] {assetName} thiếu ký tự: {missing}");
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        /// <summary>Font chữ thường: Nunito ExtraBold.</summary>
        static TMP_FontAsset BodyFont() => PopJetFont("Nunito-ExtraBold.ttf", "Nunito SDF");

        /// <summary>Font tiêu đề: Baloo 2 ExtraBold.</summary>
        static TMP_FontAsset TitleFont() => PopJetFont("Baloo2-ExtraBold.ttf", "Baloo2 SDF");
    }
}
