namespace Vostok
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Internal;

    public class ByjQuery
            : By
    {
        private readonly string _selector;
        
        public static By Selector(string selector)
        {
            if (IsParentSelector(selector))
            {
                return By.XPath("..");
            }

            if (IsSizzleSelector(selector))
            {
                return new ByjQuery(selector);
            }

            return By.CssSelector(selector);
        }

        private static bool IsParentSelector(string selector)
        {
            return "..".Equals(selector);
        }

        private static bool IsSizzleSelector(string selector)
        {
            //check jQuery's extended selector definition....
            return true;
        }

        private ByjQuery(string selector)
        {
            _selector = selector;
        }

        public override ReadOnlyCollection<IWebElement> FindElements(ISearchContext context)
        {
            var result = new EagerReadOnlyCollection<IWebElement>(() =>
            {
                var scriptExecutor = GetScriptExecutor(context);

                this.EnsurejQueryIsLoaded(scriptExecutor);

                object scriptResult = null;
                var escapedSelector = Newtonsoft.Json.JsonConvert.SerializeObject(_selector).Trim('"');
                if (context is IWebElement)
                {
                    var script = string.Format(@"return jQuery.makeArray(jQuery(""{0}"", arguments[0]))",
                        escapedSelector);
                    scriptResult = scriptExecutor.ExecuteScript(script, context);
                }
                else
                {
                    var script = string.Format(@"return jQuery.makeArray(jQuery(""{0}""))", escapedSelector);
                    scriptResult = scriptExecutor.ExecuteScript(script);
                }

                var scriptResultObjects = (IEnumerable<object>) scriptResult;

                return scriptResultObjects.Cast<IWebElement>().ToList();
            });
            return result;
        }


        private IJavaScriptExecutor GetScriptExecutor(ISearchContext context)
        {
            var scriptExecutor = context as IJavaScriptExecutor;

            if (scriptExecutor != null)
            {
                return scriptExecutor;
            }

            var driverWrapper = context as IWrapsDriver;
            if (driverWrapper != null)
            {
                return driverWrapper.WrappedDriver as IJavaScriptExecutor;
            }

            throw new Exception("Unable to convert javascript executor from context");
        }

        private void EnsurejQueryIsLoaded(IJavaScriptExecutor scriptExecutor)
        {
            var jqueryLoaded = WaitUntil(() =>
            {
                var script = @"return typeof(jQuery) === 'function';";
                var response = scriptExecutor.ExecuteScript(script);
                return (bool) response;

            }, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(100));

            if (!jqueryLoaded)
            {
                throw new Exception("jQuery was not loaded on the page");
            }
        }

        private bool WaitUntil(Func<bool> predicate, TimeSpan timeout, TimeSpan pollInterval)
        {
            var stopwatch = Stopwatch.StartNew();

            bool result = false;
            while (!(result = predicate()) && stopwatch.Elapsed < timeout)
            {
                System.Threading.Thread.Sleep(pollInterval);
            }

            return result;
        }
    }
}
