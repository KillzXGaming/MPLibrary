using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;
using Toolbox.Core.OpenGL;
using OpenTK;

namespace MPLibrary.DS
{
    public class HBDF : IFileFormat, IModelFormat
    {
        public bool CanSave { get; set; } = false;

        public string[] Description { get; set; } = new string[] { "Mario Party DS Resource" };
        public string[] Extension { get; set; } = new string[] { "*.hdbf" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream, true))
            {
                return reader.CheckSignature(4, "HBDF");
            }
        }

        public ModelRenderer Renderer => new ModelRenderer(ToGeneric());

        public HsdfFile Header;

        public void Load(System.IO.Stream stream) {
            Header = new HsdfFile(stream);
        }

        public void Save(System.IO.Stream stream) {
            Header.Save(stream);
        }

        public STGenericModel ToGeneric()
        {
            var model = new STGenericModel(FileInfo.FileName);
            foreach (var mdl in Header.Models) {
                foreach (var obj in mdl.Objects) {
                    if (obj.Type == ObjectBlock.ObjectType.Mesh)
                        model.Meshes.Add(LoadMesh(obj, obj.MeshData));
                }
            }
            return model;
        }

        private STGenericMesh LoadMesh(ObjectBlock obj, MeshBlock mesh)
        {
            STGenericMesh genericMesh = new STGenericMesh();
            genericMesh.Name = obj.Name;

            var transform = obj.GetTransform();
            var meshInfo = obj.MeshData;
            var ctx = NitroGX.ReadCmds(meshInfo.Data);
            for (int i = 0; i < ctx.vertices.Count; i++)
            {
                STVertex vertex = new STVertex();
                vertex.Position = ctx.vertices[i].Position;
                vertex.Normal = ctx.vertices[i].Normal;
                vertex.TexCoords = new Vector2[1]
                    { ctx.vertices[i].TexCoord };
                vertex.Colors = new Vector4[1]
                    { new Vector4(ctx.vertices[i].Color,ctx.vertices[i].Alpha) };

                vertex.Position = Vector3.TransformPosition(vertex.Position, transform);
                genericMesh.Vertices.Add(vertex);
            }

            uint[] faces = new uint[ctx.indices.Count];
            for (int i = 0; i < ctx.indices.Count; i++)
                faces[i] = ctx.indices[i];

            genericMesh.PolygonGroups.Add(new STPolygonGroup()
            {
                PrimitiveType = STPrimitiveType.Triangles,
                Faces = faces.ToList(),
            });

            return genericMesh;
        }
    }
}
