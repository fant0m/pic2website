using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pic2Website.core.model.elements;

namespace Pic2Website.core.model
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
