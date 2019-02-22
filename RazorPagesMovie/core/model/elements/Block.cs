using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesMovie.core.model.elements
{
    public class Block : Element
    {
        public List<Element> Elements { get; set; }

        public Block()
        {
            Elements = new List <Element>();
        }

        public override string StartTag()
        {
            return $"<div style=\"{GetStyles()}\">";
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
