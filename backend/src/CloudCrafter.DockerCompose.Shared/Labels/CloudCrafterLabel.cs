namespace CloudCrafter.DockerCompose.Shared.Labels;

public class CloudCrafterLabel(CloudCrafterLabelKey Key, string Value)
{
    private string GetLabelKey()
    {
        switch (Key)
        {
            case CloudCrafterLabelKey.ApplicationId:
                return "cloudcrafter.application";
        }
        
        throw new KeyNotFoundException($"Key not found in CloudCrafterLabel, Key: {Key}");
    }
    
    public (string, string) ToDockerComposeLabel()
    {
        var labelKey = GetLabelKey();
        
        return (labelKey, Value);
    }

    public string ToLabelString()
    {
        var labelKey = GetLabelKey();

        return $"{labelKey}={Value}";
    }
}

public enum CloudCrafterLabelKey
{
    ApplicationId
}
