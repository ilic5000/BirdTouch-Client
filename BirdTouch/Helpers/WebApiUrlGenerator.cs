using Android.App;
using Android.Content;
using System;

namespace BirdTouch.Helpers
{
    public class WebApiUrlGenerator
    {
        /// <summary>
        /// Returns fully formated endpoint Uri
        /// </summary>
        /// <param name="endpointStringId">Id from Resource String table</param>
        /// <returns></returns>
        public static Uri GenerateWebApiUrl(int endpointStringId)
        {
            var sharedPreferences = WebApiUrlGenerator.GetSharedPreferencesForWebApiSettings();
            var protocol = WebApiUrlGenerator.GetProtocol(sharedPreferences);
            var ipAddress = WebApiUrlGenerator.GetIpAddress(sharedPreferences);
            var port = WebApiUrlGenerator.GetPort(sharedPreferences);

            // Get endpoint from ApiEndpoints.xml file by endpoint Id
            string endpoint = Application.Context.GetString(endpointStringId);

            string fullEndpoint = $"{protocol}://{ipAddress}:{port}/{endpoint}";

            return new Uri(fullEndpoint);
        }

        public static ISharedPreferences GetSharedPreferencesForWebApiSettings()
        {
            return Application.Context.GetSharedPreferences("WebApiServerSettings", FileCreationMode.Private);
            
        }

        public static string GetProtocol(ISharedPreferences sharedPreferences = null)
        {
            if (sharedPreferences == null)
            {
                sharedPreferences = WebApiUrlGenerator.GetSharedPreferencesForWebApiSettings();
            }

            string protocol = Application.Context.GetString(Resource.String.webapi_protocol);

            if (sharedPreferences != null)
            {
                protocol = sharedPreferences.GetString("WebApiProtocol", defValue: Application.Context.GetString(Resource.String.webapi_protocol));
            }

            return protocol;
        }

        public static void SetProtocol(string value, ISharedPreferences sharedPreferences = null)
        {
            if (sharedPreferences == null)
            {
                sharedPreferences = WebApiUrlGenerator.GetSharedPreferencesForWebApiSettings();
            }

            sharedPreferences.Edit().PutString("WebApiProtocol", value).Commit();
        }

        public static string GetIpAddress(ISharedPreferences sharedPreferences = null)
        {
            if (sharedPreferences == null)
            {
                sharedPreferences = WebApiUrlGenerator.GetSharedPreferencesForWebApiSettings();
            }

            string ipAddress = Application.Context.GetString(Resource.String.webapi_address);

            if (sharedPreferences != null)
            {
                ipAddress = sharedPreferences.GetString("WebApiAddress", defValue: Application.Context.GetString(Resource.String.webapi_address));
            }

            return ipAddress;
        }

        public static void SetIpAddress(string value, ISharedPreferences sharedPreferences = null)
        {
            if (sharedPreferences == null)
            {
                sharedPreferences = WebApiUrlGenerator.GetSharedPreferencesForWebApiSettings();
            }

            sharedPreferences.Edit().PutString("WebApiAddress", value).Commit();
        }

        public static string GetPort(ISharedPreferences sharedPreferences = null)
        {
            if (sharedPreferences == null)
            {
                sharedPreferences = WebApiUrlGenerator.GetSharedPreferencesForWebApiSettings();
            }

            string port = Application.Context.GetString(Resource.String.webapi_port);

            if (sharedPreferences != null)
            {
                port = sharedPreferences.GetString("WebApiPort", defValue: Application.Context.GetString(Resource.String.webapi_port));
            }

            return port;
        }

        public static void SetPort(string value, ISharedPreferences sharedPreferences = null)
        {
            if (sharedPreferences == null)
            {
                sharedPreferences = WebApiUrlGenerator.GetSharedPreferencesForWebApiSettings();
            }

            sharedPreferences.Edit().PutString("WebApiPort", value).Commit();
        }
    }
}