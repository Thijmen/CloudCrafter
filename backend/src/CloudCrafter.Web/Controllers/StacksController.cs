using CloudCrafter.Core.Commands.Stacks;
using CloudCrafter.Core.Commands.Stacks.EnvironmentVariables;
using CloudCrafter.Core.Commands.Stacks.Providers;
using CloudCrafter.Core.Commands.Stacks.Service;
using CloudCrafter.Core.Commands.Stacks.Volumes;
using CloudCrafter.Domain.Domain.Deployment;
using CloudCrafter.Domain.Domain.Stack;
using CloudCrafter.Domain.Domain.Stacks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CloudCrafter.Web.Controllers;

public class StacksController : CloudCrafterController
{
    [HttpPost]
    public async Task<ActionResult<StackCreatedDto>> PostCreateStack(
        [FromBody] CreateStackCommand command,
        ISender sender
    )
    {
        return await sender.Send(command);
    }

    [HttpPost("provider")]
    public async Task<ActionResult<StackCreatedDto>> PostCreateStackFromSourceProvider(
        [FromBody] CreateStackFromSourceProviderCommand command,
        ISender sender
    )
    {
        return await sender.Send(command);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StackDetailDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StackDetailDto>> GetStackDetail(
        [FromRoute] Guid id,
        ISender sender
    )
    {
        var details = await sender.Send(new GetStackDetailQuery { StackId = id });
        return details is not null ? Ok(details) : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteStack([FromRoute] Guid id, ISender sender)
    {
        await sender.Send(new DeleteStackCommand { StackId = id });
        return Ok();
    }

    [HttpPost("{id}/deploy")]
    public async Task<ActionResult<DeploymentCreatedDetailsDto>> PostDispatchStackDeployment(
        [FromRoute] Guid id,
        ISender sender
    )
    {
        return await sender.Send(new DispatchStackDeployment.Command(id));
    }

    [HttpGet("{id}/deployments")]
    public async Task<ActionResult<List<SimpleDeploymentDto>>> GetDeploymentsForStack(
        [FromRoute] Guid id,
        ISender sender
    )
    {
        return await sender.Send(new GetStackSimpleDeployments.Query(id));
    }

    [HttpGet("deployments/{deploymentId}/logs")]
    public async Task<ActionResult<List<DeploymentLogDto>>> GetDeploymentLogs(
        [FromRoute] Guid deploymentId,
        ISender sender
    )
    {
        return await sender.Send(new GetStackDeploymentLogs.Query(deploymentId));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StackDetailDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StackDetailDto>> UpdateStack(
        [FromRoute] Guid id,
        [FromBody] UpdateStackCommand command,
        ISender sender
    )
    {
        command = command with { StackId = id };
        var result = await sender.Send(command);
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpPatch("{stackId}/services/{stackServiceId}")]
    public async Task<IActionResult> UpdateStackService(
        [FromRoute] Guid stackId,
        [FromRoute] Guid stackServiceId,
        [FromBody] UpdateStackServiceCommand command,
        ISender sender
    )
    {
        command = command with { StackId = stackId, StackServiceId = stackServiceId };
        var result = await sender.Send(command);

        return result is not null ? Ok(result) : NotFound();
    }

    // Region: Environment Variables
    [HttpGet("{stackId}/environment-variables")]
    public async Task<List<StackEnvironmentVariableDto>> GetEnvironmentVariables(
        ISender sender,
        [FromRoute] Guid stackId,
        [FromQuery] bool includeSecrets = false
    )
    {
        var result = await sender.Send(
            new GetStackEnvironmentVariablesQuery(stackId, includeSecrets)
        );
        return result;
    }

    [HttpPost("{stackId}/environment-variables")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IResult> PostCreateEnvironmentVariable(
        ISender sender,
        [FromRoute] Guid stackId,
        [FromBody] CreateStackEnvironmentVariableCommand command
    )
    {
        command.StackId = stackId;
        await sender.Send(command);
        return Results.Created();
    }

    [HttpPut("{stackId}/environment-variables/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> PutUpdateEnvironmentVariable(
        ISender sender,
        [FromRoute] Guid stackId,
        [FromRoute] Guid id,
        [FromBody] UpdateStackEnvironmentVariableCommand command
    )
    {
        command.StackId = stackId;
        command.Id = id;
        await sender.Send(command);
        return Results.Ok();
    }

    [HttpDelete("{stackId}/environment-variables/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IResult> DeleteEnvironmentVariable(
        ISender sender,
        [FromRoute] Guid stackId,
        [FromRoute] Guid id
    )
    {
        await sender.Send(new DeleteStackEnvironmentVariableCommand(stackId, id));
        return Results.Ok();
    }

    [HttpGet("{stackId}/environment-variable-groups")]
    public async Task<List<StackEnvironmentVariableGroupDto>> GetEnvironmentVariableGroups(
        ISender sender,
        [FromRoute] Guid stackId
    )
    {
        var result = await sender.Send(new GetStackEnvironmentVariableGroupsQuery(stackId));
        return result;
    }

    [HttpPost("{stackId}/environment-variable-groups")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IResult> PostCreateEnvironmentVariableGroup(
        ISender sender,
        [FromRoute] Guid stackId,
        [FromBody] CreateStackEnvironmentVariableGroupCommand command
    )
    {
        command.StackId = stackId;
        await sender.Send(command);
        return Results.Created();
    }

    [HttpPut("{stackId}/environment-variable-groups/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> PutUpdateEnvironmentVariableGroup(
        ISender sender,
        [FromRoute] Guid stackId,
        [FromRoute] Guid id,
        [FromBody] UpdateStackEnvironmentVariableGroupCommand command
    )
    {
        command.StackId = stackId;
        command.Id = id;
        await sender.Send(command);
        return Results.Ok();
    }

    [HttpDelete("{stackId}/environment-variable-groups/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IResult> DeleteEnvironmentVariableGroup(
        ISender sender,
        [FromRoute] Guid stackId,
        [FromRoute] Guid id
    )
    {
        await sender.Send(new DeleteStackEnvironmentVariableGroupCommand(stackId, id));
        return Results.Ok();
    }

    // Region: StackService Volumes
    [HttpGet("{stackId}/services/{stackServiceId}/volumes")]
    public async Task<List<StackServiceVolumeDto>> GetStackServiceVolumes(
        ISender sender,
        [FromRoute] Guid stackId,
        [FromRoute] Guid stackServiceId
    )
    {
        var result = await sender.Send(new GetStackServiceVolumesQuery(stackId, stackServiceId));
        return result;
    }

    [HttpPost("{stackId}/services/{stackServiceId}/volumes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IResult> PostCreateStackServiceVolume(
        ISender sender,
        [FromRoute] Guid stackId,
        [FromRoute] Guid stackServiceId,
        [FromBody] CreateStackServiceVolumeCommand command
    )
    {
        command.StackId = stackId;
        command.StackServiceId = stackServiceId;
        await sender.Send(command);
        return Results.Created();
    }

    [HttpPut("{stackId}/services/{stackServiceId}/volumes/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> PutUpdateStackServiceVolume(
        ISender sender,
        [FromRoute] Guid stackId,
        [FromRoute] Guid stackServiceId,
        [FromRoute] Guid id,
        [FromBody] UpdateStackServiceVolumeCommand command
    )
    {
        command.StackId = stackId;
        command.StackServiceId = stackServiceId;
        command.Id = id;
        await sender.Send(command);
        return Results.Ok();
    }

    [HttpDelete("{stackId}/services/{stackServiceId}/volumes/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IResult> DeleteStackServiceVolume(
        ISender sender,
        [FromRoute] Guid stackId,
        [FromRoute] Guid stackServiceId,
        [FromRoute] Guid id
    )
    {
        await sender.Send(new DeleteStackServiceVolumeCommand(stackId, stackServiceId, id));
        return Results.Ok();
    }
}
