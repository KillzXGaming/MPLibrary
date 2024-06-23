using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MPLibrary.GCN
{
    public class HSFJsonMaterialConverter
    {
        public class MaterialYaml
        {
            public MaterialObject Material { get; set; } = new MaterialObject();
            public List<TextureMapYaml> Attributes { get; set; } = new List<TextureMapYaml>();
        }

        public static string Export(Material material) {

            var attributes = new List<TextureMapYaml>();
            foreach (var tex in material.TextureAttributes)
                attributes.Add(new TextureMapYaml() { TextureAttribute = tex.AttributeData, Name = tex.Name, });

            var m = new MaterialYaml()
            {
                Material = material.MaterialData,
                Attributes = attributes,
            };
            return JsonConvert.SerializeObject(m, Formatting.Indented);
        }

        public static void Import(Material material, string fileName)
        {
            var attributes = new List<TextureAttribute>();
            foreach (var tex in material.TextureAttributes)
                attributes.Add(new TextureAttribute() { AttributeData = tex.AttributeData, Name = tex.Name, });

            var m = JsonConvert.DeserializeObject<MaterialYaml>(System.IO.File.ReadAllText(fileName));
            material.MaterialData = m.Material;
            material.TextureAttributes = attributes;
        }

        public static MaterialYaml Import(string fileName) {
            return JsonConvert.DeserializeObject<MaterialYaml>(System.IO.File.ReadAllText(fileName));
        }

        public class TextureMapYaml
        {
            public string Name { get; set; }

            public AttributeData TextureAttribute { get; set; }
        }
    }
}
