using System.Collections.Generic;

namespace RazorPagesMovie.core.model.elements.grid
{
    public class Column : Element
    {
        public List<Element> Elements { get; set; }
        public override string Tag { get; set; } = "div";
        public override bool PairTag { get; set; } = true;

        public Column(int id)
        {
            Id = id;
            Elements = new List<Element>();

            ClassNames.Add("col");
        }

        public override List<Element> GetSubElements()
        {
            return Elements;
        }
    }
}
