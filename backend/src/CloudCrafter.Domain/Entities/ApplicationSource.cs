﻿namespace CloudCrafter.Domain.Entities;

public class ApplicationSource
{
    public required ApplicationSourceType Type { get; init; }

    // When Type == Type.Git
    public ApplicationSourceGit? Git { get; set; }
}

public class ApplicationSourceGit
{
    public required string Repository { get; init; }
    public string? Branch { get; init; }
}

public enum ApplicationSourceType
{
    Git,
    GitSsh,
}
