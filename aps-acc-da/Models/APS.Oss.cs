using Autodesk.Oss.Model;
using Autodesk.Oss;


public partial class APS
{
    /// <summary>
    /// Ensures that a bucket with the specified key exists. If the bucket does not exist, it creates a new one.
    /// </summary>
    /// <param name="bucketKey">The key of the bucket to check or create.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task EnsureBucketExists(string bucketKey)
    {
        // Get the internal token for authentication
        var auth = await GetInternalToken();
        var ossClient = new OssClient(_sdkManager);

        try
        {
            // Attempt to get the bucket details
            await ossClient.GetBucketDetailsAsync(accessToken: auth.AccessToken, bucketKey);
        }
        catch (OssApiException ex)
        {
            // If the bucket is not found, create a new one
            if (ex.HttpResponseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var payload = new CreateBucketsPayload
                {
                    BucketKey = bucketKey,
                    PolicyKey = PolicyKey.Transient
                };
                await ossClient.CreateBucketAsync(auth.AccessToken, Region.US, payload);
            }
            else
            {
                // If the error is not a "Not Found" error, rethrow the exception
                throw;
            }
        }
    }

    /// <summary>
    /// Uploads a file to the specified bucket and returns the object ID.
    /// </summary>
    /// <param name="bucketKey">The key of the bucket to upload the file to.</param>
    /// <param name="objectKey">The key of the object to create within the bucket.</param>
    /// <param name="filePath">The local file path of the file to upload.</param>
    /// <returns>A task that represents the asynchronous operation, with a string result containing the object ID.</returns>
    public async Task<string> UploadAndGetObjectIdAsync(string bucketKey, string objectKey, string filePath)
    {
        // Ensure the bucket exists
        await EnsureBucketExists(bucketKey);

        // Get the internal token for authentication
        var auth = await GetInternalToken();
        var ossClient = new OssClient(_sdkManager);

        // Upload the file and get the object details
        ObjectDetails objectDetails = await ossClient.Upload(bucketKey, objectKey, filePath, auth.AccessToken, new System.Threading.CancellationToken());

        // Return the object ID
        return objectDetails.ObjectId;
    }

}