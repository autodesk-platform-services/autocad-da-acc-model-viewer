using System;
using System.Threading.Tasks;
using Autodesk.Authentication;
using Autodesk.Authentication.Model;
using Newtonsoft.Json.Linq;

public partial class APS
{
    // Token record to hold access token and its expiration time
    public record Token(string AccessToken, DateTime ExpiresAt);

    private Token? _internalTokenCache;  // Cache for internal token
    private Token? _publicTokenCache;    // Cache for public token

    /// <summary>
    /// Generates an authorization URL for the user to log in.
    /// </summary>
    /// <returns>The authorization URL as a string.</returns>
    public string GetAuthorizationURL()
    {
        var authenticationClient = new AuthenticationClient(_sdkManager);
        return authenticationClient.Authorize(_clientId, ResponseType.Code, _callbackUri, InternalTokenScopes);
    }

    /// <summary>
    /// Generates tokens using the authorization code.
    /// </summary>
    /// <param name="code">The authorization code obtained from the login process.</param>
    /// <returns>A task that represents the asynchronous operation, with a Tokens object containing the generated tokens.</returns>
    /// <exception cref="ApplicationException">Thrown if the token generation fails.</exception>
    public async Task<Tokens> GenerateTokens(string code)
    {
        var authenticationClient = new AuthenticationClient(_sdkManager);

        // Obtain internal and public tokens using the authorization code
        var internalAuth = await authenticationClient.GetThreeLeggedTokenAsync(_clientId, _clientSecret, code, _callbackUri);
        var publicAuth = await authenticationClient.GetRefreshTokenAsync(_clientId, _clientSecret, internalAuth.RefreshToken, PublicTokenScopes);

        // Check if the token generation was successful
        if (publicAuth == null || internalAuth == null)
        {
            throw new ApplicationException("Failed to get tokens.");
        }
        if (internalAuth.ExpiresIn == null)
        {
            throw new ApplicationException("Failed to get refresh tokens.");
        }

        // Return the generated tokens
        return new Tokens
        {
            PublicToken = publicAuth.AccessToken,
            InternalToken = internalAuth.AccessToken,
            RefreshToken = publicAuth._RefreshToken,
            ExpiresAt = DateTime.Now.ToUniversalTime().AddSeconds((double)internalAuth.ExpiresIn)
        };
    }

    /// <summary>
    /// Refreshes the tokens using the existing refresh token.
    /// </summary>
    /// <param name="tokens">The current tokens that need to be refreshed.</param>
    /// <returns>A task that represents the asynchronous operation, with a Tokens object containing the refreshed tokens.</returns>
    /// <exception cref="ApplicationException">Thrown if the token refresh fails.</exception>
    public async Task<Tokens> RefreshTokens(Tokens tokens)
    {
        var authenticationClient = new AuthenticationClient(_sdkManager);

        // Refresh the internal and public tokens using the existing refresh token
        var internalAuth = await authenticationClient.GetRefreshTokenAsync(_clientId, _clientSecret, tokens.RefreshToken, InternalTokenScopes);
        var publicAuth = await authenticationClient.GetRefreshTokenAsync(_clientId, _clientSecret, internalAuth._RefreshToken, PublicTokenScopes);

        // Check if the token refresh was successful
        if (internalAuth.ExpiresIn == null)
        {
            throw new ApplicationException("Failed to get refresh tokens.");
        }

        // Return the refreshed tokens
        return new Tokens
        {
            PublicToken = publicAuth.AccessToken,
            InternalToken = internalAuth.AccessToken,
            RefreshToken = publicAuth._RefreshToken,
            ExpiresAt = DateTime.Now.ToUniversalTime().AddSeconds((double)internalAuth.ExpiresIn)
        };
    }

    /// <summary>
    /// Retrieves the user profile information using the internal token.
    /// </summary>
    /// <param name="tokens">The tokens containing the internal token used to fetch the user profile.</param>
    /// <returns>A task that represents the asynchronous operation, with a UserInfo object containing the user profile information.</returns>
    public async Task<UserInfo> GetUserProfile(Tokens tokens)
    {
        var authenticationClient = new AuthenticationClient(_sdkManager);

        // Fetch the user profile using the internal token
        UserInfo userInfo = await authenticationClient.GetUserInfoAsync(tokens.InternalToken);
        return userInfo;
    }

    /// <summary>
    /// Gets an authentication token with the specified scopes.
    /// </summary>
    /// <param name="scopes">The list of scopes for which the token is requested.</param>
    /// <returns>A token containing the access token and its expiration time.</returns>
    /// <exception cref="ApplicationException">Thrown if the token request fails.</exception>
    private async Task<Token> GetToken(List<Scopes> scopes)
    {
        // Create an authentication client using the SDK manager
        var authenticationClient = new AuthenticationClient(_sdkManager);

        // Request a two-legged token with the specified scopes
        var auth = await authenticationClient.GetTwoLeggedTokenAsync(_clientId, _clientSecret, scopes);

        // If the token request fails, throw an exception
        if (auth.ExpiresIn == null)
        {
            throw new ApplicationException("Failed to get tokens.");
        }

        // Return a new token with the access token and its expiration time
        return new Token(auth.AccessToken, DateTime.UtcNow.AddSeconds((double)auth.ExpiresIn));
    }

    /// <summary>
    /// Gets a public token with read-only viewable scopes.
    /// </summary>
    /// <returns>A token containing the access token and its expiration time.</returns>
    public async Task<Token> GetPublicToken()
    {
        // If the public token cache is null or expired, request a new token
        if (_publicTokenCache == null || _publicTokenCache.ExpiresAt < DateTime.UtcNow)
        {
            _publicTokenCache = await GetToken(new List<Scopes> { Scopes.ViewablesRead });
        }

        // Return the cached public token
        return _publicTokenCache;
    }

    /// <summary>
    /// Gets an internal token with full access scopes.
    /// </summary>
    /// <returns>A token containing the access token and its expiration time.</returns>
    public async Task<Token> GetInternalToken()
    {
        // If the internal token cache is null or expired, request a new token
        if (_internalTokenCache == null || _internalTokenCache.ExpiresAt < DateTime.UtcNow)
        {
            _internalTokenCache = await GetToken(new List<Scopes>
        {
            Scopes.BucketCreate,
            Scopes.BucketRead,
            Scopes.DataRead,
            Scopes.DataWrite,
            Scopes.DataCreate,
            Scopes.CodeAll
        });
        }

        // Return the cached internal token
        return _internalTokenCache;
    }
}
