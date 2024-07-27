using CloudCrafter.DockerCompose.Engine.Yaml;
using CloudCrafter.DockerCompose.Shared.Labels;
using FluentAssertions;

namespace CloudCrafter.DockerCompose.Engine.UnitTests.Yaml;

public class DockerComposeLabelServiceTest
{
    private DockerComposeLabelService _service;

    [SetUp]
    public void Setup()
    {
        _service = new DockerComposeLabelService();
    }

    [Test]
    public void ShouldBeAbleToAddCloudCrafterLabel()
    {
        var guid = Guid.NewGuid();
        _service.AddLabel(LabelFactory.GenerateApplicationLabel(guid));
        
        var labels = _service.ToLabelList();

        labels.Count.Should().Be(1);
        labels[0].Should().Be($"cloudcrafter.application={guid}");
    }
}
