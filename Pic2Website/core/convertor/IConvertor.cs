using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pic2Website.core.model;

namespace Pic2Website.core.convertor
{
    interface IConvertor
    {
        void Convert();
        void Save();
        string GetContentPath();
    }
}
