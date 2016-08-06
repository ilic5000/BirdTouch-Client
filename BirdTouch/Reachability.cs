using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace BirdTouch
{
    abstract class  Reachability
    {
        //Klasa koja mi sluzi za proveru da li je dostupan net
        public static bool isOnline(Activity activity)
        {
            Android.Net.ConnectivityManager connectivityManager = (Android.Net.ConnectivityManager)activity.GetSystemService(Context.ConnectivityService);
            Android.Net.NetworkInfo activeConnection = connectivityManager.ActiveNetworkInfo;
            bool isOnline = (activeConnection != null) && activeConnection.IsConnected;
            return isOnline;
        }
    }
}