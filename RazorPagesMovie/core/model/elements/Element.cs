using System;
using OpenCvSharp;

namespace RazorPagesMovie.core.model.elements
{
    public abstract class Element : IWebElement
    {
        public int Id;
        public double Padding;
        public double Margin;
        public double Width;
        public int Height;
        public int[] Color;
        public string Class;
        public Border Border;
        public Scalar BackgroundColor;

        // @todo tu asi bude musieť byť list sub elementov

        protected Element()
        {
            Color = new int[3];
        }

        public string GetId()
        {
            return (GetType().Name + "-" + Id).ToLower();
        }

        public abstract string StartTag();

        public abstract string Content();
        public abstract string EndTag();
    }
}
