using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using RazorPagesMovie.core.model.elements.basic;
using Tesseract;
using Image = System.Drawing.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

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
            Bitmap imgBmp = new Bitmap(@"./wwwroot/images/" + image);
            Pix threshold;
            int xFrom, xTo, yFrom, yTo;
            Random random = new Random();

            // 2. detect font family
            using (var page = _tessOnly.Process(img, PageSegMode.SingleBlock))
            {
                var regions = page.GetSegmentedRegions(PageIteratorLevel.Symbol);
                if (regions.Count == 0)
                {
                    return null;
                }

                threshold = page.GetThresholdedImage();
                xFrom = regions[0].X;
                xTo = regions[0].X + regions[0].Width;
                yFrom = regions[0].Y;
                yTo = regions[0].Y + regions[0].Height;

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

            // 5. detect font color
            // @todo skúsiť to riešiť bez ukladania temp.png ale nejak to prekonvertovať na image/bmp
            // @todo neviem či niečo z toho nenechať triede backgroundanalyser (premenovať na coloranalyser?)
            // save thresholded image
            threshold.Save("temp.png", Tesseract.ImageFormat.Png);
            var bmp = new Bitmap("temp.png");
            var foundX = 0;
            var foundY = 0;

            // find pixel with black color (text)
            var found = false;
            while (!found)
            {
                var x = random.Next(xFrom, xTo);
                var y = random.Next(yFrom, yTo);
                var color = bmp.GetPixel(x, y);
                if (color.R == 0 && color.G == 0 && color.B == 0)
                {
                    foundX = x;
                    foundY = y;
                    found = true;
                }
            }

            // get original color
            var px = imgBmp.GetPixel(foundX, foundY);
            var fontColor = new int[] { px.R, px.G, px.B };

            // 6. return new text instance
            return new Text(text, fontFamily, fontColor, fontSize, bold, italic);
        }

        private string NormalizeText(string text)
        {
            return text.Replace("\n", "");
        }

        private string NormalizeFontName(string name)
        {
            var map = new Dictionary<string, string>
            {
                { "Courier_New", "Courier New" },
                { "Verdana", "Verdana" },
                { "Arial", "Arial" }
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
