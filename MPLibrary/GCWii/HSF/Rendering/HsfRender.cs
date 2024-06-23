using GCNRenderLibrary.Rendering;
using GLFrameworkEngine;
using MPLibrary.GCN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.ViewModels;
using OpenTK;
using Toolbox.Core;
using MPLibrary.DS;
using System.Runtime.InteropServices;

namespace PartyStudio.GCN
{
    public class HsfRender : EditableObject, ISelectableContainer, IColorPickable
    {
        HsfFile HsfFile;

        public Action<HSFObject, SceneNode> OnMeshSelected;

        public static GLTexture2D LightMap;
        public static GLTexture2D ReflectMap;

        static GLSamplerObject LightMapSampler;
        static GLSamplerObject ReflectMapSampler;

        public List<GLGXTexture> TextureCache = new List<GLGXTexture>();

        List<SceneNode> DrawListOPA = new List<SceneNode>();
        List<SceneNode> DrawListXLU = new List<SceneNode>();

        Matrix4[] BoneViewMatrixArray = new Matrix4[0];
        Matrix4[] BoneWorldMatrixArray = new Matrix4[0];
        Matrix4[] BoneWorldInverseMatrixArray = new Matrix4[0];

        private CameraFrustum.Frustum[] BoneWorldMatrixArrayVisiblity;

        public SkeletonRenderer SkeletonRenderer;

        private bool DebugBoundingBoxDisplay = false;

        public IEnumerable<ITransformableObject> Selectables
        {
            get
            {
                List<ITransformableObject> meshes = new List<ITransformableObject>();
                if (RenderGlobals.MeshPicking)
                {
                    foreach (var mesh in DrawListOPA)
                        meshes.Add(mesh);
                    foreach (var mesh in DrawListXLU)
                        meshes.Add(mesh);
                }
                return meshes;
            }
        }

        public void UpdateLight(LightObject light)
        {
            //HSF only uses one light set
            foreach (var node in DrawListOPA)
                node.UpdateLight(light, 0);
            foreach (var node in DrawListXLU)
                node.UpdateLight(light, 0);
        }

        public void ReloadTextures()
        {
            foreach (var node in this.DrawListOPA)
                ReloadTextures(node);
            foreach (var node in this.DrawListXLU)
                ReloadTextures(node);
        }

        public void ReloadTextures(SceneNode node)
        {
            //Prepare the textures
            node.TextureObjects.Clear();
            for (int i = 0; i < node.Material.Textures.Length; i++)
            {
                if (node.Material.Textures[i] == null)
                    continue;

                var sampler = node.Material.Textures[i];
                var target = TextureCache.FirstOrDefault(x => x.Name == sampler.Texture);
                //Map by index instead if used
                if (sampler.TextureIndex > -1 && TextureCache.Count > sampler.TextureIndex)
                    target = TextureCache[sampler.TextureIndex];

                //Failed to find texture, skip
                if (target == null)
                    continue;

                //Create into a renderable GL texture map
                GLSamplerObject obj = new GLSamplerObject(target);
                obj.Name = sampler.Texture;
                obj.WrapU = GLEnumConverter.gxWrapToGL(sampler.WrapX);
                obj.WrapV = GLEnumConverter.gxWrapToGL(sampler.WrapY);
                obj.MagFilter = GLEnumConverter.gxFilterMagToGL(sampler.MagFilter);
                obj.MinFilter = GLEnumConverter.gxFilterMinToGL(sampler.MinFilter);
                node.TextureObjects.Add(obj);

                //Setup uniforms for the texture.
               // node.materialParams.TexParams[i] = new Vector4(target.Width, target.Height, 0, sampler.LODBias);
            }
            //Light map highlight
            node.TextureObjects.Add(LightMapSampler);
            //Reflection map
            node.TextureObjects.Add(ReflectMapSampler);
        }

        public void AddTexture(HSFTexture tex)
        {
            var info = tex.TextureInfo;

            tex.RenderTexture = new GLGXTexture(
               tex.Name, info.Width, info.Height, (uint)tex.GcnFormat, (uint)tex.GcnPaletteFormat, 1, tex.ImageData, tex.PaletteData);
            TextureCache.Add(tex.RenderTexture);
        }

        public void SetFog(FogSection fogData)
        {
            var fog = new FogObject();
            if (fogData.Count != 0 && (fogData.Start != 0 || fogData.End != 0))
            {
                fog.SetFog(GX.FogType.PERSP_EXP, //Always type 0x4
                    fogData.Start,
                    fogData.End, 1, 10000.000f); //znear/zfar is roughly a guess
                fog.Color = fogData.Color; //We only need the end color. HSF never uses start color.
            }

            foreach (var draw in this.DrawListOPA)
                draw.UpdateFog(fog);
            foreach (var draw in this.DrawListXLU)
                draw.UpdateFog(fog);
        }

        public HsfRender(HsfFile hsfFile, NodeBase parentNode = null) : base(parentNode)
        {
            HsfFile = hsfFile;
            CanSelect = true;
            this.Transform = new GLTransform();
            this.Transform.UpdateMatrix(true);

            Reload(hsfFile);
            SetFog(hsfFile.FogData);
        }

        public void Reload(HsfFile hsfFile)
        {
            CanSelect = true;

            BoneViewMatrixArray = new Matrix4[hsfFile.ObjectNodes.Count];
            BoneWorldMatrixArray = new Matrix4[hsfFile.ObjectNodes.Count];
            BoneWorldInverseMatrixArray = new Matrix4[hsfFile.ObjectNodes.Count];
            BoneWorldMatrixArrayVisiblity = new CameraFrustum.Frustum[hsfFile.ObjectNodes.Count];

            if (LightMap == null)
            {
                LightMap = GLTexture2D.FromBitmap(MPLibrary.Resources.Resource1.HsfLightMap);

                LightMapSampler =   new GLSamplerObject()
                {
                    TextureTarget = LightMap.Target,
                    ID = LightMap.ID,
                    Name = "LightMap",
                    WrapU = OpenTK.Graphics.OpenGL.TextureWrapMode.ClampToEdge,
                    WrapV = OpenTK.Graphics.OpenGL.TextureWrapMode.ClampToEdge,
                    MagFilter = OpenTK.Graphics.OpenGL.TextureMagFilter.Linear,
                    MinFilter = OpenTK.Graphics.OpenGL.TextureMinFilter.Linear,
                };
            }
            if (ReflectMap == null)
            {
                ReflectMap = GLTexture2D.FromBitmap(MPLibrary.Resources.Resource1.Reflect);

                ReflectMapSampler = new GLSamplerObject()
                {
                    TextureTarget = ReflectMap.Target,
                    ID = ReflectMap.ID,
                    Name = "ReflectMap",
                    WrapU = OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat,
                    WrapV = OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat,
                    MagFilter = OpenTK.Graphics.OpenGL.TextureMagFilter.Linear,
                    MinFilter = OpenTK.Graphics.OpenGL.TextureMinFilter.Linear,
                };
            }

            TextureCache.Clear();
            DrawListOPA.Clear();
            DrawListXLU.Clear();

            foreach (var tex in hsfFile.Textures)
            {
                var info = tex.TextureInfo;

                tex.RenderTexture = new GLGXTexture(
                    tex.Name, info.Width, info.Height, (uint)tex.GcnFormat, (uint)tex.GcnPaletteFormat, 1, tex.ImageData, tex.PaletteData);
                TextureCache.Add(tex.RenderTexture);
            }

            //load in hierarchy order
            void LoadNode(HSFObject node)
            {
                int index = hsfFile.ObjectNodes.IndexOf(node);

                var nodeName = node.Name;

                BoneViewMatrixArray[index] = Matrix4.Identity;
                BoneWorldMatrixArray[index] = Matrix4.Identity;
                BoneWorldMatrixArrayVisiblity[index] = CameraFrustum.Frustum.FULL;

                if (node.AnimatedLocalMatrix != null)
                {

                }

                if (node.MeshData != null) 
                {
                    var mesh = node.MeshData;
                    mesh.Init();

                    foreach (var gxMesh in mesh.GXMeshes)
                    {
                        var mat = hsfFile.Materials[gxMesh.Key];
                        mat.Init(hsfFile, node.Data);

                        gxMesh.Value.BoneIndex = index;

                        bool isOpaquePass = true;

                        if (mat.IsXLUPass())
                            isOpaquePass = false;

                        var sceneNode = new SceneNode(gxMesh.Value, mat, TextureCache, 0);
                        sceneNode.UINode.Header = mesh.Name;
                        //Light map highlight
                        sceneNode.TextureObjects.Add(LightMapSampler);
                        sceneNode.TextureObjects.Add(ReflectMapSampler);

                        sceneNode.OnSelected = (n) =>
                        {
                            OnSceneSelected(node, n);
                        };

                        HsfFile.ObjectNodes[index].Meshes.Add(sceneNode);

                        if (isOpaquePass)
                            DrawListOPA.Add(sceneNode);
                        else
                            DrawListXLU.Add(sceneNode);
                    }
                }

                foreach (var c in node.Children)
                    LoadNode(c);
            }

            foreach (var n in hsfFile.ObjectNodes.Where(x => x.Parent == null))
                LoadNode(n);
        }

        public void ReloadDrawList()
        {
            DrawListOPA.Clear();
            DrawListXLU.Clear();

            foreach (var node in HsfFile.ObjectNodes)
            {
                foreach (var sceneNode in node.Meshes)
                {
                    if (((Material)sceneNode.Material).IsXLUPass())
                        DrawListXLU.Add(sceneNode);
                    else
                        DrawListOPA.Add(sceneNode);
                }
            }
            GLContext.ActiveContext.UpdateViewport = true;
        }

        private void OnSceneSelected(HSFObject objNode, SceneNode node)
        {
            OnMeshSelected?.Invoke(objNode, node);
        }

        public void DrawColorPicking(GLContext context)
        {
            if (!CanSelect)
            {
                return;
            }

            PrepareRender(context, this.Transform.TransformMatrix);

            //Set the target picking color
            OpenTK.Vector4 pickingColor = OpenTK.Vector4.Zero;
            if (RenderGlobals.MeshPicking)
                pickingColor = context.ColorPicker.SetPickingColor(this);

            foreach (var node in DrawListOPA)
                node.RenderColorPicking(context, BoneWorldMatrixArray, BoneViewMatrixArray, pickingColor);

            foreach (var node in DrawListXLU)
                node.RenderColorPicking(context, BoneWorldMatrixArray, BoneViewMatrixArray, pickingColor);

            MegaState.SetGLDefaults();
        }

        public override void DrawModel(GLContext context, Pass pass)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            PrepareRender(context, this.Transform.TransformMatrix);
            PrepareDebugShading();

            foreach (var node in DrawListOPA)
            {
                if (BoneWorldMatrixArrayVisiblity[node.Mesh.BoneIndex] == CameraFrustum.Frustum.NONE)
                    continue;

                node.Render(context, IsSelected, BoneWorldMatrixArray, BoneViewMatrixArray);
            }

            foreach (var node in DrawListXLU)
            {
                if (BoneWorldMatrixArrayVisiblity[node.Mesh.BoneIndex] == CameraFrustum.Frustum.NONE)
                    continue;

                node.Render(context, IsSelected, BoneWorldMatrixArray, BoneViewMatrixArray);
            }

            for (int i = 0; i < SkeletonRenderer.Bones.Count; i++)
                SkeletonRenderer.Bones[i].BoneData.Transform = BoneWorldMatrixArray[i];

            SkeletonRenderer.DrawModel(context, pass);
        }

        private void PrepareDebugShading()
        {
            switch (DebugShaderRender.DebugRendering)
            {
                case DebugShaderRender.DebugRender.Normal:
                    RenderGlobals.DebugShadingMode = GCNRenderLibrary.GXDebugShading.Normals;
                    break;
                case DebugShaderRender.DebugRender.Diffuse:
                    RenderGlobals.DebugShadingMode = GCNRenderLibrary.GXDebugShading.Texture0;
                    break;
                case DebugShaderRender.DebugRender.VertexColors:
                    RenderGlobals.DebugShadingMode = GCNRenderLibrary.GXDebugShading.VertexColor;
                    break;
                case DebugShaderRender.DebugRender.Lighting:
                    RenderGlobals.DebugShadingMode = GCNRenderLibrary.GXDebugShading.RasterColor0;
                    break;
                case DebugShaderRender.DebugRender.Specular:
                    RenderGlobals.DebugShadingMode = GCNRenderLibrary.GXDebugShading.RasterColor1;
                    break;
                case DebugShaderRender.DebugRender.Tangents:
                    RenderGlobals.DebugShadingMode = GCNRenderLibrary.GXDebugShading.Tangent;
                    break;
                case DebugShaderRender.DebugRender.Bitangents:
                    RenderGlobals.DebugShadingMode = GCNRenderLibrary.GXDebugShading.Binormal;
                    break;
                default:
                    RenderGlobals.DebugShadingMode = GCNRenderLibrary.GXDebugShading.Default;
                    break;
            }
        }

        private void PrepareRender(GLContext context, Matrix4 modelMatrix)
        {
            if (BoneWorldMatrixArray.Length == 0)
                return;

            var modelVisible = CameraFrustum.Frustum.PARTIAL;

            if (modelVisible != CameraFrustum.Frustum.NONE)
            {
                //Prepare the matrices used for rendering.
                ExecNodeTreeOpList(context.Camera, modelMatrix, modelVisible);
                CalculateView(context.Camera);

                foreach (var mesh in HsfFile.Meshes)
                    HSFEnvelopeHandler.UpdateCPUSkinning(mesh, BoneWorldInverseMatrixArray);

                //Skinning
                /*     for (int i = 0; i < DrawListOPA.Count; i++)
                         DrawListOPA[i].UpdateDirectSkinning(BoneWorldInverseMatrixArray);
                     for (int i = 0; i < DrawListXLU.Count; i++)
                         DrawListXLU[i].UpdateDirectSkinning(BoneWorldInverseMatrixArray);*/
            }

            var LightObject = new LightObject();
            LightObject.Color = new System.Numerics.Vector4(1, 1, 1, 1);

            var pos = context.Camera.GetViewPostion();
            var dir = context.Camera.InverseRotationMatrix.Row2;
            //Light source attaches via camera.
            LightObject.Position = new System.Numerics.Vector3(pos.X, pos.Y, pos.Z);
            LightObject.Direction = new System.Numerics.Vector3(dir.X, dir.Y, dir.Z);
            //Todo how are these calculated?
            LightObject.CosAtten = new System.Numerics.Vector3(0, 0, 1);
            LightObject.DistAtten = new System.Numerics.Vector3(499999.50f, 0, -499998.50f);

            //For now use defaults from SetDefLight()
            LightObject.CosAtten = new System.Numerics.Vector3(1, 0, 0);
           // LightObject.DistAtten = new System.Numerics.Vector3(1, 0, 0);
            LightObject.SetSpot(20, GX.SpotFunction.COS);


            //   LightObject.SetDistAttn(1, 1f, GX.DistAttnFunction.OFF);


            //Player viewer settings

            LightObject.CosAtten = new System.Numerics.Vector3(1, 0, 0);
            LightObject.DistAtten = new System.Numerics.Vector3(1, 0, 0);
            LightObject.Position = new System.Numerics.Vector3(577350.25f, 577350.25f, 577350.25f);
            LightObject.Direction = new System.Numerics.Vector3(0.57735f, 0.57735f, 0.57735f);
            LightObject.Color = new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1);

            UpdateLight(LightObject);
        }

        SphereRender SphereDebug;

        private void ExecNodeTreeOpList(Camera camera, OpenTK.Matrix4 modelMatrix, CameraFrustum.Frustum rootVisiblity)
        {
            //Assign the model matrix in world space. This is used to transform models into the scene.
            BoneWorldMatrixArray[0] = modelMatrix;
            for (int i = 0; i < HsfFile.ObjectNodes.Count; i++)
            {
                var node = HsfFile.ObjectNodes[i];
                //Hack for nodes that get set during runtime. Those are filled with dummy data
                if (node.Data.ParentIndex < -1 || node.Data.ParentIndex > HsfFile.ObjectNodes.Count)
                    continue;

                BoneWorldMatrixArray[i] = GetWorldMatrix(node);

                BoneWorldInverseMatrixArray[i] = node.InvertedBindPose * BoneWorldMatrixArray[i];

                var bb = BoundingBox.FromMinMax(
                               new Vector3(node.Data.CullBoxMin.X, node.Data.CullBoxMin.Y, node.Data.CullBoxMin.Z),
                               new Vector3(node.Data.CullBoxMax.X, node.Data.CullBoxMax.Y, node.Data.CullBoxMax.Z));
                bb.UpdateTransform(BoneWorldMatrixArray[i]);
                BoneWorldMatrixArrayVisiblity[i] = camera.GetFustrumState(new BoundingNode() { Box = bb, });

                if (!node.IsVisible)
                    BoneWorldMatrixArrayVisiblity[i] = CameraFrustum.Frustum.NONE;

                if (DebugBoundingBoxDisplay || node.Data.Type == ObjectType.Mesh)
                {
                    if (HsfFile.ObjectNodes[i].Meshes.Any(x => x.IsSelected))
                    {
                        var mat = new StandardMaterial();
                        mat.Render(GLContext.ActiveContext);

                        BoundingBoxRender.Draw(GLContext.ActiveContext, bb);
                    }
                }
            }
        }

        private Matrix4 GetWorldMatrix(HSFObject node)
        {
            var boneMatrix = node.IsAnimated ? node.AnimatedLocalMatrix : node.LocalMatrix;

            if (node.Parent != null && node.Parent != node)
               return boneMatrix * GetWorldMatrix(node.Parent);
            return boneMatrix;
        }

        /// <summary>
        /// Updates the current bone matrix list into proper view space of the current camera.
        /// </summary>
        public void CalculateView(Camera camera)
        {
            for (int i = 0; i < HsfFile.ObjectNodes.Count; i++)
            {
                var nodeWorldMatrix = this.BoneWorldMatrixArray[i];
                /*    if (HsfFile.ObjectNodes[i].Envelopes.Any(x => x.VertexCount > 0))
                        BoneViewMatrixArray[i] = camera.ViewMatrix;
                    else
                        BoneViewMatrixArray[i] = nodeWorldMatrix * camera.ViewMatrix;
                    */
                BoneViewMatrixArray[i] = camera.ViewMatrix;

                //Billboarding
                if ((HsfFile.ObjectNodes[i].Data.RenderFlags & HsfGlobals.BILLBOARD) != 0)
                {
                    BoneViewMatrixArray[i] =  CalculateBillboardMatrix(BoneViewMatrixArray[i]);
                }
            }
        }

        private Matrix4 CalculateBillboardMatrix(Matrix4 matrix)
        {
            //Todo add options to limit via axis
            return matrix.ClearRotation();
        }
    }
}
