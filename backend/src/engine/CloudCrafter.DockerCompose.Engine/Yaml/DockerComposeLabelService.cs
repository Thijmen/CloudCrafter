using CloudCrafter.DockerCompose.Shared.Labels;

namespace CloudCrafter.DockerCompose.Engine.Yaml;

public class DockerComposeLabelService
{
    private Dictionary<string, string> _labels = new();

    public void AddLabel(CloudCrafterLabel label)
    {
        var dockerComposeLabel = label.ToDockerComposeLabel();
        _labels.Add(dockerComposeLabel.Item1, dockerComposeLabel.Item2);
    }

    public void AddTraefikLabels(DockerComposeLabelServiceTraefikOptions traefikOptions)
    {
        var routerBase = $"traefik.http.routers.{traefikOptions.Service}";
        var serviceBase = $"traefik.http.services.{traefikOptions.Service}";
        
        AddLabel("traefik.enable", "true");
        AddLabel($"{routerBase}.rule", traefikOptions.Rule);
        AddLabel($"{routerBase}.entrypoints", "web");

        if (traefikOptions.LoadBalancerPort.HasValue)
        {
            AddLabel($"{routerBase}.service", traefikOptions.Service);
            AddLabel($"{serviceBase}.loadbalancer.server.port", traefikOptions.LoadBalancerPort.Value.ToString());
        }
    }

    private void AddLabel(string key, string value)
    {
        if (!_labels.TryAdd(key, value))
        {
            throw new ArgumentException($"Label with key {key} already exists");
        }
    }


    public List<string> ToLabelList()
    {
        return _labels.Select(x => $"{x.Key}={x.Value}").ToList();
    }
    
    public Dictionary<string, string> ToDictionary()
    {
        return _labels;
    }
}

public class DockerComposeLabelServiceTraefikOptions
{
    public required string Service { get; init; }
    public required string Rule { get; init; }

    public int? LoadBalancerPort { get; init; }
}
