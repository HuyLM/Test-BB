namespace PopJet
{
    /// <summary>
    /// Mọi chữ hiển thị trong game, gom một chỗ. Game chỉ có tiếng Anh (GDD §10.8).
    /// Sau này: chuyển sang bảng chuỗi (CSV hoặc ScriptableObject) để dịch được.
    /// </summary>
    public static class GameText
    {
        public const string GameTitle = "Pop Jet";
        public const string Retry = "Retry";
        public const string Stuck = "Stuck?";
        public const string Air = "Air";

        public const string StarCollected = "Star!";

        // Màn hình (GDD §10.8)
        public const string Play = "Play";
        public const string Next = "Next";
        public const string Resume = "Resume";
        public const string Paused = "Paused";
        public const string Map = "Map";
        public const string Home = "Home";
        public const string Settings = "Settings";
        public const string Back = "Back";
        public const string Look = "Look";
        public const string LevelComplete = "Level Complete!";
        public const string Crown = "Crown!";
        public const string Coins = "Coins";
        public const string Sound = "Sound";
        public const string Music = "Music";
        public const string Haptics = "Haptics";
        public const string ReduceMotion = "Reduce Motion";
        public const string On = "On";
        public const string Off = "Off";
        public const string TapToStart = "Tap to start";
        public const string AllDone = "More levels coming soon!";

        // Gợi ý (GDD §3.5, §7.2)
        public const string Hint = "Hint";
        public const string HintInvite = "Need a hint?";

        // Tutorial (GDD §8.4)
        public const string CancelTip = "Release in the middle to cancel";
        public static string ParIntro(int par) => par == 1 ? "Par 1: finish in 1 poke" : $"Par {par}: finish in {par} pokes or fewer";

        // Cột mốc (GDD §6 #9)
        public const string Checkpoint = "Checkpoint!";
        public const string RestartLevel = "Restart Level";

        // Mục tiêu đầu màn (GDD §10.3)
        public const string GoalReach = "Reach the goal";
        public const string GoalStar = "Grab the star";
        public static string GoalPar(int par) => par == 1 ? "Do it in 1 poke" : $"Do it in {par} pokes";

        public static string Pokes(int count) => "Pokes " + count;
        public static string Pokes(int count, int par) => $"Pokes {count} / Par {par}";
        public static string CoinsEarned(int n) => "+" + n + " " + Coins;
        public static string World(int n)
        {
            switch (n)
            {
                case 1: return "The House";
                case 2: return "The Garden";
                case 3: return "The City";
                case 4: return "Toy Factory";
                case 5: return "Storm Sky";
                case 6: return "Outer Space";
                default: return "World " + n;
            }
        }

        /// <summary>Báo khi về đích. Font chưa chắc có ký tự ★ nên viết bằng chữ.</summary>
        public static string Result(int stars, bool crown) =>
            (stars == 1 ? "1 Star" : stars + " Stars") + (crown ? " + Crown!" : "!");

        public static string Fail(FailReason reason)
        {
            switch (reason)
            {
                case FailReason.OutOfAir: return "Out of air!";
                case FailReason.Burned: return "Burned!";
                case FailReason.Zapped: return "Zapped!";
                case FailReason.Sliced: return "Sliced!";
                case FailReason.Squashed: return "Squashed!";
                case FailReason.SuckedIn: return "Sucked in!";
                case FailReason.CatAttack: return "Cat attack!";
                default: return "Pop!";
            }
        }
    }
}
