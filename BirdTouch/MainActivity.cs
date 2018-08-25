using System;
using Android.Widget;
using Android.OS;
using Android.Support.V4.App;
using Android.App;

namespace BirdTouch
{
    [Activity(Label = "BirdTouch v1.0.7", MainLauncher = true, Icon = "@drawable/Logo", Theme = "@style/Theme.DesignDemo")]
    public class MainActivity : FragmentActivity
    {
        private Button _btnRegister;
        private Button _btnSignIn;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Find buttons
            _btnRegister = FindViewById<Button>(Resource.Id.btnRegister);
            _btnSignIn = FindViewById<Button>(Resource.Id.btnSignIn);

            // Add desired actions for buttons

            // Shows dialog for signup
            _btnRegister.Click += (object sender, EventArgs e) =>
            {
                new dialog_Register().Show(SupportFragmentManager, "Dialog fragment");
            };

            // Shows dialog for signing in
            _btnSignIn.Click += (object sender, EventArgs e) =>
            {
                new dialog_SignIn().Show(SupportFragmentManager, "Dialog fragment");
            };
        }
    }
}

