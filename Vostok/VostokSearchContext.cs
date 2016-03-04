namespace Vostok
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using OpenQA.Selenium;

    public class VostokSearchContext
        : ISearchContext
    {
        private readonly VostokSettings settings;
        private readonly By selfSelector;
        private readonly Func<IWebElement> selfLookup;
        private ISearchContext context;

        public VostokSearchContext(VostokSettings settings, By selfSelector, ISearchContext context, Func<IWebElement> selfLookup)
        {
            this.settings = settings;
            this.selfSelector = selfSelector;
            this.context = context;
            this.selfLookup = selfLookup;
        }

        public IWebElement FindElement(By @by)
        {
            var element = this.context as IWebElement;
            this.settings.DebugLogger(element != null ? $"element->element: {@by}" : $"driver->element: {@by}");

            try
            {
                var child = this.selfLookup == null
                    ? this.context.FindElement(@by)
                    : this.selfLookup().FindElement(@by);
                
                return new VostokWebElement(this.settings, child, @by, this.context, ctx => ctx.FindElement(@by));
            }
            catch (StaleElementReferenceException)
            {
                this.settings.DebugLogger($"Element '{this.selfSelector}' is stale. - child element lookup failed");
                var vostokElement = this.context as VostokWebElement;
                if (vostokElement != null)
                {
                    vostokElement.Nuke();
                }

                return this.FindElement(@by);
            }
        }
        
        public ReadOnlyCollection<IWebElement> FindElements(By @by)
        {
            var element = this.context as IWebElement;
            
            return new EagerReadOnlyCollection<IWebElement>(() =>
            {
                if (element != null)
                {
                    this.settings.DebugLogger($"element->elements: {@by}");
                    try
                    {
                        element = this.selfLookup();
                        var children = element.FindElements(@by)
                            .Select((lmnt, index) =>
                            {
                                return new VostokWebElement(this.settings, lmnt, @by, this.context,
                                    ctx => ctx.FindElements(@by).ElementAt(index));
                            })
                            .ToArray();



                        this.settings.DebugLogger($"Found {children.Count()} to {this.selfSelector} matching {@by}");
                        return children;
                    }
                    catch (StaleElementReferenceException)
                    {
                        this.settings.DebugLogger($"Element '{this.selfSelector}' is stale. - children lookup failed");
                        var vostokElement = this.context as VostokWebElement;
                        if (vostokElement != null)
                        {
                            vostokElement.Nuke();
                        }

                        return this.FindElements(@by);
                    }
                }

                this.settings.DebugLogger($"browser->elements: {@by}");
                return this.context.FindElements(@by)
                    .Select((lmnt, index) =>
                    {
                        //each element must be able to re-resolve it self
                        //in this case, re-resolve all elements again and just pick the
                        //element that has the same index as before
                        return new VostokWebElement(this.settings, lmnt, @by, this.context,
                            ctx =>
                            {
                                var children = (selfLookup == null
                                    ? this.context.FindElements(@by)
                                    : this.selfLookup().FindElements(@by)).ToArray();

                                this.settings.DebugLogger($"Found {children.Count()} matching {@by}");
                                return children.ElementAt(index);
                            });
                    })
                    .ToArray();
            });
        }
    }
}