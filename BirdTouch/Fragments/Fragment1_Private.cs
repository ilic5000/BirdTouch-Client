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
using Android.Views.Animations;

namespace BirdTouch.Fragments
{
    public class Fragment1_Private : SupportFragment, ILocationListener
    {
        private Clans.Fab.FloatingActionButton fab_menu_refresh;
        private Clans.Fab.FloatingActionButton fab_menu_automatically;
        private Clans.Fab.FloatingActionButton fab_menu_gps;
        private Clans.Fab.FloatingActionMenu fab_menu;

        private FrameLayout coordLay;

        private RecyclerView recycleView;
        public static SwitchCompat switchVisibility;
        private Location currLocation;
        private LocationManager locationManager;
        private ProgressBar progressBarLocation;
        private ProgressBar progressBarGetPrivateUsers;
        string _locationProvider;

        private WebClient webClientMakeUserVisible;
        private WebClient webClientMakeUserInvisible;
        private WebClient webClientGetPrivateUsersNearMe;
        private Uri uri;

        private bool visible = false;
        private List<User> listOfUsersAroundMe;

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
            progressBarGetPrivateUsers = view.FindViewById<ProgressBar>(Resource.Id.progressBarGetPrivateUsers);
            switchVisibility.CheckedChange += SwitchVisibility_CheckedChange;
            webClientMakeUserVisible = new WebClient();
            webClientMakeUserInvisible = new WebClient();
            webClientGetPrivateUsersNearMe = new WebClient();

            webClientMakeUserVisible.DownloadDataCompleted += WebClientMakeUserVisible_DownloadDataCompleted;
            webClientMakeUserInvisible.DownloadDataCompleted += WebClientMakeUserInvisible_DownloadDataCompleted;
            webClientGetPrivateUsersNearMe.DownloadDataCompleted += WebClientGetPrivateUsersNearMe_DownloadDataCompleted;


            listOfUsersAroundMe = new List<User>();

            coordLay = view.FindViewById<FrameLayout>(Resource.Id.coordinatorLayoutPrivate);

            fab_menu_refresh = view.FindViewById<Clans.Fab.FloatingActionButton>(Resource.Id.fab_menu_refresh_private);
            fab_menu_automatically = view.FindViewById<Clans.Fab.FloatingActionButton>(Resource.Id.fab_menu_automatically_private);
            fab_menu_gps = view.FindViewById<Clans.Fab.FloatingActionButton>(Resource.Id.fab_menu_gps_private);
            fab_menu = view.FindViewById<Clans.Fab.FloatingActionMenu>(Resource.Id.fab_menu_private);

            fab_menu_refresh.Click += Fab_menu_refresh_Click1;
            fab_menu_automatically.Click += Fab_menu_automatically_Click1;
            fab_menu_gps.Click += Fab_menu_gps_Click1;

            fab_menu.MenuToggle += Fab_menu_MenuToggle;

            fab_menu.Visibility = ViewStates.Gone;



            return view;
        }

        private void Fab_menu_MenuToggle(object sender, Clans.Fab.FloatingActionMenu.MenuToggleEventArgs e)
        {
            if (e.Opened)
            {
                //fab_menu.Click += Fab_menu_Click;
                recycleView.Visibility = ViewStates.Invisible;
                //switchVisibility.Visibility = ViewStates.Invisible;
            }
            else
            {
                recycleView.Visibility = ViewStates.Visible;
                //switchVisibility.Visibility = ViewStates.Visible;
            }

        }

       

        private void Fab_menu_gps_Click1(object sender, EventArgs e)
        {
            
           
            locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this);
            // Snackbar.Make(coordLay, Html.FromHtml("<font color=\"#ffffff\">Fragment1:Clicked on GPS</font>"), Snackbar.LengthLong).Show();
            fab_menu.Close(true);
            
        }

        private void Fab_menu_automatically_Click1(object sender, EventArgs e)
        {
            //Snackbar.Make(coordLay, Html.FromHtml("<font color=\"#ffffff\">Fragment1:Clicked on Automatically</font>"), Snackbar.LengthLong).Show();


            fab_menu.Close(true);
            
        }

        private void Fab_menu_refresh_Click1(object sender, EventArgs e)
        {
            GetPrivateUsersNearMe();
            // Snackbar.Make(coordLay, Html.FromHtml("<font color=\"#ffffff\">Fragment1:Clicked on Refresh</font>"), Snackbar.LengthLong).Show();
            fab_menu.Close(true);
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
                    Snackbar.Make(coordLay, Android.Text.Html.FromHtml("<font color=\"#ffffff\">No GPS or Network Geolocation available</font>"), Snackbar.LengthLong).Show();


                }

            }
            else
            {
                //  Snackbar.Make(coordLay, Android.Text.Html.FromHtml("<font color=\"#ffffff\">Off</font>"), Snackbar.LengthLong).Show();

                fab_menu.Visibility = ViewStates.Gone;
                
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
                Snackbar.Make(coordLay, Android.Text.Html.FromHtml("<font color=\"#ffffff\">Unable to determine location</font>"), Snackbar.LengthLong).Show();

            }
            else
            {
                //  Snackbar.Make(coordLay, Android.Text.Html.FromHtml("<font color=\"#ffffff\">"+location.Provider +": " + string.Format("{0:f6},{1:f6}", location.Latitude, location.Longitude)+ " " +location.Time.ToString()+"</font>"), Snackbar.LengthLong).Show();
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

                Snackbar.Make(coordLay, Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"), Snackbar.LengthLong).Show();

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
                Snackbar.Make(coordLay, Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"), Snackbar.LengthLong).Show();
                visible = false;
            }
            else
            {

                Console.WriteLine("Success!");
                string jsonResult = Encoding.UTF8.GetString(e.Result);
                Console.Out.WriteLine(jsonResult);
                visible = true;
                progressBarLocation.Visibility = ViewStates.Invisible;
                Snackbar.Make(coordLay, Html.FromHtml("<font color=\"#ffffff\">" + StartPageActivity.user.Username.ToString() + " is now visible in private mode</font>"), Snackbar.LengthLong).Show();
                
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
                parameters.Add("mode", "1");
                parameters.Add("id", StartPageActivity.user.Id.ToString());

                String restUriString = GetString(Resource.String.server_ip_makeUserInvisible);
                uri = new Uri(restUriString);

                webClientMakeUserInvisible.Headers.Clear();
                webClientMakeUserInvisible.Headers.Add(parameters);
                webClientMakeUserInvisible.DownloadDataAsync(uri);

            }
            else
            {

                Snackbar.Make(coordLay, Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"), Snackbar.LengthLong).Show();

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
                Snackbar.Make(coordLay, Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"), Snackbar.LengthLong).Show();
                visible = true;
            }
            else
            {

                Console.WriteLine("Success!");
                string jsonResult = Encoding.UTF8.GetString(e.Result);
                Console.Out.WriteLine(jsonResult);
                visible = false;
                Snackbar.Make(coordLay, Html.FromHtml("<font color=\"#ffffff\">" + StartPageActivity.user.Username.ToString() + " is now invisible in private mode</font>"), Snackbar.LengthLong).Show();
            }
        }



        private void GetPrivateUsersNearMe()
        {

            if (Reachability.isOnline(Activity) && !webClientGetPrivateUsersNearMe.IsBusy)
            {


                if (visible) { //ako je korisnik visible tj. u active_users bazi upisan

                progressBarGetPrivateUsers.Visibility = ViewStates.Visible;
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
                    Snackbar.Make(coordLay, Html.FromHtml("<font color=\"#ffffff\">You are not visible to others</font>"), Snackbar.LengthLong).Show();

                }

            }
            else
            {

                Snackbar.Make(coordLay, Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"), Snackbar.LengthLong).Show();

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
                Snackbar.Make(coordLay, Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"), Snackbar.LengthLong).Show();
                progressBarGetPrivateUsers.Visibility = ViewStates.Gone;
            }
            else
            {

                Console.WriteLine("Success!");
                string jsonResult = Encoding.UTF8.GetString(e.Result);

                List<User> newListOfUsersAroundMe = Newtonsoft.Json.JsonConvert.DeserializeObject<List<User>>(jsonResult);

                //SimpleStringRecyclerViewAdapter adapterUsersFromRecyclerView = (SimpleStringRecyclerViewAdapter)recycleView.GetAdapter();
                //adapterUsersFromRecyclerView.changeData(newListOfUsersAroundMe);

                //if (recycleView.GetAdapter() != null)
                //{
                //    SimpleStringRecyclerViewAdapter adapterUsersFromRecyclerView = (SimpleStringRecyclerViewAdapter)recycleView.GetAdapter();
                //    adapterUsersFromRecyclerView.clearData();
                //    recycleView.RemoveAllViews();
                //    recycleView.RemoveAllViewsInLayout();
                //}
                SetUpRecyclerView(recycleView, newListOfUsersAroundMe);


                fab_menu.Visibility = ViewStates.Visible;
              
                progressBarGetPrivateUsers.Visibility = ViewStates.Gone;
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
            Snackbar.Make(coordLay, Android.Text.Html.FromHtml("<font color=\"#ffffff\">" + provider + " is disabled</font>"), Snackbar.LengthLong).Show();

        }

        public void OnProviderEnabled(string provider)
        {
            Snackbar.Make(coordLay, Android.Text.Html.FromHtml("<font color=\"#ffffff\">" + provider + " is enabled</font>"), Snackbar.LengthLong).Show();

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


        private void SetUpRecyclerView(RecyclerView recyclerView, List<User> listOfUsersAroundMe) //ovde da se napravi lista dobijenih korisnika
        {
            

            recyclerView.SetLayoutManager(new LinearLayoutManager(recyclerView.Context));

            recyclerView.SetAdapter(new SimpleStringRecyclerViewAdapter(recyclerView.Context, listOfUsersAroundMe, Activity.Resources, recycleView));

            
            //recycleView.SetItemClickListener((rv, position, view) =>
            //{
            //    Context context = view.Context;
            //    Intent intent = new Intent(context, typeof(UserDetailActivity));
            //    intent.PutExtra("userInformation", Newtonsoft.Json.JsonConvert.SerializeObject(listOfUsersAroundMe[position]));
            //    context.StartActivity(intent);
            //});

        }



        public class SimpleStringRecyclerViewAdapter : RecyclerView.Adapter
        {
            private readonly TypedValue mTypedValue = new TypedValue();
            private int mBackground;
            private List<User> mValues;
            private RecyclerView recycleView;
            Resources mResource;
            //private Dictionary<int, int> mCalculatedSizes;

            public SimpleStringRecyclerViewAdapter(Context context, List<User> items, Resources res, RecyclerView rv)
            {
                context.Theme.ResolveAttribute(Resource.Attribute.selectableItemBackground, mTypedValue, true);
                mBackground = mTypedValue.ResourceId;
                
                mValues = items;
                mResource = res;

                recycleView = rv;
               // mCalculatedSizes = new Dictionary<int, int>();
            }

            public override int ItemCount
            {
                get
                {
                    return mValues.Count;
                }
            }


            //public void clearData()
            //{
            //        //int prevCount = mValues.Count;
            //        //mValues.Clear();
            //        //NotifyItemRangeRemoved(0, prevCount);
            //        //NotifyDataSetChanged();
                    
            //}

            ////public void changeData(List<User> newUsers)
            ////{

            ////    mValues = newUsers;
            ////    NotifyDataSetChanged();
            ////    NotifyItemRangeChanged(0, mValues.Count);
            ////}

            public override async void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                var simpleHolder = holder as SimpleViewHolder;

                simpleHolder.mBoundString = mValues[position].Id.ToString();
                simpleHolder.mTxtView.Text = mValues[position].FirstName + " " + mValues[position].LastName;
                


                if (mValues[position].ProfilePictureData != null)
                {
                    Bitmap bm = BitmapFactory.DecodeByteArrayAsync(mValues[position].ProfilePictureData, 0, mValues[position].ProfilePictureData.Length).Result;
                    simpleHolder.mImageView.SetImageBitmap(Bitmap.CreateScaledBitmap(bm,200,100,false));// mozda treba malo da se igra sa ovim
                }
                else
                {
                    //ako korisnik nije postavio profilnu sliku

                    BitmapFactory.Options options = new BitmapFactory.Options();
                    options.InJustDecodeBounds = true;

                    BitmapFactory.DecodeResource(mResource, Resource.Drawable.blank_navigation, options);

                    options.InSampleSize = CalculateInSampleSize(options, 100, 100);
                    options.InJustDecodeBounds = false;

                    var bitMap = await BitmapFactory.DecodeResourceAsync(mResource, Resource.Drawable.blank_navigation, options);

                    simpleHolder.mImageView.SetImageBitmap(bitMap);
                }

                simpleHolder.mView.Click -= MView_Click; //da se ne bi gomilali delegati
                simpleHolder.mView.Click += MView_Click;

                Random rand = new Random();
                if(rand.Next() % 2 ==1)
                setScaleAnimation(holder.ItemView);
                else setFadeAnimation(holder.ItemView);

                simpleHolder.checkbox.Tag = position;
                simpleHolder.checkbox.CheckedChange += Checkbox_CheckedChange;
            }

            private void Checkbox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
            {
                //Newtonsoft.Json.JsonConvert.SerializeObject(mValues[position])
                CheckBox vsender = sender as CheckBox;
                int position = (int)vsender.Tag;
                int userId = StartPageActivity.user.Id;
               // View vsender = sender as View;

                ISharedPreferences pref = vsender.Context.ApplicationContext.GetSharedPreferences("SavedUsers", FileCreationMode.Private);
                ISharedPreferencesEditor edit = pref.Edit();

                
                if (e.IsChecked)
                {//checked
                    
                    if (!pref.Contains("SavedUsersDictionary")) //prvi put u aplikaciji dodajemo private usera u saved
                    {
                        // Snackbar.Make((View)sender, Android.Text.Html.FromHtml("<font color=\"#ffffff\">does not contain</font>"), Snackbar.LengthLong).Show();

                        Dictionary<int, Dictionary<int, List<User>>> dictionary = new Dictionary<int, Dictionary<int, List<User>>>();
                        dictionary.Add(userId, new Dictionary<int, List<User>>());
                        dictionary[userId].Add(1, new List<User>());// 1 je private mode
                        dictionary[userId][1].Add(mValues[position]);

                        edit.Clear();
                        edit.PutString("SavedUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                        edit.Apply();

                    }
                    else //vec postoji dictionary
                    {
                        string serializedDictionary = pref.GetString("SavedUsersDictionary", String.Empty);
                        if (serializedDictionary != String.Empty)
                        {

                            Dictionary<int, Dictionary<int, List<User>>> dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, List<User>>>>(serializedDictionary);
                            if (!dictionary.ContainsKey(userId))
                            {//ako user nije uopste dodavao usere
                                dictionary.Add(userId, new Dictionary<int, List<User>>());
                            }
                            if (!dictionary[userId].ContainsKey(1))
                            {//ako nije dodavao private usere
                                dictionary[userId].Add(1, new List<User>());
                            }

                            //samo dodamo private usera iz recyclerViewa
                            dictionary[userId][1].Add(mValues[position]);
                            edit.Clear();
                            edit.PutString("SavedUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                            edit.Apply();

                        }

                    }

                }
                else
                {//unchecked
                  
                    string serializedDictionary = pref.GetString("SavedUsersDictionary", String.Empty);
                    if (serializedDictionary != String.Empty)
                    {
                        Dictionary<int, Dictionary<int, List<User>>> dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, List<User>>>>(serializedDictionary);
                        dictionary[userId][1].RemoveAll(a => a.Id == mValues[position].Id);
                        edit.Clear();
                        edit.PutString("SavedUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                        edit.Apply();
                    }

                }
            }

            private void setFadeAnimation(View view)
            {
                int FADE_DURATION = 1400; // in milliseconds
                AlphaAnimation anim = new AlphaAnimation(0.0f, 1.0f);
                anim.Duration=FADE_DURATION;
                view.StartAnimation(anim);
            }

            private void setScaleAnimation(View view)
            {
                int FADE_DURATION = 1000; // in milliseconds
                ScaleAnimation anim = new ScaleAnimation(0.0f, 1.0f, 0.0f, 1.0f);
                anim.Duration = FADE_DURATION;
                view.StartAnimation(anim);
            }

            private void MView_Click(object sender, EventArgs e)
            {
                int position = recycleView.GetChildAdapterPosition((View)sender);

                Context context = recycleView.Context;
                Intent intent = new Intent(context, typeof(UserDetailActivity));
                intent.PutExtra("userInformation", Newtonsoft.Json.JsonConvert.SerializeObject(mValues[position]));
                context.StartActivity(intent);

            }

            private int CalculateInSampleSize(BitmapFactory.Options options, int requestedWidth, int requestedHeight)
            {
                //Raw height and width of image
                int height = options.OutHeight;
                int width = options.OutWidth;
                int inSampleSize = 1;

                if (height > requestedHeight || width > requestedWidth)
                {
                    //slika je veca nego sto nam treba
                    int halfHeight = height / 2;
                    int halfWidth = width / 2;

                    while ((halfHeight / inSampleSize) >= requestedHeight && (halfWidth / inSampleSize) >= requestedWidth)
                    {
                        inSampleSize *= 2;
                    }
                }
                Console.WriteLine();
                Console.WriteLine("SampleSizeBitmap: " + inSampleSize.ToString());
                Console.WriteLine();
                return inSampleSize;
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
           // public readonly ImageView mImageViewSavedContact;
            public readonly TextView mTxtView;
            public readonly CheckBox checkbox;

            public SimpleViewHolder(View view) : base(view)
            {
                mView = view;
                mImageView = view.FindViewById<ImageView>(Resource.Id.avatar); //profilna slika usera
                mTxtView = view.FindViewById<TextView>(Resource.Id.text1); //ime + prezime usera
                checkbox = view.FindViewById<CheckBox>(Resource.Id.checkboxSaveUserRecycleViewRow);
               
               // mImageViewSavedContact = view.FindViewById<ImageView>(Resource.Id.saveContactRecycleViewRow);
            }

            public override string ToString()
            {
                return base.ToString() + " '" + mTxtView.Text;
            }
        }


        

    }
}

















//private List<string> GetRandomSubList(List<string> items, int amount)
//{
//    List<string> list = new List<string>();
//    Random random = new Random();
//    while (list.Count < amount)
//    {
//        list.Add(items[random.Next(items.Count)]);
//    }
//    return list;
//}