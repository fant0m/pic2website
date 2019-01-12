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
using RazorPagesMovie.core.model.elements.grid;
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
    public class TripleExt<T, X, Y, Z>
    {
        public T Item1 { get; set; }
        public X Item2 { get; set; }
        public Y Item3 { get; set; }
        public Z Element { get; set; }
    }

    public class TemplateParser
    {
        private string _imagePath;
        private Mat _image;
        private Point[][] _contours;
        private HierarchyIndex[] _hierarchy;
        private TemplateStructure _templateStructure;
        private TesseractEngine _tess;
        private int limit = 0;

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

            byte[] imageData = File.ReadAllBytes(@"./wwwroot/images/template2_fluid.png");
            _image = Mat.FromImageData(imageData, ImreadModes.Color);
            //Convert the img1 to grayscale and then filter out the noise
            Mat gray1 = Mat.FromImageData(imageData, ImreadModes.GrayScale);
            // @todo naozaj to chceme blurovať? robí to len bordel a zbytočné contours
            gray1 = gray1.GaussianBlur(new OpenCvSharp.Size(3, 3), 0);

            //Canny Edge Detector
            Mat cannyGray = gray1.Canny(15, 20); // 0, 12, blur 9; 2, 17,  blur 7; 0, 25 blur 13; 20 35 blur 0; 15, 25 blur 3

            Random r = new Random();
            int lastY = 0;

            Cv2.FindContours(cannyGray, out _contours, out _hierarchy, mode: RetrievalModes.Tree, method: ContourApproximationModes.ApproxSimple);

            _templateStructure = new TemplateStructure();

            //var gray2 = Mat.FromImageData(imageData, ImreadModes.GrayScale);
            //gray2 = gray2.AdaptiveThreshold(255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 105, 2);
            //var reduced = gray2.Reduce(ReduceDimension.Row, ReduceTypes.Avg, 1);
            //reduced.SaveImage("wwwroot/images/output.png");
            //Debug.WriteLine("rows " + reduced.Rows + "," + reduced.Cols + "," + gray1.Rows);

            var draw = AnalyzeSections();

            Mat copy = _image.Clone();
            Cv2.DrawContours(copy, _contours, -1, Scalar.Orange);


            Debug.WriteLine("počet " + _contours.Length);




            var convertor = new WebConvertor();
            var output = convertor.Convert(_templateStructure);
            var fileOutpout = output.Replace("src=\".", "src=\"C:/Users/tomsh/source/repos/RazorPagesMovie/RazorPagesMovie/wwwroot");
            fileOutpout = fileOutpout.Replace("href=\".", "href=\"C:/Users/tomsh/source/repos/RazorPagesMovie/RazorPagesMovie/wwwroot");

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
            var lastSectionY = -1;
            var sections = new List<Section>();
            var sectionContours = new Dictionary<int, List<int>>();
            var currentSectionContours = new List<int>();

            var rects = new List<Rect>();

            // @todo filter noise

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
            lastSection.Height = _image.Height - lastSectionY - 1;
            lastSection.Top = lastSectionY + 1;
            lastSection.Rect = new Rect(0, lastSectionY + 1, _image.Width, lastSection.Height);

            // Save section image
            roi = _image.Clone(lastSection.Rect);
            roi.SaveImage("./wwwroot/images/section-" + sectionId + ".png");
            sections.Add(lastSection);
            sectionContours.Add(sectionId, currentSectionContours);

            var copy = _image.Clone();

            // Analyse section layouts
            var lastY = 0;
            var maxLayout = Layout.LayoutWidth.W800;
            var mostLeftSections = new List<double>();
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
                }

                // Save longest layout width
                if (section.Layout.Type == Layout.LayoutType.Centered && section.Layout.Width > maxLayout)
                {
                    maxLayout = section.Layout.Width;
                }
            }

            // Sort mostLeftSection values
            mostLeftSections.Sort();

            // Process sections
            foreach (var section in sections)
            {
                // Make sure each centered section has same container width
                if (section.Layout.Type == Layout.LayoutType.Centered && section.Layout.Width != maxLayout)
                {
                    section.Layout.Width = maxLayout;
                }

                // Access sections rects
                var contours = sectionContours[section.Id];

                // Analyse section background image / color
                // @todo vymyslieť ako sa to má správať pri ďalších elementoch, či tam proste pošlem vždy len Element, alebo aj zoznam ktoré elementy nemá prechádzať atď.
                AnalyseSectionBackground(section, contours);

                // Calculate section left and right padding
                section.Layout.CalculatePadding(mostLeftSections.FirstOrDefault());
                Debug.WriteLine("padding " + section.Layout.PaddingLeft + "," + section.Layout.PaddingRight);


                // Create a container
                var container = new Container(1, section.Layout);
                var containerRect = new Rect(0, section.Top, copy.Width, section.Height);

                // Process inner blocks
                // @todo tak nakoniec bude mať ten lavy a pravy padding container, v process inner blocks si to už budú riešiť rowy samé o koľko sa majú odsadiť ešte zľava, takže nebude treba ani paddingFix, akurát ten containerRect bude lepšie posunúť o ten padding
                // @todo 2019-01-11 ten lavy a pravy sa asi ešte nerieši? takisto ten parameter asi vyhodiť, treba doriešiť aby to bolo pekne odsadené a centrované
                // @todo fluid layout je rozdrbaný, možno tam namiesto marginov dať len tie width
                // @todo vnorené boxy sú rozdrbané to acsascolumn treba inak vyriešiť, tak isto na fluid režime sa neozbrazujú ako fluid
                // @todo samotné obrázky keď sú iba vedľa seba už nemajú padding/margin
                Debug.WriteLine("padding fix " + (section.Layout.Type == Layout.LayoutType.Centered ? new double[] { section.Layout.PaddingLeft, section.Layout.PaddingRight } : null));
                //container.Rows = ProcessInnerBlocks(contours, copy, containerRect, section.Layout.Type == Layout.LayoutType.Centered ? new[] { section.Layout.PaddingLeft, section.Layout.PaddingRight } : null);
                // @todo uncomment below
                container.Rows = ProcessInnerBlocks(contours, copy, containerRect, section.Layout.Type == Layout.LayoutType.Fluid, new[] { 0.0, 0.0 });

                // Append container to section
                section.Containers.Add(container);

                // Draw section
                Debug.WriteLine("section " + lastY + "-" + (lastY + section.Height));
                Cv2.Rectangle(copy, new Point(0, lastY), new Point(copy.Width, lastY + section.Height), Scalar.Red);
                lastY += section.Height;

                // Append section into template structure
                _templateStructure.Sections.Add(section);
            }

            copy.SaveImage("wwwroot/images/output.png");

            return startIndex;
        }

        // @todo asi vlastná trieda, kde budú metódy na seckciu a elementy zvlášť
        private void AnalyseSectionBackground(Section section, List<int> contours)
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

            Vec3b[] colors;

            if (section.Layout.Type == Layout.LayoutType.Centered)
            {
                // we want to analyse pixels outside of the container
                var spaceWidth = (section.Rect.Width - (int)section.Layout.Width) / 2;
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
                    colors[i] = _image.At<Vec3b>(y, x1);
                    colors[i + 10] = _image.At<Vec3b>(y, x2);
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
                        colors[found] = _image.At<Vec3b>(y, x);
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

        /**
         * Check if contour doesn't have more blocks inside it
         */
        private List<Row> CheckSubBlocks(int contour, Mat copy)
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
                            // Recursive call with child element
                            // @todo zmazať - tuto rekurzívne do n-tej úrovne už nejdeme
                           // CheckSubBlocks(find, copy);
                        }
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
                    List<Row> rows = ProcessInnerBlocks(inner, copy, rect);

                    return rows;
                }
            }

            return null;
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

        private List<Row> ProcessInnerBlocks(List<int> contours, Mat copy, Rect parent, bool fluid = false, double[] paddingFix = null)
        {
            var r = new Random();
            var sectionRows = new List<Row>();
            var sectionRects = new Rect[contours.Count];
            var sectionRectRows = new List<Row>[contours.Count];
            var k = 0;
            foreach (var contour in contours)
            {
                // Edges
                var edges = _contours[contour];

                // Bounding box
                var rect = Cv2.BoundingRect(edges);
                sectionRects[k] = rect;

                //Debug.WriteLine("processing sub blocks " + k);
                sectionRectRows[k] = CheckSubBlocks(contour, copy);

                k++;
            }

            // @todo spojenie riadkov ak sú moc blízko
            // @todo spájanie do zvlášť containera ak majú rovnaký štýl - rovnaká výška, medzery, ..
            //Debug.WriteLine("sekcia počet rectov " + sectionRects.Length);

            // make copy of rects
            var sectionRectsUnsorted = sectionRects;

            // align rects from the top to the bottom
            sectionRects = sectionRects.OrderBy(rec => rec.Top).ToArray();

            // analyse section rows
            var rows = new List<TripleExt<int, int, List<Rect>, Element>>(); // y start y end
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
                        var sortedRight = latest.Item3.OrderBy(rec => rec.Right).ToList().Last();

                        // @todo neviem či budeme riešiť aj double alebo sa to zaokrúhli
                        // apply right padding for row
                        latest.Element.Padding[1] = paddingFix != null ? (int) paddingFix[1] : parent.X + parent.Width - (sortedRight.X + sortedRight.Width);

                        // apply left padding for row
                        latest.Element.Padding[3] = paddingFix != null ? (int) paddingFix[0] : sortedLeft.X - parent.X;
                    }

                    // create section row
                    var sectionRow = new Row(1);

                    // set section row styles
                    var latestTop = rows.Count == 0 ? parent.Y : rows.Last().Item2;
                    // @todo bez tej podmienky vždy to tak asi bude
                    var latestBottom = rows.Count == 0 ? parent.Y + parent.Height : parent.Y + parent.Height;
                    var latestLeft = parent.X;
                    sectionRow.Padding[0] = rect.Y - latestTop;

                    if (sectionRects.Length == 1) {
                        sectionRow.Padding[3] = rect.X - latestLeft;
                        sectionRow.Padding[2] = latestBottom - (rect.Y + rect.Height);
                    }
                    //sectionRow.BackgroundColor = new[] { r.Next(0, 255), r.Next(0, 255), r.Next(0, 255) };

                    var triple = new TripleExt<int, int, List<Rect>, Element>
                    {
                        
                        Item1 = rect.Y,
                        Item2 = rect.Y + rect.Height,
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

                // apply bottom padding for row
                last.Element.Padding[2] = parent.Y + parent.Height - last.Item2;

                var sortedLeft = last.Item3.OrderBy(rec => rec.Left).ToList().First();
                var sortedRight = last.Item3.OrderBy(rec => rec.Right).ToList().Last();

                // apply right padding for row
                last.Element.Padding[1] = paddingFix != null ? (int)paddingFix[1] : parent.X + parent.Width - (sortedRight.X + sortedRight.Width);

                // apply left padding for row
                last.Element.Padding[3] = paddingFix != null ? (int)paddingFix[0] : sortedLeft.X - parent.X;
            }

            // proceed rows
            var c = 1;
            foreach (var row in rows)
            {
                //var container = new Container(c);
                var sectionRow = (Row) row.Element;

                // @todo treba spracovať každý riadok do stĺpcov, t.j. taký istý princíp ako riadky - zoradia sa zľava doprava (čo už vlastne je vyššie):
                // @todo zoberiem prvý element, zistím aká je medzera medzi ďalším a poďalším (za podmienky že existuje poďalší), ak sú medzery cca rovnaká (rátam s nejakou odchýlkou) tak ich pridám to 1 stĺpca a prejdem na ďalší
                // @todo potom v sekcii ešte pozriem riadky a zistím či nemajú rovnaké stĺpce (prípadne s odchylkou) a ak hej tak spojím tie riadky
                // @todo riadok teda bude obsahovať zoznam stĺpcov a každý stĺpec bude obsahovať zoznam riadkov, t.j. na každý stĺpec potom znova aplikujem algoritmus delenia na riadky už ale bez stĺpcovania (asi či?)

                /* Columns start */

                // align rects from the left to the right
                var alignedRects = row.Item3.OrderBy(rec => rec.Left).ToArray();

                // analyse section columns
                var fluidPercents = 0.0;
                var columns = new List<TripleExt<int, int, List<Rect>, Element>>(); // x start, x end, list of items
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
                            latestElem.Width = latest.Item2 - latest.Item1;
                            latestElem.Margin[1] = rect.X - latest.Item2;
                            if (fluid)
                            {
                                latestElem.Width = Math.Round(latestElem.Width / _image.Width * 100);
                                latestElem.Margin[1] = (int)Math.Round((100.0 * latestElem.Margin[1] / _image.Width));
                                latestElem.Fluid = true;
                                fluidPercents += latestElem.Width;
                                fluidPercents += latestElem.Margin[1];
                            }
                        }

                        // create new column
                        var column = new Column(1);

                        var triple = new TripleExt<int, int, List<Rect>, Element>
                        {
                            Item1 = rect.X,
                            Item2 = rect.X + rect.Width,
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
                    latestElem.Width = latest.Item2 - latest.Item1;
                    if (fluid)
                    {
                        latestElem.Width = 100 - fluidPercents;
                        latestElem.Fluid = true;
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
                    // when we have nested elements we don't need to analyse single contour
                    var recursiveRows = false;

                    var recursiveAdded = 0;
                    var nonRecursiveItems = new List<Rect>();
                    foreach (var e in column.Item3)
                    {
                        var index = Array.IndexOf(sectionRectsUnsorted, e);
                        //Debug.WriteLine("našiel som index " + index + " počet v zozname " + column.Item3.Count);

                        if (sectionRectRows[index] != null)
                        {
                            recursiveRows = true;
                            recursiveAdded++;
                            //Debug.WriteLine("zoznam nie je prázdny " + sectionRectRows[index].Count);
                            foreach (var columnRow in sectionRectRows[index])
                            {
                                //((Column)column.Element).Elements.Add(columnRow);
                            }
                        }
                        else
                        {
                            //nonRecursiveItems.Add(e);
                        }

                        nonRecursiveItems.Add(e);
                    }

                    //if (recursiveAdded > 1)
                    //{
                    //    Debug.WriteLine("presne tak");
                    //}

                    //if (recursiveRows) break;
                    if (nonRecursiveItems.Count == 0) break;

                    // @todo refactor rows, columns do metód

                    /* Column rows start */

                    // align rects from the top to the bottom
                    //var alignedColumnRects = column.Item3.OrderBy(rec => rec.Top).ToArray();
                    var alignedColumnRects = nonRecursiveItems.OrderBy(rec => rec.Top).ToArray();

                    // detect column rows
                    var columnRows = new List<TripleExt<int, int, List<Rect>, Element>>(); // y start y end
                    foreach (var rect in alignedColumnRects)
                    {
                        var rowIndex = FindRowForRect(columnRows, rect);
                        if (rowIndex == -1)
                        {
                            var columnRow = new Row(1);
                            var triple = new TripleExt<int, int, List<Rect>, Element>
                            {
                                Item1 = rect.Y,
                                Item2 = rect.Y + rect.Height,
                                Item3 = new List<Rect> { rect },
                                Element = columnRow
                            };

                            // apply styles
                            var latestTop = columnRows.Count == 0 ? row.Item1 : columnRows.Last().Item2;
                            columnRow.Padding[0] = rect.Y - latestTop;

                            ((Column)column.Element).Elements.Add(columnRow);
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
                        lastColumnRow.Element.Padding[2] = row.Item2 - lastColumnRow.Item2;
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
                                var gap = Math.Abs(nextRect.X - (currentRect.X + currentRect.Width));
                                var merge = currentRect;

                                // Add current item's width
                                mergedWidths.Add(merge.Width);

                                //while ((distance <= maxTextGap || (currentRect & nextRect).area() > 0) && j + 1 < alignVertical.Length - 1)
                                while (gap <= maxTextGap || merge.IntersectsWith(nextRect))
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

                                    //Debug.WriteLine("merging " + j + " with " + (j+1) + ", distance = " + distance);

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
                                        gap = Math.Abs(nextRect.X - (currentRect.X + currentRect.Width));
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

                                //Debug.WriteLine("p. " + j + " distance " + distance, " gap " + firstGap);
                            }
                        }

                        // Create single column inside row
                        var singleColumn = new Column(1);

                        // Add items to col
                        var lastX = -1;
                        foreach (var rect in connectedHorizontal)
                        {

                            // Element has recursive content so we need to replace it with the right content
                            if (sectionRectsUnsorted.Contains(rect) && sectionRectRows[Array.IndexOf(sectionRectsUnsorted, rect)] != null)
                            {
                                var index = Array.IndexOf(sectionRectsUnsorted, rect);
                                //if (sectionRectRows[index] != null)
                                //{
                                    recursiveRows = true;
                                    recursiveAdded++;
                                    //Debug.WriteLine("zoznam nie je prázdny " + sectionRectRows[index].Count);
                                    foreach (var recursiveRow in sectionRectRows[index])
                                    {
                                        // @todo neviem či sa to vždy bude mať tváriť ako column
                                        recursiveRow.ActAsColumn = true;
                                        if (lastX != -1)
                                        {
                                            recursiveRow.Margin[3] = rect.X - lastX;
                                        }
                                        singleColumn.Elements.Add(recursiveRow);
                                        //((Column)column.Element).Elements.Add(columnRow);
                                    }
                                //}

                                lastX = rect.X + rect.Width;
                            }
                            else
                            {
                                
                            




                                limit++;
                                //if (limit == 100) break;

                                //Debug.WriteLine(limit + "=" + rect.Width + "," + rect.Height);

                                var roi2 = _image.Clone(rect);
                                roi2.SaveImage("wwwroot/images/image-" + limit + ".png");

                                // @todo replace with object recognizer
                                var image = new Image("./images/image-" + limit + ".png");

                                //Debug.WriteLine("margin " + (rect.Y - columnRow.Item1) + "," + (columnRow.Item2 - (rect.Y + rect.Height)));
                                if (lastX != -1)
                                {
                                    image.Margin[3] = rect.X - lastX;
                                
                                }
                                lastX = rect.X + rect.Width;

                                singleColumn.Elements.Add(image);

                            

                                //using (var page = _tess.Process(Pix.LoadFromFile("image-" + limit + ".png"), PageSegMode.SingleBlock))
                                //{
                                //    var text = page.GetText();

                                //    Debug.Write("image " + limit + "=" + text);
                                //}

                                Cv2.Rectangle(copy, new Point(rect.X, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height), Scalar.Purple);

                            }
                        }

                        // Add single column into row
                        ((Row)columnRow.Element).Columns.Add(singleColumn);
                    }
                    
                    /* Column row letters merging end */
                }


                /* Merge columns into logical parts start */
                // @todo keď bude normálna štruktúra v inštanciách
                /*
                Debug.WriteLine("row has " + columns.Count + " columns");
                if (columns.Count > 2)
                {
                    var index = 0;
                    while (index + 2 < columns.Count)
                    {
                        var currentColumn = columns[index];
                        var firstColumn = columns[index + 1];
                        var secondColumn = columns[index + 2];

                        var firstGap = firstColumn.Item1 - currentColumn.Item2;
                        var secondGap = secondColumn.Item1 - firstColumn.Item2;

                        // Compare first and second gap between columns
                        //Debug.WriteLine("gap 1. - 2. " + firstGap + "," + secondGap);
                        if (AreSame(firstGap, secondGap))
                        {
                            // @todo merge 1 2 into one logical parent
                            // @todo bude ten merge ako nejaký atribút id merge a pri generovaní sa len hodia vedľa seba pod 1 element?

                            index++;
                            continue;
                        }

                        // Compare first and third gap between columns
                        if (index + 3 < columns.Count)
                        {
                            var thirdColumn = columns[index + 3];
                            var thirdGap = thirdColumn.Item1 - secondColumn.Item2;

                            //Debug.WriteLine("gap 1. - 3. " + firstGap + "," + secondGap);
                            if (AreSame(firstGap, thirdGap))
                            {
                                // @todo merge 1 2 into one logical parent

                                index++;
                            }
                            // Might be not connected but other columns are further
                            else if (thirdGap > firstGap * 2)
                            {
                                // @todo merge 1 2  into one logical parent

                                index++;
                            }
                        }

                        index++;
                    }
                }*/

                /* Merge columns into logical parts end */


                /* Adjust column widths start */

                // @todo asi width ostanú a len marginy sa nastavia, alebo všetky riadky budú musieť vedieť že majú fixnú width

                /* Adjust column widths end */


                sectionRows.Add(sectionRow);
                c++;
            }

            //Debug.WriteLine("sekcia počet row " + rows.Count);

            foreach (var row in rows)
            {
                Cv2.Rectangle(copy, new Point(parent.X, row.Item1), new Point(parent.X + parent.Width, row.Item2), Scalar.Orange);
            }

            return sectionRows;
        }

        private int FindRowForRect(List<TripleExt<int, int, List<Rect>, Element>> rows, Rect rect)
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

        private int FindColumnForRect(List<TripleExt<int, int, List<Rect>, Element>> columns, Rect rect)
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
            // @todo budeme zisťovať globálne mostLeft a mostRight?
            //return rect.Left <= _mostLeft && rect.Height <= MaxSeparatorHeight && rect.Width > MinSeparatorWidth;
            return rect.Left <= 0 && rect.Height <= MaxSeparatorHeight && rect.Width > MinSeparatorWidth;
        }

        /**
        * Detect type of template layout and it's dimensions
        * 
        * @todo lepší algoritmus na hľadanie oboch súradníc
        * @todo teoreticky skúsiť brať úplne prvý element, alebo skôr hodnota ktorá predstavuje min./max. ohraničenie (ľavý/pravý) pre 90% všetkých hodnôt
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

                // add left corners from 25 % left-most of image
                if (rect.Left < width * 0.25 && (area > 100 || rect.Width * rect.Height > 100))
                {
                    left.Add(rect.Left);
                }

                // add right corners from 20% right-most of image
                // @todo to filtrovanie z pravej/lavej strany už asi nie je potrebné max pár %
                if (/*rect.Right > width * 0.5 &&*/ rect.Left != 0 && (area > 100 || rect.Width * rect.Height > 100))
                {
                    right.Add(rect.Right);
                }
            }

            // filter top 50 % and select most common
            var filterLeft = left.Count > 1 ? left.OrderBy(j => j).Take(left.Count * 50 / 100) : left;
            var mostLeft = left.Count > 0 ? filterLeft.MostCommon() : 0;

            // filter top 50 % and sselect most common
            var filterRight = right.Count > 1 ? right.OrderByDescending(j => j).Take(right.Count * 50 / 100) : right;
            var mostRight = right.Count > 0 ? filterRight.MostCommon() : width;

            Debug.WriteLine("most left: " + mostLeft);
            Debug.WriteLine("most right: " + mostRight);

            // most left position must be placed approx. within 10% of layout width
            var type = mostLeft < width * 0.05 ? Layout.LayoutType.Fluid : Layout.LayoutType.Centered;
            Debug.WriteLine("type " + type);
            return new Layout(type, mostRight - mostLeft, height, mostLeft, mostRight);
        }
    }
}
