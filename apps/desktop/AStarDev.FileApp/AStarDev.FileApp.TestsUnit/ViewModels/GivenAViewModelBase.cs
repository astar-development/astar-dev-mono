using AStar.Dev.File.App.ViewModels;

namespace AStar.Dev.File.App.TestsUnit.ViewModels;

public class GivenAViewModelBase
{
    private sealed class TestViewModel : ViewModelBase
    {
        public CancellationTokenSource ExposedCancellationTokenSource => CancellationTokenSource;
    }

    [Fact]
    public void when_disposed_then_cancellation_token_source_is_disposed()
    {
        var sut = new TestViewModel();
        var cancellationTokenSource = sut.ExposedCancellationTokenSource;

        sut.Dispose();

        Should.Throw<ObjectDisposedException>(() => cancellationTokenSource.Token);
    }

    [Fact]
    public void when_disposed_multiple_times_then_does_not_throw()
    {
        var sut = new TestViewModel();

        Should.NotThrow(() =>
        {
            sut.Dispose();
            sut.Dispose();
        });
    }
}
