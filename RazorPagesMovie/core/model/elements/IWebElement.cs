using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesMovie.core.model.elements
{
    interface IWebElement
    {
        String StartTag();
        String Content();
        String EndTag();
    }
}
