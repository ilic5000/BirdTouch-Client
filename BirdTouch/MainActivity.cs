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

        private Button btnRegister;
        private Button btnSignIn;
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            ActionBar.Hide();

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            btnRegister = FindViewById<Button>(Resource.Id.btnRegister);
            btnRegister.Click += (object sender, EventArgs e) =>
        {
            //poziva dijalog za register
            FragmentTransaction transaction = FragmentManager.BeginTransaction();
            dialog_Register dialogRegister = new dialog_Register();
            dialogRegister.Show(transaction, "dialog fragment");
        };

            btnSignIn = FindViewById<Button>(Resource.Id.btnSignIn);
            btnSignIn.Click += (object sender, EventArgs e) =>
            {
                //poziva dijalog za register
                FragmentTransaction transaction = FragmentManager.BeginTransaction();
                dialog_SignIn dialogRegister = new dialog_SignIn();
                dialogRegister.Show(transaction, "dialog fragment");
            };













            // Get our button from the layout resource,
            // and attach an event to it
            //   Button button = FindViewById<Button>(Resource.Id.MyButton);

            //    button.Click += delegate { button.Text = string.Format("{0} clicks!", count++); };





        }


    }
}

