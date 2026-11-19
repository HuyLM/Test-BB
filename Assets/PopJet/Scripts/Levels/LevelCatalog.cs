using UnityEngine;

namespace PopJet
{
    /// <summary>Danh sách màn theo đúng thứ tự chơi. LevelLoader đọc danh sách này.</summary>
    [CreateAssetMenu(menuName = "PopJet/Level Catalog", fileName = "LevelCatalog")]
    public class LevelCatalog : ScriptableObject
    {
        [SerializeField] LevelDefinition[] levels = new LevelDefinition[0];

        public int Count => levels.Length;
        public LevelDefinition this[int index] => levels[index];

        public int IndexOf(LevelDefinition level) => System.Array.IndexOf(levels, level);

        /// <summary>Màn đầu luôn mở; các màn sau mở khi màn trước đã về đích ít nhất một lần (GDD §12.1).</summary>
        public bool IsUnlocked(int index)
        {
            if (index <= 0) return true;
            if (index >= levels.Length || levels[index - 1] == null) return false;
            var rec = SaveSystem.GetRecord(levels[index - 1].Id);
            return rec != null && rec.goal;
        }
    }
}
