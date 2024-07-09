using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autodesk.DataManagement;
using Autodesk.DataManagement.Http;
using Autodesk.DataManagement.Model;
using Autodesk.Oss;
using Newtonsoft.Json.Linq;

public partial class APS
{
    /// <summary>
    /// Retrieves a list of hubs using the provided tokens.
    /// </summary>
    /// <param name="tokens">The authentication tokens.</param>
    /// <returns>A task that represents the asynchronous operation, with an IEnumerable containing the hub data.</returns>
    public async Task<IEnumerable<HubsData>> GetHubs(Tokens tokens)
    {
        var dataManagementClient = new DataManagementClient(_sdkManager);
        var hubs = await dataManagementClient.GetHubsAsync(accessToken: tokens.InternalToken);
        return hubs.Data;
    }

    /// <summary>
    /// Retrieves a list of projects for a specific hub using the provided tokens.
    /// </summary>
    /// <param name="hubId">The ID of the hub.</param>
    /// <param name="tokens">The authentication tokens.</param>
    /// <returns>A task that represents the asynchronous operation, with an IEnumerable containing the project data.</returns>
    public async Task<IEnumerable<ProjectsData>> GetProjects(string hubId, Tokens tokens)
    {
        var dataManagementClient = new DataManagementClient(_sdkManager);
        var projects = await dataManagementClient.GetHubProjectsAsync(hubId, accessToken: tokens.InternalToken);
        return projects.Data;
    }

    /// <summary>
    /// Retrieves the top-level folders of a specific project in a hub using the provided tokens.
    /// </summary>
    /// <param name="hubId">The ID of the hub.</param>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="tokens">The authentication tokens.</param>
    /// <returns>A task that represents the asynchronous operation, with an IEnumerable containing the top folders data.</returns>
    public async Task<IEnumerable<TopFoldersData>> GetTopFolders(string hubId, string projectId, Tokens tokens)
    {
        var dataManagementClient = new DataManagementClient(_sdkManager);
        var folders = await dataManagementClient.GetProjectTopFoldersAsync(hubId, projectId, accessToken: tokens.InternalToken);
        return folders.Data;
    }

    /// <summary>
    /// Retrieves the contents of a specific folder in a project using the provided tokens.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="folderId">The ID of the folder.</param>
    /// <param name="tokens">The authentication tokens.</param>
    /// <returns>A task that represents the asynchronous operation, with an IEnumerable containing the folder contents data.</returns>
    public async Task<IEnumerable<FolderContentsData>> GetFolderContents(string projectId, string folderId, Tokens tokens)
    {
        var dataManagementClient = new DataManagementClient(_sdkManager);
        var contents = await dataManagementClient.GetFolderContentsAsync(projectId, folderId, accessToken: tokens.InternalToken);
        return contents.Data;
    }

    /// <summary>
    /// Retrieves the versions of a specific item in a project using the provided tokens.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="itemId">The ID of the item.</param>
    /// <param name="tokens">The authentication tokens.</param>
    /// <returns>A task that represents the asynchronous operation, with an IEnumerable containing the versions data.</returns>
    public async Task<IEnumerable<VersionsData>> GetVersions(string projectId, string itemId, Tokens tokens)
    {
        var dataManagementClient = new DataManagementClient(_sdkManager);
        var versions = await dataManagementClient.GetItemVersionsAsync(projectId, itemId, accessToken: tokens.InternalToken);
        return versions.Data;
    }
}
