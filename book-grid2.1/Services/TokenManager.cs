using BookGrid.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace BookGrid.Services;

public class TokenManager
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenManager> _logger;
    private readonly string _tokenRefreshUrl;
    private readonly string _staticToken;
    private readonly string _refreshToken;
    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);

    private const string TokenCacheKey = "LibCalToken";
    private const string RefreshTokenCacheKey = "LibCalRefreshToken";
    private const string ExpiryKey = "TokenExpiry";

    public TokenManager(
        HttpClient httpClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<TokenManager> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        _tokenRefreshUrl = configuration["LibCal:TokenRefreshUrl"] ?? "";
        _staticToken = configuration["LibCal:StaticToken"] ?? "";
        _refreshToken = configuration["LibCal:RefreshToken"] ?? "";
    }

    public async Task<bool> LinkLibCalTokenAsync(string accessToken, DateTime expiresAt, object? metadata = null)
    {
        var userId = _configuration["LibCal:UserId"] ?? "";
        var refreshToken = _configuration["LibCal:RefreshToken"] ?? "";
        var linkUrl = _configuration["LibCal:TokenLinkUrl"] ?? "https://auth.library.uri.edu/api/v1/libcal/link";

        var requestBody = new {
            user_id = userId,
            refresh_token = refreshToken,
            access_token = accessToken,
            expires_at = expiresAt.ToString("o"),
            metadata = metadata ?? new { }
        };
        _logger.LogInformation("Linking LibCal token for user_id: {UserId}", userId);
        using var request = new HttpRequestMessage(HttpMethod.Post, linkUrl);
        request.Content = JsonContent.Create(requestBody);
        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Token linked successfully: {Content}", content);
            return true;
        }
        else
        {
            _logger.LogError("Failed to link token: {StatusCode}, {Content}", response.StatusCode, content);
            return false;
        }
    }

    public async Task<string> GetValidTokenAsync()
    {
        // Check if we have a valid cached token
        if (_cache.TryGetValue(TokenCacheKey, out string? cachedToken) &&
            _cache.TryGetValue(ExpiryKey, out DateTime expiry) &&
            expiry > DateTime.UtcNow.AddMinutes(10)) // Refresh 10 minutes before expiry
        {
            return cachedToken!;
        }

        // Try to refresh token
        if (!string.IsNullOrEmpty(_tokenRefreshUrl))
        {
            try
            {
                var refreshedToken = await RefreshTokenAsync();
                if (!string.IsNullOrEmpty(refreshedToken))
                {
                    return refreshedToken;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token refresh failed, falling back to static token");
            }
        }

        // Fall back to static token
        _logger.LogInformation("Using static token as fallback");
        return _staticToken;
    }

    public async Task<string?> RefreshTokenAsync()
    {
        await _refreshSemaphore.WaitAsync();
        try
        {
            _logger.LogInformation("Attempting to refresh LibCal token");

            // Get the current refresh token from cache or config
            var currentRefreshToken = _cache.Get<string>(RefreshTokenCacheKey) ?? _refreshToken;
            
            if (string.IsNullOrEmpty(currentRefreshToken))
            {
                _logger.LogError("No refresh token available");
                return null;
            }

            var userId = _configuration["LibCal:UserId"] ?? "";
            var requestBody = new {
                user_id = userId,
                refresh_token = currentRefreshToken,
                metadata = new { }
            };
            _logger.LogDebug("Sending token refresh request to {Url} with user_id: {UserId}, refresh_token: {Token}", _tokenRefreshUrl, userId, currentRefreshToken);

            using var request = new HttpRequestMessage(HttpMethod.Post, _tokenRefreshUrl);
            request.Content = JsonContent.Create(requestBody);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token refresh failed with status: {StatusCode}, body: {Body}", response.StatusCode, errorContent);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<AuthServiceTokenResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenResponse?.AccessToken != null)
            {
                var expiryTime = tokenResponse.ExpiresAt;
                _cache.Set(TokenCacheKey, tokenResponse.AccessToken, expiryTime.AddMinutes(-5)); // Cache until 5 min before expiry
                _cache.Set(ExpiryKey, expiryTime);
                _logger.LogInformation("Token refreshed successfully, expires at {ExpiryTime}", expiryTime);
                return tokenResponse.AccessToken;
            }

            _logger.LogError("Invalid token response received");
            return null;
        }
        finally
        {
            _refreshSemaphore.Release();
        }
    }

    public void ClearToken()
    {
        _cache.Remove(TokenCacheKey);
        _cache.Remove(ExpiryKey);
        _logger.LogInformation("Token cache cleared");
    }
}