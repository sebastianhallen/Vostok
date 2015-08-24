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
        private readonly By selfSelector;
        private IWebElement element;
        private readonly VostokSearchContext context;
        private readonly Func<IWebElement> elementLookup;
        public VostokWebElement(IWebElement element, By selfSelector, ISearchContext parent, Func<ISearchContext, IWebElement> selfLookup)
        {
            this.selfSelector = selfSelector;
            this.element = element;
            this.elementLookup = () =>
                {
                    if (this.element == null)
                    {
                        //Console.WriteLine("resolving: {0}", selfSelector);
                        this.element = selfLookup(parent);
                    }

                    return this.element;
                };
            this.context = new VostokSearchContext(selfSelector, this, this.elementLookup);
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
            VostokInteractionWrapper.Interact(ref this.element, this.selfSelector, () => this.elementLookup(), query);
        }

        private T Interact<T>(Func<IWebElement, T> query)
        {
            return VostokInteractionWrapper.Interact(ref this.element, this.selfSelector, () => this.elementLookup(), query);
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