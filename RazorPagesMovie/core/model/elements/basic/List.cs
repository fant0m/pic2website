using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesMovie.core.model.elements.basic
{
    public class List : Element
    {
        public List<ListItem> Items;
        public string Type;

        public List()
        {
            Items = new List<ListItem>();
            Type = "unordered";
        }

        public override string StartTag()
        {
            var tag = Type == "unordered" ? "ul" : "ol";
            return $"<{tag} style=\"{GetStyles()}\">";
        }

        public override string Content()
        {
            var output = "";
            foreach (var item in Items)
            {
                output += item.StartTag();
                output += item.Content();
                output += item.EndTag();
            }
            return output;
        }

        public override string EndTag()
        {
            var tag = Type == "unordered" ? "ul" : "ol";
            return $"</{tag}>";
        }

        
    }
}
