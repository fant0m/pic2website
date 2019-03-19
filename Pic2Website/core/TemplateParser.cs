using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using OpenCvSharp;
using Pic2Website.core.convertor;
using Pic2Website.core.model;
using Pic2Website.core.model.elements;
using Pic2Website.core.model.elements.basic;
using Pic2Website.core.model.elements.grid;
using Image = Pic2Website.core.model.elements.basic.Image;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;

namespace Pic2Website.core
{
    public class TemplateParser
    {
        private string _imagePath;
        private Mat _image;
        private Point[][] _contours;
        private HierarchyIndex[] _hierarchy;
        private TemplateStructure _templateStructure;
        private Ocr _ocr;
        private ColorAnalyser _colorAnalyser;
        private WebConvertor _convertor;
        private int limit = 0;
        private int test = 0;

        public const int MaxSeparatorHeight = 10;
        public const int MinSeparatorWidth = 400;
        public const int MaxTextGap = 6;
        public const int MinColumnGap = 10; // @todo možno podľa šírky layoutu

        public TemplateParser(string imagePath, string uuid)
        {
            _imagePath = imagePath;
            _convertor = new WebConvertor(uuid);
            _ocr = new Ocr();
        }

        public void Analyse()
        {
            // Load image
            byte[] imageData = File.ReadAllBytes(@_imagePath);
            _image = Mat.FromImageData(imageData);
            _colorAnalyser = new ColorAnalyser(_image);

            // Convert image to grayscale and then filter out the noise
            Mat gray = Mat.FromImageData(imageData, ImreadModes.Grayscale);
            gray = gray.GaussianBlur(new OpenCvSharp.Size(3, 3), 0);

            // Canny Edge Detector
            Mat cannyGray = gray.Canny(15, 18); // 0, 12, blur 9; 2, 17,  blur 7; 0, 25 blur 13; 20 35 blur 0; 15, 25 blur 3

            // Find contours
            Cv2.FindContours(cannyGray, out _contours, out _hierarchy, mode: RetrievalModes.Tree, method: ContourApproximationModes.ApproxSimple);

            // Init a new template structure
            _templateStructure = new TemplateStructure();
            _convertor.SetTemplateStructure(_templateStructure);

            // Analyse sections
            AnalyseSections();

            Mat copy = _image.Clone();
            Cv2.DrawContours(copy, _contours, -1, Scalar.Orange);
        }

        public void Convert(HttpResponse respnse)
        {
            _convertor.Convert();
            _convertor.Save();
        }

        private int AnalyseSections()
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
            var lastSectionY = -1;
            var sections = new List<Section>();
            var sectionContours = new Dictionary<int, List<int>>();
            var currentSectionContours = new List<int>();

            var rects = new List<Rect>();

            // Find sections
            i = startIndex;
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
                    section.Height = rect.Y - lastSectionY - 1;
                    section.Top = lastSectionY + 1;
                    section.Rect = new Rect(0, lastSectionY + 1, _image.Width, section.Height);
                    
                    // Add section to sections list
                    sections.Add(section);

                    // Save section image
                    roi = _image.Clone(section.Rect);
                    roi.SaveImage(_convertor.GetContentPath() + "images/section-" + sectionId + ".png");

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
            lastSection.Height = _image.Height - lastSectionY - 1;
            lastSection.Top = lastSectionY + 1;
            lastSection.Rect = new Rect(0, lastSectionY + 1, _image.Width, lastSection.Height);

            // Save section image
            roi = _image.Clone(lastSection.Rect);
            roi.SaveImage(_convertor.GetContentPath() + "images/section-" + sectionId + ".png");
            sections.Add(lastSection);
            sectionContours.Add(sectionId, currentSectionContours);

            var copy = _image.Clone();

            // Analyse section layouts
            var maxLayout = Layout.LayoutWidth.W800;
            var mostLeftSections = new List<double>();
            var mostRightSections = new List<double>();
            foreach (var section in sections)
            {
                // Access sections rects
                var contours = sectionContours[section.Id];

                // Analyse section layout
                section.Layout = DetectLayout(contours, copy.Width, section.Height);

                // Save mostLeft value for centered layout
                if (section.Layout.Type == Layout.LayoutType.Centered)
                {
                    mostLeftSections.Add(section.Layout.MostLeft);
                    mostRightSections.Add(section.Layout.MostRight);
                }

                // Save longest layout width
                if (section.Layout.Type == Layout.LayoutType.Centered && section.Layout.Width > maxLayout)
                {
                    maxLayout = section.Layout.Width;
                }
            }

            // Sort values
            mostLeftSections.Sort();
            mostRightSections.Sort();
            mostRightSections.Reverse();

            // Process sections
            foreach (var section in sections)
            {
                // Make sure each centered section has same container width
                if (section.Layout.Type == Layout.LayoutType.Centered && section.Layout.Width != maxLayout)
                {
                    section.Layout.Width = maxLayout;
                }

                // Set section min. width
                if (section.Layout.Type == Layout.LayoutType.Centered)
                {
                    section.MinWidth = (int) section.Layout.Width;
                }

                // Access sections rects
                var contours = sectionContours[section.Id];

                // Analyse section background image / color
                // @todo vymyslieť ako sa to má správať pri ďalších elementoch, či tam proste pošlem vždy len Element, alebo aj zoznam ktoré elementy nemá prechádzať atď.
                var rectangles = ContoursToRects(contours);
                ColorAnalyser.AnalyseSectionBackground(section, rectangles, _image);

                // Create a container
                var container = new Container(section.Layout);
                Rect containerRect;

                // Center content in container and create container rect
                if (section.Layout.Type == Layout.LayoutType.Centered)
                {
                    // left container padding
                    container.Padding[3] = (int) Math.Floor(((int) section.Layout.Width - (mostRightSections.FirstOrDefault() - mostLeftSections.FirstOrDefault() + 1)) / 2);

                    // right container padding should be same as left
                    container.Padding[1] = container.Padding[3];

                    // create container rect
                    var mostLeft = (int) mostLeftSections.FirstOrDefault();
                    var mostRight = (int) mostRightSections.FirstOrDefault();
                    containerRect = new Rect(mostLeft, section.Top, mostRight - mostLeft + 1, section.Height);
                }
                else
                {
                    // fluid layout has full width
                    containerRect = new Rect(0, section.Top, copy.Width, section.Height);
                }

                // Process inner blocks
                // @todo hm7.png nezoberie dobre text button ako sublement, algoritmu určite vadia rohy, to by chcelo nejak zisťovať a rovno aplikovať border-radius len pozor aby si to nemýlilo s inými tvarmi potom, kontrolovať sa musia iba rohy
                // @todo pozadie elementov bude treba ešte tuning, niekedy treba aby row mal farbu; taktiež optimiser bude asi musieť prejsť a nechať farbu len v poslednej úrovni
                // @todo text gap merging - space podľa fontu + info že je to text
                // @todo replace element width with right padding
                container.Rows = ProcessInnerBlocks(contours, copy, containerRect, section.BackgroundColor, section.Layout.Type == Layout.LayoutType.Fluid);

                // Append container to section
                section.Containers.Add(container);

                // Draw section and container
                Cv2.Rectangle(copy, new Point(section.Rect.Left, section.Rect.Top), new Point(section.Rect.Right, section.Rect.Bottom), Scalar.Red);
                if (containerRect.Height > 1)
                {
                    Cv2.Rectangle(copy, new Point(containerRect.Left, containerRect.Top + 1), new Point(containerRect.Right, containerRect.Bottom - 1), Scalar.LightSeaGreen);
                }
                else
                {
                    Cv2.Rectangle(copy, new Point(containerRect.Left, containerRect.Top + 1), new Point(containerRect.Right, containerRect.Bottom), Scalar.LightSeaGreen);
                }

                // Append section into template structure
                _templateStructure.Sections.Add(section);
            }

            copy.SaveImage("wwwroot/images/output.png");
            copy.SaveImage(_convertor.GetContentPath() + "structure.png");

            return startIndex;
        }

        private Rect[] ContoursToRects(List<int> contours)
        {
            // init an array of section rectangles (which we don't want to proccess)
            var rects = new Rect[contours.Count];
            var k = 0;
            foreach (var contour in contours)
            {
                // Edges
                var edges = _contours[contour];

                // Bounding box
                var rect = Cv2.BoundingRect(edges);
                rects[k] = rect;

                k++;
            }

            return rects;
        }

        /**
         * Check if contour doesn't have more blocks inside it
         */
        private Tuple<bool, List<Element>> CheckSubBlocks(int contour, Mat copy, int[] color)
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
                    }
                    else
                    {
                        break;
                    }

                    find = subitem.Next;
                }

                //Debug.WriteLine("počet sub " + inner.Count);

                // We have found inner elements
                if (inner.Count > 0)
                {
                    List<Element> rows;

                    //Check if inner elements doesn't form an image
                    var isImage = IsImage(contour, inner, copy);
                    Debug.WriteLine("result is image " + isImage);
                    Debug.WriteLine(rect.X + "," + rect.Y + "," + rect.Width + "," + rect.Height);
                    if (isImage)
                    {
                        return new Tuple<bool, List<Element>>(true, null);
                    }
                    else
                    {
                        // Otherwise process them
                        rows = ProcessInnerBlocks(inner, copy, rect, color);
                    }

                    return new Tuple<bool, List<Element>>(false, rows);
                }
            }

            return new Tuple<bool, List<Element>>(false, null);
        }

        /**
         * Check if contour has rectangle shape and required dimensions
         */
        private bool HasContourSubElements(Point[] edges)
        {
            var contoursAp = Cv2.ApproxPolyDP(edges, Cv2.ArcLength(edges, true) * 0.03, true);
            var rect = Cv2.BoundingRect(edges);

            // @todo možno bude treba inú podmienku ako length = 4, niečo viac sotisfikované čo sa pozrie či to má body len ako obdĺžnik alebo aj niečo vo vnútri
            //if (contoursAp.Length == 4 && rect.Width >= 10 && rect.Height >= 10)
            //{
            //    var roi2 = _image.Clone(rect);
            //    roi2.SaveImage("sub-" + DateTime.Now.Ticks + ".png");
            //}

            return contoursAp.Length == 4 && rect.Width >= 30 && rect.Height >= 30;
        }

        private bool IsImage(int parent, List<int> contours, Mat copy)
        {
            var parentEdges = _contours[parent];
            var parentRect = Cv2.BoundingRect(parentEdges);
            var isText = parentRect.Width * 1.0 / parentRect.Height > 3 && parentRect.Height < 50;
            if (isText)
            {
                return false;
            }

            var isSmall = parentRect.Width < 40 && parentRect.Height < 40;
            if (isSmall)
            {
                return true;
            }

            var count = contours.Count;
           
            //var sectionRects = new Rect[contours.Count];
            //var k = 0;
            var r = new Random();
            //foreach (var contour in contours)
            //{
            //    // Edges
            //    var edges = _contours[contour];

            //    // Bounding box
            //    var rect = Cv2.BoundingRect(edges);
            //    sectionRects[k] = rect;

            //    //Cv2.Rectangle(copy, new Point(rect.X, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height), Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255)));
            //    //Debug.WriteLine(rect.Width + "," + rect.Height);
            //    k++;
            //}
            var rects = ContoursToRects(contours);
            //foreach (var rect in rects)
            //{
            //    Cv2.Rectangle(copy, new Point(rect.X, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height), Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255)));
            //    Debug.WriteLine(rect.ToString());
            //}


            Debug.WriteLine("check is image " + contours.Count() + "," + parentRect.Width + "," + parentRect.Height);

            var checkSmallElements = rects.Where(e => (e.Width < 8 && e.Height < 8) || e.Height < 5 || e.Width < 5).Count();
            Debug.WriteLine("počet malých elem " + checkSmallElements);
            if (checkSmallElements * 1.0 / rects.Count() >= 0.7)
            {
                return true;
            }

            var subCount = 0;
            //var subRects = new List<Rect>();
            //Debug.WriteLine("normal count " + count);
            foreach (var contour in contours)
            {
                var item = _hierarchy[contour];
                if (item.Child != -1)
                {
                    var subItem = _hierarchy[item.Child];
                    while (subItem.Next != -1)
                    {
                        subCount++;

                        //if (subCount <= 50)
                        //{
                        //    var edges = _contours[subItem.Next];
                        //    var rect = Cv2.BoundingRect(edges);
                        //    subRects.Add(rect);
                        //}

                        subItem = _hierarchy[subItem.Next];
                    }
                }
            }
            // @todo to číslo nejak podľa proporcí obrázka?
            if (subCount > 300)
            {
                return true;
            }
            else
            {
                //Debug.WriteLine("menej ako 300");
                //checkSmallElements = subRects.Where(e => (e.Width < 10 && e.Height < 10) || e.Height < 5 || e.Width < 5).Count();
                //Debug.WriteLine("počet dodatočných " + subRects.Count() + ", počet z nich malých " + checkSmallElements + ",pomer" + (checkSmallElements * 1.0 / subRects.Count()));
                //if (checkSmallElements * 1.0 / subRects.Count() >= 0.7)
                //{
                    //return true;
                //}
            }
            //Debug.WriteLine("sub count " + subCount);

            return false;
        }

        

        private List<Element> ProcessInnerBlocks(List<int> contours, Mat copy, Rect parent, int[] color, bool fluid = false)
        {
            Debug.WriteLine("process inner blocks count " + contours.Count + "=rect " + parent.X + "," + parent.Y + "," + parent.Width + "," + parent.Height);
            var r = new Random();
            var sectionRows = new List<Element>();
            var sectionRects = new Rect[contours.Count];
            
            var k = 0;
            foreach (var contour in contours)
            {
                // Edges
                var edges = _contours[contour];

                // Bounding box
                var rect = Cv2.BoundingRect(edges);
                sectionRects[k] = rect;

                k++;
            }

            // check if there's not rect which should have recursive content but it's children rects are here
            //var wrongRects = sectionRects.Where(rect => rect.Width > 100 && rect.Height > 100);
            var rectsCopy = sectionRects;

            for (var i = rectsCopy.Length - 1; i >= 0; i--)
            {
                var rect = rectsCopy[i];
                if (rect.Width > 100 && rect.Height > 100)
                {
                    var contains = sectionRects.Where(re => rect.Contains(re)).Count();

                    if (contains > 1)
                    {
                        Debug.WriteLine("carefull! removed one element");
                        sectionRects = sectionRects.Where(re => re != rect).ToArray();
                        contours.Remove(contours[i]);
                    }
                }
            }

            // process sub blocks of contours
            var sectionRecursiveRows = new List<Element>[sectionRects.Length];
            var sectionRectsImages = new bool[sectionRects.Length];
            var img = 0;
            k = 0;
            foreach (var contour in contours)
            {
                //Debug.WriteLine("processing sub blocks " + k);
                var result = CheckSubBlocks(contour, copy, color);
                //Debug.WriteLine("end of sub block " + k + "=" + result.Item1 + "," + result.Item2);
                sectionRectsImages[k] = result.Item1;
                sectionRecursiveRows[k] = result.Item2;

                img += result.Item1 ? 1 : 0;
                k++;
            }


            // make copy of rects
            var sectionRectsUnsorted = sectionRects;

            var isImg = false;
            // all sub contours are images
            if (img == contours.Count)
            {
                // @todo not sure
                isImg = true;
                //Debug.WriteLine("all contours are images");
            }
            else
            {
                // count mess elements
                //var mess = sectionRects.Where(e => (e.Width < 10 && e.Height < 10) || e.Height < 5 || e.Width < 5).Count();
                //if (mess * 1.0 / sectionRects.Length >= 0.5)
                //{
                //    isImg = true;
                //}

                // @todo skúsiť pozrieť vo zvýšných rectoch či sa nenachádza aj niečo rozumné ako text


                if (img > 0)
                {
                    // find image rects
                    var imageRects = new List<Rect>(img);
                    for (var i = 0; i < sectionRects.Length; i++)
                    {
                        if (sectionRectsImages[i])
                        {
                            imageRects.Add(sectionRects[i]);
                        }
                    }

                    // check if other rects are not intersecting image rects
                    //bool intersects = false;
                    //for (var i = 0; i < sectionRects.Length; i++)
                    //{
                    //    var rect = sectionRects[i];
                    //    foreach (var imageRect in imageRects)
                    //    {
                    //        if (imageRect != rect && (imageRect.Contains(rect) || imageRect.IntersectsWith(rect)))
                    //        {
                    //            intersects = true;
                    //            break;
                    //        }
                    //    }
                    //}

                    //// @todo intersects > xx?
                    //if (intersects)
                    //{
                        //Debug.WriteLine("it intersects so its image");
                        //isImg = true;
                    //}
                }
            }

            // skip content parsing if it's image
            if (isImg)
            {
                Debug.WriteLine("its directly image, skipping content parsing");
                var row = new Row();
                var column = new Column();

                limit++;

                // create image
                var roi = _image.Clone(parent);
                roi.SaveImage(_convertor.GetContentPath() + "images/image-" + limit + ".png");

                var image = new Image("./images/image-" + limit + ".png");
                image.Display = "inline";

                // draw image
                if (parent.Height > 1)
                {
                    Cv2.Rectangle(copy, new Point(parent.Left, parent.Top + 1), new Point(parent.Right, parent.Bottom - 1), Scalar.Purple);
                }
                else
                {
                    Cv2.Rectangle(copy, new Point(parent.Left, parent.Top + 1), new Point(parent.Right, parent.Bottom), Scalar.Purple);
                }

                // fill structure
                column.Elements.Add(image);
                row.Columns.Add(column);
                sectionRows.Add(row);

                return sectionRows;
            }

            // align rects from the top to the bottom
            sectionRects = sectionRects.OrderBy(rec => rec.Top).ToArray();

            // analyse section rows
            var rows = new List<TemplateBlock<int, int, List<Rect>, Element>>(); // y start y end
            foreach (var rect in sectionRects)
            {
                var rowIndex = FindRowForRect(rows, rect);
                if (rowIndex == -1)
                {
                    // set last row dimensions
                    if (rows.Count > 0)
                    {
                        var latest = rows.Last();
                        var sortedLeft = latest.Item3.OrderBy(rec => rec.Left).ToList().First();
                        var sortedRight = latest.Item3.OrderByDescending(rec => rec.Right).ToList().First();

                        // apply margins for last row
                        latest.Element.Margin[1] = parent.Right - sortedRight.Right;
                        latest.Element.Margin[3] = sortedLeft.X - parent.X;

                        // adjust rect
                        //latest.Element.Rect.X += latest.Element.Margin[3];
                        //latest.Element.Rect.Width -= latest.Element.Margin[1] + latest.Element.Margin[3];

                        // we need to change paddings from pixels to percents in fluid layout
                        if (fluid)
                        {
                            latest.Element.Margin[3] = (int)Math.Floor(latest.Element.Margin[3] * 1.0 / parent.Width * 100);
                            latest.Element.Margin[1] = latest.Element.Margin[3];

                            latest.Element.Fluid = true;
                        }
                    }

                    // create section row
                    var sectionRow = new Row();
                    sectionRow.Rect = new Rect(parent.X, rect.Y, parent.Width, rect.Height);

                    // set section row styles
                    var latestTop = rows.Count == 0 ? parent.Y : rows.Last().Item2;
                    // @todo bez tej podmienky vždy to tak asi bude
                    var latestBottom = rows.Count == 0 ? parent.Bottom : parent.Bottom;
                    var latestLeft = parent.X;

                    // apply styles for row
                    sectionRow.Margin[0] = rect.Y - latestTop;

                    //if (sectionRects.Length == 1)
                    //{
                    //    sectionRow.Margin[3] = rect.X - latestLeft;
                    //    sectionRow.Padding[2] = latestBottom - rect.Bottom;
                    //}

                    // @todo test
                    //sectionRow.BackgroundColor = new[] { r.Next(0, 255), r.Next(0, 255), r.Next(0, 255) };

                    var triple = new TemplateBlock<int, int, List<Rect>, Element>
                    {
                        Item1 = rect.Top,
                        Item2 = rect.Bottom,
                        Item3 = new List<Rect> { rect },
                        Element = sectionRow
                    };
                    rows.Add(triple);
                }
                else
                {
                    rows[rowIndex].Item3.Add(rect);
                }
            }

            // apply bottom padding for last section row
            if (rows.Count > 0)
            {
                var last = rows.Last();
                var sortedLeft = last.Item3.OrderBy(rec => rec.Left).ToList().First();
                var sortedRight = last.Item3.OrderByDescending(rec => rec.Right).ToList().First();

                // apply styles for row
                last.Element.Margin[1] = parent.Right - sortedRight.Right;
                last.Element.Margin[2] = parent.Bottom - last.Item2;
                last.Element.Margin[3] = sortedLeft.X - parent.X;

                // adjust rect
                //last.Element.Rect.X += last.Element.Margin[3];
                //last.Element.Rect.Width -= last.Element.Margin[1] + last.Element.Margin[3];

                // we need to change paddings from pixels to percents in fluid layout
                if (fluid)
                {
                    last.Element.Margin[3] = (int)Math.Floor(last.Element.Margin[3] * 1.0 / parent.Width * 100);
                    last.Element.Margin[1] = last.Element.Margin[3];

                    last.Element.Fluid = true;
                }
            }

            // proceed rows
            var c = 1;
            var fluidWidths = new List<int>();
            foreach (var row in rows)
            {
                var sectionRow = (Row) row.Element;

                /* Columns start */

                // align rects from the left to the right
                var alignedRects = row.Item3.OrderBy(rec => rec.Left).ToArray();

                // analyse section columns
                var fluidPercents = 0.0;
                var columns = new List<TemplateBlock<int, int, List<Rect>, Element>>(); // x start, x end, list of items
                foreach (var rect in alignedRects)
                {
                    var columnIndex = FindColumnForRect(columns, rect);
                    if (columnIndex == -1)
                    {
                        // set last column dimensions
                        if (columns.Count > 0)
                        {
                            // apply right margin for column
                            var latest = columns.Last();
                            var latestElem = latest.Element;
                            latestElem.Width = latest.Item2 - latest.Item1 + 1;
                            latestElem.Margin[1] = rect.X - latest.Item2 - 1;
                            latestElem.Rect = new Rect(latest.Item1, row.Item1, latest.Item2 - latest.Item1, row.Item2 - row.Item1);
                            if (latestElem.Rect.Height > 20)
                            {
                                // @todo možno až na konci toto aplikovať, nakoľko v tomto momente nevieme či je dobrý nápad to aplikovať (vo vnútri môže byť text a na pozadie dá farbu textu)
                                latestElem.BackgroundColor = _colorAnalyser.AnalyseRect(latestElem.Rect, color);
                            }

                            if (fluid)
                            {
                                var margin = (parent.Width / 100 * sectionRow.Margin[1]) * 2;

                                // replace width with percentage value
                                // @todo možno pozrieť aká je tam medzera a ak nie dosť veľká tak to tu nedávať
                                if (latestElem.BackgroundColor != null)
                                {
                                    var percents = Math.Ceiling(latestElem.Width / (parent.Width - margin) * 100);
                                    percents += (int)Math.Floor(100.0 * latestElem.Margin[1] / (parent.Width - margin));
                                    latestElem.MarginCalc[1] = latestElem.MarginCalc[3] = $"calc(({percents}% - {latestElem.Width}px) / 2)";

                                    fluidPercents += percents;
                                }
                                else
                                {
                                    latestElem.Width = Math.Ceiling(latestElem.Width / (parent.Width - margin) * 100);
                                    latestElem.Margin[1] = (int)Math.Floor(100.0 * latestElem.Margin[1] / (parent.Width - margin));
                                    if (latestElem.Margin[1] > 0)
                                    {
                                        latestElem.Width++;
                                        latestElem.Margin[1]--;
                                    }

                                    fluidPercents += latestElem.Width + latestElem.Margin[1];
                                    latestElem.Fluid = true;
                                }
                            }
                        }

                        // create new column
                        var column = new Column();
                        var triple = new TemplateBlock<int, int, List<Rect>, Element>
                        {
                            Item1 = rect.Left,
                            Item2 = rect.Right,
                            Item3 = new List<Rect> { rect },
                            Element = column
                        };
                        columns.Add(triple);
                        sectionRow.Columns.Add(column);
                    }
                    else
                    {
                        columns[columnIndex].Item3.Add(rect);
                    }
                }

                // Set width for the last column
                if (columns.Count > 0)
                {
                    var latest = columns.Last();
                    var latestElem = latest.Element;
                    latestElem.Width = latest.Item2 - latest.Item1 + 1;
                    latestElem.Rect = new Rect(latest.Item1, row.Item1, latest.Item2 - latest.Item1 + 1, row.Item2 - row.Item1 + 1);
                    if (latestElem.Rect.Height > 20)
                    {
                        latestElem.BackgroundColor = _colorAnalyser.AnalyseRect(latestElem.Rect, color);
                    }

                    if (fluid)
                    {
                        if (latestElem.BackgroundColor != null)
                        {
                            var percents = 100 - fluidPercents;
                            latestElem.MarginCalc[1] = latestElem.MarginCalc[3] = $"calc(({percents}% - {latestElem.Width}px) / 2)";
                        }
                        else
                        {
                            latestElem.Width = 100 - fluidPercents;
                            latestElem.Fluid = true;
                            // @todo poriadne pozrieť či to nezarovnáva zle
                            // if there are multiple column in fluid layout and last column is located at the end we want to keep it there at any resolution
                            if (columns.Count == 2 && latest.Item2 >= _image.Width * 0.95)
                            {
                                latestElem.TextAlign = "right";
                            }
                        }
                    }

                    // normalize widths for fluid layout
                    // @todo probably can be removed, split into columns is replacing it, it's not working correctly aswell
                    if (false && fluid)
                    {
                        if (fluidWidths.Count == 0)
                        {
                            foreach (var column in columns)
                            {
                                fluidWidths.Add((int)column.Element.Width);
                            }
                        }
                        else
                        {
                            var refill = false;
                            if (fluidWidths.Count == columns.Count)
                            {
                                // check if columns have almost same widths
                                var sameWidths = true;
                                for (var i = 0; i < columns.Count; i++)
                                {
                                    if (!Util.AreSame(columns[i].Element.Width, fluidWidths[i], 2))
                                    {
                                        sameWidths = false;
                                    }
                                }

                                if (sameWidths)
                                {
                                    for (var i = 0; i < columns.Count; i++)
                                    {
                                        columns[i].Element.Width = fluidWidths[i];
                                    }
                                }
                                else
                                {
                                    refill = true;
                                }
                            }
                            else
                            {
                                refill = true;
                            }


                            if (refill)
                            {
                                fluidWidths.Clear();
                                foreach (var column in columns)
                                {
                                    fluidWidths.Add((int)column.Element.Width);
                                }
                            }
                        }
                    }
                }

                // draw columns
                foreach (var column in columns)
                {
                    Cv2.Rectangle(copy, new Point(column.Item1, row.Item1), new Point(column.Item2, row.Item2), Scalar.GreenYellow);
                }

                /* Columns end */


                // Process columns into rows
                foreach (var column in columns)
                {
                    /* Column rows start */

                    // align rects from the top to the bottom
                    var alignedColumnRects = column.Item3.OrderBy(rec => rec.Top).ToArray();

                    // detect column rows
                    var columnRows = new List<TemplateBlock<int, int, List<Rect>, Element>>(); // y start y end
                    foreach (var rect in alignedColumnRects)
                    {
                        var rowIndex = FindRowForRect(columnRows, rect);
                        if (rowIndex == -1)
                        {
                            //var columnRow = new Row(1);
                            var columnRow = new Block();
                            var triple = new TemplateBlock<int, int, List<Rect>, Element>
                            {
                                Item1 = rect.Top,
                                Item2 = rect.Bottom,
                                Item3 = new List<Rect> { rect },
                                Element = columnRow
                            };

                            // apply styles
                            var latestTop = columnRows.Count == 0 ? row.Item1 : columnRows.Last().Item2;
                            columnRow.Margin[0] = rect.Y - latestTop;

                            
                            columnRows.Add(triple);
                        }
                        else
                        {
                            columnRows[rowIndex].Item3.Add(rect);
                        }
                    }

                    // apply bottom padding for last section row
                    var lastColumnRow = columnRows.Last();
                    if (lastColumnRow != null)
                    {
                        lastColumnRow.Element.Margin[2] = row.Item2 - lastColumnRow.Item2;
                    }


                    // draw column rows
                    foreach (var columnRow in columnRows)
                    {
                        Cv2.Rectangle(copy, new Point(column.Item1, columnRow.Item1), new Point(column.Item2, columnRow.Item2), Scalar.DarkOrange);
                    }

                    /* Column rows end */


                    /* Column row rects */
                    // process column rows
                    foreach (var columnRow in columnRows)
                    {
                        var block = (Block) columnRow.Element;
                        // align contours inside row from left to right and filter small elements
                        // @todo neviem či filtrovať aj tie rozmery nakoľko môžeme prísť o znaky v texte ako bodka
                        var alignHorizontal = columnRow.Item3.Where(rect => rect.Width * rect.Height >= 5).OrderBy(rect => rect.X).ToArray();
                        var connectedHorizontal = new List<Rect>();

                        // is item x in connectedHorizontal merged from more than 2 elements?
                        var mergedHorizontal = new List<bool>();

                        //var l = 0;
                        //foreach (var contour in alignHorizontal)
                        //{
                        //    //var roi2 = _image.Clone(contour);
                        //    var roi2 = _image.Clone();
                        //    Cv2.Rectangle(roi2, new Point(contour.X, contour.Y), new Point(contour.X + contour.Width, contour.Y + contour.Height), Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255)));
                        //    roi2.SaveImage("image2-" + test + ".png");
                        //    l++;
                        //    test++;
                        //    Cv2.Rectangle(copy, new Point(contour.X, contour.Y), new Point(contour.X + contour.Width, contour.Y + contour.Height), Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255)));
                        //}

                        //Debug.WriteLine("počet " + alignHorizontal.Length);

                        /* Column row letters merging start */

                        // connect letters into words
                        // @todo tú medzeru medzi textom asi bude treba riešiť tak že sa zistí typ fontu, veľkosť a zistí sa koľko by mala mať px medzera
                        // @todo taktiež sa bude spájať iba to čo je jasné že je text, t.j. podľa rozmerov určite nemôže spojiť input a button ako v template2
                        // @todo asi bude aj tak problém zisťovať napr. či nie je vedľa textu ikona, bude sa musieť kontrolovať gap medzi ikonou a samotnými písmenami
                        var maxTextGap = MaxTextGap;
                        var mergedWidths = new List<double>();
                        var maxGap = 0;
                        for (var j = 0; j < alignHorizontal.Length; j++)
                        {
                            // last element cant have a gap or element might have sub elements or it might be an image
                            var indexCurrent = Array.IndexOf(sectionRectsUnsorted, alignHorizontal[j]);
                            var indexNext = Array.IndexOf(sectionRectsUnsorted, alignHorizontal[j + 1 <= alignHorizontal.Length - 1 ? j + 1 : j]);
                            if (j + 1 == alignHorizontal.Length ||
                                sectionRecursiveRows[indexCurrent] != null ||
                                sectionRectsImages[indexCurrent] == true ||
                                sectionRecursiveRows[indexNext] != null ||
                                sectionRectsImages[indexNext] == true)
                            {
                                connectedHorizontal.Add(alignHorizontal[j]);
                                mergedHorizontal.Add(false);
                            }
                            else
                            {
                                var currentRect = alignHorizontal[j];
                                var nextRect = alignHorizontal[j + 1];
                                var gap = Math.Abs(nextRect.Left - currentRect.Right - 1);
                                var merge = currentRect;
                                var merged = false;

                                // Add current item's width
                                mergedWidths.Add(merge.Width);
                                //Debug.WriteLine("gap = " + gap);

                                //while ((distance <= maxTextGap || (currentRect & nextRect).area() > 0) && j + 1 < alignVertical.Length - 1)
                                // either end position of next element is smaller end position of current element or slightly higher
                                var horizontalMerge = nextRect.Right <= currentRect.Right || Util.AreSame(nextRect.Right, currentRect.Right);

                                while (gap <= maxTextGap || merge.IntersectsWith(nextRect) || horizontalMerge)
                                {
                                    // Add merging item's width
                                    if (!merge.IntersectsWith(nextRect))
                                    {
                                        mergedWidths.Add(nextRect.Width);
                                        if (gap > maxGap) maxGap = gap;
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
                                    merged = true;

                                    //Debug.WriteLine("merging " + j + " with " + (j + 1));

                                    j++;

                                    // Check if we are not on the last element in the row
                                    // @todo not sure či to má byť zakomentované, neviem či je prednejší rekurzívny obsah alebo že sa to má mergnúť
                                    if (j + 1 <= alignHorizontal.Length - 1 /*&&
                                        sectionRecursiveRows[Array.IndexOf(sectionRectsUnsorted, alignHorizontal[j + 1])] == null &&
                                        sectionRectsImages[Array.IndexOf(sectionRectsUnsorted, alignHorizontal[j + 1])] == false*/)
                                    {
                                        // Next rect's right position might be lower than current rect's (next rect is inside current rect)
                                        if (nextRect.Right >= currentRect.Right)
                                        {
                                            currentRect = alignHorizontal[j];
                                        }

                                        nextRect = alignHorizontal[j + 1];
                                        gap = Math.Abs(nextRect.Left - currentRect.Right - 1);
                                        //Debug.WriteLine("gap = " + gap);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                connectedHorizontal.Add(merge);
                                mergedHorizontal.Add(merged);

                                // Reset gaps
                                mergedWidths = new List<double>();
                                maxTextGap = MaxTextGap;
                                maxGap = 0;
                                //Debug.WriteLine("resetting text gap");

                                //Debug.WriteLine("p. " + j + " distance " + distance, " gap " + firstGap);
                            }
                        }
                        
                        /* Column row letters merging end */


                        /*var intersect = false;
                        var connectedCopy = connectedHorizontal;
                        for (var i = 0; i < connectedCopy.Count; i++)
                        {
                            var mergedCount = 0;
                            for (var j = i + 1; j < connectedCopy.Count; j++)
                            {
                                if (connectedCopy[i].IntersectsWith(connectedCopy[j]))
                                {
                                    mergedCount++;
                                    Debug.WriteLine("it intersects :O");
                                    Debug.WriteLine(connectedCopy[i].X + "," + connectedCopy[i].Y + "," + connectedCopy[i].Width + "," + connectedCopy[i].Height);
                                    Debug.WriteLine(connectedCopy[j].X + "," + connectedCopy[j].Y + "," + connectedCopy[j].Width + "," + connectedCopy[j].Height);
                                    //break;
                                }
                            }

                            if (mergedCount > 0)
                            {
                                connectedHorizontal.RemoveRange(i + 1, mergedCount);
                                connectedHorizontal[i] = merged;
                                break;
                            }
                        }

                        if (intersect)
                        {
                            Debug.WriteLine("it intersects :O" + parent.X + "," + parent.Y + "," + parent.Width + "," + parent.Height);
                        }*/

                        // Add items to col
                        var lastX = -1;
                        for (var i = 0; i < connectedHorizontal.Count; i++)
                        {
                            // Access current rect
                            var rect = connectedHorizontal[i];

                            // Element has recursive content so we need to replace it with the right content
                            if (sectionRectsUnsorted.Contains(rect) && sectionRecursiveRows[Array.IndexOf(sectionRectsUnsorted, rect)] != null)
                            {
                                var index = Array.IndexOf(sectionRectsUnsorted, rect);

                                // There's just one row and one column so we dont need these elements
                                // @todo remove - toto už asi rieši optimiser
                                if (false && sectionRecursiveRows[index].Count == 1 && sectionRecursiveRows[index].First().GetType() == typeof(Row) && 
                                    ((Row)sectionRecursiveRows[index].First()).Columns.Count == 1 && ((Row)sectionRecursiveRows[index].First()).Columns.First().Elements.Count == 1)
                                {
                                    throw new Exception("kidding me?");
                                    //foreach (var element in sectionRecursiveRows[index].First().Columns.First().Elements)
                                    //{
                                    //    block.Elements.Add(element);
                                    //}
                                    var firstRow = sectionRecursiveRows[index].First();
                                    var firstColumn = ((Row)firstRow).Columns.First();
                                    var element = firstColumn.Elements.First();
                                    element.Margin = element.Margin.Zip(firstRow.Margin, (a, b) => a + b).ToArray();
                                    element.Padding = element.Padding.Zip(firstRow.Padding, (a, b) => a + b).ToArray();

                                    if (firstColumn.BackgroundColor != null)
                                    {
                                        element.BackgroundColor = firstColumn.BackgroundColor;
                                        element.Width = firstColumn.Width;
                                    }

                                    if (lastX != -1)
                                    {
                                        //element.Margin[3] += rect.X - lastX - 1;
                                    }

                                    block.Elements.Add(element);
                                }
                                // Append all recursive rows
                                else
                                {
                                    Rect? lastRect = null;

                                    foreach (var recursiveRow in sectionRecursiveRows[index])
                                    {
                                        // check if items are is in the same row
                                        if (lastRect == null || recursiveRow.Rect.Y >= ((Rect)lastRect).Y && recursiveRow.Rect.Y <= ((Rect)lastRect).Bottom)
                                        {
                                            // @todo 2019-02-26 v oboch prípadoch má display inline-block a width auto takže sa to už bude môcť komplet odstrániť (AcsAsColumn & podmienky)
                                            if (recursiveRow.GetType() == typeof(Row) && sectionRecursiveRows[index].Count > 1)
                                            {
                                                ((Row)recursiveRow).AcsAsColumn();
                                            }
                                            else
                                            {
                                                recursiveRow.Display = "inline-block";
                                            }
                                        }

                                        if (recursiveRow.Margin[3] == 0)
                                        {
                                            if (lastX != -1)
                                            {
                                                recursiveRow.Margin[3] = rect.X - lastX - 1;
                                            }
                                            else
                                            {
                                                recursiveRow.Margin[3] = rect.X - column.Item1;
                                            }
                                        }

                                        // Add result into block
                                        block.Elements.Add(recursiveRow);

                                        lastRect = recursiveRow.Rect;
                                    }
                                }

                                lastX = rect.Right;
                            }
                            else
                            {
                                Element result = null;
                                var text = mergedHorizontal[i];

                                // filter non-text elements
                                if (rect.Width < 15 || rect.Height < 10 || rect.Height > 100)
                                {
                                    text = false;
                                }
                                // this might be a text even though there wasn't any merge
                                else if (!text && rect.Width > 20 && rect.Height >= 10 && rect.Width * 1.0 / rect.Height >= 1.4)
                                {
                                    text = true;
                                }

                                // this is an icon/image inside wrapper
                                if (text && connectedHorizontal.Count <= 5 && parent.Width == parent.Height && parent.Width < 110)
                                {
                                    // @todo would be best to merge all those connectedHorizontal items
                                    text = false;
                                }

                                Debug.WriteLine("text=" + text + "," + rect.ToString());

                                if (text)
                                {
                                    // tesseract needs a margin to read the text properly
                                    var margin = 2;
                                    var x = rect.X - margin;
                                    var y = rect.Y - margin;
                                    var width = rect.Width + 2 * margin;
                                    var height = rect.Height + 2 * margin;

                                    if (x < 0) x = 0;
                                    if (y < 0) y = 0;
                                    if (x + width > _image.Width) width = _image.Width - x;
                                    if (y + height > _image.Height) height = _image.Height - y;

                                    var tessRect = new Rect(x, y, width, height);

                                    var roi = _image.Clone(tessRect);
                                    Bitmap bitmap = new Bitmap(roi.ToMemoryStream());

                                    var textElem = _ocr.GetText(bitmap);
                                    if (textElem == null || !IsTextValid(textElem, rect, connectedHorizontal.Count))
                                    {
                                        Debug.WriteLine("textelem = null or !IsTextValid, create img");
                                        text = false;
                                    }
                                    else
                                    {
                                        result = textElem;
                                    }
                                }

                                if (!text)
                                {
                                    limit++;

                                    var roi = _image.Clone(rect);
                                    //Debug.WriteLine(rect.X + "," + rect.Y + "," + rect.Width + "," + rect.Height);
                                    //Debug.WriteLine(roi2.Width + "," + roi2.Height);
                                    roi.SaveImage(_convertor.GetContentPath() + "images/image-" + limit + ".png");

                                    var image = new Image("./images/image-" + limit + ".png");
                                    // align elements next each other

                                    //Debug.WriteLine("margin " + (rect.Y - columnRow.Item1) + "," + (columnRow.Item2 - (rect.Y + rect.Height)));
                                    

                                    result = image;
                                }

                                if (lastX != -1)
                                {
                                    result.Margin[3] = rect.X - lastX - 1;
                                }
                                else
                                {
                                    result.Margin[3] = rect.X - column.Item1;
                                }

                                if (rect.Top > 0)
                                {
                                    result.Margin[0] = rect.Top - columnRow.Item1;
                                }
                                // align elements next each other
                                if (connectedHorizontal.Count > 1)
                                {
                                    result.Display = "inline-block";
                                }

                                // Add result into block
                                block.Elements.Add(result);

                                lastX = rect.Right;

                                if (rect.Height > 1)
                                {
                                    Cv2.Rectangle(copy, new Point(rect.Left, rect.Top + 1), new Point(rect.Right, rect.Bottom - 1), Scalar.Purple);
                                }
                                else
                                {
                                    Cv2.Rectangle(copy, new Point(rect.Left, rect.Top + 1), new Point(rect.Right, rect.Bottom), Scalar.Purple);
                                }
                            }
                        }
                    }

                    /* Column row rects end */

                    // add column rows (blocks) into column
                    /*if (columnRows.Count > 1)
                    {
                        // there are multiple rows (blocks)
                        foreach (var columnRow in columnRows)
                        {
                            var block = (Block)columnRow.Element;
                            var columnElement = ((Column)column.Element);

                            // check if there's not just one element
                            if (block.Elements.Count == 1)
                            {
                                var firstElement = block.Elements.First();

                                // copy styles
                                firstElement.Margin = firstElement.Margin.Zip(block.Margin, (a, b) => a + b).ToArray();
                                firstElement.Padding = firstElement.Padding.Zip(block.Padding, (a, b) => a + b).ToArray();

                                columnElement.Elements.Add(firstElement);
                            }
                            else
                            {
                                columnElement.Elements.Add(block);
                            }

                            //((Column)column.Element).Elements.Add(columnRow.Element);
                        }
                    }
                    else
                    {
                        // there's just one row (block) so this element is useless and we can merge it with column
                        var block = ((Block)columnRows.First().Element);
                        var columnElement = ((Column)column.Element);

                        // @todo not sure či to mergovať iba keď je tam 1 element alebo viac

                        // check if there's not just one element
                        if (block.Elements.Count == 1)
                        {
                            columnElement.Elements = block.Elements;

                            // copy styles
                            columnElement.Padding[0] += block.Padding[0];
                            columnElement.Padding[2] += block.Padding[2];

                            // access that element
                            var element = columnElement.Elements.First();

                            // check if element has padding/margin; if not it doesn't need any background color
                            if (element.Padding.Sum() == 0 && element.Margin.Sum() == 0)
                            {
                                columnElement.BackgroundColor = null;
                            }
                            else
                            {
                                // @todo možno check if element == Text, a znova zistiť background color s maskou imagu
                            }
                        }
                        else
                        {
                            columnElement.Elements.Add(block);
                        }
                    }*/

                    foreach (var columnRow in columnRows)
                    {
                        var block = (Block)columnRow.Element;

                        // Check if result are not unmerged texts
                        StructureOptimiser.CheckForUnmergedTexts(block);

                        var columnElement = ((Column)column.Element);

                        // check if there's not just one element
                        if (block.Elements.Count == 1)
                        {
                            var firstElement = block.Elements.First();

                            // copy styles
                            /*if (firstElement.GetType() == typeof(Text) && columnElement.BackgroundColor == null)
                            {
                                firstElement.Padding = firstElement.Padding.Zip(block.Margin, (a, b) => a + b).ToArray();
                                firstElement.Padding = firstElement.Padding.Zip(block.Padding, (a, b) => a + b).ToArray();
                            }
                            else
                            {*/
                                firstElement.Margin = firstElement.Margin.Zip(block.Margin, (a, b) => a + b).ToArray();
                                firstElement.Padding = firstElement.Padding.Zip(block.Padding, (a, b) => a + b).ToArray();
                            //}
                          
                            columnElement.Elements.Add(firstElement);
                        }
                        else
                        {
                            columnElement.Elements.Add(block);
                        }
                        //((Column)column.Element).Elements.Add(columnRow.Element);
                    }
                }

                /* Adjust column widths start */

                // @todo asi width ostanú a len marginy sa nastavia, alebo všetky riadky budú musieť vedieť že majú fixnú width
                // @todo neviem čo to malo robiť :-D

                /* Adjust column widths end */


                sectionRows.Add(sectionRow);
                c++;
            }

            // Fix columns count
            StructureOptimiser.FixColumnsCount(sectionRows, fluid);

            // Split into columns
            StructureOptimiser.SplitIntoColumns(sectionRows, fluid);

            // Merge columns into logical parts
            StructureOptimiser.MergeIntoLogicalColumns(sectionRows, fluid);

            // Optimiser text elements
            StructureOptimiser.OptimiseText(sectionRows);

            // Draw rows
            foreach (var row in rows)
            {
                Cv2.Rectangle(copy, new Point(parent.Left + 1, row.Item1), new Point(parent.Right - 1, row.Item2), Scalar.Green);
            }

            return sectionRows;
        }

        private bool IsTextValid(Text textElem, Rect rect, int numberOfElements)
        {
            if (textElem.GetText()[0].Length == 1)
            {
                return false;
            }

            if (rect.Width > 150 && textElem.GetText()[0].Length <= 2)
            {
                return false;
            }

            if (numberOfElements == 1 && textElem.GetText()[0].Length <= 2)
            {
                return false;
            }

            if (rect.Width == rect.Height)
            {
                return false;
            }

            var allowedChars = new char[] { ' ', ',', '.', '/', '©', '@', '-', ':', '+', '(', ')', '\'', '|', '#', '&', '"', '?', '=', '‘', '’', '$', '€' };
            bool result = textElem.GetText()[0].All(c => char.IsLetterOrDigit(c) || allowedChars.Contains(c));
            if (!result)
            {
                return false;
            }

            return true;
        }

        private int FindRowForRect(List<TemplateBlock<int, int, List<Rect>, Element>> rows, Rect rect)
        {
            int index = -1;

            int i = 0;
            foreach (var row in rows)
            {
                // rect fits exactly into row
                if (rect.Top >= row.Item1 && rect.Bottom <= row.Item2)
                {
                    return i;
                }
                // end of the rect doesnt fit
                if (rect.Top <= row.Item2 && rect.Bottom > row.Item2)
                {
                    row.Item2 += rect.Bottom - row.Item2;
                    return i;
                }

                i++;
            }

            return index;
        }

        private int FindColumnForRect(List<TemplateBlock<int, int, List<Rect>, Element>> columns, Rect rect)
        {
            int index = -1;

            int i = 0;
            foreach (var column in columns)
            {
                // rect fits exactly into column
                if (rect.Left >= column.Item1 && rect.Right <= column.Item2)
                {
                    return i;
                }
                // end of the rect doesnt fit
                if (rect.Left <= column.Item2 && rect.Right > column.Item2)
                {
                    column.Item2 += rect.Right - column.Item2;
                    return i;
                }
                // rect doesn't fit just by a few pixels so we will merge them anyway
                if (rect.Left > column.Item2 && rect.Left - column.Item2 - 1 <= MinColumnGap)
                {
                    column.Item2 += rect.Right - column.Item2;
                    return i;
                }

                i++;
            }

            return index;
        }

        private bool IsRectSeparator(Rect rect)
        {
            // @todo prvá časť podmienky rect.X <= _mostLef || teoreticky na rect.X == 0
            //return rect.Left <= _mostLeft && rect.Height <= MaxSeparatorHeight && rect.Width > MinSeparatorWidth;
            return rect.Left <= 0 && rect.Height <= MaxSeparatorHeight && rect.Width > MinSeparatorWidth;
        }

        /**
        * Detect type of template layout and it's dimensions
        * 
        * @todo lepší algoritmus na hľadanie oboch súradníc
        * @todo teoreticky skúsiť brať úplne prvý element, alebo skôr hodnota ktorá predstavuje min./max. ohraničenie (ľavý/pravý) pre 90% všetkých hodnôt
        * @todo asi bude stačiť komplet vynechať elementy mimo containera tak aby neboli v dizajne a potom stačí zobrať najmenšiu ľavú hodnotu a najväčšiu pravú
        */
        private Layout DetectLayout(List<int> contours, double width, double height)
        {
            var left = new List<double>();
            var right = new List<double>();

            // Process outer contours
            foreach (var i in contours)
            {
                // find current item
                var index = _hierarchy[i];
                var item = _contours[i];

                // find edges & boundix box
                var edges = _contours[i];
                var rect = Cv2.BoundingRect(edges);
                var area = Cv2.ContourArea(edges, false);

                //Debug.WriteLine("area " + area + " left " + rect.Left + " right " + rect.Right + " width " + rect.Width);

                // add left corners from 50 % left-most of image
                //if (rect.Left < width * 0.50 && (area > 100 || rect.Width * rect.Height > 100))
                if (rect.Left < width * 0.50 && (area > 10 || rect.Width * rect.Height > 10) && rect.Height > 1 && rect.Width > 1)
                {
                    left.Add(rect.Left);
                }

                // add right corners from 20% right-most of image
                // @todo to filtrovanie z pravej/lavej strany už asi nie je potrebné max pár %
                //if (/*rect.Right > width * 0.5 &&*/ rect.Left != 0 && (area > 100 || rect.Width * rect.Height > 100))
                if (/*rect.Right > width * 0.5 &&*/ rect.Left != 0 && (area > 10 || rect.Width * rect.Height > 10) && rect.Height > 1 && rect.Width > 1)
                {
                    right.Add(rect.Right);
                }
            }

            // filter top 50 %
            var filterLeft = left.Count > 1 ? left.OrderBy(j => j).Take(left.Count * 50 / 100).ToList() : left;
       
            // filter top 50 %
            var filterRight = right.Count > 1 ? right.OrderByDescending(j => j).Take(right.Count * 50 / 100).ToList() : right;

            double mostLeft, mostRight;

            // if detected width is below 1200 we can take first and last values
            if (filterRight.FirstOrDefault() - filterLeft.FirstOrDefault() <= (int) Layout.LayoutWidth.W1600)
            {
                mostLeft = filterLeft.FirstOrDefault();
                mostRight = filterRight.FirstOrDefault();
            }
            // otherwise we will take most common values
            else
            {
                // @todo mostCommon asi nebude vždy dobre fungovať
                mostLeft = left.Count > 0 ? filterLeft.First() : 0;
                //mostLeft = left.Count > 0 ? filterLeft.MostCommon() : 0;
                mostRight = right.Count > 0 ? filterRight.First() : width;
                //mostRight = right.Count > 0 ? filterRight.MostCommon() : width;
            }

            Debug.WriteLine("most left: " + mostLeft);
            Debug.WriteLine("most right: " + mostRight);

            // most left position must be placed approx. within 5% of layout width
            var type = mostLeft < width * 0.05 ? Layout.LayoutType.Fluid : Layout.LayoutType.Centered;
            Debug.WriteLine("type " + type);
            return new Layout(type, mostRight - mostLeft, height, mostLeft, mostRight);
        }
    }
}
