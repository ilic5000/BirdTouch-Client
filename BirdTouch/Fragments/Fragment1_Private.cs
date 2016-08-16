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
        private Uri uri;


        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            // return inflater.Inflate(Resource.Layout.YourFragment, container, false);
            //   RecyclerView recyclerView = inflater.Inflate(Resource.Layout.Fragment1_private, container, false) as RecyclerView;

           

           // return recyclerView;




            View view = inflater.Inflate(Resource.Layout.Fragment1_private, container, false);
            recycleView = view.FindViewById<RecyclerView>(Resource.Id.recyclerViewPrivate);
            switchVisibility = view.FindViewById<SwitchCompat>(Resource.Id.activatePrivateSwitch);
            progressBarLocation = view.FindViewById<ProgressBar>(Resource.Id.progressBarGetLocation);
            switchVisibility.CheckedChange += SwitchVisibility_CheckedChange;
            webClientMakeUserVisible = new WebClient();
            webClientMakeUserVisible.DownloadDataCompleted += WebClientMakeUserVisible_DownloadDataCompleted;
            SetUpRecyclerView(recycleView);
            

            return view;
        }

        

        private void InitializeLocationManager()
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
                Snackbar.Make(switchVisibility, Android.Text.Html.FromHtml("<font color=\"#ffffff\">"+location.Provider +": " + string.Format("{0:f6},{1:f6}", location.Latitude, location.Longitude)+ " " +location.Time.ToString()+"</font>"), Snackbar.LengthLong).Show();
                progressBarLocation.Visibility = ViewStates.Invisible;
                SendLocationToDatabase();

            }
            locationManager.RemoveUpdates(this);
            
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
                parameters.Add("mode", StartPageActivity.visibilityMode.ToString());
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
            throw new NotImplementedException();
        }


        public void OnProviderDisabled(string provider)
        {
            Snackbar.Make(switchVisibility, Android.Text.Html.FromHtml("<font color=\"#ffffff\">"+provider+" is disabled</font>"), Snackbar.LengthLong).Show();

        }

        public void OnProviderEnabled(string provider)
        {
            Snackbar.Make(switchVisibility, Android.Text.Html.FromHtml("<font color=\"#ffffff\">" + provider + " is enabled</font>"), Snackbar.LengthLong).Show();

        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            //throw new NotImplementedException();
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

        private void SwitchVisibility_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (e.IsChecked) {
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
                //   Location cLocation = locationManager.GetLastKnownLocation(LocationManager.GpsProvider);
                //   Snackbar.Make(switchVisibility, Android.Text.Html.FromHtml("<font color=\"#ffffff\">" + string.Format("{0:f6},{1:f6}", cLocation.Latitude, cLocation.Longitude) + "</font>"), Snackbar.LengthLong).Show();

                //  Snackbar.Make(switchVisibility, Android.Text.Html.FromHtml("<font color=\"#ffffff\">On</font>"), Snackbar.LengthLong).Show();


                //ILocationService locationService = Microsoft.Practices.ServiceLocation.ServiceLocator.Current.GetInstance<ILocationService>();
                //Position position = locationService.GetPositionAsync(10000).Result;

                //Console.WriteLine("GPS latitude: {0}", position.Latitude);
                //Console.WriteLine("GPS longitude: {0}", position.Longitude);
            }
            else { 
                Snackbar.Make(switchVisibility, Android.Text.Html.FromHtml("<font color=\"#ffffff\">Off</font>"), Snackbar.LengthLong).Show();
                locationManager.RemoveUpdates(this);
                progressBarLocation.Visibility = ViewStates.Invisible;
            }
        }

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