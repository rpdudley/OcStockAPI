using System.ComponentModel.DataAnnotations;

namespace OcStockAPI.Settings;

public class AppSettings
{
    public AlphaVantageSettings AlphaVantage { get; set; } = new();
    public NewsSettings NewsAPI { get; set; } = new();
    public FinnhubSettings Finnhub { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new();
}

public class AlphaVantageSettings
{
    [Required]
    public string ApiKey { get; set; } = string.Empty;
}

public class NewsSettings
{
    [Required]
    public string ApiKey { get; set; } = string.Empty;
}

public class FinnhubSettings
{
    [Required]
    public string ApiKey { get; set; } = string.Empty;
}

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    
    // Individual connection components
    public string Host { get; set; } = string.Empty;
    public string Port { get; set; } = "5432";
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RequireSsl { get; set; } = true;
    
    /// <summary>
    /// Gets the effective connection string. If ConnectionString is provided, uses that.
    /// Otherwise, builds connection string from individual components.
    /// </summary>
    public string GetEffectiveConnectionString()
    {
        // If full connection string is provided, use it
        if (!string.IsNullOrEmpty(ConnectionString) && ConnectionString != "from-env")
        {
            return ConnectionString;
        }
        
        // Build connection string from components
        if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Database) || 
            string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
        {
            throw new InvalidOperationException(
                "Database connection requires either a full ConnectionString or all components (Host, Database, Username, Password)");
        }
        
        var sslMode = RequireSsl ? "Require" : "Prefer";
        return $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};SSL Mode={sslMode};Trust Server Certificate=true;";
    }
}
