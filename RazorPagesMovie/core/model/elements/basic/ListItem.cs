using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesMovie.core.model.elements.basic
{
    public class ListItem : Element
    {
        public Element Element;
        public Link Link;

        public ListItem(Element element)
        {
            Element = element;
        }

        public ListItem(Element element, string link)
        {
            Element = element;
            Link = new Link(link, element);
        }

        public ListItem(Element element, string link, string target)
        {
            Element = element;
            Link = new Link(link, element, target);
        }

        public override string StartTag()
        {
            return "<li>";
        }

        public override string Content()
        {
            var output = "";

            if (Link != null)
            {
                output += Link.StartTag();
                output += Link.Content();
                output += Link.EndTag();
            }
            else
            {
                output += Element.StartTag();
                output += Element.Content();
                output += Element.EndTag();
            }

            return output;
        }

        public override string EndTag()
        {
            return "</li>";
        }
    }
}
