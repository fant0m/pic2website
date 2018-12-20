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
            W1600 = 1600,
            W2000 = 2000
        }

        public LayoutType Type { get; set; }
        public LayoutWidth Width { get; set; }
        public double RealWidth { get; set; }
        public double RealHeight { get; set; }
        public double Padding { get; set; }

        public Layout(LayoutType type, double realWidth, double realheight)
        {
            Type = type;
            RealWidth = realWidth;
            RealHeight = realheight;

            DetectContainerWidth(RealWidth);
        }

        /**
         * Find smallest container width fitting template width
         */
        private void DetectContainerWidth(double width)
        {
            var values = Enum.GetValues(typeof(LayoutWidth)).Cast<LayoutWidth>().ToList();
            Width = values.OrderByDescending(x => (int)x >= width).First();
            Padding = (int) Width - RealWidth;
        }

        public void RecalculateWidth(LayoutWidth layoutWidth)
        {
            Width = layoutWidth;
            Padding = (int) Width - RealWidth;
        }
    }
}
