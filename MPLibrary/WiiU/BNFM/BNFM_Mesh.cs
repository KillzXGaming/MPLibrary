using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using OpenTK;

namespace MPLibrary.MP10
{
    public class BNFM_Mesh
    {
        public string Name { get; set; }
        public uint NameHash { get; set; }

        public uint MaterialIndex { get; set; }

        public List<ushort> Faces = new List<ushort>();
        public List<BNFM_Vertex> Vertices = new List<BNFM_Vertex>();

        public uint[] BoneIndices = new uint[0];
    }
}
