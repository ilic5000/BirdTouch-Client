using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using BirdTouch.Models;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace BirdTouch
{
    [Activity(Label = "RegisterFragment", Theme = "@style/Theme.DesignDemo")]
    class dialog_Register : Android.Support.V4.App.DialogFragment  //v4 support je potreban zbog edittexta iz te biblioteke
    {
        private TextInputLayout usernameWrapper;
        private TextInputLayout passwordWrapper;
        private TextInputLayout passwordCheckWrapper;
        private ProgressBar progressBar;
        private WebClient webClient;
        private WebClient webClientRegister;
        private System.Uri uri;
        private Button btnRegister;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {

            base.OnCreateView(inflater, container, savedInstanceState);

            var view = inflater.Inflate(Resource.Layout.dialog_register, container, false);

            usernameWrapper = view.FindViewById<TextInputLayout>(Resource.Id.txtInputLayoutRegisterUsername);
            passwordWrapper = view.FindViewById<TextInputLayout>(Resource.Id.txtInputLayoutRegisterPassword);
            passwordCheckWrapper = view.FindViewById<TextInputLayout>(Resource.Id.txtInputLayoutRegisterPasswordCheck);
            progressBar = view.FindViewById<ProgressBar>(Resource.Id.progressBarRegister);
            btnRegister = view.FindViewById<Button>(Resource.Id.btnDialogRegister);

            webClient = new WebClient();
            webClientRegister = new WebClient();

            webClient.DownloadDataCompleted += WebClient_DownloadDataCompleted;
            webClientRegister.DownloadDataCompleted += WebClientRegister_DownloadDataCompleted;

            usernameWrapper.EditText.FocusChange += UsernameEditText_AfterFocusChanged;
            passwordCheckWrapper.EditText.AfterTextChanged += PasswordCheckEditText_AfterTextChanged;

            usernameWrapper.Error = "";
            passwordWrapper.Error = "";
            passwordCheckWrapper.Error = "";


            btnRegister.Click += (object sender, EventArgs e) =>
            {

                //provera da li je aplikaciji dostupan net
                if (Reachability.isOnline(Activity) && !webClientRegister.IsBusy)
                {
                    //provera da li je username i password ok i da nisu prazna polja
                    if ((usernameWrapper.Error=="" || usernameWrapper.Error==null) && (passwordCheckWrapper.Error=="" || passwordWrapper.Error == null) && usernameWrapper.EditText.Text!="" && passwordCheckWrapper.EditText.Text != "" && passwordWrapper.EditText.Text != "" && passwordCheckWrapper.EditText.Text.Equals(passwordWrapper.EditText.Text))
                    {
                        progressBar.Visibility = ViewStates.Visible;
                        String restUriString = GetString(Resource.String.server_ip_registerUser);
                        uri = new System.Uri(restUriString);


                        NameValueCollection parameters = new NameValueCollection();
                        parameters.Add("username", usernameWrapper.EditText.Text);
                        parameters.Add("password", passwordCheckWrapper.EditText.Text);

                        webClientRegister.Headers.Clear();
                        webClientRegister.Headers.Add(parameters);
                        webClientRegister.DownloadDataAsync(uri);

                        Snackbar.Make(view, Android.Text.Html.FromHtml("<font color=\"#ffffff\">Welcome to BirdTouch</font>"), Snackbar.LengthLong).Show();

                    }
                    else
                    {
                        Snackbar.Make(view, Android.Text.Html.FromHtml("<font color=\"#ffffff\">Pease fix username/password</font>"), Snackbar.LengthLong).Show();

                    }

                }
                else
                {

                    Snackbar.Make(view, Android.Text.Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"), Snackbar.LengthLong).Show();

                }

            };
            return view;
        }



        private void PasswordCheckEditText_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            if (!passwordWrapper.EditText.Text.Equals(passwordCheckWrapper.EditText.Text))
            {
                passwordCheckWrapper.Error = "Password missmatch";

            }
            else
            {
                passwordCheckWrapper.Error = "";

            }
        }

        private void UsernameEditText_AfterFocusChanged(object sender, View.FocusChangeEventArgs e)
        {
            if (Reachability.isOnline(Activity) && !webClient.IsBusy)
            { //provera da li je aplikaciji dostupan net

                String restUriString = GetString(Resource.String.server_ip_doesUsernameExist);
                uri = new System.Uri(restUriString);

                NameValueCollection parameters = new NameValueCollection();
                parameters.Add("username", usernameWrapper.EditText.Text);

                webClient.Headers.Clear();
                webClient.Headers.Add(parameters);
                webClient.DownloadDataAsync(uri);

            }

        }

        private void WebClient_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                //ovde naknadno ubaciti proveru da li je doslo do nestanka neta, a ne da postoji samo jedan error, ali za betu je ovo dovoljno
                usernameWrapper.Error = "";

            }
            else
            {

                usernameWrapper.Error = "Username already exists";

            }
        }

        private void WebClientRegister_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                //ovde naknadno ubaciti proveru da li je doslo do nestanka neta, a ne da postoji samo jedan error, ali za betu je ovo dovoljno
                Snackbar.Make(this.View, Android.Text.Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"), Snackbar.LengthLong).Show();


            }
            else
            {
                Console.WriteLine("Success, user is registred and retrieved!");
                string jsonResult = Encoding.UTF8.GetString(e.Result);
                Console.Out.WriteLine(jsonResult);
                User user = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(jsonResult);

                Console.WriteLine("*******Deserialized user");
                Console.WriteLine("{0} - {1}", user.FirstName, user.LastName);
                Console.WriteLine("******************************************************");

                Intent intent = new Intent(this.Activity, typeof(StartPageActivity));
                intent.PutExtra("userPassword", passwordCheckWrapper.EditText.Text);//mozda mi ne treba, ali zbog bolje zastite
                intent.PutExtra("userLoggedInJson", jsonResult);
                this.StartActivity(intent);
                this.Activity.Finish();

            }
            progressBar.Visibility = ViewStates.Gone; //nestaje progress bar jer je ucitavanje sa neta zavrseno

        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            Dialog.Window.RequestFeature(WindowFeatures.NoTitle); //izbacujemo title bar, stavljamo na invisible
            base.OnActivityCreated(savedInstanceState);
            Dialog.Window.Attributes.WindowAnimations = Resource.Style.dialog_animation;
        }
    }
}