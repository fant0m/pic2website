using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Pic2Website.Pages
{
    public class ResultModel : PageModel
    {
        public string Path { get; set; }
        public string Html { get; set; }
        public string Css { get; set; }

        public void OnGet()
        {
            Path = "/output/" + @RouteData.Values["UUid"];
            Html = System.IO.File.ReadAllText(@"wwwroot" + Path + "/" + "index.html");
            Css = System.IO.File.ReadAllText(@"wwwroot" + Path + "/" + "custom.css");
        }

        public ActionResult OnGetDownloadFiles()
        {
            return File("/output/" + @RouteData.Values["UUid"] + "/web.zip", "application/zip", "web.zip");
        }
    }
}