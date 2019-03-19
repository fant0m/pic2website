using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenCvSharp;
using Pic2Website.core.model.elements.basic;

namespace Pic2Website.core.model.elements
{
    public abstract class Element : IWebElement
    {
        private int id;
        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                if (id != 0 && ClassNames.Contains(GetId()))
                {
                    ClassNames.Remove(GetId());
                }
                id = value;
                if (value != 0)
                {
                    ClassNames.Add(GetId());
                }
            }
        }
        public List<string> ClassNames { get; set; }
        public Dictionary<string, string> Attributes { get; set; }

        public int[] Padding;
        public int[] Margin;
        public string[] MarginCalc;
        public double Width;
        public int MinWidth;
        public int Height;
        public int[] Color;
        public string Class;
        public int[] BackgroundColor;
        public string BackgroundImage;
        public string TextAlign;
        public string FontFamily;
        public int FontSize;
        public int FontWeight;
        public string FontStyle;
        public double LineHeight;
        public string Display;

        public Rect Rect;
        public bool Fluid;

        protected Element()
        {
            Color = null;
            Margin = new[] { 0, 0, 0, 0 };
            Padding = new[] { 0, 0, 0, 0 };
            MarginCalc = new[] { "", "", "", "" };
            ClassNames = new List<string>();
            Attributes = new Dictionary<string, string>();
        }

        public string GetId()
        {
            return id != 0 ? (GetType().Name + "-" + id).ToLower() : GetType().Name.ToLower();
        }

        public string GetStyles()
        {
            var styles = "";

            // element doesn't have a tag so we don't need to check for styles
            if (Tag == "")
            {
                return styles;
            }

            if (Width > 0)
            {
                styles += $"width: {Width}";
                styles += Fluid ? "%" : "px";
                styles += ";";
            }

            if (MinWidth > 0)
            {
                styles += $"min-width: {MinWidth}px;";
            }

            if (Height > 0)
            {
                styles += $"height: {Height}px;";
            }

            if (BackgroundImage != null)
            {
                styles += $"background: url({BackgroundImage});";
            }
            else if (BackgroundColor != null)
            {
                styles += $"background: rgb({BackgroundColor[0]}, {BackgroundColor[1]}, {BackgroundColor[2]});";
            }

            if (Fluid)
            {
                styles += $"margin: {Margin[0]}px {Margin[1]}% {Margin[2]}px {Margin[3]}%;";
            }
            else if (!MarginCalc[1].Equals(""))
            {
                styles += $"margin: {Margin[0]}px {MarginCalc[1]} {Margin[2]}px {MarginCalc[3]};";
            }
            else if (Margin.Sum() != 0)
            {
                styles += $"margin: {Margin[0]}px {Margin[1]}px {Margin[2]}px {Margin[3]}px;";
            }

            if (Fluid && GetType() != typeof(Container))
            {
                styles += $"padding: {Padding[0]}px {Padding[1]}% {Padding[2]}px {Padding[3]}%;";
            }
            else if (Padding.Sum() != 0)
            {
                styles += $"padding: {Padding[0]}px {Padding[1]}px {Padding[2]}px {Padding[3]}px;";
            }

            if (TextAlign != null)
            {
                styles += $"text-align: {TextAlign};";
            }

            if (!string.IsNullOrEmpty(FontFamily))
            {
                styles += $"font-family: \"{FontFamily}\";";
            }

            if (!string.IsNullOrEmpty(FontStyle))
            {
                styles += $"font-style: {FontStyle};";
            }

            if (FontSize > 0)
            {
                styles += $"font-size: {FontSize}px;";
            }

            if (FontWeight > 0)
            {
                styles += $"font-weight: {FontWeight};";
            }

            if (Color != null)
            {
                styles += $"color: rgb({Color[0]}, {Color[1]}, {Color[2]});";
            }

            if (Display != null)
            {
                styles += $"display: {Display};";
            }

            if (LineHeight != 0)
            {
                styles += $"line-height: {LineHeight.ToString().Replace(",", ".")}px;";
            }

            // remove last semicolon
            if (styles != "")
            {
                styles = styles.Remove(styles.Length - 1);
            }

            return styles;
        }

        public string GetStyleSheet(string parent, int subId = 0)
        {
            var prefix = parent == "" ? "" : parent + " > ";
            var sheet = "";

            if (subId != 0)
            {
                Id = subId;
            }
            var currentSelector = Id != 0 ? "." + GetId() : Tag;
            var selector = prefix + currentSelector;

            var styles = GetStyles();
            
            if (styles != "")
            {
                var separate = styles.Split(";");

                sheet += selector + " {\n";

                for (var i = 0; i < separate.Length; i++)
                {
                    sheet += "\t" + separate[i] + ";";
                    if (i != separate.Length - 1)
                    {
                        sheet += "\n";
                    }
                }

                sheet += "\n}\n";
            }

            var subElements = GetSubElements();
            if (subElements != null)
            {
                if (subElements.Count == 1 && styles == "")
                {
                    Id = 0;
                }

                currentSelector = Id != 0 ? "." + GetId() : Tag;
                selector = prefix + currentSelector;

                for (var i = 0; i < subElements.Count; i++)
                {
                    subId++;
                    sheet += subElements[i].GetStyleSheet(selector, subId);
                }
            }
            else if (subElements == null && styles == "")
            {
                Id = 0;
            }

            return sheet;
        }
        
        public string GetClassAttribute()
        {
            if (ClassNames.Count != 0)
            {
                var names = string.Join(" ", ClassNames);
                return $" class=\"{names}\"";
            }
            else
            {
                return "";
            }
        }

        public string GetAttributes()
        {
            if (Attributes.Count != 0)
            {
                var output = "";
                foreach (var attr in Attributes)
                {
                    output += $" {attr.Key}=\"{attr.Value}\"";
                }
                return output;
            }
            else
            {
                return "";
            }
        }

        public string StartTag(int level)
        {
            if (Tag != "")
            {
                var startTag = "<" + Tag;
                startTag += GetClassAttribute();
                startTag += GetAttributes();
                //startTag += $" style=\"{GetStyles()}\"";
                startTag += PairTag ? ">" : "/>";

                return Util.Repeat('\t', level) + startTag + "\n";
            }
            else
            {
                return "";
            }
        }

        public string Content(int level)
        {
            var output = "";
            var subElements = GetSubElements();
            if (subElements != null)
            {
                foreach (var element in GetSubElements())
                {
                    output += element.StartTag(level);
                    output += element.Content(level + 1);
                    output += element.EndTag(level);
                }
            }
         
            if (GetType() == typeof(Text))
            {
                if (((Text)this).Tag == "")
                {
                    level--;
                }
                var self = (Text)this;
                output += Util.Repeat('\t', level) + string.Join("<br>", self.GetText()) + "\n";
            }

            return output;
        }

        public string EndTag(int level)
        {
            return PairTag ? $"{Util.Repeat('\t', level)}</{Tag}>\n" : "";
        }

        public abstract List<Element> GetSubElements();
        public abstract string Tag { get; set; }
        public abstract bool PairTag { get; set; }
    }
}
