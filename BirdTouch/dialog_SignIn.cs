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
using System.Net;
using System.Collections.Specialized;
using System.Xml;
using BirdTouch.Models;

namespace BirdTouch
{
     
    class dialog_SignIn : DialogFragment
    {
        private EditText editTxtUsername;
        private EditText editTxtPassword;
        private Button btnSignIn;
        private ProgressBar progressBar;
        private WebClient webClient;
        private Uri uri;
        private int a=0;
        
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            var view = inflater.Inflate(Resource.Layout.dialog_signin, container, false);

            btnSignIn = view.FindViewById<Button>(Resource.Id.btnDialogSignIn);
            editTxtUsername = view.FindViewById<EditText>(Resource.Id.txtUsernameSignIn);
            editTxtPassword = view.FindViewById<EditText>(Resource.Id.txtPasswordSignIn);
            progressBar = view.FindViewById<ProgressBar>(Resource.Id.progressBarSignIn);
            

            webClient = new WebClient();
            webClient.DownloadDataCompleted += WebClient_DownloadDataCompleted;

            btnSignIn.Click += (object sender, EventArgs e) =>
            {
                progressBar.Visibility=ViewStates.Visible;
                String restUriString = "http://10.13.1.66:80/BirdTouchServer/rest/getUserLogin/" + editTxtUsername.Text + "/" + editTxtPassword.Text;
                uri = new Uri(restUriString);
                webClient.DownloadDataAsync(uri);
                


            };
            
            return view;
        }

       

        private void WebClient_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
               
                // Exceptions definitely available here.
                //Console.WriteLine(e.Error.ToString());
                Console.WriteLine(e.Error.Message);

                AlertDialog.Builder alert = new AlertDialog.Builder(Activity);
                alert.SetTitle("Wrong username/password");

                alert.SetNegativeButton("Try Again", (senderAlert, args) => {
                    //change value write your own set of instructions
                    //you can also create an event for the same in xamarin
                    //instead of writing things here
                    alert.Dispose();
                });

                alert.SetPositiveButton("Cancel", (senderAlert, args) => {
                    //perform your own task for this conditional button click
                    this.Dismiss();
                   
                });

                //run the alert in UI thread to display in the screen
                //RunOnUiThread(() => {
                //    alert.Show();
                //});
                alert.Show();

            }
            else
            {
                
                Console.WriteLine("Success!");
                string jsonResult = Encoding.UTF8.GetString(e.Result);
                Console.Out.WriteLine(jsonResult);
                User user = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(jsonResult);

                Console.WriteLine("{0} - {1}", user.FirstName, user.LastName);

                Intent intent = new Intent(this.Activity, typeof(StartPageActivity));
                intent.PutExtra("userLoggedInJson", jsonResult);
                this.StartActivity(intent);
                this.Activity.Finish();

            }

            
            progressBar.Visibility = ViewStates.Gone;
            
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            Dialog.Window.RequestFeature(WindowFeatures.NoTitle); //izbacujemo title bar, stavljamo na invisible
            base.OnActivityCreated(savedInstanceState);
            Dialog.Window.Attributes.WindowAnimations = Resource.Style.dialog_animation;
            

        }
    }
}