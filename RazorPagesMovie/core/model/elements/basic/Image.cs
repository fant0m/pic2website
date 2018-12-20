namespace RazorPagesMovie.core.model.elements.basic
{
    public class Image : Element
    {
        private string _path;

        public Image(string path)
        {
            _path = path;
        }

        public override string StartTag()
        {
            return "<img ";
        }

        public override string Content()
        {
            return $"src=\"{_path}\" style=\"{GetStyles()}\"";
        }

        public override string EndTag()
        {
            return "/>";
        }
    }
}
