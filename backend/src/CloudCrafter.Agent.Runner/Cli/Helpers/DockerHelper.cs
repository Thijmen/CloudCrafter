using System.Text;
using CloudCrafter.Agent.Models.Docker.Filters;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.Exceptions;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace CloudCrafter.Agent.Runner.Cli.Helpers;

public class DockerHelper(IDockerClientProvider provider) : IDockerHelper
{
    private readonly IDockerClient _client = provider.GetClient();

    public async Task<IList<ContainerListResponse>> GetContainerFromFilter(DockerContainerFilter filter)
    {
        var applications = filter.GetCloudCrafterApplications();

        var dockerFilter = new ContainersListParameters { All = true };

        if (applications.Any())
        {
            // TODO: Add to constructor if more filters in the future
            if (dockerFilter.Filters == null)
            {
                dockerFilter.Filters = new Dictionary<string, IDictionary<string, bool>>();
            }

            dockerFilter.Filters.Add("label", applications);
        }

        var containers = await _client.Containers.ListContainersAsync(dockerFilter);
        return containers;
    }

    public async Task<IList<NetworkResponse>> GetNetworks()
    {
        var networks = await _client.Networks.ListNetworksAsync();

        return networks;
    }

    public async Task<DockerHelperResponseResult> RunCommandInContainer(string containerName, IList<string> commands,
        Action<DockerHelperResponse>? onLog = null)
    {
        var container = await GetContainer(containerName);

        var execParams = new ContainerExecCreateParameters { AttachStdout = true, AttachStderr = true, Cmd = commands };

        var execCreateResponse =
            await _client.Exec.ExecCreateContainerAsync(container.ID, execParams);

        // Get the stream
        using var stream = await _client.Exec.StartAndAttachContainerExecAsync(execCreateResponse.ID, false);
        // Now you can read from the stream

        var buffer = new byte[81920];
        var stdout = new MemoryStream();
        var stderr = new MemoryStream();


        while (true)
        {
            var result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, CancellationToken.None);
            if (result.EOF)
            {
                break;
            }

            var content = Encoding.UTF8.GetString(buffer, 0, result.Count);


            switch (result.Target)
            {
                case MultiplexedStream.TargetStream.StandardOut:
                    onLog?.Invoke(new DockerHelperResponse { Response = content, IsStdOut = true });
                    stdout.Write(buffer, 0, result.Count);
                    break;
                case MultiplexedStream.TargetStream.StandardError:
                    onLog?.Invoke(new DockerHelperResponse { Response = content, IsStdOut = false });
                    stderr.Write(buffer, 0, result.Count);
                    break;
            }
        }

        var stdoutContent = Encoding.UTF8.GetString(stdout.ToArray());
        var stderrContent = Encoding.UTF8.GetString(stderr.ToArray());

        var execInspectResponse = await _client.Exec.InspectContainerExecAsync(execCreateResponse.ID);
        var exitCode = execInspectResponse.ExitCode;
        return new DockerHelperResponseResult { ExitCode = exitCode, StdOut = stdoutContent, StdErr = stderrContent };
    }

    public Task<ContainerInspectResponse> GetDockerContainer(string containerId)
    {
        return _client.Containers.InspectContainerAsync(containerId);
    }




    private async Task<ContainerInspectResponse> GetContainer(string containerName)
    {
        try
        {
            var container = await _client.Containers.InspectContainerAsync(containerName);

            if (container.State is not { Running: true })
            {
                throw new AgentDockerException($"Container with name '{containerName}' is not in running state");
            }

            return container;
        }
        catch (Exception ex)
        {
            throw new AgentDockerException($"Container '{containerName}' is not running", ex);
        }
    }
}

public class DockerHelperResponse
{
    public required string Response { get; set; }
    public required bool IsStdOut { get; set; }
}

public class DockerHelperResponseResult
{
    public long ExitCode { get; init; }
    public string? StdOut { get; init; }
    public string? StdErr { get; init; }
}
