#nullable enable

namespace _Project.Scripts.Services.Save
{
    public static class SaveKeys
    {
        public static readonly SaveKey<int> Coins = new("coins", 0);
        public static readonly SaveKey<int> DailyRewardLastDay = new("daily_last_day", 0);
    }
}