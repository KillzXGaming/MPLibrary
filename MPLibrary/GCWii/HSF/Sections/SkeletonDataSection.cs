using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using System.Runtime.InteropServices;

namespace MPLibrary
{
    public class SkeletonDataSection : HSFSection
    {
        public class Node
        {
            public string Name { get; set; }
            public Transform Transform;
        }

        public List<Node> Nodes = new List<Node>();

        public override void Read(FileReader reader, HsfFile header)
        {
            for (int i = 0; i < this.Count; i++) {
                Nodes.Add(new Node()
                {
                    Name = header.GetString(reader, reader.ReadUInt32()),
                    Transform = reader.ReadStruct<Transform>()
                });
            }
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                writer.Write(header.GetStringOffset(Nodes[i].Name));
                writer.WriteStruct(Nodes[i].Transform);
            }
        }
    }

}
