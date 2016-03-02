using System.Text;

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
        protected VostokSettings Settings;

        private const string chromeDriverDirectory = @"ChromeDriver\2.21";
        private readonly ManualResetEvent signal = new ManualResetEvent(false);
        private Thread nancyThread;

        [TestFixtureSetUp]
        public void BeforeAll()
        {
            //disable shadow copying in your test runner otherwise you will get 404s when trying to get the static content
            this.nancyThread = new Thread(() =>
            {
                    using (var nancy = new NancyHost(this.EndpointAddress))
                    {
                        nancy.Start();
                        this.signal.WaitOne();
                    }
                });
            this.nancyThread.Start();

            this.Settings = new VostokSettings();
            var innerDriver = new ChromeDriver(chromeDriverDirectory);
            this.Driver = new VostokWebDriver(innerDriver, this.Settings);
            //this.Driver = innerDriver;
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
        private StringBuilder log;

        [SetUp]
        public void BeforeEach()
        {
            this.retrier = new Retrier(new RetryTimerFactory());
            this.log = new StringBuilder();
            this.Settings.DebugLogger = message => this.log.AppendLine(message);
        }

        [TearDown]
        public void AfterEach()
        {
            Console.WriteLine(this.log);
        }

        [Test]
        public void Should_be_able_to_reresolve_an_element_after_refreshing_a_page()
        {
            //find original element
            this.Driver.Navigate().GoToUrl(this.EndpointAddress + "Content/changing-element.html");
            var element = this.Driver.FindElement(By.CssSelector("[id='foo'] p"));

            //invalidate it...
            var removeLink = this.Driver.FindElement(By.TagName("a"));
            removeLink.Click();
            this.retrier.DoUntil(() => { }, () =>
            {
                var text = this.Driver.FindElement(By.CssSelector("[id='foo'] p")).Text;
                return "re-created".Equals(text);
            });
            // refresh page to restore state
            this.Driver.Navigate().Refresh();

            Assert.That(element.Text, Is.EqualTo("to be removed"));
        }

        [Test]
        public void Should_reresolve_vostok_element_arguments_before_running_script()
        {
            this.Driver.Navigate().GoToUrl(this.EndpointAddress + "Content/changing-element.html");

            var recreatedElement = this.Driver.FindElement(By.CssSelector("[id='foo'] p"));
            var removeLink = this.Driver.FindElement(By.TagName("a"));
            removeLink.Click();
            this.retrier.DoUntil(() => { }, () =>
            {
                var text = this.Driver.FindElement(By.CssSelector("[id='foo'] p")).Text;
                return "re-created".Equals(text);
            });
        

            (this.Driver as IJavaScriptExecutor).ExecuteScript("console.log('this should not be possible')", recreatedElement);
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

    [TestFixture]
    public class OriginReResolveGuard : IntegrationTest
    {
        [TestCase(PageOriginStrictness.AllowNonMatchingAnchorHashes)]
        [TestCase(PageOriginStrictness.AllowNonMatchingQueryStrings)]
        [TestCase(PageOriginStrictness.ExactMatch)]
        public void Should_not_allow_re_resolve_of_elements_that_originiate_from_a_page_with_another_url_when_checking_origin(PageOriginStrictness level)
        {
            this.Settings.SamePageOriginStrictness = level;

            this.Open("origin-0.html");
            var div = this.Driver.FindElement(By.Id("foo"));

            this.Open("origin-1.html");

            Assert.Throws<InvalidElementStateException>(() => div.Click());
        }

        [Test]
        public void Should_allow_re_resolve_of_elements_that_originiate_from_a_page_with_another_url_when_not_checking_origin()
        {
            this.Settings.SamePageOriginStrictness = PageOriginStrictness.DontCheckOrigin;

            this.Open("origin-0.html");
            var div = this.Driver.FindElement(By.Id("foo"));

            this.Open("origin-1.html");

            Assert.That(div.Text, Is.EqualTo("Baz"));
        }

        [TestCase(PageOriginStrictness.AllowNonMatchingAnchorHashes)]
        [TestCase(PageOriginStrictness.AllowNonMatchingQueryStrings)]
        public void Should_allow_re_resolve_of_elements_that_originiate_from_the_same_url_with_different_hash(PageOriginStrictness level)
        {
            this.Settings.SamePageOriginStrictness = level;
            this.Open("origin-0.html");
            var div = this.Driver.FindElement(By.Id("foo"));

            this.Open("origin-1.html");
            this.Open("origin-0.html#same-same-but-different");

            Assert.That(div.Text, Is.EqualTo("Bar"));
        }

        [Test]
        public void Should_not_allow_re_resolve_elements_that_originiate_from_the_same_url_with_different_query_with_AllowNonMatchingAnchorHashes()
        {
            this.Settings.SamePageOriginStrictness = PageOriginStrictness.AllowNonMatchingAnchorHashes;
            this.Open("origin-0.html?foo=bar");
            var div = this.Driver.FindElement(By.Id("foo"));

            this.Open("origin-0.html?bar=baz");

            Assert.Throws<InvalidElementStateException>(() => div.Click());
        }

        [Test]
        public void Should_allow_re_resolve_of_elements_that_originiate_from_the_same_url_with_different_query_with_AllowNonMatchingQueryStrings()
        {
            this.Settings.SamePageOriginStrictness = PageOriginStrictness.AllowNonMatchingQueryStrings;

            this.Open("origin-0.html?foo=bar");
            var div = this.Driver.FindElement(By.Id("foo"));

            this.Open("origin-0.html?bar=baz");
            Assert.That(div.Text, Is.EqualTo("Bar"));
        }

        [Test]
        public void Should_allow_re_resolve_of_elements_when_urls_match_exactly_with_ExactMatch()
        {
            this.Settings.SamePageOriginStrictness = PageOriginStrictness.ExactMatch;
            this.Open("origin-0.html?foo=bar#baz");
            

            var div = this.Driver.FindElement(By.Id("foo"));
            this.Open("origin-1.html");

            this.Open("origin-0.html?foo=bar#baz");
            Assert.That(div.Text, Is.EqualTo("Bar"));
        }

        [TestCase("origin-0.html?foo=bar#baz", "origin-0.html?foo=bar#baz1")]
        [TestCase("origin-0.html?foo=bar#baz", "origin-0.html?foo=foo#baz")]
        [TestCase("origin-0.html?foo=bar#baz", "origin-0.html")]
        [TestCase("origin-0.html", "origin-0.html?foo=bar#baz")]
        public void Should_not_allow_re_resolve_of_elements_when_urls_dont_match_exactly_with_ExactMatch(string first, string second)
        {
            this.Settings.SamePageOriginStrictness = PageOriginStrictness.ExactMatch;
            this.Open(first);

            var div = this.Driver.FindElement(By.Id("foo"));
            this.Open("origin-1.html");

            this.Open(second);
            Assert.Throws<InvalidElementStateException>(() => div.Click());
        }

        private void Open(string route)
        {
            this.Driver.Navigate().GoToUrl(this.EndpointAddress + "Content/" + route);
        }
    }
}

