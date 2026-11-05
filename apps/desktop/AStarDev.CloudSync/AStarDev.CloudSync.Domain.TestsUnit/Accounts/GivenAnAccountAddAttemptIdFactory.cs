using AStarDev.CloudSync.Domain.Accounts;

namespace AStarDev.CloudSync.Domain.TestsUnit.Accounts;

public class GivenAnAccountAddAttemptIdFactory
{
    [Fact]
    public void when_an_attempt_id_is_created_then_it_keeps_the_guid()
    {
        var value = Guid.Parse("3c2b1a09-8f7e-4d6c-b5a4-9382716f5e4d");

        AccountAddAttemptIdFactory.Create(value).Value.ShouldBe(value);
    }
}
