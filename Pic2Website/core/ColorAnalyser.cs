using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Pic2Website.core.helper;
using Pic2Website.core.model;
using Pic2Website.core.model.elements;
using Tesseract;
using Point = OpenCvSharp.Point;

namespace Pic2Website.core
{
    public class ColorAnalyser
    {
        private Mat _image;
        public ColorAnalyser(Mat image)
        {
            _image = image;
        }

        public int[] AnalyseRect(OpenCvSharp.Rect rect, int[] color)
        {
            if (rect.Width == 0 || rect.Height == 0)
            {
                return null;
            }

            //Debug.WriteLine("analysing" + rect.X + "," + rect.Y + "," + rect.Width + "," + rect.Height);
            
            // calculate middle coordinates
            var middleX = (int)Math.Round(rect.X + rect.Width / 2.0);
            var middleY = (int)Math.Round(rect.Y + rect.Height / 2.0);

            // check if element is properly erased (might have 1px margin)
            if (rect.Width > 2 && _image.At<Vec3b>(middleY, rect.Left) != _image.At<Vec3b>(middleY, rect.Left + 1))
            {
                rect.Width -= 1;
                rect.X += 1;
            }
            if (rect.Width > 2 && _image.At<Vec3b>(middleY, rect.Right) != _image.At<Vec3b>(middleY, rect.Right - 1))
            {
                rect.Width -= 1;
            }
            if (rect.Height > 2 && _image.At<Vec3b>(rect.Top, middleX) != _image.At<Vec3b>(rect.Top + 1, middleX))
            {
                rect.Height -= 1;
                rect.Y += 1;
            }
            if (rect.Height > 2 && _image.At<Vec3b>(rect.Bottom, middleX) != _image.At<Vec3b>(rect.Bottom - 1, middleX))
            {
                rect.Height -= 1;
            }

            var innerLeft = _image.At<Vec3b>(middleY, rect.X + 3);
            var innerRight = _image.At<Vec3b>(middleY, rect.X + rect.Width - 3);
            var innerTop = _image.At<Vec3b>(rect.Y + 3, middleX);
            var innerBottom = _image.At<Vec3b>(rect.Y + rect.Height - 3, middleX);
            var outerColor = _image.At<Vec3b>(rect.Y, rect.X - 2);

            // check if rect contains other color than in outer area
            if (outerColor != innerLeft || outerColor != innerRight || outerColor != innerTop || outerColor != innerBottom)
            {
                // calculate histogram
                var roi = new Mat(_image, rect);
                var split = roi.Split();
                var hist = new[] { new Mat(), new Mat(), new Mat() };

                for (var i = 0; i < 3; i++)
                {
                    Cv2.CalcHist(new[] { split[i] }, new[] { 0 }, null, hist[i], 1, new[] { 256 }, new[] { new Rangef(0, 256) });
                }

                // find most common value
                var mostCommon = new int[3];
                for (var j = 0; j < 3; j++)
                {
                    // detect min max values and it's positions
                    Cv2.MinMaxLoc(hist[j], out _, out var max, out _, out var maxLoc);
                    mostCommon[j] = maxLoc.Y;
                }

                // check if most common color is not outer color
                if (mostCommon[0] == outerColor.Item0 && mostCommon[1] == outerColor.Item1 && mostCommon[2] == outerColor.Item2)
                {
                    //return new int[] { outerColor.Item2, outerColor.Item1, outerColor.Item0 };
                    return null;
                }

                //Debug.WriteLine("its most common color" + mostCommon[2] + "," + mostCommon[1] + "," + mostCommon[0]);

                // we dont want to return color that is already at the background
                if (color != null && mostCommon[2] == color[0] && mostCommon[1] == color[1] && mostCommon[0] == color[2])
                {
                    return null;
                }

                return new[] { mostCommon[2], mostCommon[1], mostCommon[0] };
            }

            //Debug.WriteLine("its outer color");

            // we dont want to return color that is already at the background
            if (color != null && outerColor.Item2 == color[0] && outerColor.Item1 == color[1] && outerColor.Item0 == color[2])
            {
                return null;
            }

            return new int[] { outerColor.Item2, outerColor.Item1, outerColor.Item0 };
        }

        public static void AnalyseSectionBackground(Section section, OpenCvSharp.Rect[] rects, Mat image)
        {
            Vec3b[] colors;

            if (section.Layout.Type == Layout.LayoutType.Centered)
            {
                // we want to analyse pixels outside of the container

                var spaceWidth = (section.Rect.Width - (int)section.Layout.Width) / 2;
                if (spaceWidth < 0)
                {
                    spaceWidth = image.Width / 2;
                }
                var leftFrom = section.Rect.X;
                var leftTo = leftFrom + spaceWidth;
                var rightFrom = section.Rect.Width - spaceWidth;
                var rightTo = section.Rect.Width;
                var yFrom = section.Rect.Y;
                var yTo = section.Rect.Y + section.Rect.Height;

                var random = new Random();
                var num = 5;
                colors = new Vec3b[num * 2];
                // test random 5*2 pixels
                for (var i = 0; i < num; i++)
                {
                    // generate random coordinates
                    var x1 = random.Next(leftFrom, leftTo);
                    var x2 = random.Next(rightFrom, rightTo);
                    var y = random.Next(yFrom, yTo);

                    // check pixels
                    colors[i] = image.At<Vec3b>(y, x1);
                    colors[i + 5] = image.At<Vec3b>(y, x2);
                }
            }
            else
            {
                // we want to analyse all pixels except those saved in rects array
                var random = new Random();
                var num = 5;
                colors = new Vec3b[num];
                //var found = 0;

                var xFrom = section.Rect.X;
                var xTo = section.Rect.X + section.Rect.Width;
                var yFrom = section.Rect.Y;
                var yTo = section.Rect.Y + section.Rect.Height;

                colors[0] = image.At<Vec3b>(yFrom, xFrom);
                colors[3] = image.At<Vec3b>(yFrom, xTo - 1);
                colors[2] = image.At<Vec3b>(yTo - 1, xFrom);
                colors[1] = image.At<Vec3b>(yTo - 1, xTo - 1);
                /*while (found != num)
                {
                    // generate random coordinates
                    var x = random.Next(xFrom, xTo);
                    var y = random.Next(yFrom, yTo);

                    // check if coordinates don't collide with rects
                    var collides = false;
                    for (var i = 0; i < rects.Length; i++)
                    {
                        var rect = rects[i];
                        if (rect.Contains(new OpenCvSharp.Rect(x, y, 1, 1)))
                        {
                            Debug.WriteLine("collides " + rect.ToString() + "," + section.Rect.ToString());
                            collides = true;
                        }

                        if (collides) break;
                    }

                    // if doesn't collide check background color
                    if (!collides)
                    {
                        colors[found] = image.At<Vec3b>(y, x);
                        found++;
                    }
                }*/
            }

            var unique = colors.Distinct().Count();
            if (unique <= 2)
            {
                // background is just one color
                var color = colors.MostCommon();
                section.BackgroundColor = new int[] { color.Item2, color.Item1, color.Item0 };
            }
            else
            {
                // background seems to be more complicated (image)
                section.BackgroundImage = $"https://via.placeholder.com/{section.Rect.Width}x{section.Rect.Height}";
            }
        }

        public static int[] AnalyseTextColor(Rectangle region, Bitmap image, Pix threshold)
        {
            // load image
            Mat src = BitmapConverter.ToMat(image);

            // convert threshold to mat
            Bitmap bmp = PixToBitmapConverter.Convert(threshold);
            Mat mask = BitmapConverter.ToMat(bmp);

            // detect background color
            var bgColor = src.At<Vec3b>(0, 0);

            // filter text region
            // @todo probably remove it's destroying histrogram colors
            //var roi = new Mat(src, new Rect(region.X, region.Y, region.Width, region.Height));

            // split image into r,g,b parts
            var split = src.Split();

            // calculate histogram for each channel
            var hist = new[] { new Mat(), new Mat(), new Mat() };
            for (var i = 0; i < 3; i++)
            {
                Cv2.CalcHist(new [] { split[i] }, new[] { 0 }, mask, hist[i], 1, new[] { 256 }, new[] { new Rangef(0, 256) });
            }

            int[] color = null;

            var colors = 5;
            var maxLocs = new int[colors, 3];
            var maxValues = new double[colors, 3];
            var firstBg = false;

            for (var i = 0; i < colors; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    // detect min max values and it's positions
                    Cv2.MinMaxLoc(hist[j], out _, out var max, out _, out var maxLoc);

                    // save values
                    maxLocs[i, j] = maxLoc.Y;
                    maxValues[i, j] = max;

                    // reset max value
                    hist[j].Set(maxLoc.X, maxLoc.Y, 0);
                }

                if (i == 0)
                {
                    firstBg = maxLocs[0, 0] == bgColor.Item0 && maxLocs[0, 1] == bgColor.Item1 && maxLocs[0, 2] == bgColor.Item2;
                }
                else if (i == 1)
                {
                    int index = firstBg ? 1 : 0;

                    // we don't need to calculate other values if we have got clear peak
                    if (Math.Round(maxValues[index + 1, 0] / maxValues[index, 0], 2) <= 0.7 &&
                        Math.Round(maxValues[index + 1, 1] / maxValues[index, 1], 2) <= 0.7 &&
                        Math.Round(maxValues[index + 1, 2] / maxValues[index, 2], 2) <= 0.7)
                    {
                        //Debug.WriteLine("we got color since we have clear peak");
                        color = new[] { maxLocs[index, 2], maxLocs[index, 1], maxLocs[index, 0] };
                        break;
                    }

                }
            }

            // we need to find closest color to the mean
            if (color == null)
            {
                //Debug.WriteLine("we have got a problem.. colors are too close to each other in histogram");

                // we will skip the first color since it's background color
                var start = firstBg ? 1 : 0;
                var average = new[] { 0, 0, 0 };
                // sum r, g, b values
                for (var i = start; i < colors; i++)
                {
                    for (var j = 0; j < 3; j++)
                    {
                        //average[j] += maxLocs[i, j] * maxLocs[i, j];
                        average[j] += maxLocs[i, j];
                    }
                }

                // calculate average values
                for (var i = 0; i < 3; i++)
                {
                    //average[i] = (int)Math.Sqrt(average[i]);
                    average[i] /= colors;
                }

                // calculate distances from original colors to the average value
                var distances = new int[colors];
                for (var i = start; i < colors; i++)
                {
                    var b = average[0] - maxLocs[i, 0];
                    var g = average[1] - maxLocs[i, 1];
                    var r = average[2] - maxLocs[i, 2];
                    distances[i] = (int)Math.Sqrt(b * b + g * g + r * r);
                }

                // find the closest
                var closest = distances
                    .Select((x, i) => new KeyValuePair<int, int>(i, x))
                    .OrderBy(x => x.Value)
                    .First();
                var closestIndex = firstBg ? closest.Key + 1 : closest.Key;

                color = new[] { maxLocs[closestIndex, 2], maxLocs[closestIndex, 1], maxLocs[closestIndex, 0] };
            }

            return color;
        }
    }
}
