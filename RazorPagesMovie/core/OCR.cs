﻿using System;
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
            Rectangle region;
            
            // 2. detect font family
            var mode = PageSegMode.SingleLine;
            using (var page = _tessOnly.Process(img, mode))
            {
                var regions = page.GetSegmentedRegions(PageIteratorLevel.Symbol);
                if (regions.Count == 0)
                {
                    return null;
                }
                region = regions[0];

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
            // @todo tá font size nie je dobrá, bol tam všade tiež dáva asi by som to vypol globálne
            var size = DetectFontSize(bold, img.Width, img.Height, fontFamily, text);
            var fontSize = PointsToPixels(size);

            // 5. detect font color
            var fontColor = ColorAnalyser.AnalyseTextColor(region, image);

            // 6. return new text instance
            return new Text(text, fontFamily, fontColor, fontSize, bold, italic);
        }

        public static IEnumerable<Color> GetPixels(Bitmap bitmap)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    yield return pixel;
                }
            }
        }

        private string NormalizeText(string text)
        {
            return text.Replace("\n", "");
        }

        private string NormalizeFontName(string name)
        {
            // @todo load https://github.com/tesseract-ocr/langdata/blob/master/font_properties

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

            // remove last key after _ e.g. Arial_Bold => Arial, Trebuchet_MS_Bold => Trebuchet_MS
            var split = name.Split("_");
            Array.Resize(ref split, split.Length - 1);
            var cut = String.Join("_", split);
            if (map.ContainsKey(cut))
            {
                return map[cut];
            }

            return "";
        }

        private int DetectFontSize(bool bold, int maxWidth, int maxHeight, string fontFamily, string text)
        {
            var best = 1;
            for (var i = 1; i < 100; i++)
            {
                var font = new Font(fontFamily, i, bold ? FontStyle.Bold : FontStyle.Regular);
                var fakeImage = new Bitmap(1, 1);
                var graphics = Graphics.FromImage(fakeImage);
                graphics.PageUnit = GraphicsUnit.Pixel;
                var size = graphics.MeasureString(text, font, int.MaxValue, StringFormat.GenericDefault);

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
            return (int) Math.Round(points * (96.0 / 72.0));
        }
    }
}
