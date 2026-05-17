using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SustechAITeacher
{
    public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
    {
        public AdapterWithErrorHandler(IConfiguration configuration, ILogger<BotFrameworkHttpAdapter> logger)
            : base(configuration, logger)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                // কোনো এরর হলে বট এই মেসেজটি দেবে
                await turnContext.SendActivityAsync("Sorry, it looks like something went wrong in the bot's code.");
            };
        }
    }
}
