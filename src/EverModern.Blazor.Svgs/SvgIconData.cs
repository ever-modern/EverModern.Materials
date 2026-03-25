using System.Collections.Generic;

namespace EverModern.Svgs
{
    /// <summary>
    /// Represents SVG icon data with path information and viewBox settings.
    /// </summary>
    public struct SvgIconData
    {
        /// <summary>
        /// Gets the path data for the SVG icon.
        /// </summary>
        public IReadOnlyList<string> Paths { get; }

        /// <summary>
        /// Gets the viewBox for the SVG icon.
        /// </summary>
        public string ViewBox { get; }

        /// <summary>
        /// Initializes a new instance of SvgIconData with the specified paths and viewBox.
        /// </summary>
        /// <param name="paths">The path data for the SVG icon.</param>
        /// <param name="viewBox">The viewBox for the SVG icon.</param>
        public SvgIconData(IReadOnlyList<string> paths, string viewBox)
        {
            Paths = paths;
            ViewBox = viewBox;
        }

        /// <summary>
        /// Default viewBox constant for Material Design icons.
        /// </summary>
        public const string DefaultViewBox = "0 0 24 24";
    }
}