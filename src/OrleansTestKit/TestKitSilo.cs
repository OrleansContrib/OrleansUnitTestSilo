using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using Orleans.Core;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans.TestKit.Reminders;
using Orleans.TestKit.Services;
using Orleans.TestKit.Storage;
using Orleans.TestKit.Streams;
using Orleans.TestKit.Timers;

namespace Orleans.TestKit
{
    public sealed class TestKitSilo
    {
        /// <summary>
        /// Flag indicating if a grain has already been created in this test silo.
        /// Since this is all mocked up only the grain under test should be real, therefore
        /// only a single grain should ever be created.
        /// </summary>
        private bool _isGrainCreated;

        private readonly TestGrainCreator _grainCreator;

        private readonly TestGrainRuntime _grainRuntime;

        private readonly TestGrainLifecycle _grainLifecycle = new TestGrainLifecycle();

        public TestKitSilo()
        {
            GrainFactory = new TestGrainFactory(Options);
            ServiceProvider = new TestServiceProvider(Options);
            StorageManager = new StorageManager(Options);
            TimerRegistry = new TestTimerRegistry();
            ReminderRegistry = new TestReminderRegistry();
            StreamProviderManager = new TestStreamProviderManager(Options);
            ServiceProvider.AddService<IKeyedServiceCollection<string, IStreamProvider>>(StreamProviderManager);
            _grainRuntime = new TestGrainRuntime(GrainFactory, TimerRegistry, ReminderRegistry, ServiceProvider, StorageManager);
            _grainCreator = new TestGrainCreator(_grainRuntime, ServiceProvider);
        }

        /// <summary>
        /// Silo service provider used when creating new grain instances.
        /// </summary>
        /// <returns></returns>
        public TestServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Silo grain factory used by the test grain with creating other grains
        /// This should only be used by the grain, not any test code.
        /// </summary>
        public TestGrainFactory GrainFactory { get; }

        /// <summary>
        /// Manages all test silo timers.
        /// </summary>
        public TestTimerRegistry TimerRegistry { get; }

        /// <summary>
        /// Manages all test silo reminders
        /// </summary>
        /// <returns></returns>
        public TestReminderRegistry ReminderRegistry { get; }

        /// <summary>
        /// Manages all test silo streams
        /// </summary>
        /// <returns></returns>
        public TestStreamProviderManager StreamProviderManager { get; }

        /// <summary>
        /// Manages all test silo storage
        /// </summary>
        /// <returns></returns>
        public StorageManager StorageManager { get; }

        /// <summary>
        /// Configures the test silo
        /// </summary>
        /// <returns></returns>
        public TestKitOptions Options { get; } = new TestKitOptions();

        public Task<T> CreateGrainAsync<T>(long id)
            where T : Grain, IGrainWithIntegerKey =>
            CreateGrainAsync<T>(new TestGrainIdentity(id));

        public Task<T> CreateGrainAsync<T>(Guid id)
            where T : Grain, IGrainWithGuidKey =>
            CreateGrainAsync<T>(new TestGrainIdentity(id));

        public Task<T> CreateGrainAsync<T>(string id)
            where T : Grain, IGrainWithStringKey =>
            CreateGrainAsync<T>(new TestGrainIdentity(id));

        public Task<T> CreateGrainAsync<T>(Guid id, string keyExtension)
            where T : Grain, IGrainWithGuidCompoundKey =>
            CreateGrainAsync<T>(new TestGrainIdentity(id, keyExtension));

        public Task<T> CreateGrainAsync<T>(long id, string keyExtension)
            where T : Grain, IGrainWithIntegerCompoundKey =>
            CreateGrainAsync<T>(new TestGrainIdentity(id, keyExtension));

        private async Task<T> CreateGrainAsync<T>(IGrainIdentity identity)
            where T : Grain
        {
            if (_isGrainCreated)
            {
                throw new Exception("A grain has already been created in this silo. Only 1 grain per test silo should ever be created. Add grain probes for supporting grains.");
            }

            // Add state attribute mapping for storage facets
            this.AddService<IAttributeToFactoryMapper<PersistentStateAttribute>>(StorageManager.stateAttributeFactoryMapper);

            _isGrainCreated = true;
            Grain grain;
            var grainContext = new TestGrainActivationContext
            {
                ActivationServices = ServiceProvider,
                GrainIdentity = identity,
                GrainType = typeof(T),
                ObservableLifecycle = _grainLifecycle,
            };

            //Create a stateless grain
            grain = _grainCreator.CreateGrainInstance(grainContext) as T;
            if (grain == null)
            {
                throw new Exception($"Unable to instantiate grain {typeof(T)} properly");
            }

            //Check if there are any reminders for this grain and set the reminder target
            if (grain is IRemindable remindable)
            {
                ReminderRegistry.SetGrainTarget(remindable);
            }

            //Trigger the lifecycle hook that will get the grain's state from the runtime
            await _grainLifecycle.TriggerStartAsync().ConfigureAwait(false);
            return grain as T;
        }

        public void VerifyRuntime(Expression<Action<IGrainRuntime>> expression, Func<Times> times)
        {
            _grainRuntime.Mock.Verify(expression, times);
        }

        /// <summary>
        /// Deactivate the given <see cref="Grain"/>
        /// </summary>
        /// <param name="grain">Grain to Deactivate</param>
        public Task DeactivateAsync(Grain grain)
        {
            if (grain == null)
            {
                throw new ArgumentNullException(nameof(grain));
            }

            return _grainLifecycle.TriggerStopAsync();
        }
    }
}
