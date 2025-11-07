namespace JobDisplayer.Web.Services;

public static class TimeframeFilter
{
    public static bool TryGetCutoff(string? timeframe, out DateTime cutoff)
    {
        cutoff = default;

        if (string.IsNullOrWhiteSpace(timeframe) || string.Equals(timeframe, "recent", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var now = DateTime.UtcNow;
        cutoff = timeframe switch
        {
            "24h" or "last24" or "last24h" or "last24hrs" or "last24hours" => now.AddHours(-24),
            "3d" or "72h" or "past3days" => now.AddDays(-3),
            "5d" or "past5days" => now.AddDays(-5),
            _ => default
        };

        return cutoff != default;
    }
}
