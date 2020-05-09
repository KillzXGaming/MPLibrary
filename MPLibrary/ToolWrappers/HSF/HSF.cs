using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;
using Toolbox.Core.ModelView;
using Toolbox.Core.OpenGL;
using OpenTK;

namespace MPLibrary.GCN
{
    public class HSF : ObjectTreeNode, IFileFormat, IModelFormat
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "Mario Party GCN Resource" };
        public string[] Extension { get; set; } = new string[] { "*.hsf" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream, true))
            {
                return reader.CheckSignature(4, "HSFV");
            }
        }

        public List<STGenericTexture> GenericTextures = new List<STGenericTexture>();
        public List<STGenericMesh> GenericMeshes = new List<STGenericMesh>();

        public HsfFile Header;
        public HSF_Renderer HSFRenderer;
        public STSkeleton Skeleton;

        public ModelRenderer Renderer => HSFRenderer;

        private STGenericModel Model;

        public STGenericModel ToGeneric()
        {
            if (Model != null) return Model;

            STGenericModel model = new STGenericModel(FileInfo.FileName);
            model.Skeleton = Skeleton;
            foreach (var mesh in GenericMeshes)
                model.Meshes.Add(mesh);

            foreach (var mesh in GenericMeshes)
                model.Materials.AddRange(mesh.GetMaterials());

            Console.WriteLine($"ToGeneric Meshes {model.Meshes.Count}");

            model.Textures.AddRange(GenericTextures);

            Model = model;
            return model;
        }

        public void Load(System.IO.Stream stream)
        {
            Header = new HsfFile(stream);
            Skeleton = new STSkeleton();
            this.Label = FileInfo.FileName;
            this.Tag = this;

            LoadSkeleton();
            LoadMeshes();
            LoadTextures();

            HSFRenderer = new HSF_Renderer(this, ToGeneric());
        }

        public void Save(System.IO.Stream stream)
        {
            Header.Save(stream);
        }

        private void LoadSkeleton()
        {
            ObjectTreeNode skeletonFolder = new ObjectTreeNode("Skeleton");
            this.AddChild(skeletonFolder);

            for (int i = 0; i < Header.ObjectCount; i++)
            {
                var info = Header.ObjectData.Objects[i];
                var name = Header.ObjectData.ObjectNames[i];

                //Add a dummy bone. Some bone data is set at runtime and uses large random values
                if (info.ChildrenCount > Header.ObjectCount)
                {
                    Skeleton.Bones.Add(new HSFBoneWrapper(info, Skeleton)
                    {
                        Name = name,
                        ParentIndex = -1,
                        Position = new Vector3(),
                        Scale = Vector3.One,
                        EulerRotation = new Vector3(),
                    });
                }
                else
                {
                    Skeleton.Bones.Add(new HSFBoneWrapper(info, Skeleton)
                    {
                        Name = name,
                        ParentIndex = -1,
                        Position = new OpenTK.Vector3(
                        info.BaseTransform.Translate.X,
                        info.BaseTransform.Translate.Y,
                        info.BaseTransform.Translate.Z) * HSF_Renderer.PreviewScale,
                        EulerRotation = new OpenTK.Vector3(
                    MathHelper.DegreesToRadians(info.BaseTransform.Rotate.X),
                    MathHelper.DegreesToRadians(info.BaseTransform.Rotate.Y),
                    MathHelper.DegreesToRadians(info.BaseTransform.Rotate.Z)),
                        Scale = new OpenTK.Vector3(
                        info.BaseTransform.Scale.X == 0 ? 1 : info.BaseTransform.Scale.X,
                        info.BaseTransform.Scale.Y == 0 ? 1 : info.BaseTransform.Scale.Y,
                        info.BaseTransform.Scale.Z == 0 ? 1 : info.BaseTransform.Scale.Z),
                    });
                }
            }

            for (int i = 0; i < Header.ObjectCount; i++)
            {
                if (Header.ObjectData.Objects[i].ChildrenCount > Header.ObjectCount)
                    Skeleton.Bones[i].ParentIndex = -1;
                else
                    Skeleton.Bones[i].ParentIndex = Header.ObjectData.Objects[i].ParentIndex;
            }

            var boneNodes = Skeleton.CreateBoneTree();
            foreach (var bone in boneNodes)
                skeletonFolder.AddChild(bone);

            Skeleton.Reset();
            Skeleton.Update();
        }

        private void LoadMeshes()
        {
            ObjectTreeNode meshesFolder = new ObjectTreeNode("Meshes");
            this.AddChild(meshesFolder);

            foreach (var mesh in Header.Meshes)
            {
                Dictionary<int, List<STVertex>> VertexMatMapper = new Dictionary<int, List<STVertex>>();

                int index = 0;
                foreach (var primative in mesh.Primitives)
                {
                    if (!VertexMatMapper.ContainsKey(primative.MaterialIndex))
                    {
                        VertexMatMapper.Add(primative.MaterialIndex, new List<STVertex>());
                    }

                    var vertices = VertexMatMapper[primative.MaterialIndex];
                    switch (primative.Type)
                    {
                        case PrimitiveType.Triangle:
                            vertices.Add(ToGenericVertex(mesh, primative.Vertices[0], Skeleton));
                            vertices.Add(ToGenericVertex(mesh, primative.Vertices[1], Skeleton));
                            vertices.Add(ToGenericVertex(mesh, primative.Vertices[2], Skeleton));
                            break;
                        case PrimitiveType.Quad:
                            vertices.Add(ToGenericVertex(mesh, primative.Vertices[0], Skeleton));
                            vertices.Add(ToGenericVertex(mesh, primative.Vertices[1], Skeleton));
                            vertices.Add(ToGenericVertex(mesh, primative.Vertices[2], Skeleton));
                            vertices.Add(ToGenericVertex(mesh, primative.Vertices[1], Skeleton));
                            vertices.Add(ToGenericVertex(mesh, primative.Vertices[3], Skeleton));
                            vertices.Add(ToGenericVertex(mesh, primative.Vertices[2], Skeleton));
                            break;
                        case PrimitiveType.TriangleStrip:
                            var verts = new List<STVertex>();

                            foreach (var dv in primative.Vertices)
                                verts.Add(ToGenericVertex(mesh, dv, Skeleton));
                            verts = ConvertTriStrips(verts);

                            vertices.AddRange(verts);
                            break;
                    }
                }

                foreach (var poly in VertexMatMapper)
                {
                    HSFMesh genericMesh = new HSFMesh();
                    genericMesh.Name = mesh.Name;
                    GenericMeshes.Add(genericMesh);

                    meshesFolder.AddChild(new ObjectTreeNode(mesh.Name) { Tag = genericMesh });

                    int objectIndex = Header.ObjectData.Objects.IndexOf(mesh.ObjectData);
                    if (objectIndex != -1)
                    {
                        var bone = Skeleton.Bones[objectIndex];
                        genericMesh.ObjectNode = bone;
                        bone.Visible = false;
                    }

                    HSFMaterialWrapper genericMat = new HSFMaterialWrapper(this);

                    STPolygonGroup group = new STPolygonGroup();
                    group.Material = genericMat;
                    genericMesh.PolygonGroups.Add(group);

                    var matData = Header.Materials[poly.Key].MaterialData;

                    genericMat.Material = Header.Materials[poly.Key];
                    genericMat.Name = Header.Materials[poly.Key].Name;

                    var pass_flags = matData.AltFlags & HSF_Renderer.PASS_BITS;
                    if (pass_flags != 0 || (matData.TransparencyInverted != 0 &&
                        matData.VertexMode == 0))
                    {
                        genericMesh.IsTransparent = true;
                    }

                    genericMat.Mesh = mesh;

                    var attributes = Header.Materials[poly.Key].Textures;
                    for (int i = 0; i < attributes.Count; i++)
                    {
                        var attribute = attributes[i].Item2;
                        var texIndex = attribute.TextureIndex;
                        genericMat.Attributes.Add(attribute);
                        group.Material.TextureMaps.Add(new HSFMatTexture(this)
                        {
                            Attribute = attribute,
                            Name = Header.Textures[texIndex].Name,
                            TextureIndex = texIndex,
                            Type = i == 0 ? STTextureType.Diffuse : STTextureType.None,
                            WrapU = ConvertWrapMode(attribute.WrapS),
                            WrapV = ConvertWrapMode(attribute.WrapT),
                            MagFilter = STTextureMagFilter.Linear,
                            MinFilter = STTextureMinFilter.Linear,
                        });
                    }

                    genericMesh.Vertices.AddRange(poly.Value);
                    genericMesh.Optmize(group);
                }
            }
        }

        private void LoadTextures()
        {
            var textureFolder = new ObjectTreeNode("Textures");
            this.AddChild(textureFolder);

            GenericTextures = new List<STGenericTexture>();
            for (int i = 0; i < Header.TextureCount; i++)
            {
                var data = Header.Textures[i].ImageData;
                var name = Header.Textures[i].Name;
                var info = Header.Textures[i].TextureInfo;
                var tex = new PartyStudio.HSFTexture(name, info, data);
                if (Header.Textures[i].HasPaletteData())
                {
                    var palette = Header.Textures[i].PaletteData;
                    var format = Decode_Gamecube.PaletteFormats.IA8;
                    if (info.Format == 0x09 || info.Format == 0x0B)
                        format = Decode_Gamecube.PaletteFormats.RGB565;
                    if (info.Format == 0x0A)
                        format = Decode_Gamecube.PaletteFormats.RGB5A3;
                    ((Toolbox.Core.Imaging.GamecubeSwizzle)tex.Platform).SetPalette(palette, format);

                   // tex.SetPaletteData(palette, Decode_Gamecube.ToGenericPaletteFormat(format));
                }

                textureFolder.AddChild(new ObjectTreeNode(tex.Name) { Tag = tex, ImageKey = "Texture" });
                GenericTextures.Add(tex);
            }
        }

        private STTextureWrapMode ConvertWrapMode(int value)
        {
            if (value == 0) return STTextureWrapMode.Clamp;
            else if (value == 1) return STTextureWrapMode.Repeat;
            else if (value == 2) return STTextureWrapMode.Mirror;
            else
                return STTextureWrapMode.Mirror;
        }

        private List<STVertex> ConvertTriStrips(List<STVertex> vertices)
        {
            List<STVertex> outVertices = new List<STVertex>();
            for (int index = 2; index < vertices.Count; index++)
            {

                bool isEven = (index % 2 != 1);

                var vert1 = vertices[index - 2];
                var vert2 = isEven ? vertices[index] : vertices[index - 1];
                var vert3 = isEven ? vertices[index - 1] : vertices[index];

                if (!vert1.Position.Equals(vert2.Position) &&
                    !vert2.Position.Equals(vert3.Position) &&
                    !vert3.Position.Equals(vert1.Position))
                {
                    outVertices.Add(vert3);
                    outVertices.Add(vert2);
                    outVertices.Add(vert1);
                }
            }
            return outVertices;
        }

        public STVertex ToGenericVertex(Mesh mesh, VertexGroup group, STSkeleton skeleton)
        {
            List<int> boneIndices = new List<int>();
            List<float> boneWeights = new List<float>();

            Vector3 position = Vector3.Zero;
            Vector3 normal = Vector3.Zero;
            Vector2 uv0 = Vector2.Zero;
            Vector4 color = Vector4.One;

            if (mesh.Positions.Count > group.PositionIndex)
                position = mesh.Positions[group.PositionIndex];
            if (mesh.Normals.Count > group.NormalIndex && group.NormalIndex != -1)
                normal = mesh.Normals[group.NormalIndex];
            if (mesh.TexCoords.Count > group.UVIndex && group.UVIndex != -1)
                uv0 = mesh.TexCoords[group.UVIndex];
            if (mesh.Colors.Count > group.ColorIndex && group.ColorIndex != -1)
                color = mesh.Colors[group.ColorIndex];

            foreach (var msh in Header.ObjectData.Meshes)
            {
                if (msh.ObjectParent == mesh.ObjectData)
                {
                    if (msh.Positions.Count > group.PositionIndex)
                        position = msh.Positions[group.PositionIndex];
                    if (msh.Normals.Count > group.NormalIndex)
                        normal = msh.Normals[group.NormalIndex];
                }
            }

            position *= HSF_Renderer.PreviewScale;

            if (!mesh.HasRigging)
            {
                int nodeIndex = Header.ObjectData.Objects.IndexOf(mesh.ObjectData);
                boneIndices.Clear();
                boneWeights.Clear();
                boneIndices.Add(nodeIndex);
                boneWeights.Add(1);

                 position = Vector3.TransformPosition(position, skeleton.Bones[nodeIndex].Transform);
            }

            if (mesh.HasRigging)
            {
                foreach (var singleBind in mesh.RiggingInfo.SingleBinds)
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
                foreach (var multiBind in mesh.RiggingInfo.DoubleBinds)
                {
                    for (int i = mbOffset; i < mbOffset + multiBind.Count; i++)
                    {
                        var w = mesh.RiggingInfo.DoubleWeights[i];
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
                foreach (var multiBind in mesh.RiggingInfo.MultiBinds)
                {
                    if (group.PositionIndex >= multiBind.PositionIndex && group.PositionIndex < multiBind.PositionIndex + multiBind.PositionCount)
                    {
                        boneIndices.Clear();
                        boneWeights.Clear();

                        Vector4 indices = new Vector4(0);
                        Vector4 weight = new Vector4(0);
                        for (int i = mbOffset; i < mbOffset + multiBind.Count; i++)
                        {
                            indices[i - mbOffset] = mesh.RiggingInfo.MultiWeights[i].BoneIndex;
                            weight[i - mbOffset] = mesh.RiggingInfo.MultiWeights[i].Weight;
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

            }

            return new STVertex()
            {
                Position = position,
                Colors = new Vector4[] { color },
                Normal = normal,
                BoneIndices = boneIndices,
                BoneWeights = boneWeights,
                TexCoords = new Vector2[] { uv0 },
            };
        }
    }
}
