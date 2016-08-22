using Android.OS;
using Android.Views;
using SupportFragment = Android.Support.V4.App.Fragment;

namespace BirdTouch.Fragments
{
    public class Fragment3_Celebrity : SupportFragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            // return inflater.Inflate(Resource.Layout.YourFragment, container, false);

            View view = inflater.Inflate(Resource.Layout.Fragment3_celebrity, container, false);

            return view;
        }
    }
}