﻿using System.Collections.Generic;

namespace RazorPagesMovie.core.model.elements.grid
{
    public class Column : Element
    {
        public List<Element> Elements { get; }

        public Column(int id)
        {
            Id = id;
            Elements = new List<Element>();
        }

        public override string StartTag()
        {
            // @todo inteligentnejšie pripraviť css vlastnosti, globlálne pre všetky elementy
            if (Width > 0)
            {
                return $"<div class=\"col\" style=\"width:{Width}px;margin:{Margin[0]}px {Margin[1]}px {Margin[2]}px {Margin[3]}px\">";
            }
            else
            {
                return $"<div class=\"col\" style=\"margin:{Margin[0]}px {Margin[1]}px {Margin[2]}px {Margin[3]}px\">";
            }
        }

        public override string Content()
        {
            var output = "";
            foreach (var element in Elements)
            {
                output += element.StartTag();
                output += element.Content();
                output += element.EndTag();
            }
            return output;
        }

        public override string EndTag()
        {
            return "</div>";
        }
    }
}
