using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Storage.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace gc_storage_reader
{
    public class HttpResponseDetails
    {
        public string ResponseData { get; set; }
        public long DurationMS { get; set; }
        public HttpStatusCode Status { get; set; }
        public string LocationHeader { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Task t = new Task(StartGcReader);
            t.Start();

            Console.WriteLine("doing requests...");
            Console.ReadLine();
        }

        static async void StartGcReader()
        {
            try
            {
                
                var token = await GetGoogleOAuthTokenAsync();

                //read the bucket
                var url = "https://www.googleapis.com/storage/v1/b/mq-pls-dev-test-bucket?project=mq-media-cms-dev";
                var res = await DoGetRequestAsync(url, token);

                //write to the bucket
                url = "https://www.googleapis.com/upload/storage/v1/b/mq-pls-dev-test-bucket/o?uploadType=media&name=objectOne";

                res = await DoPostRequestAsync(url, "this is my contents", token);

            }
            catch(Exception ex)
            {
                
            }





        }

        static async Task<HttpResponseDetails> DoGetRequestAsync(string url, string oauthToken, bool includeAcceptCsv = false)
        {
            using (HttpClient client = new HttpClient())
            {
                //Console.WriteLine("GET {0}", url);
                //add the token as HTTP Authorization header
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + oauthToken);
                //client.DefaultRequestHeaders.Add("Accept", "application/json");
                //client.DefaultRequestHeaders.Add("Accept", "text/html");
                if (includeAcceptCsv)
                {
                    //client.DefaultRequestHeaders.Add("Accept", "text/html");
                    var d = client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "text/csv");
                }


                var start = DateTime.Now;
                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    var end = DateTime.Now;
                    using (HttpContent content = response.Content)
                    {
                        // ... Read the string.
                        string result = await content.ReadAsStringAsync();
                        return new HttpResponseDetails() { ResponseData = result, DurationMS = (long)((end - start).TotalMilliseconds), Status = response.StatusCode };
                    }
                }
            }
        }

        static async Task<HttpResponseDetails> DoPostRequestAsync(string url, string postBody, string oauthToken)
        {
            using (HttpClient client = new HttpClient())
            {
                var postContent = new StringContent(postBody, Encoding.UTF8, "text/html");

                //add the token as HTTP Authorization header
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + oauthToken);

                var start = DateTime.Now;
                using (HttpResponseMessage response = await client.PostAsync(url, postContent))
                {
                    var end = DateTime.Now;
                    using (HttpContent content = response.Content)
                    {
                        // ... Read the string.
                        string result = await content.ReadAsStringAsync();

                        //get location headerif it exists
                        HttpHeaders headers = response.Headers;
                        IEnumerable<string> values;
                        var location = "";
                        if (headers.TryGetValues("Location", out values))
                        {
                            foreach (var val in values)
                            {
                                location = val;
                            }
                        }
                        return new HttpResponseDetails() { ResponseData = result, DurationMS = (long)((end - start).TotalMilliseconds), Status = response.StatusCode, LocationHeader = location };
                    }
                }
            }
        }

        static async Task<string> GetGoogleOAuthTokenAsync()
        {
            //setup ouath
            
            //string serviceAccountEmail = "852139745084-0j93espauh965nl4rf9fq2uh4floi2md@developer.gserviceaccount.com"; //pls dev service account - 1
            string serviceAccountEmail = "service-oauth@mq-media-cms-dev.iam.gserviceaccount.com"; //service-oauth


            string O_AUTH_EMAIL_SCOPE = "https://www.googleapis.com/auth/devstorage.read_write";

            var certificate = new X509Certificate2(@"key.p12", "notasecret", X509KeyStorageFlags.Exportable);

            ServiceAccountCredential credential = new ServiceAccountCredential(
               new ServiceAccountCredential.Initializer(serviceAccountEmail)
               {
                   Scopes = new[] { O_AUTH_EMAIL_SCOPE }
               }.FromCertificate(certificate));



            //var done = await credential.RequestAccessTokenAsync(new System.Threading.CancellationToken());
            return await credential.GetAccessTokenForRequestAsync();


        }
    }
}
