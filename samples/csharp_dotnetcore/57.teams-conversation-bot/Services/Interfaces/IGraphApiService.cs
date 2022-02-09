using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace Microsoft.BotBuilderSamples.Services.Interfaces
{
    public interface IGraphApiService
    {
        Task<Calendar> GetUserCalendar(string token, string upn, CancellationToken cancellationToken);

        Task<Event> AddEventToCalendar(string token, string upn, Event eventInstance, CancellationToken cancellationToken);
    }
}
