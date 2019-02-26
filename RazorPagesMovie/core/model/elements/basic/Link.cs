using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesMovie.core.model.elements.basic
{
    public class Link : Element
    {
        public string Url;
        public string Target;
        public Element Element;
        public override string Tag { get; set; } = "a";
        public override bool PairTag { get; set; } = true;

        public Link(string url, Element element)
        {
            Url = url;
            Element = element;

            Attributes.Add("href", Url);
        }

        public Link(string url, Element element, string target)
        {
            Url = url;
            Element = element;
            Target = target;

            Attributes.Add("href", Url);
            Attributes.Add("target", "_" + target);
        }

        public override List<Element> GetSubElements()
        {
            var elements = new List<Element>(1);
            elements.Add(Element);
            return elements;
        }
    }
}
