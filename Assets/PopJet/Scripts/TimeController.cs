using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PopJet
{
    /// <summary>Các lý do làm dừng thế giới (GDD §3.7). Thế giới chạy khi không còn lý do nào.</summary>
    [Flags]
    public enum TimeReason
    {
        None = 0,
        /// <summary>Đang giữ ngắm kim.</summary>
        Aim = 1 << 0,
        /// <summary>Menu Tạm dừng.</summary>
        Pause = 1 << 1,
        /// <summary>Nút Xem màn: camera thu toàn cảnh.</summary>
        Look = 1 << 2,
        /// <summary>Camera giới thiệu đầu màn.</summary>
        Intro = 1 << 3,
        /// <summary>Trang chủ, Bản đồ, Cài đặt phủ lên màn chơi.</summary>
        Menu = 1 << 4,
        /// <summary>Bóng ma Gợi ý đang diễn lời giải (GDD §3.5, §7.2).</summary>
        Hint = 1 << 5,
    }

    /// <summary>
    /// Nơi DUY NHẤT được ghi Time.timeScale (GDD §3.7, §15.3). Mỗi tính năng bật/tắt lý do của riêng nó,
    /// nên nhả ngắm trong lúc đang Tạm dừng không làm thế giới chạy lại.
    /// Load scene mới thì tự xóa mọi lý do (không bao giờ kẹt ở timeScale 0 sau khi chơi lại hay đổi màn).
    /// </summary>
    public static class TimeController
    {
        static TimeReason reasons;

        /// <summary>Đổi khi tập lý do thay đổi. Tham số: tập lý do mới.</summary>
        public static event Action<TimeReason> Changed;

        public static TimeReason Reasons => reasons;
        public static bool IsFrozen => reasons != TimeReason.None;

        /// <summary>
        /// true = một màn hình UI (Tạm dừng, Xem màn, giới thiệu, menu) đang chiếm màn chơi:
        /// không được bắt đầu ngắm. Riêng Aim không tính.
        /// </summary>
        public static bool IsBlockingInput => (reasons & ~TimeReason.Aim) != TimeReason.None;

        public static bool Has(TimeReason reason) => (reasons & reason) != 0;

        public static void Set(TimeReason reason, bool on)
        {
            var next = on ? reasons | reason : reasons & ~reason;
            if (next == reasons) return;
            reasons = next;
            Apply();
            Changed?.Invoke(reasons);
        }

        /// <summary>Xóa mọi lý do, thế giới chạy lại. Gọi khi chơi lại, đổi màn, hoặc teardown.</summary>
        public static void ResetAll()
        {
            bool changed = reasons != TimeReason.None;
            reasons = TimeReason.None;
            Apply();
            if (changed) Changed?.Invoke(reasons);
        }

        static void Apply()
        {
            Time.timeScale = reasons == TimeReason.None ? 1f : 0f;
            // Tạm dừng thì tắt hẳn tiếng thế giới. Ngắm thì vẫn nghe (TimeFreezeAudio lọc nhạc).
            AudioListener.pause = (reasons & (TimeReason.Pause | TimeReason.Menu)) != 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            reasons = TimeReason.None;
            Changed = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene s, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) ResetAll();
        }
    }
}
