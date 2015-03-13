namespace Vostok
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using OpenQA.Selenium;

    public class VostokWebDriver
        : IWebDriver
    {
        private readonly IWebDriver driver;
        private readonly VostokSearchContext context;

        public VostokWebDriver(IWebDriver driver)
        {
            this.driver = driver;
            this.context = new VostokSearchContext(null, this.driver, () => null);
        }

        public IWebElement FindElement(By @by)
        {
            return this.context.FindElement(@by);
        }

        public ReadOnlyCollection<IWebElement> FindElements(By @by)
        {
            return this.context.FindElements(@by);
        }

        public void Dispose()
        {
            this.driver.Dispose();
        }

        public void Close()
        {
            this.driver.Close();
        }

        public void Quit()
        {
            this.driver.Quit();
        }

        public IOptions Manage()
        {
            return this.driver.Manage();
        }

        public INavigation Navigate()
        {
            return this.driver.Navigate();
        }

        public ITargetLocator SwitchTo()
        {
            return this.driver.SwitchTo();
        }

        public string Url
        {
            get { return this.driver.Url; }
            set { this.driver.Url = value; }
        }

        public string Title
        {
            get { return this.driver.Title; }
        }

        public string PageSource
        {
            get { return this.driver.PageSource; }
        }

        public string CurrentWindowHandle
        {
            get { return this.driver.CurrentWindowHandle; }
        }

        public ReadOnlyCollection<string> WindowHandles
        {
            get { return new EagerReadOnlyCollection<string>(() => this.driver.WindowHandles); }
        }
    }

    public class EagerReadOnlyCollection<T>
        : ReadOnlyCollection<T>
    {
        public EagerReadOnlyCollection(Func<IEnumerable<T>> collection)
            : this(new EagerList<T>(collection))
        {
        }

        public EagerReadOnlyCollection(EagerList<T> list)
            : base(list)
        {
        }
    }

    public class EagerList<T>
        : IList<T>
    {
        private Func<IEnumerable<T>> collectionQuery;
        private IEnumerable<T> Collection
        {
            get 
            {
                foreach (var item in this.collectionQuery())
                {
                    yield return item;
                }
            }
        }

        public EagerList(Func<IEnumerable<T>> collection)
        {
            this.collectionQuery = collection;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.Collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            throw new InvalidOperationException("Cannot add items to a read only collection.");
        }

        public void Clear()
        {
            throw new InvalidOperationException("Cannot clear a read only collection.");
        }

        public bool Contains(T item)
        {
            return this.Collection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.Collection.ToList().CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            throw new InvalidOperationException("Cannot remove items from a read only collection.");
        }

        public int Count
        {
            get { return this.Collection.Count(); }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public int IndexOf(T item)
        {
            return this.Collection.ToList().IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            throw new InvalidOperationException("Cannot insert items in a read only collection.");
        }

        public void RemoveAt(int index)
        {
            throw new InvalidOperationException("Cannot remove items from a read only collection.");
        }

        public T this[int index]
        {
            get { return this.Collection.ToList()[index]; }
            set { throw new InvalidOperationException("Cannot modify items in a read only collection."); }
        }
    }
}
