using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RazorPagesMovie.core.model;

namespace RazorPagesMovie.core.convertor
{
    interface IConvertor
    {
        String Convert();
        // @todo zoznam metód
        // @todo napr. na uloženie do súboru, resp. nech to vráti zoznam súborov vygenerovaných / priečinok
    }
}
