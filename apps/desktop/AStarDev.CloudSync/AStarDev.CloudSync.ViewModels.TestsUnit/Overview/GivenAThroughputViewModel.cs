using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Overview;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Overview;

public class GivenAThroughputViewModel
{
    [Fact]
    public void when_created_with_samples_then_the_values_are_scaled_to_the_peak()
    {
        ThroughputSample[] samples = [ThroughputSampleFactory.Create(TestData.Now.AddMinutes(-2), 1_000), ThroughputSampleFactory.Create(TestData.Now.AddMinutes(-1), 4_000), ThroughputSampleFactory.Create(TestData.Now, 2_000)];

        var throughput = new ThroughputViewModel(samples, TestData.CreateLocalization());

        throughput.Values.ShouldBe([0.25, 1, 0.5]);
    }

    [Fact]
    public void when_created_with_samples_then_the_current_text_shows_the_latest_rate()
    {
        ThroughputSample[] samples = [ThroughputSampleFactory.Create(TestData.Now.AddMinutes(-1), 1_000), ThroughputSampleFactory.Create(TestData.Now, 14_889_779)];

        var throughput = new ThroughputViewModel(samples, TestData.CreateLocalization());

        throughput.CurrentText.ShouldBe("14.2 MB/s now");
    }

    [Fact]
    public void when_created_without_samples_then_there_are_no_values_and_the_text_says_no_data()
    {
        var throughput = new ThroughputViewModel([], TestData.CreateLocalization());

        throughput.Values.ShouldBeEmpty();
        throughput.CurrentText.ShouldBe("No data synced yet");
    }

    [Fact]
    public void when_every_sample_is_zero_then_the_values_are_zero()
    {
        ThroughputSample[] samples = [ThroughputSampleFactory.Create(TestData.Now.AddMinutes(-1), 0), ThroughputSampleFactory.Create(TestData.Now, 0)];

        var throughput = new ThroughputViewModel(samples, TestData.CreateLocalization());

        throughput.Values.ShouldBe([0d, 0d]);
    }
}
