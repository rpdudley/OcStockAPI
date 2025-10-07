namespace OcStockAPI.Settings;

public class DatabaseConnectionSettings
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    
    public string ToConnectionString()
    {
        return $"Host={Host};Port={Port};Database={Database};Username={UserName};Password={Password};SSL Mode=Require;Trust Server Certificate=true;";
    }
    
    public static DatabaseConnectionSettings FromPostgreSqlUrl(string connectionUrl)
    {
        // Parse postgresql://username:password@host:port/database
        var uri = new Uri(connectionUrl);
        
        return new DatabaseConnectionSettings
        {
            Host = uri.Host,
            Port = uri.Port.ToString(),
            Database = uri.AbsolutePath.TrimStart('/'),
            UserName = uri.UserInfo.Split(':')[0],
            Password = uri.UserInfo.Split(':')[1]
        };
    }
}
