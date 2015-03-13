namespace Vostok
{
    using System;

    public class Retrier
        : IRetrier, IDo, IDoFor
    {
        private readonly IRetryTimerFactory retryTimerFactory;

        private TimeSpan timeoutLimit;
        private Func<bool> until;
        private Action action;

        public Retrier(IRetryTimerFactory retryTimerFactory)
        {
            this.retryTimerFactory = retryTimerFactory;
            this.timeoutLimit = TimeSpan.FromSeconds(5);
            this.until = () => true;
            this.action = () => { throw new InvalidOperationException("No state changing action defined."); };
        }

        void IRetrier.DoUntil(Action action, Func<bool> condition, TimeSpan? timeout)
        {
            this.timeoutLimit = timeout.HasValue ? timeout.Value : this.timeoutLimit;

            var timer = this.retryTimerFactory.Create(this.timeoutLimit);

            while (!condition() && !timer.TimedOut())
            {
                action();
            }
        }

        void IRetrier.DontDoUntil(Action perform, Func<bool> whenFulfilled, TimeSpan? timeout)
        {
            this.timeoutLimit = timeout.HasValue ? timeout.Value : this.timeoutLimit;

            var timer = this.retryTimerFactory.Create(this.timeoutLimit);
            bool fulfilled;
            while (!(fulfilled = whenFulfilled()) && !timer.TimedOut())
            {

            }

            if (fulfilled)
            {
                perform();
            }

        }

        void IDoFor.Until(Func<bool> until)
        {
            this.until = until;
            ((IRetrier)this).DoUntil(this.action, this.until, this.timeoutLimit);
        }

        IDoFor IDo.ForNoLongerThan(TimeSpan timeoutLimit)
        {
            this.timeoutLimit = timeoutLimit;
            return this;
        }

        IDo IRetrier.Do(Action action)
        {
            this.action = action;
            return this;
        }
    }
}
