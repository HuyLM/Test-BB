using System;
using UnityEngine;

namespace PopJet
{
    /// <summary>Chế độ ghi lời giải trong Editor (menu Tools > PopJet > Solutions).</summary>
    public enum SolutionRecordMode
    {
        Off = 0,
        /// <summary>Chỉ lưu khi chưa có lời giải hoặc lượt mới ít cú chọc hơn.</summary>
        KeepBest = 1,
        /// <summary>Luôn ghi đè (dùng sau khi sửa bố cục làm lời giải cũ hỏng).</summary>
        Always = 2,
    }

    /// <summary>
    /// Ghi lời giải của designer khi chơi trong Editor (GDD §8.3 điều 5, §15.7) và phát lại các thao tác đã ghi
    /// (test phát lại, menu Tools > PopJet > Solutions > Replay Current Level).
    /// - Đếm số bước vật lý từ lúc vào màn. Mỗi cú chọc hoặc lần bắn pháo giấy được ghi kèm số bước đó; phát lại đúng
    ///   bước thì vật lý ra y hệt, vì mọi luật tác động lên bóng đều chạy trong FixedUpdate hoặc callback vật lý.
    /// - Ghi vị trí bóng mỗi sampleInterval giây thế giới (nội suy giữa hai bước vật lý), từ thao tác đầu tiên tới khi
    ///   về đích. Bóng ma Gợi ý dùng các vị trí này.
    /// - Về đích khi đang bật ghi (Editor, không phải lượt chơi lại từ cột mốc, không phải lượt phát lại):
    ///   lưu vào LevelDefinition và đặt Par = số cú chọc.
    /// Chạy đầu tiên trong mỗi bước vật lý (DefaultExecutionOrder -200) để thao tác phát lại vào trước mọi script khác,
    /// đúng như cú chọc thật xảy ra trong Update giữa hai bước vật lý.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class SolutionRecorder : MonoBehaviour
    {
        const string PrefKey = "PopJet.SolutionRecordMode";

        [SerializeField] BalloonController balloon;
        [SerializeField] LevelLoader loader;
        [Tooltip("Ghi vị trí bóng mỗi từng này giây thế giới (GDD §15.7: 0.05).")]
        [SerializeField] float sampleInterval = 0.05f;

        /// <summary>Lần load scene kế tiếp sẽ phát lại lời giải này từ bước vật lý đầu tiên (test, menu Replay).</summary>
        public static LevelSolution ReplayOnNextLoad;

        /// <summary>true = không lưu vào asset dù đang bật ghi (smoke test bật lên để không đụng dữ liệu màn).</summary>
        public static bool SuppressSaving;

        public static SolutionRecorder Current { get; private set; }

        /// <summary>Chế độ ghi, lưu trong EditorPrefs. Bản build luôn là Off.</summary>
        public static SolutionRecordMode Mode
        {
            get
            {
#if UNITY_EDITOR
                return (SolutionRecordMode)UnityEditor.EditorPrefs.GetInt(PrefKey, 0);
#else
                return SolutionRecordMode.Off;
#endif
            }
            set
            {
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetInt(PrefKey, (int)value);
#endif
            }
        }

        /// <summary>Về đích, kèm lượt vừa ghi (kể cả khi không lưu vào asset).</summary>
        public event Action<LevelSolution> Completed;

        LevelSolution run;
        int completedTicks;
        int firstTick = -1;
        int nextSample;
        float prevTime;
        Vector3 prevState;
        bool finished;

        LevelSolution replay;
        int nextStep;
        BalloonInput input;
        string status;
        float statusUntil;

        /// <summary>Số bước vật lý đã chạy xong từ lúc vào màn.</summary>
        public int CompletedTicks => completedTicks;
        /// <summary>Lượt đang ghi, hoặc vừa kết thúc (về đích / thua).</summary>
        public LevelSolution Run => run;
        public bool RunFinished => finished;
        public bool IsReplaying => replay != null;
        public bool ReplayDone => replay != null && nextStep >= replay.steps.Count;
        /// <summary>Khác null khi một thao tác phát lại không thực hiện được (bóng đã đi khác lúc ghi).</summary>
        public string ReplayError { get; private set; }

        void Awake()
        {
            Current = this;
            run = new LevelSolution { sampleInterval = sampleInterval, fixedDeltaTime = Time.fixedDeltaTime };
            if (ReplayOnNextLoad != null)
            {
                replay = ReplayOnNextLoad;
                ReplayOnNextLoad = null;
                // Chạy trước LevelLoader (-100): lời giải luôn phát lại từ điểm xuất phát, không từ cột mốc.
                CheckpointState.Clear();
            }
        }

        void Start()
        {
            if (replay == null) return;
            // Đang phát lại thì người chơi không chạm vào bóng được.
            input = FindAnyObjectByType<BalloonInput>();
            if (input != null) input.enabled = false;
        }

        void OnEnable()
        {
            if (balloon == null) return;
            balloon.VentStarted += OnVent;
            balloon.Launched += OnLaunch;
            balloon.GoalReached += OnGoal;
            balloon.BecameEmpty += OnEmpty;
        }

        void OnDisable()
        {
            if (balloon == null) return;
            balloon.VentStarted -= OnVent;
            balloon.Launched -= OnLaunch;
            balloon.GoalReached -= OnGoal;
            balloon.BecameEmpty -= OnEmpty;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void FixedUpdate()
        {
            // Đầu bước vật lý: thao tác phát lại của bước này vào trước, như cú chọc thật trong Update vừa rồi.
            while (replay != null && nextStep < replay.steps.Count && replay.steps[nextStep].tick <= completedTicks)
            {
                int index = nextStep++;
                Execute(replay.steps[index], index);
            }
            if (firstTick >= 0 && !finished) Record((completedTicks - firstTick) * Time.fixedDeltaTime);
            completedTicks++;
        }

        void Execute(SolutionStep step, int index)
        {
            bool ok = false;
            if (step.kind == SolutionStepKind.Poke)
            {
                ok = balloon != null && balloon.TryVent(step.direction);
            }
            else
            {
                foreach (var launcher in FindObjectsByType<Launcher>(FindObjectsSortMode.None))
                {
                    if (!launcher.HasBalloon) continue;
                    // Đổi tốc độ bắn sau khi ghi thì theo tốc độ mới, giữ hướng đã ghi.
                    Vector2 v = step.velocity;
                    if (Mathf.Abs(v.magnitude - launcher.LaunchSpeed) > launcher.LaunchSpeed * 0.01f)
                        v = step.direction.normalized * launcher.LaunchSpeed;
                    launcher.FireVelocity(v);
                    ok = true;
                    break;
                }
            }
            if (!ok && ReplayError == null)
                ReplayError = $"thao tác {index + 1}/{replay.steps.Count} ({step.kind}) ở bước {step.tick} không thực hiện được";
        }

        Vector3 State() => new Vector3(balloon.Body.position.x, balloon.Body.position.y, balloon.Radius);

        /// <summary>Thêm các mẫu đều nhau tới thời điểm time (nội suy giữa trạng thái trước và trạng thái hiện tại).</summary>
        void Record(float time)
        {
            var state = State();
            if (time > prevTime)
            {
                while (nextSample * sampleInterval <= time + 1e-5f)
                {
                    float k = (nextSample * sampleInterval - prevTime) / (time - prevTime);
                    run.path.Add(Vector3.Lerp(prevState, state, k));
                    nextSample++;
                }
            }
            prevTime = time;
            prevState = state;
        }

        void OnVent(Vector2 hole, Vector2 flyDir) { AddStep(SolutionStepKind.Poke, flyDir, Vector2.zero); }

        void OnLaunch(Vector2 velocity) { AddStep(SolutionStepKind.Launch, velocity.normalized, velocity); }

        void AddStep(SolutionStepKind kind, Vector2 direction, Vector2 velocity)
        {
            if (finished || balloon == null) return;
            if (firstTick < 0)
            {
                // Đường bay bắt đầu từ thao tác đầu tiên (trước đó bóng đứng yên, không cần ghi).
                firstTick = completedTicks;
                prevTime = 0f;
                prevState = State();
                run.path.Add(prevState);
                nextSample = 1;
            }
            run.steps.Add(new SolutionStep
            {
                kind = kind,
                tick = completedTicks,
                time = (completedTicks - firstTick) * Time.fixedDeltaTime,
                direction = direction,
                velocity = velocity,
            });
            if (kind == SolutionStepKind.Poke) run.pokes++;
        }

        void Finish()
        {
            finished = true;
            run.totalTicks = completedTicks;
            if (firstTick < 0) return;
            float end = (completedTicks - firstTick) * Time.fixedDeltaTime;
            Record(end);
            // Mẫu cuối phủ tới đúng lúc kết thúc (bóng ma dừng ở vị trí cuối).
            if ((nextSample - 1) * sampleInterval < end - 1e-4f)
            {
                run.path.Add(State());
                nextSample++;
            }
        }

        void OnEmpty()
        {
            if (finished) return;
            Finish();
        }

        void OnGoal()
        {
            if (finished) return;
            Finish();
            run.recordedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            Completed?.Invoke(run);
#if UNITY_EDITOR
            bool fromCheckpoint = loader != null && loader.StartedFromCheckpoint;
            if (replay == null && !fromCheckpoint && !SuppressSaving && Mode != SolutionRecordMode.Off && run.steps.Count > 0)
                SaveToLevel();
#endif
        }

#if UNITY_EDITOR
        void SaveToLevel()
        {
            var def = loader != null ? loader.Level : null;
            if (def == null) return;
            var old = def.HasSolution ? def.Solution : null;
            if (Mode == SolutionRecordMode.KeepBest && old != null && old.pokes <= run.pokes)
            {
                ShowStatus($"Not saved: {run.pokes} pokes, best is {old.pokes}");
                Debug.Log($"[PopJet] Lời giải {def.Id}: {run.pokes} cú chọc, không ít hơn bản đã ghi ({old.pokes}) nên không lưu.");
                return;
            }
            int oldPar = def.Par;
            def.SetSolution(run.Clone());
            UnityEditor.EditorUtility.SetDirty(def);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(def);
            ShowStatus($"Saved: {run.pokes} pokes, Par {def.Par}");
            Debug.Log($"[PopJet] Đã ghi lời giải {def.Id}: {run.pokes} cú chọc, {run.steps.Count} thao tác, " +
                      $"{run.Duration:0.0} giây. Par {oldPar} -> {def.Par}" + (old != null ? $" (bản cũ {old.pokes} cú chọc)." : "."));
        }

        void ShowStatus(string text)
        {
            status = text;
            statusUntil = Time.unscaledTime + 4f;
        }

        GUIStyle style;

        // Nhãn nhỏ để designer biết đang ghi hoặc đang phát lại (chỉ trong Editor).
        void OnGUI()
        {
            var mode = Mode;
            bool showStatus = status != null && Time.unscaledTime < statusUntil;
            if (replay == null && mode == SolutionRecordMode.Off && !showStatus) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
                style.normal.textColor = new Color(1f, 0.35f, 0.3f);
            }
            string text = replay != null
                ? $"REPLAY {nextStep}/{replay.steps.Count}" + (ReplayError != null ? "  ERROR: " + ReplayError : "")
                : mode != SolutionRecordMode.Off
                    ? $"REC {(mode == SolutionRecordMode.KeepBest ? "best" : "always")}  pokes {run.pokes}" +
                      (loader != null && loader.StartedFromCheckpoint ? "  (checkpoint run: not saved)" : "")
                    : "";
            if (showStatus) text += (text.Length > 0 ? "\n" : "") + status;
            GUI.Label(new Rect(0f, Screen.height * 0.13f, Screen.width, 120f), text, style);
        }
#endif
    }
}
