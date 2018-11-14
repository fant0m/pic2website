using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenCvSharp;
using RazorPagesMovie.core.model;
using RazorPagesMovie.core.model.elements;
using RazorPagesMovie.core.model.elements.basic;
using Tesseract;
using Rect = OpenCvSharp.Rect;

namespace RazorPagesMovie.core
{
    public class Triple<T, X, Y>
    {
        public T Item1 { get; set; }
        public X Item2 { get; set; }
        public Y Item3 { get; set; }
    }

    public class TemplateParser
    {
        private string _imagePath;
        private Mat _image;
        private double _mostLeft;
        private double _mostRight;
        private Point[][] _contours;
        private HierarchyIndex[] _hierarchy;
        private TemplateStructure _templateStructure;

        public const int MaxSeparatorHeight = 10;
        public const int MinSeparatorWidth = 400;

        public TemplateParser(string imagePath)
        {
            _imagePath = imagePath;
        }

        public string Analyse()
        {
            var tess = new TesseractEngine(@"./wwwroot/tessdata", "eng", EngineMode.LstmOnly);

            byte[] imageData = File.ReadAllBytes(@"./wwwroot/images/test5.png");
            _image = Mat.FromImageData(imageData, ImreadModes.Color);
            //Convert the img1 to grayscale and then filter out the noise
            Mat gray1 = Mat.FromImageData(imageData, ImreadModes.GrayScale);
            // @todo naozaj to chceme blurovať? robí to len bordel a zbytočné contours
            //gray1 = gray1.GaussianBlur(new OpenCvSharp.Size(3, 3), 0);

            //Canny Edge Detector
            Mat cannyGray = gray1.Canny(15, 25); // 0, 12, blur 9; 2, 17,  blur 7; 0, 25 blur 13; 20 35 blur 0; 15, 25 blur 3

            Random r = new Random();
            int lastY = 0;

            Cv2.FindContours(cannyGray, out _contours, out _hierarchy, mode: RetrievalModes.Tree, method: ContourApproximationModes.ApproxSimple);

            var layout = DetectLayout(cannyGray.Width, cannyGray.Height);
            _templateStructure = new TemplateStructure(layout);

            //var gray2 = Mat.FromImageData(imageData, ImreadModes.GrayScale);
            //gray2 = gray2.AdaptiveThreshold(255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 105, 2);
            //var reduced = gray2.Reduce(ReduceDimension.Row, ReduceTypes.Avg, 1);
            //reduced.SaveImage("wwwroot/images/output.png");
            //Debug.WriteLine("rows " + reduced.Rows + "," + reduced.Cols + "," + gray1.Rows);

            var draw = AnalyzeSections();

            Mat copy = _image.Clone();
            Cv2.DrawContours(copy, _contours, -1, Scalar.Orange);

            //var j = 0;
            //var count = 0;
            //while (j != -1)
            //{
            //    var index = _hierarchy[j];
            //    if (index.Parent != -1)
            //    {
            //        j = index.Next;
            //        continue;
            //    }

            //    Scalar scalar = Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
            //    Cv2.DrawContours(copy, _contours, j, scalar);
            //    count++;

            //    j = index.Next;
            //}
            //Debug.WriteLine("Počet" + count);

            /*for (var j = contours.Length - 1; j >= 0; j--)
            {
                Scalar scalar = Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                Cv2.DrawContours(copy, contours, j, scalar);
            }*/

            //copy.SaveImage("wwwroot/images/output.png");


            Debug.WriteLine("počet " + _contours.Length);

            Debug.WriteLine(_hierarchy.Length);

            //var limit = 0;
            //var i = 0;
            //while (i != -1)
            //{
            //    // Find current item
            //    var item = _hierarchy[i];

            //    // Filter only outer contours
            //    if (item.Parent != -1)
            //    {
            //        i = item.Next;
            //        continue;
            //    }

            //    limit++;

            //    // Edges
            //    var edges = _contours[i];

            //    // Bounding box
            //    var rect = Cv2.BoundingRect(edges);

            //    // Area
            //    var area = Cv2.ContourArea(edges, false);

            //    // Polygon Approximations
            var contoursAp = Cv2.ApproxPolyDP(edges, Cv2.ArcLength(edges, true) * 0.05, true);

            //    if (draw == i)
            //    { 
            //        var roi = _image.Clone(rect);
            //        roi.SaveImage("image-" + i + ".png");
            //    }


            //    Debug.WriteLine(contoursAp.Length);
            //    Debug.WriteLine("left: " + rect.Left + ", top: " + rect.Top);

            //    // Process only outer structure
            //    if (limit == 9999999)
            //    {
            //        i = -1;
            //    }
            //    else
            //    {
            //        i = item.Next;
            //    }
            //}


            var output = Convertor.Convert(_templateStructure);
            var fileOutpout = output.Replace("src=\".", "src=\"C:/Users/tomsh/source/repos/RazorPagesMovie/RazorPagesMovie/wwwroot");

            using (var tw = new StreamWriter("test.html"))
            {
                tw.Write(fileOutpout);
                tw.Close();
            }

            return output;
        }

        private int AnalyzeSections()
        {
            var r = new Random();
            // Find index of the first top to bottom contour
            var startIndex = -1;
            var i = 0;
            while (i != -1)
            {
                // Find current item
                var item = _hierarchy[i];

                // Filter only outer contours
                if (item.Parent != -1 && item.Next != -1)
                {
                    i = item.Next;
                    continue;
                }

                if (item.Next == -1)
                {
                    startIndex = i;
                }

                i = item.Next;
            }

            // Section ids counter
            var sectionId = 0;
            var lastSectionY = 0;
            var sections = new List<Section>();
            var sectionContours = new Dictionary<int, List<int>>();
            var currentSectionContours = new List<int>();

            var rects = new List<Rect>();

            // @todo filter noise

            // Find sections
            i = startIndex;
            Container container;
            Image image;
            Rect area;
            Mat roi;
            while (i != -1)
            {
                // Find current item
                var item = _hierarchy[i];

                // Filter only outer contours
                if (item.Parent != -1)
                {
                    i = item.Previous;
                    continue;
                }

                // Edges
                var edges = _contours[i];

                // Bounding box
                var rect = Cv2.BoundingRect(edges);
                rects.Add(rect);

                if (IsRectSeparator(rect))
                {
                    // @todo separator height, color - bude to ako border-bottom/top/ ďalší element asi
                    var height = rect.Height;
                    var color = "";

                    // Init a new section
                    sectionId++;
                    var section = new Section(sectionId);
                    section.Height = rect.Y - lastSectionY;
                    section.BackgroundColor = Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                    container = new Container(sectionId);
                    image = new Image("./images/section-" + sectionId + ".png");
                    container.Elements.Add(image);
                    section.Containers.Add(container);
                    sections.Add(section);

                    // Save section image
                    area = new Rect(0, lastSectionY, _image.Width, section.Height);
                    roi = _image.Clone(area);
                    roi.SaveImage("./wwwroot/images/section-" + sectionId + ".png");

                    lastSectionY = rect.Y;
                    sectionContours.Add(sectionId, currentSectionContours);

                    // Reset contours list
                    currentSectionContours = new List<int>();
                }
                else
                {
                    currentSectionContours.Add(i);
                }

                // Continue to the next item
                i = item.Previous;
            }

            // Finalize last section
            sectionId++;
            var lastSection = new Section(sectionId);
            lastSection.Height = _image.Height - lastSectionY;
            lastSection.BackgroundColor = Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
            container = new Container(sectionId);
            image = new Image("./images/section-" + sectionId + ".png");
            container.Elements.Add(image);
            lastSection.Containers.Add(container);

            // Save section image
            area = new Rect(0, lastSectionY, _image.Width, lastSection.Height);
            roi = _image.Clone(area);
            roi.SaveImage("./wwwroot/images/section-" + sectionId + ".png");
            sections.Add(lastSection);
            sectionContours.Add(sectionId, currentSectionContours);

            var copy = _image.Clone();

            // Process sections
            var lastY = 0;
            foreach (var section in sections)
            {
                // @todo process sectionContours
                // @todo nebudem tam asi ukladať ID ale Rect
                // @todo zoradiť sort left to right, prípadne predtým pospájať ktoré sú moc blízko (text)
                // @todo najhoršie riešenie môže byť rozdeliť to ručne do riadkov - zoradiť podľa lavej súradnice a pri vytváraní riadku vždy definovať odkiaľ pokiaľ y je daný riadok, ak príde nový element a nezmestí sa tam s jeho Y + height tak ho dať do ďalšieho riadku

                // save sections rects into list
                var contours = sectionContours[section.Id];
                var sectionRects = new Rect[contours.Count];
                var k = 0;
                foreach (var contour in sectionContours[section.Id])
                {
                    // Edges
                    var edges = _contours[contour];

                    // Bounding box
                    var rect = Cv2.BoundingRect(edges);
                    sectionRects[k] = rect;
                    k++;
                }


                // @todo skontrolovať najskôr/alebo potom či tam náhodou nie je column layout a až potom robiť toto/preorganizovať to potom
                // @todo spojenie riadkov ak sú moc blízko
                Debug.WriteLine("sekcia počet rectov " + sectionRects.Length);

                // analyse section rows
                var rows = new List<Triple<int, int, List<Rect>>>(); // y start y end
                foreach (var rect in sectionRects)
                {
                    var rowIndex = findRowForRect(rows, rect);
                    if (rowIndex == -1)
                    {
                        var triple = new Triple<int, int, List<Rect>>
                        {
                            Item1 = rect.Y,
                            Item2 = rect.Y + rect.Height,
                            Item3 = new List<Rect> {rect}
                        };
                        rows.Add(triple);
                    }
                    else
                    {
                        rows[rowIndex].Item3.Add(rect);
                    }
                }

                // proceed rows
                var limit = 0;
                foreach (var row in rows)
                {
                    // align contours inside row from left to right
                    var alignVertical = row.Item3.OrderBy(rect => rect.X).ToArray();
                    var connectedVertical = new List<Rect>();

                    foreach (var contour in alignVertical)
                    {
                        Cv2.Rectangle(copy, new Point(contour.X, contour.Y), new Point(contour.X + contour.Width, contour.Y + contour.Height), Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255)));
                    }

                    // connect letters into words
                    var maxTextGap = 10;
                    for (var j = 0; j < alignVertical.Length - 1; j++)
                    {
                        var currentRect = alignVertical[j];
                        var nextRect = alignVertical[j + 1];
                        var firstGap = Math.Abs(nextRect.X - (currentRect.X + currentRect.Width));
                        var distance = Distance_BtwnPoints(new Point(currentRect.X + currentRect.Width, currentRect.Y + currentRect.Height), new Point(nextRect.X, nextRect.Y + nextRect.Height));
                        var merge = currentRect;

                        //while ((distance <= maxTextGap || (currentRect & nextRect).area() > 0) && j + 1 < alignVertical.Length - 1)
                        while ((firstGap <= maxTextGap || merge.IntersectsWith(nextRect)) && j + 1 < alignVertical.Length - 1)
                        {
                            merge = merge | nextRect;
 
                            j++;

                            // @todo ak tam má intersect tak porovnávať distance aj s tým predtým nakoľko môže mať x pozíciu menšiu ako to predtým

                            currentRect = alignVertical[j];
                            nextRect = alignVertical[j + 1];
                            firstGap = Math.Abs(nextRect.X - (currentRect.X + currentRect.Width));
                            distance = Distance_BtwnPoints(new Point(currentRect.X + currentRect.Width, currentRect.Y + currentRect.Height), new Point(nextRect.X, nextRect.Y + nextRect.Height));
                        }

                        connectedVertical.Add(merge);

                        Debug.WriteLine("p. " + j + " distance " + distance, " gap " + firstGap);
                    }


                    foreach (var rect in connectedVertical)
                    {
                        limit++;
                        if (limit == 100) break;

                        var roi2 = _image.Clone(rect);
                        roi2.SaveImage("image-" + limit + ".png");
                    }
                }

                Debug.WriteLine("sekcia počet row " + rows.Count);

                //foreach (var row in rows)
                //{
                //    Cv2.Rectangle(copy, new Point(0, row.Item1), new Point(copy.Width, row.Item2), Scalar.Orange);
                //}

                Cv2.Rectangle(copy, new Point(0, lastY), new Point(copy.Width, lastY+section.Height), Scalar.Red);
                lastY += section.Height;

                //sectionRects = sectionRects.OrderBy(rect => rect.Left).ToArray();
                //var limit = 0;
                //foreach (var rect in sectionRects)
                //{
                //    limit++;
                //    if (limit == 10) break;

                //    var roi2 = _image.Clone(rect);
                //    roi2.SaveImage("image-" + limit + ".png");
                //}


                _templateStructure.Sections.Add(section);
            }

            copy.SaveImage("wwwroot/images/output.png");

            return startIndex;
        }

        private double Distance_BtwnPoints(Point p, Point q)
        {
            int X_Diff = p.X - q.X;
            int Y_Diff = p.Y - q.Y;
            return Math.Sqrt((X_Diff * X_Diff) + (Y_Diff * Y_Diff));
        }

        private int findRowForRect(List<Triple<int, int, List<Rect>>> rows, Rect rect)
        {
            int index = -1;

            int i = 0;
            foreach (var row in rows)
            {
                // rect fits exactly into row
                if (rect.Y >= row.Item1 && rect.Y + rect.Height <= row.Item2)
                {
                    return i;
                }
                // end of the rect doesnt fit
                if (rect.Y <= row.Item2 && rect.Y + rect.Height > row.Item2)
                {
                    row.Item2 += rect.Y + rect.Height - row.Item2;
                    return i;
                }

                i++;
            }

            return index;
        }

        private bool IsRectSeparator(Rect rect)
        {
            // @todo prvá časť podmienky rect.X <= _mostLef || teoreticky na rect.X == 0
            return rect.Left == 0 && rect.Height <= MaxSeparatorHeight && rect.Width > MinSeparatorWidth;
        }

        /**
        * Detect type of template design and it's dimensions
        * 
        * @todo lepší algoritmus na hľadanie oboch súradníc
        * @todo teoreticky skúsiť brať úplne prvý element, alebo skôr hodnota ktorá predstavuje min./max. ohraničenie (ľavý/pravý) pre 90% všetkých hodnôt
        */
        private Layout DetectLayout(double width, double height)
        {
            var left = new List<double>();
            var right = new List<double>();

            var i = 0;
            while (i != -1)
            {
                // find current item
                var index = _hierarchy[i];
                var item = _contours[i];

                // find edges & boundix box
                var edges = _contours[i];
                var rect = Cv2.BoundingRect(edges);
                var area = Cv2.ContourArea(edges, false);

                // add left corners from 25 % left-most of image
                if (rect.Left < width * 0.25 && area > 100)
                {
                    left.Add(rect.Left);
                }

                // add right corners from 20% right-most of image
                if (rect.Right > width * 0.7 && rect.Left != 0 && area > 100)
                {
                    Debug.WriteLine("area " + area + " right " + rect.Right + " width " + rect.Width);
                    right.Add(rect.Right);
                }

                // Process only outer contours
                i = index.Next;
            }

            // filter top 50 % and select most common
            var filterLeft = left.Count > 1 ? left.OrderBy(j => j).Take(left.Count * 50 / 100) : left;
            _mostLeft = left.Count > 0 ? filterLeft.MostCommon() : 0;

            // filter top 50 % and sselect most common
            var filterRight = right.Count > 1 ? right.OrderByDescending(j => j).Take(right.Count * 50 / 100) : right;
            _mostRight = right.Count > 0 ? filterRight.MostCommon() : width;

            Debug.WriteLine("most left: " + _mostLeft);
            Debug.WriteLine("most right: " + _mostRight);

            var type = _mostLeft < 10 ? Layout.LayoutType.Fluid : Layout.LayoutType.Centered;
            return new Layout(type, _mostRight - _mostLeft, height);
        }
    }
}
