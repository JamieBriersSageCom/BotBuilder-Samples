using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using static Microsoft.BotBuilderSamples.Controllers.SalesforceController;

namespace Microsoft.BotBuilderSamples.Services.Interfaces
{
    public interface IGraphApiService
    {
        Task<Calendar> GetUserCalendar(string token, string upn, CancellationToken cancellationToken);

        Task<Event> AddEventToCalendar(string token, string upn, Event eventInstance, CancellationToken cancellationToken);

        Task<string> NotifyUserInChat(string token, string upn, AbsenceObject absence, CancellationToken cancellationToken);
    }
}
