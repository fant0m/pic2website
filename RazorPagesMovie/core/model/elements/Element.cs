using System;

namespace RazorPagesMovie.core.model.elements
{
    public abstract class Element
    {
        protected int Id;
        protected double Padding;
        protected double Margin;
        protected double Width;
        protected double Height;
        protected int[] Color;
        protected string Class;

        // @todo tu asi bude musieť byť list sub elementov

        protected Element()
        {
            Color = new int[3];
        }

        public string GetId()
        {
            return (GetType().Name + "-" + Id).ToLower();
        }

        public abstract String StartTag();
        public abstract String Body();
        public abstract String EndTag();
    }
}
