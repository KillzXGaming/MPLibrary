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
    public class BnfmMesh : ListItem, IFileData
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
        /// The name of the mesh.
        /// </summary>
        [Browsable(false)]
        public StringHash Name { get; set; } = new StringHash("");

        /// <summary>
        /// Gets or sets the polygon data for drawing.
        /// </summary>
        [Browsable(false)]
        public List<BnfmPolygon> Polygons { get; set; } = new List<BnfmPolygon>();

        /// <summary>
        /// The material used to render onto the mesh.
        /// </summary>
        [Browsable(false)]
        public BnfmMaterial Material { get; set; }

        /// <summary>
        /// The attributes used by the mesh.
        /// </summary>
        [Browsable(false)]
        public BnfmAttributeList AttributeList { get; set; }

        /// <summary>
        /// The bounding sphere radius
        /// </summary>
        public float SphereRadius { get; set; }

        /// <summary>
        /// The bounding sphere offset.
        /// </summary>
        [Browsable(false)]
        public Vector3 SphereOffset { get; set; }

        /// <summary>
        /// An unknown value.
        /// </summary>
        public uint Value { get; set; } = 1;

        /// <summary>
        /// The name of the material.
        /// </summary>
        [Browsable(false)]
        private StringHash MaterialName { get; set; }

        void IFileData.Read(FileReader reader)
        {
            Name = reader.LoadStringHash();
            MaterialName = reader.LoadStringHash();
            uint polyCount = reader.ReadUInt32();
            Polygons = reader.LoadList<BnfmPolygon>(polyCount);
            Material = reader.Load<BnfmMaterial>();
            AttributeList = reader.Load<BnfmAttributeList>();
            Value = reader.ReadUInt32();
            SphereRadius = reader.ReadSingle();
            SphereOffset = reader.ReadVector3();
            Index = reader.ReadInt32();
            Flag = reader.ReadInt32();
        }

        void IFileData.Write(FileWriter writer)
        {
            MaterialName = Material.Name;

            writer.Write(Name);
            writer.Write(MaterialName);
            writer.Write(Polygons.Count);
            writer.SaveList(Polygons);
            writer.Save(Material);
            writer.Save(AttributeList);
            writer.Write(Value);
            writer.Write(SphereRadius);
            writer.Write(SphereOffset);
            writer.Write(Index);
            writer.Write(Flag);
        }
    }
}
