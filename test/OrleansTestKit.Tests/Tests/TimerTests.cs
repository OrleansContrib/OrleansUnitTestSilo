using System.Threading.Tasks;
using FluentAssertions;
using Orleans.TestKit.Storage;
using Orleans.TestKit.Timers;
using TestGrains;
using Xunit;

namespace Orleans.TestKit.Tests
{
    public class TimerTests : TestKitBase
    {
        [Fact]
        public async Task ShouldFireAllTimersAsync()
        {
            // Arrange
            var grain = await Silo.CreateGrainAsync<HelloTimers>(0);

            // Act
            await Silo.FireAllTimersAsync();

            // Assert
            var state = Silo.State<HelloTimers, HelloTimersState>();
            state.Timer0Fired.Should().BeTrue();
            state.Timer1Fired.Should().BeTrue();
        }



        [Fact]
        public async Task ShouldFireFirstTimerAsync()
        {
            // Arrange
            var grain = await Silo.CreateGrainAsync<HelloTimers>(0);

            // Act
            await Silo.FireTimerAsync(0);

            // Assert
            var state = Silo.State<HelloTimers, HelloTimersState>();
            state.Timer0Fired.Should().BeTrue();
            state.Timer1Fired.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldFireSecondTimerAsync()
        {
            // Arrange
            var grain = await Silo.CreateGrainAsync<HelloTimers>(0);

            // Act
            await Silo.FireTimerAsync(1);

            // Assert
            var state = Silo.State<HelloTimers, HelloTimersState>();
            state.Timer0Fired.Should().BeFalse();
            state.Timer1Fired.Should().BeTrue();
        }
    }
}
