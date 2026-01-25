using CloudCrafter.DockerCompose.Engine.Yaml;
using CloudCrafter.Shared.Deployment.Docker.Labels;

namespace CloudCrafter.Core.Utils;

public class LabelIdentifier(DockerComposeEditor.ServiceEditor service)
{
    private string? GetService()
    {
        return service.GetLabelValue(CloudCrafterLabelKeys.ServiceType);
    }

    private string? GetServiceInstance()
    {
        return service.GetLabelValue(CloudCrafterLabelKeys.ServiceInstanceType);
    }

    public bool IsDatabase()
    {
        return GetService() == "database";
    }

    public bool IsDatabasePostgres()
    {
        return GetServiceInstance() == "postgres";
    }

    public string? GetInstanceVersion()
    {
        return service.GetLabelValue(CloudCrafterLabelKeys.ServiceVersion);
    }
}
