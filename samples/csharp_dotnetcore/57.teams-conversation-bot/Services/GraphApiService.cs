using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards.Templating;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.BotBuilderSamples.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using static Microsoft.BotBuilderSamples.Controllers.SalesforceController;

namespace Microsoft.BotBuilderSamples.Services
{
    public class GraphApiService : IGraphApiService
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly ILogger<GraphApiService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _absenceCardTemplate = Path.Combine(".", "Resources", "AbsenceCardTemplate.json");
        public GraphApiService(
            IBotFrameworkHttpAdapter adapter,
            ILogger<GraphApiService> logger,
            IConfiguration configuration)
        {
            _adapter = adapter;
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


        public async Task<string> NotifyUserInChat(string token, string upn, AbsenceObject absence, CancellationToken cancellationToken)
        {
            try
            {
                var conversationId = await GetProactiveChatIdForUser(token, upn, cancellationToken);

                var credentials = new MicrosoftAppCredentials(_configuration["MicrosoftAppId"], _configuration["MicrosoftAppPassword"]);

                var connectorClient = new ConnectorClient(new Uri(_configuration["ServiceUrl"]), credentials);

                var members = await connectorClient.Conversations.GetConversationMembersAsync(conversationId);

                var conversationParameters = new ConversationParameters
                {
                    IsGroup = false,
                    Bot = new ChannelAccount
                    {
                        Id = "28:" + credentials.MicrosoftAppId,
                        Name = "This is your bot!"
                    },
                    Members = new ChannelAccount[] { members[0] },
                    TenantId = _configuration["MicrosoftAppTenantId"]
                };
                //string msg = "well hello!";
                await ((CloudAdapter)_adapter).CreateConversationAsync(credentials.MicrosoftAppId, null, _configuration["ServiceUrl"], credentials.OAuthScope, conversationParameters, async (t1, c1) =>
                {
                    var conversationReference = t1.Activity.GetConversationReference();
                    await ((CloudAdapter)_adapter).ContinueConversationAsync(credentials.MicrosoftAppId, conversationReference,
                    async(turnContext, cancelToken) =>
                    {
                        //await turnContext.SendActivityAsync("proactive hello " + msg);
                        var templateJSON = System.IO.File.ReadAllText(_absenceCardTemplate);
                        AdaptiveCardTemplate template = new AdaptiveCardTemplate(templateJSON);
                        var memberData = new
                        {
                            title = "has booked an absence",
                            description = "An absence requires your approval",
                            createdUtc = "2017-02-14T06:08:39Z",
                            viewUrl = "https://adaptivecards.io",
                            absence = new
                            {
                                teamMemberName = absence.TeamMemberName,// "Jay Briers",
                                teamMemberImg = "https://pbs.twimg.com/profile_images/3647943215/d7f12830b3c17a5a9e4afcc370e3a37e_400x400.jpeg",
                                start = "2022-02-14T06:08:39Z",
                                end = "2022-02-15T06:08:39Z",
                                duration = "1 Day",
                                reason = "Sickness"
                            }
                        };
                        string cardJSON = template.Expand(memberData);
                        var adaptiveCardAttachment = new Bot.Schema.Attachment
                        {
                            ContentType = "application/vnd.microsoft.card.adaptive",
                            Content = JsonConvert.DeserializeObject(cardJSON),
                        };
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(adaptiveCardAttachment), cancellationToken);







                    }, cancellationToken);
                }, cancellationToken);

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

            return "done";
        }

        private async Task<string> GetProactiveChatIdForUser(string token, string upn, CancellationToken cancellationToken)
        {
            var graphClient = GetGraphServiceClient(token);

            var installedApps = await graphClient.Users[upn].Teamwork.InstalledApps
                .Request()
                .Filter($"teamsApp/externalId eq '{_configuration["TeamsAppId"]}'")
                .Expand("teamsApp")
                .GetAsync(cancellationToken);

            var app = installedApps.FirstOrDefault();
            if (app == null)
            {
                return null;
            }

            var chat = await graphClient.Users[upn].Teamwork.InstalledApps[app.Id].Chat
                .Request()
                .GetAsync(cancellationToken);

            return chat.Id;
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
