using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Âm thanh còn thiếu của GDD §11.2 (BalloonFeedback lo chọc, rò, vá, nảy, nổ):
    /// - Nút UI kêu "pốp" nhỏ (PuffyButton gọi UiPop), 3 biến thể.
    /// - Nhặt Sao: tiếng chuông, 3 biến thể. Màn Kết quả: mỗi sao phồng lên một nốt Đô, Mi, Son (ResultNote).
    /// - Khí còn chưa tới 1 cú chọc: "cót két" nhẹ mỗi creakInterval giây (thời gian thế giới, dừng khi ngắm).
    /// - Nổ: ép nhạc nhỏ đi duckSeconds giây (TimeFreezeAudio.Duck).
    /// Tiếng UI vẫn phát khi Tạm dừng/menu (ignoreListenerPause). Clip là placeholder do builder sinh.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        [SerializeField] BalloonController balloon;
        [SerializeField] LevelSession session;
        [SerializeField] TimeFreezeAudio music;
        [SerializeField] AudioClip[] uiClips;
        [SerializeField] AudioClip[] starClips;
        [Tooltip("3 nốt cho 3 sao ở màn Kết quả: Đô, Mi, Son.")]
        [SerializeField] AudioClip[] resultNotes;
        [SerializeField] AudioClip creakClip;
        [Tooltip("Tiếng nổ khi chuyển màn bằng bóng nổ (ScreenTransition).")]
        [SerializeField] AudioClip[] popClips;
        [Tooltip("Cót két mỗi bao nhiêu giây khi khí còn chưa tới 1 cú chọc. Starting value.")]
        [SerializeField] float creakInterval = 1f;
        [Tooltip("Nổ thì nhạc nhỏ đi bao lâu (giây). GDD §11.2: 0.5.")]
        [SerializeField] float duckSeconds = 0.5f;
        [SerializeField, Range(0f, 1f)] float uiVolume = 0.7f;

        public static GameAudio Current { get; private set; }

        AudioSource world, ui;
        float creakT;

        /// <summary>Đếm số lần phát (test đọc).</summary>
        public int UiCount { get; private set; }
        public int StarCount { get; private set; }
        public int CreakCount { get; private set; }
        public int NoteCount { get; private set; }

        void Awake()
        {
            Current = this;
            world = gameObject.AddComponent<AudioSource>();
            world.playOnAwake = false;
            ui = gameObject.AddComponent<AudioSource>();
            ui.playOnAwake = false;
            ui.ignoreListenerPause = true;
        }

        void OnDestroy() { if (Current == this) Current = null; }

        void OnEnable()
        {
            if (session != null) session.StarCollected += OnStar;
            if (balloon != null) balloon.BecameEmpty += OnEmpty;
        }

        void OnDisable()
        {
            if (session != null) session.StarCollected -= OnStar;
            if (balloon != null) balloon.BecameEmpty -= OnEmpty;
        }

        /// <summary>Tiếng "pốp" nhỏ khi nhấn nút UI.</summary>
        public static void UiPop()
        {
            var a = Current;
            if (a == null || a.uiClips == null || a.uiClips.Length == 0) return;
            a.ui.pitch = Random.Range(0.95f, 1.08f);
            a.ui.PlayOneShot(a.uiClips[Random.Range(0, a.uiClips.Length)], a.uiVolume);
            a.UiCount++;
        }

        /// <summary>Tiếng "bụp" khi bóng chuyển màn nổ thành confetti.</summary>
        public static void Pop()
        {
            var a = Current;
            if (a == null || a.popClips == null || a.popClips.Length == 0) return;
            a.ui.pitch = Random.Range(0.95f, 1.05f);
            a.ui.PlayOneShot(a.popClips[Random.Range(0, a.popClips.Length)], a.uiVolume);
        }

        /// <summary>Nốt của sao thứ index (0..2) khi sao phồng ở màn Kết quả.</summary>
        public void ResultNote(int index)
        {
            if (resultNotes == null || index < 0 || index >= resultNotes.Length || resultNotes[index] == null) return;
            ui.pitch = 1f;
            ui.PlayOneShot(resultNotes[index], uiVolume);
            NoteCount++;
        }

        void OnStar()
        {
            if (starClips == null || starClips.Length == 0) return;
            world.pitch = Random.Range(0.97f, 1.03f);
            world.PlayOneShot(starClips[Random.Range(0, starClips.Length)]);
            StarCount++;
        }

        void OnEmpty()
        {
            if (balloon != null && balloon.Burst && music != null) music.Duck(duckSeconds);
        }

        void Update()
        {
            if (balloon == null || creakClip == null) return;
            var s = balloon.State;
            bool low = balloon.Started && balloon.PokesLeft < 1f && (s == BalloonState.Floating || s == BalloonState.Venting);
            if (!low) { creakT = 0f; return; }
            creakT += Time.deltaTime; // scaled: dừng khi ngắm, tạm dừng
            if (creakT < creakInterval) return;
            creakT -= creakInterval;
            world.pitch = Random.Range(0.92f, 1.08f);
            world.PlayOneShot(creakClip, 0.6f);
            CreakCount++;
        }
    }
}
