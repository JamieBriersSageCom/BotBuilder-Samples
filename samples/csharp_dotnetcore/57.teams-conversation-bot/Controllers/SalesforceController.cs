using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Graph;
using System;
using Microsoft.BotBuilderSamples.Services.Interfaces;
using NetCoreForce.Client;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples.Controllers
{
    [ApiController]
    public class SalesforceController : ControllerBase
    {

        private readonly ITokenAcquisition tokenAcquisition;
        private readonly IGraphApiService graphApiService;

        private readonly ForceClient forceClient;
        public SalesforceController(ITokenAcquisition tokenAcquisition, IGraphApiService graphApiService, ForceClient forceClient)
        {
            this.tokenAcquisition = tokenAcquisition;
            this.graphApiService = graphApiService;
            this.forceClient = forceClient;
        }

        [HttpPost]
        [Route("api/salesforce/absence")]
        public async Task<ActionResult> AbsenceEvent([FromBody] AbsenceRequest absenceEvent, CancellationToken cancellationToken)
        {
            var absence = await forceClient.GetObjectById<AbsenceObject>("fHCM2__Absence__c", absenceEvent.AbsenceId);
            var newEvent = new Event
            {
                Subject = absence.AbsenceName + " For " + absence.TeamMemberName,
                ShowAs = FreeBusyStatus.Oof,
                IsAllDay = true,
                Start = DateTimeTimeZone.FromDateTime(DateTime.Parse(absence.Start), "Europe/London"),
                End = DateTimeTimeZone.FromDateTime(DateTime.Parse(absence.End).AddDays(1), "Europe/London"),
            };

            var accessToken = await tokenAcquisition.GetAccessTokenForAppAsync("https://graph.microsoft.com/.default");
            var eventResponse = await graphApiService.AddEventToCalendar(accessToken, absenceEvent.CalendarId, newEvent, cancellationToken);
            return Ok(eventResponse);
        }

        public class AbsenceRequest
        {
            public string AbsenceId { get; set; }
            public string CalendarId { get; set; }
        }

        public class AbsenceObject
        {
            [JsonProperty("Id")]
            public string Id { get; set; }

            [JsonProperty("fHCM2__Start_Date__c")]
            public string Start { get; set; }

            [JsonProperty("fHCM2__End_Date__c")]
            public string End { get; set; }

            [JsonProperty("fHCM2__Type__c")]
            public string AbsenceName { get; set; }

            [JsonProperty("fHCM2__Team_Member_Name__c")]
            public string TeamMemberName { get; set; }

        }
    }
}
