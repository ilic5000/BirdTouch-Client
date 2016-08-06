using System;
using Android.Widget;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.App;
using Android.Net;

namespace BirdTouch
{
    [Activity(Label = "BirdTouch v0.01", MainLauncher = true, Icon = "@drawable/Logo", Theme = "@style/Theme.DesignDemo")]
    public class MainActivity : FragmentActivity
    {

        private Button btnRegister;
        private Button btnSignIn;
        
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
          //  ActionBar.Hide();
            
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            
            btnRegister = FindViewById<Button>(Resource.Id.btnRegister);
            btnRegister.Click += (object sender, EventArgs e) =>
        {
            //poziva dijalog za register
            Android.Support.V4.App.FragmentManager fm = SupportFragmentManager;
            dialog_Register dialogRegister = new dialog_Register();
            dialogRegister.Show(fm, "Dialog fragment");
        };
            
            btnSignIn = FindViewById<Button>(Resource.Id.btnSignIn);
            btnSignIn.Click += (object sender, EventArgs e) =>
            {
                //poziva dijalog za SIGNin
                Android.Support.V4.App.FragmentManager fm = SupportFragmentManager;
                dialog_SignIn dialogSignIn = new dialog_SignIn();
                dialogSignIn.Show(fm, "Dialog fragment");
            };


            

        }


    }
}

