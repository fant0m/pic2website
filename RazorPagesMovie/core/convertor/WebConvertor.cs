using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RazorPagesMovie.core.model;

namespace RazorPagesMovie.core.convertor
{
    public class WebConvertor : IConvertor
    {
        public string Convert(TemplateStructure templateStructure)
        {
            string htmlStart = "<!DOCTYPE html><html><head><link href=\"./style.css\" rel=\"stylesheet\"><meta charset=\"UTF-8\"><title>Test</title></head><body>";
            string htmlBody = "";
            string htmlEnd = "</body></html>";

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
                var startTag = section.StartTag();
                var endTag = section.EndTag();

                if (i == header)
                {
                    startTag = startTag.Replace("section", "header");
                    endTag = endTag.Replace("section", "header");
                }
                else if (i == footer)
                {
                    startTag = startTag.Replace("section", "footer");
                    endTag = endTag.Replace("section", "footer");
                }

                htmlBody += startTag;
                htmlBody += section.Content();
                htmlBody += endTag;
            }

            return htmlStart + htmlBody + htmlEnd;
        }
    }
}
