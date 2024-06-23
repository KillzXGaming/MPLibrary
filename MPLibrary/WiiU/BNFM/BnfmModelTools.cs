using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using IONET;
using IONET.Core;
using IONET.Core.Model;
using IONET.Core.Skeleton;

namespace MPLibrary.MP10
{
    public class BnfmModelTools
    {
        public static void Export(BnfmModel model, string filePath)
        {
            IOScene scene = new IOScene();

            IOModel iomodel = new IOModel();
            scene.Models.Add(iomodel);

            IOBone LoadBone(BnfmBone bn)
            {
                IOBone iobone = new IOBone();
                iobone.Name = bn.Name.Value;
                iobone.RotationEuler = bn.RotationEuler;
                iobone.Translation = bn.Position;
                iobone.Scale = bn.Scale;

                foreach (var child in model.Bones)
                {
                    if (child.Parent == bn)
                        iobone.AddChild(LoadBone(child));
                }

                return iobone;
            }

            foreach (var bn in model.Bones)
            {
                IOBone iobone = LoadBone(bn);
                if (bn.Parent == null)
                    iomodel.Skeleton.RootBones.Add(iobone);
            }

            var boneMatrices = iomodel.Skeleton.BreathFirstOrder().Select(x => x.WorldTransform).ToList();

            foreach (var mat in model.Meshes.Select(x => x.Material))
            {
                IOMaterial iomaterial = new IOMaterial();
                iomaterial.Name = mat.Name.Value.ToString();
                scene.Materials.Add(iomaterial);

                iomaterial.DiffuseMap = new IOTexture()
                {
                    FilePath = $"{mat.Textures[0].Name.Value}",
                    Name = mat.Textures[0].Name.Value,
                };
            }

            foreach (var mesh in model.Meshes)
            {
                IOMesh iomesh = new IOMesh();
                iomodel.Meshes.Add(iomesh);
                iomesh.Name = mesh.Name.Value;

                List<int[]> boneIndices = new List<int[]>();
                List<float[]> boneWeights = new List<float[]>();

                List<bool> assigned = new List<bool>();

                foreach (var poly in mesh.Polygons)
                {
                    foreach (var vertex in poly.Vertices)
                    {
                        boneIndices.Add(new int[4]);
                        boneWeights.Add(new float[4]);

                        assigned.Add(false);
                    }
                }

                foreach (var poly in mesh.Polygons)
                {
                    foreach (var bid in poly.BoneIndices)
                    {
                        var bone = model.Bones[(int)bid];
                        Console.WriteLine($"skin {poly.SkinningCount} {bone.Name} {bone.Unknown2}");
                    }

                    foreach (var ind in poly.Faces)
                    {
                        var vertex = mesh.Polygons[0].Vertices[ind];
                        if (assigned[ind])
                            continue;

                        assigned[ind] = true;

                        for (int i = 0; i < poly.SkinningCount; i++)
                        {
                            float weight = vertex.GetWeight(i);

                            int index = (int)poly.BoneIndices[vertex.GetIndex(i)];
                            boneIndices[ind][i] = index;
                            boneWeights[ind][i] = weight;
                        }
                    }
                }

                int vertexID = 0;
                foreach (var poly in mesh.Polygons)
                {
                    foreach (var vertex in poly.Vertices)
                    {
                        IOVertex iovertex = new IOVertex();
                        iomesh.Vertices.Add(iovertex);

                        iovertex.Position = vertex.Position;
                        iovertex.Normal = new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);
                        iovertex.Tangent = new Vector3(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z);

                        if (mesh.AttributeList.ColorData.HasValue)
                            iovertex.SetColor(vertex.Color.X, vertex.Color.Y, vertex.Color.Z, vertex.Color.W, 0);

                        if (mesh.AttributeList.TexCoordData0.HasValue)
                            iovertex.SetUV(vertex.TexCoord0.X, vertex.TexCoord0.Y, 0);
                        if (mesh.AttributeList.TexCoordData1.HasValue)
                            iovertex.SetUV(vertex.TexCoord1.X, vertex.TexCoord1.Y, 1);
                        if (mesh.AttributeList.TexCoordData2.HasValue)
                            iovertex.SetUV(vertex.TexCoord2.X, vertex.TexCoord2.Y, 2);

                        for (int i = 0; i < 4; i++)
                        {
                            if (boneWeights[vertexID][i] == 0)
                                continue;

                            int index = (int)boneIndices[vertexID][i];
                            iovertex.Envelope.Weights.Add(new IOBoneWeight()
                            {
                                BoneName = model.Bones[index].Name.Value,
                                Weight = boneWeights[vertexID][i],
                            });
                        }
                        vertexID++;
                    }
                }

                foreach (var poly in mesh.Polygons)
                {
                    IOPolygon iopoly = new IOPolygon();
                    iomesh.Polygons.Add(iopoly);
                    iopoly.PrimitiveType = IOPrimitive.TRIANGLE;
                    iopoly.MaterialName = mesh.Material.Name.Value;
                    foreach (var face in poly.Faces)
                        iopoly.Indicies.Add(face);
                }
            }

            IOManager.ExportScene(scene, filePath, new ExportSettings());
        }
    }
}
