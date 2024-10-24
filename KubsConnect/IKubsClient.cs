using System;
using k8s;
using KubsConnect.Settings;

namespace KubsConnect;

public interface IKubsClient
{
    KubernetesClientConfiguration? K8sConfig { get; set; }
    Kubernetes? client { get; set; }
    StartupConfig? config { get; set; }
    T? GetSecret<T>(string EnvironmentVariable);
    T? GetSecret<T>(string K8SNameSpace, string SecretName);
}