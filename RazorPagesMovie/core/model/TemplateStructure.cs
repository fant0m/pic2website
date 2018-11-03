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
        private Layout _layout;
        public List<Section> Sections { get; }

        public TemplateStructure(Layout layout)
        {
            _layout = layout;
            Sections = new List<Section>();
        }
    }
}
