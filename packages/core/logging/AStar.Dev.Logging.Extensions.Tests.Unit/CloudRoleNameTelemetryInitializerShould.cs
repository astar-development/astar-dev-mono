using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using NSubstitute;

namespace AStar.Dev.Logging.Extensions.Tests.Unit;

[TestSubject(typeof(CloudRoleNameTelemetryInitializer))]
public class CloudRoleNameTelemetryInitializerShould
{
    [Fact]
    public void Initialize_ShouldSetRoleNameAndInstrumentationKey()
    {
        string roleName             = "TestRole";
        string instrumentationKey   = "TestKey";
        var telemetryInitializer = new CloudRoleNameTelemetryInitializer(roleName, instrumentationKey);
        ITelemetry? mockTelemetry        = Substitute.For<ITelemetry>();
        var telemetryContext     = new TelemetryContext();
        mockTelemetry.Context.Returns(telemetryContext);

        telemetryInitializer.Initialize(mockTelemetry);

        telemetryContext.Cloud.RoleName.ShouldBe(roleName);
        telemetryContext.InstrumentationKey.ShouldBe(instrumentationKey);
    }

    [Fact]
    public void Initialize_ShouldNotThrowForNullTelemetry()
    {
        string roleName             = "TestRole";
        string instrumentationKey   = "TestKey";
        var telemetryInitializer = new CloudRoleNameTelemetryInitializer(roleName, instrumentationKey);

        Exception? exception = Record.Exception(() => telemetryInitializer.Initialize(null!));
        exception.ShouldBeNull();
    }

    [Fact]
    public void Initialize_ShouldHandleDifferentRoleNameAndInstrumentationKey()
    {
        string roleName             = "DifferentRole";
        string instrumentationKey   = "DifferentKey";
        var telemetryInitializer = new CloudRoleNameTelemetryInitializer(roleName, instrumentationKey);
        ITelemetry? mockTelemetry        = Substitute.For<ITelemetry>();
        var telemetryContext     = new TelemetryContext();
        mockTelemetry.Context.Returns(telemetryContext);

        telemetryInitializer.Initialize(mockTelemetry);

        telemetryContext.Cloud.RoleName.ShouldBe(roleName);
        telemetryContext.InstrumentationKey.ShouldBe(instrumentationKey);
    }

    [Fact]
    public void Initialize_ShouldNotOverrideExistingRoleNameOrInstrumentationKey()
    {
        string roleName             = "NewRole";
        string instrumentationKey   = "NewKey";
        var telemetryInitializer = new CloudRoleNameTelemetryInitializer(roleName, instrumentationKey);
        ITelemetry? mockTelemetry        = Substitute.For<ITelemetry>();
        var telemetryContext     = new TelemetryContext { Cloud = { RoleName = "ExistingRole" }, InstrumentationKey = "ExistingKey" };
        mockTelemetry.Context.Returns(telemetryContext);

        telemetryInitializer.Initialize(mockTelemetry);

        telemetryContext.Cloud.RoleName.ShouldBe("ExistingRole");
        telemetryContext.InstrumentationKey.ShouldBe("ExistingKey");
    }

    [Fact]
    public void Initialize_WithEmptyRoleNameAndInstrumentationKey_ShouldSetEmptyValues()
    {
        string roleName             = string.Empty;
        string instrumentationKey   = string.Empty;
        var telemetryInitializer = new CloudRoleNameTelemetryInitializer(roleName, instrumentationKey);
        ITelemetry? mockTelemetry        = Substitute.For<ITelemetry>();
        var telemetryContext     = new TelemetryContext();
        mockTelemetry.Context.Returns(telemetryContext);

        telemetryInitializer.Initialize(mockTelemetry);

        telemetryContext.Cloud.RoleName.ShouldBeNull();
        telemetryContext.InstrumentationKey.ShouldBe(string.Empty);
    }
}
