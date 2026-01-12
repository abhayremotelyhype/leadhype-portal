using System.Text.Json;
using LeadHype.Api.ServiceApis;

namespace LeadHype.Api.Managers;

public class TaskManager
{
    #region Default Constructor
    public TaskManager(MultiLogin multiLogin, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _multiLogin = multiLogin;
        _tasks = new Dictionary<int, TaskModel>();
        _semaphoreSlim = new SemaphoreSlim(1, 1);
    }
    #endregion

    #region Private Fields

    private int _id = 1000;
    private SemaphoreSlim _semaphoreSlim;
    private Dictionary<int, TaskModel> _tasks;
    private MultiLogin _multiLogin;
    private HttpClient _httpClient;
    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a task
    /// </summary>
    /// <param name="request"></param>
    /// <param name="id"></param>
    /// <param name="userId"></param>
    /// <returns>Returns Task Id</returns>
    public int Create(OAuthRequest request, int? id, int? userId)
    {
        TaskModel task = CreateTaskId();
        
        void UpdateTask(bool isSuccess, string message, bool isCompleted)
        {
            _semaphoreSlim.Wait();
            try
            {
                task.IsSuccess = isSuccess;
                task.Message = message;
                task.IsCompleted = isCompleted;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        Task.Run(() =>
        {
            using GoogleOAuthService oauthService = new GoogleOAuthService(request.WorkspaceAccount!.Email ?? "", request.WorkspaceAccount!.Password ?? "");

            //Create multilogin profile
            bool isMultiloginRunning = _multiLogin.IsMultiLoginRunning();
            if (!isMultiloginRunning)
            {
                UpdateTask(false, "MultiLogin is not running", true);
                return;
            }
            
            QuickProfileResponse? quickProfileResponse = _multiLogin.StartQuickProfile(request.Proxy);

            try
            {
                if (quickProfileResponse is not { IsSuccess: true })
                {
                    UpdateTask(false, "Failed to start quick profile on multilogin", true);
                    return;
                }

                string port = quickProfileResponse.Port;
                oauthService.Connect(port);

                bool? isLoggedIn = oauthService.Login(id, userId);
                if (!isLoggedIn.GetValueOrDefault())
                {
                    UpdateTask(false, "Failed to login to workspace account", true);
                    return;
                }
            
                bool? isOAuthSuccessful = oauthService.SmartleadOAuth();

                if (isOAuthSuccessful.GetValueOrDefault())
                {
                    UpdateTask(true, "OAuth successful", true);
                    CallbackUrl(request.CallbackUrl, task);
                }
                else
                {
                    UpdateTask(false, "OAuth failed", true);
                }
            }
            finally
            {
                _multiLogin.Stop(quickProfileResponse?.Id);
            }
        });

        return task.Id;
    }

    public TaskModel? GetTaskById(int id)
    {
        _semaphoreSlim.Wait();

        try
        {
            return _tasks.GetValueOrDefault(id);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    #endregion

    #region Private Methods

    private void CallbackUrl(string? url, TaskModel? task)
    {
        if (string.IsNullOrEmpty(url))
            return;
        
        try
        {
            using HttpRequestMessage message = new()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
            };
            
            string postContent = JsonSerializer.Serialize(task);

            message.Content = new StringContent(postContent, System.Text.Encoding.UTF8, "application/json");

            using HttpResponseMessage responseMessage = _httpClient.Send(message);
        }
        catch
        {
            //Ignored
        }
    }
    private TaskModel CreateTaskId()
    {
        _semaphoreSlim.Wait();
        try
        {
            _id++;

            TaskModel task = new TaskModel()
            {
                Id = _id
            };

            _tasks.Add(_id, task);
            return task;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    #endregion
}