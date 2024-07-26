﻿using CloudCrafter.DockerCompose.Shared.Labels;

namespace CloudCrafter.Agent.Models.Docker.Filters;

public class DockerContainerFilter
{
    public List<Guid> CloudCrafterApplicationIds { get; init; } = new();

    public Dictionary<string, bool> GetCloudCrafterApplications()
    {
        return CloudCrafterApplicationIds.ToDictionary(LabelHelper.GenerateApplicationLabel, _ => true);
    }
}
