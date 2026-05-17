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
            // Send a typing indicator so the user knows the AI is thinking
            await turnContext.SendActivityAsync(new Activity { Type = ActivityTypes.Typing }, cancellationToken);

            string userMessage = turnContext.Activity.Text;
            string aiResponse = await GetAIResponse(userMessage);

            await turnContext.SendActivityAsync(MessageFactory.Text(aiResponse, aiResponse), cancellationToken);
        }

        private async Task<string> GetAIResponse(string prompt)
        {
            // Pulling credentials safely from Azure Environment Variables
            string endpoint = Environment.GetEnvironmentVariable("AZURE_AI_ENDPOINT");
            string apiKey = Environment.GetEnvironmentVariable("AZURE_AI_API_KEY");
            string modelName = Environment.GetEnvironmentVariable("AZURE_AI_MODEL_NAME"); 

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                return "My AI brain is not connected yet! Please add my API keys in Azure Environment Variables.";
            }

            // Default to gpt-4o if no model name is specified
            if (string.IsNullOrEmpty(modelName)) { modelName = "gpt-4o"; }

            try
            {
                // Format the endpoint correctly for Azure AI Foundry
                string requestUrl = endpoint.EndsWith("/") ? $"{endpoint}chat/completions" : $"{endpoint}/chat/completions";
                requestUrl += "?api-version=2024-05-01-preview";

                var requestBody = new
                {
                    model = modelName,
                    messages = new[]
                    {
                        // THIS IS THE TEACHER PERSONA
                        new { role = "system", content = "You are the Sustech AI Teacher. You are an expert, patient, and knowledgeable educator. Answer questions clearly, encouragingly, and professionally." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1000,
                    temperature = 0.7
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("api-key", apiKey);

                HttpResponseMessage response = await client.PostAsync(requestUrl, content);
                
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
            catch (Exception ex)
            {
                return $"Oops, my system encountered an error: {ex.Message}";
            }
        }
    }
}
