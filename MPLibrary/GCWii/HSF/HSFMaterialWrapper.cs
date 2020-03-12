using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Library;

namespace MPLibrary
{
    public class HSFMaterialWrapper : STGenericMaterial
    {
        public MaterialObject Material { get; set; }
        public ObjectData ObjectData { get; set; }
        public AttributeData Attribute { get; set; }
        public Mesh Mesh { get; set; }
    }
}
