namespace Vostok
{
    using System;
    using System.Collections.ObjectModel;
    using System.Drawing;
    using OpenQA.Selenium;

    public class VostokWebElement
        : IWebElement
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
            VostokInteractionWrapper.Interact(ref this.element, this.selfSelector.ToString(), () => this.elementLookup(), query);
        }

        private T Interact<T>(Func<IWebElement, T> query)
        {
            return VostokInteractionWrapper.Interact(ref this.element, this.selfSelector.ToString(), () => this.elementLookup(), query);
        }
    }
}