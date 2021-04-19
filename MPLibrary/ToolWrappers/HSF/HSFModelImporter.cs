using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Toolbox.Core;
using Toolbox.Core.IO;
using Toolbox.Core.ModelView;
using Toolbox.Core.OpenGL;
using OpenTK;
using BrawlLib.Modeling.Triangle_Converter;
using Toolbox.Core.WiiU;

namespace MPLibrary.GCN
{
    public class HSFModelImporter
    {
        public class ImportSettings
        {
            public bool UseTriStrips = false;
        }

        public class ObjectSettings
        {
            public List<ObjectData> Objects = new List<ObjectData>();
        }

        public class FogSettings
        {
            public Vector4 ColorStart = new Vector4(77, 77, 77, 128);
            public Vector4 ColorEnd = new Vector4(0, 0, 0, 255);
            public float Start = 0;
            public float End = 10000;
        }

        public static HsfFile Import(STGenericScene scene, ImportSettings settings)
        {
            HsfFile hsf = new HsfFile();
            var root = ObjectDataSection.CreateNewObject(ObjectType.Root);
        
            hsf.ObjectData.Objects.Add(root);
            hsf.ObjectData.ObjectNames.Add("Root");

            foreach (var tex in scene.Models[0].Textures)
            {
                Console.WriteLine($"tex {tex.Name}");

                var newtex = CreateTexture(tex);
                newtex.Name = tex.Name;
                hsf.Textures.Add(newtex);
            }

            if (hsf.Textures.Count == 0)
            {

            }

            var newtex2 = CreateTexture(new GenericBitmapTexture("dummy.png"));
            newtex2.Name = "dummy";
            hsf.Textures.Add(newtex2);

            hsf.FogData.Count = 1;
            hsf.FogData.ColorStart = new Vector4(77, 77, 77, 128);
            hsf.FogData.ColorEnd = new Vector4(255, 0, 0, 255);
            hsf.FogData.Start = 0;
            hsf.FogData.End = 10000;

            Console.WriteLine("Importing Models");

            foreach (var model in scene.Models)
            {
                foreach (var material in model.GetMaterials())
                {
                    var mat = HSF_MaterialConverter.Import($"mat1.json");
                    mat.MaterialData.VertexMode = 2;
                    mat.Name = material.Name;
                    hsf.Materials.Add(mat);

                    if (material.TextureMaps.Count > 0)
                    {
                        var texMap = material.TextureMaps[0].Name;
                        int index = hsf.Textures.FindIndex(x => x.Name == texMap);
                        if (index != -1)
                            mat.Textures[0].Item2.TextureIndex = index;
                    }
                    mat.Textures[0].Item2.WrapS = 1;
                    mat.Textures[0].Item2.WrapT = 1;
                }

                if (hsf.Materials.Count == 0) {
                    var mat = HSF_MaterialConverter.Import($"mat1.json");
                    mat.MaterialData.VertexMode = 2;
                    mat.Name = "Basic";
                    hsf.Materials.Add(mat);
                }

                foreach (var mesh in model.Meshes)
                {
                    root.ChildrenCount += 1;

                    List<Vector3> positions = new List<Vector3>();
                    List<Vector3> normals = new List<Vector3>();
                    List<Vector2> texCoords = new List<Vector2>();
                    List<Vector4> colors = new List<Vector4>();

                    for (int v = 0; v < mesh.Vertices.Count; v++)
                    {
                        var vertex = mesh.Vertices[v];

                        if (!positions.Contains(vertex.Position))
                            positions.Add(vertex.Position);

                        if (!normals.Contains(vertex.Normal))
                            normals.Add(vertex.Normal);

                        if (vertex.Colors.Length > 0)
                        {
                            if (!colors.Contains(vertex.Colors[0]))
                                colors.Add(vertex.Colors[0]);
                        }
                        if (vertex.TexCoords.Length > 0)
                        {
                            if (!texCoords.Contains(vertex.TexCoords[0]))
                                texCoords.Add(vertex.TexCoords[0]);
                        }
                    }

                    var bounding = GenerateBoundingBox(mesh);

                    ObjectData objData = ObjectDataSection.CreateNewObject(ObjectType.Mesh);
                    objData.CullBoxMax = bounding.Max;
                    objData.CullBoxMin = bounding.Min;
                    objData.AttributeIndex = 0;
                    objData.ParentIndex = 0;
                    hsf.ObjectData.Objects.Add(objData);
                    hsf.ObjectData.ObjectNames.Add(mesh.Name);

                    Mesh msh = new Mesh(objData, mesh.Name);
                    msh.Positions.AddRange(positions);
                    msh.Normals.AddRange(normals);
                    msh.TexCoords.AddRange(texCoords);
                    msh.Colors.AddRange(colors);

                    foreach (var group in mesh.PolygonGroups)
                    {
                        int materialIndex = 0;
                        if (group.MaterialIndex != -1 && hsf.Materials.Count > group.MaterialIndex) {
                            materialIndex = group.MaterialIndex;
                        }

                        byte mode = 2;
                        if (colors.Count > 0)
                            mode = 5;

                        hsf.Materials[materialIndex].MaterialData.VertexMode = mode;

                        Weight[] weightList = new Weight[mesh.Vertices.Count];
                        for (int i = 0; i < mesh.Vertices.Count; i++)
                        {
                            Weight vertWeight = new Weight();
                            for (int j = 0; j < mesh.Vertices[i].BoneIndices.Count; j++)
                            {
                                int boneId = mesh.Vertices[i].BoneIndices[j];
                                float boneWeight = mesh.Vertices[i].BoneWeights[j];
                                vertWeight.AddWeight(boneWeight, boneId);
                            }
                            weightList[i] = vertWeight;
                        }

                        List<PrimitiveBrawl> primlist = new List<PrimitiveBrawl>();

                        //Turn triangle set into triangle strips
                        if (settings.UseTriStrips)
                        {
                            TriStripper stripper = new TriStripper(mesh.Faces.ToArray(), weightList);
                            primlist = stripper.Strip();
                        }
                        else
                        {
                            primlist = new List<PrimitiveBrawl>();
                            for (int i = 0; i < mesh.Faces.Count; i++)
                            {
                                PrimitiveBrawl prim = new PrimitiveBrawl(PrimType.TriangleList); // Trilist
                                prim.Indices.Add(mesh.Faces[i++]);
                                prim.Indices.Add(mesh.Faces[i++]);
                                prim.Indices.Add(mesh.Faces[i]);
                                primlist.Add(prim);
                            }
                        }

                        foreach (var primitive in primlist)
                        {
                            PrimitiveObject prim = new PrimitiveObject();
                            prim.Type = PrimitiveType.Triangle;
                            if (settings.UseTriStrips)
                                prim.Type = PrimitiveType.TriangleStrip;

                            prim.Flags = 0;
                            prim.NbtData = new OpenTK.Vector3(1, 0, 0);
                            if (hsf.Materials.Count > materialIndex)
                                prim.MaterialIndex = materialIndex;

                            if (settings.UseTriStrips)
                            {
                                prim.Vertices = new VertexGroup[primitive.Indices.Count + 1];
                            }
                            else
                            {
                                prim.Vertices = new VertexGroup[4];
                                prim.Vertices[3] = new VertexGroup(); //Empty last grou
                            }
                            msh.Primitives.Add(prim);

                            if (prim.Type == PrimitiveType.TriangleStrip)
                            {
                                for (int i = 0; i < 3; i++)
                                {
                                    var vertexIndex = (int)primitive.Indices[i];
                                    var vertex = mesh.Vertices[vertexIndex];

                                    short colorIndex = -1;
                                    short texCoordIndex = -1;
                                    short positionIndex = (short)positions.IndexOf(vertex.Position);
                                    short normaIndex = (short)normals.IndexOf(vertex.Normal);

                                    if (vertex.Colors.Length > 0)
                                        colorIndex = (short)colors.IndexOf(vertex.Colors[0]);
                                    if (vertex.TexCoords.Length > 0)
                                        texCoordIndex = (short)texCoords.IndexOf(vertex.TexCoords[0]);

                                    prim.Vertices[i] = new VertexGroup()
                                    {
                                        PositionIndex = positionIndex,
                                        UVIndex = texCoordIndex,
                                        ColorIndex = colorIndex,
                                        NormalIndex = normaIndex,
                                    };
                                }

                                prim.Vertices[3] = new VertexGroup();
                                for (int i = 4; i < prim.Vertices.Length; i++)
                                {
                                    var vertexIndex = (int)primitive.Indices[i-1];
                                    var vertex = mesh.Vertices[vertexIndex];

                                    short colorIndex = -1;
                                    short texCoordIndex = -1;
                                    short positionIndex = (short)positions.IndexOf(vertex.Position);
                                    short normaIndex = (short)normals.IndexOf(vertex.Normal);

                                    if (vertex.Colors.Length > 0)
                                        colorIndex = (short)colors.IndexOf(vertex.Colors[0]);
                                    if (vertex.TexCoords.Length > 0)
                                        texCoordIndex = (short)texCoords.IndexOf(vertex.TexCoords[0]);

                                    prim.Vertices[i] = new VertexGroup()
                                    {
                                        PositionIndex = positionIndex,
                                        UVIndex = texCoordIndex,
                                        ColorIndex = colorIndex,
                                        NormalIndex = normaIndex,
                                    };
                                }
                            }
                            else
                            {
                                for (int i = 0; i < primitive.Indices.Count; i++)
                                {
                                    var vertexIndex = (int)primitive.Indices[i];
                                    var vertex = mesh.Vertices[vertexIndex];

                                    short colorIndex = -1;
                                    short texCoordIndex = -1;
                                    short positionIndex = (short)positions.IndexOf(vertex.Position);
                                    short normaIndex = (short)normals.IndexOf(vertex.Normal);

                                    if (vertex.Colors.Length > 0)
                                        colorIndex = (short)colors.IndexOf(vertex.Colors[0]);
                                    if (vertex.TexCoords.Length > 0)
                                        texCoordIndex = (short)texCoords.IndexOf(vertex.TexCoords[0]);

                                    prim.Vertices[i] = new VertexGroup()
                                    {
                                        PositionIndex = positionIndex,
                                        UVIndex = texCoordIndex,
                                        ColorIndex = colorIndex,
                                        NormalIndex = normaIndex,
                                    };
                                }
                            }
                        }
                    }

                    hsf.Meshes.Add(msh);
                }
            }

            Console.WriteLine("Finished generating HSF binary!");

            return hsf;
        }

        static HSFTexture CreateTexture(STGenericTexture texture)
        {
            var format = Decode_Gamecube.TextureFormats.CMPR;
            var info = new TextureInfo();
            info.Bpp = 4;
            info.Width = (ushort)texture.Width;
            info.Height = (ushort)texture.Height;
            info.PaletteEntries = 0;
            info.TextureTint = 0;
            info.PaletteIndex = -1;
            info.Format = (byte)MPLibrary.GCN.HSFTexture.GetFormatId(format);
            info.MaxLOD = 0;

             var data = Decode_Gamecube.EncodeFromBitmap(texture.GetBitmap(),
                Decode_Gamecube.TextureFormats.CMPR);

            return new HSFTexture(texture.Name, info, data.Item1);
        }

        class BoundingBox
        {
            public Vector3XYZ Min;
            public Vector3XYZ Max;
        }

        static BoundingBox GenerateBoundingBox(STGenericMesh mesh)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            for (int v = 0; v < mesh.Vertices.Count; v++)
            {
                minX = Math.Min(minX, mesh.Vertices[v].Position.X);
                minY = Math.Min(minY, mesh.Vertices[v].Position.Y);
                minZ = Math.Min(minZ, mesh.Vertices[v].Position.Z);
                maxX = Math.Max(maxX, mesh.Vertices[v].Position.X);
                maxY = Math.Max(maxY, mesh.Vertices[v].Position.Y);
                maxZ = Math.Max(maxZ, mesh.Vertices[v].Position.Z);
            }
            return new BoundingBox()
            {
                Min = new Vector3XYZ(minX, minY, minZ),
                Max = new Vector3XYZ(maxX, maxY, maxZ),
            };
        }
    }
}
