using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace BirdTouch
{
    [Activity(Label = "BirdTouch v0.01", MainLauncher = true, Icon = "@drawable/Logo")]
    public class MainActivity : Activity
    {
        

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            ActionBar.Hide();

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
         //   Button button = FindViewById<Button>(Resource.Id.MyButton);

        //    button.Click += delegate { button.Text = string.Format("{0} clicks!", count++); };
        }
    }
}

