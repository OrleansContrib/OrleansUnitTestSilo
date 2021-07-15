using System.Threading.Tasks;
using Orleans;

namespace TestInterfaces
{
    public interface IColorRankingGrain : IGrainWithGuidKey
    {
        Task<Color> GetFavouriteColor();
        Task<Color> GetLeastFavouriteColor();
        Task SetFavouriteColor(Color color);
        Task SetLeastFavouriteColor(Color color);
    }
}
