using System;
using TMPro;
using UnityEngine;
using System.Threading.Tasks;
using _Project.Scripts.Currency.Coins;
using _Project.Scripts.Services;
using _Project.Scripts.Services.Save;

namespace _Project.Scripts.Meta.DailyReward
{
    public class DailyReward : MonoBehaviour
    {
        [SerializeField] CoinCounter coins;
        [SerializeField] TMP_Text dailyRewardLabel;
        [SerializeField] private int reward = 50;


        private static int Today => (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalDays;   // UTC day number
    
        private async void Start() => Show(await ClaimedTodayAsync());

        public async void Claim()                         // On Click () → DailyReward.Claim
        {
            if (await ClaimedTodayAsync()) return;
            await ServiceProvider.Save.SaveAsync(SaveKeys.DailyRewardLastDay, Today);
            coins.Add(reward);
            Show(true);
        }

        [ContextMenu("Reset daily")]                // right-click the component to test
        private async void ResetDaily()
        {
            await ServiceProvider.Save.SaveAsync(SaveKeys.DailyRewardLastDay, 0);
            Show(false);
        }

        private static async ValueTask<bool> ClaimedTodayAsync() =>
            await ServiceProvider.Save.LoadAsync(SaveKeys.DailyRewardLastDay) >= Today;

        private void Show(bool claimed) =>
            dailyRewardLabel.text = claimed ? "Come back tomorrow" : $"Daily gift: +{reward}";
    }
}