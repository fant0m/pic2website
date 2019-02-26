using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenCvSharp;
using Tesseract;

namespace RazorPagesMovie.Pages
{
    public class GenerateModel : PageModel
    {
        public void OnGet()
        {
            var img = Request.Query["image"];
            var canny1 = Double.Parse(Request.Query["canny1"]);
            var canny2 = Double.Parse(Request.Query["canny2"]);
            var blur = Double.Parse(Request.Query["blur"]);

            Test3(img, canny1, canny2, blur);
            //Test(img, canny1, canny2, blur);

            /**
             * Postup:
             * -zisti ak˝ typ template to je (full page, centrovan˝)
             * -detekcia hlavn˝ch blokov (section - öpeci·lne section header, footer, nav extends section)
             * -detekcia elementov v kaûdom bloku
             * 
             * 
             * pozrieù sa na hierarchiu
             * posp·jaù pÌsmen· podæa veækosti medzery
             * 
             * -triedy ako button, text, image kaûdÈ atrib˙ty ako id (v˝sledok potom bude button-id; image-id; text-id), margin, padding, farba + öpeci·lne takûe bud˙ tieû inheritovaù z nejakÈho ˙plne abstraktnÈho so spoloËn˝mi atrib˙tmi
             * -trieda, ktor· bude drûaù cel˙ ötrukt˙ru hierarchicky webu
             * -t· sa potom poöle inej triede, ktor· ju skonvertuje na html & css (t· bude moûno rieöiù aj sp·janie rovnak˝ch elementov)
             */
        }

        private void Test2(double canny1, double canny2, double blur)
        {
            byte[] imageData = System.IO.File.ReadAllBytes(@"./wwwroot/images/section-4.png");
            Mat img1 = Mat.FromImageData(imageData, ImreadModes.Color);
            //Convert the img1 to grayscale and then filter out the noise
            Mat gray1 = Mat.FromImageData(imageData, ImreadModes.Grayscale).PyrDown().PyrUp();
            //gray1 = gray1.GaussianBlur(new OpenCvSharp.Size(blur, blur), 0);
            var edges = gray1.Canny(canny1, canny2);
            //var lines = edges.HoughLines(1, Math.PI / 180, 250);
            var lines = edges.HoughLinesP(1, Math.PI / 180, 50, 50, 10);
            var r = new Random();

            Mat copy = img1.Clone();
            foreach (var line in lines)
            {
                Scalar scalar = Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                copy.Line(line.P1, line.P2, scalar);


                /*float rho = line.Rho, theta = line.Theta;
                Point pt1, pt2;
                double a = Math.Cos(theta), b = Math.Sin(theta);
                double x0 = a * rho, y0 = b * rho;
                pt1.X = (int) Math.Round(x0 + 1000 * (-b));
                pt1.Y = (int)Math.Round(y0 + 1000 * (a));
                pt2.X = (int)Math.Round(x0 - 1000 * (-b));
                pt2.Y = (int)Math.Round(y0 - 1000 * (a));
                copy.Line(pt1, pt2, scalar);*/
            }
            copy.SaveImage("wwwroot/images/output.png");

        }

        private void Test3(string img, double canny1, double canny2, double blur)
        {
            var tess = new TesseractEngine(@"./wwwroot/tessdata", "eng", EngineMode.LstmOnly);

            byte[] imageData = System.IO.File.ReadAllBytes(@"./wwwroot/images/" + img);
            Mat img1 = Mat.FromImageData(imageData, ImreadModes.Color);
            //Convert the img1 to grayscale and then filter out the noise
            Mat gray1 = Mat.FromImageData(imageData, ImreadModes.Grayscale)/*.PyrDown().PyrUp()*/;
            gray1 = gray1.GaussianBlur(new OpenCvSharp.Size(blur, blur), 0);

            //gray1 = gray1.AdaptiveThreshold(255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.BinaryInv, (int)canny1, canny2); // 11,2 ; 75,10 ; 60,255

            //gray1 = gray1.GaussianBlur(new Size(blur, blur), 0);

            //gray1 = gray1.Threshold(128, 255, ThresholdTypes.Binary);

            //Canny Edge Detector
            //Image<Gray, Byte> cannyGray = gray1.Canny(20, 50);
            //Image<Bgr, Byte> imageResult = img1.Copy();
            Mat cannyGray = gray1.Canny(canny1, canny2);

            cannyGray.SaveImage("wwwroot/images/output.png");
            return;
            //var cannyGray = gray1;

            // treba aj GaussianBlur, adaptiveThreshold

            Random r = new Random();
            //int lastY = 0;

            Point[][] contours; //vector<vector<Point>> contours;

            HierarchyIndex[] hierarchy; //vector<Vec4i> hierarchy;
            //int draw = 0;

            Cv2.FindContours(cannyGray, out contours, out hierarchy, mode: RetrievalModes.Tree, method: ContourApproximationModes.ApproxSimple);

            Debug.WriteLine("poËet - " + contours.Length);

            Mat copy = img1.Clone();
            //Cv2.DrawContours(copy, contours, -1, Scalar.Orange);
            var j = 0;
            while (false && j != -1)
            {
                var index = hierarchy[j];
                if (index.Parent != -1)
                {

                    j = index.Next;
                    continue;
                }

                Scalar scalar = Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                Cv2.DrawContours(copy, contours, j, scalar);

                var edges = contours[j];
                Point[] contoursAp = Cv2.ApproxPolyDP(edges, Cv2.ArcLength(edges, true) * 0.01, true);
                //Debug.WriteLine(j + "," + contoursAp.Length);

                /*var rect = Cv2.BoundingRect(edges);
                var roi2 = img1.Clone(rect);
                roi2.SaveImage("pozri-" + j + ".png");*/

                j = index.Next;
            }

            var m = 0;
            foreach (var c in contours)
            {
                var rect = Cv2.BoundingRect(c);
                Scalar scalar = Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                copy.Rectangle(rect, scalar);
                //var roi2 = img1.Clone(rect);
                //roi2.SaveImage("pozri-" + m + ".png");
                //m++;
            }


            copy.SaveImage("wwwroot/images/output.png");

           
        }

        private string Test(string img, double canny1, double canny2, double blur)
        {
            var tess = new TesseractEngine(@"./wwwroot/tessdata", "eng", EngineMode.LstmOnly);

            byte[] imageData = System.IO.File.ReadAllBytes(@"./wwwroot/images/" + img);
            Mat img1 = Mat.FromImageData(imageData, ImreadModes.Color);
            //Convert the img1 to grayscale and then filter out the noise
            Mat gray1 = Mat.FromImageData(imageData, ImreadModes.Grayscale)/*.PyrDown().PyrUp()*/;
            //gray1 = gray1.GaussianBlur(new OpenCvSharp.Size(blur, blur), 0);
            //gray1 = gray1.AdaptiveThreshold(255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 105, 2); // 11,2 ; 75,10 ; 60,255
            //gray1 = gray1.Threshold(60, 255, ThresholdTypes.BinaryInv);

            Console.WriteLine(img1.Width + img1.Height);

            //Canny Edge Detector
            //Image<Gray, Byte> cannyGray = gray1.Canny(20, 50);
            //Image<Bgr, Byte> imageResult = img1.Copy();
            Mat cannyGray = gray1.Canny(canny1, canny2);
            //var cannyGray = gray1;

            // treba aj GaussianBlur, adaptiveThreshold


            Random r = new Random();
            int lastY = 0;

            Point[][] contours; //vector<vector<Point>> contours;

            HierarchyIndex[] hierarchy; //vector<Vec4i> hierarchy;
            int draw = 0;

            Cv2.FindContours(cannyGray, out contours, out hierarchy, mode: RetrievalModes.Tree, method: ContourApproximationModes.ApproxSimple);

            Mat copy = img1.Clone();
            Cv2.DrawContours(copy, contours, -1, Scalar.Orange);

            /*for (int i = contours.Length - 1; i >= 0; i--)
            {
                Scalar scalar = Scalar.FromRgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
                Cv2.DrawContours(copy, contours, i, scalar);
            }*/


            var j = 0;
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

                j = index.Next;
            }
            copy.SaveImage("wwwroot/images/output.png");


            Debug.WriteLine("poËet " + contours.Length);
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


            return "";
        }
    }
}