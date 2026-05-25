using AmsaAPI.Tests.Infrastructure;

namespace AmsaAPI.Tests.Utilities;

/// <summary>
/// Assertions helper for common test validation patterns
/// </summary>
public static class CustomAssertions
{
    /// <summary>
    /// Asserts that two HTTP responses have the same status code and deserialize to equal objects
    /// </summary>
    public static async Task AssertResponsesEqualAsync<T>(
        HttpResponseMessage expected,
        HttpResponseMessage actual,
        string? message = null) where T : class
    {
        Assert.Equal(expected.StatusCode, actual.StatusCode);
        var expectedContent = await expected.Content.ReadAsAsync<T>();
        var actualContent = await actual.Content.ReadAsAsync<T>();
        Assert.Equal(expectedContent, actualContent);
    }

    /// <summary>
    /// Asserts that a token is valid by checking it can be parsed
    /// </summary>
    public static void AssertTokenIsValid(string token)
    {
        Assert.False(string.IsNullOrWhiteSpace(token), "Token should not be empty");
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    /// <summary>
    /// Asserts that a timestamp is recent (within last few seconds)
    /// </summary>
    public static void AssertRecentTimestamp(DateTime timestamp, int secondsThreshold = 5)
    {
        var now = DateTime.UtcNow;
        var difference = Math.Abs((now - timestamp).TotalSeconds);
        Assert.True(difference <= secondsThreshold, $"Timestamp {timestamp} is not recent (difference: {difference}s)");
    }
}
