using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace PopJet.EditorTools
{
    /// <summary>
    /// GDD phần 2.1: dựng các màn thành prefab (Levels/Prefabs), tạo LevelDefinition + LevelCatalog (Levels/),
    /// và gắn LevelLoader vào scene gameplay. Build Sample Scene tự gọi.
    /// Chạy lại sẽ ghi đè prefab màn và cập nhật LevelDefinition (giữ nguyên GUID nên tham chiếu không vỡ).
    /// Hai màn hiện có chỉ là màn thử: 20 màn thật của Thế giới 1 làm ở phần 2.7.
    /// </summary>
    public static partial class PopJetSceneBuilder
    {
        const string LevelDir = Root + "/Levels";
        const string LevelPrefabDir = LevelDir + "/Prefabs";
        static readonly Color WallColor = new Color(0.45f, 0.48f, 0.55f);

        static Sprite starSprite;

        [MenuItem("Tools/PopJet/Delete Save File")]
        public static void DeleteSaveFile()
        {
            SaveSystem.DeleteAll();
            Debug.Log("[PopJet] Đã xóa file save: " + SaveSystem.FilePath);
        }

        [MenuItem("Tools/PopJet/Show Save File")]
        public static void ShowSaveFile()
        {
            EditorUtility.RevealInFinder(SaveSystem.FilePath);
        }

        static void SetupLevels(BalloonController balloon, LevelManager level, CinemachineCamera vcam)
        {
            EnsureDir(LevelPrefabDir);
            starSprite = MakeSprite("Star", 128, StarShape);

            var tutorial = SaveLevelPrefab(BuildLevelTutorial());
            var sandbox = SaveLevelPrefab(BuildLevelSandbox());

            var defs = new Object[]
            {
                SaveLevelDefinition("Level_1-1", "1-1", "Up, Up!", tutorial, 1,
                    "Màn thử 1-1 (GDD §8.4): đích ngay phía trên. Lời giải: kéo kim xuống dưới, thả 1 lần."),
                SaveLevelDefinition("Level_1-2", "1-2", "Test Room", sandbox, 4,
                    "Màn thử từ scene mẫu cũ: gai, keo, quạt, nút + cửa, Sao ở góc trái trên. Par 4 là tạm, chưa có lời giải ghi lại."),
            };

            string catalogPath = $"{LevelDir}/LevelCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }
            SetArray(catalog, "levels", defs);
            EditorUtility.SetDirty(catalog);

            var loader = Object.FindAnyObjectByType<LevelLoader>();
            if (loader == null) loader = new GameObject("LevelLoader").AddComponent<LevelLoader>();
            SetRefs(loader, ("catalog", catalog), ("balloon", balloon), ("vcam", vcam));

            // Phần 2.2: theo dõi lượt chơi, 3 sao, Vương miện.
            var session = Object.FindAnyObjectByType<LevelSession>();
            if (session == null) session = new GameObject("LevelSession").AddComponent<LevelSession>();
            SetRefs(session, ("balloon", balloon), ("loader", loader));
            var hud = Object.FindAnyObjectByType<PopJetHud>();
            if (hud != null) SetRefs(hud, ("session", session));

            // Về đích thì tự sang màn kế (màn hình Kết quả làm ở phần 2.5).
            var so = new SerializedObject(level);
            so.FindProperty("autoLoadNextOnWin").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        static LevelRoot SaveLevelPrefab(GameObject root)
        {
            string path = $"{LevelPrefabDir}/{root.name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<LevelRoot>();
        }

        static LevelDefinition SaveLevelDefinition(string file, string id, string title, LevelRoot prefab, int par, string notes)
        {
            string path = $"{LevelDir}/{file}.asset";
            var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            var so = new SerializedObject(def);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("title").stringValue = title;
            so.FindProperty("world").intValue = 1;
            so.FindProperty("prefab").objectReferenceValue = prefab;
            so.FindProperty("par").intValue = par;
            so.FindProperty("notes").stringValue = notes;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static GameObject NewLevelRoot(string name, Vector2 spawn)
        {
            var root = new GameObject(name);
            var spawnPoint = new GameObject("Spawn").transform;
            spawnPoint.SetParent(root.transform, false);
            spawnPoint.localPosition = spawn;
            SetRefs(root.AddComponent<LevelRoot>(), ("spawnPoint", spawnPoint));
            return root;
        }

        /// <summary>Tường bao 5.6 x 10 đơn vị (giống scene mẫu cũ).</summary>
        static void OuterWalls(Transform level)
        {
            Wall("Wall_Left", new Vector2(-2.95f, 0f), new Vector2(0.3f, 10.6f), WallColor, level);
            Wall("Wall_Right", new Vector2(2.95f, 0f), new Vector2(0.3f, 10.6f), WallColor, level);
            Wall("Wall_Bottom", new Vector2(0f, -5.15f), new Vector2(6.2f, 0.3f), WallColor, level);
            Wall("Wall_Top", new Vector2(0f, 5.15f), new Vector2(6.2f, 0.3f), WallColor, level);
        }

        static void Goal(Transform level, Vector2 pos, float size)
        {
            var goal = new GameObject("Goal");
            goal.transform.SetParent(level);
            goal.transform.position = pos;
            goal.transform.localScale = Vector3.one * size;
            AddSprite(goal, circle, new Color(0.3f, 0.85f, 0.45f, 0.6f), 1);
            goal.AddComponent<CircleCollider2D>().isTrigger = true;
            goal.AddComponent<GoalZone>();
        }

        /// <summary>1-1: một cú chọc thẳng lên là tới đích (khoảng cách ~2.4 đơn vị, một cú chọc đi được ~3).</summary>
        static GameObject BuildLevelTutorial()
        {
            var root = NewLevelRoot("Level_1-1", new Vector2(0f, -3.2f));
            var level = root.transform;
            OuterWalls(level);
            Goal(level, new Vector2(0f, -0.3f), 1.1f);
            return root;
        }

        /// <summary>1-2: bố cục của scene mẫu cũ (gai, keo, quạt, nút + cửa, buồng đích trên phải).</summary>
        static GameObject BuildLevelSandbox()
        {
            var root = NewLevelRoot("Level_1-2", new Vector2(0f, -3.6f));
            var level = root.transform;
            OuterWalls(level);
            // Buồng đích phía trên bên phải: vách đứng + cửa làm sàn.
            Wall("Wall_GoalDivider", new Vector2(1.0f, 3.35f), new Vector2(0.25f, 3.4f), WallColor, level);

            // ---- Gai (collider thường = chặn bóng) ----
            var spike = new GameObject("SpikeStrip");
            spike.transform.SetParent(level);
            spike.transform.position = new Vector3(0.4f, 0.2f, 0f);
            var spikeCol = spike.AddComponent<BoxCollider2D>();
            spikeCol.size = new Vector2(1.6f, 0.4f);
            spike.AddComponent<Spike>();
            for (int i = 0; i < 4; i++)
            {
                var t = new GameObject("Tooth_" + i);
                t.transform.SetParent(spike.transform, false);
                t.transform.localPosition = new Vector3(-0.6f + i * 0.4f, 0f, 0f);
                t.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                AddSprite(t, triangle, new Color(0.7f, 0.72f, 0.78f), 5);
                // Răng dưới chĩa xuống
                var b = new GameObject("ToothDown_" + i);
                b.transform.SetParent(spike.transform, false);
                b.transform.localPosition = new Vector3(-0.6f + i * 0.4f, 0f, 0f);
                b.transform.localRotation = Quaternion.Euler(0, 0, 180);
                b.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                AddSprite(b, triangle, new Color(0.7f, 0.72f, 0.78f), 5);
            }

            // ---- Keo vá ----
            var patch = new GameObject("Patch");
            patch.transform.SetParent(level);
            patch.transform.position = new Vector3(2.1f, -1.2f, 0f);
            patch.transform.localScale = Vector3.one * 0.5f;
            AddSprite(patch, circle, new Color(1f, 0.85f, 0.25f), 4);
            patch.AddComponent<CircleCollider2D>().isTrigger = true;
            var patchComp = patch.AddComponent<PatchPickup>();
            SetFloat(patchComp, "airRefill", 0.3f);

            // ---- Quạt: +X local là hướng gió, xoay 90 độ để thổi lên ----
            var fan = new GameObject("Fan");
            fan.transform.SetParent(level);
            fan.transform.position = new Vector3(-1.75f, -0.4f, 0f);
            fan.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            var fanCol = fan.AddComponent<BoxCollider2D>();
            fanCol.size = new Vector2(3.4f, 1.6f);
            fanCol.isTrigger = true;
            fan.AddComponent<Fan>();
            var wind = new GameObject("WindZone");
            wind.transform.SetParent(fan.transform, false);
            wind.transform.localScale = new Vector3(3.4f, 1.6f, 1f);
            AddSprite(wind, square, new Color(0.5f, 0.85f, 1f, 0.15f), 1);
            var fanBody = new GameObject("FanBody");
            fanBody.transform.SetParent(fan.transform, false);
            fanBody.transform.localPosition = new Vector3(-1.7f, 0f, 0f);
            fanBody.transform.localScale = new Vector3(0.25f, 1.6f, 1f);
            AddSprite(fanBody, square, new Color(0.3f, 0.65f, 0.85f), 3);

            // ---- Cửa (sàn buồng đích) ----
            var door = new GameObject("Door");
            door.transform.SetParent(level);
            door.transform.position = new Vector3(1.95f, 1.65f, 0f);
            door.transform.localScale = new Vector3(1.75f, 0.25f, 1f);
            AddSprite(door, square, new Color(0.6f, 0.4f, 0.25f), 2);
            door.AddComponent<BoxCollider2D>();
            var doorComp = door.AddComponent<Door>();
            SetValue(doorComp, "openOffset", new Vector2(1.9f, 0f));

            // ---- Nút ----
            var button = new GameObject("Button");
            button.transform.SetParent(level);
            button.transform.position = new Vector3(-2.65f, 3.6f, 0f);
            button.transform.localScale = new Vector3(0.25f, 0.9f, 1f);
            AddSprite(button, square, new Color(0.9f, 0.25f, 0.45f), 2);
            button.AddComponent<BoxCollider2D>().isTrigger = true;
            var sw = button.AddComponent<ButtonSwitch>();
            SetArray(sw, "doors", new Object[] { doorComp });

            Goal(level, new Vector2(1.95f, 3.7f), 1.1f);
            // Sao lệch khỏi đường chính, ở góc trái dưới nút (phải đi vòng qua quạt).
            StarAt(level, new Vector2(-2.2f, 2.2f));
            return root;
        }

        static void StarAt(Transform level, Vector2 pos)
        {
            var star = new GameObject("Star");
            star.transform.SetParent(level);
            star.transform.position = pos;
            star.transform.localScale = Vector3.one * 0.6f;
            AddSprite(star, starSprite, new Color(1f, 0.824f, 0.247f), 6); // #FFD23F
            star.AddComponent<CircleCollider2D>().radius = 0.4f;
            star.AddComponent<StarPickup>();
        }

        /// <summary>Ngôi sao 5 cánh (bán kính ngoài 0.48, trong 0.2), khử răng cưa ~1 px.</summary>
        static float StarShape(float x, float y, int s)
        {
            var p = new Vector2(x / s - 0.5f, y / s - 0.5f);
            var v = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? 0.48f : 0.2f;
                v[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            bool inside = false;
            float d = float.MaxValue;
            for (int i = 0, j = 9; i < 10; j = i++)
            {
                if ((v[i].y > p.y) != (v[j].y > p.y) &&
                    p.x < (v[j].x - v[i].x) * (p.y - v[i].y) / (v[j].y - v[i].y) + v[i].x)
                    inside = !inside;
                Vector2 e = v[j] - v[i];
                float t = Mathf.Clamp01(Vector2.Dot(p - v[i], e) / e.sqrMagnitude);
                d = Mathf.Min(d, Vector2.Distance(p, v[i] + e * t));
            }
            float px = d * s;
            return inside ? px + 0.5f : 0.5f - px;
        }
    }
}
