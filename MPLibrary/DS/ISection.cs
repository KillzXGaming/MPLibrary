using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;

namespace MPLibrary.DS
{
    public interface IBlockSection
    {
        void Read(HsdfFile header, FileReader reader);
        void Write(HsdfFile header, FileWriter writer);
    }
}
