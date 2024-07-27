using CloudCrafter.DockerCompose.Engine.Yaml;
using CloudCrafter.Shared.Utils.Http;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace CloudCrafter.Agent.Console.IntegrationTests;

public static class Helpers
{
    private static readonly DockerClient _client;

    static Helpers()
    {
        _client = new DockerClientConfiguration()
            .CreateClient();
    }

    public static async Task ShouldHaveDockerImage(string image)
    {
        var result = await _client.Images
            .InspectImageAsync(image);

        result.Should().NotBeNull();
    }

    public static async Task EnsureNetworkExists(string networkName)
    {
        var networks = await _client.Networks.ListNetworksAsync();
        var network = networks.FirstOrDefault(x => x.Name == networkName);

        if (network == null)
        {
            await _client.Networks.CreateNetworkAsync(new NetworksCreateParameters
            {
                Name = networkName
            });
        }
    }


    public static async Task ShouldHaveEndpointResponse(string url, string responseContains, int statusCode = 200)
    {

        var client = RetryHttpClientFactory.Create();
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        var response = await client.SendAsync(request);
        ((int)response.StatusCode).Should().Be(statusCode);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain(responseContains);
    }

    public static DockerComposeEditor GetDummyEditor(string repository, string tag)
    {
        var editor = new DockerComposeEditor();
        var service = editor.AddService("frontend");

        service.SetImage(repository, tag);

        service.AddExposedPort(3000, 3000);

        return editor;
    }
}
