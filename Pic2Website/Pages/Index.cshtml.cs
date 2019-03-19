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
            new Tuple<string, string, string>("images/template4.png", "template4", "1200x800px, expected wait time 1 minute"),
            new Tuple<string, string, string>("images/github.png", "github", "1500x900px, expected wait time 2 minutes")
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
