using NUnit.Framework;
using Responsible;
using static Responsible.Bdd.Keywords;

namespace Light_Migrations.Tests.EditorMode
{
    public sealed class ExampleBddTest
    {
        private readonly InstructionExecutor _executor = new();

        [Test]
        public async void Example()
        {
            
            await _executor.RunScenario(
                Scenario("Example scenario"),
                Given("the setup is correct", Responsibly.DoAndReturn("setup", () => new object())),
                When("the user does something", Responsibly.Do("action", () => {
                    var c = 0;
                    while (c < 1000000000)
                        c++;
                })),
                Then("the state should be updated correctly", Responsibly.Do("assertion", () => { })));
            
        }
    
        [TearDown]
        public void TearDown()
        {
            _executor.Dispose();
        }
    
        [SetUp]
        public void SetUp()
        {
        }
    }
}