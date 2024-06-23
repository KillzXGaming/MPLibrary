using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using MPLibrary.MP10.IO;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

namespace MPLibrary.MP10
{
    public class BnfmMaterial : ListItem, IFileData
    {
        [Browsable(true)]
        [Category("Material Data")]
        [DisplayName("Name")]
        public string Text
        {
            get { return Name.Value; }
            set
            {
                Name = new StringHash(value);
            }
        }

        /// <summary>
        /// The name of the material.
        /// </summary>
        [Browsable(false)]
        public StringHash Name { get; set; } = new StringHash("");

        /// <summary>
        /// The texture maps binded to the material (max of 6)
        /// </summary>
        [Browsable(false)]
        public BnfmTextureSlot[] Textures { get; set; } = new BnfmTextureSlot[6];

        [Browsable(true)]
        [Category("Unknowns Data")]
        [DisplayName("Unknowns1")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public float[] Unknowns1 { get; set; } = new float[27]
        {
            0,
            0,
            0,
            0,
            0,
            1,

            0.3f, 0.3f, 0.3f, 1,

            1, 1, 1, 1,

            0,
            0,
            0,
            128,

            0, 0, 0, 0,
            0, 0, 0, 0,
            0
        };

        [Browsable(true)]
        [Category("Unknowns Data")]
        [DisplayName("Unknowns2")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public int[] Unknowns2 { get; set; } = new int[7]
        {
            1, 1, 1, 0, 2, 0, 4,
        };

        [Browsable(true)]
        [Category("Unknowns Data")]
        [DisplayName("Unknowns3")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public float[] Unknowns3 { get; set; } = new float[17]
        {
            0, 0, 0, 0, 0, 0,
            1, 0, 0, 0,
            10,
            0, 0, 0, 0, 0, 0
        };

        public BnfmMaterial()
        {
            for (int i = 0; i < Textures.Length; i++)
                Textures[i] = new BnfmTextureSlot();
            this.Flag = 1
                ;
        }

        void IFileData.Read(FileReader reader)
        {
            string def = $"{String.Join(",", Unknowns1)} {String.Join(",", Unknowns2)} {String.Join(",", Unknowns3)}";

            Name = reader.LoadStringHash();
            Textures[0] = reader.Load<BnfmTextureSlot>();
            Textures[1] = reader.Load<BnfmTextureSlot>();
            Textures[2] = reader.Load<BnfmTextureSlot>();
            //WTF. 3 more slots after
            var slots = reader.LoadList<BnfmTextureSlot>(3);
            Textures[3] = slots[0];
            Textures[4] = slots[1];
            Textures[5] = slots[2];
            Unknowns1 = reader.ReadSingles(27);
            Unknowns2 = reader.ReadInt32s(7);
            Unknowns3 = reader.ReadSingles(17);
            Index = reader.ReadInt32();
            Flag = reader.ReadInt32();

            string n = $"{String.Join(",", Unknowns1)} {String.Join(",", Unknowns2)} {String.Join(",", Unknowns3)}";
            if (def != n)
            {
                Console.WriteLine($"{def}");
                Console.WriteLine($"{n}");
            }
        }

        void IFileData.Write(FileWriter writer)
        {
            writer.Write(Name);
            writer.Save(Textures[0]);
            writer.Save(Textures[1]);
            writer.Save(Textures[2]);
            writer.SaveList(new BnfmTextureSlot[3] { Textures[3], Textures[4], Textures[5] });
            writer.Write(Unknowns1);
            writer.Write(Unknowns2);
            writer.Write(Unknowns3);
            writer.Write(Index);
            writer.Write(Flag);
        }

        public void Export(string filePath)
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}
