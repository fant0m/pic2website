using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using OpenCvSharp;
using RazorPagesMovie.core.convertor;
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
        private TesseractEngine _tess;

        public const int MaxSeparatorHeight = 10;
        public const int MinSeparatorWidth = 400;
        public const int MaxTextGap = 6;
        public const int MínColumnGap = 20;

        public TemplateParser(string imagePath)
        {
            _imagePath = imagePath;
        }

        public string Analyse()
        {
            _tess = new TesseractEngine(@"./wwwroot/tessdata", "eng", EngineMode.LstmOnly);

            byte[] imageData = File.ReadAllBytes(@"./wwwroot/images/works12.png");
            _image = Mat.FromImageData(imageData, ImreadModes.Color);
            //Convert the img1 to grayscale and then filter out the noise
            Mat gray1 = Mat.FromImageData(imageData, ImreadModes.GrayScale);
            // @todo naozaj to chceme blurovať? robí to len bordel a zbytočné contours
            gray1 = gray1.GaussianBlur(new OpenCvSharp.Size(3, 3), 0);

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
            //var contoursAp = Cv2.ApproxPolyDP(edges, Cv2.ArcLength(edges, true) * 0.05, true);

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

            var convertor = new WebConvertor();
            var output = convertor.Convert(_templateStructure);
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

            Debug.WriteLine("start index " + startIndex);

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
                    //container = new Container(sectionId);
                    //image = new Image("./images/section-" + sectionId + ".png");
                    //container.Elements.Add(image);
                    //section.Containers.Add(container);
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
            //container = new Container(sectionId);
            //image = new Image("./images/section-" + sectionId + ".png");
            //container.Elements.Add(image);
            //lastSection.Containers.Add(container);

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
                // save sections rects into list
                var contours = sectionContours[section.Id];

                foreach (var contour in contours)
                {
                    TestSubBlocks(contour, copy);
                }

                section.Containers = ProcessInnerBlocks(contours, copy);

                Cv2.Rectangle(copy, new Point(0, lastY), new Point(copy.Width, lastY + section.Height), Scalar.Red);
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

        private void TestSubBlocks(int contour, Mat copy)
        {
            // Find current item
            var item = _hierarchy[contour];

            // Edges
            var edges = _contours[contour];

            // Bounding box
            var rect = Cv2.BoundingRect(edges);

            // Looking for next level inner elements
            if (item.Child != -1 && HasContourSubElements(edges))
            {
                var inner = new List<int>();
                var find = item.Child;
                while (find != -1)
                {
                    var subitem = _hierarchy[find];
                    if (subitem.Parent == contour)
                    {
                        inner.Add(find);
                        if (subitem.Child != -1)
                        {
                            TestSubBlocks(find, copy);
                            //testSubBlocks(subitem.Child, copy);
                        }
                    }
                    else
                    {
                        break;
                    }

                    find = subitem.Next;
                }

                Debug.WriteLine("počet sub " + inner.Count);

                // We have found inner elements
                if (inner.Count > 0)
                {
                    ProcessInnerBlocks(inner, copy, rect);
                }
            }
        }

        /**
         * Check if contour has rectangle shape and required dimensions
         */
        private bool HasContourSubElements(Point[] edges)
        {
            var contoursAp = Cv2.ApproxPolyDP(edges, Cv2.ArcLength(edges, true) * 0.02, true);
            var rect = Cv2.BoundingRect(edges);

            // @todo možno bude treba inú podmienku ako length = 4, niečo viac sotisfikované čo sa pozrie či to má body len ako obdĺžnik alebo aj niečo vo vnútri
            //if (contoursAp.Length == 4 && rect.Width >= 10 && rect.Height >= 10)
            //{
            //    var roi2 = _image.Clone(rect);
            //    roi2.SaveImage("sub-" + DateTime.Now.Ticks + ".png");
            //}

            return contoursAp.Length == 4 && rect.Width >= 10 && rect.Height >= 10;
        }

        private List<Container> ProcessInnerBlocks(List<int> contours, Mat copy, Rect parent = new Rect())
        {
            var r = new Random();
            var containers = new List<Container>();
            var sectionRects = new Rect[contours.Count];
            var k = 0;
            foreach (var contour in contours)
            {
                Debug.WriteLine("filling " + contour);
                // Edges
                var edges = _contours[contour];

                // Bounding box
                var rect = Cv2.BoundingRect(edges);
                sectionRects[k] = rect;
                k++;
            }


            // @todo spojenie riadkov ak sú moc blízko
            // @todo spájanie do zvlášť containera ak majú rovnaký štýl - rovnaká výška, medzery, ..
            Debug.WriteLine("sekcia počet rectov " + sectionRects.Length);

            // align rects from the top to the bottom
            sectionRects = sectionRects.OrderBy(rec => rec.Top).ToArray();

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
                        Item3 = new List<Rect> { rect }
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
            var c = 1;
            foreach (var row in rows)
            {
                var container = new Container(c);

                // @todo treba spracovať každý riadok do stĺpcov, t.j. taký istý princíp ako riadky - zoradia sa zľava doprava (čo už vlastne je vyššie):
                // @todo zoberiem prvý element, zistím aká je medzera medzi ďalším a poďalším (za podmienky že existuje poďalší), ak sú medzery cca rovnaká (rátam s nejakou odchýlkou) tak ich pridám to 1 stĺpca a prejdem na ďalší
                // @todo potom v sekcii ešte pozriem riadky a zistím či nemajú rovnaké stĺpce (prípadne s odchylkou) a ak hej tak spojím tie riadky
                // @todo riadok teda bude obsahovať zoznam stĺpcov a každý stĺpec bude obsahovať zoznam riadkov, t.j. na každý stĺpec potom znova aplikujem algoritmus delenia na riadky už ale bez stĺpcovania (asi či?)

                /* Columns start */

                // align rects from the left to the right
                var alignedRects = row.Item3.OrderBy(rec => rec.Left).ToArray();

                // analyse section columns
                var columns = new List<Triple<int, int, List<Rect>>>(); // x start, x end, list of items
                foreach (var rect in alignedRects)
                {
                    var columnIndex = findColumnForRect(columns, rect);
                    if (columnIndex == -1)
                    {
                        var triple = new Triple<int, int, List<Rect>>
                        {
                            Item1 = rect.X,
                            Item2 = rect.X + rect.Width,
                            Item3 = new List<Rect> { rect }
                        };
                        columns.Add(triple);
                    }
                    else
                    {
                        columns[columnIndex].Item3.Add(rect);
                    }
                }

                // draw columns
                foreach (var column in columns)
                {
                    Cv2.Rectangle(copy, new Point(column.Item1, row.Item1), new Point(column.Item2, row.Item2), Scalar.GreenYellow);
                }

                /* Columns end */
                
                // process columns into rows
                foreach (var column in columns)
                {
                    // @todo refactor rows, columns do metód

                    /* Column rows start */

                    // align rects from the top to the bottom
                    var alignedColumnRects = column.Item3.OrderBy(rec => rec.Top).ToArray();

                    // detect column rows
                    var columnRows = new List<Triple<int, int, List<Rect>>>(); // y start y end
                    foreach (var rect in alignedColumnRects)
                    {
                        var rowIndex = findRowForRect(columnRows, rect);
                        if (rowIndex == -1)
                        {
                            var triple = new Triple<int, int, List<Rect>>
                            {
                                Item1 = rect.Y,
                                Item2 = rect.Y + rect.Height,
                                Item3 = new List<Rect> { rect }
                            };
                            columnRows.Add(triple);
                        }
                        else
                        {
                            columnRows[rowIndex].Item3.Add(rect);
                        }
                    }

                    // draw column rows
                    foreach (var columnRow in columnRows)
                    {
                        Cv2.Rectangle(copy, new Point(column.Item1, columnRow.Item1), new Point(column.Item2, columnRow.Item2), Scalar.DarkOrange);
                    }

                    /* Column rows end */


                    /* Column row letters merging start */
                    // process column rows
                    foreach (var columnRow in columnRows)
                    {
                        // align contours inside row from left to right
                        // @todo neviem či filtrovať aj tie rozmery nakoľko môžeme prísť o znaky v text ako bodka
                        //var alignHorizontal = row.Item3.Where(rect => rect.Width * rect.Height >= 13).OrderBy(rect => rect.X).ToArray();
                        var alignHorizontal = columnRow.Item3.Where(rect => rect.Width * rect.Height >= 5).OrderBy(rect => rect.X).ToArray();
                        var connectedHorizontal = new List<Rect>();

                        var l = 0;
                        foreach (var contour in alignHorizontal)
                        {
                            //var roi2 = _image.Clone(contour);
                            //roi2.SaveImage("image2-" + l + ".png");
                            //l++;
                            //Cv2.Rectangle(copy, new Point(contour.X, contour.Y), new Point(contour.X + contour.Width, contour.Y + contour.Height), Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255)));
                        }

                        // connect letters into words
                        // @todo tú medzeru medzi textom asi bude treba riešiť tak že sa zistí typ fontu, veľkosť a zistí sa koľko by mala mať px medzera
                        // @todo asi bude aj tak problém zisťovať napr. či nie je vedľa textu ikona, bude sa musieť kontrolovať gap medzi ikonou a samotnými písmenami
                        var maxTextGap = TemplateParser.MaxTextGap;
                        var mergedWidths = new List<double>();
                        var maxGap = 0;
                        for (var j = 0; j < alignHorizontal.Length; j++)
                        {
                            // last element cant have a gap
                            if (j + 1 == alignHorizontal.Length)
                            {
                                connectedHorizontal.Add(alignHorizontal[j]);
                            }
                            else
                            {
                                var currentRect = alignHorizontal[j];
                                var nextRect = alignHorizontal[j + 1];
                                var firstGap = Math.Abs(nextRect.X - (currentRect.X + currentRect.Width));
                                var distance = Distance_BtwnPoints(new Point(currentRect.X + currentRect.Width, currentRect.Y + currentRect.Height), new Point(nextRect.X, nextRect.Y + nextRect.Height));
                                var merge = currentRect;

                                // Add current item's width
                                mergedWidths.Add(merge.Width);

                                //while ((distance <= maxTextGap || (currentRect & nextRect).area() > 0) && j + 1 < alignVertical.Length - 1)
                                while (firstGap <= maxTextGap || merge.IntersectsWith(nextRect))
                                {
                                    // Add merging item's width
                                    if (!merge.IntersectsWith(nextRect))
                                    {
                                        mergedWidths.Add(nextRect.Width);
                                        if (firstGap > maxGap) maxGap = firstGap;
                                        //Debug.WriteLine("adding gap " + firstGap);
                                        // calculate new max text gap
                                        var ratio = mergedWidths.Average() > 15 ? 3 : 2.5;
                                        maxTextGap = (int) (maxGap * ratio);
                                        if (maxTextGap < MaxTextGap)
                                        {
                                            maxTextGap = MaxTextGap;
                                        }
                                        //Debug.WriteLine("new max text gap " + maxTextGap);
                                    }

                                    // Merge items
                                    merge = merge | nextRect;

                                    Debug.WriteLine("merging " + j + " with " + (j+1) + ", distance = " + distance);

                                    j++;

                                    // @todo na font size bude musieť byť asi js skript ktorý vytvorí html a bude skúšať tak aby sa to tam vošlo a zistí teda koľko px bude mať font

                                    // Check if we are not on the last element in the row
                                    if (j + 1 <= alignHorizontal.Length - 1)
                                    {
                                        // Next rect's right position might be lower than current rect's (next rect is inside current rect)
                                        if (nextRect.X + nextRect.Width >= currentRect.X + currentRect.Width)
                                        {
                                            currentRect = alignHorizontal[j];
                                        }

                                        nextRect = alignHorizontal[j + 1];
                                        firstGap = Math.Abs(nextRect.X - (currentRect.X + currentRect.Width));
                                        distance = Distance_BtwnPoints(new Point(currentRect.X + currentRect.Width, currentRect.Y + currentRect.Height), new Point(nextRect.X, nextRect.Y + nextRect.Height));
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                connectedHorizontal.Add(merge);

                                // Reset gaps
                                mergedWidths = new List<double>();
                                maxTextGap = MaxTextGap;
                                maxGap = 0;
                                //Debug.WriteLine("resetting text gap");

                                Debug.WriteLine("p. " + j + " distance " + distance, " gap " + firstGap);
                            }
                        }

                        foreach (var rect in connectedHorizontal)
                        {
                            //limit++;
                            //if (limit == 100) break;

                            //Debug.WriteLine(limit + "=" + rect.Width + "," + rect.Height);

                            //var roi2 = _image.Clone(rect);
                            //roi2.SaveImage("wwwroot/images/image-" + limit + ".png");

                            //var image = new Image("./images/image-" + limit + ".png");
                            //container.Elements.Add(image);

                            //using (var page = _tess.Process(Pix.LoadFromFile("image-" + limit + ".png"), PageSegMode.SingleBlock))
                            //{
                            //    var text = page.GetText();

                            //    Debug.Write("image " + limit + "=" + text);
                            //}

                            Cv2.Rectangle(copy, new Point(rect.X, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height), Scalar.Purple);
                        }
                    }
                    
                    /* Column row letters merging end */
                }


                containers.Add(container);
                c++;
            }

            Debug.WriteLine("sekcia počet row " + rows.Count);

            foreach (var row in rows)
            {
                if (parent.Width == 0)
                {
                    Cv2.Rectangle(copy, new Point(0, row.Item1), new Point(copy.Width, row.Item2), Scalar.Orange);
                }
                else
                {
                    Cv2.Rectangle(copy, new Point(parent.X, row.Item1), new Point(parent.X + parent.Width, row.Item2), Scalar.Orange);
                }
            }

            return containers;
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

        private int findColumnForRect(List<Triple<int, int, List<Rect>>> columns, Rect rect)
        {
            int index = -1;

            int i = 0;
            foreach (var column in columns)
            {
                // rect fits exactly into column
                if (rect.X >= column.Item1 && rect.X + rect.Width <= column.Item2)
                {
                    return i;
                }
                // end of the rect doesnt fit
                if (rect.X <= column.Item2 && rect.X + rect.Width > column.Item2)
                {
                    column.Item2 += rect.X + rect.Width - column.Item2;
                    return i;
                }
                // rect doesn't fit just by a few pixels so we will merge them anyway
                if (rect.X > column.Item2 && rect.X - column.Item2  <= MínColumnGap)
                {
                    column.Item2 += rect.Width + rect.X - column.Item2;
                    return i;
                }

                i++;
            }

            return index;
        }

        private bool IsRectSeparator(Rect rect)
        {
            // @todo prvá časť podmienky rect.X <= _mostLef || teoreticky na rect.X == 0
            // @todo čo ak zoberie ten separator ako obdlznik s celym obsahom?
            return rect.Left <= _mostLeft && rect.Height <= MaxSeparatorHeight && rect.Width > MinSeparatorWidth;
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
