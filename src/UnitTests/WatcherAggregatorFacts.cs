using System;
using Microsoft.Extensions.Logging;
using Microsoft.Reactive.Testing;
using Moq;
using Moq.Protected;
using Xunit;

namespace Contrib.KubeClient.CustomResources
{
    public class WatcherAggregatorFacts : IDisposable
    {
        private readonly Mock<ICustomResourceWatcher<CustomResource<string>>> _watcherMock = new Mock<ICustomResourceWatcher<CustomResource<string>>>();
        private readonly Mock<ICustomResourceWatcher<CustomResource<string>>> _watcherMock2 = new Mock<ICustomResourceWatcher<CustomResource<string>>>();
        private readonly TestScheduler _testScheduler = new TestScheduler();
        private readonly TimeSpan _debounceDuration = TimeSpan.FromMinutes(1);
        private readonly Mock<WatcherAggregatorBase> _aggregatorMock;
        private readonly WatcherAggregatorBase _aggregator;

        public WatcherAggregatorFacts()
        {
            _aggregatorMock = new Mock<WatcherAggregatorBase>(
                MockBehavior.Default,
                new[] {_watcherMock.Object, _watcherMock2.Object},
                _debounceDuration,
                new Logger<WatcherAggregatorBase>(new LoggerFactory()),
                _testScheduler)
            {
                CallBase = true
            };
            _aggregator = _aggregatorMock.Object;
        }

        public void Dispose() => _aggregator.Dispose();

        [Fact]
        public void DebouncesWatcherEvents()
        {
            _watcherMock.Raise(watcher => watcher.DataChanged += null, 0, EventArgs.Empty);
            _watcherMock2.Raise(watcher => watcher.DataChanged += null, 1, EventArgs.Empty);

            _aggregatorMock.Protected().Verify(WatcherAggregatorBase.OnChangedAsyncName, Times.Never());
            _testScheduler.AdvanceBy(_debounceDuration.Ticks);
            _aggregatorMock.Protected().Verify(WatcherAggregatorBase.OnChangedAsyncName, Times.Once());
        }

        [Fact]
        public void FailingOnChangedDoesNotStopStream()
        {
            _aggregatorMock.Protected().Setup(WatcherAggregatorBase.OnChangedAsyncName).Throws<Exception>();

            _watcherMock.Raise(watcher => watcher.DataChanged += null, 0, EventArgs.Empty);
            _testScheduler.AdvanceBy(_debounceDuration.Ticks);
            _watcherMock.Raise(watcher => watcher.DataChanged += null, 0, EventArgs.Empty);
            _testScheduler.AdvanceBy(_debounceDuration.Ticks);

            _aggregatorMock.Protected().Verify(WatcherAggregatorBase.OnChangedAsyncName, Times.Exactly(2));
        }
    }
}
