using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using System.Runtime.InteropServices;
using OpenTK;

namespace MPLibrary
{
    public interface ISection
    {
        void Read(FileReader reader, HsfFile header);
        void Write(FileWriter reader, HsfFile header);
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

        public ObjectData ObjectData;

        public Mesh(ObjectData objectData, string name) {
            ObjectData = objectData;
            Name = name;
        }
    }

    public class Material
    {
        public string Name { get; set; }
        public int ID { get; set; }

        public MaterialObject MaterialData { get; set; }

        public List<Tuple<string, AttributeData>> Textures = new List<Tuple<string, AttributeData>>();
    }

    public class EffectMesh
    {
        public string Name { get; set; }

        public List<Vector3> Positions = new List<Vector3>();
        public List<Vector3> Normals = new List<Vector3>();

        internal uint PositionOffset;
        internal uint NormalOffset;
    }

    public class RiggingInfo
    {
        public List<RiggingSingleBind> SingleBinds = new List<RiggingSingleBind>();
        public List<RiggingDoubleBind> DoubleBinds = new List<RiggingDoubleBind>();
        public List<RiggingMultiBind> MultiBinds = new List<RiggingMultiBind>();
        public List<RiggingDoubleWeight> DoubleWeights = new List<RiggingDoubleWeight>();
        public List<RiggingMultiWeight> MultiWeights = new List<RiggingMultiWeight>();

        public uint VertexCount;
        public uint Unknown; //0xCCCCCCCC (Usually that value if value is null and unused)

        public uint SingleBind;
    }

    public class HSFTexture
    {
        public string Name { get; set; }
        public TextureInfo TextureInfo { get; set; }
        public byte[] ImageData { get; set; }

        public PaletteInfo PaletteInfo;
        public byte[] PaletteData;

        public HSFTexture()
        {

        }

        public static int GetFormatId(STLibrary.Decode_Gamecube.TextureFormats format)
        {
            return TextureSection.FormatList.FirstOrDefault(x => x.Value == format).Key;
        }

        public bool HasPaletteData()
        {
            return PaletteData != null && PaletteData.Length > 0;
        }

        public HSFTexture(string name, TextureInfo info, byte[] imageData)
        {
            Name = name;
            TextureInfo = info;
            ImageData = imageData;
        }
    }

    //Parser based on https://github.com/Ploaj/Metanoia/blob/master/Metanoia/Formats/GameCube/HSF.cs
    public class HsfFile
    {
        internal static string NullString => "<0>";

        public FogSection FogData = new FogSection();

        public AttributeSection AttributeData = new AttributeSection();

        public ObjectDataSection ObjectData = new ObjectDataSection();

        internal TextureSection TextureData = new TextureSection();
        internal PaletteSection PaletteData = new PaletteSection();

        public MotionDataSection MotionData = new MotionDataSection();
        public CenvDataSection CenvData = new CenvDataSection();
        public SkeletonDataSection SkeletonData = new SkeletonDataSection();

        internal MaterialSection MaterialData = new MaterialSection();

        internal ColorSection ColorData = new ColorSection();
        internal PositionSection PositionData = new PositionSection();
        internal NormalSection NormalData = new NormalSection();
        internal TexCoordSection TexCoordData = new TexCoordSection();
        internal FaceDataSection FaceData = new FaceDataSection();

        public List<ObjectDataNode> ObjectRoots = new List<ObjectDataNode>();

        public PartDataSection PartData = new PartDataSection();
        public ClusterDataSection ClusterData = new ClusterDataSection();
        public ShapeDataSection ShapeData = new ShapeDataSection();
        public MapAttributeDataSection MapAttributeData = new MapAttributeDataSection();

        public MatrixDataSection MatrixData = new MatrixDataSection();
        public SymbolDataSection SymbolData = new SymbolDataSection();

        public Dictionary<int, MatAnimController> MatAnimControllers = new Dictionary<int, MatAnimController>();
        public Dictionary<int, AttributeAnimController> AttributeAnimControllers = new Dictionary<int, AttributeAnimController>();

        public string Version { get; set; } = "V037";

        internal uint StringTableOffset = 0;
        internal uint StringTableSize = 0;

        public List<Mesh> Meshes = new List<Mesh>();
        public List<Material> Materials = new List<Material>();
        public List<HSFTexture> Textures = new List<HSFTexture>();

        public int TextureCount => Textures.Count;

        public int ObjectCount => ObjectData.Objects.Count;

        public HsfFile() { }

        public HsfFile(string fileName) {
            Read(new FileReader(fileName));
        }

        public HsfFile(System.IO.Stream stream) {
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

            //Next get object data. This section is necessary to link data
            using (reader.TemporarySeek(72, System.IO.SeekOrigin.Begin)) {
                ObjectData = ReadSection<ObjectDataSection>(reader, this); //Nodes/bones
            }

            //Create a list of all the meshes from objects
            for (int i = 0; i < ObjectData.Objects.Count; i++) {
                if (ObjectData.Objects[i].Type == ObjectType.Mesh)
                    Meshes.Add(new Mesh(ObjectData.Objects[i], ObjectData.ObjectNames[i]));
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
            reader.Seek(8); //Object data (already read from above)
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

     /*       List<ObjectDataNode> objects = new List<ObjectDataNode>();
            foreach (var obj in ObjectData.Objects)
                objects.Add(new ObjectDataNode(obj));

            foreach (var obj in objects)
            {
                var data = obj.ObjectData;
                var symbol = data.SymbolIndex;
                for (int i = 0; i < data.ChildrenCount; i++)
                    obj.Children.Add(objects[SymbolData.SymbolIndices[symbol++]]);

                if (data.ParentIndex != -1) {
                    obj.Parent = objects[(int)data.ParentIndex];
                }
            }

            foreach (var obj in objects) {
                if (obj.Parent == null)
                    ObjectRoots.Add(obj);
            }

            objects.Clear();*/

            foreach (var material in Materials)
            {
                var matData = material.MaterialData;
                if (matData.TextureCount > 0)
                {
                    var symbol = matData.FirstSymbol;
                    for (int i = 0; i < matData.TextureCount; i++)
                    {
                        var index = SymbolData.SymbolIndices[symbol++];
                        var name = AttributeData.AttributeNames[index];
                        material.Textures.Add(Tuple.Create(name, AttributeData.Attributes[index]));
                    }
                }
            }

            ObjectData.ReadEffectMeshes(reader, this);
        }

        internal void AddTexture(string name, TextureInfo info, byte[] imageData) {
            Textures.Add(new HSFTexture(name, info, imageData));
        }

        internal void AddPalette(List<PaletteInfo> paletteInfos, List<byte[]> paletteDatas) {
            for (int i = 0; i < Textures.Count; i++)
            {
                var info = Textures[i].TextureInfo;
                var index = info.PaletteIndex; 
                if (index != -1) {
                    Textures[i].PaletteInfo = paletteInfos[index];
                    Textures[i].PaletteData = paletteDatas[index];
                }
            }
        }

        internal void AddMaterial(MaterialObject mat, string name) {
            Materials.Add(new Material() {
                MaterialData = mat,
                ID = Materials.Count,
                Name = name });
        }

        private Dictionary<string, int> savedStrings;

        internal int GetStringOffset(string name)
        {
            if (name == NullString)
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
            using (var writer = new FileWriter(fileName)) {
                Write(writer);
            }
        }

        public void Save(System.IO.Stream stream, bool keepOpen = false) {
            using (var writer = new FileWriter(stream, keepOpen)) {
                Write(writer);
            }
        }

        //Regenerates symbols used by attributes and objects
        private void GenerateSymbolTable()
        {
            List<int> symbols = new List<int>();
           /* for (int i = 0; i < AttributeData.Attributes.Count; i++)
            {
                symbols.Add(i);
            }
            */

            for (int i = 0; i < Materials.Count; i++)
            {
                var data = Materials[i].MaterialData;

                data.FirstSymbol = symbols.Count;
                foreach (var att in Materials[i].Textures)
                {
                    var index = AttributeData.Attributes.IndexOf(att.Item2);
                    if (!symbols.Contains(index))
                        symbols.Add(index);
                }
                data.TextureCount = Materials[i].Textures.Count;
            }
            Console.WriteLine($"ATT symbols {symbols.Count}");

            symbols.AddRange(ObjectData.GenerateSymbols(symbols.Count - 1));
            Console.WriteLine($"symbols {symbols.Count}");

            SymbolData.SymbolIndices = symbols.ToArray();
        }

        //Saves and applies all the attributes used for meshes 
        private void SaveMeshAttributes()
        {
            List<AttributeData> attributes = new List<AttributeData>();
            List<string> attributeNames = new List<string>();
            for (int i = 0; i < Materials.Count; i++)
            {
                foreach (var tex in Materials[i].Textures)
                {
                    if (!attributes.Contains(tex.Item2))
                    {
                        attributeNames.Add(tex.Item1);
                        attributes.Add(tex.Item2);
                    }
                }
            }

            AttributeData.Attributes = attributes;
            AttributeData.AttributeNames = attributeNames;
        }

        private void Write(FileWriter writer)
        {
            SaveMeshAttributes();

            writer.SetByteOrder(true);
            writer.WriteSignature("HSF");
            writer.WriteSignature(Version);
            writer.Seek(1);

            savedStrings = SaveStrings();
            GenerateSymbolTable();

            var numUsedPositions = 0;
            var numUsedColors = 0;
            var numUsedTexCoords = 0;
            var numUsedNormals =0;
            var numUsedPrimitives = 0;
            var numUsedRigs = 0;
            var numFaces = 0;
            foreach (var mesh in Meshes) {
                mesh.ObjectData.FaceIndex = numFaces;
                if (mesh.Colors.Count > 0)
                    mesh.ObjectData.ColorIndex = numUsedColors;

                if (mesh.Positions.Count > 0) numUsedPositions++;
                if (mesh.Normals.Count > 0) numUsedNormals++;
                if (mesh.TexCoords.Count > 0) numUsedTexCoords++;
                if (mesh.Colors.Count > 0) numUsedColors++;
                if (mesh.Primitives.Count > 0) numUsedPrimitives++;
                if (mesh.HasRigging) numUsedRigs++;

                numFaces++;
            }
            SaveSectionHeader(writer, (uint)FogData.Count, this);
            SaveSectionHeader(writer, (uint)numUsedColors, this);
            SaveSectionHeader(writer, (uint)Materials.Count, this);
            SaveSectionHeader(writer, (uint)AttributeData.Attributes.Count, this);
            SaveSectionHeader(writer, (uint)numUsedPositions, this);
            SaveSectionHeader(writer, (uint)numUsedNormals, this);
            SaveSectionHeader(writer, (uint)numUsedTexCoords, this);
            SaveSectionHeader(writer, (uint)numUsedPrimitives, this);
            SaveSectionHeader(writer, (uint)ObjectData.Objects.Count, this);
            SaveSectionHeader(writer, (uint)Textures.Count, this);
            SaveSectionHeader(writer, (uint)PaletteData.PaletteData.Count, this);
            SaveSectionHeader(writer, (uint)MotionData.Animations.Count, this);
            SaveSectionHeader(writer, (uint)CenvData.Count, this);
            SaveSectionHeader(writer, (uint)SkeletonData.Count, this);
            SaveSectionHeader(writer, (uint)PartData.Count, this);
            SaveSectionHeader(writer, (uint)ClusterData.Count, this);
            SaveSectionHeader(writer, (uint)ShapeData.Count, this);
            SaveSectionHeader(writer, (uint)MapAttributeData.Count, this);
            SaveSectionHeader(writer, (uint)MatrixData.Count, this);
            SaveSectionHeader(writer, (uint)SymbolData.SymbolIndices.Length, this);
            long _stringTableOfsPos = writer.Position;
            writer.Write(uint.MaxValue);
            writer.Write(uint.MaxValue);

            if (FogData.Count > 0) {
                writer.WriteUint32Offset(8);
                FogData.Write(writer, this);
            }
            if (Materials.Count > 0)  {
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

            ObjectData.WriteEffectPositions(writer, this);

            writer.WriteUint32Offset(48);
            if (numUsedNormals > 0) {
                NormalData.Write(writer, this);
            }

            ObjectData.WriteEffectNormals(writer, this);

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
            if (Textures.Count > 0) {
                TextureData.Write(writer, this);
            }
            writer.WriteUint32Offset(88);
            if (PaletteData.PaletteData.Count > 0) {
                PaletteData.Write(writer, this);
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
            writer.WriteUint32Offset(96);
            if (MotionData.Animations.Count > 0) {
                MotionData.Write(writer, this);
            }

            if (SymbolData.SymbolIndices.Length > 0) {
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
            foreach (var mat in Materials)
                fileStrings.Add(mat.Name);
            foreach (var mat in AttributeData.AttributeNames)
                fileStrings.Add(mat);
            foreach (var mesh in Meshes)
                fileStrings.Add(mesh.Name);
            foreach (var obj in ObjectData.ObjectNames)
                fileStrings.Add(obj);
            foreach (var tex in Textures)
                fileStrings.Add(tex.Name);
            foreach (var anim in MotionData.GetStrings())
                fileStrings.Add(anim);

            //Save strings to lookup dictionary
            foreach (var str in fileStrings) {
                if (!values.ContainsKey(str) && str != NullString) {
                    values.Add(str, offset);
                    offset += str.Length + 1;
                }
            }

            return values;
        }

        //Adds a position component to the mesh
        internal void AddPositionComponent(int index, List<Vector3> positions)
        {
            for (int i = 0; i < Meshes.Count; i++) {
                if (Meshes[i].ObjectData.VertexIndex == index)
                    Meshes[i].Positions = positions;
            }
        }

        //Adds a position component to the mesh
        internal void AddColorComponent(int index, List<Vector4> colors)
        {
            for (int i = 0; i < Meshes.Count; i++) {
                if (Meshes[i].ObjectData.ColorIndex == index)
                    Meshes[i].Colors = colors;
            }
        }

        //Adds a normal component to the mesh
        internal void AddNormalComponent(int index, List<Vector3> normals)
        {
            for (int i = 0; i < Meshes.Count; i++) {
                if (Meshes[i].ObjectData.NormalIndex == index)
                    Meshes[i].Normals = normals;
            }
        }

        //Adds a UV component to the mesh
        internal void AddUVComponent(int index, List<Vector2> uvs)
        {
            for (int i = 0; i < Meshes.Count; i++)
            {
                if (Meshes[i].ObjectData.TexCoordIndex == index)
                    Meshes[i].TexCoords = uvs;
            }
        }

        //Adds a primitive   component to the mesh
        internal void AddPrimitiveComponent(int index, List<PrimitiveObject> primitives)
        {
            for (int i = 0; i < Meshes.Count; i++)
            {
                if (Meshes[i].ObjectData.FaceIndex == index)
                    Meshes[i].Primitives = primitives;
            }
        }

        //Read the string from the string table given a relative offset
        internal string GetString(FileReader reader, uint offset) {
            if (offset == uint.MaxValue)
                return NullString;

            using (reader.TemporarySeek(StringTableOffset + offset, System.IO.SeekOrigin.Begin))
            {
                return reader.ReadZeroTerminatedString();
            }
        }

        internal T ReadSection<T>(FileReader reader, HsfFile header) where T : HSFSection, new()
        {
            T instance = new T();
            instance.Offset = reader.ReadUInt32();
            instance.Count = reader.ReadUInt32();
            if (instance.Offset == 0)
                return instance;

            using (reader.TemporarySeek(instance.Offset, System.IO.SeekOrigin.Begin)) {
                instance.Read(reader, header);
            }

            return instance;
        }

        internal void SaveSectionHeader(FileWriter writer, uint count, HsfFile header)
        {
            writer.Write(0);
            writer.Write(count);
        }
    }
}
