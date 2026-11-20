using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Camera toàn cảnh của màn (GDD §10.3): dùng cho nút Xem màn và camera giới thiệu đầu màn.
    /// Tạo một CinemachineCamera thứ hai lúc chạy, khung vừa toàn bộ màn (theo bounds các renderer của prefab màn).
    /// Bật nó thì Brain blend từ camera follow sang, tắt thì blend về. Brain chạy theo unscaled time
    /// (IgnoreTimeScale) để blend được cả khi thế giới đang dừng.
    /// </summary>
    public class LevelCameraDirector : MonoBehaviour
    {
        /// <summary>Sprite có sortingOrder &lt;= giá trị này là nền trang trí (builder đặt -20/-19), không tính vào khung màn.</summary>
        public const int BackgroundOrderMax = -10;

        [SerializeField] CinemachineCamera followCam;
        [SerializeField] LevelLoader loader;
        [Tooltip("Thời gian blend giữa camera follow và toàn cảnh (giây, unscaled). Starting value.")]
        [SerializeField] float blendSeconds = 0.6f;
        [Tooltip("Giữ toàn cảnh bao lâu ở đầu màn trước khi lia về bóng (giây). Starting value.")]
        [SerializeField] float introHoldSeconds = 1.6f;
        [Tooltip("Chừa lề quanh màn khi thu toàn cảnh (đơn vị world).")]
        [SerializeField] float margin = 0.4f;

        CinemachineCamera lookCam;
        bool looking;

        public bool IsLooking => looking;
        public float BlendSeconds => blendSeconds;

        void Awake()
        {
            var brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
            if (brain != null)
            {
                brain.IgnoreTimeScale = true;
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendSeconds);
            }
        }

        void Start()
        {
            if (followCam == null) return;
            var go = new GameObject("CM LookCam");
            go.SetActive(false);
            lookCam = go.AddComponent<CinemachineCamera>();
            lookCam.Priority = followCam.Priority.Value + 10;
            Frame();
        }

        /// <summary>Đặt camera toàn cảnh vừa khít màn hiện tại.</summary>
        void Frame()
        {
            if (lookCam == null) return;
            Bounds b = LevelBounds();
            var cam = Camera.main;
            float aspect = cam != null ? cam.aspect : 9f / 16f;
            var lens = followCam.Lens;
            lens.OrthographicSize = Mathf.Max(b.extents.y + margin, (b.extents.x + margin) / Mathf.Max(0.1f, aspect));
            lookCam.Lens = lens;
            lookCam.transform.position = new Vector3(b.center.x, b.center.y, followCam.transform.position.z);
        }

        Bounds LevelBounds()
        {
            var root = loader != null && loader.Root != null ? loader.Root.transform : null;
            var b = new Bounds(followCam.transform.position, Vector3.one * 2f);
            if (root == null) return b;
            bool any = false;
            foreach (var r in root.GetComponentsInChildren<SpriteRenderer>())
            {
                // Bỏ qua lớp nền trang trí (order <= -10): ảnh nền rộng hơn hộp màn, tính vào sẽ làm khung toàn cảnh bị lệch.
                if (r.sortingOrder <= BackgroundOrderMax) continue;
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            return b;
        }

        /// <summary>Bật/tắt toàn cảnh. Bật thì dừng thế giới (lý do Look).</summary>
        public void SetLook(bool on)
        {
            if (lookCam == null || on == looking) return;
            looking = on;
            if (on) Frame();
            lookCam.gameObject.SetActive(on);
            TimeController.Set(TimeReason.Look, on);
        }

        public void ToggleLook() { SetLook(!looking); }

        /// <summary>
        /// Camera giới thiệu: bắt đầu ở toàn cảnh, giữ một lúc rồi lia về bóng. skip() trả true thì cắt ngắn.
        /// Thế giới dừng suốt lúc giới thiệu (lý do Intro).
        /// </summary>
        public IEnumerator Intro(Func<bool> skip, Action done)
        {
            TimeController.Set(TimeReason.Intro, true);
            if (lookCam != null)
            {
                Frame();
                // Vào thẳng toàn cảnh không blend: cắt camera ngay frame đầu.
                lookCam.gameObject.SetActive(true);
                lookCam.PreviousStateIsValid = false;
                var brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
                var blend = brain != null ? brain.DefaultBlend : default;
                if (brain != null) brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
                yield return null;
                if (brain != null) brain.DefaultBlend = blend;

                float t = 0f;
                while (t < introHoldSeconds && !skip())
                {
                    // Đặt lại mỗi frame: scene vừa load có thể xóa mọi lý do (TimeController.OnSceneLoaded) sau Start.
                    TimeController.Set(TimeReason.Intro, true);
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                lookCam.gameObject.SetActive(false);
                t = 0f;
                while (t < blendSeconds && !skip())
                {
                    TimeController.Set(TimeReason.Intro, true);
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            TimeController.Set(TimeReason.Intro, false);
            done?.Invoke();
        }

        void OnDisable()
        {
            if (looking) TimeController.Set(TimeReason.Look, false);
            looking = false;
            TimeController.Set(TimeReason.Intro, false);
        }
    }
}
