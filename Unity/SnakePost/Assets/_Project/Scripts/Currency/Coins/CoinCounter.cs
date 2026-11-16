using _Project.Scripts.Services;
using TMPro;
using UnityEngine;

namespace _Project.Scripts.Currency.Coins
{
    public class CoinCounter : MonoBehaviour
    {
        [SerializeField] private TMP_Text coinCounter;

        private void Start() => Show();

        private void Show() => coinCounter.text = ServiceProvider.Save.GetAsync<int>("Coins").Result.ToString();

        public void Add(int amount)
        {
            ServiceProvider.Save.SetAsync("Coins", amount + ServiceProvider.Save.GetAsync<int>("Coins").Result);
            Show();
        }
    }
}