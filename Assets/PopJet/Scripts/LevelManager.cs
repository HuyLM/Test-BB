using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace PopJet
{
    /// <summary>
    /// Điều phối màn chơi: hết khí = thất bại mềm (tự chơi lại sau một chút, hoặc chạm để chơi lại ngay),
    /// chạm đích = thắng. Phát hiện "kẹt": còn chưa tới 1 cú chọc mà bóng gần như đứng yên một lúc.
    /// UI (nút Retry, panel thắng...) nối qua UnityEvent, sự kiện StuckChanged, hoặc gọi Restart/LoadNext.
    /// Mọi giá trị số là starting value.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] BalloonController balloon;
        [Tooltip("Thua rồi thì tự chơi lại sau từng này giây (GDD §3.5).")]
        [SerializeField] float failRestartDelay = 1.2f;
        [Tooltip("Sau khi thua, chạm bất kỳ đâu để chơi lại ngay. Chờ ít nhất từng này giây để tránh chạm nhầm.")]
        [SerializeField] float tapToRetryAfter = 0.25f;
        [Tooltip("Chỉ dùng khi scene KHÔNG có LevelLoader: tên scene màn kế tiếp (phải có trong Build Settings).")]
        [SerializeField] string nextSceneName;
        [SerializeField] bool autoLoadNextOnWin = false;
        [SerializeField] float winDelay = 1.2f;

        [Header("Phát hiện kẹt (starting values, GDD §3.5)")]
        [Tooltip("Khí còn chưa tới 1 cú chọc và tốc độ dưới mức này (đơn vị/giây)...")]
        [SerializeField] float stuckSpeed = 0.05f;
        [Tooltip("...trong từng này giây (thời gian thế giới, không đếm lúc đang ngắm) thì coi là kẹt.")]
        [SerializeField] float stuckSeconds = 3f;

        [SerializeField] UnityEvent onFail;
        [SerializeField] UnityEvent onWin;

        /// <summary>true khi bóng bị kẹt (HUD làm nút Retry nhấp nháy).</summary>
        public event Action<bool> StuckChanged;

        bool timerRunning;
        bool failed;
        bool restarting;
        float failedAt;
        float stuckTimer;

        /// <summary>
        /// Thời gian chơi (giây), tính từ lần chọc đầu tiên đến khi thắng/thua.
        /// Dùng unscaled time nên vẫn đếm cả lúc đang giữ ngắm kim (thế giới bị dừng bằng timeScale = 0).
        /// </summary>
        public float PlayTime { get; private set; }
        public bool IsStuck { get; private set; }
        public bool IsFailed => failed;

        void OnEnable()
        {
            if (balloon == null) return;
            balloon.GameStarted += HandleStarted;
            balloon.BecameEmpty += HandleEmpty;
            balloon.GoalReached += HandleGoal;
        }

        void OnDisable()
        {
            if (balloon == null) return;
            balloon.GameStarted -= HandleStarted;
            balloon.BecameEmpty -= HandleEmpty;
            balloon.GoalReached -= HandleGoal;
        }

        void Update()
        {
            if (timerRunning) PlayTime += Time.unscaledDeltaTime;

            if (failed)
            {
                PointerHelper.Poll();
                if (PointerHelper.Down && Time.unscaledTime - failedAt >= tapToRetryAfter) Restart();
                return;
            }
            UpdateStuck();
        }

        void UpdateStuck()
        {
            // Floating = đã chọc lần đầu, không đang ngắm, không có cú chọc nào đang xì.
            bool candidate = balloon != null && balloon.State == BalloonState.Floating &&
                             balloon.PokesLeft < 1f && balloon.Velocity.magnitude < stuckSpeed;
            stuckTimer = candidate ? stuckTimer + Time.deltaTime : 0f;
            SetStuck(stuckTimer >= stuckSeconds);
        }

        void SetStuck(bool value)
        {
            if (value == IsStuck) return;
            IsStuck = value;
            StuckChanged?.Invoke(value);
        }

        void HandleStarted() { timerRunning = true; }

        void HandleEmpty()
        {
            timerRunning = false;
            failed = true;
            failedAt = Time.unscaledTime;
            SetStuck(false);
            onFail?.Invoke();
            Invoke(nameof(Restart), failRestartDelay);
        }

        void HandleGoal()
        {
            timerRunning = false;
            SetStuck(false);
            onWin?.Invoke();
            var loader = LevelLoader.Current;
            bool hasNext = loader != null ? loader.HasNext : !string.IsNullOrEmpty(nextSceneName);
            if (autoLoadNextOnWin && hasNext)
                Invoke(nameof(LoadNext), winDelay);
        }

        public void Restart()
        {
            if (restarting) return; // nút Retry và "chạm để chơi lại" có thể cùng gọi trong một frame
            restarting = true;
            CancelInvoke();
            TimeController.ResetAll();
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex);
                return;
            }
#if UNITY_EDITOR
            // Scene chưa có trong Build Settings: vẫn cho chơi lại khi test trong Editor.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            restarting = false;
            Debug.LogError("[PopJet] Scene chưa có trong Build Settings nên không Restart được.");
#endif
        }

        /// <summary>Sang màn kế: theo LevelCatalog nếu scene có LevelLoader, nếu không thì theo Next Scene Name.</summary>
        public void LoadNext()
        {
            if (restarting) return;
            var loader = LevelLoader.Current;
            if (loader != null)
            {
                if (!loader.HasNext) return;
                restarting = true;
                loader.LoadNext();
                return;
            }
            if (string.IsNullOrEmpty(nextSceneName)) return;
            restarting = true;
            TimeController.ResetAll();
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
