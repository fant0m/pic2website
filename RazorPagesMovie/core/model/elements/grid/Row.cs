using System.Collections.Generic;

namespace RazorPagesMovie.core.model.elements.grid
{
    public class Row : Element
    {
        public List<Column> Columns { get; }

        public Row(int id)
        {
            Id = id;
            Columns = new List<Column>();
        }

        public override string StartTag()
        {
            return "<div class=\"row\">";
        }

        public override string Content()
        {
            var output = "";
            foreach (var column in Columns)
            {
                output += column.StartTag();
                output += column.Content();
                output += column.EndTag();
            }
            return output;
        }

        public override string EndTag()
        {
            return "</div>";
        }
    }
}
