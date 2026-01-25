using System.Text;
using System.Text.RegularExpressions;
using CloudCrafter.DockerCompose.Engine.Exceptions;
using CloudCrafter.DockerCompose.Engine.Models;
using CloudCrafter.DockerCompose.Engine.Validator;
using Slugify;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace CloudCrafter.DockerCompose.Engine.Yaml;

public class DockerComposeEditor
{
    private static readonly Regex ServiceNameRegex =
        new(@"^[a-z0-9][a-z0-9_-]*$", RegexOptions.Compiled);

    private readonly Dictionary<string, EnvironmentVariableCollection> _environmentVariables =
        new();

    private readonly YamlMappingNode rootNode;
    private readonly YamlMappingNode servicesNode;

    private readonly SlugHelper SlugHelper = new();
    private readonly YamlStream yaml;
    private YamlMappingNode? networksNode;
    private YamlMappingNode? volumesNode;

    public DockerComposeEditor(string yamlString)
    {
        yaml = new YamlStream();
        using (var reader = new StringReader(yamlString))
        {
            yaml.Load(reader);
        }

        rootNode = (YamlMappingNode)yaml.Documents[0].RootNode;

        // Ensure services node exists
        if (!rootNode.Children.ContainsKey(new YamlScalarNode("services")))
        {
            // Or handle the case where 'services' might be missing, e.g., throw exception or create it
            servicesNode = new YamlMappingNode();
            rootNode.Add("services", servicesNode);
        }
        else
        {
            servicesNode = (YamlMappingNode)rootNode["services"];
            // Normalize labels for all existing services upon loading
            NormalizeLabelsForAllServices();
        }

        if (rootNode.Children.ContainsKey(new YamlScalarNode("networks")))
        {
            networksNode = (YamlMappingNode)rootNode["networks"];
        }

        if (rootNode.Children.ContainsKey(new YamlScalarNode("volumes")))
        {
            volumesNode = (YamlMappingNode)rootNode["volumes"];
        }
    }

    public DockerComposeEditor()
    {
        yaml = new YamlStream { new YamlDocument(new YamlMappingNode()) };

        rootNode = (YamlMappingNode)yaml.Documents[0].RootNode;
        servicesNode = new YamlMappingNode();
        rootNode.Add("services", servicesNode);
    }

    public void SetEnvironmentVariables(
        string filename,
        List<EnvironmentVariable> environmentVariables
    )
    {
        _environmentVariables.Add(
            filename,
            new EnvironmentVariableCollection(environmentVariables)
        );
    }

    public Dictionary<string, EnvironmentVariableCollection> GetEnvironmentVariables()
    {
        return _environmentVariables;
    }

    private bool IsValidServiceName(string serviceName)
    {
        return !string.IsNullOrWhiteSpace(serviceName) && ServiceNameRegex.IsMatch(serviceName);
    }

    private string SanitizeServiceName(string serviceName)
    {
        // Slugify the service name
        var slugified = SlugHelper.GenerateSlug(serviceName).ToLower();

        // Ensure it starts with a letter or number (prepend 's' if it doesn't)
        if (!char.IsLetterOrDigit(slugified[0]))
        {
            slugified = "s" + slugified;
        }

        return slugified;
    }

    public ServiceEditor Service(string serviceName)
    {
        var serviceNode = GetServiceNode(serviceName);

        return new ServiceEditor(this, serviceName);
    }

    public List<string> Services()
    {
        return servicesNode.Children.Keys.Select(key => key.ToString()).ToList();
    }

    public ServiceEditor AddService(string serviceName)
    {
        var sanitizedName = SanitizeServiceName(serviceName);
        if (!IsValidServiceName(sanitizedName))
        {
            throw new ArgumentException(
                $"Unable to create a valid service name from '{serviceName}'. Please provide a name that can be converted to a valid Docker Compose service name."
            );
        }

        if (servicesNode.Children.ContainsKey(sanitizedName))
        {
            throw new ServiceAlreadyExistsException(sanitizedName);
        }

        var serviceNode = new YamlMappingNode();
        servicesNode.Add(sanitizedName, serviceNode);
        return new ServiceEditor(this, sanitizedName);
    }

    public NetworkEditor AddNetwork(string networkName)
    {
        if (networksNode == null)
        {
            networksNode = new YamlMappingNode();
            rootNode.Add("networks", networksNode);
        }

        if (networksNode.Children.ContainsKey(networkName))
        {
            throw new NetworkAlreadyExistsException(networkName);
        }

        var networkNode = new YamlMappingNode();
        networksNode.Add(networkName, networkNode);

        return new NetworkEditor(this, networkName);
    }

    public NetworkEditor Network(string networkName)
    {
        var networkNode = GetNetworkNode(networkName);

        return new NetworkEditor(this, networkName);
    }

    public VolumeEditor AddVolume(string volumeName)
    {
        if (volumesNode == null)
        {
            volumesNode = new YamlMappingNode();
            rootNode.Add("volumes", volumesNode);
        }

        if (volumesNode.Children.ContainsKey(volumeName))
        {
            throw new VolumeAlreadyExistsException(volumeName);
        }

        var volumeNode = new YamlMappingNode();
        volumesNode.Add(volumeName, volumeNode);

        return new VolumeEditor(this, volumeName);
    }

    public VolumeEditor Volume(string volumeName)
    {
        var volumeNode = GetVolumeNode(volumeName);
        return new VolumeEditor(this, volumeName);
    }

    public string ToBase64()
    {
        var yaml = GetYaml();
        var bytes = Encoding.UTF8.GetBytes(yaml);
        return Convert.ToBase64String(bytes);
    }

    public string GetYaml()
    {
        if (servicesNode.Children.Count == 0)
        {
            throw new DockerComposeInvalidStateException("No services defined");
        }

        using (var writer = new StringWriter())
        {
            yaml.Save(writer, false);
            var yamlString = writer.ToString();

            // Remove the trailing "..."
            if (yamlString.EndsWith("...\n"))
            {
                yamlString = yamlString.Substring(0, yamlString.Length - 4);
            }

            return yamlString;
        }
    }

    public Task<DockerComposeValidator.Result> IsValid()
    {
        var yaml = GetYaml();
        var validator = new DockerComposeValidator(this);

        return validator.IsValid();
    }

    private YamlMappingNode GetServiceNode(string serviceName)
    {
        try
        {
            return (YamlMappingNode)servicesNode[serviceName];
        }
        catch (KeyNotFoundException)
        {
            throw new InvalidServiceException(serviceName);
        }
    }

    private YamlMappingNode GetNetworkNode(string networkName)
    {
        try
        {
            if (networksNode == null)
            {
                throw new DockerComposeInvalidStateException("Networks are not created or defined");
            }

            return (YamlMappingNode)networksNode[networkName];
        }
        catch (KeyNotFoundException)
        {
            throw new InvalidNetworkException(networkName);
        }
    }

    private YamlMappingNode GetVolumeNode(string volumeName)
    {
        try
        {
            if (volumesNode == null)
            {
                throw new DockerComposeInvalidStateException("Volumes are not created or defined");
            }

            var node = volumesNode[volumeName]; // Get the node

            if (node is YamlMappingNode mappingNode)
            {
                return mappingNode; // Already a mapping, return it
            }

            // If it's a scalar (e.g., null or just the key name), treat as default.
            // Replace it with an empty mapping node for consistency and future edits.
            if (node is YamlScalarNode)
            {
                var newMappingNode = new YamlMappingNode();
                volumesNode.Children[new YamlScalarNode(volumeName)] = newMappingNode; // Replace node in the document
                return newMappingNode;
            }

            // Handle unexpected node types if necessary
            throw new DockerComposeInvalidStateException(
                $"Unexpected node type '{node.GetType().Name}' for volume '{volumeName}'. Expected YamlMappingNode or YamlScalarNode."
            );
        }
        catch (KeyNotFoundException)
        {
            throw new InvalidVolumeException(volumeName);
        }
    }

    public class NetworkEditor(DockerComposeEditor Editor, string NetworkName)
    {
        public NetworkEditor SetKeyValue(string key, string value)
        {
            var networkNode = Editor.GetNetworkNode(NetworkName);

            networkNode.Add(key, value);

            return this;
        }

        public NetworkEditor SetNetworkName(string networkName)
        {
            return SetKeyValue("name", networkName);
        }

        public NetworkEditor SetIsExternalNetwork()
        {
            return SetKeyValue("external", "true");
        }

        public string GetNetworkName()
        {
            return NetworkName;
        }
    }

    public class ServiceEditor
    {
        private readonly DockerComposeEditor editor;
        private readonly string serviceName;

        public ServiceEditor(DockerComposeEditor editor, string serviceName)
        {
            this.editor = editor;
            this.serviceName = serviceName;
        }

        public string ServiceName()
        {
            return serviceName;
        }

        public ServiceEditor AddLabel(string key, string value)
        {
            var serviceNode = editor.GetServiceNode(serviceName);
            YamlMappingNode labelsMappingNode;

            if (!serviceNode!.Children.ContainsKey("labels"))
            {
                // If 'labels' doesn't exist, create a new mapping node
                labelsMappingNode = new YamlMappingNode();
                serviceNode.Add("labels", labelsMappingNode);
            }
            else
            {
                var labelsNode = serviceNode["labels"];

                // If it's a sequence, convert it to a mapping
                if (labelsNode is YamlSequenceNode sequenceNode)
                {
                    labelsMappingNode = new YamlMappingNode();
                    foreach (var item in sequenceNode)
                    {
                        if (item is YamlScalarNode scalarItem)
                        {
                            var label = scalarItem.Value;
                            var parts = label?.Split('=', 2);
                            if (parts?.Length == 2)
                            {
                                // Add existing items from sequence to the new map, ensuring quotes
                                labelsMappingNode.Add(
                                    new YamlScalarNode(parts[0]),
                                    new YamlScalarNode(parts[1])
                                    {
                                        Style = ScalarStyle.DoubleQuoted,
                                    }
                                );
                            }
                            else if (!string.IsNullOrEmpty(label))
                            {
                                // Handle cases like just "label" without a value if needed, maybe add as key with empty string?
                                // For now, just adding with empty value and quotes. Adjust if needed.
                                labelsMappingNode.Add(
                                    new YamlScalarNode(label),
                                    new YamlScalarNode("") { Style = ScalarStyle.DoubleQuoted }
                                );
                            }
                        }
                    }
                    // Replace the sequence node with the new mapping node
                    serviceNode.Children[new YamlScalarNode("labels")] = labelsMappingNode;
                }
                // If it's already a mapping, just use it
                else if (labelsNode is YamlMappingNode mappingNode)
                {
                    labelsMappingNode = mappingNode;
                }
                // If it's something unexpected, create a new mapping node (overwriting)
                else
                {
                    labelsMappingNode = new YamlMappingNode();
                    serviceNode.Children[new YamlScalarNode("labels")] = labelsMappingNode;
                }
            }

            // Add the new label to the mapping node, ensuring the value is double-quoted
            labelsMappingNode.Add(
                new YamlScalarNode(key), // Key usually doesn't need quotes unless it has special chars
                new YamlScalarNode(value) { Style = ScalarStyle.DoubleQuoted } // Force double quotes for value
            );

            return this;
        }

        public string? GetLabelValue(string key)
        {
            var serviceNode = editor.GetServiceNode(serviceName);

            if (!serviceNode.Children.ContainsKey("labels"))
            {
                return null;
            }

            var labelsNode = serviceNode["labels"];

            // Handle mapping format (key: value)
            if (labelsNode is YamlMappingNode mappingNode)
            {
                if (!mappingNode.Children.ContainsKey(key))
                {
                    return null;
                }

                return ((YamlScalarNode)mappingNode[key]).Value;
            }

            // Handle sequence format (array of "key=value")
            if (labelsNode is YamlSequenceNode sequenceNode)
            {
                foreach (var item in sequenceNode)
                {
                    var label = ((YamlScalarNode)item).Value;
                    var parts = label?.Split('=', 2);
                    if (parts?.Length == 2 && parts[0] == key)
                    {
                        return parts[1];
                    }
                }
            }

            return null;
        }

        public ServiceEditor AddLabels(DockerComposeLabelService labelService)
        {
            var labels = labelService.ToDictionary();

            foreach (var label in labels)
            {
                AddLabel(label.Key, label.Value);
            }

            return this;
        }

        public ServiceEditor SetImage(string image, string tag)
        {
            var serviceNode = editor.GetServiceNode(serviceName);

            var imageTag = $"{image}:{tag}";
            if (serviceNode.Children.ContainsKey("image"))
            {
                var imageNode = (YamlScalarNode)serviceNode["image"];
                imageNode.Value = imageTag;
                return this;
            }

            serviceNode!.Add("image", imageTag);
            return this;
        }

        public ServiceEditor SetEnvironmentFilename(string environmentFilename)
        {
            var serviceNode = editor.GetServiceNode(serviceName);

            if (serviceNode.Children.ContainsKey("env_file"))
            {
                var envNode = (YamlScalarNode)serviceNode["env_file"];
                envNode.Value = environmentFilename;
                return this;
            }

            serviceNode!.Add("env_file", environmentFilename);
            return this;
        }

        public ServiceEditor AddEnvironmentVariable(string key, string value)
        {
            var serviceNode = editor.GetServiceNode(serviceName);
            if (!serviceNode!.Children.ContainsKey("environment"))
            {
                serviceNode.Add("environment", new YamlMappingNode());
            }

            var envNode = (YamlMappingNode)serviceNode["environment"];
            envNode.Add(key, new YamlScalarNode(value));
            return this;
        }

        public ServiceEditor AddVolume(string hostPath, string containerPath)
        {
            var serviceNode = editor.GetServiceNode(serviceName);
            if (!serviceNode!.Children.ContainsKey("volumes"))
            {
                serviceNode.Add("volumes", new YamlSequenceNode());
            }

            var volumesNode = (YamlSequenceNode)serviceNode["volumes"];
            volumesNode.Add(new YamlScalarNode($"{hostPath}:{containerPath}"));
            return this;
        }

        public ServiceEditor AddNetwork(NetworkEditor network)
        {
            var serviceNode = editor.GetServiceNode(serviceName);

            YamlSequenceNode networks;
            if (!serviceNode!.Children.ContainsKey("networks"))
            {
                networks = new YamlSequenceNode();
                serviceNode.Add("networks", networks);
            }
            else
            {
                networks = (YamlSequenceNode)serviceNode["networks"];
            }

            networks.Add(new YamlScalarNode(network.GetNetworkName()));

            return this;
        }

        public ServiceEditor AddExposedPort(int hostPort, int containerPort)
        {
            var serviceNode = editor.GetServiceNode(serviceName);

            YamlSequenceNode ports;
            if (!serviceNode!.Children.ContainsKey("ports"))
            {
                ports = new YamlSequenceNode();
                serviceNode.Add("ports", ports);
            }
            else
            {
                ports = (YamlSequenceNode)serviceNode["ports"];
            }

            ports.Add(new YamlScalarNode($"{hostPort}:{containerPort}"));

            return this;
        }

        public bool HasHealthCheck()
        {
            var serviceNode = editor.GetServiceNode(serviceName);
            return serviceNode!.Children.ContainsKey("healthcheck");
        }

        public ServiceEditor AddHealthCheck(ServiceHealthCheck healthCheck)
        {
            var serviceNode = editor.GetServiceNode(serviceName);
            var healthcheckNode = new YamlMappingNode();

            healthcheckNode.Add("test", healthCheck.Test);
            if (healthCheck.IntervalSeconds.HasValue)
            {
                healthcheckNode.Add("interval", $"{healthCheck.IntervalSeconds}s");
            }

            if (healthCheck.TimeoutSeconds.HasValue)
            {
                healthcheckNode.Add("timeout", $"{healthCheck.TimeoutSeconds}s");
            }

            if (healthCheck.Retries.HasValue)
            {
                healthcheckNode.Add("retries", healthCheck.Retries.Value.ToString());
            }

            if (healthCheck.StartPeriodSeconds.HasValue)
            {
                healthcheckNode.Add("start_period", $"{healthCheck.StartPeriodSeconds}s");
            }

            serviceNode.Add("healthcheck", healthcheckNode);
            return this;
        }

        public Dictionary<string, string> GetEnvironmentVariables()
        {
            var serviceNode = editor.GetServiceNode(serviceName);
            var environmentVariables = new Dictionary<string, string>();

            if (serviceNode!.Children.ContainsKey("environment"))
            {
                var envNode = serviceNode["environment"];
                if (envNode is YamlMappingNode mappingNode)
                {
                    foreach (var entry in mappingNode.Children)
                    {
                        if (
                            entry.Key is YamlScalarNode keyNode
                            && entry.Value is YamlScalarNode valueNode
                        )
                        {
                            environmentVariables.Add(keyNode.Value!, valueNode.Value ?? "");
                        }
                    }
                }
                // Optionally handle sequence format if needed, though mapping is more common
            }

            return environmentVariables;
        }

        public List<(string HostPath, string ContainerPath, bool IsDockerVolume)> GetVolumes()
        {
            var serviceNode = editor.GetServiceNode(serviceName);
            var volumes = new List<(string HostPath, string ContainerPath, bool IsDockerVolume)>();

            if (serviceNode!.Children.ContainsKey("volumes"))
            {
                var volumesNode = serviceNode["volumes"];
                if (volumesNode is YamlSequenceNode sequenceNode)
                {
                    foreach (var item in sequenceNode)
                    {
                        if (item is YamlScalarNode scalarItem)
                        {
                            var volume = scalarItem.Value;
                            if (!string.IsNullOrEmpty(volume))
                            {
                                var parts = volume.Split(':', 2);
                                if (parts.Length == 2)
                                {
                                    // Check if the host path is a Docker volume by looking at the root node's volumes
                                    var isDockerVolume =
                                        editor.rootNode.Children.ContainsKey("volumes")
                                        && (
                                            (YamlMappingNode)editor.rootNode["volumes"]
                                        ).Children.ContainsKey(parts[0]);

                                    volumes.Add((parts[0], parts[1], isDockerVolume));
                                }
                            }
                        }
                    }
                }
            }

            return volumes;
        }
    }

    public class VolumeEditor
    {
        private readonly DockerComposeEditor editor;
        private readonly string volumeName;

        public VolumeEditor(DockerComposeEditor editor, string volumeName)
        {
            this.editor = editor;
            this.volumeName = volumeName;
        }

        public string VolumeName()
        {
            return volumeName;
        }

        public VolumeEditor SetDriver(string driver)
        {
            var volumeNode = editor.GetVolumeNode(volumeName);
            volumeNode.Add("driver", driver);
            return this;
        }

        public VolumeEditor SetDriverOpt(string key, string value)
        {
            var volumeNode = editor.GetVolumeNode(volumeName);

            if (!volumeNode.Children.ContainsKey("driver_opts"))
            {
                volumeNode.Add("driver_opts", new YamlMappingNode());
            }

            var driverOptsNode = (YamlMappingNode)volumeNode["driver_opts"];
            driverOptsNode.Add(key, value);
            return this;
        }

        public VolumeEditor SetExternal(bool isExternal)
        {
            var volumeNode = editor.GetVolumeNode(volumeName);
            volumeNode.Add("external", isExternal.ToString().ToLower());
            return this;
        }

        public VolumeEditor SetName(string name)
        {
            var volumeNode = editor.GetVolumeNode(volumeName);
            volumeNode.Add("name", name);
            return this;
        }
    }

    private void NormalizeLabelsForAllServices()
    {
        foreach (var serviceEntry in servicesNode.Children)
        {
            if (serviceEntry.Value is YamlMappingNode serviceNode)
            {
                NormalizeLabelsForService(serviceNode);
            }
        }
    }

    private void NormalizeLabelsForService(YamlMappingNode serviceNode)
    {
        var labelsKey = new YamlScalarNode("labels");
        if (!serviceNode.Children.ContainsKey(labelsKey))
        {
            return; // No labels to normalize
        }

        var labelsNode = serviceNode.Children[labelsKey];
        var newLabelsMappingNode = new YamlMappingNode();

        // Case 1: Labels are a sequence (e.g., - "key=value")
        if (labelsNode is YamlSequenceNode sequenceNode)
        {
            foreach (var item in sequenceNode)
            {
                if (item is YamlScalarNode scalarItem && scalarItem.Value != null)
                {
                    var parts = scalarItem.Value.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        newLabelsMappingNode.Add(
                            new YamlScalarNode(parts[0]),
                            new YamlScalarNode(parts[1]) { Style = ScalarStyle.DoubleQuoted }
                        );
                    }
                    else if (parts.Length == 1 && !string.IsNullOrEmpty(parts[0]))
                    {
                        // Handle label with no value, add as key: ""
                        newLabelsMappingNode.Add(
                            new YamlScalarNode(parts[0]),
                            new YamlScalarNode("") { Style = ScalarStyle.DoubleQuoted }
                        );
                    }
                }
            }
            // Replace sequence with the new mapping
            serviceNode.Children[labelsKey] = newLabelsMappingNode;
        }
        // Case 2: Labels are already a mapping
        else if (labelsNode is YamlMappingNode mappingNode)
        {
            foreach (var entry in mappingNode.Children)
            {
                if (entry.Key is YamlScalarNode keyNode && entry.Value is YamlScalarNode valueNode)
                {
                    // Ensure existing values have double quotes
                    newLabelsMappingNode.Add(
                        keyNode, // Keep original key node
                        new YamlScalarNode(valueNode.Value ?? "")
                        {
                            Style = ScalarStyle.DoubleQuoted,
                        } // Ensure value is quoted
                    );
                }
                // Potentially handle non-scalar keys/values if necessary, though unlikely for labels
            }
            // Replace original mapping with the new one (ensuring quoted values)
            serviceNode.Children[labelsKey] = newLabelsMappingNode;
        }
        // Else: Unexpected format, maybe log a warning or ignore.
        // Currently, it will leave the labels node as is if it's not sequence or mapping.
    }
}
