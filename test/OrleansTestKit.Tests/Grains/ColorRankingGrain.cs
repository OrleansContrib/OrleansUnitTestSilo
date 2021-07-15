using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using TestInterfaces;

namespace TestGrains
{
    public class ColorRankingGrain : Grain<ColorGrainState>, IColorRankingGrain
    {
        /// <summary>The persisted state.</summary>
        private readonly IPersistentState<ColorGrainState> state;

        public ColorRankingGrain(
            [PersistentState("Default")]
            IPersistentState<ColorGrainState> state)
        {
            this.state = state;
        }

        public Task<Color> GetFavouriteColor() => Task.FromResult(state.State.Color);

        public Task<Color> GetLeastFavouriteColor() => Task.FromResult(State.Color);

        /// <summary>Asynchronously sets the favourite color.</summary>
        /// <param name="color">The new color.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        ///     If <paramref name="color"/> is <see cref="Color.Unknown"/> or an invalid value.
        /// </exception>
        public async Task SetFavouriteColor(Color color)
        {
            SetColor(color, state.State);
            await state.WriteStateAsync();
        }

        /// <summary>Asynchronously sets the least favourite color.</summary>
        /// <param name="color">The new color.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        ///     If <paramref name="color"/> is <see cref="Color.Unknown"/> or an invalid value.
        /// </exception>
        public async Task SetLeastFavouriteColor(Color color)
        {
            SetColor(color, State);
            await WriteStateAsync();
        }

        private void SetColor(Color color, ColorGrainState state)
        {
            if (!Enum.IsDefined(typeof(Color), color))
            {
                throw new ArgumentException("The color is invalid.", nameof(color));
            }

            if (color == Color.Unknown)
            {
                throw new ArgumentException("The color must not be \"unknown\".", nameof(color));
            }

            if (this.state.State.Initialized)
            {
                if (color == state.Color)
                {
                    return;
                }
            }
            else
            {
                state.Id = this.GetPrimaryKey();
            }

            state.Color = color;
        }
    }
}
