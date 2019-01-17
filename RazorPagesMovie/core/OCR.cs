using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesMovie.core
{
    public static class OCR
    {
        // @todo remove static keyword
        // @todo add tesseract stuff
        // @todo ako sa bude riešiť či je text italic, bold
        // @todo detect font family

        public static double DetectFontSize(int maxWidth, int maxHeight, string fontFamily, string text)
        {
            var best = 1;
            for (var i = 1; i < 100; i++)
            {
                var font = new Font(fontFamily, i);
                var fakeImage = new Bitmap(1, 1);
                var graphics = Graphics.FromImage(fakeImage);
                graphics.PageUnit = GraphicsUnit.Pixel;
                var size = graphics.MeasureString(text, font, int.MaxValue, StringFormat.GenericTypographic);

                //if (size.Width > maxWidth || size.Height > maxHeight)
                if (size.Width > maxWidth)
                {
                    best = i - 1;
                    break;
                }
            }

            return best;
        }

        public static int PointsToPixels(double points)
        {
            return (int) Math.Ceiling(points * (96.0 / 72.0));
        }
    }
}
