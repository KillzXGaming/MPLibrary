using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STLibrary.IO;
using System.Runtime.InteropServices;

namespace MPLibrary
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
