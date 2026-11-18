using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// "Thời gian dừng" nghe được (GDD §11.1): khi bắt đầu ngắm, nhạc bị lọc low-pass và nhỏ đi
    /// kèm tiếng tick; khi thả tay, bộ lọc mở lại, chọc thật thì có tiếng "vút".
    /// AudioSource vẫn phát khi timeScale = 0, nên mọi chuyển đổi chạy bằng unscaled time.
    /// Gắn vào object nhạc (AudioSource loop + AudioLowPassFilter). Tiếng tick/vút phát từ sfxSource
    /// ở object con để không bị bộ lọc làm nghẹt. Mọi giá trị số là starting value.
    /// </summary>
    [RequireComponent(typeof(AudioSource), typeof(AudioLowPassFilter))]
    public class TimeFreezeAudio : MonoBehaviour
    {
        [SerializeField] BalloonInput input;
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioClip tickClip;
        [SerializeField] AudioClip whooshClip;
        [Tooltip("Tần số cắt khi thế giới dừng. Người chơi không nhận ra \"dừng\" thì hạ xuống 500.")]
        [SerializeField] float frozenCutoff = 800f;
        [SerializeField] float frozenVolumeDb = -4f;
        [SerializeField] float freezeSeconds = 0.15f;
        [SerializeField] float releaseSeconds = 0.2f;

        const float OpenCutoff = 22000f;

        AudioSource music;
        AudioLowPassFilter lowPass;
        float baseVolume;
        float amount, target, speed = 1f;

        void Awake()
        {
            music = GetComponent<AudioSource>();
            lowPass = GetComponent<AudioLowPassFilter>();
            baseVolume = music.volume;
            Apply();
        }

        void OnEnable()
        {
            if (input == null) return;
            input.AimStarted += OnAimStart;
            input.AimEnded += OnAimEnd;
        }

        void OnDisable()
        {
            if (input == null) return;
            input.AimStarted -= OnAimStart;
            input.AimEnded -= OnAimEnd;
        }

        void OnAimStart()
        {
            target = 1f;
            speed = 1f / Mathf.Max(0.01f, freezeSeconds);
            PlaySfx(tickClip);
        }

        void OnAimEnd(bool vented)
        {
            target = 0f;
            speed = 1f / Mathf.Max(0.01f, releaseSeconds);
            if (vented) PlaySfx(whooshClip);
        }

        void Update()
        {
            if (Mathf.Approximately(amount, target)) return;
            amount = Mathf.MoveTowards(amount, target, speed * Time.unscaledDeltaTime);
            Apply();
        }

        void Apply()
        {
            // Nội suy theo thang log để tai nghe thấy đều.
            lowPass.cutoffFrequency = Mathf.Exp(Mathf.Lerp(Mathf.Log(OpenCutoff), Mathf.Log(frozenCutoff), amount));
            music.volume = baseVolume * Mathf.Lerp(1f, Mathf.Pow(10f, frozenVolumeDb / 20f), amount);
        }

        void PlaySfx(AudioClip clip)
        {
            if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
        }
    }
}
