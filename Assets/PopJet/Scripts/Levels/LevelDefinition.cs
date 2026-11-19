using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Dữ liệu một màn (GDD §15.7): prefab chứa toàn bộ nội dung màn, Par, thế giới, ghi chú lời giải.
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
        [Tooltip("Số cú chọc chuẩn để được sao Par = lời giải tốt nhất của designer (GDD §3.4).")]
        [SerializeField, Min(1)] int par = 3;
        [Tooltip("Lời giải của designer và ghi chú dựng màn. Không hiện trong game.")]
        [SerializeField, TextArea(2, 6)] string notes;

        public string Id => id;
        public string Title => title;
        public int World => world;
        public LevelRoot Prefab => prefab;
        public int Par => par;
    }
}
