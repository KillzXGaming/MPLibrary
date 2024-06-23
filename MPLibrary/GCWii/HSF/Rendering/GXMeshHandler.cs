using GCNRenderLibrary.Rendering;
using GLFrameworkEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static GCNRenderLibrary.Rendering.VtxLoader;

namespace MPLibrary.GCN
{
    public class GXMeshHandler
    {
        public static Dictionary<int, GXMesh> LoadPrimitives(Mesh hsfMesh)
        {
            Dictionary<int, PrimList> VertexMatMapper = new Dictionary<int, PrimList>();

            Dictionary<int, GXMesh> meshList = new Dictionary<int, GXMesh>();

            foreach (var primative in hsfMesh.Primitives)
            {
                if (!VertexMatMapper.ContainsKey(primative.MaterialIndex)) {
                    VertexMatMapper.Add(primative.MaterialIndex, new PrimList());
                }

                var primList = VertexMatMapper[primative.MaterialIndex];
                var currentDraw = primList.CurrentDraw;

                var primMode = GX.Command.DRAW_TRIANGLES;

                if (primative.Type == PrimitiveType.TriangleStrip)
                    primMode = GX.Command.DRAW_TRIANGLE_STRIP;

                if (primative.Type == PrimitiveType.Quad)
                    primMode = GX.Command.DRAW_QUADS;

                int vertexCount = 0;
                int indexCount = 0;
                switch ((GX.Command)primMode)
                {
                    case GX.Command.DRAW_TRIANGLES:
                        //HSF still has an extra group on triangles so subtract by 1
                        indexCount = primative.Vertices.Length - 1;
                        vertexCount = primative.Vertices.Length - 1;
                        break;
                    case GX.Command.DRAW_TRIANGLE_FAN:
                    case GX.Command.DRAW_TRIANGLE_STRIP:
                        indexCount = (primative.Vertices.Length - 2) * 3;
                        vertexCount = primative.Vertices.Length;
                        break;
                    case GX.Command.DRAW_QUADS:
                    case GX.Command.DRAW_QUADS_2:
                        indexCount = ((primative.Vertices.Length * 6) / 4) * 3;
                        vertexCount = primative.Vertices.Length;
                        break;
                }

                for (int v = 0; v < vertexCount; v++)
                {
                    var vertex = primative.Vertices[v];
                    if ( vertex.NormalIndex != -1 && hsfMesh.Normals.Count > vertex.NormalIndex)
                    {
                        //Use first normal ID for each primitive
                        var n = hsfMesh.Normals[primative.Vertices[0].NormalIndex];
                        var t = new Vector3(primative.NbtData.X, primative.NbtData.Y, primative.NbtData.Z);

                        primList.Mesh.Tangents.Add(t);
                        primList.Mesh.Binormals.Add(Vector3.Normalize(Vector3.Cross(t, n)));
                    }

                    if (vertex.PositionIndex != -1 && hsfMesh.Positions.Count > vertex.PositionIndex)
                        primList.Mesh.Positions.Add(hsfMesh.Positions[vertex.PositionIndex]);
                    else
                        primList.Mesh.Positions.Add(new Vector3());

                    if (vertex.NormalIndex != -1 && hsfMesh.Normals.Count > vertex.NormalIndex)
                        primList.Mesh.Normals.Add(hsfMesh.Normals[vertex.NormalIndex]);

                    if (vertex.UVIndex != -1 && hsfMesh.TexCoord0.Count > vertex.UVIndex)
                        primList.Mesh.TexCoord0.Add(hsfMesh.TexCoord0[vertex.UVIndex]);

                    if (vertex.ColorIndex != -1 && hsfMesh.Color0.Count > vertex.ColorIndex)
                        primList.Mesh.Color0.Add(hsfMesh.Color0[vertex.ColorIndex]);

                    if (hsfMesh.HasEnvelopes)
                    {
                        foreach (var cenv in hsfMesh.Envelopes)
                            PrepareSkinning(primList.Mesh, vertex, cenv);
                    }
                }

                primList.drawCalls.Add(new DrawCall() { primType = primMode, vertexCount = vertexCount });
                currentDraw.IndexCount += indexCount;
                primList.totalIndexCount += indexCount;
            }

            foreach (var primList in VertexMatMapper)
            {
                var indices = GenerateIndices(primList.Value.drawCalls, primList.Value.totalIndexCount, 0);
                primList.Value.Mesh.SetIndices(indices);
                meshList.Add(primList.Key, primList.Value.Mesh);
            }
             return meshList;
        }

        private static void PrepareSkinning(GXMesh mesh, VertexGroup group, HSFEnvelope envelope)
        {
            if (envelope.SingleBinds.Count == 0 &&
                envelope.DoubleBinds.Count == 0 &&
                envelope.MultiBinds.Count == 0)
            {
                return;
            }

            List<int> boneIndices = new List<int>();
            List<float> boneWeights = new List<float>();

            foreach (var singleBind in envelope.SingleBinds)
            {
                if (group.PositionIndex >= singleBind.PositionIndex && group.PositionIndex < singleBind.PositionIndex + singleBind.PositionCount)
                {
                    boneIndices.Clear();
                    boneWeights.Clear();

                    boneIndices.Add(singleBind.BoneIndex);
                    boneWeights.Add(1);
                    break;
                }
            }

            int mbOffset = 0;
            foreach (var multiBind in envelope.DoubleBinds)
            {
                for (int i = mbOffset; i < mbOffset + multiBind.Count; i++)
                {
                    var w = envelope.DoubleWeights[i];
                    if (group.PositionIndex >= w.PositionIndex && group.PositionIndex < w.PositionIndex + w.PositionCount)
                    {
                        boneIndices.Clear();
                        boneWeights.Clear();

                        boneIndices.Add(multiBind.Bone1);
                        boneIndices.Add(multiBind.Bone2);
                        boneWeights.Add(w.Weight);
                        boneWeights.Add(1 - w.Weight);
                        break;
                    }
                }
                mbOffset += multiBind.Count;
            }

            mbOffset = 0;
            foreach (var multiBind in envelope.MultiBinds)
            {
                if (group.PositionIndex >= multiBind.PositionIndex && group.PositionIndex < multiBind.PositionIndex + multiBind.PositionCount)
                {
                    boneIndices.Clear();
                    boneWeights.Clear();

                    OpenTK.Vector4 indices = new OpenTK.Vector4(0);
                    OpenTK.Vector4 weight = new OpenTK.Vector4(0);
                    for (int i = mbOffset; i < mbOffset + MathF.Min(multiBind.Count, 4); i++)
                    {
                        indices[i - mbOffset] = envelope.MultiWeights[i].BoneIndex;
                        weight[i - mbOffset] = envelope.MultiWeights[i].Weight;
                    }

                    if (weight.X != 0)
                    {
                        boneIndices.Add((int)indices.X);
                        boneWeights.Add(weight.X);
                    }
                    if (weight.Y != 0)
                    {
                        boneIndices.Add((int)indices.Y);
                        boneWeights.Add(weight.Y);
                    }
                    if (weight.Z != 0)
                    {
                        boneIndices.Add((int)indices.Z);
                        boneWeights.Add(weight.Z);
                    }
                    if (weight.W != 0)
                    {
                        boneIndices.Add((int)indices.W);
                        boneWeights.Add(weight.W);
                    }
                    break;
                }
                mbOffset += multiBind.Count;
            }

            int GetIndex(int id)
            {
                return id;

                if (!mesh.BoneIndexTable.Contains(id))
                    mesh.BoneIndexTable.Add(id);

                return mesh.BoneIndexTable.IndexOf(id);
            }

            //Add indices
            mesh.BoneIndices.Add(new Vector4(
                boneIndices.Count > 0 ? GetIndex(boneIndices[0]) : 0,
                boneIndices.Count > 1 ? GetIndex(boneIndices[1]) : 0,
                boneIndices.Count > 2 ? GetIndex(boneIndices[2]) : 0,
                boneIndices.Count > 3 ? GetIndex(boneIndices[3]) : 0));

            mesh.BoneWeights.Add(new Vector4(
                boneWeights.Count > 0 ? boneWeights[0] : 0,
                boneWeights.Count > 1 ? boneWeights[1] : 0,
                boneWeights.Count > 2 ? boneWeights[2] : 0,
                boneWeights.Count > 3 ? boneWeights[3] : 0));
        }

        class PrimList
        {
            public List<DrawCall> drawCalls = new List<DrawCall>();
            public int totalIndexCount = 0;

            public GXMesh Mesh = new GXMesh();

            public GXDraw CurrentDraw;
            public GXDraw CurrentXfmem = null;

            public PrimList()
            {
                CurrentDraw = new GXDraw(0);
                CurrentXfmem = new GXDraw(0);
                Mesh.DrawCalls.Add(CurrentDraw);
            }
        }

        public static ushort[] GenerateIndices(List<DrawCall> drawCalls, int totalIndexCount, ushort firstVertexId)
        {
            //Generate the indices
            int indexDataIdx = 0;
            ushort[] dstIndexData = new ushort[totalIndexCount];
            ushort vertexId = firstVertexId;

            for (int z = 0; z < drawCalls.Count; z++)
            {
                var drawCall = drawCalls[z];

                // Convert topology to triangles.
                switch (drawCall.primType)
                {
                    case GX.Command.DRAW_TRIANGLES:
                        // Copy vertices.
                        for (int i = 0; i < drawCall.vertexCount; i++)
                        {
                            dstIndexData[indexDataIdx++] = vertexId++;
                        }
                        break;
                    case GX.Command.DRAW_TRIANGLE_STRIP:
                        for (int i = 0; i < 3; i++)
                        {
                            dstIndexData[indexDataIdx++] = vertexId++;
                        }

                        for (int i = 3; i < drawCall.vertexCount; i++)
                        {
                            dstIndexData[indexDataIdx++] = (ushort)(vertexId - ((i & 1) == 1 ? 1 : 2));
                            dstIndexData[indexDataIdx++] = (ushort)(vertexId - ((i & 1) == 1 ? 2 : 1));
                            dstIndexData[indexDataIdx++] = vertexId++;
                        }
                        break;
                    case GX.Command.DRAW_TRIANGLE_FAN:
                        // First vertex defines original triangle.
                        ushort firstVertex = vertexId;

                        for (int i = 0; i < 3; i++)
                        {
                            dstIndexData[indexDataIdx++] = vertexId++;
                        }

                        for (int i = 3; i < drawCall.vertexCount; i++)
                        {
                            dstIndexData[indexDataIdx++] = firstVertex;
                            dstIndexData[indexDataIdx++] = (ushort)(vertexId - 1);
                            dstIndexData[indexDataIdx++] = vertexId++;
                        }
                        break;
                    case GX.Command.DRAW_QUADS:
                    case GX.Command.DRAW_QUADS_2:
                        // Each quad (4 vertices) is split into 2 triangles (6 vertices)
                        for (int i = 0; i < drawCall.vertexCount; i += 4)
                        {
                            dstIndexData[indexDataIdx++] = (ushort)(vertexId + 0);
                            dstIndexData[indexDataIdx++] = (ushort)(vertexId + 1);
                            dstIndexData[indexDataIdx++] = (ushort)(vertexId + 2);

                            dstIndexData[indexDataIdx++] = (ushort)(vertexId + 1);
                            dstIndexData[indexDataIdx++] = (ushort)(vertexId + 3);
                            dstIndexData[indexDataIdx++] = (ushort)(vertexId + 2);
                            vertexId += 4;
                        }
                        break;
                }
            }
            return dstIndexData;
        }
    }
}
