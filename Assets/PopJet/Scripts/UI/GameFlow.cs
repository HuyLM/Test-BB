using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Trạng thái luồng màn hình sống qua các lần load lại scene gameplay (GDD §10.2):
    /// lần mở app đầu tiên, yêu cầu hiện Trang chủ, màn nào đã chạy camera giới thiệu.
    /// </summary>
    public static class GameFlow
    {
        /// <summary>Trang chủ chỉ xuất hiện sau khi đã qua màn thứ mấy (1-3 = chỉ số 2).</summary>
        public const int HomeUnlockedAfterIndex = 2;

        /// <summary>Tắt camera giới thiệu và bảng mục tiêu đầu màn (smoke test tắt để thời gian không bị dừng).</summary>
        public static bool IntroEnabled = true;

        static bool coldStart = true;
        static bool homeRequested;
        static int lastIntroIndex = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IntroEnabled = true;
            coldStart = true;
            homeRequested = false;
            lastIntroIndex = -1;
        }

        /// <summary>Lần load scene kế tiếp sẽ mở Trang chủ.</summary>
        public static void RequestHome() { homeRequested = true; }

        /// <summary>
        /// Gọi một lần mỗi lần load scene. true = mở Trang chủ: khi được yêu cầu, hoặc khi mở app mà
        /// người chơi đã qua 1-3 (lần đầu mở game thì vào thẳng màn 1-1, không có menu).
        /// </summary>
        public static bool ConsumeShowHome(LevelCatalog catalog)
        {
            bool cold = coldStart;
            coldStart = false;
            if (homeRequested) { homeRequested = false; return true; }
            return cold && HomeUnlocked(catalog);
        }

        public static bool HomeUnlocked(LevelCatalog catalog)
        {
            if (catalog == null || catalog.Count <= HomeUnlockedAfterIndex) return false;
            var def = catalog[HomeUnlockedAfterIndex];
            var rec = def != null ? SaveSystem.GetRecord(def.Id) : null;
            return rec != null && rec.goal;
        }

        /// <summary>true = lần đầu vào màn này kể từ lần vào màn khác (chơi lại thì bỏ qua để giữ nhịp nhanh).</summary>
        public static bool ConsumeIntro(int levelIndex)
        {
            if (!IntroEnabled || levelIndex == lastIntroIndex) return false;
            lastIntroIndex = levelIndex;
            return true;
        }

        /// <summary>Lần vào màn kế tiếp sẽ chạy lại camera giới thiệu (ví dụ: bấm Chơi từ Trang chủ).</summary>
        public static void ForgetIntro() { lastIntroIndex = -1; }

        /// <summary>Màn nên chơi khi bấm Chơi: màn đầu tiên đã mở mà chưa về đích, hết thì màn cuối.</summary>
        public static int NextLevelToPlay(LevelCatalog catalog)
        {
            if (catalog == null || catalog.Count == 0) return 0;
            for (int i = 0; i < catalog.Count; i++)
            {
                var rec = catalog[i] != null ? SaveSystem.GetRecord(catalog[i].Id) : null;
                if (rec == null || !rec.goal) return catalog.IsUnlocked(i) ? i : Mathf.Max(0, i - 1);
            }
            return catalog.Count - 1;
        }
    }

    /// <summary>Áp cài đặt âm thanh lên scene (GDD §10.4): nhạc là AudioSource có TimeFreezeAudio, còn lại là SFX.</summary>
    public static class GameSettings
    {
        public static void Apply()
        {
            var s = SaveSystem.Data.settings;
            foreach (var src in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bool music = src.GetComponent<TimeFreezeAudio>() != null;
                src.mute = music ? !s.MusicOn : !s.SoundOn;
            }
        }

        public static void SetSound(bool on) { SaveSystem.Data.settings.sfxVolume = on ? 1f : 0f; Changed(); }
        public static void SetMusic(bool on) { SaveSystem.Data.settings.musicVolume = on ? 1f : 0f; Changed(); }
        public static void SetReduceMotion(bool on) { SaveSystem.Data.settings.reduceMotion = on; Changed(); }

        static void Changed()
        {
            SaveSystem.MarkDirty();
            Apply();
        }
    }
}
