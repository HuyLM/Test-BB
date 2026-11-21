using System;
using System.Collections.Generic;
using UnityEngine;

namespace PopJet
{
    public enum BalloonState { Floating, Aiming, Venting, Empty, Goal, Ready }

    /// <summary>Lý do bóng nổ hoặc xẹp, để HUD hiện chữ (GDD §3.5; chữ tiếng Anh nằm ở GameText).</summary>
    public enum FailReason { None, OutOfAir, Burned, Zapped, Sliced, Squashed, SuckedIn, CatAttack }

    /// <summary>Ảnh chụp chỉ đọc của một jet đang chạy, dùng cho quỹ đạo dự đoán và hiệu ứng lỗ rò.</summary>
    public struct JetInfo
    {
        public int id;
        /// <summary>Hướng lực đẩy = hướng bóng bay.</summary>
        public Vector2 dir;
        public float thrust;
        public float duration;
        public float elapsed;
        public float airCost;
        public bool isLeak;

        /// <summary>Hệ số lực còn lại, từ 1 giảm tuyến tính về 0.</summary>
        public float Strength01 => duration > 0f ? Mathf.Clamp01(1f - elapsed / duration) : 0f;
        /// <summary>Hướng khí thoát ra (ngược hướng bay), cũng là vị trí lỗ trên mặt bóng.</summary>
        public Vector2 Outward => -dir;
    }

    /// <summary>
    /// Bóng bay: quản lý khí, lực đẩy khi chọc lỗ (vent) và lỗ rò (leak).
    /// Lực đẩy giảm tuyến tính theo thời gian; lỗ rò tự đóng, hoặc được vá bằng PatchAll().
    /// Mọi giá trị số là "starting value" - cần playtest để chỉnh.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class BalloonController : MonoBehaviour
    {
        [Header("Air")]
        [SerializeField, Range(0.1f, 1f)] float startAir = 1f;
        [Tooltip("Tỉ lệ scale khi hết khí (bóng xẹp dần).")]
        [SerializeField, Range(0.2f, 1f)] float minScale = 0.55f;
        [Tooltip("Hệ số lực khi khí gần cạn (1 = không giảm).")]
        [SerializeField, Range(0.2f, 1f)] float minThrustWhenLow = 0.75f; // dễ hơn: cú chọc cuối không yếu hẳn (cũ 0.6)

        [Header("Player vent (starting values)")]
        // Vent lâu hơn, êm hơn (cũ 6 / 1.5): giữ nguyên xung lực thrust*duration/2 = 4.5 nên quãng bay mỗi cú
        // chọc vẫn ~3 đơn vị như bố cục 20 màn; chỉ tốc độ đỉnh thấp hơn và khí phun kéo dài hơn.
        [SerializeField] float ventThrust = 4f;
        [SerializeField] float ventDuration = 2.25f;
        [Tooltip("Tổng khí mất cho mỗi lần chọc (0..1).")]
        [SerializeField, Range(0.01f, 0.5f)] float ventAirCost = 0.12f; // dễ hơn: ~8 cú chọc mỗi bình (cũ 0.18 ≈ 5.5)

        [Header("Leak from spikes (starting values)")]
        [SerializeField] float leakThrust = 1.8f;
        [SerializeField] float leakDuration = 3f;
        [Tooltip("Tổng khí mất cho mỗi lỗ rò (0..1).")]
        [SerializeField, Range(0.005f, 0.3f)] float leakAirCost = 0.04f; // giảm cùng tỉ lệ với ventAirCost (cũ 0.06)
        [Tooltip("Thời gian tối thiểu giữa 2 lỗ rò, chống spam khi trượt dọc thanh gai.")]
        [SerializeField] float leakCooldown = 0.5f;
        [SerializeField] int maxLeaks = 4;

        [Header("Physics")]
        [SerializeField] float damping = 1.5f;
        [SerializeField] bool freezeRotation = true;
        [Tooltip("true = hết khí thì bóng vỡ tung (đứng yên tại chỗ, BalloonFeedback lo hiệu ứng nổ và ẩn bóng).\n" +
                 "false = bóng xẹp rồi rơi xuống với Empty Gravity Scale.")]
        [SerializeField] bool popWhenEmpty = true;
        [Tooltip("Chỉ dùng khi Pop When Empty tắt: trọng lực khi bóng xẹp rơi.")]
        [SerializeField] float emptyGravityScale = 1.2f;
        [Tooltip("Trọng lực sau khi đã bắt đầu chơi (lần chọc đầu tiên). Rất nhỏ = rơi cực chậm.\n" +
                 "Vận tốc rơi tối đa ≈ 9.81 * giá trị này / Damping (0.05 => ~0.33 đơn vị/giây).")]
        [SerializeField] float playGravityScale = 0.05f; // starting value

        // holePosition, flyDirection
        public event Action<Vector2, Vector2> VentStarted;
        public event Action<Vector2, Vector2> LeakAdded;
        public event Action PatchedUp;
        public event Action BecameEmpty;
        public event Action GoalReached;
        /// <summary>Bắn ra một lần ở lần chọc đầu tiên: từ đây bóng bắt đầu rơi chậm.</summary>
        public event Action GameStarted;
        /// <summary>Sau mỗi cú chọc thành công, kèm tổng số cú chọc từ đầu màn (để tính Par).</summary>
        public event Action<int> Poked;
        /// <summary>Pháo giấy vừa bắn bóng ra, kèm vận tốc (SolutionRecorder ghi lại để phát lại đúng y hệt).</summary>
        public event Action<Vector2> Launched;

        struct Jet
        {
            public int id;
            public Vector2 dir;
            public float t;
            public float duration;
            public float thrust;
            public float airCost;
            public bool isLeak;
        }

        readonly List<Jet> jets = new List<Jet>();
        Rigidbody2D rb;
        CircleCollider2D circle;
        float air;
        float baseScale;
        // Thời gian vật lý tính từ lúc vào màn (cộng trong FixedUpdate). Luật về thời gian dùng biến này thay cho
        // Time.time để phát lại lời giải ra y hệt (Time.time tính từ lúc mở app, khác nhau giữa các lần chơi).
        float simTime;
        float lastLeakTime = -99f;
        int nextJetId;
        int pokeCount;
        bool aiming;
        bool empty;
        bool burst;
        bool goal;
        bool started;
        bool held;
        FailReason lastFailReason;

        public float Air01 => air;
        /// <summary>false = vừa load màn, bóng đứng im chờ người chơi chọc lần đầu.</summary>
        public bool Started => started;
        /// <summary>true = hết khí thì vỡ tung thay vì xẹp rơi.</summary>
        public bool PopWhenEmpty => popWhenEmpty;
        /// <summary>true = bóng đã vỡ tung (hết khí khi Pop When Empty bật, hoặc bị vật cứng làm nổ qua Pop()).</summary>
        public bool Burst => burst;
        /// <summary>Lý do lần nổ/xẹp gần nhất (None khi bóng còn sống).</summary>
        public FailReason LastFailReason => lastFailReason;
        public Vector2 Center => transform.position;
        public float Radius => circle.radius * transform.lossyScale.x;
        public bool CanAim => !empty && !goal && !held && air > 0f;
        /// <summary>true = bóng đang bị giữ trong pháo giấy (Launcher), chưa bắn ra.</summary>
        public bool IsHeld => held;
        public bool HasLeaks => LeakCount > 0;
        public int LeakCount { get { int n = 0; foreach (var j in jets) if (j.isLeak) n++; return n; } }
        public int VentCount { get { int n = 0; foreach (var j in jets) if (!j.isLeak) n++; return n; } }

        /// <summary>Số cú chọc từ đầu màn.</summary>
        public int PokeCount => pokeCount;
        /// <summary>Số cú chọc còn lại tính theo khí (có thể lẻ, ví dụ 2.6).</summary>
        public float PokesLeft => ventAirCost > 0f ? air / ventAirCost : 0f;
        /// <summary>true = khí chỉ còn đủ cho tối đa 1 cú chọc: cú chọc tiếp theo sẽ làm bóng hết khí.</summary>
        public bool IsLastPoke => air <= ventAirCost;

        // Thông số chỉ đọc cho quỹ đạo dự đoán.
        public float VentAirCost => ventAirCost;
        public float VentThrust => ventThrust;
        public float VentDuration => ventDuration;
        public float MinThrustWhenLow => minThrustWhenLow;
        public float PlayGravityScale => playGravityScale;
        public Rigidbody2D Body => rb;

        public Vector2 Velocity
        {
            get
            {
#if UNITY_6000_0_OR_NEWER
                return rb.linearVelocity;
#else
                return rb.velocity;
#endif
            }
        }

        /// <summary>Damping hiện tại của Rigidbody2D (tăng gấp 3 khi về đích).</summary>
        public float Damping
        {
            get
            {
#if UNITY_6000_0_OR_NEWER
                return rb.linearDamping;
#else
                return rb.drag;
#endif
            }
        }

        public BalloonState State
        {
            get
            {
                if (goal) return BalloonState.Goal;
                if (empty) return BalloonState.Empty;
                if (aiming) return BalloonState.Aiming;
                if (!started) return BalloonState.Ready;
                return VentCount > 0 ? BalloonState.Venting : BalloonState.Floating;
            }
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            circle = GetComponent<CircleCollider2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = freezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            SetDamping(damping);
            baseScale = transform.localScale.x;
            air = startAir;
            ApplyScale();
        }

        // ---------- Public API ----------

        public void SetAiming(bool value) { aiming = value && CanAim; }

        /// <summary>Chọc lỗ chủ động. flyDir là hướng bóng sẽ bay (ngược với hướng kim).</summary>
        public bool TryVent(Vector2 flyDir)
        {
            if (!CanAim || flyDir.sqrMagnitude < 1e-6f) return false;
            // Chỉ chuẩn hóa khi chưa là vector đơn vị: hướng đã ghi trong lời giải phải phát lại đúng từng bit.
            if (Mathf.Abs(flyDir.sqrMagnitude - 1f) > 1e-4f) flyDir.Normalize();
            jets.Add(new Jet
            {
                id = nextJetId++,
                dir = flyDir,
                duration = ventDuration,
                thrust = ventThrust,
                airCost = ventAirCost,
                isLeak = false
            });
            StartGame();
            pokeCount++;
            VentStarted?.Invoke(Center - flyDir * Radius, flyDir);
            Poked?.Invoke(pokeCount);
            return true;
        }

        /// <summary>Lần di chuyển đầu tiên: bật trọng lực rất nhỏ để bóng không còn đứng im nữa.</summary>
        void StartGame()
        {
            if (started) return;
            started = true;
            rb.gravityScale = playGravityScale;
            GameStarted?.Invoke();
        }

        /// <summary>Gai gọi hàm này khi chạm bóng. hitPoint là điểm chạm trên bề mặt bóng.</summary>
        public bool AddLeak(Vector2 hitPoint)
        {
            if (empty || goal || held) return false;
            if (LeakCount >= maxLeaks) return false;
            if (simTime - lastLeakTime < leakCooldown) return false;
            lastLeakTime = simTime;

            // Vị trí vật lý thật: transform có thể đang nội suy theo khung hình (Interpolate), không lặp lại được.
            Vector2 center = rb.position;
            Vector2 outward = hitPoint - center;
            if (outward.sqrMagnitude < 1e-6f) outward = Vector2.up;
            outward.Normalize();

            // Khí thoát ra phía lỗ => bóng bị đẩy ngược lại.
            Vector2 flyDir = -outward;
            jets.Add(new Jet
            {
                id = nextJetId++,
                dir = flyDir,
                duration = leakDuration,
                thrust = leakThrust,
                airCost = leakAirCost,
                isLeak = true
            });
            LeakAdded?.Invoke(center + outward * Radius, flyDir);
            return true;
        }

        /// <summary>Vá ngay tất cả lỗ rò (keo). Không ảnh hưởng lực chọc chủ động.</summary>
        public bool PatchAll()
        {
            int removed = jets.RemoveAll(j => j.isLeak);
            if (removed > 0) PatchedUp?.Invoke();
            return removed > 0;
        }

        public void AddAir(float amount)
        {
            if (empty || goal) return;
            air = Mathf.Clamp01(air + amount);
            ApplyScale();
        }

        /// <summary>
        /// Chơi lại từ cột mốc (GDD §6 #9): đặt lượng khí và số cú chọc như lúc chạm cột mốc.
        /// LevelLoader gọi trong Start, trước bước vật lý đầu tiên. Bóng vẫn chờ cú chọc đầu như mở màn.
        /// </summary>
        public void RestoreRun(float airLevel, int pokes)
        {
            if (empty || goal) return;
            air = Mathf.Clamp01(airLevel);
            pokeCount = Mathf.Max(0, pokes);
            ApplyScale();
        }

        /// <summary>
        /// Giữ bóng đứng yên tại pos (pháo giấy). Bóng thành kinematic, mọi cú chọc và lỗ rò đang chạy dừng lại,
        /// không ngắm được cho tới khi Launch.
        /// </summary>
        public bool Hold(Vector2 pos)
        {
            if (empty || goal || held) return false;
            held = true;
            aiming = false;
            jets.Clear();
            rb.bodyType = RigidbodyType2D.Kinematic;
            SetVelocity(Vector2.zero);
            rb.position = pos;
            transform.position = pos;
            return true;
        }

        /// <summary>Thả bóng đang bị giữ với vận tốc cho trước. Không tốn khí, không tính là cú chọc.</summary>
        public void Launch(Vector2 velocity)
        {
            if (!held) return;
            held = false;
            rb.bodyType = RigidbodyType2D.Dynamic;
            StartGame();
            SetVelocity(velocity);
            Launched?.Invoke(velocity);
        }

        public void ReachGoal()
        {
            if (goal || empty) return;
            if (held) { held = false; rb.bodyType = RigidbodyType2D.Dynamic; }
            goal = true;
            aiming = false;
            jets.Clear();
            rb.gravityScale = 0f; // về đích thì nằm yên trong vùng đích, không rơi tiếp
            SetDamping(damping * 3f);
            GoalReached?.Invoke();
        }

        /// <summary>
        /// Làm nổ bóng ngay (vật cản nhóm D: lửa, cưa, điện...). Luôn vỡ tung, kể cả khi Pop When Empty tắt.
        /// </summary>
        public void Pop(FailReason reason)
        {
            if (empty || goal) return;
            if (held) { held = false; rb.bodyType = RigidbodyType2D.Dynamic; }
            air = 0f;
            BecomeEmpty(reason, true);
        }

        /// <summary>Chép các jet đang chạy (cú chọc và lỗ rò) vào results. Không tạo rác bộ nhớ.</summary>
        public void GetJets(List<JetInfo> results)
        {
            results.Clear();
            foreach (var j in jets)
            {
                results.Add(new JetInfo
                {
                    id = j.id,
                    dir = j.dir,
                    thrust = j.thrust,
                    duration = j.duration,
                    elapsed = j.t,
                    airCost = j.airCost,
                    isLeak = j.isLeak
                });
            }
        }

        // ---------- Simulation ----------

        void FixedUpdate()
        {
            simTime += Time.fixedDeltaTime;
            if (empty || goal || held) return;

            float dt = Time.fixedDeltaTime;
            float thrustScale = Mathf.Lerp(minThrustWhenLow, 1f, air);

            for (int i = jets.Count - 1; i >= 0; i--)
            {
                Jet j = jets[i];
                float k = Mathf.Clamp01(1f - j.t / j.duration); // lực giảm dần về 0
                rb.AddForce(j.dir * (j.thrust * k * thrustScale), ForceMode2D.Force);
                // Tích phân của k trên [0,1] là 0.5 => nhân 2 để tổng khí mất = airCost.
                air -= j.airCost * 2f * k * dt / j.duration;
                j.t += dt;
                if (j.t >= j.duration) jets.RemoveAt(i);
                else jets[i] = j;
            }

            air = Mathf.Max(0f, air);
            ApplyScale();
            if (air <= 0f) BecomeEmpty(FailReason.OutOfAir, false);
        }

        void BecomeEmpty(FailReason reason, bool forceBurst)
        {
            if (empty) return;
            empty = true;
            aiming = false;
            jets.Clear();
            lastFailReason = reason;
            burst = popWhenEmpty || forceBurst;
            if (burst)
            {
                // Vỡ: không còn là vật thể nữa - tắt vật lý để mảnh vỡ tự bay, không còn va chạm/kích hoạt gai, nút...
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector2.zero;
#else
                rb.velocity = Vector2.zero;
#endif
                rb.simulated = false;
            }
            else
            {
                rb.gravityScale = emptyGravityScale;
            }
            BecameEmpty?.Invoke();
        }

        void ApplyScale()
        {
            float s = Mathf.Lerp(minScale, 1f, air) * baseScale;
            transform.localScale = new Vector3(s, s, 1f);
        }

        void SetVelocity(Vector2 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }

        void SetDamping(float value)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearDamping = value;
#else
            rb.drag = value;
#endif
        }
    }
}
