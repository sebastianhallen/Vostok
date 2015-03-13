namespace Vostok
{
    using System;
    using System.Diagnostics;

    public interface IRetrier
    {
        void DoUntil(Action perform, Func<bool> until, TimeSpan? timeout = null);
        void DontDoUntil(Action perform, Func<bool> whenFulfilled, TimeSpan? timeout = null);

        IDo Do(Action action);
    }

    public interface IDo
    {
        IDoFor ForNoLongerThan(TimeSpan fromSeconds);
    }

    public interface IDoFor
    {
        void Until(Func<bool> until);
    }

    public class RetryTimerFactory
    : IRetryTimerFactory
    {
        public IRetryTimer Create(TimeSpan timeoutLimit)
        {
            return new RetryTimer(timeoutLimit);
        }
    }

    public interface IRetryTimerFactory
    {
        IRetryTimer Create(TimeSpan timeoutLimit);
    }

    public class RetryTimer
    : IRetryTimer
    {
        private readonly TimeSpan timeoutLimit;
        private readonly Stopwatch stopwatch;

        public RetryTimer(TimeSpan timeoutLimit)
        {
            this.timeoutLimit = timeoutLimit;
            this.stopwatch = Stopwatch.StartNew();
        }

        public bool TimedOut()
        {
            return this.stopwatch.Elapsed > this.timeoutLimit;
        }
    }

    public interface IRetryTimer
    {
        bool TimedOut();
    }
}