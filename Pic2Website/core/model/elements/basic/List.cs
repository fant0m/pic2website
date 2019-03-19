using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pic2Website.core.model.elements.basic
{
    public class List : Element
    {
        public List<ListItem> Items;
        public string Type;
        public override string Tag { get; set; }
        public override bool PairTag { get; set; } = true;

        public List()
        {
            Items = new List<ListItem>();
            Type = "unordered";

            Tag = Type == "unordered" ? "ul" : "ol";
        }

        public override List<Element> GetSubElements()
        {
            return Items.Cast<Element>().ToList();
        }
    }
}
