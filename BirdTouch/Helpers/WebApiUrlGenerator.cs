using Android.App;
using Android.Content;
using BirdTouch.Constants;
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
            var ipAddress = WebApiUrlGenerator.GetAddress(sharedPreferences);
            var port = WebApiUrlGenerator.GetPort(sharedPreferences);

            // Get endpoint from ApiEndpoints.xml file by endpoint Id
            string endpoint = Application.Context.GetString(endpointStringId);

            string fullEndpoint = $"{protocol}://{ipAddress}:{port}/{endpoint}";

            return new Uri(fullEndpoint);
        }

        public static ISharedPreferences GetSharedPreferencesForWebApiSettings()
        {
            return Application.Context.GetSharedPreferences(SharedPreferencesConstants.WEB_SERVER_SETTINGS, FileCreationMode.Private);
        }

        private static string GetValueFromSharedPreferenceWebApiSettings(int resIdOfDefaultStringValue, string sharedPreferenceKey, ISharedPreferences sharedPreferences = null)
        {
            if (sharedPreferences == null)
            {
                sharedPreferences = WebApiUrlGenerator.GetSharedPreferencesForWebApiSettings();
            }

            string value = Application.Context.GetString(resIdOfDefaultStringValue);

            if (sharedPreferences != null)
            {
                value = sharedPreferences.GetString(sharedPreferenceKey, defValue: Application.Context.GetString(resIdOfDefaultStringValue));
            }

            return value;
        }

        private static void SetValueSharedPreferenceWebApiSetting(string sharedPreferenceKey, string value, ISharedPreferences sharedPreferences = null)
        {
            if (sharedPreferences == null)
            {
                sharedPreferences = WebApiUrlGenerator.GetSharedPreferencesForWebApiSettings();
            }

            sharedPreferences.Edit().PutString(sharedPreferenceKey, value).Commit();
        }

        public static string GetProtocol(ISharedPreferences sharedPreferences = null)
        {
            return GetValueFromSharedPreferenceWebApiSettings(resIdOfDefaultStringValue: Resource.String.webapi_protocol,
                                                              sharedPreferenceKey: SharedPreferencesConstants.WEB_SERVER_SETTINGS_PROTOCOL, 
                                                              sharedPreferences);
        }

        public static void SetProtocol(string value, ISharedPreferences sharedPreferences = null)
        {
            SetValueSharedPreferenceWebApiSetting(SharedPreferencesConstants.WEB_SERVER_SETTINGS_PROTOCOL,
                                                  value,
                                                  sharedPreferences);
        }

        public static string GetAddress(ISharedPreferences sharedPreferences = null)
        {
            return GetValueFromSharedPreferenceWebApiSettings(resIdOfDefaultStringValue: Resource.String.webapi_address,
                                                              sharedPreferenceKey: SharedPreferencesConstants.WEB_SERVER_SETTINGS_ADDRESS,
                                                              sharedPreferences);
        }

        public static void SetAddress(string value, ISharedPreferences sharedPreferences = null)
        {
            SetValueSharedPreferenceWebApiSetting(SharedPreferencesConstants.WEB_SERVER_SETTINGS_ADDRESS,
                                                  value,
                                                  sharedPreferences);
        }

        public static string GetPort(ISharedPreferences sharedPreferences = null)
        {
            return GetValueFromSharedPreferenceWebApiSettings(resIdOfDefaultStringValue: Resource.String.webapi_port,
                                                              sharedPreferenceKey: SharedPreferencesConstants.WEB_SERVER_SETTINGS_PORT,
                                                              sharedPreferences);
        }

        public static void SetPort(string value, ISharedPreferences sharedPreferences = null)
        {
            SetValueSharedPreferenceWebApiSetting(SharedPreferencesConstants.WEB_SERVER_SETTINGS_PORT,
                                                  value,
                                                  sharedPreferences);
        }
    }
}