using System.Net;

namespace LeadHype.Api;

public static class CookieContainerExtensions
{
    public static void Clear(this CookieContainer container)
    {
        var allCookies = container.GetAllCookies();
        foreach (Cookie cookie in allCookies)
        {
            cookie.Expires = DateTime.Now.AddYears(-1);
            cookie.Expired = true;
        }
    }
}