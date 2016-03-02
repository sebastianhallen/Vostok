namespace Vostok
{
    using System;
    using System.Collections.ObjectModel;
    using System.Drawing;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Interactions.Internal;
    using OpenQA.Selenium.Internal;

    public class VostokWebElement
        : IWebElement 
        ,IFindsByLinkText, IFindsById, IFindsByName, IFindsByTagName, IFindsByClassName, IFindsByXPath, IFindsByPartialLinkText, IFindsByCssSelector
        ,IWrapsDriver, IWrapsElement, ILocatable, ITakesScreenshot
    {
        private readonly VostokSettings settings;
        private readonly By selfSelector;
        private IWebElement element;
        private readonly Uri origin;

        private readonly VostokSearchContext context;
        private readonly Func<IWebElement> elementLookup;
        public VostokWebElement(VostokSettings settings, IWebElement element, By selfSelector, ISearchContext parent, Func<ISearchContext, IWebElement> selfLookup)
        {
            this.settings = settings;
            this.selfSelector = selfSelector;
            this.element = element;
            this.origin = this.DetermineOrigin(this.element);
            this.elementLookup = () =>
                {
                    //has the element become stale and been null:ed by VostokInteractionWrapper
                    if (this.element == null)
                    {
                        this.settings.DebugLogger(string.Format("resolving: {0}", selfSelector));
                        this.element = selfLookup(parent);
                                                
                        var currentUri = this.DetermineOrigin(this.element);

                        if (this.origin != null)
                        {
                            if (!this.MatchesOriginStrictnessLevel(currentUri))
                            {
                                var message = string.Format("Navigation occured between resolving elements. Original element was resolved on {0} but a StaleElementReferenceException caused it to be re-resolved on {1}. You can control the sensitivty of this check by changing SamePageOriginStrictness in the settings passed to the VostokWebDriver", this.origin, currentUri);
                                throw new InvalidElementStateException(message);
                            }
                        }
                    }

                    return this.element;
                };
            this.context = new VostokSearchContext(this.settings, selfSelector, this, this.elementLookup);
        }

        private bool MatchesOriginStrictnessLevel(Uri currentUri)
        {
            switch (this.settings.SamePageOriginStrictness)
            {
                case PageOriginStrictness.DontCheckOrigin:
                {
                    return true;
                }
                case PageOriginStrictness.AllowNonMatchingAnchorHashes:
                {
                    var originHash0 = this.origin.Scheme + this.origin.Authority + this.origin.Port + this.origin.PathAndQuery;
                    var currentHash0 = currentUri.Scheme + currentUri.Authority + currentUri.Port + currentUri.PathAndQuery;
                    return originHash0.Equals(currentHash0);
                }

                case PageOriginStrictness.AllowNonMatchingQueryStrings:
                {
                    var originHash1 = this.origin.Scheme + this.origin.Authority + this.origin.Port + this.origin.AbsolutePath;
                    var currentHash1 = currentUri.Scheme + currentUri.Authority + currentUri.Port + currentUri.AbsolutePath;
                    return originHash1.Equals(currentHash1);
                }
                case PageOriginStrictness.ExactMatch:
                {
                    return this.origin.ToString().Equals(currentUri.ToString());
                }
            }

            throw new Exception("Unhandled PageOriginStrictness setting: " + this.settings);
        }

        private Uri DetermineOrigin(IWebElement lmnt)
        {
            var driver = lmnt as IWrapsDriver;

            if (driver == null)
            {
                return null;
            }

            return new Uri(driver.WrappedDriver.Url);
        }

        public IWebElement FindElement(By @by)
        {
            return this.context.FindElement(@by);
        }

        public ReadOnlyCollection<IWebElement> FindElements(By @by)
        {
            return this.context.FindElements(@by);
        }

        public void Clear()
        {
            this.Interact(lmnt => lmnt.Clear());
        }

        public void SendKeys(string text)
        {
            this.Interact(lmnt => lmnt.SendKeys(text));
        }

        public void Submit()
        {
            this.Interact(lmnt => lmnt.Submit());
        }

        public void Click()
        {
            this.Interact(lmnt => lmnt.Click());
        }

        public string GetAttribute(string attributeName)
        {
            return this.Interact(lmnt => lmnt.GetAttribute(attributeName));
        }

        public string GetCssValue(string propertyName)
        {
            return this.Interact(lmnt => lmnt.GetCssValue(propertyName));
        }

        public string TagName { get { return this.Interact(lmnt => lmnt.TagName); } }
        public string Text { get { return this.Interact(lmnt => lmnt.Text); } }
        public bool Enabled { get { return this.Interact(lmnt => lmnt.Enabled); } }
        public bool Selected { get { return this.Interact(lmnt => lmnt.Selected); } }
        public Point Location { get { return this.Interact(lmnt => lmnt.Location); } }
        public Size Size { get { return this.Interact(lmnt => lmnt.Size); } }
        public bool Displayed { get { return this.Interact(lmnt => lmnt.Displayed); } }

        private void Interact(Action<IWebElement> query)
        {
            VostokInteractionWrapper.Interact(ref this.element, this.selfSelector, () => this.elementLookup(), query, this.settings);
        }

        private T Interact<T>(Func<IWebElement, T> query)
        {
            return VostokInteractionWrapper.Interact(ref this.element, this.selfSelector, () => this.elementLookup(), query, this.settings);
        }


        IWebElement IFindsByLinkText.FindElementByLinkText(string linkText)
        {
            return this.Interact(lmnt =>
            {
                var e = ((IFindsByLinkText)lmnt);
                return e.FindElementByLinkText(linkText);
            });
        }

        ReadOnlyCollection<IWebElement> IFindsByLinkText.FindElementsByLinkText(string linkText)
        {
            return this.Interact(lmnt =>
            {
                var e = ((IFindsByLinkText) lmnt);
                return e.FindElementsByLinkText(linkText);
            });
        }

        IWebDriver IWrapsDriver.WrappedDriver
        {
            get
            {
                var wrapper = (IWrapsDriver) this.element;
                return wrapper.WrappedDriver;
            }
        }

        public IWebElement WrappedElement
        {
            get
            {
                Func<IWebElement, IWebElement> unpack = e => e; 
                unpack = e =>
                {
                    var innerWrapper = e as IWrapsElement;
                    if (innerWrapper == null)
                    {
                        return e;
                    }

                    return unpack(innerWrapper.WrappedElement);
                };


                return this.Interact(lmnt => unpack(lmnt));
            }
        }

        public Point LocationOnScreenOnceScrolledIntoView
        {
            get
            {
                return this.Interact(lmnt =>
                {
                    var locatable = (ILocatable) lmnt;
                    return locatable.LocationOnScreenOnceScrolledIntoView;
                });
            }
        }

        public ICoordinates Coordinates
        {
            get
            {
                return this.Interact(lmnt =>
                {
                    var locatable = (ILocatable) lmnt;
                    return locatable.Coordinates;
                });
            }
        }

        public Screenshot GetScreenshot()
        {
            return this.Interact(lmnt => ((ITakesScreenshot) lmnt).GetScreenshot());
        }

        public IWebElement FindElementById(string id)
        {
            return this.Interact(lmnt => ((IFindsById) lmnt).FindElementById(id));
        }

        public ReadOnlyCollection<IWebElement> FindElementsById(string id)
        {
            return this.Interact(lmnt => ((IFindsById)lmnt).FindElementsById(id));
        }

        public IWebElement FindElementByName(string name)
        {
            return this.Interact(lmnt => ((IFindsByName)lmnt).FindElementByName(name));
        }

        public ReadOnlyCollection<IWebElement> FindElementsByName(string name)
        {
            return this.Interact(lmnt => ((IFindsByName)lmnt).FindElementsByName(name));
        }

        public IWebElement FindElementByTagName(string tagName)
        {
            return this.Interact(lmnt => ((IFindsByTagName)lmnt).FindElementByTagName(tagName));
        }

        public ReadOnlyCollection<IWebElement> FindElementsByTagName(string tagName)
        {
            return this.Interact(lmnt => ((IFindsByTagName)lmnt).FindElementsByTagName(tagName));
        }

        public IWebElement FindElementByClassName(string className)
        {
            return this.Interact(lmnt => ((IFindsByClassName)lmnt).FindElementByClassName(className));
        }

        public ReadOnlyCollection<IWebElement> FindElementsByClassName(string className)
        {
            return this.Interact(lmnt => ((IFindsByClassName)lmnt).FindElementsByClassName(className));
        }

        public IWebElement FindElementByXPath(string xpath)
        {
            return this.Interact(lmnt => ((IFindsByXPath)lmnt).FindElementByXPath(xpath));
        }

        public ReadOnlyCollection<IWebElement> FindElementsByXPath(string xpath)
        {
            return this.Interact(lmnt => ((IFindsByXPath)lmnt).FindElementsByXPath(xpath));
        }

        public IWebElement FindElementByPartialLinkText(string partialLinkText)
        {
            return this.Interact(lmnt => ((IFindsByPartialLinkText)lmnt).FindElementByPartialLinkText(partialLinkText));
        }

        public ReadOnlyCollection<IWebElement> FindElementsByPartialLinkText(string partialLinkText)
        {
            return this.Interact(lmnt => ((IFindsByPartialLinkText)lmnt).FindElementsByPartialLinkText(partialLinkText));
        }

        public IWebElement FindElementByCssSelector(string cssSelector)
        {
            return this.Interact(lmnt => ((IFindsByCssSelector)lmnt).FindElementByCssSelector(cssSelector));
        }

        public ReadOnlyCollection<IWebElement> FindElementsByCssSelector(string cssSelector)
        {
            return this.Interact(lmnt => ((IFindsByCssSelector)lmnt).FindElementsByCssSelector(cssSelector));
        }
    }
}