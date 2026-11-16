using System;
using TMPro;
using UnityEngine;
using _Project.Scripts.Currency.Coins;
using _Project.Scripts.Services;

namespace _Project.Scripts.Meta.DailyReward
{
    public class DailyReward : MonoBehaviour
    {
        [SerializeField] CoinCounter coins;
        [SerializeField] TMP_Text dailyRewardLabel;
        [SerializeField] private int reward = 50;

        private const string Key = "daily_last_day";

        private static int Today => (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalDays;   // UTC day number
        private bool ClaimedToday => ServiceProvider.Save.GetAsync<int>(Key).Result >= Today;

        private void Start() => Show();
        private void Show() => dailyRewardLabel.text = ClaimedToday ? "Come back tomorrow" : $"Daily gift: +{reward}";

        public void Claim()                         // On Click () → DailyReward.Claim
        {
            if (ClaimedToday) return;
            ServiceProvider.Save.SetAsync<int>(Key, Today);
            coins.Add(reward);
            Show();
        }

        [ContextMenu("Reset daily")]                // right-click the component to test
        private void ResetDaily()
        {
            ServiceProvider.Save.SetAsync(Key, 0);
            Show();
        }
    }
}