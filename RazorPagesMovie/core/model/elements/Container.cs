using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RazorPagesMovie.core.model.elements.grid;

namespace RazorPagesMovie.core.model.elements
{
    public class Container : Element
    {
        public List<Element> Rows { get; set; }
        public override string Tag { get; set; } = "div";
        public override bool PairTag { get; set; } = true;

        public Container(Layout layout)
        {
            Rows = new List<Element>();

            ClassNames.Add("container");
            InitAttributes(layout);
        }

        private void InitAttributes(Layout layout)
        {
            if (layout.Type == Layout.LayoutType.Centered)
            {
                ClassNames.Add("container");
                Width = (int)layout.Width;
            }
            else
            {
                ClassNames.Add("container-fluid");
                Width = 100;
                Fluid = true;
            }
        }

        public override List<Element> GetSubElements()
        {
            return Rows.Cast<Element>().ToList();
        }
    }
}
