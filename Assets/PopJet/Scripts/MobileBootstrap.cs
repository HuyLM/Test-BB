using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Thiết lập chung cho mobile, tự chạy một lần trước khi load scene đầu tiên (không cần gắn vào object nào).
    /// - Khóa màn dọc. Player Settings cũng nên đặt Portrait: menu Tools > PopJet > Apply Mobile Player Settings.
    /// - 60 fps: trên mobile, nếu không đặt targetFrameRate thì Unity thường chỉ chạy 30 fps.
    /// - Không cho màn hình tự tắt: thế giới dừng khi ngắm nên người chơi có thể suy nghĩ lâu mà không chạm.
    /// Mọi giá trị số là starting value.
    /// </summary>
    public static class MobileBootstrap
    {
        public const int TargetFps = 60; // starting value

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Application.targetFrameRate = TargetFps;
            if (!Application.isMobilePlatform) return;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
