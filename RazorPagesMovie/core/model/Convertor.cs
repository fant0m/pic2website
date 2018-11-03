using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesMovie.core.model
{
    public static class Convertor
    {
        public static String Convert(TemplateStructure templateStructure)
        {
            String htmlStart = "<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><title>Test</title><style>section { width: 100%; }</style></head><body>";
            String htmlBody = "";
            String htmlEnd = "</body></html>";

            foreach (var section in templateStructure.Sections)
            {
                htmlBody += section.StartTag();

                foreach (var container in section.Containers)
                {
                    htmlBody += container.StartTag();

                    foreach (var element in container.Elements)
                    {
                        htmlBody += element.StartTag();
                        htmlBody += element.Body();
                        htmlBody += element.EndTag();
                    }

                    htmlBody += container.EndTag();
                }

                htmlBody += section.EndTag();
            }

            return htmlStart + htmlBody + htmlEnd;
        }
    }
}
