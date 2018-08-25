using System;
using Android.App;
using Android.Content;
using Android.Support.V4.App;

namespace BirdTouch.Helpers
{
    /// <summary>
    /// Checks if device can connect to the internet
    /// </summary>
    abstract class  Reachability
    {
        /// <summary>
        /// Checks if device is online
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        public static bool IsOnline(Activity activity)
        {
            var connectivityManager =
                (Android.Net.ConnectivityManager) activity.GetSystemService(Context.ConnectivityService);
            Android.Net.NetworkInfo activeConnection = connectivityManager.ActiveNetworkInfo;
            bool isOnline = (activeConnection != null) && activeConnection.IsConnected;
            return isOnline;
        }
    }
}