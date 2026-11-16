#nullable enable
using System;

namespace _Project.Scripts.Services.Save
{
    public sealed class SaveKey<T> where T : notnull
    {
        internal SaveKey(string name, T defaultValue)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A save key name is required.", nameof(name));

            Name = name;
            DefaultValue = defaultValue ?? throw new ArgumentNullException(nameof(defaultValue));
        }

        public string Name { get; }
        public T DefaultValue { get; }
    }
}