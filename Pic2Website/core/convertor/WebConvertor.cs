using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ionic.Zip;
using Ionic.Zlib;
using Pic2Website.core.model;

namespace Pic2Website.core.convertor
{
    public class WebConvertor : IConvertor
    {
        private TemplateStructure templateStructure;

        private string path;

        private string htmlStart;
        private string htmlBody;
        private string htmlEnd;
        private string styles;

        public WebConvertor(string uuid)
        {
            path = "wwwroot/output/" + uuid + "/";
        }

        public void SetTemplateStructure(TemplateStructure templateStructure)
        {
            this.templateStructure = templateStructure;
        }

        public void Convert()
        {
            htmlStart = $"<!DOCTYPE html>\n<html>\n<head>\n\t<link href=\"./style.css\" rel=\"stylesheet\">\n\t<link href=\"./custom.css\" rel=\"stylesheet\">\n\t<meta charset=\"utf-8\" />\n\t<title>Website</title>\n</head>\n<body>\n";
            htmlBody = "";
            htmlEnd = "</body>\n</html>";
            styles = "";

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
        }

        public string GetContentPath()
        {
            return path;
        }

        public void Save()
        {
            // save html content
            using (var tw = new StreamWriter(path + "index.html"))
            {
                tw.Write(htmlStart + htmlBody + htmlEnd);
                tw.Close();
            }
            
            // save custom styles
            using (var tw = new StreamWriter(path + "custom.css"))
            {
                tw.Write(styles);
                tw.Close();
            }

            // copy static styles
            File.Copy("wwwroot/style.css", path + "style.css");

            // zip all files
            using (ZipFile zip = new ZipFile())
            {
                zip.CompressionLevel = CompressionLevel.None;
                zip.AddDirectory(path);
                zip.Save(path + "web.zip");
            }
        }
    }
}
