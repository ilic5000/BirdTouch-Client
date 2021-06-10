using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace BirdTouch.Constants
{
    public static class SharedPreferencesConstants
    {
        public static string JWT_STORAGE = "JwtTokenStorage";
        public static string JWT_TOKEN_KEY = "token";
        public static string SEARCH_RADIUS_STORAGE = "SearchRadiusStorage";
        public static string SEARCH_RADIUS_KEY = "SearchRadiusKey";
        public static string WEB_SERVER_SETTINGS = "WebApiServerSettings";
        public static string WEB_SERVER_SETTINGS_PROTOCOL = "WebApiProtocol";
        public static string WEB_SERVER_SETTINGS_ADDRESS = "WebApiAddress";
        public static string WEB_SERVER_SETTINGS_PORT = "WebApiPort";
    }
}