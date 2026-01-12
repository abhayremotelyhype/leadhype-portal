using Microsoft.Win32;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace LeadHype.Api.ServiceApis
{
    public class DriverHelper
    {
        public static IWebDriver GetDriver(string port)
        {
            if (string.IsNullOrEmpty(port))
            {
                throw new ArgumentNullException(nameof(port));
            }

            string multiloginUrl = $"http://localhost:{port}";

            ICapabilities capabilities = new ChromeOptions()
                .ToCapabilities();

            return new RemoteWebDriver(new Uri(multiloginUrl), capabilities);
        }
    }
}