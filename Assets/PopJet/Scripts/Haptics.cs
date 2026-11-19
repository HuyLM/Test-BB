using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace PopJet
{
    /// <summary>
    /// Rung (haptics) cho mobile (GDD §11.3). Unity chỉ có Handheld.Vibrate (rung dài một kiểu), nên:
    /// - Android gọi thẳng Vibrator / VibrationEffect (API 26+ có chỉnh cường độ).
    /// - iOS gọi plugin Plugins/iOS/PopJetHaptics.mm (UIImpactFeedbackGenerator).
    /// - Editor và PC: không làm gì.
    /// Bật/tắt lưu trong file save (SaveSystem, settings.haptics). Thời lượng và cường độ là starting value.
    /// </summary>
    public static class Haptics
    {
        public static bool Enabled
        {
            get => SaveSystem.Data.settings.haptics;
            set
            {
                SaveSystem.Data.settings.haptics = value;
                SaveSystem.MarkDirty();
            }
        }

        /// <summary>Tick rất nhẹ: bắt đầu ngắm.</summary>
        public static void Selection() => Play(0);
        /// <summary>Nảy, bị rò, vá.</summary>
        public static void Light() => Play(1);
        /// <summary>Chọc.</summary>
        public static void Medium() => Play(2);
        /// <summary>Nổ.</summary>
        public static void Heavy() => Play(3);
        /// <summary>Về đích.</summary>
        public static void Success() => Play(4);

        static void Play(int kind)
        {
            if (!Enabled) return;
#if UNITY_IOS && !UNITY_EDITOR
            switch (kind)
            {
                case 0: PopJetHaptics_Selection(); break;
                case 1: PopJetHaptics_Impact(0); break;
                case 2: PopJetHaptics_Impact(1); break;
                case 3: PopJetHaptics_Impact(2); break;
                default: PopJetHaptics_Success(); break;
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            switch (kind)
            {
                case 0: Vibrate(8, 40); break;
                case 1: Vibrate(12, 70); break;
                case 2: Vibrate(20, 140); break;
                case 3: Vibrate(40, 255); break;
                default: Vibrate(25, 180); break;
            }
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void PopJetHaptics_Impact(int style);
        [DllImport("__Internal")] static extern void PopJetHaptics_Selection();
        [DllImport("__Internal")] static extern void PopJetHaptics_Success();
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static int sdkInt;
        static bool hasAmplitude;
        static bool initDone;

        static void Vibrate(long ms, int amplitude)
        {
            try
            {
                if (!initDone) InitAndroid();
                if (vibrator == null) return;
                if (sdkInt >= 26)
                {
                    int amp = hasAmplitude ? Mathf.Clamp(amplitude, 1, 255) : -1; // -1 = DEFAULT_AMPLITUDE
                    using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amp))
                        vibrator.Call("vibrate", effect);
                }
                else vibrator.Call("vibrate", ms);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[PopJet] Rung lỗi, tắt rung cho phiên này: " + e.Message);
                vibrator = null;
            }
        }

        static void InitAndroid()
        {
            initDone = true;
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            if (vibrator != null && !vibrator.Call<bool>("hasVibrator")) vibrator = null;
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                sdkInt = version.GetStatic<int>("SDK_INT");
            hasAmplitude = sdkInt >= 26 && vibrator != null && vibrator.Call<bool>("hasAmplitudeControl");
        }
#endif

#if UNITY_ANDROID
        // Không bao giờ được gọi. Unity chỉ tự thêm quyền VIBRATE vào AndroidManifest khi thấy
        // Handheld.Vibrate trong code, nên giữ tham chiếu này lại.
        static void RequestVibratePermission() { Handheld.Vibrate(); }
#endif
    }
}
