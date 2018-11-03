using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesMovie.Pages
{
    public class IndexModel : PageModel
    {
        public bool Test { get; set; }

        public void OnGet()
        {
            Test = System.IO.File.Exists(@"./wwwroot/tessdata/eng.traineddata");

            Response.Redirect("/Test");
        }

    }
}
