using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace MPLibrary.SMP
{
    public class SpaceNode
    {
        public string Type { get; set; }

        public string Attribute1 { get; set; }
        public string Attribute2 { get; set; }

        public string ID { get; set; }

        public List<ushort> ChildrenIndices = new List<ushort>();

        public Vector3 Position { get; set; }
        public Vector3 EulerRotation { get; set; }
        public Vector3 Scale { get; set; }

        public SpaceNode()
        {
            Type = "EMPTY";
            ID = "";
        }

        public SpaceNode(string line)
        {
            string[] values = line.Split(',');
            ID = values[0];
            for (int i = 0; i < 4; i++)
            {
                var index = values[i];
                if (index != string.Empty)
                    ChildrenIndices.Add(ushort.Parse(index));
            }

            Type = values[5];
            Attribute1 = values[6];
            Attribute2 = values[7];
        }
    }
}
