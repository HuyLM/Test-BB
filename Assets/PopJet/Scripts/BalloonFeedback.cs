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
            PlayRandom(ventClips);
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
            if (!balloon.PopWhenEmpty) { PlayOne(emptyClip); return; }

            // Vỡ tung: mảnh vỡ cùng màu bóng bắn ra mọi hướng + tiếng nổ + rung camera, rồi ẩn bóng.
            Color color = Color.white;
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length > 0) color = renderers[0].color;

            if (popBurstPrefab != null)
            {
                var ps = Instantiate(popBurstPrefab, balloon.Center, Quaternion.identity);
                var main = ps.main;
                main.startColor = color;
                Destroy(ps.gameObject, 3f);
            }

            if (popClips != null && popClips.Length > 0) PlayRandom(popClips);
            else PlayOne(emptyClip);

            foreach (var r in renderers) r.enabled = false;
            onPopped?.Invoke();
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
