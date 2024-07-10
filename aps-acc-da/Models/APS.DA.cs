using Autodesk.DataManagement.Model;
using Autodesk.DataManagement;
using Autodesk.Oss;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Autodesk.Oss.Model;
using static APS;

public partial class APS
{
    /// <summary>
    /// Creates a new item version in the specified project and folder using the provided storage ID and tokens.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="folderId">The ID of the folder.</param>
    /// <param name="storageId">The ID of the storage.</param>
    /// <param name="tokens">The authentication tokens.</param>
    /// <returns>A task that represents the asynchronous operation, with a string containing the ID of the created item.</returns>
    public async Task<string> CreateItemVersion(string projectId, string folderId, string storageId, Tokens tokens)
    {
        var dataManagementClient = new DataManagementClient(_sdkManager);
        var itemPayload = CreateItemPayload(projectId, folderId, storageId);
        var createdItem = await dataManagementClient.CreateItemAsync(projectId, null, null, itemPayload: itemPayload, tokens.InternalToken);
        Console.WriteLine($"Created an item: {createdItem.Data.Id} on ACC {projectId}\\{folderId}");
        Console.WriteLine("Congrats! Done !!!!");
        return createdItem.Data.Id;
    }

    public async Task<string> UpdateItemVersion(string projectId, string folderId, string storageId, string itemId,Tokens tokens)
    {
        var dataManageClient = new DataManagementClient(_sdkManager);
        VersionPayload versionPayload = new VersionPayload()
        {
            Jsonapi = new ModifyFolderPayloadJsonapi()
            {
                _Version = VersionNumber._10
            },
            Data = new VersionPayloadData()
            {
                Type = Autodesk.DataManagement.Model.Type.Versions,
                Attributes = new VersionPayloadDataAttributes()
                {
                    Name = "House.collaboration",
                    Extension = new RelationshipRefsPayloadDataMetaExtension()
                    {
                        Type = projectId.StartsWith("b.") 
                        ? Autodesk.DataManagement.Model.Type.VersionsautodeskBim360File
                        : Autodesk.DataManagement.Model.Type.VersionsautodeskCoreFile,
                        _Version = VersionNumber._10
                    }
                },
                Relationships = new VersionPayloadDataRelationships()
                {
                    Item = new FolderPayloadDataRelationshipsParent()
                    {
                        Data = new FolderPayloadDataRelationshipsParentData()
                        {
                            Type = Autodesk.DataManagement.Model.Type.Items,
                            Id = itemId
                        }
                    },
                    Storage = new FolderPayloadDataRelationshipsParent()
                    {
                        Data = new FolderPayloadDataRelationshipsParentData()
                        {
                            Type = Autodesk.DataManagement.Model.Type.Objects,
                            Id = storageId
                        }
                    }
                }
            }
        };
        try
        {
            ModelVersion createdVersion = await dataManageClient.CreateVersionAsync(projectId: projectId, versionPayload: versionPayload, accessToken: tokens.InternalToken);
            return createdVersion.Data.Id;
        }
        catch (DataManagementApiException ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }       
       
    }


    public async Task<string> GetItemId(string projectId, string folderId, Tokens tokens)
    {
        var dataManagementClient = new DataManagementClient(_sdkManager);
        var contents = await dataManagementClient.GetFolderContentsAsync(projectId, folderId, accessToken: tokens.InternalToken);
        foreach (var content in contents.Data)
        {
            if (content.Attributes.DisplayName == "House.collaboration" && content.Type == Autodesk.DataManagement.Model.Type.Items.ToString() )
            {
                return content.Id;
            }
        }
        return string.Empty;
    }
    /// <summary>
    /// Retrieves the storage ID for a specific hub, project, and folder using the provided tokens.
    /// </summary>
    /// <param name="hubId">The ID of the hub.</param>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="folderId">The ID of the folder.</param>
    /// <param name="tokens">The authentication tokens.</param>
    /// <returns>A task that represents the asynchronous operation, with a string containing the storage ID.</returns>
    /// <exception cref="Exception">Thrown if an error occurs while retrieving the storage ID.</exception>
    public async Task<string> GetStorageId(string hubId, string projectId, string folderId, Tokens tokens)
    {
        try
        {
            var dataManagementClient = new DataManagementClient(_sdkManager);            
            var storagePayload = CreateStoragePayload(folderId);
            var storage = await dataManagementClient.CreateStorageAsync(projectId, null, storagePayload: storagePayload, accessToken:tokens.InternalToken);
            Console.WriteLine($"Storage: {storage}");
            return storage.Data.Id;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates the payload for storage creation in a specified folder.
    /// </summary>
    /// <param name="folderId">The ID of the folder.</param>
    /// <returns>A StoragePayload object containing the storage creation data.</returns>
    private StoragePayload CreateStoragePayload(string folderId)
    {
        return new StoragePayload
        {
            Jsonapi = new ModifyFolderPayloadJsonapi
            {
                _Version = VersionNumber._10
            },
            Data = new StoragePayloadData
            {
                Type = Autodesk.DataManagement.Model.Type.Objects,
                Attributes = new StoragePayloadDataAttributes
                {
                    Name = "House.collaboration"
                },
                Relationships = new StoragePayloadDataRelationships
                {
                    Target = new ModifyFolderPayloadDataRelationshipsParent
                    {
                        Data = new ModifyFolderPayloadDataRelationshipsParentData
                        {
                            Type = Autodesk.DataManagement.Model.Type.Folders,
                            Id = folderId
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// Extracts the bucket key and object key from an object ID.
    /// </summary>
    /// <param name="objectId">The object ID to extract the keys from.</param>
    /// <returns>A tuple containing the bucket key and object key.</returns>
    private (string bucketKey, string objectKey) ExtractBucketAndObjectKey(string objectId)
    {
        var match = Regex.Match(objectId, ".*:.*:(.*)/(.*)");
        return (match.Groups[1].Value, match.Groups[2].Value);
    }

    /// <summary>
    /// Creates the payload for item creation in a specified project and folder using the provided object ID.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="folderId">The ID of the folder.</param>
    /// <param name="objectId">The object ID to be included in the item payload.</param>
    /// <returns>An ItemPayload object containing the item creation data.</returns>
    private ItemPayload CreateItemPayload(string projectId, string folderId, string objectId)
    {
        return new ItemPayload
        {
            Jsonapi = new ModifyFolderPayloadJsonapi
            {
                _Version = VersionNumber._10
            },
            Data = new ItemPayloadData
            {
                Type = Autodesk.DataManagement.Model.Type.Items,
                Attributes = new ItemPayloadDataAttributes
                {
                    DisplayName = "House.collaboration",
                    Extension = new ItemPayloadDataAttributesExtension
                    {
                        Type = projectId.StartsWith("b.") ? Autodesk.DataManagement.Model.Type.ItemsautodeskBim360File : Autodesk.DataManagement.Model.Type.ItemsautodeskCoreFile,
                        _Version = VersionNumber._10
                    }
                },
                Relationships = new ItemPayloadDataRelationships
                {
                    Tip = new FolderPayloadDataRelationshipsParent
                    {
                        Data = new FolderPayloadDataRelationshipsParentData
                        {
                            Type = Autodesk.DataManagement.Model.Type.Versions,
                            Id = "1"
                        }
                    },
                    Parent = new FolderPayloadDataRelationshipsParent
                    {
                        Data = new FolderPayloadDataRelationshipsParentData
                        {
                            Type = Autodesk.DataManagement.Model.Type.Folders,
                            Id = folderId
                        }
                    }
                }
            },
            Included = new List<ItemPayloadIncluded>
            {
                new ItemPayloadIncluded
                {
                    Type = Autodesk.DataManagement.Model.Type.Versions,
                    Id = "1",
                    Attributes = new ItemPayloadIncludedAttributes
                    {
                        Name = "House.collaboration",
                        Extension = new ItemPayloadDataAttributesExtension
                        {
                            Type = Autodesk.DataManagement.Model.Type.VersionsautodeskBim360File,
                            _Version = VersionNumber._10
                        }
                    },
                    Relationships = new ItemPayloadIncludedRelationships
                    {
                        Storage = new FolderPayloadDataRelationshipsParent
                        {
                            Data = new FolderPayloadDataRelationshipsParentData
                            {
                                Type = Autodesk.DataManagement.Model.Type.Objects,
                                Id = objectId
                            }
                        }
                    }
                }
            }
        };
    }
}
