using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RazorPagesMovie.core.model;

namespace RazorPagesMovie.core.convertor
{
    interface IConvertor
    {
        void Convert();
        void Save();
        string GetContentPath();
    }
}
