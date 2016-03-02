using System;

namespace Vostok
{
    public enum PageOriginStrictness
    {
        /// <summary>
        /// Don't perform any check when re-resolving stale elements
        /// </summary>
        DontCheckOrigin,

        /// <summary>
        /// Check that the uri of the page where the element was first resolved matches the page where the element is re-resolved after becoming stale but ignore differences in #anchor part of the uri.
        /// </summary>
        AllowNonMatchingAnchorHashes,

        /// <summary>
        /// Check that the uri of the page where the element was first resolved matches the page where the element is re-resolved after becoming stale but ignore differences in query strings and #anchor part of the uri.
        /// </summary>
        AllowNonMatchingQueryStrings,

        /// <summary>
        /// Check that the uri of the page where the element was first resolved matches exactly to the page where the element is re-resolved after becoming stale
        /// </summary>
        ExactMatch

    }

    public class VostokSettings
    {
        /// <summary>
        /// Setting for when to allow re-resolving of elements
        /// </summary>
        public PageOriginStrictness SamePageOriginStrictness { get; set; }
        public Action<string> DebugLogger { get; set; }

        public VostokSettings()
        {
            this.DebugLogger = _ => { };
        }

        public static VostokSettings Default
        {
            get
            {
                return new VostokSettings
                {
                    SamePageOriginStrictness = PageOriginStrictness.AllowNonMatchingAnchorHashes
                };
            }
        }
    }
}