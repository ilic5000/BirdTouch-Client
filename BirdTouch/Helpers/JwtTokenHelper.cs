using Android.Content;
using BirdTouch.Constants;

namespace BirdTouch.Helpers
{
    public static class JwtTokenHelper
    {
        public static bool IsUserSignedIn(Context context)
        {
            ISharedPreferences pref = context.GetSharedPreferences(
                SharedPreferencesConstants.JWT_STORAGE, FileCreationMode.Private);

            if (pref.Contains(SharedPreferencesConstants.JWT_TOKEN_KEY))
            {
                return true;
            }

            return false ;
        }

        public static void AddTokenToSharedPreferences(Context context, string token)
        {
            ISharedPreferences pref = context.GetSharedPreferences(
                SharedPreferencesConstants.JWT_STORAGE, FileCreationMode.Private);

            pref.Edit().PutString(SharedPreferencesConstants.JWT_TOKEN_KEY, token).Commit();
        }

        public static string GetTokenFromSharedPreferences(Context context)
        {
            ISharedPreferences pref = context.GetSharedPreferences(
                SharedPreferencesConstants.JWT_STORAGE, FileCreationMode.Private);

            if (!IsUserSignedIn(context))
            {
                return null;
            }

            return pref.GetString(SharedPreferencesConstants.JWT_TOKEN_KEY, string.Empty);
        }

        public static void RemoveTokenFromSharedPreferences(Context context)
        {
            ISharedPreferences pref = context.GetSharedPreferences(
                SharedPreferencesConstants.JWT_STORAGE, FileCreationMode.Private);

            if (IsUserSignedIn(context))
            {
                pref.Edit().Remove(SharedPreferencesConstants.JWT_TOKEN_KEY).Commit();
            }
        }
    }
}