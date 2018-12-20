using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RazorPagesMovie.core.model.elements;

namespace RazorPagesMovie.core.model
{
    public class TemplateStructure
    {
        public List<Section> Sections { get; }

        public TemplateStructure()
        {
            Sections = new List<Section>();
        }
    }
}
