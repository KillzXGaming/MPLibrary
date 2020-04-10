using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STLibrary.IO;
using System.Runtime.InteropServices;

namespace MPLibrary.DS
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MaterialBlock
    {
        public uint NameOffset { get; set; }
        public uint ColorFlags1 { get; set; } //Diffuse, ambient
        public uint ColorFlags2 { get; set; } //Specular, Emissive
        public uint HiliteScale { get; set; }
        public short AttributeIndex { get; set; }
        public short UnknownIndex { get; set; }
        public short Unknown5 { get; set; }
        public short LightingFlags { get; set; }
    }
}
