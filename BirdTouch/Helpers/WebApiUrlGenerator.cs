using Android.App;
using System;

namespace BirdTouch.Helpers
{
    public class WebApiUrlGenerator
    {
        /// <summary>
        /// Returns fully formated endpoint uri
        /// </summary>
        /// <param name="endpointStringId">Id from Resource String table</param>
        /// <returns></returns>
        public static Uri GenerateWebApiUrl(int endpointStringId)
        {
            string protocol = Application.Context.GetString(Resource.String.webapi_http_protocol);
            string ipAdress = Application.Context.GetString(Resource.String.webapi_ip_adress);
            string endpoint = Application.Context.GetString(endpointStringId);

            string fullEndpoint = protocol + ipAdress + "/" + endpoint;

            return new Uri(fullEndpoint);
        }

    }
}