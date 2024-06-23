using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;
using System.Numerics;

namespace MPLibrary.GCN
{
    public class HSFSection
    {
        public uint Offset;
        public uint Count;

        public virtual void Read(FileReader reader, HsfFile header)
        {

        }

        public virtual void Write(FileWriter writer, HsfFile header)
        {

        }
    }

    public enum PrimitiveType
    {
        Triangle = 0x02,
        Quad = 0x03,
        TriangleStrip = 0x04,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FaceData
    {
        public uint StringOffset;
        public uint SurfaceCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Transform
    {
        public Vector3XYZ Translate;
        public Vector3XYZ Rotate;
        public Vector3XYZ Scale;

        public Matrix4x4 ToMatrix()
        {
           return Matrix4x4.CreateScale(Scale.X, Scale.Y, Scale.Z) *
                  (Matrix4x4.CreateRotationX(Rotate.X) *
                   Matrix4x4.CreateRotationY(Rotate.Y) *
                   Matrix4x4.CreateRotationZ(Rotate.Z)) *
                  Matrix4x4.CreateTranslation(Translate.X, Translate.Y, Translate.Z);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Vector3XYZ
    {
        public float X, Y, Z;
        public override string ToString() => $"{X},{Y},{Z}";

        public Vector3XYZ() { }

        public Vector3XYZ(float x, float y, float z)
        {
            X = x; Y = y; Z = z;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vector2XYZ
    {
        public float X, Y;
        public override string ToString() => $"{X},{Y}";

        public Vector2XYZ(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ComponentData
    {
        public uint StringOffset;
        public uint DataCount;
        public uint DataOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexGroup
    {
        public short PositionIndex;
        public short NormalIndex;
        public short ColorIndex;
        public short UVIndex;

        public override string ToString() {
            return $"{PositionIndex} {NormalIndex} {ColorIndex} {UVIndex}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ColorRGB_8
    {
        public byte R;
        public byte G;
        public byte B;

        public ColorRGB_8(byte r, byte g, byte b) {
            R = r;
            G = g;
            B = b;
        }
    }

    public class Primative : ISection
    {
        public void Read(FileReader reader, HsfFile header)
        {

        }

        public void Write(FileWriter reader, HsfFile header)
        {

        }
    }
}
