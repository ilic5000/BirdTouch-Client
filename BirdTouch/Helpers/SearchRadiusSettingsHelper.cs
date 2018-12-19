using Android.Content;
using BirdTouch.Constants;

namespace BirdTouch.Helpers
{
    public class SearchRadiusSettingsHelper
    {
        public static void AddSearchRadiusToSharedPreferences(Context context, string radius)
        {
            ISharedPreferences pref = context.GetSharedPreferences(
                SharedPreferencesConstants.SEARCH_RADIUS_STORAGE, FileCreationMode.Private);

            pref.Edit().PutString(SharedPreferencesConstants.SEARCH_RADIUS_KEY, radius).Commit();
        }

        public static int GetSearchRadiusFromSharedPreferences(Context context)
        {
            ISharedPreferences pref = context.GetSharedPreferences(
                SharedPreferencesConstants.SEARCH_RADIUS_STORAGE, FileCreationMode.Private);

            return int.Parse(pref.GetString(SharedPreferencesConstants.SEARCH_RADIUS_KEY, "50"));
        }

        public static double GetSearchRadiusInKm(Context context)
        {
            var radiusInMeters = GetSearchRadiusFromSharedPreferences(context);
            return radiusInMeters / 1000.0;
        }
    }
}