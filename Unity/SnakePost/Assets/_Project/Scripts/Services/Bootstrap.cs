using _Project.Scripts.Services.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Services
{
    public class Bootstrap : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            ServiceProvider.Save = new PlayerPrefsSaveService();

            Debug.Log("Bootstrap ServiceProvider loaded");
            SceneManager.LoadScene("SnakeWorld");
        }

        private async void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                await ServiceProvider.Save.FlushAsync();
            }
        }
    }
}
