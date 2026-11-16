#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Services.Save
{
    public sealed class PlayerPrefsSaveService : ISaveService
    {
        private const string KeyPrefix = "SnakePost_Demo_";

        public ValueTask<T> LoadAsync<T>(SaveKey<T> key, CancellationToken cancellationToken = default)
            where T : notnull
        {
            cancellationToken.ThrowIfCancellationRequested();
            string storageKey = GetStorageKey(key);

            object value = key switch
                           {
                               SaveKey<int> entry    => PlayerPrefs.GetInt(storageKey, entry.DefaultValue),
                               SaveKey<float> entry  => PlayerPrefs.GetFloat(storageKey, entry.DefaultValue),
                               SaveKey<string> entry => PlayerPrefs.GetString(storageKey, entry.DefaultValue),
                               SaveKey<bool> entry   => PlayerPrefs.GetInt(storageKey, entry.DefaultValue ? 1 : 0) != 0,
                               _                     => throw UnsupportedType<T>()
                           };

            return new((T)value);
        }

        /// <summary>
        ///     Updates the value in memory until preferences are flushed.
        /// </summary>
        public ValueTask SaveAsync<T>(SaveKey<T> key, T value, CancellationToken cancellationToken = default)
            where T : notnull
        {
            cancellationToken.ThrowIfCancellationRequested();
            string storageKey = GetStorageKey(key);

            switch (key, value)
            {
                case (SaveKey<int>, int number):     PlayerPrefs.SetInt(storageKey, number); break;
                case (SaveKey<float>, float number): PlayerPrefs.SetFloat(storageKey, number); break;
                case (SaveKey<string>, string text): PlayerPrefs.SetString(storageKey, text); break;
                case (SaveKey<bool>, bool flag):     PlayerPrefs.SetInt(storageKey, flag ? 1 : 0); break;
                case (_, null):                      throw new ArgumentNullException(nameof(value));
                default:                             throw UnsupportedType<T>();
            }

            return default;
        }

        public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PlayerPrefs.Save();
            return default;
        }

        private static string GetStorageKey<T>(SaveKey<T> key) where T : notnull =>
            KeyPrefix + (key ?? throw new ArgumentNullException(nameof(key))).Name;

        private static NotSupportedException UnsupportedType<T>() => new($"PlayerPrefs cannot store {typeof(T).Name}.");
    }
}