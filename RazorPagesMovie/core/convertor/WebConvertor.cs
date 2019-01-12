using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RazorPagesMovie.core.model;

namespace RazorPagesMovie.core.convertor
{
    public class WebConvertor : IConvertor
    {
        public String Convert(TemplateStructure templateStructure)
        {
            String htmlStart = "<!DOCTYPE html><html><head><link href=\"./style.css\" rel=\"stylesheet\"><meta charset=\"UTF-8\"><title>Test</title></head><body>";
            String htmlBody = "";
            String htmlEnd = "</body></html>";

            foreach (var section in templateStructure.Sections)
            {
                htmlBody += section.StartTag();
                htmlBody += section.Content();
                htmlBody += section.EndTag();
            }

            return htmlStart + htmlBody + htmlEnd;
        }
    }
}
