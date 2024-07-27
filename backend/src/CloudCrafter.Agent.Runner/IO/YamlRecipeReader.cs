using CloudCrafter.Agent.Models.Recipe;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CloudCrafter.Agent.Runner.IO;

public class YamlRecipeReader
{
    public DeploymentRecipe FromBase64(string base64)
    {
        var yaml = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(base64));

        return FromString(yaml);
    }

    public DeploymentRecipe FromString(string yaml)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var recipe = deserializer.Deserialize<DeploymentRecipe>(yaml);


            if (recipe == null)
            {
                throw new InvalidOperationException("Deserialization resulted in a null object.");
            }


            return recipe;
        }
        catch (YamlException ex)
        {
            throw new InvalidOperationException("Failed to deserialize YAML: " + ex.Message, ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while deserializing YAML: " + ex.Message,
                ex);
        }
    }
}
