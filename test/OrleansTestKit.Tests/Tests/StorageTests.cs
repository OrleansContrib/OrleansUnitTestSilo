using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans.Core;
using Orleans.TestKit.Storage;
using TestGrains;
using Xunit;

namespace Orleans.TestKit.Tests
{
    public class CustomStorage<TState> :
        IStorageStats,
        IStorage<TState>
    {
        public CustomStorage()
        {
            Stats = new TestStorageStats() { Reads = -1 };
            InitializeState();
        }

        public string Etag => throw new System.NotImplementedException();

        public virtual bool RecordExists { get; set; }

        public TState State { get; set; }

        public TestStorageStats Stats { get; }

        public Task ClearStateAsync()
        {
            InitializeState();
            Stats.Clears++;
            RecordExists = false;
            return Task.CompletedTask;
        }

        public Task ReadStateAsync()
        {
            Stats.Reads++;
            return Task.CompletedTask;
        }

        public Task WriteStateAsync()
        {
            Stats.Writes++;
            RecordExists = true;
            return Task.CompletedTask;
        }

        private void InitializeState()
        {
            if (!typeof(TState).IsValueType && typeof(TState).GetConstructor(Type.EmptyTypes) == null)
            {
                throw new NotSupportedException(
                    $"No parameterless constructor defined for {typeof(TState).Name}. This is currently not supported");
            }

            State = Activator.CreateInstance<TState>();
        }
    }

    public class StorageTests : TestKitBase
    {
        /// <summary>
        ///     This test demonstrates how to assert changes occurred to the state object using the instance returned by
        ///     the <see cref="StorageExtensions.State{TState}(TestKitSilo)"/> extension method.
        /// </summary>
        [Fact]
        public async Task GreetingArchiveGrain_AddGreeting_StateChanged()
        {
            // Arrange
            const long id = 3000;
            const string greeting1 = "Bonjour";
            const string greeting2 = "Hei";

            var grain = await this.Silo.CreateGrainAsync<GreetingArchiveGrain>(id);

            // Act
            await grain.AddGreeting(greeting1);
            await grain.AddGreeting(greeting2);

            var greetings = (await grain.GetGreetings()).ToList();

            // Assert
            var state = this.Silo.State<GreetingArchiveGrain, GreetingArchiveGrainState>();
            state.Greetings.Should().Equal(greeting1, greeting2);
            greetings.Should().Equal(greeting1, greeting2);
            Assert.True(this.Silo.StorageManager.GetGrainStorage<GreetingArchiveGrain, GreetingArchiveGrainState>().RecordExists);
        }

        /// <summary>
        ///     This test demonstrates how to assert a `WriteStateAsync` call occurred using the counters returned by
        ///     the <see cref="StorageExtensions.StorageStats(TestKitSilo)"/> extension method.
        /// </summary>
        [Fact]
        public async Task GreetingArchiveGrain_AddGreeting_StatsChanged()
        {
            // Arrange
            const long id = 4000;
            const string greeting1 = "Salve";
            const string greeting2 = "Konnichiwa";

            var grain = await this.Silo.CreateGrainAsync<GreetingArchiveGrain>(id);

            // Act
            await grain.AddGreeting(greeting1);
            await grain.AddGreeting(greeting2);

            var greetings = (await grain.GetGreetings()).ToList();

            // Assert
            var stats = this.Silo.StorageStats();
            stats.Clears.Should().Be(0);
            stats.Reads.Should().Be(0);
            stats.Writes.Should().Be(2);

            greetings.Should().Equal(greeting1, greeting2);
            Assert.True(this.Silo.StorageManager.GetGrainStorage<GreetingArchiveGrain, GreetingArchiveGrainState>().RecordExists);
        }

        /// <summary>
        ///     This test demonstrates how to customize the state object before the grain is initialized using the
        ///     instance returned by the <see cref="StorageExtensions.State{TState}(TestKitSilo)"/> extension method.
        /// </summary>
        [Fact]
        public async Task GreetingArchiveGrain_GetGreetings_CustomInitialState()
        {
            // Arrange
            const long id = 2000;
            const string greeting = "Hola";

            var state = this.Silo.State<GreetingArchiveGrain, GreetingArchiveGrainState>();
            state.Greetings.Add(greeting);

            var grain = await this.Silo.CreateGrainAsync<GreetingArchiveGrain>(id);

            // Act
            var greetings = (await grain.GetGreetings()).ToList();

            // Assert
            greetings.Should().Equal(greeting);
        }

        /// <summary>This test demonstrates how the grain is initialized with a default state object.</summary>
        [Fact]
        public async Task GreetingArchiveGrain_GetGreetings_DefaultInitialState()
        {
            // Arrange
            const long id = 1000;

            var grain = await this.Silo.CreateGrainAsync<GreetingArchiveGrain>(id);

            // Act
            var greetings = (await grain.GetGreetings()).ToList();

            // Assert
            greetings.Should().BeEmpty();
        }

        /// <summary>This test demonstrates how to use storage factory.</summary>
        [Fact]
        public async Task GreetingArchiveGrain_GetGreetings_WithCustomStateFactory()
        {
            // Arrange
            this.Silo.Options.StorageFactory = type => Activator.CreateInstance(typeof(CustomStorage<>).MakeGenericType(type));

            const long id = 1000;

            var grain = await this.Silo.CreateGrainAsync<GreetingArchiveGrain>(id);

            // Act
            var greetings = (await grain.GetGreetings()).ToList();

            // Assert
            greetings.Should().BeEmpty();
        }

        /// <summary>
        ///     This test demonstrates how to assert changes occurred to the state object using the instance returned by
        ///     the <see cref="StorageExtensions.State{TState}(TestKitSilo)"/> extension method.
        /// </summary>
        [Fact]
        public async Task GreetingArchiveGrain_ResetGreetings_StateChanged()
        {
            // Arrange
            const long id = 5000;
            const string greeting = "Olá";

            var state = this.Silo.State<GreetingArchiveGrain, GreetingArchiveGrainState>();
            state.Greetings.Add(greeting);

            var grain = await this.Silo.CreateGrainAsync<GreetingArchiveGrain>(id);

            // Act
            await grain.ResetGreetings();

            var greetings = (await grain.GetGreetings()).ToList();

            // Assert
            state = this.Silo.State<GreetingArchiveGrain, GreetingArchiveGrainState>();
            state.Greetings.Should().BeEmpty();

            greetings.Should().BeEmpty();
            Assert.False(this.Silo.StorageManager.GetGrainStorage<GreetingArchiveGrain, GreetingArchiveGrainState>().RecordExists);
        }

        /// <summary>
        ///     This test demonstrates how to assert a `ClearStateAsync` call occurred using the counters returned by
        ///     the <see cref="StorageExtensions.StorageStats(TestKitSilo)"/> extension method.
        /// </summary>
        [Fact]
        public async Task GreetingArchiveGrain_ResetGreetings_StatsChanged()
        {
            // Arrange
            const long id = 6000;
            const string greeting = "Hallo";

            var state = this.Silo.State<GreetingArchiveGrain, GreetingArchiveGrainState>();
            state.Greetings.Add(greeting);

            var grain = await this.Silo.CreateGrainAsync<GreetingArchiveGrain>(id);

            // Act
            await grain.ResetGreetings();

            var greetings = (await grain.GetGreetings()).ToList();

            // Assert
            var stats = this.Silo.StorageStats();
            stats.Clears.Should().Be(1);
            stats.Reads.Should().Be(0);
            stats.Writes.Should().Be(0);

            greetings.Should().BeEmpty();
            Assert.False(this.Silo.StorageManager.GetGrainStorage<GreetingArchiveGrain, GreetingArchiveGrainState>().RecordExists);
        }

        /// <summary>This test demonstrates how to use the RecordExists flag</summary>
        [Fact]
        public async Task RecordExistsFlagTest()
        {
            var manager = new StorageManager(new TestKitOptions());
            var state = manager.GetGrainStorage<GreetingArchiveGrain, GreetingArchiveGrainState>();

            // be default, RecordExists is false
            Assert.False(state.RecordExists);

            // Write and check that RecordExists is true
            await state.WriteStateAsync();
            Assert.True(state.RecordExists);

            // Clear and check that RecordExists is false
            await state.ClearStateAsync();
            Assert.False(state.RecordExists);
        }
    }
}
