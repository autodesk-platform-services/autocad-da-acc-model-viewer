using System;
using Autodesk.SDKManager;
using Autodesk.Authentication.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Oss.Model;

public class Tokens
{
    public string? InternalToken;
    public string? PublicToken;
    public string? RefreshToken;
    public DateTime ExpiresAt;
}

public partial class APS
{
    private readonly SDKManager _sdkManager;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _callbackUri;
    private readonly string _bucket;
    private readonly List<Scopes> InternalTokenScopes = new List<Scopes> { Scopes.DataRead, Scopes.DataCreate, Scopes.DataSearch, Scopes.DataWrite,Scopes.BucketRead, Scopes.BucketCreate,Scopes.BucketDelete, Scopes.BucketUpdate, Scopes.ViewablesRead};
    private readonly List<Scopes> PublicTokenScopes = new List<Scopes> { Scopes.ViewablesRead };

    public APS(string clientId, string clientSecret, string bucket, string callbackUri)
    {
        _sdkManager = SdkManagerBuilder.Create().Build();
        _clientId = clientId;
        _clientSecret = clientSecret;
        _callbackUri = callbackUri;
        _bucket = string.IsNullOrEmpty(bucket) ? $"adn-clb-{DateTimeOffset.Now.ToUnixTimeSeconds()}" : bucket;
    }
    public string WorkingBucket => _bucket;
    public string ClientId => _clientId;
    
}
