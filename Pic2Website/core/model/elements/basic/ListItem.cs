using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pic2Website.core.model.elements.basic
{
    public class ListItem : Element
    {
        public Element Element;
        public Link Link;
        public override string Tag { get; set; } = "li";
        public override bool PairTag { get; set; } = true;

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

        public override List<Element> GetSubElements()
        {
            var elements = new List<Element>(1);

            if (Link != null)
            {
                elements.Add(Link);
            }
            else
            {
                elements.Add(Element);
            }

            return elements;
        }
    }
}
