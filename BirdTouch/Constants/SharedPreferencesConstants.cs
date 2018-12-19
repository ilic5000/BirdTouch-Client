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
    }
}