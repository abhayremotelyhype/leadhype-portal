using Newtonsoft.Json.Linq;

namespace LeadHype.Api
{
    public static class StringExtensions
    {
        #region Extension Methods
        public static bool IsJson(this string rawJson, out JObject? json)
        {
            try
            {
                json = JObject.Parse(rawJson);
                return true;
            }
            catch
            {
                json = null;
                return false;
            }
        }

        public static bool IsJsonArray(this string rawJson, out JArray? jsonArray)
        {
            try
            {
                jsonArray = JArray.Parse(rawJson);
                return true;
            }
            catch
            {
                jsonArray = null;
                return false;
            }
        }

        public static bool Contains(this string? source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        #endregion
    }
}
