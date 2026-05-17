using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace SustechAITeacher.Bots
{
    public class EchoBot : ActivityHandler
    {
        private static readonly HttpClient client = new HttpClient();

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(new Activity { Type = ActivityTypes.Typing }, cancellationToken);

            string userMessage = turnContext.Activity.Text;
            string aiResponse = await GetAIResponse(userMessage);

            await turnContext.SendActivityAsync(MessageFactory.Text(aiResponse, aiResponse), cancellationToken);
        }

        private async Task<string> GetAIResponse(string prompt)
        {
            // The ?.Trim() ensures no accidental spaces break your keys!
            string endpoint = Environment.GetEnvironmentVariable("AZURE_AI_ENDPOINT")?.Trim();
            string apiKey = Environment.GetEnvironmentVariable("AZURE_AI_API_KEY")?.Trim();
            string modelName = Environment.GetEnvironmentVariable("AZURE_AI_MODEL_NAME")?.Trim(); 

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                return "My AI brain is not connected yet! Please add my API keys in Azure Environment Variables.";
            }

            if (string.IsNullOrEmpty(modelName)) { modelName = "gpt-5.2-chat"; }

            try
            {
                if (!endpoint.EndsWith("/")) { endpoint += "/"; }
                string requestUrl = $"{endpoint}openai/deployments/{modelName}/chat/completions?api-version=2024-12-01-preview";

                var requestBody = new
                {
                    messages = new[]
                    {
                        new { role = "system", content = "You are the Sustech AI Teacher. You are an expert, patient, and knowledgeable educator. Answer questions clearly, encouragingly, and professionally." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1000,
                    temperature = 0.7
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                
                // Bulletproof way to send the API key
                using (var request = new HttpRequestMessage(HttpMethod.Post, requestUrl))
                {
                    request.Headers.Add("api-key", apiKey);
                    request.Content = content;

                    HttpResponseMessage response = await client.SendAsync(request);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        string responseString = await response.Content.ReadAsStringAsync();
                        dynamic jsonResponse = JsonConvert.DeserializeObject(responseString);
                        return jsonResponse.choices[0].message.content;
                    }
                    else
                    {
                        string errorResponse = await response.Content.ReadAsStringAsync();
                        return $"Error connecting to brain: {response.StatusCode} - {errorResponse}";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Oops, my system encountered an error: {ex.Message}";
            }
        }
    }
}
