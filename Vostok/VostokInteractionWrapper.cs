namespace Vostok
{
    using System;
    using OpenQA.Selenium;

    internal static class VostokInteractionWrapper
    {
        public static void Interact(ref IWebElement element, By selfSelector, Action elementLookup, Action<IWebElement> query, VostokSettings settings)
        {
            Func<IWebElement, object> queryWrapper = e =>
                {
                    query(e);
                    return null;
                };
            Interact(ref element, selfSelector, elementLookup, queryWrapper, settings);
        }

        internal static T Interact<T>(ref IWebElement element, By selfSelector, Action elementLookup, Func<IWebElement, T> query, VostokSettings settings)
        {
            try
            {
                elementLookup();
                return query(element);
            }
            catch (StaleElementReferenceException)
            {
                settings.DebugLogger(string.Format("Element '{0}' is stale.", selfSelector));
                
                //note that this clears the element that is now stale but keeps the internal reference so we can re-resolve it to the same variable higher up
                element = null;
                return Interact(ref element, selfSelector, elementLookup, query, settings);
            }
        }
    }
}