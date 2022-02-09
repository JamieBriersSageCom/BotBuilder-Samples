using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.BotBuilderSamples.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;


namespace Microsoft.BotBuilderSamples.Services
{
    public class GraphApiService : IGraphApiService
    {
        private readonly ILogger<GraphApiService> _logger;
        private readonly IConfiguration _configuration;

        public GraphApiService(
            ILogger<GraphApiService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<Calendar> GetUserCalendar(string token, string upn, CancellationToken cancellationToken)
        {
            var graphClient = GetGraphServiceClient(token);

            var calendar = await graphClient.Users[upn].Calendar.Request().GetAsync(cancellationToken);

            return calendar;
        }

        public async Task<Event> AddEventToCalendar(string token, string upn, Event eventInstance, CancellationToken cancellationToken)
        {
            var graphClient = GetGraphServiceClient(token);

            return await graphClient.Users[upn].Calendar.Events.Request().AddAsync(eventInstance, cancellationToken);
        }

        private GraphServiceClient GetGraphServiceClient(string token) => new GraphServiceClient(
               new DelegateAuthenticationProvider(
           requestMessage =>
           {
               requestMessage.Headers.Authorization = new AuthenticationHeaderValue(CoreConstants.Headers.Bearer, token);
               return Task.CompletedTask;
           }));

        private async Task<string> GetTokenForApp(string token, string tenantId)
        {
            var builder = ConfidentialClientApplicationBuilder.Create(_configuration["MicrosoftAppId"])
                .WithClientSecret(_configuration["MicrosoftAppPassword"])
                .WithTenantId(tenantId)
                .WithRedirectUri("msal" + _configuration["MicrosoftAppId"] + "://auth");

            var client = builder.Build();

            var tokenBuilder = client.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" });

            var result = await tokenBuilder.ExecuteAsync();

            return result.AccessToken;
        }
    }
}
