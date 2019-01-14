using System.Collections.Generic;

namespace RazorPagesMovie.core.model.elements.grid
{
    public class Column : Element
    {
        public List<Element> Elements { get; set; }

        public Column(int id)
        {
            Id = id;
            Elements = new List<Element>();
        }

        public override string StartTag()
        {
            return $"<div class=\"col\" style=\"{GetStyles()}\">";
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
