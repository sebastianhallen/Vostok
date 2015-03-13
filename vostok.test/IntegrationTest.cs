namespace Vostok.Test
{
    using System;
    using System.Linq;
    using System.Threading;
    using NUnit.Framework;
    using Nancy.Hosting.Self;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Chrome;

    public class IntegrationTest
    {
        protected readonly Uri EndpointAddress = new Uri("http://localhost:1963");
        protected IWebDriver Driver;

        private const string chromeDriverDirectory = @"ChromeDriver\2.13";
        private readonly ManualResetEvent signal = new ManualResetEvent(false);
        private Thread nancyThread;

        [TestFixtureSetUp]
        public void BeforeAll()
        {
            this.nancyThread = new Thread(() =>
                {
                    var configuration = new HostConfiguration();
                    using (var nancy = new NancyHost(configuration, this.EndpointAddress))
                    {
                        nancy.Start();
                        this.signal.WaitOne();
                    }
                });
            this.nancyThread.Start();

            var innerDriver = new ChromeDriver(chromeDriverDirectory);
            this.Driver = new VostokWebDriver(innerDriver);
           // this.Driver = innerDriver;
        }

        [TestFixtureTearDown]
        public void AfterAll()
        {
            this.signal.Set();
            this.nancyThread.Join();

            this.Driver.Dispose();
        }
    }

    [TestFixture]
    public class StaleGuardedElementLookupTest
        : IntegrationTest
    {
        private IRetrier retrier;

        [SetUp]
        public void BeforeEach()
        {
            this.retrier = new Retrier(new RetryTimerFactory());
        }

        [Test]
        public void Elements_found_with_driver_should_not_throw_stale_element_exceptions_when_removed_and_recreated()
        {
            this.Driver.Navigate().GoToUrl(this.EndpointAddress + "Content/stale-element.html");
            
            var bottom = this.Driver.FindElement(By.Id("bottom"));

            this.VerifyContentChanged(bottom);
        }

        [Test]
        public void Elements_found_via_another_element_should_not_throw_stale_element_exceptions_when_removed_and_recreated()
        {
            this.Driver.Navigate().GoToUrl(this.EndpointAddress + "Content/stale-element.html");
            
            var bottom = this.Driver.FindElement(By.Id("top")).FindElement(By.Id("middle")).FindElement(By.Id("bottom"));

            this.VerifyContentChanged(bottom);
        }

        [Test]
        public void Result_from_drivers_FindElements_should_be_eager()
        {
            this.Driver.Navigate().GoToUrl(this.EndpointAddress + "Content/stale-element.html");

            var divs = this.Driver.FindElements(By.CssSelector(".mod10"));

            retrier.Do(() => { })
                .ForNoLongerThan(TimeSpan.FromSeconds(15))
                .Until(() => divs.Count() > 5);

            Assert.That(divs.Count(), Is.GreaterThanOrEqualTo(5));
        }

        [Test]
        public void Result_from_elements_FindElements_should_be_eager()
        {
            this.Driver.Navigate().GoToUrl(this.EndpointAddress + "Content/stale-element.html");

            var divs = this.Driver.FindElement(By.Id("mod10")).FindElements(By.CssSelector(".mod10"));

            retrier.Do(() => { })
                .ForNoLongerThan(TimeSpan.FromSeconds(15))
                .Until(() => divs.Count() > 5);

            Assert.That(divs.Count(), Is.GreaterThanOrEqualTo(5));
        }

        private void VerifyContentChanged(IWebElement element)
        {
            //wait for the bottom div to actually have some content
            retrier.DoUntil(() => { }, () => !string.IsNullOrWhiteSpace(element.Text));

            //and finally wait for the hierarchy to be recreated with new content
            var text = element.Text;
            var updateText = text;
            retrier
                .Do(() => updateText = element.Text)
                .ForNoLongerThan(TimeSpan.FromSeconds(5))
                .Until(() => !text.Equals(updateText));
            Assert.That(text, Is.Not.EqualTo(updateText));
        }
    }
}
