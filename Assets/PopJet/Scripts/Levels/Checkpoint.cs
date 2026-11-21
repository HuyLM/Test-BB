using System;
using System.Collections.Generic;
using UnityEngine;

namespace PopJet
{
    /// <summary>
    /// Cột mốc đang giữ, sống qua các lần load lại scene (GDD §6 #9). Chơi lại = load lại scene, nên trạng thái để
    /// trong biến static giống chỉ số màn của LevelLoader. Xóa khi đổi màn, về đích, về Trang chủ, "Restart Level".
    /// </summary>
    public static class CheckpointState
    {
        static readonly List<int> coins = new List<int>();

        public static int LevelIndex { get; private set; } = -1;
        /// <summary>Tên object cột mốc trong prefab màn.</summary>
        public static string Id { get; private set; }
        public static Vector2 Position { get; private set; }
        public static float Air { get; private set; }
        public static int Pokes { get; private set; }
        public static bool Star { get; private set; }
        public static IReadOnlyList<int> Coins => coins;

        public static bool IsFor(int levelIndex) => levelIndex >= 0 && levelIndex == LevelIndex;

        public static void Save(int levelIndex, string id, Vector2 position, float air, int pokes)
        {
            LevelIndex = levelIndex;
            Id = id;
            Position = position;
            Air = air;
            Pokes = pokes;
        }

        /// <summary>LevelSession thêm phần của lượt chơi: Sao và xu đã nhặt trước cột mốc.</summary>
        public static void SaveRun(bool star, IEnumerable<int> coinIds)
        {
            Star = star;
            coins.Clear();
            if (coinIds != null) coins.AddRange(coinIds);
        }

        public static void Clear()
        {
            LevelIndex = -1;
            Id = null;
            Star = false;
            coins.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Clear(); }
    }

    /// <summary>
    /// Cột mốc (GDD §6 #9, §8.2 màn trùm): bóng chạm vào thì lưu vị trí cột mốc, lượng khí và số cú chọc.
    /// Thua, hoặc bấm Retry, thì chơi lại từ đây thay vì từ đầu màn; bóng đứng chờ cú chọc đầu như lúc mở màn.
    /// Sao và xu đã nhặt trước cột mốc được giữ (LevelSession). Lượt chơi lại từ cột mốc không được Vương miện.
    /// Có thể đẩy vật đuổi theo (PathMover một chiều, ví dụ máy hút bụi) tới chaserProgress khi chơi lại từ đây,
    /// để vẫn còn áp lực mà không ập tới ngay. Collider phải là trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("Chỗ bóng xuất hiện lại. Để trống = tâm cột mốc.")]
        [SerializeField] Transform respawnPoint;
        [Tooltip("Vật đuổi theo được đẩy tới chaserProgress khi chơi lại từ cột mốc này.")]
        [SerializeField] PathMover[] chasers = new PathMover[0];
        [Tooltip("Quãng đường dọc đường ray (đơn vị world, tính từ đầu đường ray) đặt cho chasers khi chơi lại từ đây.")]
        [SerializeField] float chaserProgress;
        [Tooltip("Lá cờ đổi màu khi kích hoạt.")]
        [SerializeField] SpriteRenderer flag;
        [SerializeField] Color idleColor = new Color(0.78f, 0.76f, 0.72f);
        [SerializeField] Color activeColor = new Color(0.114f, 0.62f, 0.459f); // #1D9E75 như vòng khí

        /// <summary>Bóng vừa chạm cột mốc (lần đầu trong lượt). HUD hiện chữ, LevelSession lưu Sao và xu.</summary>
        public static event Action<Checkpoint> Activated;

        float popT = -1f;
        Vector3 flagScale;

        public bool IsActive { get; private set; }
        public Vector2 RespawnPosition => respawnPoint != null ? (Vector2)respawnPoint.position : (Vector2)transform.position;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (flag != null)
            {
                flagScale = flag.transform.localScale;
                flag.color = idleColor;
            }
        }

        void Start()
        {
            // Vừa chơi lại từ cột mốc này: hiện đã kích hoạt và đẩy vật đuổi theo lên.
            var loader = LevelLoader.Current;
            if (loader == null || !loader.StartedFromCheckpoint || CheckpointState.Id != name) return;
            IsActive = true;
            if (flag != null) flag.color = activeColor;
            foreach (var c in chasers) if (c != null) c.SetProgress(chaserProgress);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (IsActive) return;
            var balloon = other.GetComponentInParent<BalloonController>();
            if (balloon == null || balloon.IsHeld) return;
            var state = balloon.State;
            if (state == BalloonState.Empty || state == BalloonState.Goal) return;
            var loader = LevelLoader.Current;
            if (loader == null) return;

            IsActive = true;
            CheckpointState.Save(loader.Index, name, RespawnPosition, balloon.Air01, balloon.PokeCount);
            if (flag != null) flag.color = activeColor;
            popT = 0f;
            Haptics.Light();
            Activated?.Invoke(this);
        }

        void Update()
        {
            if (popT < 0f || flag == null) return;
            // Cờ phồng lên rồi về cỡ cũ (scaled time).
            popT += Time.deltaTime;
            float s = popT < 0.12f ? Mathf.Lerp(1f, 1.45f, popT / 0.12f) : Mathf.Lerp(1.45f, 1f, (popT - 0.12f) / 0.2f);
            flag.transform.localScale = flagScale * s;
            if (popT >= 0.32f) { flag.transform.localScale = flagScale; popT = -1f; }
        }
    }
}
