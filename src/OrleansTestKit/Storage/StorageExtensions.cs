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
        public static TState State<TState>(this TestKitSilo silo) where TState : class, new()
        {
            if (silo == null)
            {
                throw new ArgumentNullException(nameof(silo));
            }

            return silo.StorageManager.GetStorage<TState>().State;
        }

        public static TestStorageStats StorageStats(this TestKitSilo silo)
        {
            if (silo == null)
            {
                throw new ArgumentNullException(nameof(silo));
            }

            return silo.StorageManager.GetStorageStats();
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

            silo.StorageManager.AddStorage(storage, stateName);
            var stateMock = new PersistentStateFake<T>(storage)
            {
                State = state ?? new T()
            };

            var mockMapper = silo.StorageManager.stateAttributeFactoryMapperMock;

            if (string.IsNullOrWhiteSpace(stateName))
            {
                mockMapper.Setup(o => o.GetFactory(It.IsAny<ParameterInfo>(), It.IsAny<PersistentStateAttribute>())).Returns(_ => stateMock);
            }
            else if (string.IsNullOrWhiteSpace(storageName))
            {
                mockMapper
                    .Setup(o => o.GetFactory(It.IsAny<ParameterInfo>(), It.Is<PersistentStateAttribute>(attr => attr.StateName.Equals(stateName, StringComparison.OrdinalIgnoreCase))))
                    .Returns(_ => stateMock);
            }
            else
            {
                mockMapper
                    .Setup(o => o.GetFactory(It.IsAny<ParameterInfo>(), It.Is<PersistentStateAttribute>(attr =>
                        attr.StateName.Equals(stateName, StringComparison.OrdinalIgnoreCase) && (attr.StorageName == null || attr.StorageName.Equals(storageName, StringComparison.OrdinalIgnoreCase)))))
                    .Returns(_ => stateMock);
            }

            return stateMock;
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
