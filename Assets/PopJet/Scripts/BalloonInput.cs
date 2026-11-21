using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PopJet
{
    /// <summary>Đọc con trỏ (chuột/chạm) bằng Input System mới hoặc cũ, tùy cấu hình project.</summary>
    static class PointerHelper
    {
        public static bool Down, Held, Up;
        public static Vector2 ScreenPos;
        static int polledFrame = -1;
        static readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        /// <summary>Nhiều script cùng gọi trong một frame thì chỉ đọc một lần, kết quả như nhau.</summary>
        public static void Poll()
        {
            if (polledFrame == Time.frameCount) return;
            polledFrame = Time.frameCount;
#if ENABLE_INPUT_SYSTEM
            var p = Pointer.current;
            if (p == null) { Down = Held = Up = false; return; }
            Down = p.press.wasPressedThisFrame;
            Held = p.press.isPressed;
            Up = p.press.wasReleasedThisFrame;
            ScreenPos = p.position.ReadValue();
#else
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                Down = t.phase == TouchPhase.Began;
                Up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                Held = !Up;
                ScreenPos = t.position;
            }
            else
            {
                Down = Input.GetMouseButtonDown(0);
                Held = Input.GetMouseButton(0);
                Up = Input.GetMouseButtonUp(0);
                ScreenPos = Input.mousePosition;
            }
#endif
        }

        /// <summary>
        /// true = điểm chạm nằm trên phần tử UI có Raycast Target (nút HUD...).
        /// Raycast thủ công nên không phụ thuộc pointer id, chạy được cả chuột lẫn cảm ứng.
        /// </summary>
        public static bool IsOverUI(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var data = new PointerEventData(es) { position = screenPos };
            uiHits.Clear();
            es.RaycastAll(data, uiHits);
            return uiHits.Count > 0;
        }
    }

    /// <summary>
    /// Giữ - kéo - thả: ngón tay ở hướng nào thì cây kim hiện ở hướng đó và đâm lỗ ở đó;
    /// bóng bay theo hướng ngược lại. Thả gần tâm bóng = hủy. Chạm bắt đầu trên UI thì bỏ qua.
    /// Sprite kim: đầu nhọn phải hướng lên (trục +Y local).
    /// </summary>
    public class BalloonInput : MonoBehaviour
    {
        [SerializeField] BalloonController balloon;
        [Tooltip("Sprite cây kim (đầu nhọn hướng +Y). Có thể để trống.")]
        [SerializeField] Transform needle;
        [Tooltip("Mũi tên thẳng chỉ hướng bay (LineRenderer 2 điểm, world space). Có thể để trống;\n" +
                 "scene mẫu dùng TrajectoryPreview thay cho mũi tên này.")]
        [SerializeField] LineRenderer flightArrow;
        [Tooltip("Vùng chạm để bắt đầu giữ = Radius * hệ số này.")]
        [SerializeField] float grabPadding = 1.4f;
        [Tooltip("Vùng hủy = Radius * hệ số này (tính từ tâm bóng).")]
        [SerializeField, Range(0.1f, 1f)] float cancelFactor = 0.4f;
        [Tooltip("Khoảng cách kim cách mặt bóng khi đang ngắm.")]
        [SerializeField] float needleGap = 0.6f;
        [SerializeField] float arrowLength = 1.5f;
        [SerializeField] float stabDuration = 0.08f;
        [Tooltip("Đang giữ chọn hướng kim thì dừng toàn bộ thế giới (vật lý, quạt, cửa, particle, camera) qua TimeController.\n" +
                 "Thời gian chơi trong LevelManager dùng unscaled time nên vẫn đếm.")]
        [SerializeField] bool freezeWorldWhileAiming = true;

        /// <summary>Bắt đầu giữ trên bóng (thế giới vừa dừng).</summary>
        public event Action AimStarted;
        /// <summary>Kết thúc ngắm. true = đã chọc thật, false = hủy.</summary>
        public event Action<bool> AimEnded;
        /// <summary>Người chơi cố ý hủy: đã kéo kim ra rồi thả tay trong vùng hủy (tutorial thôi nhắc cách hủy).</summary>
        public event Action Canceled;

        Camera cam;
        bool aiming;
        bool cancelNow;
        bool hadDirection;
        Vector2 needleDir = Vector2.up;
        Coroutine stabRoutine;

        public BalloonController Balloon => balloon;
        public bool IsAiming => aiming;
        /// <summary>Đang ngắm và ngón tay ngoài vùng hủy: thả ra bây giờ sẽ chọc.</summary>
        public bool HasAimDirection => aiming && !cancelNow;
        /// <summary>Đã kéo kim ra rồi đưa ngón tay về vùng hủy: thả ra bây giờ sẽ hủy (GDD §8.4, gợi ý cách hủy).</summary>
        public bool InCancelZoneAfterAiming => aiming && cancelNow && hadDirection;
        /// <summary>Hướng bóng sẽ bay nếu thả ra bây giờ (ngược hướng kim).</summary>
        public Vector2 FlyDirection => -needleDir;
        /// <summary>Vị trí ngón tay/chuột trong world (cập nhật mỗi frame).</summary>
        public Vector2 PointerWorld { get; private set; }

        void Awake()
        {
            cam = Camera.main;
            HideAimVisuals();
        }

        void Update()
        {
            PointerHelper.Poll();
            if (balloon == null || cam == null) return;

            Vector2 world = ScreenToWorld(PointerHelper.ScreenPos);
            PointerWorld = world;
            Vector2 center = balloon.Center;

            if (!aiming)
            {
                if (PointerHelper.Down && balloon.CanAim && !TimeController.IsBlockingInput &&
                    Vector2.Distance(world, center) <= balloon.Radius * grabPadding &&
                    !PointerHelper.IsOverUI(PointerHelper.ScreenPos))
                    BeginAim();
                else
                    return;
            }

            // Bóng hết khí / chạm đích trong lúc ngắm => hủy.
            if (!balloon.CanAim) { EndAim(false); return; }

            Vector2 d = world - center;
            cancelNow = d.magnitude < balloon.Radius * cancelFactor;
            if (!cancelNow)
            {
                needleDir = d.normalized;
                hadDirection = true;
            }

            UpdateVisuals(center);

            if (PointerHelper.Up)
            {
                EndAim(!cancelNow, true);
            }
            else if (!PointerHelper.Held)
            {
                EndAim(false); // mất focus/đứt chạm
            }
        }

        void BeginAim()
        {
            aiming = true;
            cancelNow = true;
            hadDirection = false;
            balloon.SetAiming(true);
            // Cú đâm kim trước đó chưa xong thì kết thúc luôn, tránh nó chạy tiếp sau khi thế giới chạy lại.
            if (stabRoutine != null) { StopCoroutine(stabRoutine); stabRoutine = null; }
            if (needle != null) needle.gameObject.SetActive(false);
            SetWorldFrozen(true);
            AimStarted?.Invoke();
        }

        /// <param name="released">true = người chơi nhấc tay (không phải mất focus hay bóng hết khí giữa chừng).</param>
        void EndAim(bool fire, bool released = false)
        {
            aiming = false;
            SetWorldFrozen(false); // chạy lại trước khi chọc để lực đẩy áp dụng ngay
            balloon.SetAiming(false);
            if (flightArrow != null) flightArrow.enabled = false;

            bool vented = fire && balloon.TryVent(-needleDir); // bay ngược hướng kim
            AimEnded?.Invoke(vented);
            if (!fire && released && hadDirection) Canceled?.Invoke();
            hadDirection = false;

            if (vented && needle != null)
            {
                if (stabRoutine != null) StopCoroutine(stabRoutine);
                stabRoutine = StartCoroutine(Stab(needleDir));
                return;
            }
            if (needle != null) needle.gameObject.SetActive(false);
        }

        void SetWorldFrozen(bool frozen)
        {
            if (!freezeWorldWhileAiming) return;
            TimeController.Set(TimeReason.Aim, frozen);
        }

        // Không bao giờ để game kẹt ở timeScale 0 khi object bị tắt/hủy giữa lúc đang giữ (đổi scene, restart...).
        void OnDisable()
        {
            if (aiming)
            {
                aiming = false;
                if (balloon != null) balloon.SetAiming(false);
                HideAimVisuals();
            }
            TimeController.Set(TimeReason.Aim, false);
        }

        void UpdateVisuals(Vector2 center)
        {
            bool show = !cancelNow;
            if (needle != null)
            {
                needle.gameObject.SetActive(show);
                if (show)
                {
                    needle.position = center + needleDir * (balloon.Radius + needleGap);
                    needle.up = -needleDir; // đầu kim chĩa vào bóng
                }
            }
            if (flightArrow != null)
            {
                flightArrow.enabled = show;
                if (show)
                {
                    flightArrow.positionCount = 2;
                    flightArrow.SetPosition(0, center);
                    flightArrow.SetPosition(1, center - needleDir * arrowLength);
                }
            }
        }

        IEnumerator Stab(Vector2 dir)
        {
            needle.gameObject.SetActive(true);
            Vector3 from = needle.position;
            float t = 0f;
            while (t < stabDuration)
            {
                t += Time.deltaTime;
                Vector3 to = balloon.Center + dir * balloon.Radius;
                needle.position = Vector3.Lerp(from, to, Mathf.Clamp01(t / stabDuration));
                yield return null;
            }
            needle.gameObject.SetActive(false);
            stabRoutine = null;
        }

        void HideAimVisuals()
        {
            if (needle != null) needle.gameObject.SetActive(false);
            if (flightArrow != null) flightArrow.enabled = false;
        }

        Vector2 ScreenToWorld(Vector2 screen)
        {
            float z = Mathf.Abs(cam.transform.position.z);
            return cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z));
        }
    }
}
