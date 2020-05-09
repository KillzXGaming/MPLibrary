using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using OpenTK;

namespace MPLibrary.MP10
{
    public class BNFM_MaterialMapper
    {
        public string MeshName { get; set; }
        public uint MeshNameHash { get; set; }

        public string MaterialName { get; set; }
        public uint MaterialNameHash { get; set; }

        public float Radius { get; set; }
        public Vector3 Offset { get; set; }
    }
}
