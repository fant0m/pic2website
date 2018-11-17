using System.Collections.Generic;
using OpenCvSharp;

namespace RazorPagesMovie.core.model.elements
{
    public class Section : Element
    {
        public List<Container> Containers { get; set; }

        public Section(int id)
        {
            Id = id;
            Containers = new List<Container>();
        }

        public override string StartTag()
        {
            return $"<section style=\"height:{Height}px;background:rgb({BackgroundColor.Val0},{BackgroundColor.Val1},{BackgroundColor.Val2})\">";
        }

        public override string Body()
        {
            return "";
        }

        public override string EndTag()
        {
            return "</section>";
        }
    }
}
