using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenCvSharp;
using OpenCvSharp.Text;
using RazorPagesMovie.core;
using RazorPagesMovie.core.convertor;
using RazorPagesMovie.core.model;
using RazorPagesMovie.core.model.elements;
using RazorPagesMovie.core.model.elements.basic;
using RazorPagesMovie.core.model.elements.grid;
using Tesseract;
using Image = RazorPagesMovie.core.model.elements.basic.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;
using Point = OpenCvSharp.Point;

namespace RazorPagesMovie.Pages
{
    public class TestModel : PageModel
    {
        public string Output { get; set; }

        public void OnGet()
        {
            //var ocr = new Ocr();
            //var text = ocr.GetText("image-1.png");
            //Debug.WriteLine("texttt " + text.GetText());

            //var tess = new TesseractEngine(@"./wwwroot/tessdata", "eng", EngineMode.LstmOnly);
            //using (var page = tess.Process(Pix.LoadFromFile("image-17.png"), PageSegMode.SingleLine))
            //{
            //    Debug.WriteLine("text == " + page.GetText());
            //}


            /*var result = OCR.DetectFontSize(60, 80, "courier new", "Hello");
            Debug.WriteLine(result);
            Debug.WriteLine(OCR.PointsToPixels(result));*/

            /*Font font = new Font("courier new", 90);
            Debug.WriteLine("font height " + font.Height);
            Bitmap fakeImage = new Bitmap(500, 500);
            Graphics g = Graphics.FromImage(fakeImage);
            g.DrawString("Hello", font, new SolidBrush(Color.White), 0, 0);
            g.PageUnit = GraphicsUnit.Pixel;
            Debug.WriteLine(g.DpiX + "," + g.DpiY);
            Graphics graphics = Graphics.FromImage(fakeImage);
            graphics.PageUnit = GraphicsUnit.Pixel;
            SizeF size = graphics.MeasureString("Hello", font, Int32.MaxValue, StringFormat.GenericTypographic);
            Debug.WriteLine("sizes " + size.Width + "," + size.Height);
            fakeImage.Save("hm.jpg", ImageFormat.Jpeg);*/


            Output = TestParser();
            //Output = TestConvertor();


            /*using (var engine = new TesseractEngine("C:/Users/tomsh/source/repos/RazorPagesMovie/RazorPagesMovie/tessdata", "eng", EngineMode.Default))
            {
                // have to load Pix via a bitmap since Pix doesn't support loading a stream.
                using (var image = new System.Drawing.Bitmap("C:/Users/tomsh/source/repos/RazorPagesMovie/RazorPagesMovie/skus.png"))
                {
                    using (var pix = PixConverter.ToPix(image))
                    {
                        using (var page = engine.Process(pix))
                        {
                            //meanConfidenceLabel.InnerText = String.Format("{0:P}", page.GetMeanConfidence());
                            //resultText.InnerText = page.GetText();
                            Console.WriteLine(page.GetText());
                        }
                    }
                }
            }*/

            //Test3();


            //OCRTesseract ocr = OCRTesseract.Create("C:/users/tomsh/desktop/", "eng", psmode: 1);
            //ocr.Run(new Mat("C:/Users/tomsh/source/repos/RazorPagesMovie/RazorPagesMovie/skus.png"), out string text, out var componentRects, out var componentTexts, out var componentConfidences);
            //Console.WriteLine(text);


        }

        private void Test3()
        {
            byte[] imageData = System.IO.File.ReadAllBytes(@"./wwwroot/images/template2.png");
            Mat img1 = Mat.FromImageData(imageData, ImreadModes.Color);
            Mat gray1 = Mat.FromImageData(imageData, ImreadModes.Grayscale);

            //gray1 = gray1.GaussianBlur (new OpenCvSharp.Size(3, 3), 0);
            gray1 = gray1.AdaptiveThreshold(255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 105, 2); // 11,2 ; 75,10 ; 60,255
            //gray1 = gray1.Threshold(60, 255, ThresholdTypes.BinaryInv);


            //Canny Edge Detector
            //Image<Gray, Byte> cannyGray = gray1.Canny(20, 50);
            //Image<Bgr, Byte> imageResult = img1.Copy();
            //Mat cannyGray = gray1.Canny(20, 35); // 0, 12, blur 9; 2, 17,  blur 7; 0, 25 blur 13; 20 35 blur 0
            //var cannyGray = gray1;


            //Cv2.FindContours(cannyGray, out var contours, out var hierarchy, mode: RetrievalModes.Tree, method: ContourApproximationModes.ApproxSimple);


            Mat copy = gray1.Clone();
            copy.SaveImage("wwwroot/images/output.png");
        }

        private string TestParser()
        {
            var templateParser = new TemplateParser("test5_1.png");

            return templateParser.Analyse();
        }

        private string TestConvertor()
        {
            /*var layout = new Layout(Layout.LayoutType.Centered, 750, 500);
            var structure = new TemplateStructure(layout);

            var section = new Section(1);
            var container = new Container(1);
            var text = new Text("test");
            var image = new Image("https://www.freeiconspng.com/uploads/format-png-image-resolution-3580x3402-size-1759-kb-star-png-image-star--6.png");
            var row = new Row(1);
            var col = new Column(1);
            row.Columns.Add(col);
            col.Elements.Add(text);
            col.Elements.Add(image);
            container.Rows.Add(row);
            section.Containers.Add(container);
            structure.Sections.Add(section);

            var convertor = new WebConvertor();
            return convertor.Convert(structure);*/
            return "";
        }

        private string Test()
        {
           /* var tess = new TesseractEngine(@"./wwwroot/tessdata", "eng", EngineMode.LstmOnly);
            
            byte[] imageData = System.IO.File.ReadAllBytes(@"./wwwroot/images/menu.png");
            Mat img1 = Mat.FromImageData(imageData, ImreadModes.Color);
            //Convert the img1 to grayscale and then filter out the noise
            Mat gray1 = Mat.FromImageData(imageData, ImreadModes.GrayScale)/*.PyrDown().PyrUp()*/
            ;
            /*
            // @todo neberie to šedú farbu takže asi menší blur / iné canny hodnoty
            gray1 = gray1.GaussianBlur (new OpenCvSharp.Size(3, 3), 0);
            //gray1 = gray1.AdaptiveThreshold(255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 105, 2); // 11,2 ; 75,10 ; 60,255
            //gray1 = gray1.Threshold(60, 255, ThresholdTypes.BinaryInv);

            Console.WriteLine(img1.Width + img1.Height);

            //Canny Edge Detector
            //Image<Gray, Byte> cannyGray = gray1.Canny(20, 50);
            //Image<Bgr, Byte> imageResult = img1.Copy();
            Mat cannyGray = gray1.Canny(20, 35); // 0, 12, blur 9; 2, 17,  blur 7; 0, 25 blur 13; 20 35 blur 0
            //var cannyGray = gray1;

            // treba aj GaussianBlur, adaptiveThreshold


            Random r = new Random();
            int lastY = 0;

            Cv2.FindContours(cannyGray, out var contours, out var hierarchy, mode: RetrievalModes.Tree, method: ContourApproximationModes.ApproxSimple);

            var layout = DetectLayout(contours, hierarchy, cannyGray.Width, cannyGray.Height);
            var structure = new TemplateStructure(layout);


            Mat copy = img1.Clone();
            Cv2.DrawContours(copy, contours, -1, Scalar.Orange);

            var j = 0;
            var count = 0;
            while (j != -1)
            {
                var index = hierarchy[j];
                if (index.Parent != -1)
                {
                    j = index.Next;
                    continue;
                }

                Scalar scalar = Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                Cv2.DrawContours(copy, contours, j, scalar);
                count++;

                j = index.Next;
            }
            Debug.WriteLine("Poèet" + count);

            /*for (var j = contours.Length - 1; j >= 0; j--)
            {
                Scalar scalar = Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                Cv2.DrawContours(copy, contours, j, scalar);
            }*/
            /*
            copy.SaveImage("wwwroot/images/output.png");


            Debug.WriteLine("poèet " + contours.Length);
            
            Debug.WriteLine(hierarchy.Length);
            Debug.WriteLine(hierarchy[0].Next);

            var limit = 0;
            var i = 0;
            while (i != -1)
            {
                // Find current item
                var index = hierarchy[i];

                // Filter only outer contours
                if (index.Parent != -1)
                {
                    i = index.Next;
                    continue;
                }

                limit++;

                // Edges
                var edges = contours[i];

                // Bounding box
                var rect = Cv2.BoundingRect(edges);

                // Area
                var area = Cv2.ContourArea(edges, false);

                // Polygon Approximations
                var contoursAp = Cv2.ApproxPolyDP(edges, Cv2.ArcLength(edges, true) * 0.05, true);


                var roi = img1.Clone(rect);
                roi.SaveImage("image-" + i + ".png");


                Debug.WriteLine(contoursAp.Length);
                Debug.WriteLine("left: " + rect.Left + ", top: " + rect.Top);

                // Process only outer structure
                if (limit == 9999999)
                {
                    i = -1;
                }
                else
                {
                    i = index.Next;
                }
            }


            return Convertor.Convert(structure);

            /*
                   for (int i = contours.Length - 1; i >= 0; i--)
                   {
                       Point[] edges = contours[i];
                       Point[] normalizedEdges = Cv2.ApproxPolyDP(edges, 17, true);
                       Boolean section = false;
                       Color color = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                       //MCvScalar color = new MCvScalar(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                       //MCvScalar color = new MCvScalar(255, 0, 0);

                       //Polygon Approximations
                       Point[] contoursAp = Cv2.ApproxPolyDP(edges, Cv2.ArcLength(edges, true) * 0.05, true);

                       // Area
                       double area = Cv2.ContourArea(edges, false);

                       // Bounding box
                       Rect rect = Cv2.BoundingRect(edges);

                       var roi2 = img1.Clone(rect);

                       //Mat roi = new Mat();
                       //gray1.CopyTo(roi, rect);

                       roi2.SaveImage("image-" + i + ".png");


                       Console.WriteLine(contoursAp.Length);


                       Boolean check = true;

                       // Check for horizontal lines
                       if (rect.X == 0 && Math.Abs(area) <= 1 && rect.Width > 100)
                       {
                           check = false;

                           if (contoursAp.Length == 2 && rect.Height <= 10)
                           {
                               //CvInvoke.DrawContours(imageResult, contours, i, color);

                               //Console.WriteLine(i + " " + color.V0 + " " + color.V1 + " " + color.V2 + " " + color.V3);
                               //Console.WriteLine("ap " + contoursAP.Size);
                               //Console.WriteLine("area " + CvInvoke.ContourArea(contours[i], true));
                               //Console.WriteLine("width " + rect.Width + " height " + rect.Height + " x " + rect.X + " y " + rect.Y);
                               //Console.WriteLine();

                               htmlBody += $"<section style=\'height:{rect.Y - lastY}px;background:rgb({color.R},{color.G},{color.B});\'>";

                               lastY = rect.Y;
                               section = true;
                           }
                           else if (contoursAp.Length == 4 && rect.Height > 10)
                           {
                               //CvInvoke.DrawContours(imageResult, contours, i, color);

                               //Console.WriteLine(i + " " + color.V0 + " " + color.V1 + " " + color.V2 + " " + color.V3);
                               //Console.WriteLine("ap " + contoursAP.Size);
                               //Console.WriteLine("area " + CvInvoke.ContourArea(contours[i], true));
                               //Console.WriteLine("width " + rect.Width + " height " + rect.Height + " x " + rect.X + " y " + rect.Y);
                               //Console.WriteLine();

                               htmlBody += $"<section style=\'height:{rect.Height}px;background:rgb({color.R},{color.G},{color.B});\'>";

                               lastY = rect.Y + rect.Height;
                               section = true;
                           }
                           else
                           {
                               check = true;
                           }
                       }

                       // Check for text
                       if (check && rect.Width * 2 + rect.Height * 2 > 50 && contoursAp.Length >= 2)
                       {
                           //Console.WriteLine("text check " + i);
                           //Bitmap cloneBitmap = img1.ToBitmap().Clone(rect, PixelFormat.DontCare);
                           //Image<Bgr, Byte> imageCV = new Image<Bgr, byte>(cloneBitmap);
                           var roi = img1.Clone(rect);

                           //Mat roi = new Mat();
                           //gray1.CopyTo(roi, rect);

                           //roi.SaveImage("Image-" + i + ".png");

                           //pictureBox1.Image = cloneBitmap;
                           //var segment = Pix.LoadTiffFromMemory(roi.ToBytes());
                           //var stream = roi.ToMemoryStream();
                           //var seg = Pix.LoadTiffFromMemory(stream.ToArray());

                           using (var page = tess.Process(Pix.LoadFromFile("Image-" + i + ".png"), PageSegMode.SingleBlock))
                           {
                               var text = page.GetText();

                               Console.WriteLine(text);

                               //ocr.Run(roi, out string text, out var componentRects, out var componentTexts, out var componentConfidences);
                               Console.WriteLine("vysledok " + text);

                               if (text.Length >= 3)
                               {
                                   htmlBody += $"<span>{text.Trim()}</span>";
                               }
                           }
                       }


                       // Close sections
                       if (section)
                       {
                           htmlBody += "</section>";
                       }
                   }
                   */
            //htmlBody += $"<section style=\'height:{cannyGray.Height - lastY}px;background:rgb(255, 0, 0);\'></section>";


            return "";
        }
    }
}