using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Services.Save
{
    public sealed class PlayerPrefsSaveService : ISaveService
    {
        public Task<T> GetAsync<T>(
            string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            object value = typeof(T) switch
                           {
                               var t when t == typeof(int)    => PlayerPrefs.GetInt(key),
                               var t when t == typeof(float)  => PlayerPrefs.GetFloat(key),
                               var t when t == typeof(string) => PlayerPrefs.GetString(key),
                               var t when t == typeof(bool)   => PlayerPrefs.GetInt(key) != 0,
                               _ => throw new NotSupportedException(
                                   $"Unsupported save type: {typeof(T)}")
                           };

            return Task.FromResult((T)value);
        }

        public Task SetAsync<T>(
            string key, T value, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            switch (value)
            {
                case int i:    PlayerPrefs.SetInt(key, i); break;
                case float f:  PlayerPrefs.SetFloat(key, f); break;
                case string s: PlayerPrefs.SetString(key, s); break;
                case bool b:   PlayerPrefs.SetInt(key, b ? 1 : 0); break;
                default:       throw new NotSupportedException($"Unsupported save type: {typeof(T)}");
            }

            return Task.CompletedTask;
        }
    }
}