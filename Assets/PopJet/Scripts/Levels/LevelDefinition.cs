using System;
using UnityEngine;

namespace PopJet
{
    /// <summary>Gợi ý bằng hình của các màn tutorial (GDD §8.4). Builder đặt theo từng màn, TutorialCoach đọc.</summary>
    [Flags]
    public enum TutorialCue
    {
        None = 0,
        /// <summary>1-1: bàn tay mờ nhấp nháy trên bóng; khi đang giữ mà chưa kéo thì bàn tay ma kéo xuống.</summary>
        DragHand = 1 << 0,
        /// <summary>1-6: ô Sao trên HUD nhấp nháy tới khi nhặt được Sao.</summary>
        StarChip = 1 << 1,
        /// <summary>1-9: phần Par trên HUD sáng lên, dòng gợi ý giải thích Par.</summary>
        ParChip = 1 << 2,
    }

    /// <summary>
    /// Dữ liệu một màn (GDD §15.7): prefab chứa toàn bộ nội dung màn, Par, thế giới, gợi ý tutorial,
    /// lời giải designer đã ghi (cho Par, bóng ma Gợi ý và test phát lại).
    /// Tạo bằng menu Assets > Create > PopJet > Level Definition, rồi thêm vào LevelCatalog.
    /// </summary>
    [CreateAssetMenu(menuName = "PopJet/Level Definition", fileName = "Level_")]
    public class LevelDefinition : ScriptableObject
    {
        [Tooltip("Mã màn hiện trên HUD và bản đồ, ví dụ \"1-1\".")]
        [SerializeField] string id = "1-1";
        [Tooltip("Tên màn (tiếng Anh, hiện trong game).")]
        [SerializeField] string title;
        [SerializeField, Min(1)] int world = 1;
        [Tooltip("Prefab gốc của màn: có LevelRoot và điểm xuất phát của bóng.")]
        [SerializeField] LevelRoot prefab;
        [Tooltip("Số cú chọc chuẩn để được sao Par = lời giải tốt nhất của designer (GDD §3.4).\n" +
                 "Ghi lời giải thì Par tự đặt bằng số cú chọc của lời giải; builder không ghi đè Par của màn đã có lời giải.")]
        [SerializeField, Min(1)] int par = 3;
        [Tooltip("Một dòng gợi ý hiện ở cuối màn hình đầu màn (tutorial GDD §8.4, tiếng Anh). Để trống = không có.")]
        [SerializeField] string hint;
        [Tooltip("Gợi ý bằng hình của màn tutorial (GDD §8.4).")]
        [SerializeField] TutorialCue tutorial;
        [Tooltip("Lời giải designer ghi và ghi chú dựng màn. Không hiện trong game.")]
        [SerializeField, TextArea(2, 6)] string notes;
        [Tooltip("Lời giải designer đã ghi (Tools > PopJet > Solutions). Dùng cho Par, bóng ma Gợi ý và test phát lại.")]
        [SerializeField] LevelSolution solution = new LevelSolution();

        public string Id => id;
        public string Title => title;
        public int World => world;
        public LevelRoot Prefab => prefab;
        public int Par => par;
        public string Hint => hint;
        public TutorialCue Tutorial => tutorial;
        public bool HasCue(TutorialCue cue) => (tutorial & cue) != 0;
        public LevelSolution Solution => solution;
        public bool HasSolution => solution != null && solution.IsValid;

#if UNITY_EDITOR
        /// <summary>Lưu lời giải mới ghi (Editor). Par = số cú chọc của lời giải.</summary>
        public void SetSolution(LevelSolution value)
        {
            solution = value;
            par = Mathf.Max(1, value.pokes);
        }

        /// <summary>Xóa lời giải đã ghi (Par giữ nguyên).</summary>
        public void ClearSolution() { solution = new LevelSolution(); }
#endif
    }
}
