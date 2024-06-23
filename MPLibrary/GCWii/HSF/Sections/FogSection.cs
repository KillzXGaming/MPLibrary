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
    public class FogSection : HSFSection
    {
        public GX.FogType FogType; //Set at runtime, value not used

        public float Start;
        public float End;

        public Vector4 Color;

        public override void Read(FileReader reader, HsfFile header) {
            FogType = (GX.FogType)reader.ReadUInt32();
            Start = reader.ReadSingle();
            End = reader.ReadSingle();
            Color = new Vector4(
                reader.ReadByte(), reader.ReadByte(),
                reader.ReadByte(), reader.ReadByte()) / 255.0f;
        }   

        public override void Write(FileWriter writer, HsfFile header) { 
            writer.Write((uint)(FogType));
            writer.Write(Start);
            writer.Write(End);
            writer.Write((byte)(Color.X * 255));
            writer.Write((byte)(Color.Y * 255));
            writer.Write((byte)(Color.Z * 255));
            writer.Write((byte)(Color.W * 255));
        }
    }
}
