using LeadHype.Api.ServiceApis;
using Newtonsoft.Json;
using OpenQA.Selenium;
using UtilityPack;
using static UtilityPack.Tasks;

namespace LeadHype.Api;

public class GoogleOAuthService : IDisposable
{
    #region Constructor

    public GoogleOAuthService(string email, string password)
    {
        Email = email;
        Password = password;
    }

    #endregion

    #region Private Fields

    private EnhancedWebDriver _driver;

    #endregion

    #region Properties

    public string Email { get; set; }
    public string Password { get; set; }

    #endregion

    #region Public Methods

    public void Connect(string port)
    {
        var driver = DriverHelper.GetDriver(port);
        _driver = new EnhancedWebDriver(driver);
    }
    
    public bool? Login(int? id, int? userId)
    {
        return Run(() => ILogin(id, userId),
            r => r is not null,
            OnError);
    }

    public bool? SmartleadOAuth()
    {
        return Run(ISmartleadOAuth,
            r => r is not null,
            OnError
        );
    }

    #endregion

    #region Private Methods

    private bool? ILogin(int? id, int? userId)
    {
        string state = JsonConvert.SerializeObject(userId.HasValue
            ? new { team_member_id = id!.Value, user_id = userId.Value }
            : new { user_id = id!.Value });

        Uri uri = Url
            .Create("https://accounts.google.com/")
            .AddPaths("o", "oauth2", "v2", "auth")
            .AddQuery("scope",
                "https://mail.google.com/ https://www.googleapis.com/auth/userinfo.profile https://www.googleapis.com/auth/userinfo.email")
            .AddQuery("prompt", "consent")
            .AddQuery("access_type", "offline")
            .AddQuery("include_granted_scopes", "true")
            .AddQuery("response_type", "code")
            .AddQuery("state", state)
            .AddQuery("client_id", "1021517043376-ipe8289dof3t2v9apjpae8hs2q9abetp.apps.googleusercontent.com")
            .AddQuery("redirect_uri", "https://server.smartlead.ai/api/email-account/gmail/callback")
            .AddQuery("flowName", "GeneralOAuthFlow")
            .Build();

        _driver.Navigate(false).GoToUrl(uri);
        
        By by = By.CssSelector("input[type='email'], input[type='password']");

        if (!_driver.WaitforElement(by, 30, ElementMode.Visible, out IWebElement? element) ||
            element is null)
            return null;

        string? typeAttribute = element.GetAttribute("type");

        if (typeAttribute?.Equals("email", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            element = _driver!.FindElement(By.CssSelector("input[type='email']"), false);
            _driver.SetAttributeJs(element, "value", Email);

            element = _driver.FindElement(By.XPath("//button//span[text()='Next']"), false);
            _driver.JsClick(element);
        }

        if (!_driver.WaitforElement(By.CssSelector("input[type='password']"), 60, ElementMode.Visible, out element))
            return null;

        _driver.SetAttributeJs(element, "value", Password);

        by = By.XPath("//button//span[text()='Next']");
        if(!_driver.WaitforElement(by, 30, ElementMode.Clickable, out element))
            return null;
        
        _driver.JsClick(element);
        
        string pageUrl = WaitforPages("https://accounts.google.com/v3/signin/challenge");
        return !string.IsNullOrEmpty(pageUrl) &&
               pageUrl.Contains("/signin/oauth/id", StringComparison.OrdinalIgnoreCase);
    }

    private bool? ISmartleadOAuth()
    {
        /*
           OLD WAY:
            By.CssSelector($"div[data-email*='{Email}' i]")
        */

        By by = By.XPath("//span[text()='Continue']");
        if (!_driver.WaitforElement(by, 30,
                ElementMode.Clickable, out IWebElement? element))
            return false;

        _driver.JsClick(element);
        
        //Wait for 2 seconds
        Thread.Sleep(TimeSpan.FromSeconds(2));
        
        // by = By.CssSelector($"form ul>li>div[data-identifier*='{Email}' i]");
        // if (!_driver.WaitforElement(by, 15,
        //         ElementMode.Clickable, out element))
        //     return false;
        //
        // _driver.JsClick(element);
        //
        // if (!_driver.WaitforElement(By.XPath("//span[contains(text(),'Continue')]"), 15, ElementMode.Clickable,
        //         out element) ||
        //     element is null)
        //     return false;
        //
        // _driver.JsClick(element);

        if (!_driver.WaitforElement(By.XPath("//span[contains(text(),'Allow')]"), 15, ElementMode.Clickable,
                out element) ||
            element is null)
            return false;

        _driver.JsClick(element);

        return _driver.WaitForUrl("https://app.smartlead.ai/", 25);
    }

    #endregion

    #region Helper Methods

    private string WaitforPages(string oldUrl, int timeoutSeconds = 30)
    {
        return _driver.WaitForUrls(oldUrl, TimeSpan.FromSeconds(timeoutSeconds),
            "/speedbump/gaplustos",
            "/speedbump/changepassword",
            "/ac/domains/manage",
            "/ac/billing/interstitial/pendingtermsofservices", //https://admin.google.com/u/0/ac/home
            "/ac/home", //https://admin.google.com/u/0/ac/home
            "/u/0/",
            "/ac/accountchooser",
            "/signin/rejected",
            "/confirmidentifier",
            "/signin/oauth/id"
        );
    }

    private void OnError(Exception obj)
    {
        DI.Logger.LogError(obj);
    }

    #endregion

    public void Dispose()
    {
        _driver?.Dispose();
    }
}