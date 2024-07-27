using System.Text;
using CloudCrafter.Agent.Runner.IO;
using CloudCrafter.Agent.Runner.Tests.Fakers;
using FluentAssertions;

namespace CloudCrafter.Agent.Runner.Tests.IO;

public class YamlRecipeReaderTest
{
    private YamlRecipeReader _reader;

    [SetUp]
    public void Setup()
    {
        _reader = new YamlRecipeReader();
    }

    [Test]
    public void ShouldNotBeAbleToCreateRecipeFromEmptyString()
    {
        Assert.Throws<InvalidOperationException>(() => _reader.FromString(string.Empty))
            .Message.Should()
            .Be("An unexpected error occurred while deserializing YAML: Deserialization resulted in a null object.");
    }

    [Test]
    public void ShouldNotBeAbleToCreateRecipeFromMalformedString()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => _reader.FromString("malformed"));

        exception.Message.Should().Be("Failed to deserialize YAML: Exception during deserialization");
    }
    
    [Test]
    public void ShouldBeAbleToCreateRecipeFromValidString()
    {
        var recipe = AgentFakers.DeploymentRecipeFaker().Generate();
        var yaml = new YamlRecipeWriter(recipe).WriteString();
        
        var result = _reader.FromString(yaml);

        result.Should().BeEquivalentTo(recipe);
    }

    [Test]
    public void ShouldBeAbleToCreateRecipeFromBase64String()
    {
        var recipe = AgentFakers.DeploymentRecipeFaker().Generate();
        var yaml = new YamlRecipeWriter(recipe).WriteString();

        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(yaml));
        
        var result = _reader.FromBase64(base64);

        result.Should().BeEquivalentTo(recipe);
        
    }
}
