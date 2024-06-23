using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;

namespace MPLibrary.GCN
{
    public class SkeletonDataSection : HSFSection
    {
        public class Node
        {
            public string Name { get; set; }
            public Transform Transform;

            public override string ToString() => Name;
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
              //  Console.WriteLine(Nodes[i].Name);
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

        public void FromObjectList(HsfFile header)
        {
            void CreateBone(HSFObject node)
            {
                Nodes.Add(new Node()
                {
                    Name = node.Name,
                    Transform = new Transform()
                    {
                        Translate = node.Data.BaseTransform.Translate,
                        Rotate = node.Data.BaseTransform.Rotate,
                        Scale = node.Data.BaseTransform.Scale,
                    }
                }); 
            }

            for (int i = 0; i < Nodes.Count; i++)
                Console.WriteLine($"prev {Nodes[i].Name}");

            Console.WriteLine($"prev {this.Nodes.Count}");

            this.Nodes.Clear();
            foreach (var node in header.ObjectNodes.Where(x => x.Data.Type == ObjectType.Effect))
                CreateBone(node);
            foreach (var node in header.ObjectNodes.Where(x => x.Data.Type == ObjectType.Root))
                CreateBone(node);
            foreach (var node in header.ObjectNodes.Where(x => x.Data.Type == ObjectType.Joint))
                CreateBone(node);

            for (int i = 0; i < Nodes.Count; i++)
                Console.WriteLine($"new {Nodes[i].Name}");


            Console.WriteLine($"new {this.Nodes.Count}");
        }
    }

}
