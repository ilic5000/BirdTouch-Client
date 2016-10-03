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
using BirdTouch.Models;
using Android.Views.Animations;

namespace BirdTouch.Fragments
{
    public class Fragment1_PrivateSavedUsers : SupportFragment
    {
        private FrameLayout frameLay;
        private RecyclerView recycleView;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.Fragment1_private_savedUsers, container, false);
            recycleView = view.FindViewById<RecyclerView>(Resource.Id.recyclerViewPrivateSavedUsers);
            frameLay = view.FindViewById<FrameLayout>(Resource.Id.coordinatorLayoutPrivateSavedUsers);

            SetUpRecyclerView();
            return view;
        }


        public void SetUpRecyclerView() //ovde da se napravi lista dobijenih korisnika
        {
            int userId = StartPageActivity.user.Id;
            List<User> listSavedPrivateUsers = new List<User>();

            ISharedPreferences pref = Context.ApplicationContext.GetSharedPreferences("SavedUsers", FileCreationMode.Private);
            ISharedPreferencesEditor edit = pref.Edit();

            if (pref.Contains("SavedPrivateUsersDictionary")) //prvi put u aplikaciji dodajemo private usera u saved
            {
                string serializedDictionary = pref.GetString("SavedPrivateUsersDictionary", String.Empty);
                if (serializedDictionary != String.Empty)
                {

                    Dictionary<int, Dictionary<int, List<User>>> dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, List<User>>>>(serializedDictionary);
                    if (dictionary.ContainsKey(userId))
                    {//ako je user dodavao usere
                        if (dictionary[userId].ContainsKey(1))
                        {//ako je dodavao private usere
                           listSavedPrivateUsers = dictionary[userId][1];
                        }
                    }
                                     
                }

            }

            
            recycleView.SetLayoutManager(new LinearLayoutManager(recycleView.Context));
            recycleView.SetAdapter(new SimpleStringRecyclerViewAdapter(recycleView.Context, listSavedPrivateUsers, Activity.Resources, recycleView));

        }





        //*****************************************************************
        //RecycleView classes


        public class SimpleStringRecyclerViewAdapter : RecyclerView.Adapter
        {
            private readonly TypedValue mTypedValue = new TypedValue();
            private int mBackground;
            private List<User> mValues;
            private RecyclerView recycleView;
            Resources mResource;
            
            public SimpleStringRecyclerViewAdapter(Context context, List<User> items, Resources res, RecyclerView rv)
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
                    simpleHolder.mImageView.SetImageBitmap(Bitmap.CreateScaledBitmap(bm, 200, 100, false));// mozda treba malo da se igra sa ovim
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
               // if (rand.Next() % 2 == 1)
               //     setScaleAnimation(holder.ItemView);
               // else setFadeAnimation(holder.ItemView);

                simpleHolder.checkbox.Checked = true;
                simpleHolder.checkbox.Tag = simpleHolder.mView; //kako bih prosledio poziciju checkboxu
                simpleHolder.checkbox.CheckedChange -= Checkbox_CheckedChange;
                simpleHolder.checkbox.CheckedChange += Checkbox_CheckedChange;
            }

            private void Checkbox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
            {

                CheckBox vsender = sender as CheckBox;

                View mView = (View)vsender.Tag;
                int position = recycleView.GetChildAdapterPosition(mView);
                int userId = StartPageActivity.user.Id;
              
                ISharedPreferences pref = vsender.Context.ApplicationContext.GetSharedPreferences("SavedUsers", FileCreationMode.Private);
                ISharedPreferencesEditor edit = pref.Edit();

                if (e.IsChecked)
                {//checked

                    if (!pref.Contains("SavedPrivateUsersDictionary")) //prvi put u aplikaciji dodajemo private usera u saved
                    {       
                        Dictionary<int, Dictionary<int, List<User>>> dictionary = new Dictionary<int, Dictionary<int, List<User>>>();
                        dictionary.Add(userId, new Dictionary<int, List<User>>());
                        dictionary[userId].Add(1, new List<User>());// 1 je private mode
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
                            edit.Remove("SavedPrivateUsersDictionary");
                            edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                            edit.Apply();
                            Fragment1_PrivateSavedUsers refToSavedUsersFragment = (Fragment1_PrivateSavedUsers)StartPageActivity.adapter.GetItem(1);
                            refToSavedUsersFragment.SetUpRecyclerView();

                        }

                    }

                }
                else
                {//unchecked

                    string serializedDictionary = pref.GetString("SavedPrivateUsersDictionary", String.Empty);
                    if (serializedDictionary != String.Empty)
                    {
                        Dictionary<int, Dictionary<int, List<User>>> dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, List<User>>>>(serializedDictionary);
                        dictionary[userId][1].RemoveAll(a => a.Id == mValues[position].Id);
                        edit.Remove("SavedPrivateUsersDictionary");
                        edit.PutString("SavedPrivateUsersDictionary", Newtonsoft.Json.JsonConvert.SerializeObject(dictionary));
                        edit.Apply();
                        mValues = dictionary[userId][1];
                        NotifyItemRemoved(position);
                        Fragment1_Private refToSavedUsersFragment = (Fragment1_Private)StartPageActivity.adapter.GetItem(0);
                        refToSavedUsersFragment.NotifyDataSetChangedFromAnotherFragment();
                       
                    }

                }
            }

            private void setFadeAnimation(View view)
            {
                int FADE_DURATION = 1400; // in milliseconds
                AlphaAnimation anim = new AlphaAnimation(0.0f, 1.0f);
                anim.Duration = FADE_DURATION;
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