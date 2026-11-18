using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace PopJet
{
    /// <summary>
    /// Điều phối màn chơi: hết khí = thất bại mềm (tự chơi lại sau một chút),
    /// chạm đích = thắng. UI (panel thắng, nút Next/Retry) nối qua UnityEvent hoặc gọi Restart/LoadNext.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] BalloonController balloon;
        [SerializeField] float failRestartDelay = 1.5f;
        [Tooltip("Tên scene màn kế tiếp (phải có trong Build Settings). Để trống nếu dùng UI để chuyển.")]
        [SerializeField] string nextSceneName;
        [SerializeField] bool autoLoadNextOnWin = false;
        [SerializeField] float winDelay = 1.2f;
        [SerializeField] UnityEvent onFail;
        [SerializeField] UnityEvent onWin;

        bool timerRunning;

        /// <summary>
        /// Thời gian chơi (giây), tính từ lần chọc đầu tiên đến khi thắng/thua.
        /// Dùng unscaled time nên vẫn đếm cả lúc đang giữ ngắm kim (thế giới bị dừng bằng timeScale = 0).
        /// </summary>
        public float PlayTime { get; private set; }

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
        }

        void HandleStarted() { timerRunning = true; }

        void HandleEmpty()
        {
            timerRunning = false;
            onFail?.Invoke();
            Invoke(nameof(Restart), failRestartDelay);
        }

        void HandleGoal()
        {
            timerRunning = false;
            onWin?.Invoke();
            if (autoLoadNextOnWin && !string.IsNullOrEmpty(nextSceneName))
                Invoke(nameof(LoadNext), winDelay);
        }

        public void Restart()
        {
            CancelInvoke();
            Time.timeScale = 1f;
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
            Debug.LogError("[PopJet] Scene chưa có trong Build Settings nên không Restart được.");
#endif
        }

        public void LoadNext()
        {
            if (string.IsNullOrEmpty(nextSceneName)) return;
            Time.timeScale = 1f;
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
