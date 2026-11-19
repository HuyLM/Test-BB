namespace PopJet
{
    /// <summary>
    /// Mọi chữ hiển thị trong game, gom một chỗ. Game chỉ có tiếng Anh (GDD §10.8).
    /// Giai đoạn 2: chuyển sang bảng chuỗi (CSV hoặc ScriptableObject) để dịch được.
    /// </summary>
    public static class GameText
    {
        public const string Retry = "Retry";
        public const string Stuck = "Stuck?";
        public const string Air = "Air";

        public const string StarCollected = "Star!";

        public static string Pokes(int count) => "Pokes " + count;
        public static string Pokes(int count, int par) => $"Pokes {count} / Par {par}";

        /// <summary>Báo khi về đích. Font tạm chưa có ký tự ★ nên viết bằng chữ.</summary>
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
