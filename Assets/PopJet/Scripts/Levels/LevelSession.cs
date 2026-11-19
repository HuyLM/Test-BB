using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PopJet
{
    /// <summary>Kết quả một lượt chơi khi về đích (GDD §3.4).</summary>
    public struct LevelResult
    {
        public string levelId;
        public int pokes;
        public int par;
        public bool goal;       // ★ Về đích
        public bool star;       // ★ Nhặt Sao (màn không có Sao thì tự đạt)
        public bool underPar;   // ★ Số cú chọc ≤ Par
        public bool crown;      // Đủ 3 sao trong cùng một lượt
        public int coinsEarned; // Xu cộng vào ví ở lượt này
        public bool firstClear; // Lần đầu về đích màn này

        public int Stars => (goal ? 1 : 0) + (star ? 1 : 0) + (underPar ? 1 : 0);
    }

    /// <summary>Thành tích tốt nhất của một màn, gộp qua nhiều lượt: mỗi sao lưu riêng (GDD §3.4).</summary>
    [Serializable]
    public class LevelRecord
    {
        public bool goal, star, underPar, crown;
        public int bestPokes = int.MaxValue;
        /// <summary>Mã các xu đã nhặt (chỉ lần nhặt đầu được tính tiền).</summary>
        public List<int> coins = new List<int>();

        public int Stars => (goal ? 1 : 0) + (star ? 1 : 0) + (underPar ? 1 : 0);

        public void Merge(LevelResult r)
        {
            goal |= r.goal;
            star |= r.star;
            underPar |= r.underPar;
            crown |= r.crown;
            if (r.goal) bestPokes = Mathf.Min(bestPokes, r.pokes);
        }
    }

    /// <summary>
    /// Theo dõi một lượt chơi (GDD §3.4): số cú chọc so với Par, đã nhặt Sao chưa, và tính 3 sao + Vương miện
    /// khi bóng về đích. Thành tích gộp theo từng sao qua các lượt và lưu ra file qua SaveSystem.
    /// Par lấy từ LevelDefinition của màn hiện tại (LevelLoader).
    /// </summary>
    public class LevelSession : MonoBehaviour
    {
        [SerializeField] BalloonController balloon;
        [SerializeField] LevelLoader loader;
        [Tooltip("Par khi scene không có LevelLoader.")]
        [SerializeField, Min(1)] int fallbackPar = 3;
        [Tooltip("Chơi lại màn đã qua thì chỉ được từng này xu, cộng xu mới chưa nhặt lần nào (GDD §12.2, §17).")]
        [SerializeField, Min(0)] int replayCoins = 5;

        public static LevelSession Current { get; private set; }

        /// <summary>Vừa nhặt Sao của màn.</summary>
        public event Action StarCollected;
        /// <summary>Về đích, kèm kết quả lượt này (đã gộp vào thành tích).</summary>
        public event Action<LevelResult> Completed;
        /// <summary>Vừa nhặt một xu. Tham số: số xu đã nhặt trong lượt này.</summary>
        public event Action<int> CoinCollected;

        bool starCollected;
        bool completed;
        readonly List<int> runCoins = new List<int>();

        public string LevelId => loader != null && loader.Level != null ? loader.Level.Id : SceneManager.GetActiveScene().name;
        public int Par => loader != null && loader.Level != null ? loader.Level.Par : fallbackPar;
        public int Pokes => balloon != null ? balloon.PokeCount : 0;
        public bool StarCollectedThisRun => starCollected;
        /// <summary>false = màn không đặt Sao nào, mục tiêu Sao tự đạt.</summary>
        public bool LevelHasStar { get; private set; }
        public int CoinsThisRun => runCoins.Count;
        public LevelResult LastResult { get; private set; }
        public bool HasResult => completed;

        /// <summary>Thành tích đã lưu của một màn (null = chưa về đích lần nào).</summary>
        public static LevelRecord GetRecord(string levelId) => SaveSystem.GetRecord(levelId);

        /// <summary>Tổng số sao đã có trên mọi màn (dùng cho mốc mở màn thưởng sau này).</summary>
        public static int TotalStars()
        {
            int n = 0;
            foreach (var e in SaveSystem.Data.levels) n += e.record.Stars;
            return n;
        }

        void Awake()
        {
            Current = this;
        }

        void Start()
        {
            // LevelLoader đã tạo prefab màn trong Awake (chạy trước mọi script).
            LevelHasStar = FindObjectsByType<StarPickup>(FindObjectsSortMode.None).Length > 0;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void OnEnable()
        {
            if (balloon != null) balloon.GoalReached += OnGoal;
        }

        void OnDisable()
        {
            if (balloon != null) balloon.GoalReached -= OnGoal;
        }

        /// <summary>StarPickup gọi khi bóng chạm Sao.</summary>
        public void CollectStar(StarPickup star)
        {
            if (completed || starCollected) return;
            starCollected = true;
            StarCollected?.Invoke();
        }

        /// <summary>true = xu này đã được nhặt ở một lượt trước (đã tính tiền rồi).</summary>
        public bool CoinAlreadyOwned(int coinId)
        {
            var rec = SaveSystem.GetRecord(LevelId);
            return rec != null && rec.coins.Contains(coinId);
        }

        /// <summary>CoinPickup gọi khi bóng chạm xu.</summary>
        public void CollectCoin(int coinId)
        {
            if (completed || runCoins.Contains(coinId)) return;
            runCoins.Add(coinId);
            CoinCollected?.Invoke(runCoins.Count);
        }

        void OnGoal()
        {
            if (completed) return;
            completed = true;
            var prev = SaveSystem.GetRecord(LevelId);
            bool replay = prev != null && prev.goal;
            int fresh = 0;
            foreach (int id in runCoins) if (prev == null || !prev.coins.Contains(id)) fresh++;
            var r = new LevelResult
            {
                levelId = LevelId,
                pokes = Pokes,
                par = Par,
                goal = true,
                star = starCollected || !LevelHasStar,
                underPar = Pokes <= Par,
                firstClear = !replay,
                coinsEarned = fresh + (replay ? replayCoins : 0)
            };
            r.crown = r.Stars == 3; // chưa có booster; phần sau: dùng booster thì không được Vương miện
            LastResult = r;
            SaveSystem.MergeResultWithCoins(r, runCoins); // gộp từng sao, cộng xu và ghi file ngay
            Completed?.Invoke(r);
        }
    }
}
