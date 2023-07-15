using System.Collections;
using Responsible;
using Responsible.Unity;
using UnityEngine.TestTools;
using static Responsible.Bdd.Keywords;

public class ExampleBddTest
{
    private TestInstructionExecutor executor = new UnityTestInstructionExecutor();

    [UnityTest]
    public IEnumerator Example()
    {
        yield return executor.RunScenario(
            Scenario("Example scenario"),
            Given("the setup is correct", Pending),
            When("the user does something", Pending),
            Then("the state should be updated correctly", Pending));
    }
}