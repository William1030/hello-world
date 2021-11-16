using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Configuration;
using Polly;

namespace OnlineNotify
{
    class HttpRequest
    {
        static double APITimeOutSec = double.Parse(ConfigurationManager.AppSettings.Get("APITimeOutSec"));
        static readonly HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(APITimeOutSec) };

        public static async Task<string> PostAsyncJson(string url, string body)
        {
            try
            {
                return await Policy.Handle<TaskCanceledException>().WaitAndRetryAsync(retryCount: 3, sleepDurationProvider: i => TimeSpan.FromMilliseconds(300)).ExecuteAsync(async () =>
                {
                    HttpContent content = new StringContent(body);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    var response = await client.PostAsync(url, content);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                });
            }
            catch (Exception ex)
            {
                //LogUtil.LogError(String.Concat("PostAsyncJson Function Exception: ", ex.ToString()));
                return "";
                throw;
            }

        }
    }
}
