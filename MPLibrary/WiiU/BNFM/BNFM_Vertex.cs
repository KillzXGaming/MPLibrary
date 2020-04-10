using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace MPLibrary.MP10
{
    public class BNFM_Vertex
    {
        public Vector3 Position { get; set; }
        public Vector3 Normal { get; set; }
        public Vector4 Color { get; set; }
        public Vector2 TexCoord0 { get; set; }
        public Vector2 TexCoord1 { get; set; }
        public Vector4 BoneIndices { get; set; }
        public Vector4 BoneWeights { get; set; }
        public Vector2 Unknown { get; set; }
    }
}
