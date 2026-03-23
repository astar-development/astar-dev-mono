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
        var roleName             = "TestRole";
        var instrumentationKey   = "TestKey";
        var telemetryInitializer = new CloudRoleNameTelemetryInitializer(roleName, instrumentationKey);
        var mockTelemetry        = Substitute.For<ITelemetry>();
        var telemetryContext     = new TelemetryContext();
        mockTelemetry.Context.Returns(telemetryContext);

        telemetryInitializer.Initialize(mockTelemetry);

        telemetryContext.Cloud.RoleName.ShouldBe(roleName);
        telemetryContext.InstrumentationKey.ShouldBe(instrumentationKey);
    }

    [Fact]
    public void Initialize_ShouldNotThrowForNullTelemetry()
    {
        var roleName             = "TestRole";
        var instrumentationKey   = "TestKey";
        var telemetryInitializer = new CloudRoleNameTelemetryInitializer(roleName, instrumentationKey);

        var exception = Record.Exception(() => telemetryInitializer.Initialize(null!));
        exception.ShouldBeNull();
    }

    [Fact]
    public void Initialize_ShouldHandleDifferentRoleNameAndInstrumentationKey()
    {
        var roleName             = "DifferentRole";
        var instrumentationKey   = "DifferentKey";
        var telemetryInitializer = new CloudRoleNameTelemetryInitializer(roleName, instrumentationKey);
        var mockTelemetry        = Substitute.For<ITelemetry>();
        var telemetryContext     = new TelemetryContext();
        mockTelemetry.Context.Returns(telemetryContext);

        telemetryInitializer.Initialize(mockTelemetry);

        telemetryContext.Cloud.RoleName.ShouldBe(roleName);
        telemetryContext.InstrumentationKey.ShouldBe(instrumentationKey);
    }

    [Fact]
    public void Initialize_ShouldNotOverrideExistingRoleNameOrInstrumentationKey()
    {
        var roleName             = "NewRole";
        var instrumentationKey   = "NewKey";
        var telemetryInitializer = new CloudRoleNameTelemetryInitializer(roleName, instrumentationKey);
        var mockTelemetry        = Substitute.For<ITelemetry>();
        var telemetryContext     = new TelemetryContext { Cloud = { RoleName = "ExistingRole" }, InstrumentationKey = "ExistingKey" };
        mockTelemetry.Context.Returns(telemetryContext);

        telemetryInitializer.Initialize(mockTelemetry);

        telemetryContext.Cloud.RoleName.ShouldBe("ExistingRole");
        telemetryContext.InstrumentationKey.ShouldBe("ExistingKey");
    }

    [Fact]
    public void Initialize_WithEmptyRoleNameAndInstrumentationKey_ShouldSetEmptyValues()
    {
        var roleName             = string.Empty;
        var instrumentationKey   = string.Empty;
        var telemetryInitializer = new CloudRoleNameTelemetryInitializer(roleName, instrumentationKey);
        var mockTelemetry        = Substitute.For<ITelemetry>();
        var telemetryContext     = new TelemetryContext();
        mockTelemetry.Context.Returns(telemetryContext);

        telemetryInitializer.Initialize(mockTelemetry);

        telemetryContext.Cloud.RoleName.ShouldBeNull();
        telemetryContext.InstrumentationKey.ShouldBe(string.Empty);
    }
}