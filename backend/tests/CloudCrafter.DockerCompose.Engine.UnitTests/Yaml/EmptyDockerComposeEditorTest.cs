using System.ComponentModel;
using CloudCrafter.DockerCompose.Engine.Exceptions;
using CloudCrafter.DockerCompose.Engine.Yaml;
using CloudCrafter.DockerCompose.Shared.Labels;
using Docker.DotNet;
using FluentAssertions;

namespace CloudCrafter.DockerCompose.Engine.UnitTests.Yaml;

public class EmptyDockerComposeEditorTest
{
    [Test]
    public void ShouldBeAbleToCreateEmptyDockerComposeEditor()
    {
        var editor = new DockerComposeEditor();
        editor.Should().NotBeNull();
    }

    [Test]
    public void ShouldNotBeAbleToGetNonExistingNetworkWhenNodeDoesNotExists()
    {
        var editor = new DockerComposeEditor();

        var exception = Assert.Throws<DockerComposeInvalidStateException>(() => editor.Network("test"));
        exception.Message.Should().Be("Networks are not created or defined");
    }

    [Test]
    public void ShouldNotBeAbleToGetNonExistingNetwork()
    {
        var editor = new DockerComposeEditor();
        editor.AddNetwork("my-network")
            .SetNetworkName("my-network");

        var exception = Assert.Throws<InvalidNetworkException>(() => editor.Network("test"));
        exception.Message.Should().Be("Network test is invalid");
    }

    [Test]
    public void ShouldNotBeAbleToAddAlreadyExistingNetwork()
    {
        var editor = new DockerComposeEditor();
        editor.AddNetwork("my-network")
            .SetNetworkName("my-network");

        var exception = Assert.Throws<NetworkAlreadyExistsException>(() => editor.AddNetwork("my-network"));
        exception.Message.Should().Be("Network my-network already exists");
    }

    [Test]
    public void ShouldBeAbleToGetNetwork()
    {
        var editor = new DockerComposeEditor();
        editor.AddNetwork("my-network")
            .SetNetworkName("my-network");

        var networkFromEditor = editor.Network("my-network");

        networkFromEditor.Should().NotBeNull();
    }

    [Test]
    public void ShouldBeAbleToAddNetworkToService()
    {
        var editor = new DockerComposeEditor();
        var network = editor.AddNetwork("my-network")
            .SetNetworkName("my-network");


        var service = editor.AddService("my-service")
            .AddNetwork(network);
        
        service.Should().NotBeNull();
    }

    [Test]
    public async Task ShouldBeAbleToAddLabelsFromLabelService()
    {
        var editor = new DockerComposeEditor();
        var service = editor.AddService("frontend");

        service.SetImage("nginx", "latest");

        var id = Guid.Parse("ddf9e4a2-3358-442d-8781-30daf32fd59d");
        var labelsService = new DockerComposeLabelService();
        
        labelsService.AddLabel(LabelFactory.GenerateApplicationLabel(id));
        labelsService.AddTraefikLabels(new()
        {
            Service = "frontend",
            Rule = "Host(`example.com`)"
        });
        
        
        service.AddLabels(labelsService);

        var isValid = await editor.IsValid();

        isValid.Should().BeTrue();
        
        var yaml = editor.GetYaml();

        await Verify(yaml);
    }

    [Test]
    public void ShouldNotBeAbleToGetTheYamlFromAnEmptyDockerComposeEditor()
    {
        var editor = new DockerComposeEditor();

        var exception = Assert.Throws<DockerComposeInvalidStateException>(() => editor.GetYaml());
        exception.Message.Should().Be("No services defined");
    }

    [Test]
    public Task ShouldBeAbleToCreateServiceForEmptyDockerComposeEditor()
    {
        var editor = new DockerComposeEditor();

        var serviceEditor = editor.AddService("service1");
        serviceEditor.Should().NotBeNull();

        serviceEditor.SetImage("redis", "alpine");
        serviceEditor.AddLabel("label1", "value1");
        serviceEditor.AddVolume("/dev-path", "/dev-path");
        serviceEditor.AddEnvironmentVariable("DEV", "1");
        serviceEditor.AddEnvironmentVariable("DB_NAME", "example");

        serviceEditor.AddExposedPort(80, 80);
        serviceEditor.AddExposedPort(443, 443);
        
        var yaml = editor.GetYaml();

        return Verify(yaml);
    }

    [Test]
    public Task ShouldBeAbleToAddNetwork()
    {
        var editor = new DockerComposeEditor();
        editor.AddService("test")
            .AddLabel("test", "true");

        var network = editor.AddNetwork("network1");
        network.SetNetworkName("network1");
        var yaml = editor.GetYaml();
        
        return Verify(yaml);
    }
    
    [Test]
    public Task ShouldBeAbleToAddNetworkWithExternalNetwork()
    {
        var editor = new DockerComposeEditor();
        var service = editor.AddService("test")
            .AddLabel("test", "true");

        var network = editor.AddNetwork("cloudcrafter");
        network.SetNetworkName("cloudcrafter")
            .SetIsExternalNetwork();

        service.AddNetwork(network);
        
        var yaml = editor.GetYaml();
        
        return Verify(yaml);
    }
}
