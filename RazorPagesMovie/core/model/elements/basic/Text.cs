namespace RazorPagesMovie.core.model.elements.basic
{
    public class Text : Element
    {
        private string _text;

        public Text(string text, string fontFamily, int[] fontColor, int fontSize, bool bold, bool italic)
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

        public string GetText()
        {
            return _text;
        }

        public override string StartTag()
        {
            return $"<div style=\"{GetStyles()}\">";
        }

        public override string Content()
        {
            return _text;
        }

        public override string EndTag()
        {
            return "</div>";
        }
    }
}
