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

        public Link(string url, Element element)
        {
            Url = url;
            Element = element;
        }

        public Link(string url, Element element, string target)
        {
            Url = url;
            Element = element;
            Target = target;
        }

        public override string StartTag()
        {
            if (Target == null)
            {
                return $"<a href=\"{Url}\" style=\"{GetStyles()}\">";
            }
            else
            {
                return $"<a href=\"{Url}\" target=\"_{Target}\" style=\"{GetStyles()}\">";
            }
        }

        public override string Content()
        {
            var output = "";

            output += Element.StartTag();
            output += Element.Content();
            output += Element.EndTag();

            return output;
        }

        public override string EndTag()
        {
            return "</a>";
        }
    }
}
