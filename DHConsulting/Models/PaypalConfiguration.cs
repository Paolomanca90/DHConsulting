using PayPal.Api;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace DHConsulting.Models
{
    public static class PaypalConfiguration
    {
        public readonly static string ClientId;
        public readonly static string ClientSecret;

        static PaypalConfiguration() 
        {
            ClientId = ConfigurationManager.AppSettings["PayPalClientId"];
            ClientSecret = ConfigurationManager.AppSettings["PayPalClientSecret"];
        }

        public static Dictionary<string, string> GetConfig()
        {
            return new Dictionary<string, string>()
            {
                { "mode", "live" }
            };
        }

        public static string GetAccessToken()
        {
            string accessToken = new OAuthTokenCredential(ClientId, ClientSecret, new Dictionary<string, string>()
            {
                { "mode", "live" }
            }).GetAccessToken();
            return accessToken;
        }

        public static APIContext GetAPIContext()
        {
            APIContext apiContext = new APIContext(GetAccessToken());
            apiContext.Config = GetConfig();
            return apiContext;
        }
    }
}