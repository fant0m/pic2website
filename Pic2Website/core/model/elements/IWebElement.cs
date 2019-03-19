using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pic2Website.core.model.elements
{
    interface IWebElement
    {
        int Id { get; set; }
        string Tag { get; set; }
        bool PairTag { get; set; }
        List<string> ClassNames { get; set; }
        Dictionary<string, string> Attributes { get; set; }
        string GetClassAttribute();
        string GetAttributes();
        string GetStyles();
        string GetStyleSheet(string parent, int subId);
        string StartTag(int level);
        string Content(int level);
        string EndTag(int level);
    }
}
