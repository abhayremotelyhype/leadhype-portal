using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace LeadHype.Api.ServiceApis;

public class EnhancedWebDriver : IDisposable
{
    #region Default Constructor
    public EnhancedWebDriver(IWebDriver driver)
    {
        _driver = driver;
        webDriverWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));
    }
    #endregion

    #region Private Fields

    private IWebDriver _driver;
    private WebDriverWait? webDriverWait;

    #endregion
    
    #region Events

    public Action<string?>? BeforeFindElement { get; set; }

    #endregion

    #region Methods

    public INavigation? Navigate(bool invokeEvent = true)
    {
        if (invokeEvent)
            BeforeFindElement?.Invoke(_driver?.Url);

        return _driver?.Navigate();
    }

    public void ClearCookies()
    {
        _driver.Manage().Cookies.DeleteAllCookies();
    }

    public IWebElement? FindElement(By by, bool invokeEvent = true)
    {
        if (invokeEvent)
            BeforeFindElement?.Invoke(_driver?.Url);

        return _driver?.FindElement(by);
    }

    public bool WaitforElement(By by, int timeSeconds, ElementMode elementMode, out IWebElement? ele)
    {
        webDriverWait!.PollingInterval = TimeSpan.FromSeconds(1);
        webDriverWait.Timeout = TimeSpan.FromSeconds(timeSeconds);

        try
        {
            ele = elementMode switch
            {
                ElementMode.Found => webDriverWait.Until(ExpectedConditions.ElementExists(by)),
                ElementMode.Visible => webDriverWait.Until(ExpectedConditions.ElementIsVisible(by)),
                _ => webDriverWait.Until(ExpectedConditions.ElementToBeClickable(by))
            };
            
            return true;
        }
        catch
        {
            ele = null;
            return false;
        }
    }

    
    public bool WaitForUrl(string url, int timeSeconds)
    {
        webDriverWait!.PollingInterval = TimeSpan.FromSeconds(2);
        webDriverWait.Timeout = TimeSpan.FromSeconds(timeSeconds);
        return webDriverWait.Until(ExpectedConditions.UrlContains(url));
    }

    public string WaitForUrls(string oldUrl, TimeSpan timeSpan, params string[] urls)
    {
        webDriverWait!.PollingInterval = TimeSpan.FromSeconds(2);
        webDriverWait.Timeout = timeSpan;

        bool result = webDriverWait.Until(IsUrlOneOf(oldUrl, urls));
        return result ? _driver.Url : string.Empty;
    }

    public object? ExecuteScript(string javascript, params object[] argument)
    {
        IJavaScriptExecutor? js = _driver as IJavaScriptExecutor;
        return js?.ExecuteScript(javascript, argument);
    }

    public void JsClick(IWebElement? element)
    {
        if (element is null)
            return;

        ExecuteScript("arguments[0].click();", element);
    }

    public void TryJsClick(IWebElement element)
    {
        TryInvokeAction(() => JsClick(element));
    }
    
    /// <summary>
    /// Sets attribute a provided value through javascript
    /// </summary>
    /// <param name="element">Element</param>
    /// <param name="attributeName">Attribute Name</param>
    /// <param name="value">Attribute Value</param>
    public void SetAttributeJs(IWebElement? element, string? attributeName, string? value)
    {
        if (element is null || string.IsNullOrEmpty(attributeName) || string.IsNullOrEmpty(value))
            return;

        ExecuteScript($"arguments[0].setAttribute('{attributeName}', arguments[1]);", element, value);
    }
    #endregion
    
    #region Helper Methods
    private static void TryInvokeAction(Action action)
    {
        try
        {
            action.Invoke();
        }
        catch
        {
            // ignored
        }
    }
    
    private static Func<IWebDriver, bool> IsUrlOneOf(string oldUrl, params string[] urls)
    {
        return (driver) =>
        {
            string currentUrl = driver.Url;
            return urls.Any(url => !url.Contains(oldUrl) && currentUrl.Contains(url, StringComparison.OrdinalIgnoreCase));
        };
    }
    #endregion

    public void Dispose()
    {
        _driver?.Dispose();
    }
}