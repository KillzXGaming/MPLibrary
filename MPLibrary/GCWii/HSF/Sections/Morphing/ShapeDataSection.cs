using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;

namespace MPLibrary.GCN
{
    public class ShapeDataSection : HSFSection
    {
        public List<HSFShape> Shapes = new List<HSFShape>();

        public override void Read(FileReader reader, HsfFile header)
        {
            for (int i = 0; i < this.Count; i++)
            {
                HSFShape shape = new HSFShape();
                shape.Name = header.GetString(reader, reader.ReadUInt32());
                this.Shapes.Add(shape);

                shape.Counts = reader.ReadUInt16s(2);
                shape.BufferSymbolIdx = reader.ReadInt32();
            }
            Console.WriteLine();
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            for (int i = 0; i < this.Shapes.Count; i++)
            {
                writer.Write(header.GetStringOffset(Shapes[i].Name));
                writer.Write(Shapes[i].BufferIndices.Count);
                writer.Write(Shapes[i].BufferSymbolIdx);
            }
        }
    }

    public class HSFShape
    {
        public string Name;

        public ushort[] Counts = new ushort[2];

        public int BufferSymbolIdx;

        public List<int> BufferIndices = new List<int>();
    }
}
