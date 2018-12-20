using System.Collections.Generic;
using OpenCvSharp;

namespace RazorPagesMovie.core.model.elements
{
    public class Section : Element
    {
        // @todo special section type - header, footer
        // @todo background image sa bude riešiť asi tu, ak bude mať sekcia bg img tak len pridám ďalšie elementy do section
        // @todo môže mať section viac containerov vôbec? teoreticky asi nie
        public Layout Layout { get; set; }
        public List<Container> Containers { get; set; }
        public int Top { get; set; }

        public Section(int id)
        {
            Id = id;
            Containers = new List<Container>();
        }

        public override string StartTag()
        {
            return $"<section style=\"{GetStyles()}\">";
        }

        public override string Content()
        {
            var output = "";
            foreach (var element in Containers)
            {
                output += element.StartTag();
                output += element.Content();
                output += element.EndTag();
            }
            return output;
        }

        public override string EndTag()
        {
            return "</section>";
        }
    }
}
