using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Toolbox.Core;
using Toolbox.Core.IO;
using Toolbox.Core.ModelView;
using System.Numerics;
using BrawlLib.Modeling.Triangle_Converter;
using Toolbox.Core.WiiU;
using IONET.Core;
using IONET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using IONET.Core.Model;
using IONET.Collada.Core.Controller;
using static Toolbox.Core.DDS;
using IONET.Core.Skeleton;
using System.Reflection;
using Newtonsoft.Json.Bson;
using MPLibrary.GCWii.HSF;
using GLFrameworkEngine;
using GCNRenderLibrary.Rendering;
using MPLibrary.MP10;
using IONET.Collada.Core.Scene;
using static Toolbox.Core.GX.DisplayListHelper;
using Toolbox.Core.Imaging;
using static MPLibrary.GCWii.HSF.HSFJsonExporter;

namespace MPLibrary.GCN
{
    public class HSFModelImporter
    {
        public class ImportSettings
        {
            public bool UseTriStrips = true;
            public bool DegenTriangles = true;

            public bool UseOriginalMaterials = false;
            public bool UseCustomBones = false;
        }

        public class ObjectSettings
        {
            public List<HSFObjectData> Objects = new List<HSFObjectData>();
        }

        public class FogSettings
        {
            public Vector4 ColorStart = new Vector4(77, 77, 77, 128);
            public Vector4 ColorEnd = new Vector4(0, 0, 0, 255);
            public float Start = 0;
            public float End = 10000;
        }

        public static void Export(HsfFile hsfFile, string filePath)
        {
            var scene = ToScene(hsfFile);

            string folder = Path.GetDirectoryName(filePath);

            int index = 0;
            foreach (var tex in hsfFile.Textures)
            {
                var rgba = gctex.Decode(tex.ImageData,
                     tex.TextureInfo.Width, tex.TextureInfo.Height,
                    (uint)tex.GcnFormat, tex.GetPaletteBytes(), (uint)tex.GcnPaletteFormat);

                var image = Image.LoadPixelData<Rgba32>(rgba, (int)tex.TextureInfo.Width, (int)tex.TextureInfo.Height);

                //Important we export with an index at the end as names can dupe
                image.SaveAsPng(Path.Combine(folder, $"{tex.Name}_{index++}.png"));
            }

            IOManager.ExportScene(scene, filePath, new ExportSettings()
            {
            });
        }

        public static IOScene ToScene(HsfFile hsfFile)
        {
            var scene = new IOScene();
            var model = new IOModel();
            scene.Models.Add(model);

            foreach (var mat in hsfFile.Materials)
            {
                IOMaterial iomaterial = new IOMaterial();
                iomaterial.Name = $"{mat.Name}_{hsfFile.Materials.IndexOf(mat)}";
                iomaterial.Label = iomaterial.Name;
                //Ambient/material colors
                iomaterial.AmbientColor = mat.AmbientColor[0].ToVector4();
                iomaterial.DiffuseColor = mat.MaterialColor[0].ToVector4();
                scene.Materials.Add(iomaterial);
                //Texture maps
                if (mat.TextureAttributes.Count > 0)
                {
                    //Map by index
                    var ind = mat.TextureAttributes[0].AttributeData.TextureIndex;
                    var tex = hsfFile.Textures[ind];
                    var name = $"{tex.Name}_{ind}.png";

                    iomaterial.DiffuseMap = new IOTexture()
                    {
                        Name = tex.Name,
                        FilePath = name,
                    };
                }
            }

            List<IONode> bones = new List<IONode>();
            foreach (var ob in hsfFile.ObjectNodes)
            {
                var info = ob.Data;

                //Add a dummy bone. Some bone data is set at runtime and uses large random values
                var bone = new IONode()
                {
                    Name = ob.Name,
                    RotationEuler = System.Numerics.Vector3.Zero,
                    Translation = System.Numerics.Vector3.Zero,
                    Scale = System.Numerics.Vector3.One,
                };
                if (ob.Data.ChildrenCount <= hsfFile.ObjectNodes.Count)
                {
                    bone = new IONode()
                    {
                        Name = ob.Name,
                        Translation = new System.Numerics.Vector3(
                          info.BaseTransform.Translate.X,
                          info.BaseTransform.Translate.Y,
                          info.BaseTransform.Translate.Z),
                        RotationEuler = new System.Numerics.Vector3(
                      OpenTK.MathHelper.DegreesToRadians(info.BaseTransform.Rotate.X),
                      OpenTK.MathHelper.DegreesToRadians(info.BaseTransform.Rotate.Y),
                      OpenTK.MathHelper.DegreesToRadians(info.BaseTransform.Rotate.Z)),
                        Scale = new System.Numerics.Vector3(
                          info.BaseTransform.Scale.X == 0 ? 1 : info.BaseTransform.Scale.X,
                          info.BaseTransform.Scale.Y == 0 ? 1 : info.BaseTransform.Scale.Y,
                          info.BaseTransform.Scale.Z == 0 ? 1 : info.BaseTransform.Scale.Z),
                    };
                }
                bone.IsJoint = true;

                bones.Add(bone);
            }

            for (int i = 0; i < hsfFile.ObjectNodes.Count; i++)
            {
                var parentIndex = hsfFile.ObjectNodes.IndexOf(hsfFile.ObjectNodes[i].Parent);

                if (parentIndex == -1)
                    model.Skeleton.RootBones.Add(bones[i]);
                else
                    bones[parentIndex].AddChild(bones[i]);
            }
            //scene.Nodes.AddRange(bones);

            foreach (var mesh in hsfFile.Meshes)
            {
                mesh.Init();

                IOMesh iomesh = new IOMesh();
                iomesh.Name = mesh.Name;
                model.Meshes.Add(iomesh);
                //Check to load uvs or not for the current mesh
                bool hasUVs = mesh.GXMeshes.Any(x => x.Value.TexCoord0.Count > 0);
                bool hasColor = mesh.GXMeshes.Any(x => x.Value.Color0.Count > 0);

                //Node matrix
                var node = hsfFile.ObjectNodes.FirstOrDefault(x => x.Data == mesh.ObjectData);
                var node_idx = hsfFile.ObjectNodes.IndexOf(node);
                var transform = ConvertMatrix(node.CalculateWorldMatrix());

                //Bone assign to node tree
                bones[node_idx].Mesh = iomesh;

                int vertexID = 0;
                foreach (var msh in mesh.GXMeshes)
                {
                    //Polygon
                    var p = msh.Value;
                    //Material ID
                    var matIndex = msh.Key;

                    IOPolygon poly = new IOPolygon();
                    iomesh.Polygons.Add(poly);
                    //Map materials by material ID
                    var mat = scene.Materials[matIndex];
                    poly.MaterialName = mat.Name;

                    for (int i = 0; i < p.Positions.Count; i++)
                    {
                        IOVertex vertex = new IOVertex();
                        vertex.Position = Vector3.Transform(p.Positions[i], transform);

                        if (p.Normals.Count > i)
                            vertex.Normal = Vector3.TransformNormal(p.Normals[i], transform);

                        if (p.TexCoord0.Count > 0) //Flip UVs
                            vertex.SetUV(p.TexCoord0[i].X, p.TexCoord0[i].Y, 0);
                        else if (hasUVs) //Force UVs to be added as mesh uses them in other polygons
                            vertex.SetUV(0, 0, 0);

                        if (p.Color0.Count > 0) //Vertex colors + alpha
                            vertex.SetColor(p.Color0[i].X, p.Color0[i].Y, p.Color0[i].Z, p.Color0[i].W);
                        else if (hasUVs) //Force color to be added as mesh uses them in other polygons
                            vertex.SetColor(1, 1, 1, 1);

                        if (p.BoneIndices.Count > 0) //Vertex colors + alpha
                        {
                            for (int j = 0; j < 4; j++)
                            {
                                var index = (int)p.GetBoneIndices(i, j);
                                var weight = p.GetWeight(i, j);
                                if (weight == 0)
                                    continue;

                                vertex.Envelope.Weights.Add(new IOBoneWeight()
                                {
                                    BoneName = bones[index].Name,
                                    Weight = weight,
                                });
                            }
                        }
                        iomesh.Vertices.Add(vertex);
                    }

                    //Indices as triangles
                    foreach (var ind in p.Indices)
                        poly.Indicies.Add(ind + vertexID);
                    //Shift indices for next polygon sharing the same vertex list
                    vertexID += p.Positions.Count;
                }
            }

            return scene;
        }

        static Matrix4x4 ConvertMatrix(OpenTK.Matrix4 m)
        {
            return new Matrix4x4(
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44);
        }

        public static HsfFile Import(string filePath, HsfFile hsfParent, ImportSettings settings)
        {
            settings.UseOriginalMaterials = false;
            settings.UseCustomBones = false;

            var node_list = hsfParent.ObjectNodes.ToList();

            //Keep original nodes by default but remove any previous mesh nodes
            foreach (var node in node_list)
            {
             /*   if (node.Data.Type == ObjectType.Mesh && node.Name.Contains("itemhook"))
                {
                    node.Data = new HSFObjectData();
                }*/
                if (node.Data.Type == ObjectType.Mesh)


                {
                    //empty mesh nodes
                    // node.Data = new HSFObjectData();

                    node.Data.Type = ObjectType.NULL1;
                    node.MeshData = null;
                    node.Data.VertexIndex = -1;
                    node.Data.NormalIndex = -1;
                    node.Data.FaceIndex = -1;
                    node.Data.TexCoordIndex = -1;
                    node.Data.ColorIndex = -1;
                    node.Data.AttributeIndex = -1;
                    node.Data.CenvCount = 0;
                    node.Data.CenvIndex = -1;
                    node.Data.ClusterPositionsOffset = 0;
                    node.Data.ClusterNormalsOffset = 0;
                    node.Data.MaterialDataOffset = 0;

                  //  if (node.Name == "body")
                    //   node.Data = new HSFObjectData();
                }
            }

            var scene = IOManager.LoadScene(filePath, new IONET.ImportSettings()
            {
                SplitMeshMaterials = false,
                FlipUVs = true,
            });

            HsfFile hsf = new HsfFile();

            if (!settings.UseCustomBones)
                hsf.ObjectNodes.AddRange(node_list);

            hsf.SkeletonData = hsfParent.SkeletonData;

            //combine meshes into one
        /*    IOMesh mesh = new IOMesh();
            mesh.Name = "body1";


            int Index = 0;
            foreach (var model in scene.Models)
            {
                foreach (var iomesh in model.Meshes)
                {
                    foreach (var iopoly in iomesh.Polygons)
                    {
                        for (int i = 0; i < iopoly.Indicies.Count; i++)
                            iopoly.Indicies[i] += Index;

                        mesh.Polygons.Add(iopoly);
                    }
                    mesh.Vertices.AddRange(iomesh.Vertices);
                    Index += iomesh.Vertices.Count;
                }
            }

           scene.Models[0].Meshes.Clear();
           scene.Models[0].Meshes.Add(mesh);*/
            
            AddNodes(scene, hsf, settings);
            AddItemHookMeshes(hsf);
            PrepareObjectNodes(hsf);
            AddTextures(scene, hsf, hsfParent.Textures.ToList(), settings);
            AddMaterials(scene, hsf, hsfParent.Materials.ToList(), settings);

            PrepareFog(hsf, hsf.FogData, settings);
            LoadMeshes(scene, hsf, settings);

            hsf.Meshes.Clear();
            foreach (var obj in hsf.ObjectNodes)
            {
                if (obj.MeshData != null)
                    hsf.Meshes.Add(obj.MeshData);
            }

            if (settings.UseCustomBones)
                hsf.SkeletonData.FromObjectList(hsf);

            hsf.MatrixData.Update(hsf);

            return hsf;
        }

        static void PrepareObjectNodes(HsfFile file)
        {
            var nodes = file.ObjectNodes.ToList();

            file.ObjectNodes.Clear();
            file.ObjectNodes.AddRange(nodes.Where(x => x.Data.Type == ObjectType.Effect));
            file.ObjectNodes.AddRange(nodes.Where(x => x.Data.Type == ObjectType.Mesh));
            file.ObjectNodes.AddRange(nodes.Where(x => !file.ObjectNodes.Contains(x)));
            
            //Update the indices
            foreach (var node in file.ObjectNodes)
            {
                node.Data.ParentIndex = -1;
                node.Data.ChildrenCount = node.Children.Count;

                if (node.Parent != null)
                    node.Data.ParentIndex = file.ObjectNodes.IndexOf(node.Parent);
            }

            return;

         //   var nodes = file.ObjectNodes.ToList();
        }

        static void AddNodes(IOScene scene, HsfFile file, ImportSettings settings)
        {
            if (!settings.UseCustomBones)
            {
                //Only add nodes for the meshes present in the import
                foreach (var model in scene.Models)
                {
                    foreach (var mesh in model.Meshes)
                    {
                        //Create a new object node
                        HSFObjectData objData = ObjectDataSection.CreateNewObject(ObjectType.Mesh);

                        //Check if the mesh node already exists in the hierachy
                        var n = file.ObjectNodes.FirstOrDefault(x => x.Name == mesh.Name);

                        //if it does exist
                        if (n != null)
                        {
                            objData = n.Data;
                        }
                        else
                        {
                            //else create a new node
                            n = new HSFObject(mesh.Name, objData);

                            //Attach additional mesh nodes to the root motion for animating if present
                            var root = file.ObjectNodes.FirstOrDefault(x => x.Name == "root_motion");
                            if (root == null)
                                root = file.ObjectNodes.FirstOrDefault(x => x.HasHierachy() && x.Parent == null);
    
                            //Add to node tree and insert to root bone
                            file.ObjectNodes.Add(n);
                            root.Children.Add(n);

                            n.Data.ParentIndex = file.ObjectNodes.IndexOf(root);
                            n.Parent = root;
                        }
                    }
                }
            }
            else
            {
                scene.PrintNodeHierachy();

                foreach (var node in scene.Nodes)
                    if (node.Parent == null)
                        AddNodes(node, file);
            }
        }

        static HSFObject AddNodes(IOBone node, HsfFile file)
        {
            string name = node.Name;
            name = Utils.RenameDuplicateString(name, file.ObjectNodes.Select(x => x.Name).ToList());

            HSFObjectData objData = ObjectDataSection.CreateNewObject(ObjectType.NULL1);
            HSFObject obj_node = new HSFObject(name, objData);
            file.ObjectNodes.Add(obj_node);

            obj_node.Data.BaseTransform.Translate = new Vector3XYZ(node.Translation.X, node.Translation.Y, node.Translation.Z);
            obj_node.Data.BaseTransform.Scale = new Vector3XYZ(node.Scale.X, node.Scale.Y, node.Scale.Z);
            obj_node.Data.BaseTransform.Rotate = new Vector3XYZ(
                node.RotationEuler.X * STMath.Rad2Deg,
                node.RotationEuler.Y * STMath.Rad2Deg,
                node.RotationEuler.Z * STMath.Rad2Deg);
            obj_node.UpdateMatrix();

            if (node.Name.StartsWith("eff_"))
                obj_node.Data.Type = ObjectType.Effect;

            if (node.Name.StartsWith("ske_"))
                obj_node.Data.Type = ObjectType.Joint;

            if (node.Name.StartsWith("root"))
                obj_node.Data.Type = ObjectType.Root;

            foreach (IONode c in node.Children)
            {
                var c_node = AddNodes(c, file);
                obj_node.Children.Add(c_node);
                c_node.Parent = obj_node;
            }

            obj_node.Data.ChildrenCount = obj_node.Children.Count;

            return obj_node;
        }

        static void AddItemHookMeshes(HsfFile hsf)
        {
            //Setup item hook meshes if not present in the import
            foreach (var node in hsf.ObjectNodes)
            {
                //Item hook is in the hierachy and is a standard node type
                //Turn it into a mesh node type
                if (node.Name.Contains("itemhook") && node.Data.Type == ObjectType.NULL1)
                {
                    node.Data.Type = ObjectType.Mesh;
                    node.MeshData = new Mesh(node, node.Name);
                    hsf.Meshes.Add(node.MeshData);

                    //Empty triangle
                    for (int i = 0; i < 6; i++)
                    {
                        node.MeshData.Positions.Add(new Vector3(0, 0.1f, 0.2f));
                        node.MeshData.Normals.Add(new Vector3(0, 1, 0));
                    }

                    //Add 2 primitive triangles
                    {
                        PrimitiveObject prim = new PrimitiveObject();
                        node.MeshData.Primitives.Add(prim);
                        prim.Flags = 0;
                        prim.NbtData = new OpenTK.Vector3(1, 0, 0);

                        prim.Type = PrimitiveType.Triangle;
                        prim.Vertices = new VertexGroup[4]
                        {
                            new VertexGroup() { PositionIndex = 0, NormalIndex = 0, UVIndex = -1, ColorIndex = -1, },
                            new VertexGroup() { PositionIndex = 2, NormalIndex = 2, UVIndex = -1, ColorIndex = -1, },
                            new VertexGroup() { PositionIndex = 3, NormalIndex = 3, UVIndex = -1, ColorIndex = -1, },
                            new VertexGroup(),//Empty last group
                        };
                        prim.MaterialIndex = hsf.Materials.Count;
                    }

                    {
                        PrimitiveObject prim = new PrimitiveObject();
                        node.MeshData.Primitives.Add(prim);
                        prim.Flags = 0;
                        prim.NbtData = new OpenTK.Vector3(1, 0, 0);

                        prim.Type = PrimitiveType.Triangle;
                        prim.Vertices = new VertexGroup[4]
                        {
                            new VertexGroup() { PositionIndex = 4, NormalIndex = 4, UVIndex = -1, ColorIndex = -1, },
                            new VertexGroup() { PositionIndex = 1, NormalIndex = 1, UVIndex = -1, ColorIndex = -1, },
                            new VertexGroup() { PositionIndex = 5, NormalIndex = 5, UVIndex = -1, ColorIndex = -1, },
                            new VertexGroup(),//Empty last group
                        };
                        prim.MaterialIndex = hsf.Materials.Count;
                    }

                    hsf.Materials.Add(new Material() { Name = node.Name, });

                    node.MeshData.Envelopes.Clear();
                    node.MeshData.Envelopes.Add(new HSFEnvelope()
                    {
                        CopyCount = (uint)node.MeshData.Positions.Count, //copy count match vertex counter
                        VertexCount = 0, //set to 0
                    });
                    node.Data.CenvCount = node.MeshData.Envelopes.Count;
                }
            }
        }

        static void AddMaterials(IOScene scene, HsfFile hsf, List<Material> original_materials, ImportSettings settings)
        {
            if (settings.UseOriginalMaterials)
            {
                hsf.Materials.AddRange(original_materials);
            }
            else
            {                
                foreach (var mat in scene.Materials)
                {
                    var hsfMat = new Material();
                    hsfMat.MaterialData.VertexMode = LightingChannelFlags.Lighting;
                    hsfMat.Name = mat.Label == null ? mat.Name : mat.Label;
                    hsf.Materials.Add(hsfMat);

                    if (mat.DiffuseMap != null)
                    {
                        string texMap = Path.GetFileNameWithoutExtension(mat.DiffuseMap.FilePath);
                        int index = hsf.Textures.FindIndex(x => x.Name == texMap);
                        if (index != -1)
                        {
                            hsfMat.TextureAttributes.Add(new TextureAttribute()
                            {
                                Name = index.ToString(),
                                AttributeData = new AttributeData(),
                                Texture = hsf.Textures[index],
                            });
                        }
                    }
                }
            }

            //Force one material if none are present
            if (hsf.Materials.Count == 0)
            {
                var hsfMat = new Material();
                hsfMat.MaterialData.VertexMode = LightingChannelFlags.Lighting;
                hsfMat.Name = "Basic";
                hsf.Materials.Add(hsfMat);
            }
        }

        static void AddTextures(IOScene scene, HsfFile hsf, List<HSFTexture> original_textures, ImportSettings settings)
        {
            List<string> importedTextures = new List<string>();

            hsf.Textures.AddRange(original_textures);
            importedTextures.AddRange(original_textures.Select(x => x.Name));

            if (settings.UseOriginalMaterials)
            {
                hsf.Textures.AddRange(original_textures);
            }
            else
            {
                foreach (var mat in scene.Materials)
                {
                    if (mat.DiffuseMap == null)
                        continue;

                    if (importedTextures.Contains(mat.DiffuseMap.FilePath))
                        continue;

                    //Import the texture
                    if (File.Exists(mat.DiffuseMap.FilePath))
                    {
                        importedTextures.Add(mat.DiffuseMap.FilePath);

                        hsf.Textures.Add(CreateTexture(mat.DiffuseMap.FilePath));
                    }
                    else //import using a default 8x8 grid texture to be replaced later
                    {
                        importedTextures.Add(mat.DiffuseMap.FilePath);

                        string name = Path.GetFileNameWithoutExtension(mat.DiffuseMap.FilePath);

                        hsf.Textures.Add(CreateTexture(Resources.Resource1.Default, name));
                    }
                }
            }
        }

        static void LoadMeshes(IOScene scene, HsfFile hsf, ImportSettings settings)
        {
            bool has_envelopes = scene.Models.Any(x => x.Meshes.Any(x => x.HasEnvelopes()));

            foreach (var model in scene.Models)
            {
                foreach (var mesh in model.Meshes)
                {
                    var n = hsf.ObjectNodes.FirstOrDefault(x => x.Name == mesh.Name);
                    n.Data.Type = ObjectType.Mesh;

                    //Create a mesh object and attach to the node
                    hsf.Meshes.Add(CreateMesh(scene, hsf, n, mesh, settings, has_envelopes));
                }
            }
        }

        static void PrepareFog(HsfFile hsf, FogSection original_fog, ImportSettings settings)
        {
            hsf.FogData.Count = 1;
            hsf.FogData.FogType = GX.FogType.PERSP_EXP;
            hsf.FogData.Color = new Vector4(255, 0, 0, 255);
            hsf.FogData.Start = 0;
            hsf.FogData.End = 10000;
        }

        static Mesh CreateMesh(IOScene scene, HsfFile hsf, HSFObject n, IOMesh mesh, ImportSettings settings, bool has_envelopes)
        {
            var w = n.CalculateWorldMatrix();
            //Transform inverse from the current node parent
            var inverse_matrix = Matrix4Extension.ToNumerics(OpenTK.Matrix4.Invert(n.CalculateWorldMatrix()));
            mesh.TransformVertices(inverse_matrix);

            //Compute a bounding box of the mesh vertices
            var bounding = GenerateBoundingBox(mesh);
            n.Data.CullBoxMax = bounding.Max;
            n.Data.CullBoxMin = bounding.Min;

            Mesh msh = new Mesh(n, mesh.Name);
            n.MeshData = msh;

            List<Vector3> positions = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> texCoords = new List<Vector2>();
            List<Vector4> colors = new List<Vector4>();

            //Sort by rigging information
            //The way weights are optimized, search in batches of the same rig info
            HSFEnvelope envelope = null;

            HSFModelImporterEnvelopeCreator weight_calculator = new HSFModelImporterEnvelopeCreator();
            if (mesh.HasEnvelopes())
            {
                //Generate envelope data with positions and normals that transform during runtime
                envelope = weight_calculator.GenerateEnvelopeData(hsf, mesh);
                positions = weight_calculator.position_list;
                normals = weight_calculator.normals_list;

                //Prepare color and tex coord attributes
                for (int v = 0; v < mesh.Vertices.Count; v++)
                {
                    var vertex = mesh.Vertices[v];

                    if (vertex.Colors.Count > 0)
                    {
                        if (!colors.Contains(vertex.Colors[0]))
                            colors.Add(vertex.Colors[0]);
                    }
                    if (vertex.UVs.Count > 0)
                    {
                        if (!texCoords.Contains(vertex.UVs[0]))
                            texCoords.Add(vertex.UVs[0]);
                    }
                }
            }
            else
            {
                //Load position, normal, color and tex coord attributes
                for (int v = 0; v < mesh.Vertices.Count; v++)
                {
                    var vertex = mesh.Vertices[v];

                    if (!positions.Contains(vertex.Position))
                        positions.Add(vertex.Position);

                    if (!normals.Contains(vertex.Normal))
                        normals.Add(vertex.Normal);

                    if (mesh.HasColorSet(0))
                    {
                        if (!colors.Contains(vertex.Colors[0]))
                            colors.Add(vertex.Colors[0]);
                    }
                    if (vertex.UVs.Count > 0)
                    {
                        if (!texCoords.Contains(vertex.UVs[0]))
                            texCoords.Add(vertex.UVs[0]);
                    }
                }
            }
            //HSF files with envelopes typically have them for all the meshes when present
            //Create empty envelopes with a set copy counter
            if (has_envelopes && !mesh.HasEnvelopes())
            {
                envelope = new HSFEnvelope()
                {
                    CopyCount = (uint)positions.Count, //copy count match vertex counter
                    VertexCount = 0, //set to 0
                };
            }

            msh.Positions.AddRange(positions);
            msh.Normals.AddRange(normals);
            msh.TexCoord0.AddRange(texCoords);
            msh.Color0.AddRange(colors);
            if (envelope != null) //add envelope if present
                msh.Envelopes.Add(envelope);

            n.Data.CenvCount = msh.Envelopes.Count;

            foreach (var group in mesh.Polygons)
            {
                if (group.Indicies.Count == 0)
                    continue;

                //Check if the material exists in the material list
                int materialIndex = -1;

                var dae_material = scene.Materials.FirstOrDefault(x => x.Name == group.MaterialName);
                if (dae_material != null)
                    materialIndex = hsf.Materials.FindIndex(x => x.Name == dae_material.Label);

                if (materialIndex == -1)  //if material does not, add a new one unique to the polygon
                {
                    string mat_name = $"{mesh.Name}_mat{mesh.Polygons.IndexOf(group)}";

                    var hsfMat = new Material();
                    hsfMat.MaterialData.VertexMode = LightingChannelFlags.Lighting;
                    hsfMat.Name = mat_name;
                    hsf.Materials.Add(hsfMat);

                    materialIndex = hsf.Materials.IndexOf(hsfMat);
                }

                if (hsf.Materials[materialIndex].TextureAttributes.Count > 0)
                    n.Data.AttributeIndex = 0;

                //Force vertex mode as vertex colors when present
                //Skip setting any if vertex alpha is hidden to prevent confusion
                if (mesh.HasColorSet(0) && mesh.Vertices.All(x => x.Colors[0].W != 0 && x.Colors[0] != Vector4.One))
                    hsf.Materials[materialIndex].MaterialData.VertexMode = LightingChannelFlags.VertexColorsWithAlpha;

                var primlist = CreatePrimitiveList(mesh, group, settings);
                foreach (var primitive in primlist)
                {
                    if (primitive.Indices.Count == 0)
                        continue;

                    PrimitiveObject prim = new PrimitiveObject();
                    msh.Primitives.Add(prim);

                    void SetVertex(int i, int distIndex = -1)
                    {
                        var vertex_id = (int)primitive.Indices[i];
                        var vertex = mesh.Vertices[vertex_id];

                        short colorIndex = -1;
                        short texCoordIndex = -1;
                        short positionIndex = -1;
                        short normaIndex = -1;

                        if (weight_calculator.pos_nrm_info.ContainsKey(vertex_id))
                        {
                            positionIndex = weight_calculator.pos_nrm_info[vertex_id].PositionIndex;
                            normaIndex = weight_calculator.pos_nrm_info[vertex_id].NormalIndex;
                        }
                        else
                        {
                            positionIndex = (short)positions.IndexOf(vertex.Position);
                            normaIndex = (short)normals.IndexOf(vertex.Normal);
                        }

                        if (vertex.Colors.Count > 0)
                            colorIndex = (short)colors.IndexOf(vertex.Colors[0]);
                        if (vertex.UVs.Count > 0)
                            texCoordIndex = (short)texCoords.IndexOf(vertex.UVs[0]);

                        prim.Vertices[distIndex == -1 ? i : distIndex] = new VertexGroup()
                        {
                            PositionIndex = positionIndex,
                            UVIndex = texCoordIndex,
                            ColorIndex = colorIndex,
                            NormalIndex = normaIndex,
                        };
                    }

                    prim.Type = PrimitiveType.Triangle;
                    if (primitive.Type == GX.Command.DRAW_TRIANGLE_STRIP)
                        prim.Type = PrimitiveType.TriangleStrip;

                    prim.Flags = 0;
                    prim.NbtData = new OpenTK.Vector3(1, 0, 0);
                    if (hsf.Materials.Count > materialIndex)
                        prim.MaterialIndex = materialIndex;

                    if (prim.Type == PrimitiveType.TriangleStrip)
                    {
                        prim.Vertices = new VertexGroup[Math.Max(primitive.Indices.Count + 2, 4)];

                        //First triangle
                        SetVertex(0);
                        SetVertex(1);
                        SetVertex(2);

                        //Game adds +1 extra for altered tri order
                        SetVertex(1, 3);
                        //We want to add one more as our tri strips use ABC, BCA, CAB order
                        SetVertex(2, 4);

                        //The tri strips
                        for (int i = 3; i < primitive.Indices.Count; i++)
                            SetVertex(i, i + 2);
                    }
                    else
                    {
                        prim.Vertices = new VertexGroup[4];
                        prim.Vertices[3] = new VertexGroup(); //Empty last group

                        for (int i = 0; i < 3; i++)
                            SetVertex(i);
                    }
                }
            }

            return msh;
        }

        static List<IndexedPrimitive> CreatePrimitiveList(IOMesh mesh, IOPolygon group, ImportSettings settings)
        {
            List<IndexedPrimitive> primlist = new List<IndexedPrimitive>();

            //Prepare a primitive of tri strips or tri fans if toggled
            rsmeshopt.StripifyAlgo stripAlgo = rsmeshopt.StripifyAlgo.NvTriStripPort;
            List<uint> tr_indices = CreatePrimitiveIndices(mesh, group,
                settings.UseTriStrips && group.Indicies.Count > 6 ? GX.Command.DRAW_TRIANGLE_STRIP : GX.Command.DRAW_TRIANGLES,
                stripAlgo);

            IndexedPrimitive indexed_prim = new IndexedPrimitive();
            indexed_prim.Type = settings.UseTriStrips ? GX.Command.DRAW_TRIANGLE_STRIP : GX.Command.DRAW_TRIANGLES;
            primlist.Add(indexed_prim);
            indexed_prim.DrawMatrixIndices[0] = 0;

            if (settings.UseTriStrips)
            {
                for (int i = 0; i < tr_indices.Count; i++)
                {
                    //Terminator reached, create new primitive
                    if (tr_indices[i] == uint.MaxValue)
                    {
                        indexed_prim = new IndexedPrimitive();
                        primlist.Add(indexed_prim);
                    }
                    else
                    {
                        indexed_prim.Indices.Add(tr_indices[i]);
                    }
                }
            }
            else
            {
                primlist.Clear();
                for (int i = 0; i < tr_indices.Count / 3; i++)
                {
                    int index = i * 3;

                    indexed_prim = new IndexedPrimitive();
                    primlist.Add(indexed_prim);

                    indexed_prim.Indices = new uint[3].ToList();
                    indexed_prim.Indices[0] = tr_indices[index + 0];
                    indexed_prim.Indices[1] = tr_indices[index + 1];
                    indexed_prim.Indices[2] = tr_indices[index + 2];
                }
            }
            return primlist;
        }

        static List<uint> CreatePrimitiveIndices(IOMesh mesh, IOPolygon group, GX.Command type, rsmeshopt.StripifyAlgo algo)
        {
            uint[] faces = new uint[group.Indicies.Count];
            for (int i = 0; i < group.Indicies.Count; i++)
                faces[i] = (uint)group.Indicies[i];

            switch (type)
            {
                case GX.Command.DRAW_TRIANGLE_STRIP:
                    {
                        List<rsmeshopt.Vec3> positions = new List<rsmeshopt.Vec3>();
                        foreach (var vertex in mesh.Vertices)
                        {
                            positions.Add(new rsmeshopt.Vec3()
                            {
                                X = vertex.Position.X,
                                Y = vertex.Position.Y,
                                Z = vertex.Position.Z,
                            });
                        }
                        return rsmeshopt.Stripify(algo, faces.ToList(), positions);
                    }
                case GX.Command.DRAW_TRIANGLE_FAN:
                    {
                        return rsmeshopt.MakeFans(faces.ToList(), 0, 4, 1);
                    }
                default:
                    return faces.ToList();
            }
        }

        public class IndexedPrimitive
        {
            public GX.Command Type = GX.Command.DRAW_TRIANGLE_STRIP;

            public short[] DrawMatrixIndices = new short[10] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };

            public List<uint> Indices = new List<uint>();
        }

        static HSFTexture CreateTexture(string filePath) {
            var name = System.IO.Path.GetFileNameWithoutExtension(filePath);
            return CreateTexture(Image.Load<Rgba32>(filePath), name);
        }

        static HSFTexture CreateTexture(byte[] data, string name) {
            return CreateTexture(Image.Load<Rgba32>(data), name);
        }

        static HSFTexture CreateTexture(Image<Rgba32> imageFile, string name)
        {
            var imageData = imageFile.GetSourceInBytes();
            BitmapExtension.ConvertBgraToRgba(imageData);

            var format = Decode_Gamecube.TextureFormats.CMPR;
            var info = new TextureInfo();
            info.Bpp = 4;
            info.Width = (ushort)imageFile.Width;
            info.Height = (ushort)imageFile.Height;
            info.PaletteEntries = 0;
            info.TextureTint = 0;
            info.PaletteIndex = -1;
            info.Format = (byte)HSFTexture.GetFormatId(format);
            info.MaxLOD = 0;

            var data = Decode_Gamecube.EncodeData(imageData,
               Decode_Gamecube.TextureFormats.CMPR, 
               Decode_Gamecube.PaletteFormats.RGB565, imageFile.Width, imageFile.Height);

            return new HSFTexture(name, info, data.Item1);
        }

        class BoundingBox
        {
            public Vector3XYZ Min;
            public Vector3XYZ Max;
        }

        static BoundingBox GenerateBoundingBox(IOMesh mesh)
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
