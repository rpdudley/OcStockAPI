namespace KubsConnect.Settings;

public class StartupConfig
{
    public AlphaVantageSettings AlphaVantageSettings { get; set; } = new();
    public NewsSettings NewsSettings { get; set; } = new();
    public FinnhubSettings FinnhubSettings { get; set; } = new();
    public DBConnectionSettings DBConnectionSettings { get; set; } = new();

    public void AddClassToServices(IServiceCollection services)
    {
        try
        {
            services.AddSingleton(this.AlphaVantageSettings);
            services.AddSingleton(this.DBConnectionSettings);
            services.AddSingleton(this.NewsSettings);
            services.AddSingleton(this.FinnhubSettings);
        }
        catch (Exception ex)
        {
            throw new Exception("Unable to add services in confapiconfig class: " + ex.Message);
        }
    }
}