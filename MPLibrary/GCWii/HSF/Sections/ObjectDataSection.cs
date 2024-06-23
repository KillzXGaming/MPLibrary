using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;
using System.Numerics;
using GCNRenderLibrary.Rendering;

namespace MPLibrary.GCN
{
    public enum ObjectType : int
    {
       NULL1 = 0,
       Replica = 1,
       Mesh = 2,
       Root = 3,
       Joint = 4,
       Effect = 5,
       Camera = 7,
       Light = 8,
       Map = 9,
    }

    public enum LightType : byte
    {
        Spot, Point, Infinite,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class HSFCamera
    {
        public Vector3XYZ Target;
        public Vector3XYZ Position;
        public float AspectRatio;
        public float Fov;
        public float Near;
        public float Far;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class HSFLight
    {
        public Vector3XYZ Position;
        public Vector3XYZ Target;
        public LightType Type; //SPOT, POINT, INF
        public byte R;
        public byte G;
        public byte B;
        public float unk2C;
        public float ref_distance;
        public float ref_brightness;
        public float cutoff;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class HSFObjectData
    {
        public const int SIZE = 0x144;

        public uint StringOffset;
        public ObjectType Type;
        public int ConstDataOffset;
        public int RenderFlags;
        public int ParentIndex;
        public int ChildrenCount;
        public int SymbolIndex;
        public Transform BaseTransform;
        public Transform CurrentTransform;
        public Vector3XYZ CullBoxMin;
        public Vector3XYZ CullBoxMax;

        public float BaseMorph;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public float[] MorphWeights;

        public int UnknownIndex = -1;
        public int FaceIndex = -1;
        public int VertexIndex = -1;
        public int NormalIndex = -1;
        public int ColorIndex = -1;
        public int TexCoordIndex = -1;
        public int MaterialDataOffset;
        public int AttributeIndex = -1;
        public byte Unknown2;
        public byte Unknown3;
        public byte ShapeType;
        public byte Unknown4;
        public int ShapeCount;
        public int ShapeSymbolIndex = -1;
        public int CluserCount;
        public int CluserSymbolIndex = -1;
        public int CenvCount;
        public int CenvIndex;
        public int ClusterPositionsOffset;
        public int ClusterNormalsOffset;

        public HSFObjectData()
        {
            MorphWeights = new float[32];

            Type = ObjectType.NULL1;

            CullBoxMin = new Vector3XYZ();
            CullBoxMax = new Vector3XYZ();

            BaseTransform.Translate = new Vector3XYZ();
            BaseTransform.Rotate = new Vector3XYZ();
            BaseTransform.Scale = new Vector3XYZ(1, 1, 1);
        }
    }

    public class HSFObject
    {
        /// <summary>
        /// The raw object data.
        /// </summary>
        public HSFObjectData Data;

        /// <summary>
        /// The light data if type is light.
        /// </summary>
        public HSFLight LightData;

        /// <summary>
        /// The camera data if type is camera.
        /// </summary>
        public HSFCamera CameraData;

        /// <summary>
        /// The parent of this node.
        /// </summary>
        public HSFObject Parent;

        /// <summary>
        /// The children that parent this node.
        /// </summary>
        public List<HSFObject> Children = new List<HSFObject>();

        /// <summary>
        /// The mesh instance attached if node is a mesh type.
        /// </summary>
        public Mesh MeshData;

        /// <summary>
        /// 
        /// </summary>
        public List<HSFEnvelope> Envelopes = new List<HSFEnvelope>();

        /// <summary>
        /// 
        /// </summary>
        public List<HSFCluster> ClusterData = new List<HSFCluster>();

        /// <summary>
        /// 
        /// </summary>
        public List<HSFShape> Shapes = new List<HSFShape>();

        /// <summary>
        /// The current transform in local space, no parenting data.
        /// </summary>
        public OpenTK.Matrix4 LocalMatrix;

        private string _name;

        /// <summary>
        /// The name of the node. The name is used to map animation data.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                if (AnimationData != null)
                    AnimationData.Name = value;
            }
        }

        /// <summary>
        /// The animations mapped to the node.
        /// </summary>
        public AnimationNode AnimationData = new AnimationNode(TrackMode.Object);

        //Animation data
        public OpenTK.Matrix4 AnimatedLocalMatrix = OpenTK.Matrix4.Identity;
        public OpenTK.Matrix4 InvertedBindPose = OpenTK.Matrix4.Identity;

        public bool IsAnimated = false;

        public bool IsVisible = true;

        public int Index = 0;

        //Rendering
        public List<SceneNode> Meshes = new List<SceneNode>();

        public HSFObject(string name, HSFObjectData obj) {
            Name = name;
            Data = obj;
            LocalMatrix = CalculateMatrix();
        }

        public HSFObject(HsfFile hsf, FileReader reader, int index)
        {
            Index = index;
            Read(hsf, reader, index);

            LocalMatrix = CalculateMatrix();
        }

        public void ResetAnimation()
        {
            IsVisible = true;
            IsAnimated = false;
            LocalMatrix = CalculateMatrix();
            AnimatedLocalMatrix = CalculateMatrix();
        }

        public void Read(HsfFile hsf, FileReader reader, int index)
        {
            long pos = reader.Position;

            this.Data = reader.ReadStruct<HSFObjectData>();

            Name = hsf.GetString(reader, this.Data.StringOffset);

            //search for other struct types to parse as
            reader.SeekBegin(pos + 16);

            switch (Data.Type)
            {
                case ObjectType.Light:
                    this.LightData = reader.ReadStruct<HSFLight>();
                    break;
                case ObjectType.Camera:
                    this.CameraData = reader.ReadStruct<HSFCamera>();
                    break;
            }

            //return to end pos
            reader.SeekBegin(pos + HSFObjectData.SIZE);
        }

        public void Write(FileWriter writer)
        {
            long pos = writer.Position;

            //write object struct
            writer.WriteStruct(this.Data);
            //write other structs and skip shared obj header
            writer.SeekBegin(pos + 16);

            switch (this.Data.Type)
            {
                case ObjectType.Light:
                    writer.WriteStruct(this.LightData);
                    break;
                case ObjectType.Camera:
                    writer.WriteStruct(this.CameraData);
                    break;
            }

            //go back to end
            writer.SeekBegin(pos + HSFObjectData.SIZE);
        }

        public bool HasHierachy()
        {
            return (Data.Type != ObjectType.Light && Data.Type != ObjectType.Camera);
        }

        public void UpdateMatrix()
        {
            LocalMatrix = CalculateMatrix();
            AnimatedLocalMatrix = CalculateMatrix();
        }

        public override string ToString() => Name;

        /// <summary>
        /// Computes and gets the transform in world space.
        /// </summary>
        public OpenTK.Matrix4 CalculateWorldMatrix()
        {
            if (Parent != null)
                return LocalMatrix * Parent.CalculateWorldMatrix();
            return LocalMatrix;
        }

        public void CreateAnimation()
        {
            AnimationData = new AnimationNode();
            AnimationData.Mode = TrackMode.Normal;
            AnimationData.Name = this.Name;

            CreateTrack(TrackEffect.Visible, 1.0f);
            CreateTrack((TrackEffect)25, 1.0f);
            CreateTrack((TrackEffect)26, 1.0f);
            CreateTrack((TrackEffect)27, 1.0f);
            CreateTrack(TrackEffect.TranslateX, Data.BaseTransform.Translate.X);
            CreateTrack(TrackEffect.TranslateY, Data.BaseTransform.Translate.Y);
            CreateTrack(TrackEffect.TranslateZ, Data.BaseTransform.Translate.Z);
            CreateTrack(TrackEffect.RotationX, Data.BaseTransform.Rotate.X);
            CreateTrack(TrackEffect.RotationY, Data.BaseTransform.Rotate.Y);
            CreateTrack(TrackEffect.RotationZ, Data.BaseTransform.Rotate.Z);
            CreateTrack(TrackEffect.ScaleX, Data.BaseTransform.Scale.X);
            CreateTrack(TrackEffect.ScaleY, Data.BaseTransform.Scale.Y);
            CreateTrack(TrackEffect.ScaleZ, Data.BaseTransform.Scale.Z);
        }

        private void CreateTrack(TrackEffect track, float value)
        {
            this.AnimationData.TrackList.Add(new AnimTrack(AnimationData, TrackMode.Normal, track, value));
        }

        private OpenTK.Matrix4 CalculateMatrix()
        {
            var translation = OpenTK.Matrix4.CreateTranslation(
                Data.BaseTransform.Translate.X,
                Data.BaseTransform.Translate.Y,
                Data.BaseTransform.Translate.Z);
            var rotationX = OpenTK.Matrix4.CreateRotationX(
                OpenTK.MathHelper.DegreesToRadians(Data.BaseTransform.Rotate.X));
            var rotationY = OpenTK.Matrix4.CreateRotationY(
                OpenTK.MathHelper.DegreesToRadians(Data.BaseTransform.Rotate.Y));
            var rotationZ = OpenTK.Matrix4.CreateRotationZ(
                OpenTK.MathHelper.DegreesToRadians(Data.BaseTransform.Rotate.Z));
            var scale = OpenTK.Matrix4.CreateScale(
                Data.BaseTransform.Scale.X,
                Data.BaseTransform.Scale.Y,
                Data.BaseTransform.Scale.Z);
            return scale * (rotationX * rotationY * rotationZ) * translation;
        }
    }

    public class ObjectDataSection : HSFSection
    {
        public List<HSFObject> Objects = new List<HSFObject>();

        public override void Read(FileReader reader, HsfFile header)
        {
            for (int i = 0; i < this.Count; i++)
            {
                reader.SeekBegin(this.Offset + (i * HSFObjectData.SIZE));
                Objects.Add(new HSFObject(header, reader, i));
            }
        }

        /// <summary>
        /// Creates an empty object 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static HSFObjectData CreateNewObject(ObjectType type)
        {
            HSFObjectData objectData = new HSFObjectData();
            objectData.Type = type;
            objectData.ConstDataOffset = 0; //Set at runtime
            objectData.RenderFlags = 0;
            objectData.ParentIndex = -1;
            objectData.ChildrenCount = 0;
            objectData.SymbolIndex = 0;
            objectData.BaseTransform = new Transform()
            {
                Translate = new Vector3XYZ(),
                Scale = new Vector3XYZ() { X = 1, Y = 1, Z = 1, },
                Rotate = new Vector3XYZ(),
            };
            objectData.CurrentTransform = new Transform() //Set at runtime
            {
                Translate = new Vector3XYZ(),
                Scale = new Vector3XYZ(),
                Rotate = new Vector3XYZ(),
            };
            objectData.MorphWeights = new float[33];
            objectData.CullBoxMin = new Vector3XYZ();
            objectData.CullBoxMax = new Vector3XYZ();
            objectData.FaceIndex = -1;
            objectData.VertexIndex = -1;
            objectData.NormalIndex = -1;
            objectData.ColorIndex = -1;
            objectData.TexCoordIndex = -1;
            objectData.MaterialDataOffset = 0; //Set at runtime
            objectData.AttributeIndex = -1;
            objectData.Unknown3 = 0;
            objectData.ShapeCount = 0;
            objectData.ShapeSymbolIndex = -1;
            objectData.CluserCount = 0;
            objectData.CluserSymbolIndex = 0;
            objectData.CenvCount = 0;
            objectData.CenvIndex = -1;
            objectData.ClusterPositionsOffset = 0;
            objectData.ClusterNormalsOffset = 0;

            if (type == ObjectType.Mesh)
            {
                objectData.AttributeIndex = 0;
                objectData.ParentIndex = 0;
            }

            return objectData;
        }

        internal void GenerateSymbols(HsfFile header, ref List<int> symbols)
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                var data = Objects[i].Data;
                if (data.Type == ObjectType.Light || data.Type == ObjectType.Camera)
                    continue;

                data.SymbolIndex = symbols.Count;
                for (int j = 0; j < Objects.Count; j++)
                {
                    if (Objects[j].Data.ParentIndex == i)
                        symbols.Add(j);
                }

                data.CluserSymbolIndex = symbols.Count;
                foreach (var cluster in Objects[i].ClusterData)
                    symbols.Add(header.ClusterData.Clusters.IndexOf(cluster));
            }
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                var obj = Objects[i].Data;
                obj.StringOffset = (uint)header.GetStringOffset(Objects[i].Name);

                header.ObjectNodes[i].Write(writer);
            }
        }

        public void WriteClusterPositions(FileWriter writer, HsfFile header)
        {
            foreach (var mesh in header.Meshes)
            {
                if (mesh.HasEnvelopes)
                {
                    writer.Align(0x20);

                    //Allocate positions for cluster envelopes to transform at runtime
                    mesh.ObjectData.ClusterPositionsOffset = (int)writer.Position;
                    for (int j = 0; j < mesh.Positions.Count; j++)
                    {
                        writer.Write(mesh.Positions[j].X);
                        writer.Write(mesh.Positions[j].Y);
                        writer.Write(mesh.Positions[j].Z);
                    }
                }
            }
        }

        public void WriteClusterNormals(FileWriter writer, HsfFile header)
        {
            foreach (var mesh in header.Meshes)
            {
                if (mesh.HasEnvelopes)
                {
                    writer.Align(0x20);

                    //Allocate normals for cluster envelopes to transform at runtime
                    mesh.ObjectData.ClusterNormalsOffset = (int)writer.Position;
                    for (int j = 0; j < mesh.Normals.Count; j++)
                    {
                        writer.Write(mesh.Normals[j].X);
                        writer.Write(mesh.Normals[j].Y);
                        writer.Write(mesh.Normals[j].Z);
                    }
                }
            }
        }
    }
}
