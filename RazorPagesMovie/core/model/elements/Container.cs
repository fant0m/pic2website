using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesMovie.core.model.elements
{
    public class Container : Element
    {
        public List<Element> Elements { get; }
        // @todo možno ešte type - div/span
        public Container(int id)
        {
            Id = id;
            Elements = new List<Element>();
        }

        public override string StartTag()
        {
            return "<div>";
        }

        public override string Content()
        {
            var output = "";
            foreach (var element in Elements)
            {
                output += element.StartTag();
                output += element.Content();
                // @todo otázka či má element sub elementy?
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
