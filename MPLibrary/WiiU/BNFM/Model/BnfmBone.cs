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
    /// <summary>
    /// Represents a bone/node used for skinning and object placement.
    /// </summary>
    public class BnfmBone : ListItem, IFileData
    {
        [Browsable(true)]
        [Category("Bone Data")]
        [DisplayName("Name")]
        public string Text
        {
            get { return Name.Value; }
            set
            {
                Name = new StringHash(value);
            }
        }

        [Browsable(true)]
        [Category("Bone Data")]
        [DisplayName("Scale X")]
        public float ScaleX
        {
            get { return Scale.X; }
            set { Scale = new Vector3(value, Scale.Y, Scale.Z); }
        }

        [Browsable(true)]
        [Category("Bone Data")]
        [DisplayName("Scale Y")]
        public float ScaleY
        {
            get { return Scale.Y; }
            set { Scale = new Vector3(Scale.X, value, Scale.Z); }
        }

        [Browsable(true)]
        [Category("Bone Data")]
        [DisplayName("Scale Z")]
        public float ScaleZ
        {
            get { return Scale.Z; }
            set { Scale = new Vector3(Scale.X, Scale.Y, value); }
        }

        [Browsable(true)]
        [Category("Bone Data")]
        [DisplayName("Position X")]
        public float TranslateX
        {
            get { return Position.X; }
            set { Position = new Vector3(value, Position.Y, Position.Z); }
        }

        [Browsable(true)]
        [Category("Bone Data")]
        [DisplayName("Position Y")]
        public float TranslateY
        {
            get { return Position.Y; }
            set { Position = new Vector3(Position.X, value, Position.Z); }
        }

        [Browsable(true)]
        [Category("Bone Data")]
        [DisplayName("Position Z")]
        public float TranslateZ
        {
            get { return Position.Z; }
            set { Position = new Vector3(Position.X, Position.Y, value); }
        }

        [Browsable(true)]
        [Category("Bone Data")]
        [DisplayName("Rotation X")]
        public float RotationX
        {
            get { return RotationEuler.X; }
            set { RotationEuler = new Vector3(value, RotationEuler.Y, RotationEuler.Z); }
        }

        [Browsable(true)]
        [Category("Bone Data")]
        [DisplayName("Rotation Y")]
        public float RotationY
        {
            get { return RotationEuler.Y; }
            set { RotationEuler = new Vector3(RotationEuler.X, value, RotationEuler.Z); }
        }

        [Browsable(true)]
        [Category("Bone Data")]
        [DisplayName("Rotation Z")]
        public float RotationZ
        {
            get { return RotationEuler.Z; }
            set { RotationEuler = new Vector3(RotationEuler.X, RotationEuler.Y, value); }
        }

        /// <summary>
        /// The name of the bone.
        /// </summary>
        [Browsable(false)]
        public StringHash Name { get; set; } = new StringHash("");

        /// <summary>
        /// The parent bone.
        /// </summary>
        [Browsable(false)]
        public BnfmBone Parent { get; set; }

        /// <summary>
        /// The position of the bone.
        /// </summary>
        [Browsable(false)]
        public Vector3 Position { get; set; }

        /// <summary>
        /// The scale of the bone.
        /// </summary>
        [Browsable(false)]
        public Vector3 Scale { get; set; }

        /// <summary>
        /// The rotation of the bone.
        /// </summary>
        [Browsable(false)]
        public Vector3 RotationEuler { get; set; }

        /// <summary>
        /// The inverse matrix used for skinning.
        /// </summary>
        [Browsable(false)]
        public Matrix4x4 InverseRotationMatrix { get; set; }
        /// <summary>
        /// .
        /// </summary>
        [Browsable(false)]
        public Matrix4x4 IdentityMatrix2 { get; set; }

        /// <summary>
        /// The matrix 1 table for unknown purpose. Always identity matrix
        /// </summary>
        [Browsable(false)]
        public BnfmMatrix IdentityMatrix { get; set; }

        /// <summary>
        /// The matrix used for attaching a bounding box.
        /// </summary>
        [Browsable(false)]
        public BnfmMatrix BoundingMatrix { get; set; }

        /// <summary>
        /// Value to determine if smooth skinning is used (1 or 0)
        /// </summary>
        [Browsable(true)]
        [Category("Bone Data")]
        [DisplayName("Bone Type")]
        public BoneType SkinningFlag { get; set; }

        /// <summary>
        /// Unknown value. Ranges from 0, 1, 2, or 3
        /// </summary>
        [Browsable(true)]
        [Category("Bone Data")]
        [DisplayName("Unknown2")]
        public uint Unknown2 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(true)]
        [Category("Bone Data")]
        [DisplayName("Unknown3")]
        public uint Unknown3 { get; set; } = 0; //Always 0?

        [Browsable(false)]
        public uint Value = 1; //Always 1

        [Browsable(false)]
        private StringHash ParentName { get; set; }

        public BnfmBone()
        {
            Name = new StringHash("");
            Parent = null;

            Position = Vector3.Zero;
            Scale = Vector3.One;
        }

        void IFileData.Read(FileReader reader)
        {
            Name = reader.LoadStringHash();
            ParentName = reader.LoadStringHash();
            reader.ReadUInt32(); //Current bone pointer
            Parent = reader.Load<BnfmBone>(); //parent
            SkinningFlag = (BoneType)reader.ReadUInt32();
            Index = reader.ReadInt32();
            Unknown2 = reader.ReadUInt32();
            Position = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle());
            Scale = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle());
            RotationEuler = new Vector3(
           reader.ReadSingle(),
           reader.ReadSingle(),
           reader.ReadSingle());
            Value = reader.ReadUInt32();
            Unknown3 = reader.ReadUInt32();

            InverseRotationMatrix = reader.ReadMatrix4();
            IdentityMatrix2 = reader.ReadMatrix4();
            IdentityMatrix = reader.Load<BnfmMatrix>();
            BoundingMatrix = reader.Load<BnfmMatrix>();
            Flag = reader.ReadInt32();

            if (IdentityMatrix != null && IdentityMatrix.Matrix != Matrix4x4.Identity)
            {
                throw new Exception();
            }
        }

        void IFileData.Write(FileWriter writer)
        {
            ParentName = Parent == null ? new StringHash("") : Parent.Name;

            writer.Write(Name);
            writer.Write(ParentName);
            writer.Save(this); //pointer to current bone
            writer.Save(Parent);
            writer.Write((uint)SkinningFlag);
            writer.Write(Index);
            writer.Write(Unknown2);
            writer.Write(Position);
            writer.Write(Scale);
            writer.Write(RotationEuler.X);
            writer.Write(RotationEuler.Y);
            writer.Write(RotationEuler.Z);
            writer.Write(Value);
            writer.Write(Unknown3);
            writer.Write(InverseRotationMatrix);
            writer.Write(IdentityMatrix2);
            //Save later
            writer.Save(IdentityMatrix);
            writer.Save(BoundingMatrix);
            writer.Write(Flag);
        }

        public enum BoneType
        {
            Normal,
            Skinning,
        }
    }
}
