using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;

namespace CloudCrafter.Agent.Runner.Cli.Helpers;

public class FileSystemHelper : IFileSystemHelper
{
    public Task WriteFile(string file, string contents)
    {
        return File.WriteAllTextAsync(file, contents);
    }

    public Task EnsureDirectoryExists(string directory)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return Task.CompletedTask;
    }
}
