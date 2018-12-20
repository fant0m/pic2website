using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RazorPagesMovie.core.model.elements.grid;

namespace RazorPagesMovie.core.model.elements
{
    public class Container : Element
    {
        public Layout.LayoutType LayoutType { get; set; }
        public List<Row> Rows { get; set; }
        public Container(int id, Layout.LayoutType layoutType)
        {
            Id = id;
            Rows = new List<Row>();
            LayoutType = layoutType;
        }

        public override string StartTag()
        {
            var type = LayoutType == Layout.LayoutType.Centered ? "container" : "container-fluid";

            return $"<div class=\"{type}\">";
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
