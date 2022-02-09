// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.BotBuilderSamples.Bots;
using Microsoft.Extensions.Hosting;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.BotBuilderSamples.Services.Interfaces;
using Microsoft.BotBuilderSamples.Services;
using Microsoft.BotBuilderSamples.Models;
using NetCoreForce.Client;

namespace Microsoft.BotBuilderSamples
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppSettings>(Configuration);
            var appSettings = Configuration.Get<AppSettings>();

            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(Configuration, "AzureAd")
                .EnableTokenAcquisitionToCallDownstreamApi(new string[] { "Calendar.ReadWrite" })
                .AddMicrosoftGraph(Configuration.GetSection("GraphBeta"))
                .AddInMemoryTokenCaches();

            services.AddHttpClient().AddControllers().AddNewtonsoftJson();

            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the Bot Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, TeamsConversationBot>();

            services.AddTransient<IGraphApiService, GraphApiService>();

            var salesforceAuth = new AuthenticationClient();
            salesforceAuth.UsernamePassword(appSettings.Salesforce.ClientId, appSettings.Salesforce.ClientSecret, appSettings.Salesforce.Username, appSettings.Salesforce.Password, "https://test.salesforce.com/services/oauth2/token");
            var client = new ForceClient(salesforceAuth.AccessInfo.InstanceUrl, salesforceAuth.ApiVersion, salesforceAuth.AccessInfo.AccessToken);
            services.AddSingleton(client);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}
