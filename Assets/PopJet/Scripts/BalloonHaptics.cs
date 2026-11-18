using System.Collections;
using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Nối sự kiện của bóng với rung máy (GDD §11.3): ngắm = tick, chọc = vừa, bị rò = hai tick nhẹ,
    /// vá = nhẹ, nảy mạnh = nhẹ, nổ = mạnh, về đích = mẫu "thành công". Gắn vào bóng.
    /// </summary>
    [RequireComponent(typeof(BalloonController))]
    public class BalloonHaptics : MonoBehaviour
    {
        [SerializeField] BalloonInput input;
        [Tooltip("Va chạm từ mức này trở lên (0..1, theo BalloonBounce) mới rung.")]
        [SerializeField, Range(0f, 1f)] float bounceThreshold = 0.35f;
        [Tooltip("Khoảng tối thiểu giữa 2 lần rung do nảy, tránh rung liên tục khi lăn dọc tường.")]
        [SerializeField] float bounceCooldown = 0.08f;

        BalloonController balloon;
        BalloonBounce bounce;
        float lastBounce = -99f;

        void Awake()
        {
            balloon = GetComponent<BalloonController>();
            bounce = GetComponent<BalloonBounce>();
        }

        void OnEnable()
        {
            balloon.VentStarted += OnVent;
            balloon.LeakAdded += OnLeak;
            balloon.PatchedUp += OnPatched;
            balloon.BecameEmpty += OnEmpty;
            balloon.GoalReached += OnGoal;
            if (bounce != null) bounce.Bounced += OnBounce;
            if (input != null) input.AimStarted += OnAim;
        }

        void OnDisable()
        {
            balloon.VentStarted -= OnVent;
            balloon.LeakAdded -= OnLeak;
            balloon.PatchedUp -= OnPatched;
            balloon.BecameEmpty -= OnEmpty;
            balloon.GoalReached -= OnGoal;
            if (bounce != null) bounce.Bounced -= OnBounce;
            if (input != null) input.AimStarted -= OnAim;
        }

        void OnAim() => Haptics.Selection();
        void OnVent(Vector2 hole, Vector2 dir) => Haptics.Medium();
        void OnLeak(Vector2 hole, Vector2 dir) => StartCoroutine(DoubleTap());
        void OnPatched() => Haptics.Light();
        void OnEmpty() { if (balloon.Burst) Haptics.Heavy(); else Haptics.Medium(); }
        void OnGoal() => Haptics.Success();

        void OnBounce(float strength)
        {
            if (strength < bounceThreshold || Time.unscaledTime - lastBounce < bounceCooldown) return;
            lastBounce = Time.unscaledTime;
            Haptics.Light();
        }

        IEnumerator DoubleTap()
        {
            Haptics.Light();
            yield return new WaitForSecondsRealtime(0.08f);
            Haptics.Light();
        }
    }
}
