using System;
using System.Collections.Generic;
using System.Reflection;
using Orleans.Core;
using Orleans.Runtime;

namespace Orleans.TestKit.Storage
{
    public sealed class TestPersistentStateAttributeToFactoryMapper : IAttributeToFactoryMapper<PersistentStateAttribute>
    {
        private readonly StorageManager storageManager;
        private readonly Dictionary<Type, Dictionary<(string StateName, string StorageName), object>>
            registeredStorage = new();
        private readonly MethodInfo AddEmptyStateMethod = typeof(TestPersistentStateAttributeToFactoryMapper)
            .GetMethod(nameof(TestPersistentStateAttributeToFactoryMapper.AddEmptyState), BindingFlags.Instance | BindingFlags.NonPublic);

        public TestPersistentStateAttributeToFactoryMapper(StorageManager storageManager)
        {
            this.storageManager = storageManager;
        }

        public IPersistentState<T> AddPersistentState<T>(
            IStorage<T> storage,
            string stateName,
            string storageName = null,
            T state = default) where T : new()
        {
            var dict = registeredStorage.TryGetValue(typeof(T), out var typeStateRegistry)
                ? typeStateRegistry
                : registeredStorage[typeof(T)] = new Dictionary<(string StateName, string StorageName), object>(1);

            var fake = new PersistentStateFake<T>(storage)
            {
                State = state ?? new T()
            };
            dict[(stateName, storageName)] = fake;

            return fake;
        }

        public Factory<IGrainActivationContext, object> GetFactory(ParameterInfo parameter, PersistentStateAttribute metadata)
        {
            if (parameter is null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            if (metadata is null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (parameter.ParameterType.IsGenericType &&
                parameter.ParameterType.GetGenericTypeDefinition() != typeof(IPersistentState<>))
            {
                throw new InvalidOperationException($"No persistent state for the parameter '{parameter.Name}'");
            }

            var parameterType = parameter.ParameterType.GenericTypeArguments[0];

            if (registeredStorage.TryGetValue(parameterType, out var typeStateRegistry))
            {
                // If a storage name is not provided then find it by the state name
                if (metadata.StorageName is null)
                {
                    foreach (var kvp in typeStateRegistry)
                    {
                        if (kvp.Key.StateName == metadata.StateName)
                        {
                            return _ => kvp.Value;
                        }
                    }
                }
                // If we must have the state and the storage name so lookup by both
                else if (typeStateRegistry.TryGetValue((metadata.StateName, metadata.StorageName), out var persistentState))
                {
                    return _ => persistentState;
                }
            }

            var state = AddEmptyStateMethod.MakeGenericMethod(parameterType).Invoke(
                this,
                new[] { metadata.StateName, metadata.StorageName });

            return _ => state;
        }

        private IPersistentState<TState> AddEmptyState<TState>(string stateName, string storageName)
            where TState : new()
        {
            var storage = storageManager.GetStorage<TState>(stateName);
            return AddPersistentState(storage, stateName, storageName);
        }
    }
}
