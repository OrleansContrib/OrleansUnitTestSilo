using System;
using System.Collections.Generic;
using Moq;
using Orleans.Core;
using Orleans.Runtime;

namespace Orleans.TestKit.Storage
{
    public sealed class StorageManager
    {
        private readonly TestKitOptions _options;

        private readonly Dictionary<string, object> _storages = new Dictionary<string, object>();

        public StorageManager(TestKitOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            stateAttributeFactoryMapper = new TestPersistentStateAttributeToFactoryMapper(this);
        }

        internal readonly TestPersistentStateAttributeToFactoryMapper stateAttributeFactoryMapper;

        public IStorage<TState> GetGrainStorage<TGrain, TState>() where TGrain : Grain<TState>
            => GetStorage<TState>(typeof(TGrain).FullName);

        public IStorage<TState> GetStorage<TState>(string stateName)
        {
            var normalisedStateName = stateName ?? "Default";

            if (_storages.TryGetValue(normalisedStateName, out var storage) is false)
            {
                storage = _storages[normalisedStateName] = _options.StorageFactory?.Invoke(typeof(TState)) ?? new TestStorage<TState>();
            }

            return storage as IStorage<TState>;
        }

        public TestStorageStats GetStorageStats() => GetStorageStats("Default");

        public TestStorageStats GetStorageStats(string stateName)
        {
            var normalisedStateName = stateName ?? "Default";

            if (_storages.TryGetValue(normalisedStateName, out var storage))
            {
                var stats = storage as IStorageStats;
                return stats?.Stats;
            }

            return null;
        }

        internal void AddStorage<TState>(IStorage<TState> storage, string stateName = default)
        {
            var normalisedStateName = stateName ?? "Default";

            _storages[normalisedStateName] = storage;
        }
    }
}
