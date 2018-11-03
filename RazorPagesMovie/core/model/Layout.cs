using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesMovie.core.model
{
    public class Layout
    {
        public enum LayoutType
        {
            Fluid,
            Centered
        }

        public enum LayoutWidth
        {
            W800 = 800,
            W1200 = 1200,
            W1600 = 1600
        }

        private LayoutType _type;
        private LayoutWidth _layoutWidth;
        private double _width;
        private double _height;
        private double _padding;

        public Layout(LayoutType type, double width, double height)
        {
            _type = type;
            _width = width;
            _height = height;

            DetectContainerWidth(width);
        }

        /**
         * Find smallest container width fitting template width
         */
        private void DetectContainerWidth(double width)
        {
            var values = Enum.GetValues(typeof(Layout.LayoutWidth)).Cast<Layout.LayoutWidth>().ToList();
            _layoutWidth = values.OrderByDescending(x => (int)x >= width).First();
            _padding = (int) _layoutWidth - _width;

            Debug.WriteLine(width + "=" +_layoutWidth + "=" + _padding);
        }
    }
}
