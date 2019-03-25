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
        public List<Tuple<string, string, string>> SampleTemplates = new List<Tuple<string, string, string>>() {
            new Tuple<string, string, string>("images/template4.png", "Website template", "1920x3905px, expected wait time 1 minute"),
            new Tuple<string, string, string>("images/github.png", "Github.com screenshot", "1898x934px, expected wait time 1 minute"),
            new Tuple<string, string, string>("images/discover-greece.jpg", "Discover Greece template", "1470x5560px, expected wait time 1 minute"),
            new Tuple<string, string, string>("images/jaspravim.png", "Jaspravim.sk screenshot", "1920x3660px, expected wait time 1 minute"),
            new Tuple<string, string, string>("images/template12.png", "E-shop template", "1500x2187px, expected wait time 1 minute"),
            new Tuple<string, string, string>("images/email-template.png", "E-mail template", "800x1816px, expected wait time 30 seconds"),

            //new Tuple<string, string, string>("images/template15.png", "Website template", "1400x5612px, expected wait time 1 minute"), nie až také dokonalé ale ani také zlé len ak bude málo ukážok
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

            // create output directories
            Directory.CreateDirectory(path);
            Directory.CreateDirectory(path + "images/");

            // requested image is sample one
            if (Request.Form.ContainsKey("sample-submit"))
            {
                if (!string.IsNullOrEmpty(Request.Form["sample"]))
                {
                    requestPath = "wwwroot/" + Request.Form["sample"];
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
                    var upload = new FileStream(path + "upload.png", FileMode.Create);
                    file.CopyTo(upload);
                    upload.Close();

                    requestPath = path + "upload.png";
                }
                else
                {
                    error = "You forgot to upload an image!";
                }
            }

            // we are good to go for template parsing
            if (requestPath != "" && error == "")
            {
                try
                {
                    var templateParser = new TemplateParser(requestPath, uuid);

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
