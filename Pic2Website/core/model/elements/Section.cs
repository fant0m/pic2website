using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;

namespace Pic2Website.core.model.elements
{
    public class Section : Element
    {
        public Layout Layout { get; set; }
        public List<Container> Containers { get; set; }
        public int Top { get; set; }
        public override string Tag { get; set; } = "section";
        public override bool PairTag { get; set; } = true;

        public Section(int id)
        {
            Id = id;
            Containers = new List<Container>();
        }

        public override List<Element> GetSubElements()
        {
            return Containers.Cast<Element>().ToList();
        }
    }
}
