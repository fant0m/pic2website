namespace RazorPagesMovie.core.model.elements.basic
{
    public class Text : Element
    {
        private readonly string _text;

        public Text(string text)
        {
            _text = text;
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
