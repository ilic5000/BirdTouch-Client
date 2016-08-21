using System;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Support.Design.Widget;
using System.Net;
using BirdTouch.Models;
using Android.Widget;
using Android.Text;

namespace BirdTouch
{
    [Activity(Label = "SignInFragment", Theme = "@style/Theme.DesignDemo")]
    class dialog_SignIn : Android.Support.V4.App.DialogFragment  //v4 support je potreban zbog edittexta iz te biblioteke
    {
        private EditText editTxtUsername;
        private EditText editTxtPassword;
        private Button btnSignIn;
        private TextInputLayout passwordWrapper;
        private ProgressBar progressBar;
        private WebClient webClient;
        private Uri uri;
          
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            var view = inflater.Inflate(Resource.Layout.dialog_signin, container, false);
           
            btnSignIn = view.FindViewById<Button>(Resource.Id.btnDialogSignIn);
            editTxtUsername = view.FindViewById<EditText>(Resource.Id.txtUsernameSignIn);
            editTxtPassword = view.FindViewById<EditText>(Resource.Id.txtPasswordSignIn);
            progressBar = view.FindViewById<ProgressBar>(Resource.Id.progressBarSignIn);
            passwordWrapper = view.FindViewById<TextInputLayout>(Resource.Id.txtInputLayoutSignInPassword);

            webClient = new WebClient();
            webClient.DownloadDataCompleted += WebClient_DownloadDataCompleted;

            

            btnSignIn.Click += (object sender, EventArgs e) =>
            {
                
    
                if (Reachability.isOnline(Activity) && !webClient.IsBusy) { //provera da li je aplikaciji dostupan net

                progressBar.Visibility=ViewStates.Visible;
                String restUriString = GetString(Resource.String.server_ip_getUserLogin) + editTxtUsername.Text + "/" + editTxtPassword.Text;
                uri = new Uri(restUriString);
                webClient.DownloadDataAsync(uri);
                }
                else {

                    Snackbar.Make(view, Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"), Snackbar.LengthLong).Show();

                }

            };
            
            return view;
        }

        private void WebClient_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                //ovde naknadno ubaciti proveru da li je doslo do nestanka neta, a ne da postoji samo jedan error, ali za betu je ovo dovoljno
                passwordWrapper.Error = "Wrong username/password, try again";
                Console.WriteLine("*******Error webclient data completed sign in error");
                Console.WriteLine(e.Error.Message);
                Console.WriteLine("******************************************************");
            }
            else
            {
                passwordWrapper.Error = "";
                Console.WriteLine("Success!");
                String jsonResult = Encoding.UTF8.GetString(e.Result);
                Console.Out.WriteLine(jsonResult);

                User user = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(jsonResult);

                Console.WriteLine("*******Deserialized user");
                Console.WriteLine("{0} - {1}", user.FirstName, user.LastName);
                Console.WriteLine("******************************************************");

                Intent intent = new Intent(this.Activity, typeof(StartPageActivity));
                intent.PutExtra("userLoggedInJson", jsonResult);
                intent.PutExtra("userPassword", editTxtPassword.Text);//mozda mi ne treba, ali zbog bolje zastite
                this.StartActivity(intent);
                this.Activity.Finish();

            }
 
            progressBar.Visibility = ViewStates.Gone; //nestaje progress bar jer je ucitavanje sa neta zavrseno
            
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            Dialog.Window.RequestFeature(WindowFeatures.NoTitle); //izbacujemo title bar, stavljamo na invisible
            base.OnActivityCreated(savedInstanceState);
            Dialog.Window.Attributes.WindowAnimations = Resource.Style.dialog_animation; //animacija za pojavu dijaloga (odozdo na gore)
         }

       
  
    }


}