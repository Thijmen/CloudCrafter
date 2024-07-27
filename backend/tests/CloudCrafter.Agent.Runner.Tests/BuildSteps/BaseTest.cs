using CloudCrafter.Agent.Models.Recipe;
using CloudCrafter.Agent.Runner.Tests.Fakers;

namespace CloudCrafter.Agent.Runner.Tests.BuildSteps;

public abstract class BaseTest
{
    protected DeploymentRecipe GetTestRecipe()
    {
        return AgentFakers.DeploymentRecipeFaker().Generate();
    }
}
