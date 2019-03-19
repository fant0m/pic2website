using System.Collections.Generic;

namespace Pic2Website.core.model.elements.basic
{
    public class Text : Element
    {
        private string[] _text;
        public override string Tag { get; set; } = "p";
        public override bool PairTag { get; set; } = true;

        public Text(string[] text, string fontFamily, int[] fontColor, int fontSize, bool bold, bool italic)
        {
            _text = text;
            FontFamily = fontFamily;
            FontSize = fontSize;
            Color = fontColor;

            if (bold)
            {
                FontWeight = 700;
            }

            if (italic)
            {
                FontStyle = "italic";
            }
        }

        public string[] GetText()
        {
            return _text;
        }

        public override List<Element> GetSubElements()
        {
            return null;
        }
    }
}
