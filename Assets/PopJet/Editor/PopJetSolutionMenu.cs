using System.Text;
using UnityEditor;
using UnityEngine;

namespace PopJet.EditorTools
{
    /// <summary>
    /// Menu Tools > PopJet > Solutions: ghi lời giải của designer (GDD §8.3 điều 5, §15.7).
    /// Cách ghi: chọn Record Best (hoặc Record Always), bấm Play, chơi tới đích. Về đích thì lời giải lưu vào
    /// LevelDefinition của màn và Par = số cú chọc. Lượt chơi lại từ cột mốc không được lưu.
    /// </summary>
    public static class PopJetSolutionMenu
    {
        const string Base = "Tools/PopJet/Solutions/";
        const string OffItem = Base + "Record Off";
        const string BestItem = Base + "Record Best (fewest pokes)";
        const string AlwaysItem = Base + "Record Always (overwrite)";

        [MenuItem(OffItem, false, 300)] static void RecordOff() { SolutionRecorder.Mode = SolutionRecordMode.Off; }
        [MenuItem(OffItem, true)] static bool RecordOffCheck() { SyncChecks(); return true; }

        [MenuItem(BestItem, false, 301)] static void RecordBest() { SolutionRecorder.Mode = SolutionRecordMode.KeepBest; }
        [MenuItem(BestItem, true)] static bool RecordBestCheck() { SyncChecks(); return true; }

        [MenuItem(AlwaysItem, false, 302)] static void RecordAlways() { SolutionRecorder.Mode = SolutionRecordMode.Always; }
        [MenuItem(AlwaysItem, true)] static bool RecordAlwaysCheck() { SyncChecks(); return true; }

        static void SyncChecks()
        {
            var mode = SolutionRecorder.Mode;
            Menu.SetChecked(OffItem, mode == SolutionRecordMode.Off);
            Menu.SetChecked(BestItem, mode == SolutionRecordMode.KeepBest);
            Menu.SetChecked(AlwaysItem, mode == SolutionRecordMode.Always);
        }

        /// <summary>Đang Play: chơi lại màn hiện tại bằng lời giải đã ghi (kiểm tra bằng mắt).</summary>
        [MenuItem(Base + "Replay Current Level", false, 320)]
        static void ReplayCurrent()
        {
            var loader = LevelLoader.Current;
            if (loader == null || loader.Level == null) return;
            if (!loader.Level.HasSolution)
            {
                Debug.LogWarning($"[PopJet] Màn {loader.Level.Id} chưa có lời giải để phát lại.");
                return;
            }
            SolutionRecorder.ReplayOnNextLoad = loader.Level.Solution;
            loader.LoadLevel(loader.Index);
        }

        [MenuItem(Base + "Replay Current Level", true)]
        static bool ReplayCurrentCheck() => Application.isPlaying && LevelLoader.Current != null;

        /// <summary>Liệt kê màn nào đã có lời giải, số cú chọc so với Par.</summary>
        [MenuItem(Base + "Report", false, 340)]
        static void Report()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/PopJet/Levels/LevelCatalog.asset");
            if (catalog == null) { Debug.LogError("[PopJet] Không thấy LevelCatalog."); return; }
            var sb = new StringBuilder();
            int have = 0;
            for (int i = 0; i < catalog.Count; i++)
            {
                var def = catalog[i];
                if (def == null) continue;
                if (def.HasSolution)
                {
                    have++;
                    var s = def.Solution;
                    sb.AppendLine($"{def.Id}: {s.pokes} cú chọc, {s.steps.Count} thao tác, {s.Duration:0.0} giây, Par {def.Par}, ghi {s.recordedAt}");
                }
                else sb.AppendLine($"{def.Id}: chưa có lời giải (Par {def.Par} là bản nháp)");
            }
            Debug.Log($"[PopJet] Lời giải: {have}/{catalog.Count} màn\n{sb}");
        }

        /// <summary>Xóa lời giải của LevelDefinition đang chọn trong Project (Par giữ nguyên).</summary>
        [MenuItem(Base + "Clear Selected Level Solution", false, 360)]
        static void ClearSelected()
        {
            if (!(Selection.activeObject is LevelDefinition def)) return;
            if (!EditorUtility.DisplayDialog("Xóa lời giải", $"Xóa lời giải đã ghi của màn {def.Id}?", "Xóa", "Thôi")) return;
            def.ClearSolution();
            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssetIfDirty(def);
            Debug.Log($"[PopJet] Đã xóa lời giải của màn {def.Id}.");
        }

        [MenuItem(Base + "Clear Selected Level Solution", true)]
        static bool ClearSelectedCheck() => Selection.activeObject is LevelDefinition d && d.HasSolution;
    }
}
