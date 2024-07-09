﻿using CloudCrafter.DockerCompose.Engine.Exceptions;
using CloudCrafter.DockerCompose.Engine.Yaml;
using FluentAssertions;

namespace CloudCrafter.DockerCompose.Engine.UnitTests.Yaml;
[TestFixture]

public class DockerComposeEditorTest
{
    private string yamlString = @"
version: '3.8'
services:
  web:
    image: php:7.4-apache
    ports:
      - ""80:80""
    volumes:
      - ./php-app:/var/www/html
    networks:
      - lampnet
  db:
    image: mysql:5.7
    environment:
      MYSQL_ROOT_PASSWORD: example
    volumes:
      - db_data:/var/lib/mysql
    networks:
      - lampnet

volumes:
  db_data:

#dummy
networks:
  lampnet:
";

    private DockerComposeEditor _editor;

    [SetUp]
    public void Setup()
    {
        _editor = new DockerComposeEditor(yamlString);
    }

    [Test]
    public void ShouldBeAbleToFetchDbService()
    {
        var db = _editor.Service("db");
        db.Should().NotBeNull();
    }
    
    [Test]
    public void ShouldNotBeAbleToFetchNonExistingService()
    {
        InvalidServiceException ex = Assert.Throws<InvalidServiceException>(() => _editor.Service("nonExistingService"));
       
        ex.Message.Should().Be("Service nonExistingService is invalid");
    }

    [Test]
    public void ShouldNotBeAbleToAddExistingService()
    {
        ServiceAlreadyExistsException ex = Assert.Throws<ServiceAlreadyExistsException>(() => _editor.AddService("db"));
        
        ex.Message.Should().Be("Service db already exists");
    }

    [Test]
    public async Task ShouldBeAbleToGetBaseYaml()
    {
        var yaml = _editor.GetYaml();
        var isValid = await _editor.IsValid();
       
        isValid.Should().BeTrue();
        
        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToAddALabel()
    {
        var service = _editor.Service("web")!
            .AddLabel("treafik.enable", "true");

        var yaml = _editor.GetYaml();

        var isValid = await _editor.IsValid();
       
        isValid.Should().BeTrue();
        
        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToAddEnvironmentVariable()
    {
        var service = _editor.Service("web")!
            .AddEnvironmentVariable("APP_ENV", "dev");
        
        var yaml = _editor.GetYaml();
        
        var isValid = await _editor.IsValid();
       
        isValid.Should().BeTrue();
        
        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToAddVolume()
    {
        var service = _editor.Service("web")!
            .AddVolume("./cache", "/var/www/html/cache");
        
        var yaml = _editor.GetYaml();
        
        var isValid = await _editor.IsValid();
       
        isValid.Should().BeTrue();
        
        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToAddService()
    {
        var service = _editor.AddService("newService");
        service.AddEnvironmentVariable("newKey", "newValue");
        service.AddVolume("newVolume", "newPath");
        
        var yaml = _editor.GetYaml();

        var isValid = await _editor.IsValid();
       
        isValid.Should().BeTrue();
        
        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToAddServiceAndEditExistingOne()
    {
        var service = _editor.AddService("newService");
        service.AddEnvironmentVariable("newKey", "newValue");
        service.AddVolume("newVolume", "newPath");

        var dbService = _editor.Service("db");
        dbService.AddEnvironmentVariable("newKey", "newValue");
        
        var yaml = _editor.GetYaml();

        var isValid = await _editor.IsValid();
       
        isValid.Should().BeTrue();
        
        await Verify(yaml);
    }

    [Test]
    public async Task ShouldBeAbleToChangeServiceImage()
    {
        var service = _editor.Service("web")!;
        service.SetImage("php", "8.0-apache");
        
        var yaml = _editor.GetYaml();

        var isValid = await _editor.IsValid();
       
        isValid.Should().BeTrue();
        
        await Verify(yaml);
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
       
        isValid.Should().BeTrue();
        
        await Verify(yaml);
        
    }

}
