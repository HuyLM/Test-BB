using UnityEngine;
using UnityEngine.Events;

namespace PopJet
{
    /// <summary>
    /// Gắn âm thanh + particle vào sự kiện của bóng (tối thiểu 2 kênh phản hồi: audio + VFX).
    /// Particle nên phát theo hướng +Y local (script sẽ xoay +Y về hướng khí thoát).
    /// </summary>
    [RequireComponent(typeof(BalloonController))]
    public class BalloonFeedback : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] AudioSource source;
        [Tooltip("Nên có từ 3 clip trở lên để tránh lặp.")]
        [SerializeField] AudioClip[] ventClips;
        [SerializeField] AudioClip[] leakClips;
        [SerializeField] AudioClip[] patchClips;
        [Tooltip("Tiếng xẹp, dùng khi bóng KHÔNG vỡ (Pop When Empty tắt).")]
        [SerializeField] AudioClip emptyClip;
        [Tooltip("Tiếng nổ khi bóng vỡ. Nên có từ 3 clip trở lên.")]
        [SerializeField] AudioClip[] popClips;
        [SerializeField] AudioClip goalClip;
        [SerializeField] Vector2 pitchRange = new Vector2(0.92f, 1.08f);

        [Header("VFX")]
        [SerializeField] ParticleSystem ventPuffPrefab;
        [SerializeField] ParticleSystem leakPuffPrefab;
        [SerializeField] ParticleSystem patchPuffPrefab;
        [Tooltip("Hiệu ứng vỡ tung. Particle gốc (mảnh vỡ) được tô theo màu bóng.")]
        [SerializeField] ParticleSystem popBurstPrefab;

        [Header("Pop")]
        [Tooltip("Gọi khi bóng vỡ - ví dụ nối vào CinemachineImpulseSource.GenerateImpulse để rung camera.")]
        [SerializeField] UnityEvent onPopped;

        BalloonController balloon;

        /// <summary>Số lần đã gọi onPopped (rung camera) trong scene này; test Giảm chuyển động đọc.</summary>
        public int ShakeCount { get; private set; }

        void Awake()
        {
            balloon = GetComponent<BalloonController>();
            if (source == null) source = GetComponent<AudioSource>();
        }

        void OnEnable()
        {
            balloon.VentStarted += OnVent;
            balloon.LeakAdded += OnLeak;
            balloon.PatchedUp += OnPatched;
            balloon.BecameEmpty += OnEmpty;
            balloon.GoalReached += OnGoal;
        }

        void OnDisable()
        {
            balloon.VentStarted -= OnVent;
            balloon.LeakAdded -= OnLeak;
            balloon.PatchedUp -= OnPatched;
            balloon.BecameEmpty -= OnEmpty;
            balloon.GoalReached -= OnGoal;
        }

        void OnVent(Vector2 pos, Vector2 flyDir)
        {
            // GDD §11.2: pitch tăng dần khi khí giảm (1.0 lúc đầy, 1.3 lúc cạn), nghe cũng biết sắp hết khí.
            if (source != null && ventClips != null && ventClips.Length > 0)
            {
                source.pitch = Mathf.Lerp(1.3f, 1f, balloon.Air01) * Random.Range(0.97f, 1.03f);
                source.PlayOneShot(ventClips[Random.Range(0, ventClips.Length)]);
            }
            Spawn(ventPuffPrefab, pos, -flyDir);
        }

        void OnLeak(Vector2 pos, Vector2 flyDir)
        {
            PlayRandom(leakClips);
            Spawn(leakPuffPrefab, pos, -flyDir);
        }

        void OnPatched()
        {
            PlayRandom(patchClips);
            Spawn(patchPuffPrefab, balloon.Center, Vector2.up);
        }

        void OnEmpty()
        {
            if (!balloon.Burst) { PlayOne(emptyClip); return; }

            // Vỡ tung: mảnh vỡ cùng màu bóng bắn ra mọi hướng + tiếng nổ + rung camera, rồi ẩn bóng.
            Color color = Color.white;
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length > 0) color = renderers[0].color;

            if (popBurstPrefab != null)
            {
                var ps = Instantiate(popBurstPrefab, balloon.Center, Quaternion.identity);
                var main = ps.main;
                main.startColor = color;
                // Giảm chuyển động (GDD §10.7): bỏ chớp sáng khi nổ.
                if (GameSettings.ReduceMotion)
                {
                    var flash = ps.transform.Find("Flash");
                    if (flash != null) flash.gameObject.SetActive(false);
                }
                Destroy(ps.gameObject, 3f);
            }

            if (popClips != null && popClips.Length > 0) PlayRandom(popClips);
            else PlayOne(emptyClip);

            foreach (var r in renderers) r.enabled = false;
            // onPopped nối vào rung camera: Giảm chuyển động thì tắt.
            if (!GameSettings.ReduceMotion)
            {
                ShakeCount++;
                onPopped?.Invoke();
            }
        }
        void OnGoal() { PlayOne(goalClip); }

        void PlayRandom(AudioClip[] clips)
        {
            if (source == null || clips == null || clips.Length == 0) return;
            PlayOne(clips[Random.Range(0, clips.Length)]);
        }

        void PlayOne(AudioClip clip)
        {
            if (source == null || clip == null) return;
            source.pitch = Random.Range(pitchRange.x, pitchRange.y);
            source.PlayOneShot(clip);
        }

        void Spawn(ParticleSystem prefab, Vector2 pos, Vector2 outward)
        {
            if (prefab == null) return;
            var rot = Quaternion.FromToRotation(Vector3.up, outward);
            var ps = Instantiate(prefab, pos, rot);
            Destroy(ps.gameObject, 2f);
        }
    }
}
