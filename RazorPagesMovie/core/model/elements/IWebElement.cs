using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesMovie.core.model.elements
{
    interface IWebElement
    {
        int Id { get; set; }
        string Tag { get; set; }
        bool PairTag { get; set; }
        List<string> ClassNames { get; set; }
        Dictionary<string, string> Attributes { get; set; }
        List<Element> GetSubElements();
        string GetClassAttribute();
        string GetAttributes();
        string GetStyles();
        string GetStyleSheet(string parent, int subId);
        string StartTag();
        string Content();
        string EndTag();
    }
}
