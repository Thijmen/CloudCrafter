namespace CloudCrafter.DockerCompose.Shared.Labels;

public abstract class LabelFactory
{
    public static CloudCrafterLabel GenerateApplicationLabel(Guid applicationId)
    {
        var label = new CloudCrafterLabel(CloudCrafterLabelKey.ApplicationId, applicationId.ToString());
        return label;
    }
}
