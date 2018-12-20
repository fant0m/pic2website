using System;
using OpenCvSharp;

namespace RazorPagesMovie.core.model.elements
{
    public abstract class Element : IWebElement
    {
        public int Id;
        public int[] Padding;
        public int[] Margin;
        public double Width;
        public int Height;
        public int[] Color;
        public string Class;
        public Border Border;
        public int[] BackgroundColor;
        public Rect Rect;

        // @todo tu asi bude musieť byť list sub elementov

        protected Element()
        {
            Color = new int[3];
            Margin = new[] { 0, 0, 0, 0 };
            Padding = new[] { 0, 0, 0, 0 };
            Padding = new[] { 0, 0, 0, 0 };
            BackgroundColor = new[] { 0, 0, 0 };
        }

        public string GetId()
        {
            return (GetType().Name + "-" + Id).ToLower();
        }

        public string GetStyles()
        {
            var styles = "";
            if (Width > 0)
            {
                styles += $"width:{Width}px;";
            }

            if (Height > 0)
            {
                styles += $"height:{Height}px;";
            }

            if (BackgroundColor[0] != 0)
            {
                styles += $"background: rgb({BackgroundColor[0]},{BackgroundColor[1]},{BackgroundColor[2]});";
            }

            styles += $"margin:{Margin[0]}px {Margin[1]}px {Margin[2]}px {Margin[3]}px;";
            styles += $"padding:{Padding[0]}px {Padding[1]}px {Padding[2]}px {Padding[3]}px;";

            return styles;
        }

        public abstract string StartTag();

        public abstract string Content();
        public abstract string EndTag();
    }
}
