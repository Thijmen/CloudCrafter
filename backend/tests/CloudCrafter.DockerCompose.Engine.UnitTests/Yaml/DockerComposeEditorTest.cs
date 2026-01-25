using System.Text;
using CloudCrafter.DockerCompose.Engine.Exceptions;
using CloudCrafter.DockerCompose.Engine.Models;
using CloudCrafter.DockerCompose.Engine.Yaml;
using FluentAssertions;

namespace CloudCrafter.DockerCompose.Engine.UnitTests.Yaml;

[TestFixture]
public class DockerComposeEditorTest
{
    [SetUp]
    public void Setup()
    {
        _editor = new DockerComposeEditor(yamlString);

        _editor.SetEnvironmentVariables(
            ".db-env",
            [new EnvironmentVariable { Key = "MYSQL_ROOT_PASSWORD", Value = "example" }]
        );
    }

    private readonly string yamlString =
        @"
version: '3.8'
services:
  web:
    image: php:7.4-apache
    ports:
      - ""80:80""
    volumes:
      - ./php-app:/var/www/html
    labels:
      - ""my.example.label=example""
      - some.value=true
    networks:
      - lampnet
  db:
    image: mysql:5.7
    environment:
      MYSQL_ROOT_PASSWORD: example
    volumes:
      - db_data:/var/lib/mysql
    env_file: .db-env
    networks:
      - lampnet

volumes:
  db_data:

#dummy
networks:
  lampnet:
";

    private DockerComposeEditor _editor;

    [Test]
    public void ShouldBeAbleToFetchDbService()
    {
        var db = _editor.Service("db");
        db.Should().NotBeNull();
    }

    [Test]
    public void ShouldNotBeAbleToFetchNonExistingService()
    {
        var ex = Assert.Throws<InvalidServiceException>(
            () => _editor.Service("nonExistingService")
        );

        ex.Message.Should().Be("Service nonExistingService is invalid");
    }

    [Test]
    public void ShouldNotBeAbleToAddExistingService()
    {
        var ex = Assert.Throws<ServiceAlreadyExistsException>(() => _editor.AddService("db"));

        ex.Message.Should().Be("Service db already exists");
    }

    [Test]
    public async Task ShouldBeAbleToGetBaseYaml()
    {
        var yaml = _editor.GetYaml();
        var isValid = await _editor.IsValid();

        isValid.IsValid.Should().BeTrue();

        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToAddALabel()
    {
        var service = _editor.Service("web");

        service.GetLabelValue("my.example.label").Should().Be("example");

        service.AddLabel("treafik.enable", "true");

        var yaml = _editor.GetYaml();

        var isValid = await _editor.IsValid();

        isValid.IsValid.Should().BeTrue();

        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToAddEnvironmentVariableToExistingService()
    {
        var service = _editor.Service("db");

        service.AddEnvironmentVariable("MYSQL_DATABASE", "example_db");
        service
            .GetEnvironmentVariables()
            .Should()
            .HaveCount(2)
            .And.Contain(x => x.Key == "MYSQL_ROOT_PASSWORD" && x.Value == "example")
            .And.Contain(x => x.Key == "MYSQL_DATABASE" && x.Value == "example_db");

        var yaml = _editor.GetYaml();

        var isValid = await _editor.IsValid();

        isValid.IsValid.Should().BeTrue();

        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToAddEnvironmentVariable()
    {
        var service = _editor.Service("web");

        service.AddEnvironmentVariable("APP_ENV", "dev");

        service
            .GetEnvironmentVariables()
            .Should()
            .HaveCount(1)
            .And.Contain(x => x.Key == "APP_ENV" && x.Value == "dev");

        var db = _editor.Service("db");
        db.GetEnvironmentVariables()
            .Should()
            .HaveCount(1)
            .And.Contain(x => x.Key == "MYSQL_ROOT_PASSWORD" && x.Value == "example");

        var yaml = _editor.GetYaml();

        var isValid = await _editor.IsValid();

        isValid.IsValid.Should().BeTrue();

        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToAddVolume()
    {
        var service = _editor.Service("web")!.AddVolume("./cache", "/var/www/html/cache");

        var yaml = _editor.GetYaml();

        var isValid = await _editor.IsValid();

        isValid.IsValid.Should().BeTrue();

        await Verify(yaml);
    }

    [Test]
    public void ShouldNotBeAbleToAddDuplicateVolume()
    {
        _editor.AddVolume("my-data");

        var ex = Assert.Throws<VolumeAlreadyExistsException>(() => _editor.AddVolume("my-data"));
    }

    [Test]
    public void ShouldNotBeAbleToFetchNonExistingVolume()
    {
        Assert.Throws<InvalidVolumeException>(() => _editor.Volume("my-non-existing-volume"));
    }

    [Test]
    public void ShouldBeAbleToFetchVolume()
    {
        var volume = _editor.Volume("db_data");
        volume.Should().NotBeNull();
        volume.VolumeName().Should().Be("db_data");
    }

    [Test]
    public async Task ShouldBeAbleToSetDriverOptsOnExistingVolume()
    {
        var volume = _editor.Volume("db_data");
        volume
            .SetDriver("local")
            .SetExternal(false)
            .SetDriverOpt("type", "nfs")
            .SetDriverOpt("o", "addr=127.0.0.1")
            .SetDriverOpt("device", ":/path/to/dir");

        var yaml = _editor.GetYaml();
        var isValid = await _editor.IsValid();

        isValid.IsValid.Should().BeTrue();
        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToAddService()
    {
        var service = _editor.AddService("newService");
        service.AddEnvironmentVariable("newKey", "newValue");
        service.AddVolume("/newVolume", "/newPath");
        service.SetImage("nginx", "latest");

        var yaml = _editor.GetYaml();

        var isValid = await _editor.IsValid();

        isValid.IsValid.Should().BeTrue();

        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToAddServiceAndEditExistingOne()
    {
        var service = _editor.AddService("newService");
        service.AddEnvironmentVariable("newKey", "newValue");
        service.AddVolume("/newVolume", "/newPath");
        service.SetImage("nginx", "latest");

        var dbService = _editor.Service("db");
        dbService.AddEnvironmentVariable("newKey", "newValue");

        var yaml = _editor.GetYaml();

        var isValid = await _editor.IsValid();

        isValid.IsValid.Should().BeTrue();

        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToChangeServiceImage()
    {
        var service = _editor.Service("web")!;
        service.SetImage("php", "8.0-apache");

        var yaml = _editor.GetYaml();

        var isValid = await _editor.IsValid();

        isValid.IsValid.Should().BeTrue();

        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToAddAVolumeButThisVolumeDoesNotExists()
    {
        var service = _editor.Service("web")!;
        service.AddVolume("my-volume", "/var/www/html/cache");

        var yaml = _editor.GetYaml();

        var isValid = await _editor.IsValid();

        isValid.IsValid.Should().BeFalse();

        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToAddAVolume()
    {
        var service = _editor.Service("web")!;
        service.AddVolume("my-volume", "/var/www/html/cache");

        var volume = _editor.AddVolume("my-volume");
        volume.SetDriver("local");

        var yaml = _editor.GetYaml();

        var isValid = await _editor.IsValid();

        isValid.IsValid.Should().BeTrue();

        await Verify(yaml);
    }

    [Test]
    public void ShouldBeAbleToListServices()
    {
        var services = _editor.Services();

        services.Should().HaveCount(2);
        services.Should().Contain("web");
        services.Should().Contain("db");
    }

    [Test]
    public void ShouldBeAbleToGetVolumes()
    {
        var webService = _editor.Service("web");
        var dbService = _editor.Service("db");

        var webVolumes = webService.GetVolumes();
        var dbVolumes = dbService.GetVolumes();

        webVolumes.Should().HaveCount(1);
        webVolumes.Should().Contain(("./php-app", "/var/www/html", false));

        dbVolumes.Should().HaveCount(1);
        dbVolumes.Should().Contain(("db_data", "/var/lib/mysql", true));
    }

    [Test]
    public async Task ShouldBeAbleToAddNetwork()
    {
        var network = _editor.AddNetwork("mynetwork");
        network.SetNetworkName("mynetwork");

        var database = _editor.Service("db");
        database.AddNetwork(network);

        var yaml = _editor.GetYaml();

        var isValid = await _editor.IsValid();

        isValid.IsValid.Should().BeTrue();

        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToAddPortsToServiceWhichHasExistingPorts()
    {
        var service = _editor.Service("web");

        service.AddExposedPort(443, 443);

        var isValid = await _editor.IsValid();

        isValid.IsValid.Should().BeTrue();

        var yaml = _editor.GetYaml();

        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToAddPortsToServiceWhichHasNoExistingPorts()
    {
        var service = _editor.Service("db");

        service.AddExposedPort(3306, 3306);
        service.AddExposedPort(3307, 3307);
        var isValid = await _editor.IsValid();

        isValid.IsValid.Should().BeTrue();

        var yaml = _editor.GetYaml();

        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToRetrieveBase64EncodedYaml()
    {
        var base64 = _editor.ToBase64();
        await Verify(base64);

        // Decode base64
        var decodedBase64 = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        decodedBase64.Should().Be(_editor.GetYaml());
    }
}
