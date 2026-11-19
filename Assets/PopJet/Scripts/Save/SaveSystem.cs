using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PopJet
{
    /// <summary>Thành tích một màn trong file save (JsonUtility không lưu được Dictionary nên dùng danh sách).</summary>
    [Serializable]
    public class LevelRecordEntry
    {
        public string id;
        public LevelRecord record = new LevelRecord();
    }

    [Serializable]
    public class SettingsData
    {
        public bool haptics = true;
        [Range(0f, 1f)] public float musicVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        public bool reduceMotion;

        public bool SoundOn => sfxVolume > 0f;
        public bool MusicOn => musicVolume > 0f;
    }

    /// <summary>Toàn bộ dữ liệu lưu của người chơi (GDD §15.7, phần 2.3).</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;
        public List<LevelRecordEntry> levels = new List<LevelRecordEntry>();
        /// <summary>Chỉ số màn chơi gần nhất trong LevelCatalog, để "Chơi tiếp".</summary>
        public int lastLevelIndex;
        public int coins;
        public SettingsData settings = new SettingsData();
    }

    /// <summary>
    /// Lưu game dạng JSON trong Application.persistentDataPath/popjet_save.json.
    /// - Ghi an toàn: ghi ra file tạm rồi thay file chính; bản trước giữ ở .bak.
    /// - File hỏng: thử đọc .bak, hỏng nốt thì bắt đầu lại từ dữ liệu trống (không ném lỗi).
    /// - Tự lưu khi qua màn (LevelSession), khi đổi màn (LevelLoader), khi app xuống nền hoặc thoát.
    /// Mọi thứ cần lưu đi qua class này; đừng ghi PlayerPrefs riêng lẻ.
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "popjet_save.json";

        static string path;
        static SaveData data;
        static bool dirty;

        /// <summary>Đường dẫn file đang dùng.</summary>
        public static string FilePath => path ??= Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            path = null;
            data = null;
            dirty = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateHook()
        {
            var go = new GameObject("[SaveSystem]");
            go.hideFlags = HideFlags.HideInHierarchy;
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<SaveSystemHook>();
        }

        /// <summary>Dùng file khác (test, debug). Bỏ dữ liệu đang giữ trong bộ nhớ và đọc lại từ file mới.</summary>
        public static void UseFile(string filePath)
        {
            path = filePath;
            data = null;
            dirty = false;
        }

        public static LevelRecord GetRecord(string levelId)
        {
            foreach (var e in Data.levels) if (e.id == levelId) return e.record;
            return null;
        }

        /// <summary>Gộp kết quả một lượt vào thành tích của màn (từng sao riêng) rồi lưu ngay.</summary>
        public static LevelRecord MergeResult(LevelResult result) => MergeResultWithCoins(result, null);

        /// <summary>
        /// Như MergeResult, kèm danh sách mã xu nhặt được trong lượt (chỉ lần nhặt đầu mới tính, GDD §12.2)
        /// và cộng result.coinsEarned vào ví.
        /// </summary>
        public static LevelRecord MergeResultWithCoins(LevelResult result, IEnumerable<int> coinIds)
        {
            var rec = GetRecord(result.levelId);
            if (rec == null)
            {
                var entry = new LevelRecordEntry { id = result.levelId };
                Data.levels.Add(entry);
                rec = entry.record;
            }
            rec.Merge(result);
            if (coinIds != null)
                foreach (int id in coinIds)
                    if (!rec.coins.Contains(id)) rec.coins.Add(id);
            if (result.coinsEarned > 0) Data.coins += result.coinsEarned;
            dirty = true;
            Save();
            return rec;
        }

        public static void SetLastLevel(int index)
        {
            if (Data.lastLevelIndex == index) return;
            Data.lastLevelIndex = index;
            dirty = true;
            Save();
        }

        /// <summary>Đánh dấu có thay đổi (cài đặt...) để lưu ở lần SaveIfDirty kế tiếp.</summary>
        public static void MarkDirty() { dirty = true; }

        public static void SaveIfDirty()
        {
            if (dirty) Save();
        }

        public static void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(Data, true);
                string file = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                string tmp = file + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(file))
                {
                    string bak = file + ".bak";
                    if (File.Exists(bak)) File.Delete(bak);
                    File.Move(file, bak);
                }
                File.Move(tmp, file);
                dirty = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PopJet] Không lưu được game: " + e.Message);
            }
        }

        /// <summary>Xóa file save và bắt đầu lại từ đầu (menu Tools > PopJet > Delete Save File).</summary>
        public static void DeleteAll()
        {
            foreach (var f in new[] { FilePath, FilePath + ".bak", FilePath + ".tmp" })
                if (File.Exists(f)) File.Delete(f);
            data = new SaveData();
            dirty = false;
        }

        static void Load()
        {
            data = TryRead(FilePath) ?? TryRead(FilePath + ".bak") ?? new SaveData();
            if (data.levels == null) data.levels = new List<LevelRecordEntry>();
            if (data.settings == null) data.settings = new SettingsData();
            foreach (var e in data.levels)
            {
                if (e.record == null) e.record = new LevelRecord();
                if (e.record.coins == null) e.record.coins = new List<int>();
            }
            data.version = SaveData.CurrentVersion; // chỗ nâng cấp dữ liệu khi đổi version
        }

        static SaveData TryRead(string file)
        {
            try
            {
                if (!File.Exists(file)) return null;
                var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(file));
                if (d == null) throw new Exception("file rỗng");
                return d;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PopJet] File save hỏng, bỏ qua ({Path.GetFileName(file)}): {e.Message}");
                return null;
            }
        }
    }

    /// <summary>Lưu khi app xuống nền hoặc thoát (mobile có thể bị tắt bất cứ lúc nào sau khi xuống nền).</summary>
    class SaveSystemHook : MonoBehaviour
    {
        void OnApplicationPause(bool paused) { if (paused) SaveSystem.SaveIfDirty(); }
        void OnApplicationQuit() { SaveSystem.SaveIfDirty(); }
    }
}
