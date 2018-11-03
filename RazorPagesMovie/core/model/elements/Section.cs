using System.Collections.Generic;

namespace RazorPagesMovie.core.model.elements
{
    public class Section : Element
    {
        public List<Container> Containers { get; }

        public Section(int id)
        {
            Id = id;
            Containers = new List<Container>();
        }

        public override string StartTag()
        {
            return "<section>";
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
