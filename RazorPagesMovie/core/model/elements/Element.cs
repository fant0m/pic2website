using System;
using OpenCvSharp;

namespace RazorPagesMovie.core.model.elements
{
    public abstract class Element : IWebElement
    {
        public int Id;
        public int[] Padding;
        public int[] Margin;
        public double Width;
        public int Height;
        public int[] Color;
        public string Class;
        public Border Border;
        // @todo toto určite nie Scalar, len normálne pole 4 double, alebo vlastná trieda
        public Scalar BackgroundColor;

        // @todo tu asi bude musieť byť list sub elementov

        protected Element()
        {
            Color = new int[3];
            Margin = new[] { 0, 0, 0, 0 };
            Padding = new[] { 0, 0, 0, 0 };
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
