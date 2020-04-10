using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using OpenTK;
using System.Runtime.InteropServices;

namespace MPLibrary.MP10
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class BNFM_Attribute
    {
        public int Format;
        public int TotalVertexStride; //Always 44?
        public uint VertexOffset; //Relative to the start of vertex data
    }

    public enum BNFM_AttributeFormat
    {
        Float = 2,
        Byte = 5,
        Sbyte = 6,
    }
}
