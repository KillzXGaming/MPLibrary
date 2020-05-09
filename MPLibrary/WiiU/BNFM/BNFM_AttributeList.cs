using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using OpenTK;
using System.Runtime.InteropServices;

namespace MPLibrary.MP10
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class BNFM_AttributeList
    {
        public BNFM_Attribute PositionData;
        public BNFM_Attribute NormalData;
        public BNFM_Attribute ColorData;
        public BNFM_Attribute TexCoordData0;
        public BNFM_Attribute TexCoordData1;
        public BNFM_Attribute TexCoordData2; //??? it is empty so guessing here
        public BNFM_Attribute Tangent;
        public BNFM_Attribute Bitangent;
        public BNFM_Attribute BoneIndices;
        public BNFM_Attribute BoneWeights;

        public uint MeshIndex;
        public uint Unknown; //Always 1?
    }
}
