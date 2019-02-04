using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using RazorPagesMovie.core.model.elements.basic;
using Tesseract;
using RazorPagesMovie.core.helper;
using ImageFormat = Tesseract.ImageFormat;

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
            string text;
            string fontFamily;
            bool bold;
            bool italic;
            Pix img = Pix.LoadFromFile(@"./wwwroot/images/" + image);
            Bitmap imgBmp = new Bitmap(@"./wwwroot/images/" + image);
            Pix threshold;
            int xFrom, xTo, yFrom, yTo;
            Random random = new Random();

            // 2. detect font family
            var mode = PageSegMode.SingleLine;
            using (var page = _tessOnly.Process(img, mode))
            {
                var regions = page.GetSegmentedRegions(PageIteratorLevel.Symbol);
                if (regions.Count == 0)
                {
                    return null;
                }

                threshold = page.GetThresholdedImage();
                threshold.Save("thr.bmp", ImageFormat.Bmp);
                xFrom = regions[0].X;
                xTo = regions[0].X + regions[0].Width;
                yFrom = regions[0].Y;
                yTo = regions[0].Y + regions[0].Height;

                var iterator = page.GetIterator();
                var attr = iterator?.GetWordFontAttributes();
                if (attr != null)
                {
                    Debug.WriteLine(attr.FontInfo);
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
            using (var page = _tessLstm.Process(img, mode))
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
            var fontSize = PointsToPixels(size);

            // 5. detect font color
            // @todo neviem či niečo z toho nenechať triede backgroundanalyser (premenovať na coloranalyser?)
            var bmp = PixToBitmapConverter.Convert(threshold);

            // find pixel with black color (text)
            // @todo doesn't work well
            var found = new Color[20];
            var i = 0;
            while (i < 20)
            {
                var x = random.Next(xFrom, xTo);
                var y = random.Next(yFrom, yTo);
                var color = bmp.GetPixel(x, y);
                // should be R,G,B == 0, not sure why 255 acts as 0
                if (color.R == 255 && color.G == 255 && color.B == 255)
                {
                    // get original color
                    found[i] = imgBmp.GetPixel(x, y);
                    i++;
                }
            }
            // filter most common
            var mostCommon = found.MostCommon();

            // fill color variable
            var fontColor = new int[] { mostCommon.R, mostCommon.G, mostCommon.B };

            // 6. return new text instance
            return new Text(text, fontFamily, fontColor, fontSize, bold, italic);
        }

        private string NormalizeText(string text)
        {
            return text.Replace("\n", "");
        }

        private string NormalizeFontName(string name)
        {
            // @todo load https://github.com/tesseract-ocr/langdata/blob/master/font_properties
            Debug.WriteLine(name);

            var map = new Dictionary<string, string>
            {
                { "Courier_New", "Courier New" },
                { "Verdana", "Verdana" },
                { "Arial", "Arial" },
                { "DejaVu_Sans_Ultra-Light", "Dejavu Sans" },
                { "Times_New_Roman", "Times New Roman" },
                { "Trebuchet_MS", "Trebuchet MS" }
            };

            if (map.ContainsKey(name))
            {
                return map[name];
            }

            var cut = name.Split("_")[0];
            if (map.ContainsKey(cut))
            {
                return map[cut];
            }

            return "";
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
