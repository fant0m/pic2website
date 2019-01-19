using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using RazorPagesMovie.core.model.elements.basic;
using Tesseract;

namespace RazorPagesMovie.core
{
    public class Ocr
    {
        private TesseractEngine _tessOnly;
        private TesseractEngine _tessLstm;

        public Ocr()
        {
            _tessOnly = new TesseractEngine(@"./wwwroot/tessdata", "eng", EngineMode.TesseractOnly);
            _tessLstm = new TesseractEngine(@"./wwwroot/tessdata", "eng", EngineMode.LstmOnly);
        }

        public Text GetText(string image)
        {
            // 1. Init font variables
            string text = "";
            string fontFamily = "";
            int fontSize = 0;
            bool bold = false;
            bool italic = false;
            Pix img = Pix.LoadFromFile(@"./wwwroot/images/" + image);

            // 2. detect font family
            using (var page = _tessOnly.Process(img, PageSegMode.SingleBlock))
            {
                var iterator = page.GetIterator();
                var attr = iterator?.GetWordFontAttributes();
                if (attr != null)
                {
                    fontFamily = NormalizeFontName(attr.FontInfo.Name);
                    bold = attr.FontInfo.IsBold;
                    italic = attr.FontInfo.IsItalic;
                }
                else
                {
                    return null;
                }
            }

            // 3. detect text
            using (var page = _tessOnly.Process(img, PageSegMode.SingleBlock))
            {
                text = NormalizeText(page.GetText());
                if (text.Equals(""))
                {
                    return null;
                }
            }

            // 4. detect font size
            // @todo maxWidth možno podľa iterator bounds, možno zapojiť aj tú height
            var size = DetectFontSize(img.Width, img.Height, fontFamily, text);
            fontSize = PointsToPixels(size);

            // @todo 5. font color´-> statický call na backgroundanalyser s konkrétnymi pixelom zisteným z bounds?

            // 6. return new text instance
            return new Text(text, fontFamily, fontSize, bold, italic);
        }

        private string NormalizeText(string text)
        {
            return text.Replace("\n", "");
        }

        private string NormalizeFontName(string name)
        {
            var map = new Dictionary<string, string>()
            {
                { "Courier_New", "Courier New"}
            };

            return map[name];
        }

        private int DetectFontSize(int maxWidth, int maxHeight, string fontFamily, string text)
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

        private int PointsToPixels(double points)
        {
            return (int) Math.Ceiling(points * (96.0 / 72.0));
        }
    }
}
