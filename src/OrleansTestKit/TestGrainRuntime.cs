using System;
using Moq;
using Orleans.Core;
using Orleans.Runtime;
using Orleans.TestKit.Storage;
using Orleans.Timers;

namespace Orleans.TestKit
{
    internal sealed class TestGrainRuntime :
        IGrainRuntime
    {
        private readonly StorageManager _storageManager;

        public TestGrainRuntime(IGrainFactory grainFactory, ITimerRegistry timerRegistry, IReminderRegistry reminderRegistry, IServiceProvider serviceProvider, StorageManager storageManager)
        {
            GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
            TimerRegistry = timerRegistry ?? throw new ArgumentNullException(nameof(timerRegistry));
            ReminderRegistry = reminderRegistry ?? throw new ArgumentNullException(nameof(reminderRegistry));
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
        }

        public IGrainFactory GrainFactory { get; }

        public Mock<IGrainRuntime> Mock { get; } = new Mock<IGrainRuntime>();

        public IReminderRegistry ReminderRegistry { get; }

        public string ServiceId => "TestService";

        public IServiceProvider ServiceProvider { get; }

        public SiloAddress SiloAddress => SiloAddress.Zero;

        public string SiloIdentity => "TestSilo";

        public ITimerRegistry TimerRegistry { get; }

        public void DeactivateOnIdle(Grain grain)
        {
            if (grain == null)
            {
                throw new ArgumentNullException(nameof(grain));
            }

            Mock.Object.DeactivateOnIdle(grain);
        }

        public void DelayDeactivation(Grain grain, TimeSpan timeSpan)
        {
            if (grain == null)
            {
                throw new ArgumentNullException(nameof(grain));
            }

            Mock.Object.DelayDeactivation(grain, timeSpan);
        }

        public IStorage<TGrainState> GetStorage<TGrainState>(Grain grain) =>
            _storageManager.GetStorage<TGrainState>(grain.GetType().FullName);
    }
}
