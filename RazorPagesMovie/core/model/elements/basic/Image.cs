using System.Collections.Generic;

namespace RazorPagesMovie.core.model.elements.basic
{
    public class Image : Element
    {
        private string _path;

        public override string Tag { get; set; } = "img";
        public override bool PairTag { get; set; } = false;

        public Image(string path)
        {
            _path = path;

            Attributes.Add("src", _path);
        }

        public override List<Element> GetSubElements()
        {
            return null;
        }
    }
}
