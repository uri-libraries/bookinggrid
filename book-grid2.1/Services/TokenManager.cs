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
    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);

    private const string TokenCacheKey = "LibCalToken";
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

            var currentToken = _cache.Get<string>(TokenCacheKey) ?? _staticToken;
            var requestBody = new { current_token = currentToken };

            var response = await _httpClient.PostAsJsonAsync(_tokenRefreshUrl, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token refresh failed with status: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<LibCalTokenResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenResponse?.AccessToken != null)
            {
                var expiryTime = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                
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