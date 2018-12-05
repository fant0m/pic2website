using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RazorPagesMovie.core.model.elements.grid;

namespace RazorPagesMovie.core.model.elements
{
    public class Container : Element
    {
        public List<Row> Rows { get; set; }
        // @todo možno ešte type - div/span resp. blok/nie blok element
        public Container(int id)
        {
            Id = id;
            Rows = new List<Row>();
        }

        public override string StartTag()
        {
            return "<div class=\"container\">";
        }

        public override string Content()
        {
            var output = "";
            foreach (var row in Rows)
            {
                output += row.StartTag();
                output += row.Content();
                output += row.EndTag();
            }
            return output;
        }

        public override string EndTag()
        {
            return "</div>";
        }
    }
}
