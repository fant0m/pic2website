using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Pic2Website.core;

namespace Pic2Website.Pages
{
    public class IndexModel : PageModel
    {
        public List<Tuple<string, string, string, string>> SampleTemplates = new List<Tuple<string, string, string, string>>() {
            new Tuple<string, string, string, string>("images/template4.png", "Portfolio template", "1920x3905px, expected wait time 1 minute", "eng"),
            new Tuple<string, string, string, string>("images/discover-greece.jpg", "Discover Greece template", "1470x5560px, expected wait time 1 minute", "eng"),
            new Tuple<string, string, string, string>("images/lms.png", "Learning management system template", "1920x5758px, expected wait time 1 minute", "eng"),
            new Tuple<string, string, string, string>("images/template12.png", "E-shop template", "1500x2187px, expected wait time 1 minute", "eng"),
            new Tuple<string, string, string, string>("images/email-template.png", "E-mail template", "800x1816px, expected wait time 30 seconds", "eng"),
            new Tuple<string, string, string, string>("images/maker.png", "Agency template", "1580x5290px, expected wait time 1 minute", "eng"),
            new Tuple<string, string, string, string>("images/eshop.png", "E-shop 2 template", "1280x1892px, expected wait time 30 seconds", "eng"),
            new Tuple<string, string, string, string>("images/tinyone-1.png", "Simple landing page template", "1400x2280px, expected wait time 1 minute", "eng"),
            new Tuple<string, string, string, string>("images/jaspravim.png", "Jaspravim.sk screenshot", "1920x3660px, expected wait time 1 minute", "slk"),
            new Tuple<string, string, string, string>("images/github.png", "Github.com screenshot", "1898x934px, expected wait time 1 minute", "eng"),
            new Tuple<string, string, string, string>("images/fb.png", "Facebook.com screenshot", "1920x1065px, expected wait time 1 minute", "slk"),
            new Tuple<string, string, string, string>("images/zive.png", "Zive.sk screenshot", "1920x4674px, expected wait time 1 minute", "slk"),

            //new Tuple<string, string, string>("images/template15.png", "Website template", "1400x5612px, expected wait time 1 minute"), nie až také dokonalé ale ani také zlé len ak bude málo ukážok
        };

        public List<KeyValuePair<string, string>> Languages = new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>("eng", "English"),
            new KeyValuePair<string, string>("slk", "Slovak"),
        };

        public void OnGet()
        {
            //Response.Redirect("/Test");
        }

        public JsonResult OnPost()
        {
            // generate unique id
            var uuid = Guid.NewGuid().ToString();
            var path = "wwwroot/output/" + uuid + "/";
            string requestPath = "";
            string error = "";
            string language = "";

            // create output directories
            Directory.CreateDirectory(path);
            Directory.CreateDirectory(path + "images/");

            // requested image is sample one
            if (Request.Form.ContainsKey("sample-submit"))
            {
                if (!string.IsNullOrEmpty(Request.Form["sample"]))
                {
                    System.IO.File.Copy("wwwroot/" + Request.Form["sample"], path + "image.png", true);
                    requestPath = path + "image.png";
                    language = SampleTemplates.Where(t => t.Item1 == Request.Form["sample"]).First().Item4;
                }
                else
                {
                    error = "You forgot to select a sample design!";
                }
            }
            // request image is uploaded one
            else if (Request.Form.ContainsKey("upload-submit"))
            {
                var file = Request.Form.Files["upload"];
                if (file != null)
                {
                    var upload = new FileStream(path + "image.png", FileMode.Create);
                    file.CopyTo(upload);
                    upload.Close();

                    requestPath = path + "image.png";
                }
                else
                {
                    error = "You forgot to upload an image!";
                }

                language = Request.Form["language"];
            }

            // we are good to go for template parsing
            if (requestPath != "" && error == "")
            {
                try
                {
                    var templateParser = new TemplateParser(requestPath, uuid, language);

                    templateParser.Analyse();
                    templateParser.Convert(Response);

                    return new JsonResult(new KeyValuePair<string, string>("success", uuid));
                }
                catch (Exception e)
                {
                    return new JsonResult(new KeyValuePair<string, string>("error", "Whoops. Unfortunately an error has occurred!"));
                }
              
            }
            else if (error != "")
            {
                return new JsonResult(new KeyValuePair<string, string>("error", error));
            }

            return new JsonResult(null);
        }
    }
}
