using System.Text;
using CloudCrafter.DockerCompose.Engine.Exceptions;
using CloudCrafter.DockerCompose.Engine.Validator;
using CloudCrafter.DockerCompose.Shared.Labels;
using YamlDotNet.RepresentationModel;

namespace CloudCrafter.DockerCompose.Engine.Yaml;

public class DockerComposeEditor
{
    private YamlStream yaml;
    private YamlMappingNode rootNode;
    private YamlMappingNode servicesNode;
    private YamlMappingNode? networksNode;

    public DockerComposeEditor(string yamlString)
    {
        yaml = new YamlStream();
        using (var reader = new StringReader(yamlString))
        {
            yaml.Load(reader);
        }

        rootNode = (YamlMappingNode)yaml.Documents[0].RootNode;
        servicesNode = (YamlMappingNode)rootNode["services"];

        if (rootNode.Children.ContainsKey("networks"))
        {
            networksNode = (YamlMappingNode)rootNode["networks"];
        }
    }

    public DockerComposeEditor()
    {
        yaml = new YamlStream { new YamlDocument(new YamlMappingNode()) };

        rootNode = (YamlMappingNode)yaml.Documents[0].RootNode;
        servicesNode = new YamlMappingNode();
        rootNode.Add("services", servicesNode);
    }


    public ServiceEditor Service(string serviceName)
    {
        var serviceNode = GetServiceNode(serviceName);

        return new ServiceEditor(this, serviceName);
    }

    public ServiceEditor AddService(string serviceName)
    {
        if (servicesNode.Children.ContainsKey(serviceName))
        {
            throw new ServiceAlreadyExistsException(serviceName);
        }

        var serviceNode = new YamlMappingNode();
        servicesNode.Add(serviceName, serviceNode);
        return new ServiceEditor(this, serviceName);
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

    public Task<bool> IsValid()
    {
        var validator = new DockerComposeValidator(GetYaml());

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
        private DockerComposeEditor editor;
        private string serviceName;

        public ServiceEditor(DockerComposeEditor editor, string serviceName)
        {
            this.editor = editor;
            this.serviceName = serviceName;
        }

        public ServiceEditor AddLabel(string key, string value)
        {
            var serviceNode = editor.GetServiceNode(serviceName);
            if (!serviceNode!.Children.ContainsKey("labels"))
            {
                serviceNode.Add("labels", new YamlMappingNode());
            }

            var labelsNode = (YamlMappingNode)serviceNode["labels"];
            labelsNode.Add(key, new YamlScalarNode(value));
            return this;
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
    }
}
