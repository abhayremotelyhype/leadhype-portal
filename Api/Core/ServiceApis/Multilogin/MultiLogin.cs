using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using UtilityPack;
using static LeadHype.Api.DI;
using static UtilityPack.Tasks;

namespace LeadHype.Api.ServiceApis
{
    public class MultiLogin
    {
        #region Default Constructor

        public MultiLogin(IConfiguration configuration)
        {
            _httpClient = new HttpClient(new HttpClientHandler()
            {
                Proxy = null
            }, true);
            
            AuthToken = configuration["MultiloginToken"];
        }

        #endregion

        #region Private Fields

        private readonly HttpClient _httpClient;
        private string _defaultFolderId;

        #endregion

        #region Public Properties

        private string mAuthToken = string.Empty;

        public string AuthToken
        {
            get => mAuthToken;
            set
            {
                if (mAuthToken == value)
                    return;

                mAuthToken = value;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mAuthToken);
            }
        }

        #endregion

        #region Public Methods

        public QuickProfileResponse? StartQuickProfile(ProxyModel? proxyModel)
        {
            return Run(() =>
                    IStartQuickProfile(proxyModel),
                2,
                TimeSpan.FromSeconds(5),
                r => r is not null,
                OnError
            );
        }

        public bool IsMultiLoginRunning()
        {
            try
            {
                HttpRequestMessage message = new()
                {
                    RequestUri = new Uri("https://launcher.mlx.yt:45001/api/v1/version"),
                    Method = HttpMethod.Get
                };

                HttpResponseMessage responseMessage = _httpClient.Send(message);
                return responseMessage.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // public string Start(string id)
        // {
        //     return Run(() =>
        //             IStartBrowser(id),
        //         r => r is not null,
        //         OnError
        //     );
        // }

        public bool? Stop(string? id)
        {
            return Run(() =>
                    IStopBrowser(id),
                r => r is not null,
                OnError
            );
        }

        #endregion

        #region Private Methods

        private QuickProfileResponse? IStartQuickProfile(ProxyModel? proxyModel)
        {
            Uri uri = Url.Create("https://launcher.mlx.yt:45001")
                .AddPaths("api", "v3", "profile", "quick")
                .Build();

            HttpRequestMessage message = new()
            {
                Method = HttpMethod.Post,
                RequestUri = uri,
            };
            message.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            bool useProxy = proxyModel is not null &&
                            proxyModel.HasProxy;

            JObject jObject = JObject.FromObject(new
            {
                name = $"{DI.AssemblyName} - {Guid.NewGuid():N}",
                folder_id = "",
                browser_type = "mimic",
                os_type = "windows",
                is_headless = false,
                automation = "selenium",
                parameters = new
                {
                    fingerprint = new
                    {
                    },
                    flags = new
                    {
                        navigator_masking = "mask",
                        audio_masking = "natural",
                        localization_masking = "mask",
                        geolocation_popup = "prompt",
                        geolocation_masking = "mask",
                        timezone_masking = "mask",
                        graphics_noise = "mask",
                        graphics_masking = "mask",
                        webrtc_masking = "mask",
                        fonts_masking = "mask",
                        media_devices_masking = "natural",
                        screen_masking = "mask",
                        proxy_masking = useProxy ? "custom" : "disabled",
                        ports_masking = "mask",
                        canvas_noise = "natural",
                    }
                }
            });

            if (useProxy)
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                JToken parameters = jObject["parameters"];
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                parameters["proxy"] = JObject.FromObject(new
                {
                    type = proxyModel.Protocol.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? "http"
                        : "socks5",
                    host = proxyModel.Host,
                    port = proxyModel.Port,
                    username = proxyModel.Username,
                    password = proxyModel.Password
                });
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }

            message.Content = new StringContent(jObject.ToString(Formatting.None), System.Text.Encoding.UTF8, "application/json");

            using HttpResponseMessage response = _httpClient.Send(message);

            string src = response.Content.ReadAsStringAsync().Result;

            if (response.StatusCode is HttpStatusCode.Unauthorized)
            {
                Logger.LogSourceCode((int)response.StatusCode, src);
                //RefreshNewToken();
                return default;
            }

            JObject? json = null;
            try
            {
                json = JObject.Parse(src);
            }
            catch
            {
                json = null;
            }
            
            if (json != null && !HasError(json, out string errorCode))
            {
                JToken? data = json?["data"];
                string? id = data?["id"]?.Value<string>();
                string? port = data?["port"]?.Value<string>();

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(port))
                {
                    return new QuickProfileResponse()
                    {
                        Id = id,
                        Port = port
                    };
                }
            }

            Logger.LogSourceCode((int)response.StatusCode, src);
            return new QuickProfileResponse();
        }

        private bool? IStopBrowser(string? id)
        {
            if (string.IsNullOrEmpty(id))
                return true;
            
            Uri uri = Url.Create("https://launcher.mlx.yt:45001")
                .AddPaths("api", "v1", "profile", "stop", "p", id)
                .Build();

            HttpRequestMessage message = new()
            {
                Method = HttpMethod.Get,
                RequestUri = uri,
            };
            message.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            using HttpResponseMessage response = _httpClient.Send(message);

            if (response.StatusCode is HttpStatusCode.Unauthorized)
            {
                //RefreshNewToken();
                return default;
            }

            string src = response.Content.ReadAsStringAsync().Result;

            JObject? json = null;
            try
            {
                json = JObject.Parse(src);
            }
            catch
            {
                json = null;
            }
            
            if (json != null && !HasError(json, out string errorCode))
            {
                return true;
            }

            Logger.LogSourceCode((int)response.StatusCode, src);
            return false;
        }

        #endregion

        #region Helper Methods

        private static string Md5Hash(string input)
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = MD5.HashData(inputBytes);

            StringBuilder sb = new();
            for (int i = 0; i < hashBytes.Length; i++)
                sb.Append(hashBytes[i].ToString("x2"));

            return sb.ToString();
        }

        private bool HasError(JObject? json, out string errorCode)
        {
            errorCode = string.Empty;

            JToken? status = json?["status"];

            if (status is not null &&
                status.Type == JTokenType.Object)
                errorCode = status?["error_code"]?.Value<string>() ?? "";

            return !string.IsNullOrEmpty(errorCode);
        }

        private void OnError(Exception exception)
        {
            Logger.LogError(exception);
        }

        #endregion
    }
}