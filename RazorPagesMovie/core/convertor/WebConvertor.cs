using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RazorPagesMovie.core.model;

namespace RazorPagesMovie.core.convertor
{
    public class WebConvertor : IConvertor
    {
        private readonly TemplateStructure templateStructure;

        public WebConvertor(TemplateStructure templateStructure)
        {
            this.templateStructure = templateStructure;
        }

        public string Convert()
        {
            //long timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            long timestamp = 0;

            string htmlStart = $"<!DOCTYPE html>\n<html>\n<head>\n\t<link href=\"./style.css\" rel=\"stylesheet\">\n\t<link href=\"./custom-{timestamp}.css\" rel=\"stylesheet\">\n\t<meta charset=\"utf-8\" />\n\t<title>Test</title>\n</head>\n<body>\n";
            string htmlBody = "";
            string htmlEnd = "</body>\n</html>";
            string styles = "";

            int header = -1;
            int footer = -1;
            if (templateStructure.Sections.Count > 2)
            {
                var firstHeight = templateStructure.Sections[0].Height;
                var secondHeight = templateStructure.Sections[1].Height;
                var lastHeight = templateStructure.Sections.Last().Height;

                if (firstHeight <= 70 && secondHeight <= 220)
                {
                    header = 1;
                }
                else if (firstHeight <= 220)
                {
                    header = 0;
                }

                if (lastHeight <= 500)
                {
                    footer = templateStructure.Sections.Count - 1;
                }
            }

            for (var i = 0; i < templateStructure.Sections.Count; i++)
            {
                var section = templateStructure.Sections[i];

                if (i == header)
                {
                    section.Tag = "header";
                    section.Id = 0;
                }
                else if (i == footer)
                {
                    section.Tag = "footer";
                    section.Id = 0;
                }

                styles += section.GetStyleSheet("");

                htmlBody += section.StartTag(1);
                htmlBody += section.Content(2);
                htmlBody += section.EndTag(1);
            }

            // save custom styles
            using (var tw = new StreamWriter("wwwroot/custom-" + timestamp + ".css"))
            {
                tw.Write(styles);
                tw.Close();
            }

            return htmlStart + htmlBody + htmlEnd;
        }
    }
}
