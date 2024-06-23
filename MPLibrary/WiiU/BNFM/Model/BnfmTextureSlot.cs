using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using MPLibrary.MP10.IO;
using System.ComponentModel;

namespace MPLibrary.MP10
{
    public class BnfmTextureSlot : ListItem, IFileData
    {
        [Browsable(true)]
        [Category("Texture Data")]
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
        /// The name of the texture slot.
        /// </summary>
        [Browsable(false)]
        public StringHash Name { get; set; } = new StringHash("");

        /// <summary>
        /// An unknown value.
        /// </summary>
        [Browsable(true)]
        [Category("Texture Data")]
        [DisplayName("Unknown 1")]
        public uint Unknown1 { get; set; }

        /// <summary>
        /// The min filter. 1 equals linear filtering.
        /// </summary>
        [Browsable(true)]
        [Category("Texture Data")]
        [DisplayName("MinFilter")]
        public FilterMode MinFilter { get; set; } = FilterMode.Linear;

        /// <summary>
        /// The mag filter. 1 equals linear filtering.
        /// </summary>
        [Browsable(true)]
        [Category("Texture Data")]
        [DisplayName("MagFilter")]
        public FilterMode MagFilter { get; set; } = FilterMode.Linear;

        [Browsable(true)]
        [Category("Texture Data")]
        [DisplayName("Scale X")]
        public float ScaleX
        {
            get { return Scale.X; }
            set { Scale = new Vector2(value, Scale.Y); }
        }

        [Browsable(true)]
        [Category("Texture Data")]
        [DisplayName("Scale Y")]
        public float ScaleY
        {
            get { return Scale.Y; }
            set { Scale = new Vector2(Scale.X, value); }
        }

        /// <summary>
        /// The rotation of the UVs.
        /// </summary>
        [Browsable(true)]
        [Category("Texture Data")]
        [DisplayName("Rotate")]
        public float Rotate { get; set; }

        [Browsable(true)]
        [Category("Texture Data")]
        [DisplayName("Translate X")]
        public float TranslateX
        {
            get { return Translate.X; }
            set { Translate = new Vector2(value, Translate.Y); }
        }

        [Browsable(true)]
        [Category("Texture Data")]
        [DisplayName("Translate Y")]
        public float TranslateY
        {
            get { return Translate.Y; }
            set { Translate = new Vector2(Translate.X, value); }
        }

        /// <summary>
        /// The scale of the UVs.
        /// </summary>
        [Browsable(false)]
        public Vector2 Scale { get; set; } = new Vector2(1, 1);

        /// <summary>
        /// The translation of the UVs.
        /// </summary>
        [Browsable(false)]
        public Vector2 Translate { get; set; } = new Vector2(0, 0);

        /// <summary>
        /// The rest of the texture map data.
        /// </summary>
        [Browsable(true)]
        [Category("Texture Data")]
        [DisplayName("Unknown Data")]
        public float[] Data { get; set; } = new float[11]
        {
            0, 0, 0, 0,
            0, 0, 1, 0,
            0, 0, 0
        };

        public BnfmTextureSlot() { }

        public BnfmTextureSlot(string name)
        {
            Name = new StringHash(name);
        }
        void IFileData.Read(FileReader reader)
        {
            Name = reader.LoadStringHash();
            Unknown1 = reader.ReadUInt32();
            MinFilter = (FilterMode)reader.ReadUInt32();
            MagFilter = (FilterMode)reader.ReadUInt32();
            Data = reader.ReadSingles(11);
            Rotate = reader.ReadSingle();
            Scale = reader.ReadVector2();
            Translate = reader.ReadVector2();
            Index = reader.ReadInt32();
            Flag = reader.ReadInt32();
        }

        void IFileData.Write(FileWriter writer)
        {
            writer.Write(Name);
            writer.Write(Unknown1);
            writer.Write((uint)MinFilter);
            writer.Write((uint)MagFilter);
            writer.Write(Data);
            writer.Write(Rotate);
            writer.Write(Scale);
            writer.Write(Translate);
            writer.Write(Index);
            writer.Write(Flag);
        }

        public enum FilterMode
        {
            Nearest,
            Linear,
        }
    }
}
