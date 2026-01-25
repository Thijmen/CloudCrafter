namespace CloudCrafter.Shared.Deployment.Docker.Labels;

public static class CloudCrafterLabelKeys
{
    public static readonly string CloudCrafterManaged = "cloudcrafter.managed";
    public static readonly string StackId = "cloudcrafter.stack.id";
    public static readonly string StackServiceId = "cloudcrafter.stack.service.id";
    public static readonly string DeploymentId = "cloudcrafter.deployment";

    // Pre-defined in Docker Compose files
    public static readonly string ServiceType = "cloudcrafter.service.type";
    public static readonly string ServiceInstanceType = "cloudcrafter.service.instance";
    public static readonly string ServiceVersion = "cloudcrafter.service.version";
}
