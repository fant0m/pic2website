using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenCvSharp;
using RazorPagesMovie.core.model;
using RazorPagesMovie.core.model.elements;

namespace RazorPagesMovie.core
{
    public static class BackgroundAnalyser
    {
        // @todo metóda pre analýzu rectu

        public static void AnalyseSection(Section section, Rect[] rects, Mat image)
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
                var found = 0;

                var xFrom = section.Rect.X;
                var xTo = section.Rect.X + section.Rect.Width;
                var yFrom = section.Rect.Y;
                var yTo = section.Rect.Y + section.Rect.Height;

                while (found != num)
                {
                    // generate random coordinates
                    var x = random.Next(xFrom, xTo);
                    var y = random.Next(yFrom, yTo);

                    // check if coordinates don't collide with rects
                    var collides = false;
                    for (var i = 0; i < rects.Length; i++)
                    {
                        var rect = rects[i];
                        if (rect.Contains(new Rect(x, y, 1, 1)))
                        {
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
                }
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
    }
}
