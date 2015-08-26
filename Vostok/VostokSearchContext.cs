﻿namespace Vostok
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
        private readonly ISearchContext context;
        private readonly Func<IWebElement> selfLookup;

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
            if (element != null)
            {
                //Console.WriteLine("element->element: {0}", @by);
                return VostokInteractionWrapper.Interact(
                        ref element, 
                        this.selfSelector, 
                        () => this.selfLookup(), 
                        lmnt => new VostokWebElement(this.settings, lmnt, selfSelector, this, ctx => ctx.FindElement(@by))
                );
            }

            //context is IWebDriver, no need to guard for stale element
            //Console.WriteLine("driver->element: {0}", @by);
            element = this.context.FindElement(@by);
            return new VostokWebElement(this.settings, element, @by, this.context, ctx => ctx.FindElement(@by));
        }

        public ReadOnlyCollection<IWebElement> FindElements(By @by)
        {
            var element = this.context as IWebElement;
            
            return new EagerReadOnlyCollection<IWebElement>(() =>
                                                            (element != null
                                                                 ? VostokInteractionWrapper.Interact(ref element, this.selfSelector, () => this.selfLookup(), lmnt => this.selfLookup().FindElements(@by))
                                                                 : this.context.FindElements(@by))
                                                                .Select((lmnt, index) =>
                                                                    {
                                                                        //each element must be able to re-resolve it self
                                                                        //in this case, re-resolve all elements again and just pick the
                                                                        //element that has the same index as before
                                                                        return new VostokWebElement(this.settings, lmnt, @by, this,
                                                                            ctx =>
                                                                            {
                                                                                var children = selfLookup == null
                                                                                    ? this.context.FindElements(@by)
                                                                                    : this.selfLookup().FindElements(@by);

                                                                                return children.ElementAt(index);
                                                                            });
                                                                    })
                );
        }
    }
}