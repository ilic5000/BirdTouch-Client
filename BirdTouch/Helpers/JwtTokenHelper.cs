using Android.Content;
using BirdTouch.Constants;

namespace BirdTouch.Helpers
{
    public static class JwtTokenHelper
    {
        public static bool IsUserSignedIn(Context context)
        {
            ISharedPreferences pref = context.GetSharedPreferences(
                SharedPreferencesConstants.JWTSTORAGE, FileCreationMode.Private);

            if (pref.Contains(SharedPreferencesConstants.JWTTOKENKEY))
            {
                return true;
            }

            return false ;
        }

        public static void AddTokenToSharedPreferences(Context context, string token)
        {
            ISharedPreferences pref = context.GetSharedPreferences(
                SharedPreferencesConstants.JWTSTORAGE, FileCreationMode.Private);

            pref.Edit().PutString(SharedPreferencesConstants.JWTTOKENKEY, token).Commit();
        }

        public static string GetTokenFromSharedPreferences(Context context)
        {
            ISharedPreferences pref = context.GetSharedPreferences(
                SharedPreferencesConstants.JWTSTORAGE, FileCreationMode.Private);

            if (!IsUserSignedIn(context))
            {
                return null;
            }

            return pref.GetString(SharedPreferencesConstants.JWTTOKENKEY, string.Empty);
        }

        public static void RemoveTokenFromSharedPreferences(Context context)
        {
            ISharedPreferences pref = context.GetSharedPreferences(
                SharedPreferencesConstants.JWTSTORAGE, FileCreationMode.Private);

            if (IsUserSignedIn(context))
            {
                pref.Edit().Remove(SharedPreferencesConstants.JWTTOKENKEY).Commit();
            }
        }
    }
}