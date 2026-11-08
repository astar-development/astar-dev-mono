using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts.TestsUnit;

public class GivenADriveIdFactory
{
    [Fact]
    public void when_created_with_surrounding_whitespace_then_the_drive_id_is_trimmed()
    {
        var driveIdValue = DriveIdFactory.Create(" 8A3F2C1D0E9B7A65 ").Match(driveId => driveId.Value, error => error.Message);

        driveIdValue.ShouldBe("8A3F2C1D0E9B7A65");
    }

    [Fact]
    public void when_created_with_mixed_case_then_the_drive_id_keeps_its_case()
    {
        var driveIdValue = DriveIdFactory.Create("b!AbCdEf").Match(driveId => driveId.Value, error => error.Message);

        driveIdValue.ShouldBe("b!AbCdEf");
    }

    [Fact]
    public void when_the_drive_id_is_converted_to_text_then_the_value_is_not_shown()
    {
        var driveIdText = DriveIdFactory.Create("8A3F2C1D0E9B7A65").Match(driveId => driveId.ToString(), error => error.Message);

        driveIdText.ShouldBe("DriveId(redacted)");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_created_with_a_missing_value_then_creation_fails(string? missingValue)
    {
        var succeeded = DriveIdFactory.Create(missingValue).Match(_ => true, _ => false);

        succeeded.ShouldBeFalse();
    }
}
