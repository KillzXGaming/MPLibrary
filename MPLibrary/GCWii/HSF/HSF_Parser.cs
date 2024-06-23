using GCNRenderLibrary.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using Toolbox.Core;
using Toolbox.Core.IO;
using static MPLibrary.GCWii.HSF.HSFJsonExporter;
using static Toolbox.Core.DDS;

namespace MPLibrary.GCN
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
        public List<Vector2> TexCoord0 = new List<Vector2>();
        public List<Vector4> Color0 = new List<Vector4>();

        public List<ShapeMorph> MorphShapes = new List<ShapeMorph>();

        public Dictionary<int, GXMesh> GXMeshes = new Dictionary<int, GXMesh>();

        public List<PrimitiveObject> Primitives = new List<PrimitiveObject>();

        public List<HSFEnvelope> Envelopes = new List<HSFEnvelope>();

        public bool HasEnvelopes => Envelopes.Count > 0;

        public HSFObjectData ObjectData;

        public HSFObject Object;

        public EventHandler OnSelected;

        public Mesh(HSFObject objectData, string name) {
            Object = objectData;
            ObjectData = objectData.Data;
            Name = name;
        }

        public void Init()
        {
            GXMeshes = GXMeshHandler.LoadPrimitives(this);
            if (GXMeshes.Count == 0)
                throw new Exception();
        }
    }

    public class ShapeMorph
    {
        public List<Vector3> Positions = new List<Vector3>();
        public List<Vector3> Normals = new List<Vector3>();
    }

    public class TextureAttribute
    {
        //The texture map data
        public AttributeData AttributeData { get; set; }

        //Texture instance, used to remap indices on save
        [JsonIgnore]
        public HSFTexture Texture { get; set; }

        //Animation data
        [JsonIgnore]
        public AnimationNode AnimationData = new AnimationNode(TrackMode.Attriubute);

        //Similar to materials, they just export with the first "name" which is probably just the symbol index
        public string Name { get; set; } = "0";

        public void CreateAnimation()
        {
            this.AnimationData = new AnimationNode();
            this.AnimationData.Name = String.IsNullOrEmpty(this.Name) ? "CustomAttribute" : this.Name;
            this.AnimationData.Mode = TrackMode.Material;

            CreateTrack(TrackEffect.CombinerBlending, this.AttributeData.BlendTextureAlpha);
            CreateTrack((TrackEffect)63, 0);

            CreateTrack((TrackEffect)57, 0);
            CreateTrack((TrackEffect)60, 0);

            CreateTrack((TrackEffect)64, 1);
            CreateTrack((TrackEffect)65, 1);
            CreateTrack((TrackEffect)66, 0);
            CreateTrack(TrackEffect.TranslateX, this.AttributeData.TexAnimStart.Position.X);
            CreateTrack(TrackEffect.TranslateY, this.AttributeData.TexAnimStart.Position.Y);
            CreateTrack(TrackEffect.TranslateZ, 0);

            CreateTrack(TrackEffect.RotationX, this.AttributeData.Rotation.X);
            CreateTrack(TrackEffect.RotationY, this.AttributeData.Rotation.Y);
            CreateTrack(TrackEffect.RotationZ, this.AttributeData.Rotation.Z);

            CreateTrack(TrackEffect.ScaleX, this.AttributeData.TexAnimStart.Scale.X);
            CreateTrack(TrackEffect.ScaleY, 1);
            CreateTrack(TrackEffect.ScaleZ, this.AttributeData.TexAnimStart.Scale.Y);
        }

        private void CreateTrack(TrackEffect track, float value)
        {
            this.AnimationData.TrackList.Add(new AnimTrack(AnimationData, TrackMode.Attriubute, track, value));
        }
    }

    public partial class Material : GXMaterial
    {
        /// <summary>
        /// The raw material data.
        /// </summary>
        public MaterialObject MaterialData { get; set; }

        /// <summary>
        /// The texture map list.
        /// </summary>
        public List<TextureAttribute> TextureAttributes = new List<TextureAttribute>();

        /// <summary>
        /// The render flags of the parent object node.
        /// </summary>
        public int RenderFlags
        {
            get { return ObjectData.RenderFlags; }
            set
            {
                ObjectData.RenderFlags = value;
            }
        }

        /// <summary>
        /// The animations mapped to the material.
        /// </summary>
        public AnimationNode AnimationData = new AnimationNode(TrackMode.Material);

        /// <summary>
        /// The parent object node.
        /// </summary>
        HSFObjectData ObjectData;

        /// <summary>
        /// The parent hsf resource file data.
        /// </summary>
        public HsfFile HsfFile;

        /// <summary>
        /// Determines if the current material renders with a light map or not.
        /// </summary>
        public bool HasLightMap
        {
            get { return (RenderFlags & HsfGlobals.HIGHLIGHT_ENABLE) != 0; }
        }

        /// <summary>
        /// Determines to alpha test by discarding alpha <= 0.5
        /// </summary>
        public bool HasAlphaTest
        {
            get { return (RenderFlags & HsfGlobals.PUNCHTHROUGH_ALPHA_BITS) != 0; }
            set
            {
                if (value)
                    RenderFlags |= HsfGlobals.PUNCHTHROUGH_ALPHA_BITS;
                else
                    RenderFlags &= ~HsfGlobals.PUNCHTHROUGH_ALPHA_BITS;
            }
        }

        /// <summary>
        /// Determines to billboard the current material.
        /// </summary>
        public bool HasBillboard
        {
            get { return (RenderFlags & HsfGlobals.BILLBOARD) != 0; }
            set
            {
                if (value)
                    RenderFlags |= HsfGlobals.BILLBOARD;
                else
                    RenderFlags &= ~HsfGlobals.BILLBOARD;
            }
        }

        /// <summary>
        /// Determines to write depth or not.
        /// </summary>
        public bool NoDepthWrite
        {
            get { return (MaterialData.AltFlags & HsfGlobals.PASS_BITS) != 0; }
            set
            {
                if (value)
                    MaterialData.AltFlags = 1;
                else
                    MaterialData.AltFlags = 0;
            }
        }

        /// <summary>
        /// Determines to cull back faces or not.
        /// </summary>
        public bool ShowBothFaces
        {
            get { return (RenderFlags & HsfGlobals.DONT_CULL_BACKFACES) != 0; }
            set
            {
                if (value)
                    RenderFlags |= HsfGlobals.DONT_CULL_BACKFACES;
                else
                    RenderFlags &= ~HsfGlobals.DONT_CULL_BACKFACES;
            }
        }

        /// <summary>
        /// Determines to write depth or not.
        /// </summary>
        public bool Hide
        {
            get { return (RenderFlags & HsfGlobals.OBJ_HIDE) != 0; }
            set
            {
                if (value)
                    RenderFlags |= HsfGlobals.OBJ_HIDE;
                else
                    RenderFlags &= ~HsfGlobals.OBJ_HIDE;
            }
        }

        /// <summary>
        /// The blending mode used.
        /// </summary>
        public RenderBlending BlendMode
        {
            get
            {
                var flags = this.RenderFlags | this.MaterialData.MaterialFlags;
                if ((flags & HsfGlobals.BLEND_ZERO_INVSRCCLR) != 0)
                    return RenderBlending.Zero_InvSrcColor;

                if ((flags & HsfGlobals.BLEND_SRCALPHA_ONE) != 0)
                    return RenderBlending.SrcAlpha_One;

                return RenderBlending.SrcAlpha_InvSrcAlpha;
            }
            set
            {
                //Clear out flags. Both material and object node can control these
                RenderFlags &= ~HsfGlobals.BLEND_ZERO_INVSRCCLR;
                RenderFlags &= ~HsfGlobals.BLEND_SRCALPHA_ONE;
                MaterialData.MaterialFlags &= ~HsfGlobals.BLEND_ZERO_INVSRCCLR;
                MaterialData.MaterialFlags &= ~HsfGlobals.BLEND_SRCALPHA_ONE;

                //Just directly set the render flags
                switch (value)
                {
                    case RenderBlending.Zero_InvSrcColor:
                        RenderFlags |= HsfGlobals.BLEND_ZERO_INVSRCCLR;
                        break;
                    case RenderBlending.SrcAlpha_One:
                        RenderFlags |= HsfGlobals.BLEND_SRCALPHA_ONE;
                        break;
                }
            }
        }

        /// <summary>
        /// Gets the texture instance of the indexed texture map. Returns null if index is invalid.
        /// </summary>
        public HSFTexture GetTexture(int i)
        {
            if (i < 0 || TextureAttributes.Count <= i) return null;

            return TextureAttributes[i].Texture;
        }

        public Material()
        {
            MaterialData = new MaterialObject();
            MaterialData.VertexMode = LightingChannelFlags.LightingSpecular;
            MaterialData.AmbientColor = new ColorRGB_8(127, 127, 127);
            MaterialData.MaterialColor = new ColorRGB_8(255, 255, 255);
            MaterialData.ShadowColor = new ColorRGB_8(255, 255, 255);
            MaterialData.HiliteScale = 50;
        }

        public void Init(HsfFile hsfFile, HSFObjectData objectData)
        {
            ObjectData = objectData;

            HsfFile = hsfFile;

            ReloadPolygonState();
            ReloadTextures();
            ReloadColors();
            ReloadBlend();
            ReloadTevStages();
        }

        public void AnimateAttributes(TextureAttribute att, AttributeAnimController controller)
        {
            int id = TextureAttributes.IndexOf(att);

            this.TextureMatrices[id].SetAnimation(GXTextureMatrix.Track.TransU, controller.TranslateX);
            this.TextureMatrices[id].SetAnimation(GXTextureMatrix.Track.TransV, controller.TranslateY);
            this.TextureMatrices[id].SetAnimation(GXTextureMatrix.Track.ScaleU, controller.ScaleX);
            this.TextureMatrices[id].SetAnimation(GXTextureMatrix.Track.ScaleV, controller.ScaleY);
            this.TextureMatrices[id].SetAnimation(GXTextureMatrix.Track.Rotate, controller.RotateZ);

            att.AttributeData.BlendTextureAlpha = controller.CombinerBlending;

            if (controller.TextureIndex != -1)
            {
                this.Textures[id].TextureIndex = controller.TextureIndex;
                this.Textures[id].Texture = HsfFile.Textures[controller.TextureIndex].Name;
            }

            this.ReloadTevStages();
        }

        public void CreateAnimation()
        {
            this.AnimationData = new AnimationNode();
            this.AnimationData.Name = String.IsNullOrEmpty(this.Name) ? "CustomMaterialAnim" : this.Name;
            this.AnimationData.Mode = TrackMode.Material;

            CreateTrack(TrackEffect.AmbientColorR, this.MaterialData.AmbientColor.R / 255.0f);
            CreateTrack(TrackEffect.AmbientColorG, this.MaterialData.AmbientColor.G / 255.0f);
            CreateTrack(TrackEffect.AmbientColorB, this.MaterialData.AmbientColor.B / 255.0f);

            CreateTrack(TrackEffect.ShadowColorR, this.MaterialData.ShadowColor.R / 255.0f);
            CreateTrack(TrackEffect.ShadowColorG, this.MaterialData.ShadowColor.G / 255.0f);
            CreateTrack(TrackEffect.ShadowColorB, this.MaterialData.ShadowColor.B / 255.0f);

            CreateTrack(TrackEffect.MaterialColorR, this.MaterialData.MaterialColor.R / 255.0f);
            CreateTrack(TrackEffect.MaterialColorG, this.MaterialData.MaterialColor.G / 255.0f);
            CreateTrack(TrackEffect.MaterialColorB, this.MaterialData.MaterialColor.B / 255.0f);

            CreateTrack(TrackEffect.HiliteScale, this.MaterialData.HiliteScale);
            CreateTrack(TrackEffect.Transparency, this.MaterialData.TransparencyInverted);
            CreateTrack(TrackEffect.MatUnknown3, this.MaterialData.Unknown3);
            CreateTrack(TrackEffect.MatUnknown4, this.MaterialData.Unknown4);
            CreateTrack(TrackEffect.ReflectionIntensity, this.MaterialData.ReflectionIntensity);
            CreateTrack(TrackEffect.MatUnknown5, this.MaterialData.Unknown5);
        }

        private void CreateTrack(TrackEffect track, float value)
        {
            this.AnimationData.TrackList.Add(new AnimTrack(AnimationData, TrackMode.Material, track, value));
        }

        public void ReloadTevStages(bool updateShaders = false)
        {
            var textureMaps = TextureAttributes.Select(x => x.AttributeData).Where(x => x != null).ToList();
            this.HasPostTexMtx = true;
            kColorIdx = 0;

            //These stages are generated based on what I've seen used in ghirda + symbol map
            if (textureMaps.Count == 0)
                SetTevStageNoTexture();
            else
                SetTevStageTexture();

            if (this.HasLightMap)
            {
                //Set the last texture as lightmap
                int id = textureMaps.Count; //Next texture
                this.Textures[id] = new GXSampler();
                this.Textures[id].Texture = "Lightmap";
                this.Textures[id].MinFilter = GX.TextureFilter.Linear;
                this.Textures[id].MagFilter = GX.TextureFilter.Linear;
                this.Textures[id].WrapX = GX.WrapMode.CLAMP;
                this.Textures[id].WrapY = GX.WrapMode.CLAMP;
            }

            if (this.MaterialData.ReflectionIntensity != 0)
            {
                int id = textureMaps.Count + 1; //Next texture
                this.Textures[id] = new GXSampler();
                this.Textures[id].Texture = "Reflectmap";
                this.Textures[id].MinFilter = GX.TextureFilter.Linear;
                this.Textures[id].MagFilter = GX.TextureFilter.Linear;
                this.Textures[id].WrapX = GX.WrapMode.CLAMP;
                this.Textures[id].WrapY = GX.WrapMode.CLAMP;
            }

            if (updateShaders)
                this.RenderScene?.ReloadShader();
        }

        /// <summary>
        /// The blending flags for displaying blend states.
        /// </summary>
        public enum RenderBlending
        {
            /// <summary>
            /// Blend with src alpha and inverse src alpha.
            /// </summary>
            SrcAlpha_InvSrcAlpha = 0x0,

            /// <summary>
            /// Blend with src alpha and one.
            /// </summary>
            SrcAlpha_One = 0x10,

            /// <summary>
            /// Blend with zero and inverse source color.
            /// </summary>
            Zero_InvSrcColor = 0x20,
        }
    }

    public class HSFEnvelope
    {
        public List<RiggingSingleBind> SingleBinds = new List<RiggingSingleBind>();
        public List<RiggingDoubleBind> DoubleBinds = new List<RiggingDoubleBind>();
        public List<RiggingDoubleWeight> DoubleWeights = new List<RiggingDoubleWeight>();
        public List<RiggingMultiBind> MultiBinds = new List<RiggingMultiBind>();
        public List<RiggingMultiWeight> MultiWeights = new List<RiggingMultiWeight>();

        public uint VertexCount;
        public uint NameSymbol = 0xCCCCCCCC; //0xCCCCCCCC (Usually that value if value is null and unused)

        public uint CopyCount; //Matches vertex count when no binds are used, else 0
    }

    /// <summary>
    /// Represents an HSF texture.
    /// </summary>
    public class HSFTexture
    {
        /// <summary>
        /// The name of the texture.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The raw texture header data.
        /// </summary>
        public TextureInfo TextureInfo { get; set; }

        /// <summary>
        /// The raw image data encoded/compressed.
        /// </summary>
        public byte[] ImageData { get; set; }

        /// <summary>
        /// The raw palette header data.
        /// </summary>
        public PaletteInfo PaletteInfo;

        /// <summary>
        /// The raw palette data.
        /// </summary>
        public ushort[] PaletteData = new ushort[0];

        /// <summary>
        /// The format as GX texture format.
        /// </summary>
        public Decode_Gamecube.TextureFormats GcnFormat;

        /// <summary>
        /// The format as GX palette texture format.
        /// </summary>
        public Decode_Gamecube.PaletteFormats GcnPaletteFormat;

        /// <summary>
        /// The render texture instance.
        /// </summary>
        public GLGXTexture RenderTexture;

        public HSFTexture()
        {
            TextureInfo = new TextureInfo();
            Name = "";
        }

        /// <summary>
        /// Sets the palette data from ushorts.
        /// </summary>
        public void SetPalette(ushort[] palette)
        {
            PaletteData = palette;
        }

        /// <summary>
        /// Gets the format ID given the GX texture format.
        /// </summary>
        public static int GetFormatId(Decode_Gamecube.TextureFormats format)
        {
            return TextureSection.FormatList.FirstOrDefault(x => x.Value == format).Key;
        }

        /// <summary>
        /// Checks if palette data is present.
        /// </summary>
        public bool HasPaletteData()
        {
            return PaletteData != null && PaletteData.Length > 0;
        }

        public HSFTexture(string name, TextureInfo info, byte[] imageData)
        {
            Name = name;
            TextureInfo = info;
            ImageData = imageData;
            GcnFormat = FormatList[info.Format];
            if (GcnFormat == Decode_Gamecube.TextureFormats.C8)
            {
                if (TextureInfo.Bpp == 4)
                    GcnFormat = Decode_Gamecube.TextureFormats.C4;
            }
            if (PaletteFormatList.ContainsKey(info.Format))
                GcnPaletteFormat = PaletteFormatList[info.Format];
        }

        /// <summary>
        /// Gets the palette data in ushorts.
        /// </summary>
        public ushort[] GetPalette()
        {
            if (PaletteData == null || PaletteData.Length == 0) return new ushort[0];

            return PaletteData;
        }

        public byte[] GetPaletteBytes()
        {
            if (PaletteData == null || PaletteData.Length == 0) return new byte[0];

            var mem = new MemoryStream();
            using (var wr = new FileWriter(mem))
            {
                wr.SetByteOrder(true);
                wr.Write(PaletteData);
            }
            return mem.ToArray();
        }


        private static Dictionary<int, Decode_Gamecube.TextureFormats> FormatList = new Dictionary<int, Decode_Gamecube.TextureFormats>()
        {
            { 0x00, Decode_Gamecube.TextureFormats.I8 },
            { 0x01, Decode_Gamecube.TextureFormats.I8 },
            { 0x02, Decode_Gamecube.TextureFormats.IA4 },
            { 0x03, Decode_Gamecube.TextureFormats.IA8 },
            { 0x04, Decode_Gamecube.TextureFormats.RGB565 },
            { 0x05, Decode_Gamecube.TextureFormats.RGB5A3 },
            { 0x06, Decode_Gamecube.TextureFormats.RGBA32 },
            { 0x07, Decode_Gamecube.TextureFormats.CMPR },
            { 0x09, Decode_Gamecube.TextureFormats.C8 }, //C4 if BPP == 4
            { 0x0A, Decode_Gamecube.TextureFormats.C8 }, //C4 if BPP == 4
            { 0x0B, Decode_Gamecube.TextureFormats.C8 }, //C4 if BPP == 4
        };

        private static Dictionary<int, Decode_Gamecube.PaletteFormats> PaletteFormatList = new Dictionary<int, Decode_Gamecube.PaletteFormats>()
        {
            { 0x09, Decode_Gamecube.PaletteFormats.RGB565 }, //C4 if BPP == 4
            { 0x0A, Decode_Gamecube.PaletteFormats.RGB5A3 }, //C4 if BPP == 4
            { 0x0B, Decode_Gamecube.PaletteFormats.IA8 }, //C4 if BPP == 4
        };
    }

    //Parser based on https://github.com/Ploaj/Metanoia/blob/master/Metanoia/Formats/GameCube/HSF.cs
    public class HsfFile
    {
        internal static string NullString => "<0>";

        public FogSection FogData = new FogSection();

        public AttributeSection AttributeData = new AttributeSection();

        internal ObjectDataSection ObjectData = new ObjectDataSection();

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

        public List<HSFObject> ObjectNodes = new List<HSFObject>();

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

            //Prepare classes that connect the data for easier editing and reference automation
            List<HSFObject> objects = ObjectData.Objects;

            //Create a list of all the meshes from objects
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Data.Type == ObjectType.Mesh)
                    Meshes.Add(new Mesh(objects[i], objects[i].Name));
            }

            reader.SeekBegin(0);

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

            for (int i = 0; i < objects.Count; i++)
            {
                if (!objects[i].HasHierachy())
                    continue;

                if (objects[i].Data.ParentIndex != -1)
                    objects[i].Parent = objects[objects[i].Data.ParentIndex];

                for (int j = 0; j < objects[i].Data.ChildrenCount; j++)
                {
                    var cindex = SymbolData.SymbolIndices[objects[i].Data.SymbolIndex + j];
                    var child = objects[cindex];
                    objects[i].Children.Add(child);
                }
            }

            foreach (var obj in objects)
            {
                obj.MeshData = Meshes.FirstOrDefault(x => x.ObjectData == obj.Data);
            }

            foreach (var obj in objects)
            {
                if (obj.HasHierachy())
                    obj.InvertedBindPose = OpenTK.Matrix4.Invert(obj.CalculateWorldMatrix());
            }

            ObjectNodes.AddRange(objects);

            List<TextureAttribute> attributes = new List<TextureAttribute>();
            for (int i = 0; i < this.AttributeData.Attributes.Count; i++)
            {
                var name = AttributeData.AttributeNames[i];
                attributes.Add(new TextureAttribute()
                {
                    Name = name,
                    AttributeData = AttributeData.Attributes[i],
                    Texture = AttributeData.Attributes[i].TextureIndex >= 0 ? Textures[AttributeData.Attributes[i].TextureIndex] : null,
                });
            }

            foreach (var material in Materials)
            {
                var matData = material.MaterialData;
                if (matData.TextureCount > 0)
                {
                    var symbol = matData.FirstSymbol;
                    for (int i = 0; i < matData.TextureCount; i++)
                    {
                        var index = SymbolData.SymbolIndices[symbol++];
                        material.TextureAttributes.Add(attributes[index]);
                    }
                }
            }

            foreach (var cluster in this.ClusterData.Clusters)
            {
                cluster.BufferIndices = new int[cluster.BufferCount];
                for (int i = 0; i < cluster.BufferCount; i++)
                    cluster.BufferIndices[i] = this.SymbolData.SymbolIndices[cluster.BufferSymbolIdx + i];
            }

            //Link motion data directly to materials, attributes, and object nodes
            //The game indexes these directly so they should be auto referenced
            foreach (var anim in this.MotionData.Animations)
            {
                foreach (AnimationNode group in anim.AnimGroups)
                {
                    //Object node
                    //The group does not have to be directed as node type
                    //Animations can combine material and node animations
                    var ob = ObjectNodes.FirstOrDefault(x => x.Name == group.Name);
                    if (ob != null)
                        ob.AnimationData = group;

                    switch (group.Mode)
                    {
                        //Material
                        case TrackMode.Material:
                            if (Materials.Count > group.ValueIndex)
                                Materials[group.ValueIndex].AnimationData = group;
                            break;
                        //Attribute data
                        case TrackMode.Attriubute:
                            if (attributes.Count > group.ValueIndex)
                                attributes[group.ValueIndex].AnimationData = group;
                            break;
                    }
                }
            }
        }

        internal void AddTexture(string name, TextureInfo info, byte[] imageData) {
            Textures.Add(new HSFTexture(name, info, imageData));
        }

        internal void AddPalette(List<PaletteInfo> paletteInfos, List<ushort[]> paletteDatas) {
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

        public List<PrimitiveObject> GetPrimitiveList()
        {
            List<PrimitiveObject> primitives = new List<PrimitiveObject>();
            foreach (var mesh in Meshes)
                primitives.AddRange(mesh.Primitives);
            return primitives;
        }

        //Regenerates symbols used by attributes and objects
        private void GenerateSymbolTable()
        {
            List<int> symbols = new List<int>();

            for (int i = 0; i < Materials.Count; i++)
            {
                var data = Materials[i].MaterialData;

                data.FirstSymbol = symbols.Count;
                foreach (var att in Materials[i].TextureAttributes)
                {
                    var index = AttributeData.Attributes.IndexOf(att.AttributeData);
                    if (!symbols.Contains(index))
                        symbols.Add(index);
                }
                data.TextureCount = Materials[i].TextureAttributes.Count;
            }
            ObjectData.GenerateSymbols(this, ref symbols);

            foreach (var cluster in this.ClusterData.Clusters)
            {
                cluster.BufferSymbolIdx = symbols.Count;
                symbols.AddRange(cluster.BufferIndices);
            }

            foreach (var shape in this.ShapeData.Shapes)
            {
                shape.BufferSymbolIdx = symbols.Count;
                symbols.AddRange(shape.BufferIndices);
            }

            SymbolData.SymbolIndices = symbols.ToArray();
        }

        //Saves and applies all the attributes used for meshes 
        private void SaveMeshAttributes()
        {
            List<AttributeData> attributes = new List<AttributeData>();
            List<string> attributeNames = new List<string>();
            for (int i = 0; i < Materials.Count; i++)
            {
                foreach (var tex in Materials[i].TextureAttributes)
                {
                    //Apply index
                    tex.AttributeData.TextureIndex = this.Textures.IndexOf(tex.Texture);

                    if (!attributes.Contains(tex.AttributeData))
                    {
                        attributeNames.Add(tex.Name);
                        attributes.Add(tex.AttributeData);
                    }
                }
            }

            AttributeData.Attributes = attributes;
            AttributeData.AttributeNames = attributeNames;
        }

        private void SaveMotionReferences()
        {
            //Only remap if meshes are present and has a single animation.
            if (this.MotionData.Animations.Count != 1 || Meshes.Count == 0)
                return;

            //HSF never has more than one animation so only need to assign to one animation
            var anim = this.MotionData.Animations.FirstOrDefault();
            //Clear out material and attribute nodes so we can re index present ones
            var groups = anim.AnimGroups.ToList();
            foreach (AnimationNode group in groups)
            {
                if (group.Mode == TrackMode.Attriubute ||
                    group.Mode == TrackMode.Material)
                {
                    anim.AnimGroups.Remove(group);
                }
            }

            //Remap indices for all animated material data
            foreach (var mat in this.Materials)
            {
                mat.AnimationData.ValueIndex = (short)this.Materials.IndexOf(mat);
                mat.AnimationData.Name = mat.Name;

                if (mat.AnimationData.TrackList.Count > 0)
                {
                    foreach (var track in mat.AnimationData.TrackList)
                        track.ValueIdx = mat.AnimationData.ValueIndex;

                    anim.AnimGroups.Add(mat.AnimationData);
                }
                foreach (var att in mat.TextureAttributes)
                {
                    att.AnimationData.ValueIndex = (short)this.AttributeData.Attributes.IndexOf(att.AttributeData);

                    foreach (var track in att.AnimationData.TrackList)
                        track.ValueIdx = att.AnimationData.ValueIndex;


                    if (att.AnimationData.TrackList.Count > 0)
                        anim.AnimGroups.Add(att.AnimationData);
                }
            }
            this.MotionData.Animations[0] = anim;
        }

        private void Write(FileWriter writer)
        {
            Meshes.Clear();
            foreach (var obj in ObjectNodes)
            {
                if (obj.MeshData != null)
                    Meshes.Add(obj.MeshData);
            }

            SaveMeshAttributes();
            SaveMotionReferences();

            this.ObjectData.Objects.Clear();
            this.ObjectData.Objects.AddRange(this.ObjectNodes);

            foreach (var obj in this.ObjectNodes)
            {
                if (obj.HasHierachy())
                {
                    obj.Data.ParentIndex = -1;

                    if (obj.Parent != null)
                        obj.Data.ParentIndex = this.ObjectNodes.IndexOf(obj.Parent);

                    obj.Data.ChildrenCount = obj.Children.Count;
                }
            }

            this.PaletteData.Palettes.Clear();
            this.PaletteData.PaletteData.Clear();

            int paletteIdx = 0;
            foreach (var tex in this.Textures)
            {
                tex.TextureInfo.PaletteIndex = -1;
                if (tex.PaletteData?.Length > 0)
                {
                    tex.TextureInfo.PaletteIndex = paletteIdx++;

                    this.PaletteData.Palettes.Add(tex.PaletteInfo);
                    this.PaletteData.PaletteData.Add(tex.PaletteData);
                }
            }

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

            //Apply indices for obj data
            foreach (var mesh in Meshes) {
                if (mesh.Color0.Count > 0) mesh.ObjectData.ColorIndex = numUsedColors;
                if (mesh.Positions.Count > 0) mesh.ObjectData.VertexIndex = numUsedPositions;
                if (mesh.Normals.Count > 0) mesh.ObjectData.NormalIndex = numUsedNormals;

                mesh.ObjectData.FaceIndex = numFaces;
                mesh.ObjectData.CenvIndex = numUsedRigs;
                mesh.ObjectData.CenvCount = mesh.Envelopes.Count;

                foreach (var mat_idx in mesh.GXMeshes.Keys)
                {
                    if (this.Materials[mat_idx].TextureAttributes.Count > 0)
                        mesh.ObjectData.AttributeIndex = 0;
                }

                if (mesh.Positions.Count > 0) numUsedPositions++;
                if (mesh.Normals.Count > 0) numUsedNormals++;
                if (mesh.TexCoord0.Count > 0) numUsedTexCoords++;
                if (mesh.Color0.Count > 0) numUsedColors++;
                if (mesh.Primitives.Count > 0) numUsedPrimitives++;
                if (mesh.HasEnvelopes) numUsedRigs += mesh.Envelopes.Count;

                numFaces++;
            }

            foreach (var obj in ObjectData.Objects)
            {
                if (obj.Data.Type != ObjectType.Mesh && obj.HasHierachy())
                {
                    obj.Data.VertexIndex = numUsedPositions - 1;
                    obj.Data.ColorIndex = numUsedColors - 1;
                    obj.Data.NormalIndex = numUsedNormals - 1;
                    obj.Data.CenvIndex = numUsedRigs - 1;
                    obj.Data.FaceIndex = numUsedPrimitives - 1;
                    obj.Data.AttributeIndex = 0;
                    obj.Data.ClusterPositionsOffset = 0;
                    obj.Data.ClusterNormalsOffset = 0;
                }
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
            SaveSectionHeader(writer, (uint)numUsedRigs, this);
            SaveSectionHeader(writer, (uint)SkeletonData.Count, this);
            SaveSectionHeader(writer, (uint)PartData.Parts.Count, this);
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

            ObjectData.WriteClusterPositions(writer, this);

            writer.WriteUint32Offset(48);
            if (numUsedNormals > 0) {
                NormalData.Write(writer, this);
            }

            ObjectData.WriteClusterNormals(writer, this);

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
            fileStrings.AddRange(MotionData.GetStrings());
            fileStrings.AddRange(Materials.Select(x => x.Name));
            fileStrings.AddRange(AttributeData.AttributeNames);
            fileStrings.AddRange(Meshes.Select(x => x.Name));
            fileStrings.AddRange(ObjectNodes.Select(x => x.Name));
            fileStrings.AddRange(Textures.Select(x => x.Name));
            fileStrings.AddRange(PartData.Parts.Select(x => x.Name));
            fileStrings.AddRange(ClusterData.GetStrings());
            fileStrings.AddRange(ShapeData.Shapes.Select(x => x.Name));

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
                    Meshes[i].Color0 = colors;
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
                    Meshes[i].TexCoord0 = uvs;
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
