using k8s;
using k8s.Models;
using KubsConnect.Settings;
using Newtonsoft.Json;
using System.Text;

namespace KubsConnect;

public class KubsClient : IKubsClient
{
    string K8SNameSpace = "default";
    public KubsClient(IServiceCollection pservices, Microsoft.Extensions.Configuration.IConfiguration pconfig, IWebHostEnvironment env)
    {
        if (env.EnvironmentName.ToLower() != "prod")
        {
            this.K8sConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile(Environment.GetEnvironmentVariable("KUBECONFIG"), K8SNameSpace);
            this.client = new Kubernetes(K8sConfig);
            this.config = this.GetSecret<StartupConfig>(K8SNameSpace, "databaseprojectapi-secret");
        }
        else
        {
            this.config = this.GetSecret<StartupConfig>("databaseprojectapi");
        }

        this.config!.AddClassToServices(pservices);
    }

    public StartupConfig? config { get; set; }
    public KubernetesClientConfiguration? K8sConfig { get; set; }
    public Kubernetes? client { get; set; }

    public T? GetSecret<T>(string EnvironmentVariable)
    {
        return this.DoGetSecret<T>(EnvironmentVariable);
    }

    public T? GetSecret<T>(string K8SNameSpace, string SecretName)
    {
        return this.DoGetSecret<T>(K8SNameSpace, SecretName);
    }

    public T? DoGetSecret<T>(string EnvironmentVariable)
    {
        try
        {
            string? secret = Environment.GetEnvironmentVariable(EnvironmentVariable);
            if (string.IsNullOrEmpty(secret))
            {
                throw new Exception("secret is null or empty: " + EnvironmentVariable);
            }
            else
            {
                Type objtype = typeof(T);
                if (objtype == typeof(string))
                {
                    return (T)Convert.ChangeType(secret, typeof(T));
                }
                else
                {
                
                    return JsonConvert.DeserializeObject<T>(secret);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Unable to get kubernetes secret: " + ex.Message);
        }
    }

    public T? DoGetSecret<T>(string K8SNameSpace, string SecretName)
    {
        try
        {
            V1Secret secret = client.ReadNamespacedSecret(SecretName, K8SNameSpace);
            if (secret == null)
            {
                throw new Exception("Kubernetes secret not found: " + SecretName);
            }
            else
            {
                var dic = secret.Data;
                byte[]? bdata;
                dic.TryGetValue(dic.Keys.First(), out bdata);
                if (bdata != null)
                {
                    Type objtype = typeof(T);
                    if (objtype == typeof(string))
                    {
                        return (T)Convert.ChangeType(Encoding.UTF8.GetString(bdata), typeof(T));
                    }
                    else
                    {
                        return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bdata));
                    }
                }
                else
                {
                    throw new Exception("Unable to get kubernetes secret: " + SecretName + " -  bdata is null");
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Unable to get kubernetes secret: " + SecretName + " - " + ex.Message);
        }
    }
}