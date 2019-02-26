using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesMovie.core.model.elements
{
    public class Block : Element
    {
        public List<Element> Elements { get; set; }
        public override string Tag { get; set; } = "div";
        public override bool PairTag { get; set; } = true;

        public Block()
        {
            Elements = new List <Element>();
        }

        public override List<Element> GetSubElements()
        {
            return Elements;
        }
    }
}
