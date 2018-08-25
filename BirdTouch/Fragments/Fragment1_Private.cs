using System;
using System.Collections.Generic;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using SupportFragment = Android.Support.V4.App.Fragment;
using Android.Support.V7.Widget;
using Android.Graphics;
using Android.Content.Res;
using Android.Support.Design.Widget;
using Android.Locations;
using System.Linq;
using Android.Runtime;
using System.Net;
using System.Collections.Specialized;
using Android.Text;
using System.Text;
using BirdTouch.Models;
using Android.Views.Animations;
using BirdTouch.Helpers;

namespace BirdTouch.Fragments
{
    public class Fragment1_Private : SupportFragment, ILocationListener
    {
        public static SwitchCompat switchVisibility;

        private Clans.Fab.FloatingActionButton fab_menu_refresh;
        private Clans.Fab.FloatingActionButton fab_menu_gps;
        private Clans.Fab.FloatingActionMenu fab_menu;

        private FrameLayout frameLay;
        private LinearLayout linearLayout;
        private RecyclerView recycleView;

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
        private bool GpsUpdateIndeterminate = false;
        private List<UserInfoModel> listOfUsersAroundMe;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {

            View view = inflater.Inflate(Resource.Layout.Fragment1_private, container, false);

            recycleView = view.FindViewById<RecyclerView>(Resource.Id.recyclerViewPrivate);
            progressBarLocation = view.FindViewById<ProgressBar>(Resource.Id.progressBarGetLocation);
            progressBarGetPrivateUsers = view.FindViewById<ProgressBar>(Resource.Id.progressBarGetPrivateUsers);
            frameLay = view.FindViewById<FrameLayout>(Resource.Id.coordinatorLayoutPrivate);
            linearLayout = view.FindViewById<LinearLayout>(Resource.Id.fragment1LinearLayoutWrapper);
            webClientMakeUserVisible = new WebClient();
            webClientMakeUserInvisible = new WebClient();
            webClientGetPrivateUsersNearMe = new WebClient();

            webClientMakeUserVisible.DownloadDataCompleted += WebClientMakeUserVisible_DownloadDataCompleted;
            webClientMakeUserInvisible.DownloadDataCompleted += WebClientMakeUserInvisible_DownloadDataCompleted;
            webClientGetPrivateUsersNearMe.DownloadDataCompleted += WebClientGetPrivateUsersNearMe_DownloadDataCompleted;



            listOfUsersAroundMe = new List<UserInfoModel>();

            switchVisibility = view.FindViewById<SwitchCompat>(Resource.Id.activatePrivateSwitch);
            fab_menu_refresh = view.FindViewById<Clans.Fab.FloatingActionButton>(Resource.Id.fab_menu_refresh_private);

            fab_menu_gps = view.FindViewById<Clans.Fab.FloatingActionButton>(Resource.Id.fab_menu_gps_private);
            fab_menu = view.FindViewById<Clans.Fab.FloatingActionMenu>(Resource.Id.fab_menu_private);

            switchVisibility.CheckedChange += SwitchVisibility_CheckedChange;
            fab_menu_refresh.Click += Fab_menu_refresh_Click;

            fab_menu_gps.Click += Fab_menu_gps_Click;

            fab_menu.MenuToggle += Fab_menu_MenuToggle;

            fab_menu.Visibility = ViewStates.Gone;

            SetUpRecyclerView(recycleView, listOfUsersAroundMe);//inicijalizacija, nema veze to sto je lista Usera prazna

            return view;
        }



        private void Fab_menu_MenuToggle(object sender, Clans.Fab.FloatingActionMenu.MenuToggleEventArgs e)
        {
            if (e.Opened) //kada se otvori fab menu, recycle view da se sakrije, kako se ne bi kliknulo greskom. a i zbog preglednosti
            {

                linearLayout.Click += linearLayoutClick;
                recycleView.Visibility = ViewStates.Invisible;

            }
            else
            {
                linearLayout.Click -= linearLayoutClick;
                recycleView.Visibility = ViewStates.Visible;

            }
        }

        private void linearLayoutClick(object sender, EventArgs e)
        {
            fab_menu.Close(true);
        }


        private void Fab_menu_gps_Click(object sender, EventArgs e)
        {
            if (GpsUpdateIndeterminate) //ako je u toku trazenje GPS a hocemo da odustanemo
            {
                fab_menu_gps.SetIndeterminate(false);
                locationManager.RemoveUpdates(this);
                GpsUpdateIndeterminate = false;
            }
            else
            {
            GpsUpdateIndeterminate = true;
            fab_menu_gps.SetIndeterminate(true);
            locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this);
            }
        }

        private void Fab_menu_automatically_Click(object sender, EventArgs e) //treba implementirati logiku, ali treba videti i razmisliti dobro sta i kako, za sada nije vidljivo dugme, mozda u buducnosti
        {

            fab_menu.Close(true);
        }

        private void Fab_menu_refresh_Click(object sender, EventArgs e)
        {
            GetPrivateUsersNearMe();
            fab_menu.Close(true);
        }


        private void SwitchVisibility_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (e.IsChecked)
            {
                InitializeLocationManager(); //svaki put kada se promeni switch na ON, da se vidi da li postoji GPS ili NETWORK location
                if (!_locationProvider.Equals(string.Empty))//ako postoje provideri za lokaciju koje smo dobili u InitializeLocationManager()
                {
                    locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this);
                    progressBarLocation.Visibility = ViewStates.Visible;
                }
                else
                {
                    Snackbar.Make(frameLay, Android.Text.Html.FromHtml("<font color=\"#ffffff\">No GPS or Network Geolocation available</font>"), Snackbar.LengthLong).Show();
                }

            }
            else
            {

                fab_menu.Visibility = ViewStates.Gone;
                locationManager.RemoveUpdates(this);
                GoInvisible();
                progressBarLocation.Visibility = ViewStates.Invisible;
            }
        }

        private void InitializeLocationManager()//ovde treba videti kako uzeti lepo lokaciju sa mreze ako nema gps, ali to nije za betu
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
                Snackbar.Make(frameLay, Android.Text.Html.FromHtml("<font color=\"#ffffff\">Unable to determine location</font>"), Snackbar.LengthLong).Show();

            }
            else
            {
                SendLocationToDatabase();
            }
            locationManager.RemoveUpdates(this); //samo jednom da uzme gps koordinate, da ne refreshuje stalno

        }

        private void SendLocationToDatabase()
        {
            if (Reachability.IsOnline(Activity) && !webClientMakeUserVisible.IsBusy)
            {

                //insert parameters for header for web request
                NameValueCollection parameters = new NameValueCollection();
                parameters.Add("latitude", currLocation.Latitude.ToString().Replace(',','.'));
                parameters.Add("longitude", currLocation.Longitude.ToString().Replace(',', '.'));
                parameters.Add("mode", ActiveModes.PRIVATE); // mozda treba mode globalni, ali videcemo
                parameters.Add("id", StartPageActivity.user.Id.ToString());

                String restUriString = GetString(Resource.String.webapi_endpoint_makeUserVisible);
                uri = new Uri(restUriString);

                webClientMakeUserVisible.Headers.Clear();
                webClientMakeUserVisible.Headers.Add(parameters);
                webClientMakeUserVisible.DownloadDataAsync(uri);

            }
            else
            {

                Snackbar.Make(frameLay, Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"), Snackbar.LengthLong).Show();

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
                Snackbar.Make(frameLay, Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"), Snackbar.LengthLong).Show();
                visible = false;
                fab_menu_gps.SetIndeterminate(false);
                fab_menu.Close(true);
            }
            else
            {

                Console.WriteLine("Success!");
                string jsonResult = Encoding.UTF8.GetString(e.Result);
                Console.Out.WriteLine(jsonResult);
                visible = true;
                progressBarLocation.Visibility = ViewStates.Invisible;

                fab_menu_gps.SetIndeterminate(false);
                GpsUpdateIndeterminate = false;
                fab_menu.Close(true);

               //Snackbar.Make(frameLay, Html.FromHtml("<font color=\"#ffffff\">" + StartPageActivity.user.Username.ToString() + " is now visible in private mode</font>"), Snackbar.LengthLong).Show();

                GetPrivateUsersNearMe();//nakon uspesnog postavljanja lokacije, a mozda i da se skloni odavde, pa samo po potrebi
            }
        }



        private void GoInvisible()
        {
            if (Reachability.IsOnline(Activity) && !webClientMakeUserInvisible.IsBusy)
            {

                //insert parameters for header for web request
                NameValueCollection parameters = new NameValueCollection();
                parameters.Add("mode", "1");
                parameters.Add("id", StartPageActivity.user.Id.ToString());

                String restUriString = GetString(Resource.String.webapi_endpoint_makeUserInvisible);
                uri = new Uri(restUriString);

                webClientMakeUserInvisible.Headers.Clear();
                webClientMakeUserInvisible.Headers.Add(parameters);
                webClientMakeUserInvisible.DownloadDataAsync(uri);

            }
            else
            {

                Snackbar.Make(frameLay, Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"), Snackbar.LengthLong).Show();

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
                Snackbar.Make(frameLay, Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"), Snackbar.LengthLong).Show();
                visible = true;
            }
            else
            {

                Console.WriteLine("Success!");
                string jsonResult = Encoding.UTF8.GetString(e.Result);
                Console.Out.WriteLine(jsonResult);
                visible = false;
              //  Snackbar.Make(frameLay, Html.FromHtml("<font color=\"#ffffff\">" + StartPageActivity.user.Username.ToString() + " is now invisible in private mode</font>"), Snackbar.LengthLong).Show();
            }
        }


        private void GetPrivateUsersNearMe()
        {

            if (Reachability.IsOnline(Activity) && !webClientGetPrivateUsersNearMe.IsBusy)
            {


                if (visible) { //ako je korisnik visible tj. u active_users bazi upisan

                progressBarGetPrivateUsers.Visibility = ViewStates.Visible;

                //insert parameters for header for web request
                NameValueCollection parameters = new NameValueCollection();
                parameters.Add("id", StartPageActivity.user.Id.ToString());

                String restUriString = GetString(Resource.String.webapi_endpoint_getPrivateUsersNearMe);
                uri = new Uri(restUriString);

                webClientGetPrivateUsersNearMe.Headers.Clear();
                webClientGetPrivateUsersNearMe.Headers.Add(parameters);
                webClientGetPrivateUsersNearMe.DownloadDataAsync(uri);
                }
                else
                {
                    Snackbar.Make(frameLay, Html.FromHtml("<font color=\"#ffffff\">You are not visible to others</font>"), Snackbar.LengthLong).Show();

                }

            }
            else
            {

                Snackbar.Make(frameLay, Html.FromHtml("<font color=\"#ffffff\">No connectivity, check your network</font>"), Snackbar.LengthLong).Show();

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
                Snackbar.Make(frameLay, Html.FromHtml("<font color=\"#ffffff\">Error has occurred</font>"), Snackbar.LengthLong).Show();
                progressBarGetPrivateUsers.Visibility = ViewStates.Gone;
            }
            else
            {

                Console.WriteLine("Success!");
                string jsonResult = Encoding.UTF8.GetString(e.Result);

                List<UserInfoModel> newListOfUsersAroundMe = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UserInfoModel>>(jsonResult);
                SetUpRecyclerView(recycleView, newListOfUsersAroundMe);

                fab_menu.Visibility = ViewStates.Visible;
                progressBarGetPrivateUsers.Visibility = ViewStates.Gone;
            }
        }








        //*********************************************************
        //LocationListener interfejs, mozda nekad implementirati

        public void OnProviderDisabled(string provider)
        {
            Snackbar.Make(frameLay, Android.Text.Html.FromHtml("<font color=\"#ffffff\">" + provider + " is disabled</font>"), Snackbar.LengthLong).Show();

        }

        public void OnProviderEnabled(string provider)
        {
            Snackbar.Make(frameLay, Android.Text.Html.FromHtml("<font color=\"#ffffff\">" + provider + " is enabled</font>"), Snackbar.LengthLong).Show();

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


        private void SetUpRecyclerView(RecyclerView recyclerView, List<UserInfoModel> listOfUsersAroundMe) //ovde da se napravi lista dobijenih korisnika
        {

            recyclerView.SetLayoutManager(new LinearLayoutManager(recyclerView.Context));
            recyclerView.SetAdapter(new SimpleStringRecyclerViewAdapter(recyclerView.Context, listOfUsersAroundMe, Activity.Resources, recycleView));

        }


        public void NotifyDataSetChangedFromAnotherFragment()
        {
            recycleView.GetAdapter().NotifyDataSetChanged();
        }


        public class SimpleStringRecyclerViewAdapter : RecyclerView.Adapter
        {
            private readonly TypedValue mTypedValue = new TypedValue();
            private int mBackground;
            private List<UserInfoModel> mValues;
            private RecyclerView recycleView;
            Resources mResource;

            public SimpleStringRecyclerViewAdapter(Context context, List<UserInfoModel> items, Resources res, RecyclerView rv)
            {
                context.Theme.ResolveAttribute(Resource.Attribute.selectableItemBackground, mTypedValue, true);
                mBackground = mTypedValue.ResourceId;
                mValues = items;
                mResource = res;
                recycleView = rv;

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

               // Random rand = new Random(); //igramo se, ali pravi probleme
                //if(rand.Next() % 2 == 1)
               // setScaleAnimation(holder.ItemView);
                //else
                //setFadeAnimation(holder.ItemView);

                simpleHolder.checkbox.CheckedChange -= Checkbox_CheckedChange;//stavljeno ovde da ne bi redovi ispod izazvali pozivanje event listenera

                if (isUserInSavedContacts(mValues[position].Id, simpleHolder)) simpleHolder.checkbox.Checked = true; //da li je vec u saved contacts
                else simpleHolder.checkbox.Checked = false;

                simpleHolder.checkbox.Tag = simpleHolder.mView; //potrebno zbog pozicije u adapteru koja nam je potrebna u Checkbox_CheckedChange
                simpleHolder.checkbox.CheckedChange += Checkbox_CheckedChange;
            }


            private bool isUserInSavedContacts(Guid userIdRecyclerView, SimpleViewHolder svh)
            {
                Guid userId = StartPageActivity.user.Id;

                ISharedPreferences pref = svh.ItemView.Context.ApplicationContext.GetSharedPreferences("SavedUsers", FileCreationMode.Private);
                ISharedPreferencesEditor edit = pref.Edit();

                List<UserInfoModel> listSavedPrivateUsers = new List<UserInfoModel>();

                if (pref.Contains("SavedPrivateUsersDictionary"))
                {
                    string serializedDictionary = pref.GetString("SavedPrivateUsersDictionary", String.Empty);
                    if (serializedDictionary != String.Empty)
                    {
                        var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                            <Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>>(serializedDictionary);

                        if (dictionary.ContainsKey(userId))
                        {//ako je user dodavao usere
                            if (dictionary[userId].ContainsKey(1))
                            {//ako je dodavao private usere
                                listSavedPrivateUsers = dictionary[userId][1];
                            }
                        }

                    }

                }

                if (!(listSavedPrivateUsers.Find(a => a.Id == userIdRecyclerView) == null)) return true;
                else return false;
            }

            private void Checkbox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
            {
                CheckBox vsender = sender as CheckBox;

                View mView = (View)vsender.Tag;
                int position = recycleView.GetChildAdapterPosition(mView);
                Guid userId = StartPageActivity.user.Id;

                ISharedPreferences pref = vsender.Context.ApplicationContext.GetSharedPreferences("SavedUsers", FileCreationMode.Private);
                ISharedPreferencesEditor edit = pref.Edit();

                if (e.IsChecked)
                {//checked

                    if (!pref.Contains("SavedPrivateUsersDictionary")) //prvi put u aplikaciji dodajemo private usera u saved
                    {

                        var dictionary = new Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>();

                        dictionary.Add(userId, new Dictionary<int, List<UserInfoModel>>());
                        dictionary[userId].Add(1, new List<UserInfoModel>());// 1 je private mode
                        dictionary[userId][1].Add(mValues[position]);

                        edit.Remove("SavedPrivateUsersDictionary");
                        edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                        edit.Apply();
                        Fragment1_PrivateSavedUsers refToSavedUsersFragment = (Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(1);
                        refToSavedUsersFragment.SetUpRecyclerView();

                    }
                    else //vec postoji dictionary
                    {
                        string serializedDictionary = pref.GetString("SavedPrivateUsersDictionary", String.Empty);
                        if (serializedDictionary != String.Empty)
                        {

                           var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>>(serializedDictionary);

                            if (!dictionary.ContainsKey(userId))
                            {//ako user nije uopste dodavao usere
                                dictionary.Add(userId, new Dictionary<int, List<UserInfoModel>>());
                            }
                            if (!dictionary[userId].ContainsKey(1))
                            {//ako nije dodavao private usere
                                dictionary[userId].Add(1, new List<UserInfoModel>());
                            }

                            //samo dodamo private usera iz recyclerViewa
                            dictionary[userId][1].Add(mValues[position]);
                            edit.Remove("SavedPrivateUsersDictionary");
                            edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                            edit.Apply();
                            Fragment1_PrivateSavedUsers refToSavedUsersFragment =(Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(1);
                            refToSavedUsersFragment.SetUpRecyclerView();
                        }

                    }

                }
                else
                {//unchecked

                    string serializedDictionary = pref.GetString("SavedPrivateUsersDictionary", String.Empty);
                    if (serializedDictionary != String.Empty)
                    {
                        var dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject
                            <Dictionary<Guid, Dictionary<int, List<UserInfoModel>>>>(serializedDictionary);

                        dictionary[userId][1].RemoveAll(a => a.Id == mValues[position].Id);
                        edit.Remove("SavedPrivateUsersDictionary");
                        edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                        edit.Apply();
                        Fragment1_PrivateSavedUsers refToSavedUsersFragment = (Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(1);
                        refToSavedUsersFragment.SetUpRecyclerView();
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
                SimpleViewHolder svh = (SimpleViewHolder)recycleView.GetChildViewHolder((View)sender);

                Context context = recycleView.Context;
                Intent intent = new Intent(context, typeof(UserDetailActivity));
                intent.PutExtra("userInformation", Newtonsoft.Json.JsonConvert.SerializeObject(mValues[position]));
                intent.PutExtra("isSaved", (svh.checkbox.Checked));
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
            public readonly TextView mTxtView;
            public readonly CheckBox checkbox;

            public SimpleViewHolder(View view) : base(view)
            {
                mView = view;
                mImageView = view.FindViewById<ImageView>(Resource.Id.avatar); //profilna slika usera
                mTxtView = view.FindViewById<TextView>(Resource.Id.text1); //ime + prezime usera
                checkbox = view.FindViewById<CheckBox>(Resource.Id.checkboxSaveUserRecycleViewRow);
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