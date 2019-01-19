namespace RazorPagesMovie.core.model.elements.basic
{
    public class Text : Element
    {
        private readonly string _text;

        public Text(string text, string fontFamily, int fontSize, bool bold, bool italic)
        {
            _text = text;
            FontFamily = fontFamily;
            FontSize = fontSize;

            if (bold)
            {
                FontWeight = 700;
            }

            if (italic)
            {
                FontStyle = "italic";
            }
        }

        public override string StartTag()
        {
            return "";
        }

        public override string Content()
        {
            return _text;
        }

        public override string EndTag()
        {
            return "";
        }
    }
}
