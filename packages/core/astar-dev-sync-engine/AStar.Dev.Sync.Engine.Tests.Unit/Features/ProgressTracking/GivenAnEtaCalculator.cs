using AStar.Dev.Sync.Engine.Features.ProgressTracking;

namespace AStar.Dev.Sync.Engine.Tests.Unit.Features.ProgressTracking;

public sealed class GivenAnEtaCalculator
{
    [Fact]
    public void when_no_files_have_completed_then_eta_is_zero()
    {
        var calculator = new EtaCalculator();

        var eta = calculator.CalculateEtaSeconds(10);

        eta.ShouldBe(0);
    }

    [Fact]
    public void when_files_total_is_zero_then_eta_is_zero()
    {
        var calculator = new EtaCalculator();
        calculator.RecordFileCompleted();

        var eta = calculator.CalculateEtaSeconds(0);

        eta.ShouldBe(0);
    }

    [Fact]
    public void when_all_files_have_completed_then_eta_is_zero_or_less()
    {
        var calculator = new EtaCalculator();
        calculator.RecordFileCompleted();
        calculator.RecordFileCompleted();

        var eta = calculator.CalculateEtaSeconds(2);

        eta.ShouldBe(0);
    }

    [Fact]
    public void when_a_delay_is_recorded_then_eta_increases()
    {
        var calculatorWithoutDelay = new EtaCalculator();
        calculatorWithoutDelay.RecordFileCompleted();
        var etaWithoutDelay = calculatorWithoutDelay.CalculateEtaSeconds(10);

        var calculatorWithDelay = new EtaCalculator();
        calculatorWithDelay.RecordFileCompleted();
        calculatorWithDelay.RecordDelay(TimeSpan.FromSeconds(30));
        var etaWithDelay = calculatorWithDelay.CalculateEtaSeconds(10);

        etaWithDelay.ShouldBeGreaterThanOrEqualTo(etaWithoutDelay);
    }

    [Fact]
    public void when_record_file_completed_is_called_multiple_times_then_eta_decreases_over_time()
    {
        var calculator = new EtaCalculator();
        calculator.RecordFileCompleted();
        var etaAfterOne = calculator.CalculateEtaSeconds(10);

        calculator.RecordFileCompleted();
        var etaAfterTwo = calculator.CalculateEtaSeconds(10);

        etaAfterTwo.ShouldBeLessThanOrEqualTo(etaAfterOne);
    }

    [Fact]
    public void when_delay_is_recorded_then_additional_ms_is_included_in_eta_calculation()
    {
        var calculator = new EtaCalculator();
        calculator.RecordFileCompleted();
        calculator.RecordDelay(TimeSpan.FromSeconds(60));

        var eta = calculator.CalculateEtaSeconds(2);

        eta.ShouldBeGreaterThan(0);
    }
}
