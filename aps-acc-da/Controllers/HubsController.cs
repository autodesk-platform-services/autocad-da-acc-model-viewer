
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

[ApiController]
[Route("api/[controller]")]
public class HubsController : ControllerBase
{
    private readonly ILogger<HubsController> _logger;
    private readonly APS _aps;
    private readonly IWebHostEnvironment _env;

    // Constructor to initialize logger, APS service, and environment
    public HubsController(ILogger<HubsController> logger, IWebHostEnvironment env, APS aps)
    {
        _logger = logger;
        _aps = aps;
        _env = env;
    }

    // GET: /api/hubs
    // Retrieves a list of hubs accessible to the authenticated user
    [HttpGet()]
    public async Task<ActionResult<string>> ListHubs()
    {
        // Prepare tokens for authentication
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
        {
            return Unauthorized();
        }

        // Retrieve hubs using APS service
        var hubs = await _aps.GetHubs(tokens);

        // Serialize hubs data to JSON and return as ActionResult
        return JsonConvert.SerializeObject(hubs);
    }

    // GET: /api/hubs/{hub}/projects
    // Retrieves a list of projects within a specified hub
    [HttpGet("{hub}/projects")]
    public async Task<ActionResult<string>> ListProjects(string hub)
    {
        // Prepare tokens for authentication
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
        {
            return Unauthorized();
        }

        // Retrieve projects for the specified hub using APS service
        var projects = await _aps.GetProjects(hub, tokens);

        // Serialize projects data to JSON and return as ActionResult
        return JsonConvert.SerializeObject(projects);
    }

    // GET: /api/hubs/{hub}/projects/{project}/contents
    // Retrieves items or folders within a project based on folder_id query parameter
    [HttpGet("{hub}/projects/{project}/contents")]
    public async Task<ActionResult<string>> ListItems(string hub, string project, [FromQuery] string? folder_id)
    {
        // Prepare tokens for authentication
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(folder_id))
        {
            // Retrieve top-level folders for the project using APS service
            var folders = await _aps.GetTopFolders(hub, project, tokens);

            // Serialize folders data to JSON and return as ActionResult
            return JsonConvert.SerializeObject(folders);
        }
        else
        {
            // Retrieve contents (items) within the specified folder using APS service
            var contents = await _aps.GetFolderContents(project, folder_id, tokens);

            // Serialize contents data to JSON and return as ActionResult
            return JsonConvert.SerializeObject(contents);
        }
    }

    // GET: /api/hubs/{hub}/projects/{project}/contents/{item}/versions
    // Retrieves versions of a specific item within a project
    [HttpGet("{hub}/projects/{project}/contents/{item}/versions")]
    public async Task<ActionResult<string>> ListVersions(string hub, string project, string item)
    {
        // Prepare tokens for authentication
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
        {
            return Unauthorized();
        }

        // Retrieve versions of the specified item within the project using APS service
        var versions = await _aps.GetVersions(project, item, tokens);

        // Serialize versions data to JSON and return as ActionResult
        return JsonConvert.SerializeObject(versions);
    }
}

