using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;
using Toolbox.Core.OpenGL;
using OpenTK;
using Toolbox.Core.ModelView;

namespace MPLibrary.MP10
{
    public class BNFM : ObjectTreeNode, IFileFormat, IModelFormat
    {
        public bool CanSave { get; set; } = false;

        public string[] Description { get; set; } = new string[] { "ND Cubed Resource" };
        public string[] Extension { get; set; } = new string[] { "*.bnfm" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream, true))
            {
                reader.SetByteOrder(true);
                return reader.ReadUInt16() == 0x5755;
            }
        }

        public ModelRenderer Renderer => new ModelRenderer(ToGeneric());

        public BnfmFile Header;

        public void Load(System.IO.Stream stream) {
            Header = new BnfmFile(stream);
            var model = ToGeneric();
            foreach (var node in model.CreateTreeHiearchy().Children)
                AddChild(node);
            this.Tag = this;
            this.Label = FileInfo.FileName;

            if (Header.SkeletalAnimations.Count > 0) {
                Tag = Header.SkeletalAnimations[0];
            }
        }

        public void Save(System.IO.Stream stream) {
            Header.Save(stream);
        }

        private STGenericModel Model;
        public STGenericModel ToGeneric()
        {
            if (Model != null) return Model;

            STGenericModel model = new STGenericModel(FileInfo.FileName);
            model.Skeleton = new STSkeleton();
            foreach (var bone in Header.Bones)
            {
                var matrix = bone.InverseTransform.Inverted();
                model.Skeleton.Bones.Add(new STBone(model.Skeleton)
                {
                    Name = bone.Name,
                    Position = bone.Position,
                    Rotation = matrix.ExtractRotation(),
                    Scale = matrix.ExtractScale(),
                    ParentIndex = bone.ParentIndex,
                });
            }
            model.Skeleton.Reset();
            model.Skeleton.Update();

            List<STGenericMaterial> materials = new List<STGenericMaterial>();
            foreach (var mat in Header.Materials)
            {
                STGenericMaterial genericMaterial = new STGenericMaterial();
                genericMaterial.Name = mat.Name;
                materials.Add(genericMaterial);

                for (int i = 0; i < mat.TextureSlots.Length; i++)
                {
                    if (mat.TextureSlots[i] == null)
                        continue;

                    genericMaterial.TextureMaps.Add(new STGenericTextureMap()
                    {
                        Name = mat.TextureSlots[i].Name,
                        Type = STTextureType.Diffuse,
                        WrapU = STTextureWrapMode.Mirror,
                        WrapV = STTextureWrapMode.Mirror,
                    });
                    break;
                }
            }

            foreach (var mesh in Header.Meshes)
            {
                if (mesh.Vertices.Count == 0)
                    continue;

                STGenericMesh genericMesh = new STGenericMesh();
                genericMesh.Name = mesh.Name;
                model.Meshes.Add(genericMesh);

                foreach (var vert in mesh.Vertices)
                {
                    if (mesh.BoneIndices.Length == 1)
                    {
                        var bone = model.Skeleton.Bones[(int)mesh.BoneIndices[0]];
                        vert.Position = Vector3.TransformPosition(vert.Position, bone.Transform);
                    }

                    genericMesh.Vertices.Add(new STVertex()
                    {
                        Position = vert.Position,
                        Normal = vert.Normal,
                        TexCoords = new Vector2[] { vert.TexCoord0, vert.TexCoord1 },
                        Colors = new Vector4[] { vert.Color },
                    });
                }

                Console.WriteLine($"Vertices {mesh.Vertices.Count}");

                genericMesh.FlipUvsVertical();

                var poly = new STPolygonGroup();
                poly.Material = materials[(int)mesh.MaterialIndex];
                foreach (var face in mesh.Faces)
                    poly.Faces.Add(face);
                genericMesh.PolygonGroups.Add(poly);
            }

            Model = model;
            return model;
        }
    }
}
