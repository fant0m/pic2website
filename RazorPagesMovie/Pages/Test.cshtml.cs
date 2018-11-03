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
using RazorPagesMovie.core.model;
using RazorPagesMovie.core.model.elements;
using RazorPagesMovie.core.model.elements.basic;
using Tesseract;
using Image = RazorPagesMovie.core.model.elements.basic.Image;
using Point = OpenCvSharp.Point;

namespace RazorPagesMovie.Pages
{
    public class TestModel : PageModel
    {
        public string Output { get; set; }

        public void OnGet()
        {
            Output = Test();
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




            //OCRTesseract ocr = OCRTesseract.Create("C:/users/tomsh/desktop/", "eng", psmode: 1);
            //ocr.Run(new Mat("C:/Users/tomsh/source/repos/RazorPagesMovie/RazorPagesMovie/skus.png"), out string text, out var componentRects, out var componentTexts, out var componentConfidences);
            //Console.WriteLine(text);


        }

        private string TestConvertor()
        {
            var layout = new Layout(Layout.LayoutType.Centered, 750, 500);
            var structure = new TemplateStructure(layout);

            var section = new Section(1);
            var container = new Container(1);
            var text = new Text("test");
            var image = new Image("https://www.freeiconspng.com/uploads/format-png-image-resolution-3580x3402-size-1759-kb-star-png-image-star--6.png");
            container.Elements.Add(text);
            container.Elements.Add(image);
            section.Containers.Add(container);
            structure.Sections.Add(section);

            return Convertor.Convert(structure);
        }

        private string Test()
        {
            var tess = new TesseractEngine(@"./wwwroot/tessdata", "eng", EngineMode.LstmOnly);
            
            byte[] imageData = System.IO.File.ReadAllBytes(@"./wwwroot/images/template2.png");
            Mat img1 = Mat.FromImageData(imageData, ImreadModes.Color);
            //Convert the img1 to grayscale and then filter out the noise
            Mat gray1 = Mat.FromImageData(imageData, ImreadModes.GrayScale)/*.PyrDown().PyrUp()*/;
            gray1 = gray1.GaussianBlur (new OpenCvSharp.Size(13, 13), 0);
            //gray1 = gray1.AdaptiveThreshold(255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 105, 2); // 11,2 ; 75,10 ; 60,255
            //gray1 = gray1.Threshold(60, 255, ThresholdTypes.BinaryInv);

            Console.WriteLine(img1.Width + img1.Height);

            //Canny Edge Detector
            //Image<Gray, Byte> cannyGray = gray1.Canny(20, 50);
            //Image<Bgr, Byte> imageResult = img1.Copy();
            Mat cannyGray = gray1.Canny(0, 25); // 0, 12, blur 9
            //var cannyGray = gray1;

            // treba aj GaussianBlur, adaptiveThreshold

            String htmlStart = "<!DOCTYPE html><html><head><meta charset='UTF-8'><title>Test</title><style>section { width: 100%; }</style></head><body>";
            String htmlBody = "";
            String htmlEnd = "</body></html>";

            Random r = new Random();
            int lastY = 0;

            Point[][] contours; //vector<vector<Point>> contours;
            
            HierarchyIndex[] hierarchy; //vector<Vec4i> hierarchy;
            int draw = 0;

            Cv2.FindContours(cannyGray, out contours, out hierarchy, mode: RetrievalModes.Tree, method: ContourApproximationModes.ApproxSimple);

            Layout layout = DetectLayout(contours, hierarchy, cannyGray.Width, cannyGray.Height);

            Mat copy = img1.Clone();
            //Cv2.DrawContours(copy, contours, -1, Scalar.Aqua);
            
            for (int i = contours.Length - 1; i >= 0; i--)
            {
                Scalar scalar = Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                Cv2.DrawContours(copy, contours, i, scalar);
            }
            
            copy.SaveImage("wwwroot/images/output.png");


            Debug.WriteLine("poèet " + contours.Length);
            
            Debug.WriteLine(hierarchy.Length);
            Debug.WriteLine(hierarchy[0].Next);


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

                       if (draw == 5)
                       {
                           //CvInvoke.DrawContours(imageResult, contours, i, color);


                           //Console.WriteLine(i + " " + color.V0 + " " + color.V1 + " " + color.V2 + " " + color.V3);
                           //Console.WriteLine("ap " + contoursAP.Size);
                           //Console.WriteLine("area " + CvInvoke.ContourArea(contours[i], true));
                           //Console.WriteLine("width " + rect.Width + " height " + rect.Height + " x " + rect.X + " y " + rect.Y);
                           //Console.WriteLine();

                           //Console.WriteLine((rect.Width * 2 + rect.Height * 2 > 40 && contoursAP.Size >= 4));
                       }
                       draw++;



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
            htmlBody += $"<section style=\'height:{cannyGray.Height - lastY}px;background:rgb(255, 0, 0);\'></section>";


            return htmlStart + htmlBody + htmlEnd;
        }

        /**
         * Detect type of template design and it's dimensions
         * 
         * @todo lepší algoritmus na h¾adanie oboch súradníc
         */
        private Layout DetectLayout(Point[][] contours, HierarchyIndex[] hierarchy, double width, double height)
        {
            var left = new List<double>();
            var right = new List<double>();

            var i = 0;
            while (i != -1)
            {
                // find current item
                var index = hierarchy[i];
                var item = contours[i];

                // find edges & boundix box
                var edges = contours[i];
                var rect = Cv2.BoundingRect(edges);
                var area = Cv2.ContourArea(edges, false);

                // add left corners from 25 % left-most of image
                if (rect.Left < width * 0.25 && area > 200)
                {
                    left.Add(rect.Left);
                }

                // add right corners from 20% right-most of image
                if (rect.Right > width * 0.7 && rect.Left != 0 && area > 200)
                {
                    Debug.WriteLine("area " + area + " right " + rect.Right + " width " + rect.Width);
                    right.Add(rect.Right);
                }

                // Process only outer contours
                i = index.Next;
            }

            // filter top 50 % and select most common
            var filterLeft = left.OrderBy(j => j).Take(left.Count * 50 / 100);
            var mostLeft = filterLeft.MostCommon();

            // filter top 50 % and sselect most common
            var filterRight = right.OrderByDescending(j => j).Take(right.Count * 50 / 100);
            var mostRight = filterRight.MostCommon();

            Debug.WriteLine("most left: " + mostLeft);
            Debug.WriteLine("most right: " + mostRight);

            var type = mostLeft < 10 ? Layout.LayoutType.Fluid : Layout.LayoutType.Centered;
            return new Layout(type, mostRight - mostLeft, height);
        }
    }
}