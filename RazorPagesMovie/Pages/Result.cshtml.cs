using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesMovie.Pages
{
    public class ResultModel : PageModel
    {
        public string Path { get; set; }

        public void OnGet()
        {
            Path = "/output/" + @RouteData.Values["UUid"];
        }

        public ActionResult OnGetDownloadFiles()
        {
            return File("/output/" + @RouteData.Values["UUid"] + "/web.zip", "application/zip", "web.zip");
        }
    }
}