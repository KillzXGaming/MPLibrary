using GLFrameworkEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static rsmeshopt;

namespace MPLibrary.GCN
{
    public class HSFEnvelopeHandler
    {
        public static void UpdateCPUSkinning(Mesh hsfMesh, OpenTK.Matrix4[] inverseMatrices)
        {
            if (hsfMesh.Envelopes.Count == 0)
                return;

            var envelope = hsfMesh.Envelopes.FirstOrDefault();

            if (envelope.SingleBinds.Count == 0 &&
                envelope.DoubleBinds.Count == 0 &&
                envelope.MultiBinds.Count == 0)
            {
            /*    var trans = hsfMesh.Object.CalculateWorldMatrix();

                foreach (var mesh in hsfMesh.GXMeshes)
                {
                    mesh.Value.WeightedPositions.Clear();
                    mesh.Value.WeightedNormals.Clear();

                    foreach (var pos in mesh.Value.Positions)
                    {
                        var vec = new OpenTK.Vector3(pos.X, pos.Y, pos.Z);
                        //vec = OpenTK.Vector3.TransformPosition(vec, trans);
                        mesh.Value.WeightedPositions.Add(new Vector3(vec.X, vec.Y, vec.Z));
                    }
                    foreach (var nrm in mesh.Value.Normals)
                    {
                        var vec = new OpenTK.Vector3(nrm.X, nrm.Y, nrm.Z);
                     //   vec = OpenTK.Vector3.TransformNormal(vec, trans);
                        mesh.Value.WeightedNormals.Add(new Vector3(vec.X, vec.Y, vec.Z));
                    }
                }

                foreach (var mesh in hsfMesh.GXMeshes)
                {
                    mesh.Value.SceneNode.UpdatePositions();
                }*/
                return;
            }

            var transform = hsfMesh.Object.CalculateWorldMatrix();

            //Weigh all vertices
            Vector3[] positions = new Vector3[hsfMesh.Positions.Count];
            Vector3[] normals = new Vector3[hsfMesh.Normals.Count];

            for (int i = 0; i < hsfMesh.Positions.Count; i++)
                positions[i] = hsfMesh.Positions[i];

            for (int i = 0; i < hsfMesh.Normals.Count; i++)
                normals[i] = hsfMesh.Normals[i];


            void TransformPositions(int index, int count, OpenTK.Matrix4 matrix)
            {
                for (int j = 0; j < count; j++)
                {
                    //Position to transform
                    var target = positions[index + j];
                    //Transform and apply
                    var vec = new OpenTK.Vector3(target.X, target.Y, target.Z);
                    vec = OpenTK.Vector3.TransformPosition(vec, transform * matrix);
                    //Set the position dest
                    positions[index + j] = new Vector3(vec.X, vec.Y, vec.Z);
                }
            }

            void TransformNormals(int index, int count, OpenTK.Matrix4 matrix)
            {
                for (int j = 0; j < count; j++)
                {
                    //Position to transform
                    var target = normals[index + j];
                    //Transform and apply
                    var vec = new OpenTK.Vector3(target.X, target.Y, target.Z);
                    vec = OpenTK.Vector3.TransformNormal(vec, transform * matrix);
                    //Set the position dest
                    normals[index + j] = new Vector3(vec.X, vec.Y, vec.Z);
                }
            }

            foreach (var singleBind in envelope.SingleBinds)
            {
                TransformPositions(singleBind.PositionIndex, singleBind.PositionCount, inverseMatrices[singleBind.BoneIndex]);
                TransformNormals(singleBind.NormalIndex, singleBind.NormalCount, inverseMatrices[singleBind.BoneIndex]);
            }

            int mbOffset = 0;
            foreach (var doubleBind in envelope.DoubleBinds)
            {
                for (int i = mbOffset; i < mbOffset + doubleBind.Count; i++)
                {
                    var w = envelope.DoubleWeights[i];

                    //Blend matrices by weights
                    var matrix = OpenTK.Matrix4.Zero;
                    matrix += inverseMatrices[doubleBind.Bone1] * w.Weight;
                    matrix += inverseMatrices[doubleBind.Bone2] * (1.0f - w.Weight);

                    TransformPositions(w.PositionIndex, w.PositionCount, matrix);
                    TransformNormals(w.NormalIndex, w.NormalCount, matrix);
                }
                mbOffset += doubleBind.Count;
            }

            mbOffset = 0;
            foreach (var multiBind in envelope.MultiBinds)
            {
                //Blend matrices by weights
                var matrix = OpenTK.Matrix4.Zero;
                for (int i = mbOffset; i < mbOffset + multiBind.Count; i++)
                {
                    var w = envelope.MultiWeights[i];
                    matrix += inverseMatrices[w.BoneIndex] * w.Weight;
                }
                TransformPositions(multiBind.PositionIndex, multiBind.PositionCount, matrix);
                TransformNormals(multiBind.NormalIndex, multiBind.NormalCount, matrix);

                mbOffset += multiBind.Count;
            }

            //Transform back to local space
          /*  var inv = OpenTK.Matrix4.Invert(transform);
            for (int i = 0; i < positions.Length; i++)
                positions[i] = Vector3.Transform(positions[i], Matrix4Extension.ToNumerics(inv));
            for (int i = 0; i < normals.Length; i++)
                normals[i] = Vector3.TransformNormal(normals[i], Matrix4Extension.ToNumerics(inv));
            */
            foreach (var mesh in hsfMesh.GXMeshes)
            {
                mesh.Value.WeightedPositions.Clear();
                mesh.Value.WeightedNormals.Clear();
            }

            foreach (var primative in hsfMesh.Primitives)
            {
                var msh = hsfMesh.GXMeshes[primative.MaterialIndex];
                var count = primative.Vertices.Length;
                if (primative.Type == PrimitiveType.Triangle)
                    count = primative.Vertices.Length - 1;

                //Assign positions again but weighted
                for (int v = 0; v < count; v++)
                {
                    var vertex = primative.Vertices[v];
                    msh.WeightedPositions.Add(positions[vertex.PositionIndex]);
                    msh.WeightedNormals.Add(normals[vertex.NormalIndex]);
                }
            }

            foreach (var mesh in hsfMesh.GXMeshes)
            {
                mesh.Value.SceneNode?.UpdatePositions();
            }
        }
    }
}
