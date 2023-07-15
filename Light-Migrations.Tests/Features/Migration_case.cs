using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.NUnit3;
using NUnit.Framework;

namespace Light_Migrations.Tests
{
    [TestFixture]
    [FeatureDescription(
        @"When I have valid json data version 1 
        I want to migrate it to version 2")]
    public partial class Migration_feature
    {
        [Scenario]
        public void Successful_migration()
        {
            Runner.RunScenario(
                given => Json_data_version_1(),
                when => The_user_call_json_convert_method(),
                then => The_data_should_be_migrated_to_version_2()
            );
        }
    }

    public partial class Migration_feature : FeatureFixture
    {
        
        private void Json_data_version_1()
        {
        }

        private void The_user_call_json_convert_method()
        {
        }

        private void The_data_should_be_migrated_to_version_2()
        {
            Assert.Fail();
        }
    }
}