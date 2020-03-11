using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Library.IO;
using System.Runtime.InteropServices;
using OpenTK;

namespace MPLibrary
{
    public interface ISection
    {
        void Read(FileReader reader, HsfFile header);
        void Write(FileWriter reader, HsfFile header);
    }

    public class MeshGroup
    {
        public string Name { get; set; }
        public List<Mesh> Meshes = new List<Mesh>();

        public void AddPositions(List<Vector3> positions)
        {
            //Find first mesh that has no values
            var mesh = Meshes.FirstOrDefault(x => x.Positions.Count == 0);
            if (mesh != null)
                mesh.Positions = positions;
            else
                Meshes.Add(new Mesh() {Name = Name, Positions = positions });
        }

        public void AddNormals(List<Vector3> normals)
        {
            //Find first mesh that has no values
            var mesh = Meshes.FirstOrDefault(x => x.Normals.Count == 0);
            if (mesh != null)
                mesh.Normals = normals;
            else
                Meshes.Add(new Mesh() { Name = Name, Normals = normals });
        }

        public void AddTexCoords(List<Vector2> texCoords)
        {
            //Find first mesh that has no values
            var mesh = Meshes.FirstOrDefault(x => x.TexCoords.Count == 0);
            if (mesh != null)
                mesh.TexCoords = texCoords;
            else
                Meshes.Add(new Mesh() { Name = Name, TexCoords = texCoords });
        }

        public void AddColors(List<Vector4> colors)
        {
            //Find first mesh that has no values
            var mesh = Meshes.FirstOrDefault(x => x.Colors.Count == 0);
            if (mesh != null)
                mesh.Colors = colors;
            else
                Meshes.Add(new Mesh() { Name = Name, Colors = colors });
        }

        public void AddPrimitives(List<PrimitiveObject> primitives)
        {
            //Find first mesh that has no values
            var mesh = Meshes.FirstOrDefault(x => x.Primitives.Count == 0);
            if (mesh != null)
                mesh.Primitives = primitives;
            else
                Meshes.Add(new Mesh() { Name = Name, Primitives = primitives });
        }

        public void AddRigging(RiggingInfo rigging)
        {
            //Find first mesh that has no values
            var mesh = Meshes.FirstOrDefault(x => !x.HasRigging);
            if (mesh != null)
                mesh.RiggingInfo = rigging;
            else
                Meshes.Add(new Mesh() { Name = Name, RiggingInfo = rigging });
        }
    }

    public class Mesh
    {
        public string Name { get; set; }

        public List<Vector3> Positions = new List<Vector3>();
        public List<Vector3> Normals = new List<Vector3>();
        public List<Vector2> TexCoords = new List<Vector2>();
        public List<Vector4> Colors = new List<Vector4>();

        public List<PrimitiveObject> Primitives = new List<PrimitiveObject>();

        public RiggingInfo RiggingInfo;

        public bool HasRigging => RiggingInfo != null;
    }

    public class RiggingInfo
    {
        public List<RiggingSingleBind> SingleBinds = new List<RiggingSingleBind>();
        public List<RiggingDoubleBind> DoubleBinds = new List<RiggingDoubleBind>();
        public List<RiggingMultiBind> MultiBinds = new List<RiggingMultiBind>();
        public List<RiggingDoubleWeight> DoubleWeights = new List<RiggingDoubleWeight>();
        public List<RiggingMultiWeight> MultiWeights = new List<RiggingMultiWeight>();

        public uint SingleBind;
    }

    //Parser based on https://github.com/Ploaj/Metanoia/blob/master/Metanoia/Formats/GameCube/HSF.cs
    public class HsfFile
    {
        public FogSection FogData = new FogSection();
        public ColorSection ColorData = new ColorSection();
        public MaterialSection MaterialData = new MaterialSection();
        public AttributeSection AttributeData = new AttributeSection();
        public PositionSection PositionData = new PositionSection();
        public NormalSection NormalData = new NormalSection();
        public TexCoordSection TexCoordData = new TexCoordSection();
        public FaceDataSection FaceData = new FaceDataSection();
        public ObjectDataSection ObjectData = new ObjectDataSection();

        public TextureSection TextureData = new TextureSection();
        public PaletteSection PaletteData = new PaletteSection();

        public MotionDataSection MotionData = new MotionDataSection();
        public CenvDataSection CenvData = new CenvDataSection();
        public SkeletonDataSection SkeletonData = new SkeletonDataSection();

        public PartDataSection PartData = new PartDataSection();
        public ClusterDataSection ClusterData = new ClusterDataSection();
        public ShapeDataSection ShapeData = new ShapeDataSection();
        public MapAttributeDataSection MapAttributeData = new MapAttributeDataSection();

        public MatrixDataSection MatrixData = new MatrixDataSection();
        public SymbolDataSection SymbolData = new SymbolDataSection();

        public string Version { get; set; }

        internal uint StringTableOffset = 0;
        internal uint StringTableSize = 0;

        public Dictionary<string, MeshGroup> Meshes = new Dictionary<string, MeshGroup>();

        public List<Mesh> GetAllMeshes()
        {
            List<Mesh> meshes = new List<Mesh>();
            foreach (var group in Meshes.Values)
                meshes.AddRange(group.Meshes);
            return meshes;
        }

        public int TextureCount => TextureData.Textures.Count;

        public int ObjectCount => ObjectData.Objects.Count;

        public HsfFile(string fileName) {
            Read(new FileReader(fileName));
        }

        private HsfFile(System.IO.Stream stream) {
            Read(new FileReader(stream));
        }

        public void Read(FileReader reader)
        {
            reader.SetByteOrder(true);

            //First get the string table so we can use it for the sections before it
            using (reader.TemporarySeek(0x0A8, System.IO.SeekOrigin.Begin)) {
                StringTableOffset = reader.ReadUInt32();
                StringTableSize = reader.ReadUInt32();
            }

            reader.ReadSignature(3, "HSF");
            Version = reader.ReadString(4, Encoding.ASCII); //Always V037?
            reader.ReadByte();
            FogData = ReadSection<FogSection>(reader, this);
            ColorData = ReadSection<ColorSection>(reader, this);
            MaterialData = ReadSection<MaterialSection>(reader, this);
            AttributeData = ReadSection<AttributeSection>(reader, this);
            PositionData = ReadSection<PositionSection>(reader, this);
            NormalData = ReadSection<NormalSection>(reader, this);
            TexCoordData = ReadSection<TexCoordSection>(reader, this);
            FaceData = ReadSection<FaceDataSection>(reader, this); //Primative face data
            ObjectData = ReadSection<ObjectDataSection>(reader, this); //Nodes/bones
            TextureData = ReadSection<TextureSection>(reader, this);
            PaletteData = ReadSection<PaletteSection>(reader, this);
            MotionData = ReadSection<MotionDataSection>(reader, this);
            CenvData = ReadSection<CenvDataSection>(reader, this);
            SkeletonData = ReadSection<SkeletonDataSection>(reader, this);

            //Unused sections
            //-----------
            PartData = ReadSection<PartDataSection>(reader, this);
            ClusterData = ReadSection<ClusterDataSection>(reader, this);
            ShapeData = ReadSection<ShapeDataSection>(reader, this);
            MapAttributeData = ReadSection<MapAttributeDataSection>(reader, this);
            //-----------

            MatrixData = ReadSection<MatrixDataSection>(reader, this);
            SymbolData = ReadSection<SymbolDataSection>(reader, this);
            reader.ReadUInt32(); //StringTableOffset
            reader.ReadUInt32(); //StringTableSize
        }

        private Dictionary<string, int> savedStrings;

        internal int GetStringOffset(string name)
        {
            if (name == string.Empty)
                return -1;

            if (savedStrings.ContainsKey(name))
                return (int)savedStrings[name];
            else
            {
                Console.WriteLine($"WARNING! Cannot find string {name}");
                return -1;
            }
        }

        public void Save(string fileName) {
            Write(new FileWriter(fileName));
        }

        public void Save(System.IO.Stream stream) {
            Write(new FileWriter(stream));
        }

        private void Write(FileWriter writer)
        {
            writer.SetByteOrder(true);
            writer.WriteSignature("HSF");
            writer.WriteSignature(Version);
            writer.Seek(1);

            savedStrings = SaveStrings();

            var numUsedPositions = 0;
            var numUsedColors = 0;
            var numUsedTexCoords = 0;
            var numUsedNormals =0;
            var numUsedPrimitives = 0;
            var numUsedRigs = 0;
            foreach (var meshGroup in Meshes.Values) {
                foreach (var mesh in meshGroup.Meshes)
                {
                    if (mesh.Positions.Count > 0) numUsedPositions++;
                    if (mesh.Normals.Count > 0) numUsedNormals++;
                    if (mesh.TexCoords.Count > 0) numUsedTexCoords++;
                    if (mesh.Colors.Count > 0) numUsedColors++;
                    if (mesh.Primitives.Count > 0) numUsedPrimitives++;
                    if (mesh.HasRigging) numUsedRigs++;
                }
            }

            SaveSectionHeader(writer, (uint)FogData.Count, this);
            SaveSectionHeader(writer, (uint)numUsedColors, this);
            SaveSectionHeader(writer, (uint)MaterialData.Materials.Count, this);
            SaveSectionHeader(writer, (uint)AttributeData.Attributes.Count, this);
            SaveSectionHeader(writer, (uint)numUsedPositions, this);
            SaveSectionHeader(writer, (uint)numUsedNormals, this);
            SaveSectionHeader(writer, (uint)numUsedTexCoords, this);
            SaveSectionHeader(writer, (uint)numUsedPrimitives, this);
            SaveSectionHeader(writer, (uint)ObjectData.Objects.Count, this);
            SaveSectionHeader(writer, (uint)TextureData.Textures.Count, this);
            SaveSectionHeader(writer, (uint)PaletteData.Palettes.Count, this);
            SaveSectionHeader(writer, (uint)MotionData.Animations.Count, this);
            SaveSectionHeader(writer, (uint)CenvData.Count, this);
            SaveSectionHeader(writer, (uint)SkeletonData.Count, this);
            SaveSectionHeader(writer, (uint)PartData.Count, this);
            SaveSectionHeader(writer, (uint)ClusterData.Count, this);
            SaveSectionHeader(writer, (uint)ShapeData.Count, this);
            SaveSectionHeader(writer, (uint)MapAttributeData.Count, this);
            SaveSectionHeader(writer, (uint)MatrixData.Count, this);
            SaveSectionHeader(writer, (uint)SymbolData.Count, this);
            long _stringTableOfsPos = writer.Position;
            writer.Write(uint.MaxValue);
            writer.Write(uint.MaxValue);

            if (FogData.Count > 0) {
                writer.WriteUint32Offset(8);
                FogData.Write(writer, this);
            }
            if (MaterialData.Materials.Count > 0)  {
                writer.WriteUint32Offset(24);
                MaterialData.Write(writer, this);
            }
            if (AttributeData.Attributes.Count > 0)
            {
                writer.WriteUint32Offset(32);
                AttributeData.Write(writer, this);
            }
            writer.WriteUint32Offset(40);
            if (numUsedPositions > 0) {
                PositionData.Write(writer, this);
            }
            writer.WriteUint32Offset(48);
            if (numUsedNormals > 0) {
                NormalData.Write(writer, this);
            }
            writer.WriteUint32Offset(56);
            if (numUsedTexCoords > 0) {
                TexCoordData.Write(writer, this);
            }
            writer.WriteUint32Offset(72);
            if (ObjectData.Objects.Count > 0) {
                ObjectData.Write(writer, this);
            }
            writer.WriteUint32Offset(16);
            if (numUsedColors > 0) {
                ColorData.Write(writer, this);
            }
            writer.WriteUint32Offset(64);
            if (numUsedPrimitives > 0) {
                FaceData.Write(writer, this);
            }
            writer.WriteUint32Offset(80);
            if (TextureData.Textures.Count > 0) {
                TextureData.Write(writer, this);
            }
            writer.WriteUint32Offset(88);
            if (PaletteData.PaletteData.Count > 0) {
                PaletteData.Write(writer, this);
            }

            writer.WriteUint32Offset(96);
            if (MotionData.Animations.Count > 0) {
                MotionData.Write(writer, this);
            }
            writer.WriteUint32Offset(104);
            if (numUsedRigs > 0) {
                CenvData.Write(writer, this);
            }
            writer.WriteUint32Offset(112);
            if (SkeletonData.Nodes.Count > 0) {
                SkeletonData.Write(writer, this);
            }
            writer.WriteUint32Offset(120);
            if (PartData.Count > 0) {
                PartData.Write(writer, this);
            }
            writer.WriteUint32Offset(128);
            if (ClusterData.Count > 0) {
                ClusterData.Write(writer, this);
            }
            writer.WriteUint32Offset(136);
            if (ShapeData.Count > 0) {
                ShapeData.Write(writer, this);
            }
            writer.WriteUint32Offset(144);
            if (MapAttributeData.Count > 0) {
                MapAttributeData.Write(writer, this);
            }
            if (MatrixData.Count > 0) {
                writer.WriteUint32Offset(152);
                MatrixData.Write(writer, this);
            }
            if (SymbolData.Count > 0) {
                writer.WriteUint32Offset(160);
                SymbolData.Write(writer, this);
            }


            writer.WriteUint32Offset(_stringTableOfsPos);
            uint tblSize = WriteStringTable(writer);
            using (writer.TemporarySeek(_stringTableOfsPos + 4, System.IO.SeekOrigin.Begin)) {
                writer.Write(tblSize);
            }
        }

        private uint WriteStringTable(FileWriter writer)
        {
            long startPos = writer.Position;
            foreach (var val in savedStrings) {
                writer.SeekBegin(startPos + val.Value);
                writer.WriteString(val.Key);
            }

            long stringTableEnd = writer.Position;
            return (uint)(stringTableEnd - startPos);
        }

        private Dictionary<string, int> SaveStrings()
        {
            //Go through all possible strings and save the position to be used in the table
            Dictionary<string, int> values = new Dictionary<string, int>();
            int offset = 0;

            List<string> fileStrings = new List<string>();

            //Note dupe names doesn't matter because value dictionary will skip them
            foreach (var mat in MaterialData.MaterialNames)
                fileStrings.Add(mat);
            foreach (var mat in AttributeData.AttributeNames)
                fileStrings.Add(mat);
            foreach (var mesh in Meshes)
                fileStrings.Add(mesh.Key);
            foreach (var obj in ObjectData.ObjectNames)
                fileStrings.Add(obj);
            foreach (var anim in MotionData.GetStrings())
                fileStrings.Add(anim);
            foreach (var anim in TextureData.TextureNames)
                fileStrings.Add(anim);

            //Save strings to lookup dictionary
            foreach (var str in fileStrings) {
                if (!values.ContainsKey(str) && str != string.Empty) {
                    values.Add(str, offset);
                    offset += str.Length + 1;
                }
            }

            return values;
        }

        //Adds a position component to the mesh
        public void AddPositionComponent(FileReader reader, ComponentData comp, List<Vector3> positions)
        {
            var name = InitializeMeshComponent(reader, comp);
            Meshes[name].AddPositions(positions);
        }

        //Adds a position component to the mesh
        public void AddColorComponent(FileReader reader, ComponentData comp, List<Vector4> colors)
        {
            var name = InitializeMeshComponent(reader, comp);
            Meshes[name].AddColors(colors);
        }

        //Adds a normal component to the mesh
        public void AddNormalComponent(FileReader reader, ComponentData comp, List<Vector3> normals)
        {
            var name = InitializeMeshComponent(reader, comp);
            Meshes[name].AddNormals(normals);
        }

        //Adds a UV component to the mesh
        public void AddUVComponent(FileReader reader, ComponentData comp, List<Vector2> uvs)
        {
            var name = InitializeMeshComponent(reader, comp);
            Meshes[name].AddTexCoords(uvs);
        }

        //Adds a primitive   component to the mesh
        public void AddPrimitiveComponent(FileReader reader, ComponentData comp, List<PrimitiveObject> primitives)
        {
            var name = InitializeMeshComponent(reader, comp);
            Meshes[name].AddPrimitives(primitives);
        }
        

        //Load component to the mesh lookup and return the key
        private string InitializeMeshComponent(FileReader reader, ComponentData comp) {
            string name = GetString(reader, comp.StringOffset);

            if (!Meshes.ContainsKey(name)) {
                Meshes.Add(name, new MeshGroup() { Name = name, });
                Meshes[name].Meshes.Add(new Mesh() { Name = name,});
            }
            return name;
        }

        //Read the string from the string table given a relative offset
        public string GetString(FileReader reader, uint offset) {
            if (offset == uint.MaxValue)
                return string.Empty;

            using (reader.TemporarySeek(StringTableOffset + offset, System.IO.SeekOrigin.Begin))
            {
                return reader.ReadZeroTerminatedString();
            }
        }

        public T ReadSection<T>(FileReader reader, HsfFile header) where T : HSFSection, new()
        {
            T instance = new T();
            instance.Offset = reader.ReadUInt32();
            instance.Count = reader.ReadUInt32();

            using (reader.TemporarySeek(instance.Offset, System.IO.SeekOrigin.Begin)) {
                instance.Read(reader, header);
            }

            return instance;
        }

        public void SaveSectionHeader(FileWriter writer, uint count, HsfFile header)
        {
            writer.Write(0);
            writer.Write(count);
        }
    }
}
