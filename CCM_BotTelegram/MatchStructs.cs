namespace CCM_BotTelegram
{
    struct Card
    {
        public int id;
        public string text;
        public bool used;
        public bool modify;

        public static Card InvalidCard() { return new Card { id = -1, text = "", used = false, modify = false }; }
        public bool IsInvalid() { return id == -1; }
    }

    struct PlayerStats
    {
        public long id;
        public string name;
        public int points;

        public bool GreaterThan(PlayerStats other) { return points > other.points; }
        public override string ToString() { return $"{name}, {points} punti"; }
    }

    struct PollInfo
    {
        public string id;
        public int messageId;
    }

    struct MatchSetting
    {
        public int MessageSettingId { get; set; }
        public int Round { get; set; } // -1 means infinite round

        public MatchSetting(int round, int messageSettingId)
        {
            Round = round;
            MessageSettingId = messageSettingId;
        }
        public string RoundToString() { return Round == -1 ? "Infiniti" : Round.ToString(); }
    }
}
