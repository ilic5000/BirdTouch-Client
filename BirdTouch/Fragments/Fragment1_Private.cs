using System;
using System.Collections.Generic;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using SupportFragment = Android.Support.V4.App.Fragment;
using Android.Support.V7.Widget;
using BirdTouch.Fragments.CheeseHelper;
using Android.Graphics;
using Android.Content.Res;
using Android.Support.Design.Widget;
using Android.Locations;
using System.Linq;
using Android.Nfc;
using Android.Runtime;
using System.Net;
using System.Collections.Specialized;
using Android.Text;
using System.Text;
using BirdTouch.Models;

namespace BirdTouch.Fragments
{
    public class Fragment1_Private : SupportFragment, ILocationListener
    {

        private RecyclerView recycleView;
        public static SwitchCompat switchVisibility;
        private Location currLocation;
        private LocationManager locationManager;
        private ProgressBar progressBarLocation;
        string _locationProvider;

        private WebClient webClientMakeUserVisible;
        private WebClient webClientMakeUserInvisible;
        private WebClient webClientGetPrivateUsersNearMe;
        private Uri uri;

        private bool visible = false;


        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
           
            View view = inflater.Inflate(Resource.Layout.Fragment1_private, container, false);
            recycleView = view.FindViewById<RecyclerView>(Resource.Id.recyclerViewPrivate);
            switchVisibility = view.FindViewById<SwitchCompat>(Resource.Id.activatePrivateSwitch);
            progressBarLocation = view.FindViewById<ProgressBar>(Resource.Id.progressBarGetLocation);
            switchVisibility.CheckedChange += SwitchVisibility_CheckedChange;
            webClientMakeUserVisible = new WebClient();
            webClientMakeUserInvisible = new WebClient();
            webClientGetPrivateUsersNearMe = new WebClient();

            webClientMakeUserVisible.DownloadDataCompleted += WebClientMakeUserVisible_DownloadDataCompleted;
            webClientMakeUserInvisible.DownloadDataCompleted += WebClientMakeUserInvisible_DownloadDataCompleted;
            webClientGetPrivateUsersNearMe.DownloadDataCompleted += WebClientGetPrivateUsersNearMe_DownloadDataCompleted;

            SetUpRecyclerView(recycleView);
            

            return view;
        }


        private void SwitchVisibility_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (e.IsChecked)
            {
                InitializeLocationManager(); //svaki put kada se promeni switch na ON, da se vidi da li postoji GPS ili NETWORK location
                if (!_locationProvider.Equals(string.Empty))
                {
                    locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this);
                    progressBarLocation.Visibility = ViewStates.Visible;
                }
                else
                {
                    Snackbar.Make(switchVisibility, Android.Text.Html.FromHtml("<font color=\"#ffffff\">No GPS or Network Geolocation available</font>"), Snackbar.LengthLong).Show();


                }

            }
            else
            {
              //  Snackbar.Make(switchVisibility, Android.Text.Html.FromHtml("<font color=\"#ffffff\">Off</font>"), Snackbar.LengthLong).Show();
                locationManager.RemoveUpdates(this);
                GoInvisible();
                progressBarLocation.Visibility = ViewStates.Invisible;
            }
        }

        private void InitializeLocationManager()//ovde treba videti kako uzeti lepo lokaciju sa mreze ako nema gps, ali to je za kraj
        {
            locationManager = (LocationManager)Activity.GetSystemService(Context.LocationService);
            Criteria criteriaForLocationService = new Criteria
            {
                Accuracy = Accuracy.Fine
            };
            IList<string> acceptableLocationProviders = locationManager.GetProviders(criteriaForLocationService, true);

            if (acceptableLocationProviders.Any())
            {
                _locationProvider = acceptableLocationProviders.First();
            }
            else
            {
                Criteria criteriaForLocationServiceBackup = new Criteria
                {
                    Accuracy = Accuracy.Coarse
                };
                IList<string> acceptableLocationProvidersBackup = locationManager.GetProviders(criteriaForLocationServiceBackup, true);
                if (acceptableLocationProvidersBackup.Any())
                {
                    _locationProvider = acceptableLocationProvidersBackup.First();

                }else
                { 
                    _locationProvider = string.Empty;
                }
            }
            Log.Debug("log debug tag", "Using " + _locationProvider + ".");
        }



        public void OnLocationChanged(Location location)
        {
            currLocation = location;
            if (this.currLocation == null)
            {
                Snackbar.Make(switchVisibility, Android.Text.Html.FromHtml("<font color=\"#ffffff\">Unable to determine location</font>"), Snackbar.LengthLong).Show();

            }
            else
            {
              //  Snackbar.Make(switchVisibility, Android.Text.Html.FromHtml("<font color=\"#ffffff\">"+location.Provider +": " + string.Format("{0:f6},{1:f6}", location.Latitude, location.Longitude)+ " " +location.Time.ToString()+"</font>"), Snackbar.LengthLong).Show();
              SendLocationToDatabase();
            }
            locationManager.RemoveUpdates(this); //samo jednom da uzme gps koordinate, da ne refreshuje stalno
            
        }

        private void SendLocationToDatabase()
        {
            if (Reachability.isOnline(Activity) && !webClientMakeUserVisible.IsBusy)
            {

               
                //insert parameters for header for web request
                NameValueCollection parameters = new NameValueCollection();
                parameters.Add("latitude", currLocation.Latitude.ToString());
                parameters.Add("longitude", currLocation.Longitude.ToString());
                StartPageActivity.CheckVisibilityMode();//provera sta je sve ukljuceno i updatevoanje visibility mode po potrebi
                parameters.Add("mode", "1"); // mozda treba mode globalni, ali videcemo 
                parameters.Add("id", StartPageActivity.user.Id.ToString());

                String restUriString = GetString(Resource.String.server_ip_makeUserVisible);
                uri = new Uri(restUriString);

                webClientMakeUserVisible.Headers.Clear();
                webClientMakeUserVisible.Headers.Add(parameters);
                webClientMakeUserVisible.DownloadDataAsync(uri);

            }
            else
            {

                Snackbar.Make(recycleView, Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"), Snackbar.LengthLong).Show();

            }
        }


        private void WebClientMakeUserVisible_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                //ovde naknadno ubaciti proveru da li je doslo do nestanka neta, a ne da postoji samo jedan error, ali za betu je ovo dovoljno


                Console.WriteLine("*******Error webclient data save changes error");
                Console.WriteLine(e.Error.Message);
                Console.WriteLine("******************************************************");
                Snackbar.Make(recycleView, Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"), Snackbar.LengthLong).Show();
                visible = false;
            }
            else
            {

                Console.WriteLine("Success!");
                string jsonResult = Encoding.UTF8.GetString(e.Result);
                Console.Out.WriteLine(jsonResult);
                visible = true;
                progressBarLocation.Visibility = ViewStates.Invisible;
                Snackbar.Make(recycleView, Html.FromHtml("<font color=\"#ffffff\">" + StartPageActivity.user.Username.ToString() + " is now visible in private mode</font>"), Snackbar.LengthLong).Show();
                GetPrivateUsersNearMe();//privremeno odavde pozivam
            }
        }



        private void GoInvisible()
        {
            if (Reachability.isOnline(Activity) && !webClientMakeUserInvisible.IsBusy)
            {


                //insert parameters for header for web request
                NameValueCollection parameters = new NameValueCollection();
                StartPageActivity.CheckVisibilityMode();
                parameters.Add("mode", StartPageActivity.visibilityMode.ToString());
                parameters.Add("id", StartPageActivity.user.Id.ToString());

                String restUriString = GetString(Resource.String.server_ip_makeUserInvisible);
                uri = new Uri(restUriString);

                webClientMakeUserInvisible.Headers.Clear();
                webClientMakeUserInvisible.Headers.Add(parameters);
                webClientMakeUserInvisible.DownloadDataAsync(uri);

            }
            else
            {

                Snackbar.Make(recycleView, Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"), Snackbar.LengthLong).Show();

            }
        }


        private void WebClientMakeUserInvisible_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                //ovde naknadno ubaciti proveru da li je doslo do nestanka neta, a ne da postoji samo jedan error, ali za betu je ovo dovoljno


                Console.WriteLine("*******Error webclient data save changes error");
                Console.WriteLine(e.Error.Message);
                Console.WriteLine("******************************************************");
                Snackbar.Make(recycleView, Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"), Snackbar.LengthLong).Show();
                visible = true;
            }
            else
            {

                Console.WriteLine("Success!");
                string jsonResult = Encoding.UTF8.GetString(e.Result);
                Console.Out.WriteLine(jsonResult);
                visible = false;
                Snackbar.Make(recycleView, Html.FromHtml("<font color=\"#ffffff\">" + StartPageActivity.user.Username.ToString() + " is now invisible in private mode</font>"), Snackbar.LengthLong).Show();
            }
        }



        private void GetPrivateUsersNearMe()
        {

            if (Reachability.isOnline(Activity) && !webClientGetPrivateUsersNearMe.IsBusy)
            {

                if (visible) { //ako je korisnik visible tj. u active_users bazi upisan
                //insert parameters for header for web request
                NameValueCollection parameters = new NameValueCollection();
                parameters.Add("id", StartPageActivity.user.Id.ToString());

                String restUriString = GetString(Resource.String.server_ip_getPrivateUsersNearMe);
                uri = new Uri(restUriString);

                webClientGetPrivateUsersNearMe.Headers.Clear();
                webClientGetPrivateUsersNearMe.Headers.Add(parameters);
                webClientGetPrivateUsersNearMe.DownloadDataAsync(uri);
                }
                else
                {
                    Snackbar.Make(recycleView, Html.FromHtml("<font color=\"#ffffff\">You are not visible to others</font>"), Snackbar.LengthLong).Show();

                }

            }
            else
            {

                Snackbar.Make(recycleView, Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"), Snackbar.LengthLong).Show();

            }


        }


        private void WebClientGetPrivateUsersNearMe_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                //ovde naknadno ubaciti proveru da li je doslo do nestanka neta, a ne da postoji samo jedan error, ali za betu je ovo dovoljno


                Console.WriteLine("*******Error webclient data error");
                Console.WriteLine(e.Error.Message);
                Console.WriteLine("******************************************************");
                Snackbar.Make(recycleView, Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"), Snackbar.LengthLong).Show();
                
            }
            else
            {

                Console.WriteLine("Success!");
                string jsonResult = Encoding.UTF8.GetString(e.Result);
                
                List<User> listOfUsersAroundMe = Newtonsoft.Json.JsonConvert.DeserializeObject<List<User>>(jsonResult);


            }
        }






        // Use this to return your custom view for this Fragment
        // return inflater.Inflate(Resource.Layout.YourFragment, container, false);
        //   RecyclerView recyclerView = inflater.Inflate(Resource.Layout.Fragment1_private, container, false) as RecyclerView;
        // return recyclerView;


     
            
        //*********************************************************
        //LocationListener interfejs, mozda nekad implementirati

        public void OnProviderDisabled(string provider)
        {
            Snackbar.Make(switchVisibility, Android.Text.Html.FromHtml("<font color=\"#ffffff\">" + provider + " is disabled</font>"), Snackbar.LengthLong).Show();

        }

        public void OnProviderEnabled(string provider)
        {
            Snackbar.Make(switchVisibility, Android.Text.Html.FromHtml("<font color=\"#ffffff\">" + provider + " is enabled</font>"), Snackbar.LengthLong).Show();

        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            //mozda nekad implementirati, ali nema potrebe
        }

        //public override void OnPause()
        //{
        //    base.OnPause();
        //    locationManager.RemoveUpdates(this);
        //}

        //public override void OnResume()
        //{
        //    base.OnResume();
        //    locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this);
        //}


        //*********************************************
        //RecycleView setup


        private void SetUpRecyclerView(RecyclerView recyclerView) //ovde da se napravi lista dobijenih korisnika
        {
            var values = GetRandomSubList(CheeseHelper.Cheeses.CheeseStrings, 30);
            recyclerView.SetLayoutManager(new LinearLayoutManager(recyclerView.Context));
            recyclerView.SetAdapter(new SimpleStringRecyclerViewAdapter(recyclerView.Context, values, Activity.Resources));
            
            recyclerView.SetItemClickListener((rv,position,view) =>
            {
                Context context = view.Context;
                Intent intent = new Intent(context, typeof(UserDetailActivity));
                intent.PutExtra(UserDetailActivity.EXTRA_NAME, values[position]);
                context.StartActivity(intent);
            });
        }

        private List<string> GetRandomSubList(List<string> items, int amount)
        {
            List<string> list = new List<string>();
            Random random = new Random();
            while(list.Count < amount)
            {
                list.Add(items[random.Next(items.Count)]);
            }
            return list;
        }

      
        public class SimpleStringRecyclerViewAdapter : RecyclerView.Adapter
        {
            private readonly TypedValue mTypedValue = new TypedValue();
            private int mBackground;
            private List<string> mValues;
            Resources mResource;
            private Dictionary<int, int> mCalculatedSizes;

            public SimpleStringRecyclerViewAdapter(Context context, List<string> items, Resources res)
            {
                context.Theme.ResolveAttribute(Resource.Attribute.selectableItemBackground, mTypedValue, true);
                mBackground = mTypedValue.ResourceId;
                mValues = items;
                mResource = res;
                mCalculatedSizes = new Dictionary<int, int>();
            }

            public override int ItemCount
            {
                get
                {
                    return mValues.Count;
                }
            }

            public override async void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                var simpleHolder = holder as SimpleViewHolder;

                simpleHolder.mBoundString = mValues[position];
                simpleHolder.mTxtView.Text = mValues[position];

                int drawableID = Cheeses.RandomCheeseDrawable;
                BitmapFactory.Options options = new BitmapFactory.Options();

                if (mCalculatedSizes.ContainsKey(drawableID))
                {
                    options.InSampleSize = mCalculatedSizes[drawableID];
                }

                else
                {
                    options.InJustDecodeBounds = true;

                    BitmapFactory.DecodeResource(mResource, drawableID, options);

                    options.InSampleSize = Cheeses.CalculateInSampleSize(options, 100, 100);
                    options.InJustDecodeBounds = false;

                    mCalculatedSizes.Add(drawableID, options.InSampleSize);
                }


                var bitMap = await BitmapFactory.DecodeResourceAsync(mResource, drawableID, options);

                simpleHolder.mImageView.SetImageBitmap(bitMap);
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                View view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.List_Item, parent, false);
                view.SetBackgroundResource(mBackground);

                return new SimpleViewHolder(view);
            }
        }

        public class SimpleViewHolder : RecyclerView.ViewHolder
        {
            public string mBoundString;
            public readonly View mView;
            public readonly ImageView mImageView;
            public readonly TextView mTxtView;

            public SimpleViewHolder(View view) : base(view)
            {
                mView = view;
                mImageView = view.FindViewById<ImageView>(Resource.Id.avatar);
                mTxtView = view.FindViewById<TextView>(Resource.Id.text1);
            }

            public override string ToString()
            {
                return base.ToString() + " '" + mTxtView.Text;
            }
        }

    }
}