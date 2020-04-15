using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MPLibrary.GCN
{
    public class HSF_MaterialConverter
    {
        public class MaterialYaml
        {
            public MaterialObject Material = new MaterialObject();
            public List<AttributeData> Attributes = new List<AttributeData>();
        }

        public static string Export(Material material) {
            return JsonConvert.SerializeObject(material, Formatting.Indented);
        }

        public static Material Import(string fileName) {
            return JsonConvert.DeserializeObject<Material>(System.IO.File.ReadAllText(fileName));
        }
    }
}
