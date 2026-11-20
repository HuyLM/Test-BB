using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace PopJet.EditorTools
{
    /// <summary>
    /// GDD phần 2.1 + 2.7: dựng 20 màn Thế giới 1 thành prefab (Levels/Prefabs), tạo LevelDefinition + LevelCatalog
    /// (Levels/), gắn LevelLoader, LevelSession, GameScreens và LevelCameraDirector vào scene gameplay.
    /// Build Sample Scene tự gọi. Chạy lại sẽ ghi đè prefab màn và cập nhật LevelDefinition (giữ nguyên GUID).
    /// Bố cục và Par là bản nháp đầu tiên dựng theo §8.2/§8.4, CHƯA playtest: mọi Par ghi trong notes là tạm.
    /// Vật mới của phần 2.6 (xu, gai di chuyển, pháo giấy, gối bông, nến, mèo, máy hút bụi) dựng ở cuối file.
    /// </summary>
    public static partial class PopJetSceneBuilder
    {
        const string LevelDir = Root + "/Levels";
        const string LevelPrefabDir = LevelDir + "/Prefabs";
        static readonly Color WallColor = new Color(0.55f, 0.42f, 0.31f);  // gỗ đậm, khung màn (§9.3 Thế giới 1)
        static readonly Color ShelfColor = new Color(0.77f, 0.60f, 0.42f); // gỗ #C49A6C
        static readonly Color PinColor = new Color(0.7f, 0.72f, 0.78f);
        static readonly Color HazardColor = new Color(1f, 0.66f, 0.1f); // nhóm D: vàng cam, không dùng đỏ (§5.1)

        static Sprite starSprite;
        static PhysicsMaterial2D pillowMat;
        static int coinCounter;

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

        struct LevelSpec
        {
            public string id, title, hint, notes;
            public int par;
            public System.Func<GameObject> build;
        }

        static void SetupLevels(BalloonController balloon, LevelManager level, CinemachineCamera vcam)
        {
            EnsureDir(LevelPrefabDir);
            starSprite = MakeSprite("Star", 128, StarShape);
            pillowMat = MakePillowMaterial();
            artCache.Clear();

            var specs = WorldOneLevels();
            var defs = new List<Object>();
            foreach (var spec in specs)
            {
                var prefab = SaveLevelPrefab(spec.build());
                defs.Add(SaveLevelDefinition("Level_" + spec.id, spec.id, spec.title, prefab, spec.par, spec.hint, spec.notes));
            }

            string catalogPath = $"{LevelDir}/LevelCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }
            SetArray(catalog, "levels", defs.ToArray());
            EditorUtility.SetDirty(catalog);

            var loader = Object.FindAnyObjectByType<LevelLoader>();
            if (loader == null) loader = new GameObject("LevelLoader").AddComponent<LevelLoader>();
            SetRefs(loader, ("catalog", catalog), ("balloon", balloon), ("vcam", vcam));

            // Phần 2.2: theo dõi lượt chơi, 3 sao, Vương miện, xu.
            var session = Object.FindAnyObjectByType<LevelSession>();
            if (session == null) session = new GameObject("LevelSession").AddComponent<LevelSession>();
            SetRefs(session, ("balloon", balloon), ("loader", loader));
            var hud = Object.FindAnyObjectByType<PopJetHud>();
            if (hud != null) SetRefs(hud, ("session", session));

            // Phần 2.4: camera toàn cảnh (Xem màn + giới thiệu đầu màn).
            var director = Object.FindAnyObjectByType<LevelCameraDirector>();
            if (director == null) director = new GameObject("LevelCameraDirector").AddComponent<LevelCameraDirector>();
            SetRefs(director, ("followCam", vcam), ("loader", loader));

            // Phần 2.4 + 2.5: HUD đầy đủ và các màn hình, dựng lúc chạy trên canvas HUD.
            if (hud != null)
            {
                var screens = GetOrAdd<GameScreens>(hud.gameObject);
                SetRefs(screens, ("balloon", balloon), ("level", level), ("session", session), ("loader", loader),
                    ("director", director), ("safeArea", hud.transform.Find("SafeArea")),
                    ("font", EnsureTmpFont()), ("roundRect", roundRect), ("starSprite", starSprite));
                var tmpFont = EnsureTmpFont();
                if (tmpFont != null) SetRefs(screens, ("outlineMaterial", TmpOutlineMaterial(tmpFont)));
            }

            // Về đích thì hiện màn Kết quả (GameScreens), không tự sang màn kế.
            var so = new SerializedObject(level);
            so.FindProperty("autoLoadNextOnWin").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        // =====================================================================================
        // 20 màn Thế giới 1 "The House" (GDD §8.2, §8.4). Hộp trong rộng 5.6 (x -2.8..2.8), cao 10 (y -5..5).
        // Một cú chọc đi được khoảng 3 đơn vị. Par là lời giải dự kiến của designer, chưa playtest.
        // =====================================================================================

        static List<LevelSpec> WorldOneLevels()
        {
            return new List<LevelSpec>
            {
                L("1-1", "Up, Up!", 1, "Hold the balloon, drag, release", BuildLevelTutorial,
                    "Giới thiệu (§8.4): đích ngay phía trên. Lời giải: kéo kim xuống dưới, thả 1 lần."),
                L("1-2", "Other Way", 2, "You fly away from the needle", Level_1_2,
                    "Giới thiệu: đích chéo lên bên phải. Lời giải: 2 cú chọc chéo (kim chĩa xuống trái). Par tạm."),
                L("1-3", "Air Counts", 3, "Every poke costs air", Level_1_3,
                    "Giới thiệu khí có hạn: kệ chắn bên trái, phải vòng qua đầu kệ. Lời giải: lên phải, lên, sang trái. Par tạm."),
                L("1-4", "Bank Shot", 3, null, Level_1_4,
                    "Nảy tường: vách giữa che đích phía dưới bên phải. Lời giải: chọc chéo lên phải qua đầu vách, nảy tường phải rồi rơi xuống đích. Par tạm."),
                L("1-5", "Ouch!", 3, null, Level_1_5,
                    "Gai và keo: thanh gai chắn giữa, keo ngay phía sau. Đi lách hai bên khe 1.2 hoặc lao thẳng qua gai rồi vá. Par tạm."),
                L("1-6", "Shiny Star", 3, "Grab the star for a bonus", Level_1_6,
                    "Sao lệch đường: Sao ở dưới bên trái, đích phía trên kệ. Par tạm."),
                L("1-7", "Breeze", 2, null, Level_1_7,
                    "Quạt: cột gió bên trái đưa bóng lên miễn phí. Lời giải: chọc sang trái vào gió, tới đỉnh chọc vào đích. Sao ở trên kệ phải. Par tạm."),
                L("1-8", "Lights Out", 4, null, BuildLevelSandbox,
                    "Công tắc và cửa (bố cục scene mẫu cũ): bấm nút trên tường trái để mở sàn buồng đích. Par 4 tạm."),
                L("1-9", "On the Move", 3, null, Level_1_9,
                    "Gai di chuyển (§5.2 #2) + Par lần đầu: hai thanh gai chạy ngược pha 1 đơn vị/giây. Canh lúc gai lệch rồi chọc thẳng. Par tạm."),
                L("1-10", "Coin Shower", 2, null, Level_1_10,
                    "Màn nghỉ: rộng, nhiều xu, đích to. Par tạm."),
                L("1-11", "Paper Cannon", 1, "Tap to fire the cannon", Level_1_11,
                    "Pháo giấy (§6 #5): chọc 1 lần xuống pháo, canh nòng xoay qua khe giữa hai thanh gai rồi chạm để bắn. Par 1."),
                L("1-12", "Soft Landing", 3, null, Level_1_12,
                    "Gối bông (§6 #4): gối giữa màn hút cú va, làm điểm dừng để ngắm tiếp. Sàn và trần phải có gai. Par tạm."),
                L("1-13", "Windy Pins", 3, null, Level_1_13,
                    "Phối hai cơ chế: quạt + gai di chuyển cắt ngang cột gió. Par tạm."),
                L("1-14", "Breather", 2, null, Level_1_14,
                    "Màn nghỉ (§8.2 màn 14): dễ, đẹp, nhiều xu. Par tạm."),
                L("1-15", "Birthday Cake", 3, "Fire pops you instantly!", Level_1_15,
                    "Nhóm D lần đầu (§5.1): ba cây nến trên bánh. Lửa nổ ngay. Vòng qua hai bên. Par tạm."),
                L("1-16", "Fire Line", 2, null, Level_1_16,
                    "Pháo giấy + nến: bắn qua khe giữa các ngọn nến. Par tạm."),
                L("1-17", "Prickly Shortcut", 3, null, Level_1_17,
                    "Cú xoay (§8.2): hành lang hẹp có gai bên trái. Cố ý cọ gai để rò đẩy lên rẻ, keo ở đầu hành lang. Par tính cả rò chiến thuật (§20). Par tạm."),
                L("1-18", "Cat Nap", 3, "Don't wake the cat", Level_1_18,
                    "Mèo ngủ (§5.2 #20): mèo trên kệ trái, đích phía trên mèo. Phải lên từ bên phải rồi chọc ngang vào đích, tránh bán kính 1.8. Par tạm."),
                L("1-19", "Kitchen Chaos", 4, null, Level_1_19,
                    "Phối trộn: mèo + nến + quạt + trạm bơm tường trái (thay keo vá: màn không có gai nên keo vô dụng). Par tạm."),
                L("1-20", "The Vacuum", 7, "Climb! The vacuum is coming", Level_1_20,
                    "Màn trùm (§8.1): máy hút bụi đuổi từ dưới lên 0.45 đơn vị/giây (chỉ chạy sau cú chọc đầu). Cao 2.2 màn hình, " +
                    "chưa có checkpoint (§6 #9 chưa làm). 3 keo vá (chỉ vá, không hồi khí), quạt và pháo giấy giúp leo miễn phí. Par tạm."),
            };
        }

        static LevelSpec L(string id, string title, int par, string hint, System.Func<GameObject> build, string notes) =>
            new LevelSpec { id = id, title = title, par = par, hint = hint, build = build, notes = notes };

        static GameObject Level_1_2()
        {
            var root = NewLevelRoot("Level_1-2", new Vector2(-1.7f, -3.4f));
            var l = root.transform;
            OuterWalls(l);
            Goal(l, new Vector2(1.5f, 0.6f), 1.3f);
            return root;
        }

        static GameObject Level_1_3()
        {
            var root = NewLevelRoot("Level_1-3", new Vector2(0f, -3.8f));
            var l = root.transform;
            OuterWalls(l);
            Shelf(l, new Vector2(-1.2f, 0.2f), new Vector2(3.2f, 0.3f));
            Goal(l, new Vector2(-1.6f, 3.6f), 1.2f);
            CoinLine(l, new Vector2(0.4f, -2.2f), new Vector2(1.6f, 0.4f), 3);
            CoinLine(l, new Vector2(1.2f, 2.0f), new Vector2(-0.6f, 3.4f), 3);
            return root;
        }

        static GameObject Level_1_4()
        {
            var root = NewLevelRoot("Level_1-4", new Vector2(-1.7f, -3.8f));
            var l = root.transform;
            OuterWalls(l);
            Shelf(l, new Vector2(0f, -2.0f), new Vector2(0.3f, 6.0f)); // vách giữa từ sàn lên y = 1
            Goal(l, new Vector2(1.6f, -3.6f), 1.2f);
            CoinLine(l, new Vector2(-1.2f, -1.2f), new Vector2(-0.2f, 2.2f), 3);
            Coins(l, new Vector2(1.6f, 2.4f), new Vector2(1.9f, 0.4f));
            return root;
        }

        static GameObject Level_1_5()
        {
            var root = NewLevelRoot("Level_1-5", new Vector2(0f, -3.8f));
            var l = root.transform;
            OuterWalls(l);
            Spikes(l, "SpikeStrip", new Vector2(0f, -0.6f), 3.2f);
            Patch(l, new Vector2(0f, 1.3f));
            Goal(l, new Vector2(0f, 3.7f), 1.2f);
            Coins(l, new Vector2(-2.2f, -1.6f), new Vector2(-2.2f, -0.4f), new Vector2(2.2f, -1.6f), new Vector2(2.2f, -0.4f));
            return root;
        }

        static GameObject Level_1_6()
        {
            var root = NewLevelRoot("Level_1-6", new Vector2(0f, -3.8f));
            var l = root.transform;
            OuterWalls(l);
            Shelf(l, new Vector2(0f, 0.3f), new Vector2(3.6f, 0.3f));
            Goal(l, new Vector2(0f, 3.6f), 1.2f);
            StarAt(l, new Vector2(-2.2f, -1.6f));
            CoinLine(l, new Vector2(1.6f, -2.6f), new Vector2(2.3f, 1.6f), 4);
            return root;
        }

        static GameObject Level_1_7()
        {
            var root = NewLevelRoot("Level_1-7", new Vector2(1.8f, -3.8f));
            var l = root.transform;
            OuterWalls(l);
            FanAt(l, new Vector2(-1.85f, -0.9f), 90f, 7.6f, 1.6f);
            Shelf(l, new Vector2(1.0f, 1.2f), new Vector2(3.6f, 0.3f));
            Goal(l, new Vector2(-1.6f, 4.0f), 1.1f);
            StarAt(l, new Vector2(1.9f, 2.6f));
            CoinLine(l, new Vector2(-1.85f, -2.8f), new Vector2(-1.85f, 2.6f), 5);
            return root;
        }

        static GameObject Level_1_9()
        {
            var root = NewLevelRoot("Level_1-9", new Vector2(0f, -3.8f));
            var l = root.transform;
            OuterWalls(l);
            MovingSpike(l, "MovingSpike_A", new Vector2(-1.7f, -0.6f), 1.6f, new Vector2(3.4f, 0f), 1f, 0f);
            MovingSpike(l, "MovingSpike_B", new Vector2(-1.7f, 1.8f), 1.6f, new Vector2(3.4f, 0f), 1f, 1f);
            Patch(l, new Vector2(2.3f, 0.6f));
            Goal(l, new Vector2(0f, 3.9f), 1.1f);
            StarAt(l, new Vector2(-2.3f, 0.6f));
            CoinLine(l, new Vector2(0f, -2.4f), new Vector2(0f, 3.0f), 4);
            return root;
        }

        static GameObject Level_1_10()
        {
            var root = NewLevelRoot("Level_1-10", new Vector2(-1.8f, -3.8f));
            var l = root.transform;
            OuterWalls(l);
            Goal(l, new Vector2(1.5f, 2.8f), 1.6f);
            StarAt(l, new Vector2(-2.1f, 2.8f));
            CoinLine(l, new Vector2(-1.4f, -2.6f), new Vector2(1.0f, 1.6f), 7);
            CoinLine(l, new Vector2(-2.1f, -1.0f), new Vector2(-2.1f, 1.8f), 4);
            return root;
        }

        static GameObject Level_1_11()
        {
            var root = NewLevelRoot("Level_1-11", new Vector2(0f, -3.6f));
            var l = root.transform;
            OuterWalls(l);
            LauncherAt(l, new Vector2(0f, -1.6f), 0f, 40f);
            Spikes(l, "SpikeLeft", new Vector2(-1.7f, 1.4f), 2.2f);
            Spikes(l, "SpikeRight", new Vector2(1.7f, 1.4f), 2.2f);
            Goal(l, new Vector2(0f, 3.9f), 1.1f);
            StarAt(l, new Vector2(-2.2f, 3.6f));
            Coins(l, new Vector2(0f, 0.2f), new Vector2(0f, 2.6f));
            return root;
        }

        static GameObject Level_1_12()
        {
            var root = NewLevelRoot("Level_1-12", new Vector2(-1.8f, -3.8f));
            var l = root.transform;
            OuterWalls(l);
            Spikes(l, "SpikeFloorRight", new Vector2(1.2f, -4.75f), 3.0f);
            Spikes(l, "SpikeCeilingRight", new Vector2(1.2f, 4.75f), 3.0f);
            Pillow(l, new Vector2(1.6f, -0.4f), new Vector2(2.0f, 0.5f));
            Shelf(l, new Vector2(-1.3f, 1.2f), new Vector2(3.0f, 0.3f));
            Goal(l, new Vector2(-1.7f, 3.5f), 1.1f);
            StarAt(l, new Vector2(2.3f, 1.4f));
            CoinLine(l, new Vector2(-1.0f, -2.6f), new Vector2(1.2f, -1.2f), 3);
            return root;
        }

        static GameObject Level_1_13()
        {
            var root = NewLevelRoot("Level_1-13", new Vector2(1.8f, -3.8f));
            var l = root.transform;
            OuterWalls(l);
            FanAt(l, new Vector2(-1.85f, -0.9f), 90f, 7.6f, 1.6f);
            MovingSpike(l, "MovingSpike", new Vector2(-2.3f, 0.8f), 1.0f, new Vector2(1.0f, 0f), 0.8f, 0f);
            Shelf(l, new Vector2(1.0f, 1.2f), new Vector2(3.6f, 0.3f));
            Patch(l, new Vector2(0.4f, -1.2f));
            Goal(l, new Vector2(-1.6f, 4.0f), 1.1f);
            StarAt(l, new Vector2(1.9f, 3.0f));
            CoinLine(l, new Vector2(1.6f, -2.4f), new Vector2(-0.6f, -0.6f), 3);
            return root;
        }

        static GameObject Level_1_14()
        {
            var root = NewLevelRoot("Level_1-14", new Vector2(0f, -3.8f));
            var l = root.transform;
            OuterWalls(l);
            Goal(l, new Vector2(0f, 3.4f), 1.6f);
            StarAt(l, new Vector2(2.2f, 0.4f));
            CoinLine(l, new Vector2(-2.0f, -2.4f), new Vector2(-2.0f, 2.4f), 5);
            CoinLine(l, new Vector2(0f, -2.4f), new Vector2(0f, 2.0f), 5);
            CoinLine(l, new Vector2(1.6f, -2.4f), new Vector2(1.9f, -0.6f), 3);
            return root;
        }

        static GameObject Level_1_15()
        {
            var root = NewLevelRoot("Level_1-15", new Vector2(0f, -3.8f));
            var l = root.transform;
            OuterWalls(l);
            // Bánh sinh nhật: khối bánh (tường) + 3 nến.
            Wall("Cake", new Vector2(0f, -0.4f), new Vector2(3.0f, 0.8f), new Color(0.98f, 0.8f, 0.85f), l);
            for (int i = 0; i < 3; i++) Candle(l, new Vector2(-1f + i, 0f));
            Goal(l, new Vector2(0f, 3.7f), 1.2f);
            StarAt(l, new Vector2(-2.3f, 2.0f));
            Coins(l, new Vector2(2.2f, -1.6f), new Vector2(2.2f, 0.4f), new Vector2(1.4f, 2.2f));
            return root;
        }

        static GameObject Level_1_16()
        {
            var root = NewLevelRoot("Level_1-16", new Vector2(-1.8f, -3.6f));
            var l = root.transform;
            OuterWalls(l);
            LauncherAt(l, new Vector2(-1.8f, -1.6f), -25f, 30f);
            Shelf(l, new Vector2(0.6f, 1.0f), new Vector2(4.4f, 0.3f));
            Candle(l, new Vector2(-0.6f, 1.15f));
            Candle(l, new Vector2(0.9f, 1.15f));
            Candle(l, new Vector2(2.3f, 1.15f));
            Goal(l, new Vector2(1.6f, 3.8f), 1.2f);
            StarAt(l, new Vector2(2.2f, -2.6f));
            CoinLine(l, new Vector2(-1.0f, 0.2f), new Vector2(0.9f, 2.6f), 3);
            return root;
        }

        static GameObject Level_1_17()
        {
            var root = NewLevelRoot("Level_1-17", new Vector2(1.4f, -3.8f));
            var l = root.transform;
            OuterWalls(l);
            // Hành lang hẹp bên trái (rộng 1.3), tường trái của hành lang có gai.
            Shelf(l, new Vector2(-0.95f, -0.6f), new Vector2(0.3f, 8.2f));
            Spikes(l, "SpikeCorridor", new Vector2(-2.6f, -0.4f), 4.4f, 90f);
            Patch(l, new Vector2(-2.0f, 3.6f));
            Shelf(l, new Vector2(1.0f, 2.2f), new Vector2(3.6f, 0.3f));
            Goal(l, new Vector2(1.2f, 3.8f), 1.1f);
            StarAt(l, new Vector2(1.8f, 0.4f));
            CoinLine(l, new Vector2(-1.9f, -3.2f), new Vector2(-1.9f, 2.2f), 5);
            return root;
        }

        static GameObject Level_1_18()
        {
            var root = NewLevelRoot("Level_1-18", new Vector2(1.8f, -3.8f));
            var l = root.transform;
            OuterWalls(l);
            Shelf(l, new Vector2(-1.8f, -0.2f), new Vector2(2.0f, 0.3f));
            CatAt(l, new Vector2(-2.0f, 0.25f));
            Goal(l, new Vector2(-1.9f, 4.0f), 1.0f);
            StarAt(l, new Vector2(-2.2f, -3.6f));
            CoinLine(l, new Vector2(1.8f, -2.4f), new Vector2(1.8f, 2.8f), 5);
            Coins(l, new Vector2(0.4f, 4.0f));
            return root;
        }

        static GameObject Level_1_19()
        {
            var root = NewLevelRoot("Level_1-19", new Vector2(-1.8f, -3.8f));
            var l = root.transform;
            OuterWalls(l);
            FanAt(l, new Vector2(1.85f, -2.0f), 90f, 5.4f, 1.6f);
            Wall("Counter", new Vector2(-0.6f, -1.2f), new Vector2(3.0f, 0.4f), ShelfColor, l);
            Candle(l, new Vector2(-1.4f, -0.95f));
            Candle(l, new Vector2(0.2f, -0.95f));
            Shelf(l, new Vector2(-0.6f, 2.0f), new Vector2(4.4f, 0.3f));
            CatAt(l, new Vector2(-0.2f, 2.45f));
            PumpAt(l, 0.9f, true); // đủ cao để bóng đang bơm không quệt lửa nến bên dưới (đỉnh lửa y ≈ 0.05)
            Goal(l, new Vector2(-1.8f, 4.0f), 1.0f);
            StarAt(l, new Vector2(2.3f, 4.0f));
            CoinLine(l, new Vector2(1.85f, -3.6f), new Vector2(1.85f, 1.6f), 4);
            return root;
        }

        /// <summary>Màn trùm: cao 22 (y -5..17), máy hút bụi đuổi từ dưới lên.</summary>
        static GameObject Level_1_20()
        {
            const float h = 22f;
            var root = NewLevelRoot("Level_1-20", new Vector2(0f, -1.4f));
            var l = root.transform;
            OuterWalls(l, h);
            Vacuum(l, new Vector2(0f, -6.2f), new Vector2(0f, h - 1.5f), 0.45f);

            // Tầng 1 (khe bên phải), tầng 2 (khe bên trái), tầng 3 (khe bên phải).
            Shelf(l, new Vector2(-0.6f, 3.0f), new Vector2(4.4f, 0.3f));
            Shelf(l, new Vector2(0.6f, 8.0f), new Vector2(4.4f, 0.3f));
            Shelf(l, new Vector2(-0.6f, 13.0f), new Vector2(4.4f, 0.3f));
            FanAt(l, new Vector2(2.2f, 1.0f), 90f, 4.4f, 1.0f);
            Patch(l, new Vector2(-2.2f, 5.0f));
            PumpAt(l, 5.0f, false); // tầng 1, ngay trên khe phải: nạp khí trước khi leo tiếp
            LauncherAt(l, new Vector2(-2.0f, 6.0f), 0f, 0f);
            Spikes(l, "Spike_F2", new Vector2(1.2f, 7.6f), 2.4f, 180f);
            Patch(l, new Vector2(2.2f, 10.0f));
            Candle(l, new Vector2(-1.0f, 8.15f));
            CatAt(l, new Vector2(-1.8f, 13.45f));
            Patch(l, new Vector2(0.4f, 11.6f));
            FanAt(l, new Vector2(2.2f, 13.6f), 90f, 2.8f, 1.0f);
            // Đích: ô cửa sổ áp mái.
            Goal(l, new Vector2(0f, 15.9f), 1.2f);
            StarAt(l, new Vector2(-2.2f, 10.6f));
            CoinLine(l, new Vector2(2.2f, -0.4f), new Vector2(2.2f, 2.4f), 3);
            CoinLine(l, new Vector2(-2.2f, 3.8f), new Vector2(-2.2f, 7.0f), 3);
            CoinLine(l, new Vector2(2.2f, 8.8f), new Vector2(2.2f, 12.4f), 3);
            return root;
        }

        // =====================================================================================
        // Helpers dựng màn
        // =====================================================================================

        static LevelRoot SaveLevelPrefab(GameObject root)
        {
            string path = $"{LevelPrefabDir}/{root.name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<LevelRoot>();
        }

        static LevelDefinition SaveLevelDefinition(string file, string id, string title, LevelRoot prefab, int par, string hint, string notes)
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
            so.FindProperty("hint").stringValue = hint ?? "";
            so.FindProperty("notes").stringValue = notes;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static GameObject NewLevelRoot(string name, Vector2 spawn)
        {
            coinCounter = 0;
            var root = new GameObject(name);
            var spawnPoint = new GameObject("Spawn").transform;
            spawnPoint.SetParent(root.transform, false);
            spawnPoint.localPosition = spawn;
            SetRefs(root.AddComponent<LevelRoot>(), ("spawnPoint", spawnPoint));
            return root;
        }

        /// <summary>Tường bao rộng 5.6, đáy y = -5, cao height (mặc định 10 = 1 màn).</summary>
        static void OuterWalls(Transform level, float height = 10f)
        {
            float cy = -5f + height * 0.5f;
            RoomBackground(level, height);
            Wall("Wall_Left", new Vector2(-2.95f, cy), new Vector2(0.3f, height + 0.6f), WallColor, level);
            Wall("Wall_Right", new Vector2(2.95f, cy), new Vector2(0.3f, height + 0.6f), WallColor, level);
            Wall("Wall_Bottom", new Vector2(0f, -5.15f), new Vector2(6.2f, 0.3f), WallColor, level);
            Wall("Wall_Top", new Vector2(0f, -5f + height + 0.15f), new Vector2(6.2f, 0.3f), WallColor, level);
        }

        static void Shelf(Transform level, Vector2 pos, Vector2 size) { Wall("Shelf", pos, size, ShelfColor, level); }

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

        /// <summary>Thanh gai bạc (nhóm A), răng chĩa hai phía. Collider thường: chặn bóng và tạo lỗ rò.</summary>
        static GameObject Spikes(Transform level, string name, Vector2 pos, float width, float angle = 0f)
        {
            var spike = new GameObject(name);
            spike.transform.SetParent(level);
            spike.transform.position = pos;
            spike.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            spike.AddComponent<BoxCollider2D>().size = new Vector2(width, 0.4f);
            spike.AddComponent<Spike>();
            var pin = ArtSprite("Pin");
            if (pin != null)
            {
                // Art §9.6: hàng đinh ghim quay lưng vào nhau, mũi chĩa ra hai phía. Hình nhô hơn hộp collider 0.4
                // một chút (±0.29) để người chơi thấy vùng nguy hiểm sớm, không bị "chạm mà không thấy".
                int pins = Mathf.Max(1, Mathf.RoundToInt(width / 0.3f));
                float pStep = width / pins;
                for (int i = 0; i < pins; i++)
                {
                    float x = -width * 0.5f + pStep * (i + 0.5f);
                    AddArt(spike.transform, pin, 0.38f, 5, new Vector2(x, 0.1f), 180f).name = "PinUp_" + i;   // ảnh gốc mũi chĩa xuống
                    AddArt(spike.transform, pin, 0.38f, 5, new Vector2(x, -0.1f)).name = "PinDown_" + i;
                }
                return spike;
            }
            int n = Mathf.Max(1, Mathf.RoundToInt(width / 0.4f));
            float step = width / n;
            for (int i = 0; i < n; i++)
            {
                float x = -width * 0.5f + step * (i + 0.5f);
                for (int k = 0; k < 2; k++)
                {
                    var t = new GameObject(k == 0 ? "Tooth_" + i : "ToothDown_" + i);
                    t.transform.SetParent(spike.transform, false);
                    t.transform.localPosition = new Vector3(x, 0f, 0f);
                    t.transform.localRotation = Quaternion.Euler(0, 0, k * 180f);
                    t.transform.localScale = new Vector3(step, 0.4f, 1f);
                    AddSprite(t, triangle, PinColor, 5);
                }
            }
            return spike;
        }

        /// <summary>Gai di chuyển (§5.2 #2): Spike + PathMover kinematic, đường ray vẽ bằng chấm.</summary>
        static void MovingSpike(Transform level, string name, Vector2 pos, float width, Vector2 offset, float speed, float phase)
        {
            var spike = Spikes(level, name, pos, width);
            var rb = spike.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var mover = spike.AddComponent<PathMover>();
            SetValue(mover, "offset", offset);
            SetFloat(mover, "speed", speed);
            SetFloat(mover, "startPhase", phase);
            SetRefs(mover, ("dotSprite", circle));
        }

        static void Patch(Transform level, Vector2 pos)
        {
            var patch = new GameObject("Patch");
            patch.transform.SetParent(level);
            patch.transform.position = pos;
            patch.transform.localScale = Vector3.one * 0.5f;
            var art = ArtSprite("Patch");
            if (art != null) AddArt(patch.transform, art, 1.1f, 4, Vector2.zero);
            else AddSprite(patch, circle, new Color(1f, 0.85f, 0.25f), 4);
            var pc = patch.AddComponent<CircleCollider2D>();
            pc.radius = 0.5f; // giữ nguyên vùng nhặt như placeholder (đường kính 0.5 world)
            pc.isTrigger = true;
            patch.AddComponent<PatchPickup>(); // chỉ vá lỗ rò, không hồi khí
        }

        /// <summary>
        /// Trạm bơm gắn tường (§6 #8): thân sát tường trong, vòi chĩa vào phòng, vùng bơm tròn bán kính 0.7
        /// cách tường 1.25. onLeftWall = gắn tường trái (vòi chĩa sang phải). Đồng hồ trên thân vơi dần theo khí còn lại.
        /// </summary>
        static void PumpAt(Transform level, float y, bool onLeftWall)
        {
            float dir = onLeftWall ? 1f : -1f; // hướng vòi
            var go = new GameObject("Pump");
            go.transform.SetParent(level);
            go.transform.position = new Vector2(-dir * 2.8f + dir * 1.25f, y);
            var c = go.AddComponent<CircleCollider2D>();
            c.radius = 0.7f;
            c.isTrigger = true;

            var zone = new GameObject("Zone");
            zone.transform.SetParent(go.transform, false);
            zone.transform.localScale = Vector3.one * 1.4f;
            var zoneSr = AddSprite(zone, circle, new Color(0.75f, 0.9f, 1f, 0.35f), 1);

            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(-dir * 0.9f, 0f, 0f);
            var art = ArtSprite("Pump");
            if (art != null)
            {
                // Ảnh gốc: giá treo bên trái, vòi chĩa sang phải; gắn tường phải thì lật ngang.
                // Đặt thân sao cho giá treo áp sát mép tường trong (cách tâm vùng bơm 1.25).
                const float artH = 0.95f;
                float artW = art.bounds.size.x / art.bounds.size.y * artH;
                body.transform.localPosition = new Vector3(-dir * (1.25f - artW * 0.5f), 0f, 0f);
                AddArt(body.transform, art, artH, 4, Vector2.zero).flipX = !onLeftWall;
            }
            else
            {
                var box = new GameObject("Box");
                box.transform.SetParent(body.transform, false);
                box.transform.localScale = new Vector3(0.5f, 0.75f, 1f);
                AddSprite(box, roundRect != null ? roundRect : square, new Color(0.35f, 0.62f, 0.85f), 4);
                var nozzle = new GameObject("Nozzle");
                nozzle.transform.SetParent(body.transform, false);
                nozzle.transform.localPosition = new Vector3(dir * 0.35f, 0f, 0f);
                nozzle.transform.localScale = new Vector3(0.25f, 0.14f, 1f);
                AddSprite(nozzle, square, new Color(0.17f, 0.17f, 0.23f), 4);
            }

            // Đồng hồ khí của trạm: nền tối + vạch xanh (neo ở mép trái để vơi từ phải sang).
            // Có art thì canh trên phần hộp (lệch về phía tường), không canh giữa cả ảnh (có cả vòi).
            float gx = art != null ? -dir * 0.18f : 0f;
            var gaugeBg = new GameObject("GaugeBg");
            gaugeBg.transform.SetParent(body.transform, false);
            gaugeBg.transform.localPosition = new Vector3(gx, 0.62f, 0f);
            gaugeBg.transform.localScale = new Vector3(0.62f, 0.12f, 1f);
            AddSprite(gaugeBg, square, new Color(0.17f, 0.17f, 0.23f), 5);
            var gauge = new GameObject("Gauge");
            gauge.transform.SetParent(body.transform, false);
            gauge.transform.localPosition = new Vector3(gx - 0.28f, 0.62f, 0f);
            var fill = new GameObject("Fill");
            fill.transform.SetParent(gauge.transform, false);
            fill.transform.localPosition = new Vector3(0.28f, 0f, 0f);
            fill.transform.localScale = new Vector3(0.56f, 0.07f, 1f);
            AddSprite(fill, square, new Color(0.114f, 0.62f, 0.459f), 6); // #1D9E75 như vòng khí

            var pump = go.AddComponent<PumpStation>();
            SetRefs(pump, ("gauge", gauge.transform), ("zone", zoneSr));
        }

        /// <summary>Quạt: +X local là hướng gió. angle 90 = thổi lên. pos là tâm vùng gió.</summary>
        static void FanAt(Transform level, Vector2 pos, float angle, float length, float width)
        {
            var fan = new GameObject("Fan");
            fan.transform.SetParent(level);
            fan.transform.position = pos;
            fan.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var col = fan.AddComponent<BoxCollider2D>();
            col.size = new Vector2(length, width);
            col.isTrigger = true;
            fan.AddComponent<Fan>();
            var wind = new GameObject("WindZone");
            wind.transform.SetParent(fan.transform, false);
            wind.transform.localScale = new Vector3(length, width, 1f);
            AddSprite(wind, square, new Color(0.5f, 0.85f, 1f, 0.15f), 1);
            var body = new GameObject("FanBody");
            body.transform.SetParent(fan.transform, false);
            body.transform.localPosition = new Vector3(-length * 0.5f, 0f, 0f);
            var art = ArtSprite("Fan");
            if (art != null)
            {
                // Quạt nhìn chính diện, đỉnh ảnh quay theo hướng gió (+X local), chân quạt đặt đúng mép đầu vùng gió
                // (vùng gió thường bắt đầu sát sàn/tường, lùi ra sau sẽ lòi qua tường).
                float size = Mathf.Clamp(width * 0.8f, 0.6f, 1.1f);
                body.transform.localPosition = new Vector3(-length * 0.5f + size * 0.5f, 0f, 0f);
                AddArt(body.transform, art, size, 3, Vector2.zero, -90f);
                return;
            }
            body.transform.localScale = new Vector3(0.25f, width, 1f);
            AddSprite(body, square, new Color(0.3f, 0.65f, 0.85f), 3);
        }

        static void StarAt(Transform level, Vector2 pos)
        {
            var star = new GameObject("Star");
            star.transform.SetParent(level);
            star.transform.position = pos;
            star.transform.localScale = Vector3.one * 0.6f;
            var art = ArtSprite("Star");
            if (art != null) AddArt(star.transform, art, 1.05f, 6, Vector2.zero);
            else AddSprite(star, starSprite, new Color(1f, 0.824f, 0.247f), 6); // #FFD23F
            star.AddComponent<CircleCollider2D>().radius = 0.4f;
            star.AddComponent<StarPickup>();
        }

        /// <summary>Xu (§7.1): đồng vàng nhỏ, mã tăng dần trong một màn.</summary>
        static void Coins(Transform level, params Vector2[] points)
        {
            foreach (var p in points)
            {
                var coin = new GameObject("Coin_" + coinCounter);
                coin.transform.SetParent(level);
                coin.transform.position = p;
                coin.transform.localScale = Vector3.one * 0.32f;
                AddSprite(coin, circle, new Color(1f, 0.78f, 0.2f), 6);
                var shine = new GameObject("Shine");
                shine.transform.SetParent(coin.transform, false);
                shine.transform.localPosition = new Vector3(-0.15f, 0.15f, 0f);
                shine.transform.localScale = Vector3.one * 0.35f;
                AddSprite(shine, circle, new Color(1f, 0.95f, 0.7f), 7);
                coin.AddComponent<CircleCollider2D>().radius = 0.6f;
                var c = coin.AddComponent<CoinPickup>();
                var so = new SerializedObject(c);
                so.FindProperty("id").intValue = coinCounter++;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void CoinLine(Transform level, Vector2 from, Vector2 to, int count)
        {
            var pts = new Vector2[count];
            for (int i = 0; i < count; i++) pts[i] = Vector2.Lerp(from, to, count == 1 ? 0.5f : i / (float)(count - 1));
            Coins(level, pts);
        }

        /// <summary>Pháo giấy (§6 #5). angle = hướng nòng ban đầu (0 = bắn lên), sweep = góc quét mỗi bên.</summary>
        static void LauncherAt(Transform level, Vector2 pos, float angle, float sweep)
        {
            var go = new GameObject("Launcher");
            go.transform.SetParent(level);
            go.transform.position = pos;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;
            col.isTrigger = true;
            var ring = new GameObject("Ring");
            ring.transform.SetParent(go.transform, false);
            ring.transform.localScale = Vector3.one * 1.1f;
            AddSprite(ring, circle, new Color(0.95f, 0.55f, 0.65f, 0.55f), 2);
            var barrel = new GameObject("Barrel");
            barrel.transform.SetParent(go.transform, false);
            barrel.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var tube = new GameObject("Tube");
            tube.transform.SetParent(barrel.transform, false);
            tube.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            tube.transform.localScale = new Vector3(0.45f, 0.7f, 1f);
            AddSprite(tube, square, new Color(0.95f, 0.45f, 0.55f), 3);
            for (int i = 0; i < 4; i++)
            {
                var d = new GameObject("AimDot_" + i);
                d.transform.SetParent(barrel.transform, false);
                d.transform.localPosition = new Vector3(0f, 1.2f + i * 0.4f, 0f);
                d.transform.localScale = Vector3.one * (0.14f - i * 0.02f);
                AddSprite(d, circle, new Color(1f, 1f, 1f, 0.6f - i * 0.12f), 3);
            }
            var l = go.AddComponent<Launcher>();
            SetRefs(l, ("barrel", barrel.transform));
            SetFloat(l, "sweepDegrees", sweep);
        }

        /// <summary>Gối bông (§6 #4): hút cú va, bóng dừng lại. Vật liệu bounciness 0, ma sát 1.</summary>
        static void Pillow(Transform level, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Pillow");
            go.transform.SetParent(level);
            go.transform.position = pos;
            var vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            vis.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = AddSprite(vis, roundRect != null ? roundRect : square, new Color(0.85f, 0.9f, 1f), 2);
            if (roundRect != null) { sr.drawMode = SpriteDrawMode.Sliced; sr.size = Vector2.one; }
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.sharedMaterial = pillowMat;
        }

        /// <summary>Nến sinh nhật (§5.2 #19, nhóm D): thân sáp chặn bóng, ngọn lửa chạm vào là nổ (Burned).</summary>
        static void Candle(Transform level, Vector2 basePos)
        {
            var go = new GameObject("Candle");
            go.transform.SetParent(level);
            go.transform.position = basePos;
            var wax = new GameObject("Wax");
            wax.transform.SetParent(go.transform, false);
            wax.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            wax.transform.localScale = new Vector3(0.16f, 0.6f, 1f);
            AddSprite(wax, square, new Color(0.6f, 0.8f, 1f), 3);
            wax.AddComponent<BoxCollider2D>();
            var flame = new GameObject("Flame");
            flame.transform.SetParent(go.transform, false);
            flame.transform.localPosition = new Vector3(0f, 0.78f, 0f);
            var glow = new GameObject("Glow");
            glow.transform.SetParent(flame.transform, false);
            glow.transform.localScale = Vector3.one * 0.7f;
            AddSprite(glow, dot, new Color(1f, 0.7f, 0.2f, 0.45f), 2);
            var fire = new GameObject("Fire");
            fire.transform.SetParent(flame.transform, false);
            fire.transform.localScale = new Vector3(0.22f, 0.34f, 1f);
            AddSprite(fire, circle, HazardColor, 4);
            var c = flame.AddComponent<CircleCollider2D>();
            c.radius = 0.17f;
            c.isTrigger = true;
            var hz = flame.AddComponent<HardHazard>();
            SetRefs(hz, ("flicker", fire.transform));
            SetEnum(hz, "reason", (int)FailReason.Burned);

            var art = ArtSprite("Candle");
            if (art != null)
            {
                // Ảnh nến gồm đế, thân sáp và lửa; đáy ảnh trùng basePos, đỉnh lửa ~0.98 khớp collider lửa (0.61..0.95).
                wax.GetComponent<SpriteRenderer>().enabled = false;
                fire.GetComponent<SpriteRenderer>().enabled = false;
                AddArt(go.transform, art, 1f, 4, new Vector2(0f, 0.5f));
            }
        }

        /// <summary>Mèo ngủ (§5.2 #20): pos = chân mèo (đặt ngay trên kệ).</summary>
        static void CatAt(Transform level, Vector2 pos)
        {
            var go = new GameObject("Cat");
            go.transform.SetParent(level);
            go.transform.position = pos + new Vector2(0f, 0.3f);
            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            var bodySr = AddSprite(body, circle, new Color(0.55f, 0.5f, 0.6f), 4);
            body.transform.localScale = new Vector3(0.8f, 0.55f, 1f);
            var earL = Ear(body.transform, -0.25f);
            var earR = Ear(body.transform, 0.25f);
            var zzz = new GameObject("Zzz");
            zzz.transform.SetParent(go.transform, false);
            zzz.transform.localPosition = new Vector3(0.4f, 0.5f, 0f);
            for (int i = 0; i < 3; i++)
            {
                var z = new GameObject("Z" + i);
                z.transform.SetParent(zzz.transform, false);
                z.transform.localPosition = new Vector3(i * 0.15f, i * 0.18f, 0f);
                z.transform.localScale = Vector3.one * (0.08f + i * 0.03f);
                AddSprite(z, circle, new Color(1f, 1f, 1f, 0.7f), 5);
            }
            // Vùng báo nguy: vòng mờ đúng bán kính đánh thức (1.8 + bán kính bóng).
            var warn = new GameObject("WakeRadius");
            warn.transform.SetParent(go.transform, false);
            warn.transform.localScale = Vector3.one * (2f * 2.25f);
            AddSprite(warn, circle, new Color(1f, 0.66f, 0.1f, 0.06f), -1);
            var cat = go.AddComponent<Cat>();
            SetRefs(cat, ("body", body.transform), ("earL", earL), ("earR", earR), ("zzz", zzz.transform), ("bodyRenderer", bodySr));
        }

        static Transform Ear(Transform body, float x)
        {
            var e = new GameObject(x < 0 ? "EarL" : "EarR");
            e.transform.SetParent(body, false);
            e.transform.localPosition = new Vector3(x, 0.45f, 0f);
            e.transform.localScale = new Vector3(0.3f, 0.45f, 1f);
            AddSprite(e, triangle, new Color(0.5f, 0.45f, 0.55f), 4);
            return e.transform;
        }

        /// <summary>Máy hút bụi (§5.2 #12): thân chạy lên theo PathMover, vùng hút ForceField, miệng HardHazard (SuckedIn).</summary>
        static void Vacuum(Transform level, Vector2 pos, Vector2 travel, float speed)
        {
            var go = new GameObject("Vacuum");
            go.transform.SetParent(level);
            go.transform.position = pos;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var mover = go.AddComponent<PathMover>();
            SetValue(mover, "offset", travel);
            SetFloat(mover, "speed", speed);
            var so = new SerializedObject(mover);
            so.FindProperty("pingPong").boolValue = false;
            so.FindProperty("waitForFirstPoke").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(5.4f, 1.6f, 1f);
            AddSprite(body, square, new Color(0.35f, 0.3f, 0.4f), 8);
            for (int i = 0; i < 6; i++)
            {
                // Sọc cảnh báo vàng cam ở miệng (nhóm D).
                var stripe = new GameObject("Stripe_" + i);
                stripe.transform.SetParent(go.transform, false);
                stripe.transform.localPosition = new Vector3(-2.25f + i * 0.9f, 0.9f, 0f);
                stripe.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
                stripe.transform.localScale = new Vector3(0.35f, 0.3f, 1f);
                AddSprite(stripe, square, HazardColor, 9);
            }
            var mouth = new GameObject("Mouth");
            mouth.transform.SetParent(go.transform, false);
            mouth.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            var mc = mouth.AddComponent<BoxCollider2D>();
            mc.size = new Vector2(5.4f, 0.3f);
            mc.isTrigger = true;
            SetEnum(mouth.AddComponent<HardHazard>(), "reason", (int)FailReason.SuckedIn);

            var suction = new GameObject("Suction");
            suction.transform.SetParent(go.transform, false);
            suction.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            var sc = suction.AddComponent<CircleCollider2D>();
            sc.radius = 3f;
            sc.isTrigger = true;
            var field = suction.AddComponent<ForceField>();
            SetFloat(field, "radius", 3f);
            var haze = new GameObject("SuctionHaze");
            haze.transform.SetParent(suction.transform, false);
            haze.transform.localScale = Vector3.one * 6f;
            AddSprite(haze, dot, new Color(0.8f, 0.85f, 1f, 0.12f), 1);
        }

        static PhysicsMaterial2D MakePillowMaterial()
        {
            string path = ArtDir + "/Pillow.physicsMaterial2D";
            var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (m == null)
            {
                m = new PhysicsMaterial2D("Pillow");
                AssetDatabase.CreateAsset(m, path);
            }
            m.bounciness = 0f;
            m.friction = 1f;
            // Bóng có bounciness 0.85: phải lấy giá trị nhỏ nhất thì gối mới hút được cú va.
            m.bounceCombine = PhysicsMaterialCombine2D.Minimum;
            m.frictionCombine = PhysicsMaterialCombine2D.Maximum;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void SetEnum(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).enumValueIndex = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- Hai màn cũ (giữ nguyên bố cục) ----

        /// <summary>1-1: một cú chọc thẳng lên là tới đích (khoảng cách ~2.9 đơn vị, một cú chọc đi được ~3).</summary>
        static GameObject BuildLevelTutorial()
        {
            var root = NewLevelRoot("Level_1-1", new Vector2(0f, -3.2f));
            var level = root.transform;
            OuterWalls(level);
            Goal(level, new Vector2(0f, -0.3f), 1.1f);
            return root;
        }

        /// <summary>1-8: bố cục của scene mẫu cũ (gai, keo, quạt, nút + cửa, buồng đích trên phải).</summary>
        static GameObject BuildLevelSandbox()
        {
            var root = NewLevelRoot("Level_1-8", new Vector2(0f, -3.6f));
            var level = root.transform;
            OuterWalls(level);
            // Buồng đích phía trên bên phải: vách đứng + cửa làm sàn.
            Wall("Wall_GoalDivider", new Vector2(1.0f, 3.35f), new Vector2(0.25f, 3.4f), WallColor, level);
            Spikes(level, "SpikeStrip", new Vector2(0.4f, 0.2f), 1.6f);
            Patch(level, new Vector2(2.1f, -1.2f));
            FanAt(level, new Vector2(-1.75f, -0.4f), 90f, 3.4f, 1.6f);

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
            CoinLine(level, new Vector2(-1.75f, -1.6f), new Vector2(-1.75f, 1.0f), 3);
            return root;
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
