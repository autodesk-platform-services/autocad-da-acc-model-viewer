using Autodesk.DataManagement;
using Autodesk.Forge.Core;
using Autodesk.Forge.DesignAutomation;
using Autodesk.Forge.DesignAutomation.Model;
using Autodesk.Oss.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Threading.Tasks;


[Route("api/[controller]")]
[ApiController]
public class DAController(IHubContext<DAController.DesignAutomationHub> hubContext,
        DesignAutomationClient api,
        ILogger<DAController> logger,
        IWebHostEnvironment env, APS aps) : ControllerBase
{
    public class StartWorkitemInput
    {
        public string? Data { get; set; }
        
    }
    public class WorkitemData
    {
        [JsonProperty("hubId")]
        public string? HubId { get; set; }

        [JsonProperty("projectId")]
        public string? ProjectId { get; set; }

        [JsonProperty("folderId")]
        public string? FolderId { get; set; }

        [JsonProperty("browserConnectionId")]
        public string? BrowserConnectionId { get; set; }
    }

    public class DASettings
    {
        public const string ACTIVITY = "createcolloboration";
        public const string OWNER = "adnworks";
        public const string LABEL = "prod";
        public const string BUNDLENAME = "lmvextractor";
        public const string TARGETENGINE = "Autodesk.AutoCAD+25_0";
        public const string SCOPES = "data:write data:read bucket:read bucket:update bucket:create bucket:delete code:all";
        public const string PACKAGENAME = "LMVExtractor.bundle.zip";
        public const string CLBNAME = "House.collaboration";
    }
    private readonly ILogger<DAController> _logger = logger;
    private readonly APS _aps = aps;
    private readonly IWebHostEnvironment _env = env;
    private IHubContext<DesignAutomationHub> _hubContext = hubContext;
    // Design Automation v3 API
    public DesignAutomationClient _da = api;

    public class DesignAutomationHub : Hub
    {
        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }
    }


    [HttpPost("workitems")]
    /// <summary>
    /// Starts a work item based on the input provided.
    /// </summary>
    /// <param name="input">The input data for starting the work item.</param>
    /// <returns>An IActionResult indicating the result of the operation.</returns>
    public async Task<IActionResult> StartWorkitem([FromForm] StartWorkitemInput input)
    {
        // Check if the input data is null or empty
        if (string.IsNullOrEmpty(input.Data))
        {
            return BadRequest(new { message = "Missing Data" });
        }
        WorkitemData? workItemData;
        try
        {
            // Parse the input JSON data
            workItemData = JsonConvert.DeserializeObject<WorkitemData>(input.Data);
            if (workItemData == null) {
                return BadRequest(new { message = "Invalid JSON format" });
            }
        }
        catch (JsonException)
        {
            // Return bad request if the JSON format is invalid
            return BadRequest(new { message = "Invalid JSON format" });
        }

        // Define the required fields
        string hubId = workItemData.HubId ?? string.Empty;
        string projectId = workItemData.ProjectId ?? string.Empty;
        string folderId = workItemData.FolderId ?? string.Empty;
        string browserConnectionId = workItemData.BrowserConnectionId ?? string.Empty;
        _logger.LogInformation($"Received workitem request for HubId: {hubId}, ProjectId: {projectId}, FolderId: {folderId}");
        // Prepare authentication tokens
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
        {
            // Return unauthorized if token preparation fails
            return Unauthorized();
        }

        // Upload the input file and get its object ID
        var inputObjectId = await _aps.UploadAndGetObjectIdAsync(_aps.WorkingBucket, "House.dwg", Path.Combine(_env.ContentRootPath, "App_Data", "House.dwg"));
        // Get the output storage ID
        var outputObjectId = await _aps.GetStorageId(hubId, projectId, folderId, tokens);

        // Get internal token for OSS.
        var acmToken = await _aps.GetInternalToken();
        var bearerToken1 = $"Bearer {acmToken.AccessToken}";
        var bearerToken2 = $"Bearer {tokens.InternalToken}";

        // Create a new activity ID for the work item
        var activityId = await CreateColloborationFileActivity();

        // Define the work item with necessary arguments
        var workitem = new WorkItem
        {
            ActivityId = activityId,
            Arguments = new Dictionary<string, IArgument>
            {
                {
                    "inputFile", new XrefTreeArgument
                    {
                        Url = inputObjectId,
                        Verb = Verb.Get,
                        Headers = new Dictionary<string, string>
                        {
                            { "Authorization", bearerToken1 }
                        }
                    }
                },
                {
                    "collaboration", new XrefTreeArgument
                    {
                        Verb = Verb.Put,
                        Url = outputObjectId,
                        Headers = new Dictionary<string, string>
                        {
                            { "Authorization", bearerToken2 }
                        }
                    }
                }
            }
        };

        // Create the work item asynchronously
        var workItemStatus = await _da.CreateWorkItemAsync(workitem);

        // Monitor the work item status Fire and Forget pattern
        _ = MonitorWorkitem(browserConnectionId, workItemStatus, projectId, folderId, outputObjectId, tokens);

        // Return OK with the work item ID
        return Ok(new { WorkItemId = workItemStatus.Id });
    }

    /// <summary>
    /// Monitors the status of a work item, sending progress updates and completion messages to the client.
    /// </summary>
    /// <param name="browserConnectionId">The connection ID of the browser client to send updates to.</param>
    /// <param name="workItemStatus">The initial status of the work item.</param>
    /// <param name="projectId">The project ID associated with the ACC.</param>
    /// <param name="folderId">The folder ID associated with the ACC.</param>
    /// <param name="storageId">The storage ID for the output file of the ACC.</param>
    /// <param name="tokens">The authentication tokens required for ACC API calls.</param>
    private async Task MonitorWorkitem(string browserConnectionId, WorkItemStatus workItemStatus, string projectId, string folderId, string storageId, Tokens tokens)
    {
        //Best practise is to use Callback URL to get the status of the workitem
        try
        {
            // Continuously check the work item status until it is done
            while (!workItemStatus.Status.IsDone())
            {
                // Wait for 2 seconds before checking the status again
                await Task.Delay(TimeSpan.FromSeconds(2));

                // Get the latest work item status
                workItemStatus = await _da.GetWorkitemStatusAsync(workItemStatus.Id);

                // Send progress updates to the client
                await _hubContext.Clients.Client(browserConnectionId).SendAsync("onProgress", workItemStatus.ToString());
            }

            // Using an HttpClient to retrieve the report of the work item
            using (var httpClient = new HttpClient())
            {
                // Get the report bytes from the report URL
                var reportBytes = await httpClient.GetByteArrayAsync(workItemStatus.ReportUrl);

                // Convert the report bytes to a string
                var report = System.Text.Encoding.Default.GetString(reportBytes);

                // Send the completion message to the client along with the report
                await _hubContext.Clients.Client(browserConnectionId).SendAsync("onComplete", report);
            }

            // Check if the work item was successful
            if (workItemStatus.Status == Autodesk.Forge.DesignAutomation.Model.Status.Success)
            
            {

                bool itemExists = false;
                // Check if the item already exists in the folder
                var folderContents = await _aps.GetFolderContents(projectId, folderId, tokens);
                var itemId = string.Empty;
                foreach (var item in folderContents)
                {
                    if (item.Type.Equals("items") &&
                        item.Attributes.DisplayName.Equals("House.collaboration"))
                    {
                        itemExists = true;
                        itemId = item.Id;
                        break;
                    }
                }
                if(itemExists)
                {
                    // Update the existing item version
                    var  versionId = await _aps.UpdateItemVersion(projectId, folderId, storageId, itemId, tokens);
                    // Notify the client of the successful completion
                    var msg = $"Collaboration exists, a new {versionId} is created";
                    await _hubContext.Clients.Client(browserConnectionId).SendAsync("onSuccess", msg);
                }
                else
                {
                    // Create a new item version using the storage ID and tokens
                    itemId = await _aps.CreateItemVersion(projectId, folderId, storageId, tokens);
                    // Notify the client of the successful completion
                    var msg = $"Collaboration file created with ID: {itemId}";
                    await _hubContext.Clients.Client(browserConnectionId).SendAsync("onSuccess", msg);
                }
               
            }
            await _hubContext.Clients.Client(browserConnectionId).SendAsync("onComplete", "Done!!");
        }
        catch (Exception ex)
        {
            // Send an error message to the client if an exception occurs
            await _hubContext.Clients.Client(browserConnectionId).SendAsync("onComplete", ex.Message);
        }
    }



    /// <summary>
    /// Creates or updates a collaboration file activity for Autodesk Design Automation.
    /// </summary>
    /// <returns>The activity name if successful; otherwise, an empty string.</returns>
    private async Task<string> CreateColloborationFileActivity()
    {
        // Set up the owner and ensure it is correctly configured
        var isSet = await SetupOwnerAsync();
        if (!isSet)
        {
            return string.Empty;
        }

        Console.WriteLine("Setting up activity...");

        // Set up the app bundle and get its name
        string appBundle = await SetupAppBundleAsync();

        // Construct the activity name
        var collaborationActivity = $"{DASettings.OWNER}.{DASettings.ACTIVITY}+{DASettings.LABEL}";

        // Check if the activity already exists
        var actResponse = await _da.ActivitiesApi.GetActivityAsync(collaborationActivity, throwOnError: false);

        // Define the activity configuration
        var activity = new Activity()
        {
            CommandLine = new List<string>
        {
            $"\"$(engine.path)\\accoreconsole.exe\" /i \"$(args[inputFile].path)\" /al \"$(appbundles[{DASettings.BUNDLENAME}].path)\" /s \"$(settings[script].path)\""
        },
            Engine = DASettings.TARGETENGINE,
            Parameters = new Dictionary<string, Parameter>()
        {
            {
                "inputFile", new Parameter()
                {
                    Verb = Verb.Get,
                    Required = true
                }
            },
            {
                "collaboration", new Parameter()
                {
                    Verb = Verb.Put,
                    LocalName = DASettings.CLBNAME,
                    Required = true
                }
            }
        },
            Settings = new Dictionary<string, ISetting>()
        {
            {
                "script", new StringSetting()
                {
                    // This script will be executed on the DA server
                    Value = "EXTRACTDATA\n"
                }
            }
        },
            Id = DASettings.ACTIVITY,
            Appbundles = [appBundle]
        };

        // If the activity does not exist, create it
        if (actResponse.HttpResponse.StatusCode == HttpStatusCode.NotFound)
        {
            await _hubContext.Clients.All.SendAsync("onProgress", $"Creating activity {collaborationActivity} ...");
            await _da.CreateActivityAsync(activity, DASettings.LABEL);
            return collaborationActivity;
        }

        // Ensure the existing activity retrieval was successful
        await actResponse.HttpResponse.EnsureSuccessStatusCodeAsync();

        // Notify that the existing activity was found
        await _hubContext.Clients.All.SendAsync("onProgress", "\tFound existing activity...");

        // If the existing activity differs from the new one, update it
        if (!Equals(activity, actResponse.Content))
        {
            await _hubContext.Clients.All.SendAsync("onProgress", $"\tUpdating activity {collaborationActivity}...");
            await _da.UpdateActivityAsync(activity, DASettings.LABEL);
        }

        // Notify the clients about the activity
        await _hubContext.Clients.All.SendAsync("onProgress", $"Activity: \n\t{activity}");
        return collaborationActivity;

        // Helper method to compare two activity objects, ignoring ID and version
        static bool Equals(Activity a, Activity b)
        {
            Console.Write("\tComparing activities...");
            b.Id = a.Id;
            b.Version = a.Version;
            var res = a.ToString() == b.ToString();
            Console.WriteLine(res ? "Same." : "Different");
            return res;
        }
    }
    /// <summary>
    /// Creates of Updates the app bundle for Autodesk Design Automation.
    /// </summary>
    /// <returns>The app bundle name if successful.</returns>
    private async Task<string> SetupAppBundleAsync()
    {
        Console.WriteLine("Setting up appbundle...");

        // Construct the app bundle name
        var myApp = $"{DASettings.OWNER}.{DASettings.BUNDLENAME}+{DASettings.LABEL}";

        // Check if the app bundle already exists
        var appResponse = await _da.AppBundlesApi.GetAppBundleAsync(myApp, throwOnError: false);

        // Define the app bundle configuration
        var app = new AppBundle()
        {
            Engine = DASettings.TARGETENGINE,
            Id = DASettings.BUNDLENAME
        };

        // Path to the app bundle package
        var package = Path.Combine(_env.ContentRootPath, "App_Data", DASettings.PACKAGENAME);

        // If the app bundle does not exist, create it
        if (appResponse.HttpResponse.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine($"\tCreating appbundle {myApp}...");
            await _da.CreateAppBundleAsync(app, DASettings.LABEL, package);
            return myApp;
        }

        // Ensure the existing app bundle retrieval was successful
        await appResponse.HttpResponse.EnsureSuccessStatusCodeAsync();
        Console.WriteLine("\tFound existing appbundle...");

        // If the existing app bundle differs from the new one, update it
        if (!await EqualsAsync(package, appResponse.Content.Package))
        {
            Console.WriteLine($"\tUpdating appbundle {myApp}...");
            await _da.UpdateAppBundleAsync(app, DASettings.LABEL, package);
        }
        return myApp;
    }

    /// <summary>
    /// Compares two files asynchronously to determine if they are equal.
    /// </summary>
    /// <param name="a">The path to the first file.</param>
    /// <param name="b">The URL of the second file.</param>
    /// <returns>True if the files are equal; otherwise, false.</returns>
    private async Task<bool> EqualsAsync(string a, string b)
    {
        Console.Write("\tComparing bundles...");

        // Open the first file for reading
        using var aStream = System.IO.File.OpenRead(a);

        // Download the second file and save it locally
        var packageFolder = Path.Combine(_env.ContentRootPath, "App_Data", "das-appbundle.zip");
        var bLocal = await DownloadToDocsAsync(b, packageFolder);

        // Open the downloaded file for reading
        using var bStream = System.IO.File.OpenRead(bLocal);

        // Create a SHA256 hasher
        using var hasher = SHA256.Create();

        // Compare the hashes of the two files
        var res = hasher.ComputeHash(aStream).SequenceEqual(hasher.ComputeHash(bStream));
        Console.WriteLine(res ? "Same." : "Different");
        return res;
    }

    /// <summary>
    /// Downloads a file from a URL and saves it locally.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
    /// <param name="localFile">The path to save the downloaded file.</param>
    /// <returns>The local file path.</returns>
    private static async Task<string> DownloadToDocsAsync(string url, string localFile)
    {
        // Delete the local file if it already exists
        if (System.IO.File.Exists(localFile))
        {
            System.IO.File.Delete(localFile);
        }

        // Download the file using HttpClient
        using var client = new HttpClient();
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        // Save the downloaded file locally
        using (var fs = new FileStream(localFile, FileMode.CreateNew))
        {
            await response.Content.CopyToAsync(fs);
        }
        Console.WriteLine($"Downloading {localFile}");
        return localFile;
    }

    /// <summary>
    /// Sets up the owner for Autodesk Design Automation.
    /// </summary>
    /// <returns>True if the setup was successful; otherwise, false.</returns>
    private async Task<bool> SetupOwnerAsync()
    {
        Console.WriteLine("Setting up owner...");

        // Get the nickname of the current Design Automation app
        string nickname = await _da.GetNicknameAsync("me");

        // Check if the nickname matches the client ID
        if (nickname == _aps.ClientId)
        {
            Console.WriteLine("\tNo nickname for this clientId yet. Attempting to create one...");
            HttpResponseMessage resp;

            // Attempt to create a new nickname
            resp = await _da.ForgeAppsApi.CreateNicknameAsync("me", new NicknameRecord() { Nickname = DASettings.OWNER }, throwOnError: false);

            // If there is a conflict, notify the user and return false
            if (resp.StatusCode == HttpStatusCode.Conflict)
            {
                Console.WriteLine("\tThere are already resources associated with this clientId or nickname is in use. Please use a different clientId or nickname.");
                return false;
            }

            // Ensure the nickname creation was successful
            await resp.EnsureSuccessStatusCodeAsync();
        }
        return true;
    }

}


