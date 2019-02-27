using System.Collections.Generic;
using System.Linq;

namespace RazorPagesMovie.core.model.elements.grid
{
    public class Row : Element
    {
        public List<Column> Columns { get; set; }
        public bool ActAsColumn { get; set; }
        public bool MergedColumns { get; set; }
        public override string Tag { get; set; } = "div";
        public override bool PairTag { get; set; } = true;

        public Row()
        {
            Columns = new List<Column>();
            MergedColumns = false;

            ClassNames.Add("row");
        }

        public void AcsAsColumn()
        {
            ActAsColumn = true;

            ClassNames.Remove("row");
            ClassNames.Add("inline-block");
        }

        public override List<Element> GetSubElements()
        {
            return Columns.Cast<Element>().ToList();
        }
    }
}
