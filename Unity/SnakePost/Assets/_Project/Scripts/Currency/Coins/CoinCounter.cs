using _Project.Scripts.Services;
using _Project.Scripts.Services.Save;
using TMPro;
using UnityEngine;

namespace _Project.Scripts.Currency.Coins
{
    public class CoinCounter : MonoBehaviour
    {
        [SerializeField] private TMP_Text coinCounter;

        private async void Start() => ShowBalance(await ServiceProvider.Save.LoadAsync(SaveKeys.Coins));

        public async void Add(int amount)
        {
            ISaveService save = ServiceProvider.Save;
            int balance = checked(await save.LoadAsync(SaveKeys.Coins) + amount);
            await save.SaveAsync(SaveKeys.Coins, balance);
            ShowBalance(balance);
        }

        private void ShowBalance(int balance)
        {
            if (coinCounter)
                coinCounter.text = balance.ToString();
        }
    }
}