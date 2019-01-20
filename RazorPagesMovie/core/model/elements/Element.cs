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
        public int MinWidth;
        public int Height;
        public int[] Color;
        public string Class;
        public Border Border;
        public int[] BackgroundColor;
        public string BackgroundImage;
        public string TextAlign;
        // @todo neviem či to nedať iba elementu text alebo to budú aj iné využívať (aby to nemusel mať každý text)
        public string FontFamily;
        public int FontSize;
        public int FontWeight;
        public string FontStyle;
        public Rect Rect;
        public bool Fluid;

        // @todo tu asi bude musieť byť list sub elementov

        protected Element()
        {
            Color = null;
            Margin = new[] { 0, 0, 0, 0 };
            Padding = new[] { 0, 0, 0, 0 };
            Padding = new[] { 0, 0, 0, 0 };
            BackgroundColor = new[] { 0, 0, 0 };
        }

        public string GetId()
        {
            return (GetType().Name + "-" + Id).ToLower();
        }

        // @todo refactor
        public string GetStyles()
        {
            var styles = "";
            if (Width > 0)
            {
                styles += $"width:{Width}";
                styles += Fluid ? "%" : "px";
                styles += ";";
            }

            if (MinWidth > 0)
            {
                styles += $"min-width:{MinWidth}px;";
            }

            if (Height > 0)
            {
                styles += $"height:{Height}px;";
            }

            if (BackgroundImage != null)
            {
                styles += $"background:url({BackgroundImage});";
            }
            else if (BackgroundColor[0] != 0)
            {
                styles += $"background:rgb({BackgroundColor[0]},{BackgroundColor[1]},{BackgroundColor[2]});";
            }

            if (Fluid)
            {
                styles += $"margin:{Margin[0]}px {Margin[1]}% {Margin[2]}px {Margin[3]}%;";
            }
            else
            {
                styles += $"margin:{Margin[0]}px {Margin[1]}px {Margin[2]}px {Margin[3]}px;";
            }
            styles += $"padding:{Padding[0]}px {Padding[1]}px {Padding[2]}px {Padding[3]}px;";

            if (TextAlign != null)
            {
                styles += $"text-align:{TextAlign};";
            }

            if (!string.IsNullOrEmpty(FontFamily))
            {
                styles += $"font-family:{FontFamily};";
            }

            if (!string.IsNullOrEmpty(FontStyle))
            {
                styles += $"font-style:{FontStyle};";
            }

            if (FontSize > 0)
            {
                styles += $"font-size:{FontSize}px;";
            }

            if (FontWeight > 0)
            {
                styles += $"font-weight:{FontWeight};";
            }

            if (Color != null)
            {
                styles += $"color:rgb({Color[0]},{Color[1]},{Color[2]});";
            }

            return styles;
        }

        public abstract string StartTag();

        public abstract string Content();
        public abstract string EndTag();
    }
}
