using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using OpenTK;
using MPLibrary.GCN;

namespace MPLibrary.GCN
{
    public class HSFMesh : STGenericMesh
    {
        public bool IsTransparent = false;

        public STBone ObjectNode;

        public struct DisplayVertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 TexCoord0;
            public Vector2 TexCoord1;
            public Vector2 TexCoord2;
            public Vector4 Color;
            public Vector4 BoneIdices;
            public Vector4 BoneWeights;

            public static int Size = 4 * (3 + 3 + 2 + 2 + 2 + 4 + 4 + 4);
        }

        public uint[] display;
        public int DisplayId;

        public List<DisplayVertex> CreateDisplayVertices()
        {
            List<uint> Faces = new List<uint>();
            foreach (var group in PolygonGroups)
                Faces.AddRange(group.Faces);

            display = Faces.ToArray();

            List<DisplayVertex> displayVertList = new List<DisplayVertex>();

            if (display.Length <= 3)
                return displayVertList;

            foreach (STVertex v in Vertices)
            {
                DisplayVertex displayVert = new DisplayVertex()
                {
                    Position = v.Position,
                    Normal = v.Normal,
                    Color = v.Colors.Length > 0 ? v.Colors[0] : Vector4.One,
                    TexCoord0 = v.TexCoords.Length > 0 ? v.TexCoords[0] : Vector2.Zero,
                    TexCoord1 = v.TexCoords.Length > 1 ? v.TexCoords[1] : Vector2.Zero,
                    TexCoord2 = v.TexCoords.Length > 2 ? v.TexCoords[2] : Vector2.Zero,
                    BoneIdices = new Vector4(
                             v.BoneIndices.Count > 0 ? v.BoneIndices[0] : -1,
                             v.BoneIndices.Count > 1 ? v.BoneIndices[1] : -1,
                             v.BoneIndices.Count > 2 ? v.BoneIndices[2] : -1,
                             v.BoneIndices.Count > 3 ? v.BoneIndices[3] : -1),
                    BoneWeights = new Vector4(
                             v.BoneWeights.Count > 0 ? v.BoneWeights[0] : 0,
                             v.BoneWeights.Count > 1 ? v.BoneWeights[1] : 0,
                             v.BoneWeights.Count > 2 ? v.BoneWeights[2] : 0,
                             v.BoneWeights.Count > 3 ? v.BoneWeights[3] : 0),
                };

                displayVertList.Add(displayVert);
            }

            return displayVertList;
        }
    }
}
