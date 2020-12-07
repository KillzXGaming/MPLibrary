using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.OpenGL;
using Toolbox.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace MPLibrary.GCN
{
    public class HSF_Renderer : ModelRenderer
    {
        public static float PreviewScale = 0.05f;

        //From https://github.com/gamemasterplc/hsfview/blob/9fadb1a8b3555d2bdd1435f16074f6333eb4978e/source/hsffile.h
        public static readonly int PASS_BITS = 0xF; //If pass_flags when ANDed by this value isn't 0 then Z Writes are Disabled
        public static readonly int VTXMODE_DEFAULT = 0x1;
        public static readonly int VTXMODE_USE_NBT = 0x2;
        public static readonly int VTXMODE_USE_CLR0 = 0x4;
        public static readonly int DONT_CULL_BACKFACES = 0x2;
        public static readonly int BLEND_MODE_MASK = 0x30;
        public static readonly int BLEND_SRCALPHA_ONE = 0x10;
        public static readonly int BLEND_ZERO_INVSRCCLR = 0x20;
        public static readonly int BLEND_SRCALPHA_INVSRCALPHA = 0x0;
        public static readonly int PUNCHTHROUGH_ALPHA_BITS = 0x1200;
        public static readonly int MATERIAL_INDEX_MASK = 0xFFF;
        public static readonly int HIGHLIGHT_FRAME_MASK = 0xF0;
        public static readonly int HIGHLIGHT_ENABLE = 0x100;

        //Attribute Defines
        public static readonly int WRAP_CLAMP = 0;
        public static readonly int WRAP_REPEAT = 1;
        public static readonly int ENABLE_NEAREST_FILTER = 0x40;
        public static readonly int ENABLE_MIPMAP = 0x80;
        public static readonly int MIPMAP_BIT_POS = 7;

        public HSF HSFParent;

        public HSF_Renderer(HSF parent, STGenericModel model) : base(model) {
            HSFParent = parent;
        }

        public override void PrepareShaders()
        {
            if (ActiveShader != null)
                return;

            var vertShader = System.IO.File.ReadAllText($"{Runtime.ExecutableDir}/Shader/HSF/HSF.vert");
            var fragShader = System.IO.File.ReadAllText($"{Runtime.ExecutableDir}/Shader/HSF/HSF.frag");

            ActiveShader = new ShaderProgram(
                new VertexShader(vertShader),
                new FragmentShader(fragShader));
        }

        public override void ReloadUniforms(ShaderProgram shader)
        {
            SetBoneUniforms(shader, Scene.Models[0].Skeleton);
            SetRenderSettings(shader);
        }

        private void SetBoneUniforms(ShaderProgram shader, STSkeleton Skeleton)
        {
            int i = 0;
            foreach (var bone in Skeleton.Bones)
            {
                Matrix4 transform = bone.Inverse * bone.Transform;
                GL.UniformMatrix4(GL.GetUniformLocation(shader.program, String.Format("bones[{0}]", i++)), false, ref transform);
            }
        }


        private void SetRenderSettings(ShaderProgram shader)
        {
            shader.SetFloat("brightness", 1.0f);
            shader.SetInt("renderType", (int)0);
            shader.SetInt("selectedBoneIndex", -1);
            shader.SetBoolToInt("renderVertColor", true);
        }


        int uboAtt = -1;
        private void SetAttributeUBO()
        {
            GL.GenBuffers(1, out uboAtt);
        }

        public override void RenderMaterials(ShaderProgram shader, STGenericMesh mesh, 
            STPolygonGroup group, STGenericMaterial material, Vector4 highlight_color)
        {
            var msh = (HSFMesh)mesh;

            shader.SetVector4("highlight_color", highlight_color);

            //Note we render picking pass here for backface culling
            shader.SetFloat("brightness", 1.0f);
            SetRenderData(group.Material, shader, msh);
            SetTextureUniforms(shader, (HSFMaterialWrapper)group.Material);
        }

        private void SetTextureUniforms(ShaderProgram shader, HSFMaterialWrapper mat)
        {
            if (uboAtt == -1)
                SetAttributeUBO();

            var index = GL.GetUniformBlockIndex(shader.program, "AttributeBlock");
            GL.UniformBlockBinding(shader.program, index, 0);

            GL.BindBuffer(BufferTarget.UniformBuffer, uboAtt);
            GL.BufferData(BufferTarget.UniformBuffer, TextureAttribute.Size, IntPtr.Zero, BufferUsageHint.StaticDraw);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, index, uboAtt);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);

            LoadDebugTextureMaps(shader);

            GL.ActiveTexture(TextureUnit.Texture0 + 1);
            GL.BindTexture(TextureTarget.Texture2D, RenderTools.defaultTex.RenderableTex.TexID);

            shader.SetInt("textureCount", mat.TextureMaps.Count);
            Console.WriteLine($"textureCount {mat.TextureMaps.Count}");

            TextureAttribute[] attriutes = new TextureAttribute[6];
            for (int i = 0; i < 6; i++)
            {
                if (i == 1)
                {
                    attriutes[i].texPositionStart = new Vector2(0.5f, 1);
                    attriutes[i].texPositionEnd = new Vector2(1, 1);
                    attriutes[i].texScaleStart = new Vector2(1, 1);
                    attriutes[i].texScaleEnd = new Vector2(0, 0);
                }
                else
                {
                    attriutes[i].texPositionStart = new Vector2(0, 0);
                    attriutes[i].texPositionEnd = new Vector2(1, 1);
                    attriutes[i].texScaleStart = new Vector2(1, 1);
                    attriutes[i].texScaleEnd = new Vector2(0, 0);
                }
            }

            var attributeData = mat.ParentHSF.Header.AttributeData;
            for (int i = 0; i < mat.TextureMaps.Count; i++)
            {
                var hsfTexture = (HSFMatTexture)mat.TextureMaps[i];
                var texStart = hsfTexture.Attribute.TexAnimStart;
                var texEnd = hsfTexture.Attribute.TexAnimEnd;
                attriutes[i].texPositionStart = new Vector2(texStart.Position.X, texStart.Position.Y);
                attriutes[i].texPositionEnd = new Vector2(texEnd.Position.X, texEnd.Position.Y);
                attriutes[i].texScaleStart = new Vector2(texStart.Scale.X, texStart.Scale.Y);
                attriutes[i].texScaleEnd = new Vector2(texEnd.Scale.X, texEnd.Scale.Y);
                // attriutes[i].texParam = hsfTexture.Attribute.NbtEnable;
                //  attriutes[i].blendingFlag = hsfTexture.Attribute.BlendingFlag;
                // attriutes[i].alphaFlag = hsfTexture.Attribute.AlphaFlag;

                int attIndex = attributeData.Attributes.IndexOf(hsfTexture.Attribute);
                if (HSFParent.Header.AttributeAnimControllers.ContainsKey(attIndex))
                {
                    var controller = HSFParent.Header.AttributeAnimControllers[attIndex];
                    attriutes[i].texPositionStart.X = controller.TranslateX;
                    attriutes[i].texPositionStart.Y = controller.TranslateY;
                }

                var binded = BindTexture(shader, Scene.Models[0].Textures, (HSFMatTexture)mat.TextureMaps[i], i + 1);
                shader.SetInt($"texture{i}", i + 1);
            }

            GL.BindBuffer(BufferTarget.UniformBuffer, uboAtt);
            GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, TextureAttribute.Size, attriutes);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        private static void LoadDebugTextureMaps(ShaderProgram shader)
        {
            /*  GL.ActiveTexture(TextureUnit.Texture0 + 1);
              GL.BindTexture(TextureTarget.Texture2D, RenderTools.defaultTex.RenderableTex.TexID);

              GL.Uniform1(shader["debugOption"], 2);

              GL.ActiveTexture(TextureUnit.Texture11);
              GL.Uniform1(shader["weightRamp1"], 11);
              GL.BindTexture(TextureTarget.Texture2D, RenderTools.BoneWeightGradient.Id);

              GL.ActiveTexture(TextureUnit.Texture12);
              GL.Uniform1(shader["weightRamp2"], 12);
              GL.BindTexture(TextureTarget.Texture2D, RenderTools.BoneWeightGradient2.Id);


              GL.ActiveTexture(TextureUnit.Texture10);
              GL.Uniform1(shader["UVTestPattern"], 10);
              GL.BindTexture(TextureTarget.Texture2D, RenderTools.uvTestPattern.RenderableTex.TexID);*/
        }

        struct TextureAttribute
        {
            public Vector2 texScaleStart;
            public Vector2 texScaleEnd;
            public Vector2 texPositionStart;
            public Vector2 texPositionEnd;
            //  public float texParam;
            // public int blendingFlag;
            // public int alphaFlag;

            public static int Size => 128;
        }

        private static bool BindTexture(ShaderProgram shader, List<STGenericTexture> textures,
            HSFMatTexture texture, int id)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + id);
            GL.BindTexture(TextureTarget.Texture2D, RenderTools.defaultTex.RenderableTex.TexID);
            for (int i = 0; i < textures.Count; i++)
            {
                if (i == texture.TextureIndex)
                    BindGLTexture(textures[i], texture, shader);
            }
            return false;
        }

        private static void BindGLTexture(STGenericTexture texture, STGenericTextureMap matTex, ShaderProgram shader)
        {
            if (texture.RenderableTex == null || !texture.RenderableTex.GLInitialized)
                texture.LoadOpenGLTexture();

            //If the texture is still not initialized then return
            if (!texture.RenderableTex.GLInitialized)
                return;

            GL.BindTexture(TextureTarget.Texture2D, texture.RenderableTex.TexID);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)OpenGLHelper.WrapMode[matTex.WrapU]);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)OpenGLHelper.WrapMode[matTex.WrapV]);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)OpenGLHelper.MinFilter[matTex.MinFilter]);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)OpenGLHelper.MagFilter[matTex.MagFilter]);
            GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, 0.0f);
        }

        public void SetRenderData(STGenericMaterial mat, ShaderProgram shader, HSFMesh m)
        {
            var hsfMaterial = (HSFMaterialWrapper)mat;
            var materialData = hsfMaterial.Material.MaterialData;
            var objectData = hsfMaterial.Mesh.ObjectData;
            var textureCount = materialData.TextureCount;
            var vtx_mode = materialData.VertexMode;
            var use_color = vtx_mode & VTXMODE_USE_CLR0;
            var material_flags = objectData.RenderFlags | materialData.MaterialFlags;
            var pass_flags = materialData.AltFlags & PASS_BITS;
            var alpha_flags = material_flags & PUNCHTHROUGH_ALPHA_BITS;

            shader.SetInt("vertex_mode", materialData.VertexMode);
            shader.SetFloat("alpha_flags", alpha_flags);
            shader.SetFloat("pass_flags", pass_flags);

            Vector3 ambientColor = new Vector3(
                materialData.AmbientColor.R / 255f,
                materialData.AmbientColor.G / 255f,
                materialData.AmbientColor.B / 255f);
            Vector3 ambientLitColor = new Vector3(
                materialData.LitAmbientColor.R / 255f,
                materialData.LitAmbientColor.G / 255f,
                materialData.LitAmbientColor.B / 255f);
            Vector3 shadowColor = new Vector3(
                materialData.ShadowColor.R / 255f,
                materialData.ShadowColor.G / 255f,
                materialData.ShadowColor.B / 255f);

            float transparency = 1 - materialData.TransparencyInverted;

            int matIndex = HSFParent.Header.Materials.IndexOf(hsfMaterial.Material);
            if (HSFParent.Header.MatAnimControllers.ContainsKey(matIndex))
            {
                var controller = HSFParent.Header.MatAnimControllers[matIndex];
                ambientColor.X = controller.AmbientColorR;
                ambientColor.Y = controller.AmbientColorG;
                ambientColor.Z = controller.AmbientColorB;
                ambientLitColor.X = controller.LitAmbientColorR;
                ambientLitColor.Y = controller.LitAmbientColorG;
                ambientLitColor.Z = controller.LitAmbientColorB;
                shadowColor.X = controller.ShadowColorR;
                shadowColor.Y = controller.ShadowColorG;
                shadowColor.Z = controller.ShadowColorB;
                transparency = 1 - controller.TransparencyInverted;
            }

            shader.SetFloat("transparency", transparency);
            shader.SetVector3("ambient_color", ambientColor);
            shader.SetVector3("ambient_lit_color", ambientLitColor);
            shader.SetVector3("shadow_color", shadowColor);

            bool cullNone = (material_flags & DONT_CULL_BACKFACES) != 0;

            if (pass_flags != 0 || materialData.TransparencyInverted != 0)
            {
            }
            else
            {

            }

            /*   if (materialData.VertexMode == 0 && alpha_flags == 0 && pass_flags == 0)
               {
                   GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
               }
               else
               {
                   GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
               }*/

            bool IsTransparent = alpha_flags != 0;

            shader.SetBoolToInt("isTransparent", false);
            if (alpha_flags != 0)
            {
                shader.SetBoolToInt("isTransparent", true);
            }
            else
            {

            }
            if (cullNone)
                GL.Disable(EnableCap.CullFace);
            else
            {
                GL.Enable(EnableCap.CullFace);
                GL.CullFace(CullFaceMode.Back);
            }

        }
    }
}
