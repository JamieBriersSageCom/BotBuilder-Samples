namespace Microsoft.BotBuilderSamples.Models
{
    public class AppSettings
    {
        public string MicrosoftAppType { get; set; }
        public string MicrosoftAppId { get; set; }
        public string MicrosoftAppPassword { get; set; }
        public string MicrosoftAppTenantId { get; set; }

        public SalesforceSettings Salesforce { get; set; }

        public class SalesforceSettings
        {
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }

        }
    }
}
