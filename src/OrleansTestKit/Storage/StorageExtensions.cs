using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
using Orleans.Core;
using Orleans.Runtime;
using Orleans.TestKit.Storage;

namespace Orleans.TestKit
{
    public static class StorageExtensions
    {
        public static TState State<TGrain, TState>(this TestKitSilo silo)
            where TGrain : Grain<TState>
            where TState : class, new()
        => silo.StorageManager.GetGrainStorage<TGrain, TState>().State;

        public static TestStorageStats StorageStats(this TestKitSilo silo)
        {
            if (silo == null)
            {
                throw new ArgumentNullException(nameof(silo));
            }

            return silo.StorageManager.GetStorage<TState>().State;
        }

        public static IStorage<T> AddGrainState<TGrain, T>(
            this TestKitSilo silo,
            T state = default)
            where TGrain : Grain<T>
            where T : new()
        {
            if (silo == null)
            {
                throw new ArgumentNullException(nameof(silo));
            }

            var storage = silo.StorageManager.GetGrainStorage<TGrain, T>();
            storage.State = state ?? new T();
            return storage;
        }

        public static IPersistentState<T> AddPersistentState<T>(
            this TestKitSilo silo,
            string stateName = default,
            string storageName = default,
            T state = default)
            where T : new()
        {
            if (silo == null)
            {
                throw new ArgumentNullException(nameof(silo));
            }

            var storage = silo.StorageManager.GetStorage<T>(stateName);

            return AddPersistentState(silo, storage, stateName, storageName, state);
        }

        public static IPersistentState<T> AddPersistentState<T>(
            this TestKitSilo silo,
            IStorage<T> storage,
            string stateName = default,
            string storageName = default,
            T state = default)
            where T : new()
        {
            if (silo == null)
            {
                throw new ArgumentNullException(nameof(silo));
            }

            if (string.IsNullOrWhiteSpace(stateName))
            {
                throw new ArgumentException("A state name must be provided", nameof(stateName));
            }

            if (storage is null)
            {
                throw new ArgumentNullException(nameof(storage));
            }

            silo.StorageManager.AddStorage(storage, stateName);
            return silo.StorageManager.stateAttributeFactoryMapper.AddPersistentState(storage, stateName, storageName, state);
        }
    }

    internal class PersistentStateFake<TState> : IPersistentState<TState>
    {
        private readonly IStorage<TState> _storage;

        public PersistentStateFake(IStorage<TState> storage)
        {
            _storage = storage;
        }

        public TState State
        {
            get => _storage.State;
            set => _storage.State = value;
        }

        public string Etag => _storage.Etag;

        public bool RecordExists => _storage.RecordExists;

        public Task ClearStateAsync() => _storage.ClearStateAsync();

        public Task ReadStateAsync() => _storage.ReadStateAsync();

        public Task WriteStateAsync() => _storage.WriteStateAsync();
    }
}
