using GCNRenderLibrary;
using GCNRenderLibrary.Rendering;
using System;
using System.Linq;
using System.Numerics;

namespace MPLibrary.GCN
{
    public partial class Material : GXMaterial
    {
        //Flags used for determining how to display I8/I4 grayscale texture maps.
        //These typically use a tint color.
        private GrayscaleTexBlendFlags GrayscaleFlags = GrayscaleTexBlendFlags.None;

        private RGBA TextureTintColor = new RGBA(255, 255, 255, 255);

        private RGBA TextureTintColor1 = new RGBA(255, 255, 255, 255);
        private RGBA TextureTintColor2 = new RGBA(255, 255, 255, 255);

        private int kColorIdx = 0;

        public float InvertedTransparency;
        public float ReflectionIntensity;

        enum GrayscaleTexBlendFlags
        {
            None,
            GrayscaleColorFirstTex,
            GrayscaleColorSecondTex,
        }

        /// <summary>
        /// Determines if the current material will draw during opaque or xlu pass.
        /// </summary>
        /// <returns></returns>
        public bool IsXLUPass()
        {
            //Combined render + material flags
            var material_flags = this.RenderFlags | this.MaterialData.MaterialFlags;
            //Alpha testing
            var alpha_flags = material_flags & HsfGlobals.PUNCHTHROUGH_ALPHA_BITS;
            //Depth writing pass check
            var pass_flags = this.MaterialData.AltFlags & HsfGlobals.PASS_BITS;
            //Uses blending or not
            bool hasBlending = (material_flags & HsfGlobals.BLEND_MODE_MASK) != 0;
            //Has translucent stage
            bool hasTranslucent = InvertedTransparency != 0;
            //Check for XLU usage
            if (hasBlending || pass_flags != 0 || alpha_flags != 0 || hasTranslucent)
                return true;
            return false;
        }

        /// <summary>
        /// Reloads and prepares the blending state based on material and render flags.
        /// </summary>
        public void ReloadBlend()
        {
            //Combine flags as render flags (by node) or material flags can be used
            var flags = this.RenderFlags | this.MaterialData.MaterialFlags;

            //Default blend mode
            SetBlend(GX.BlendMode.BLEND, GX.BlendFactor.SRCALPHA, GX.BlendFactor.INVSRCALPHA);

            if ((flags & HsfGlobals.BLEND_MODE_MASK) == 0) //Blend modes
                SetBlend(GX.BlendMode.BLEND, GX.BlendFactor.SRCALPHA, GX.BlendFactor.INVSRCALPHA);
            else if ((flags & HsfGlobals.BLEND_SRCALPHA_ONE) == 0) //Blend modes
                SetBlend(GX.BlendMode.BLEND, GX.BlendFactor.ZERO, GX.BlendFactor.INVSRCCLR);
            else
                SetBlend(GX.BlendMode.BLEND, GX.BlendFactor.SRCALPHA, GX.BlendFactor.ONE);
        }

        /// <summary>
        /// Reloads the current texture maps.
        /// </summary>
        public void ReloadTextures()
        {
            GX.WrapMode GetWrap(WrapMode wrap)
            {
                if (wrap == WrapMode.Clamp)
                    return GX.WrapMode.CLAMP;
                else if (wrap == WrapMode.Repeat)
                    return GX.WrapMode.REPEAT;
                else
                    return GX.WrapMode.MIRROR;
            }

            var textureMaps = TextureAttributes.Where(x => x != null).ToList();
            textureMaps = textureMaps.OrderBy(x => x.AttributeData.NbtEnable == 0.0f).ToList();

            for (int i = 0; i < textureMaps.Count; i++)
            {
                var textureMap = textureMaps[i].AttributeData;
                var tranform = textureMap.TexAnimStart;

                //Textures map via index. Find the current index of the file
                var textureIndex = this.HsfFile.Textures.IndexOf(textureMaps[i].Texture);

                this.Textures[i] = new GXSampler();
                this.Textures[i].TextureIndex = textureIndex;
                if (textureIndex != -1)
                    this.Textures[i].Texture = HsfFile.Textures[textureIndex].Name;
                this.Textures[i].MinFilter = GX.TextureFilter.Linear;
                this.Textures[i].MagFilter = GX.TextureFilter.Linear;
                this.Textures[i].WrapX = GetWrap(textureMap.WrapS);
                this.Textures[i].WrapY = GetWrap(textureMap.WrapT);
            }
            GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
        }

        /// <summary>
        /// Reloads the lighting channels with tev stages.
        /// </summary>
        public void ReloadLightingChannels(bool updateShaders = false)
        {
            ReloadTevStages();

            if (updateShaders)
                this.RenderScene?.ReloadShader();
        }

        /// <summary>
        /// Reloads the current colors used for lighting channels.
        /// </summary>
        public void ReloadColors()
        {
            //Prepare the colors. 
            this.AmbientColor[0] = new RGBA(
                MaterialData.AmbientColor.R,
                MaterialData.AmbientColor.G,
                MaterialData.AmbientColor.B, 255);
            this.MaterialColor[0] = new RGBA(
                MaterialData.MaterialColor.R,
                MaterialData.MaterialColor.G,
                MaterialData.MaterialColor.B, 255);

            //Default colors for amb/mat 1
            this.MaterialColor[1] = new RGBA(255, 255, 255, 255);
            this.AmbientColor[1] = new RGBA(0, 0, 0, 0);

            InvertedTransparency = this.MaterialData.TransparencyInverted;
            ReflectionIntensity = this.MaterialData.ReflectionIntensity;

            GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
        }

        public void ReloadTransparency(float frame = 0)
        {
            var transparency = this.MaterialData.TransparencyInverted;
            var track = AnimationData.FindByEffect(TrackEffect.Transparency);
            if (track != null)
                transparency = track.GetFrameValue(frame);

            InvertedTransparency = transparency;

            //TevColor used for alpha
            this.TevColors[1].A = (byte)((1 - transparency) * 255);
            this.TevColors[2].A = (byte)((1 - transparency) * 255);

            ReloadTevStages();
        }

        /// <summary>
        /// Reloads the polygon, depth and blending states.
        /// </summary>
        public void ReloadPolygonState()
        {
            var material_flags = RenderFlags | MaterialData.MaterialFlags;

            var pass_flags = MaterialData.AltFlags & HsfGlobals.PASS_BITS;
            var alpha_flags = material_flags & HsfGlobals.PUNCHTHROUGH_ALPHA_BITS;

            if ((RenderFlags & HsfGlobals.CULL_FRONTFACES) == 0)
            {
                if ((RenderFlags & HsfGlobals.DONT_CULL_BACKFACES) == 0)
                    this.CullMode = GX.CullMode.BACK;
                else
                    this.CullMode = GX.CullMode.NONE;
            }
            else
                this.CullMode = GX.CullMode.FRONT;

            if (pass_flags != 0 || InvertedTransparency != 0 ||
                ((RenderFlags & HsfGlobals.BLEND_ZERO_INVSRCCLR) != 0)) //Todo shadows use this, unsure what determines when shadows are used
            {
                this.SetZMode(true, GX.CompareType.LEQUAL, false);
            }
            else
            {
                this.SetZMode(true, GX.CompareType.LEQUAL, true);
            }


            if (alpha_flags != 0) // >= 0.5
            {
                this.SetZMode(true, GX.CompareType.LEQUAL, true);
                this.SetAlphaCompare(GX.CompareType.GEQUAL, 0.5f, GX.AlphaOp.OR, GX.CompareType.GEQUAL, 0.5f);
            }
            else
            {
                this.SetAlphaCompare(GX.CompareType.ALWAYS, 1, GX.AlphaOp.OR, GX.CompareType.ALWAYS, 1);
            }
            this.RenderScene?.ReloadMegaStage();
            GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
        }

        /// <summary>
        /// Prepares the tev stages and lighting channels with no textures present in the material.
        /// </summary>
        public void SetTevStageNoTexture()
        {
            int stageID = 1;
            SetNumTevStages(8);

            //Combine flags as render flags (by node) or material flags can be used
            var flags = this.RenderFlags | this.MaterialData.MaterialFlags;

            bool useLights = false;
            bool hasLightingChannel1 = false;
            uint lightBit = 0;
            int texCoordID = 0;

            bool NoToonTexture      = (flags & HsfGlobals.TOON_ENABLE) == 0;
            bool NoHighlightTexture = (flags & HsfGlobals.HIGHLIGHT_ENABLE) == 0;
            int highlight_no = 0;
            int reflectMap_no = 1;
            int toonMap_no = 2;

            //Raster outputs. Some configurations output alpha and some do not
            bool alphaLighting = false;
            var tevAlphaD = GX.CA.KONST;
            GX.ChannelID rasterOutput = GX.ChannelID.COLOR0;

            var tevColor = new RGBA(255, 255, 255, (byte)((1 - InvertedTransparency) * 255));
            GXSetTevColor(1, tevColor);

            if (this.MaterialData.VertexMode == LightingChannelFlags.LightingSpecular ||
                this.MaterialData.VertexMode == LightingChannelFlags.LightingSpecular2)
            {
                hasLightingChannel1 = true;
                useLights = true;
            }
            else
            {
                hasLightingChannel1 = false;

                //Check for when to use lights or not
                if (this.MaterialData.VertexMode == LightingChannelFlags.NoLighting ||
                    this.MaterialData.VertexMode == LightingChannelFlags.VertexColorsWithAlpha)
                    useLights = false;
                else
                    useLights = true;
            }

            //Vertex alpha support
            if (this.MaterialData.VertexMode == LightingChannelFlags.VertexColorsWithAlpha)
            {
                rasterOutput = GX.ChannelID.COLOR0A0;
                tevAlphaD = GX.CA.RASA;
                alphaLighting = false; //In the code this is true, but in game alpha has no lighting?
            }

            if (NoToonTexture) //Default primary stage
            {
                this.GXSetTevColor(1, tevColor);
                this.GXSetTevOrder(0, GX.TexCoordID.TEXCOORD_NULL, GX.TexMapID.TEXMAP_NULL, rasterOutput);
                this.SetTevColorIn(0, GX.CC.ZERO, GX.CC.ZERO, GX.CC.ZERO, GX.CC.RASC);
                this.SetTevColorOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);

                this.SetTevAlphaIn(0, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO, tevAlphaD);
                this.SetTevAlphaOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);
            }
            else //Toon map Unsure if this is used
            {
                //Uses material color
                RGBA toonColor = new RGBA(this.MaterialColor[0].R, this.MaterialColor[0].G, this.MaterialColor[0].B, tevColor.A);
                GXSetTevColor(1, toonColor);

                SetTexCoordGen2(0, GX.TexGenType.SRTG, GX.TexGenSrc.COLOR0,GX.TexGenMatrix.IDENTITY, false, GX.PostTexGenMatrix.PTIDENTITY);

                GXSetTevOrder(0, GX.TexCoordID.TEXCOORD0, (GX.TexMapID)toonMap_no, GX.RasColorChannelID.COLOR0A0);
                //Mix toon map and tev color
                SetTevColorIn(0, GX.CC.ZERO, GX.CC.TEXC, GX.CC.C0, GX.CC.ZERO);
                SetTevColorOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);
                //tev konstant color for alpha
                SetTevAlphaIn(0, GX.CA.KONST, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO);
                SetTevAlphaOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);
            }

            if (ReflectionIntensity != 0.0f)
            {
                SetReflectStage(ref texCoordID, ref stageID, reflectMap_no);
            }

            //Shadow stage would go here but will skip for now

            if (hasLightingChannel1) //2 color/alpha light channels
            {
                if (NoHighlightTexture) //No lightmap
                    SetSpecularStage(ref texCoordID, ref stageID, GX.TexMapID.TEXMAP_NULL);
                else //Has lightmap.
                    SetupLightmapStage(ref texCoordID, ref stageID, highlight_no, false);
            }
            else if (InvertedTransparency != 0.0f) //Add another stage with A0 used for transparency output
                SetupTransparencyStage(ref stageID, true);

            var numStages = stageID;

            var stages = new TevStage[numStages];
            for (int i = 0; i < numStages; i++)
                stages[i] = this.Stages[i];

            this.Stages = stages.ToArray();

            SetColorChannels(hasLightingChannel1, useLights, alphaLighting, lightBit);
        }

        /// <summary>
        /// Prepares the tev stages and lighting channels with textures present in the material.
        /// </summary>
        public void SetTevStageTexture()
        {
            SetNumTevStages(16);

            //Combine flags as render flags (by node) or material flags can be used
            var flags = this.RenderFlags | this.MaterialData.MaterialFlags;

            bool useLights = false;
            bool hasLightingChannel1 = false;
            uint lightBit = 0;
            int stageID = 0;
            int texCoordID = 0;

            //Raster outputs. Some configurations output alpha and some do not
            bool alphaLighting = false;
            var tevAlphaD = GX.CA.KONST;
            GX.ChannelID rasterOutput = GX.ChannelID.COLOR0;

            if (this.MaterialData.VertexMode == LightingChannelFlags.LightingSpecular ||
                this.MaterialData.VertexMode == LightingChannelFlags.LightingSpecular2)
            {
                hasLightingChannel1 = true;
                useLights = true;
            }
            else
            {
                hasLightingChannel1 = false;

                //Check for when to use lights or not
                if (this.MaterialData.VertexMode == LightingChannelFlags.NoLighting ||
                    this.MaterialData.VertexMode == LightingChannelFlags.VertexColorsWithAlpha)
                    useLights = false;
                else
                    useLights = true;
            }

            //Vertex alpha support
            if (this.MaterialData.VertexMode == LightingChannelFlags.VertexColorsWithAlpha)
            {
                rasterOutput = GX.ChannelID.COLOR0A0;
                tevAlphaD = GX.CA.RASA;
                alphaLighting = false; //In the code this is true, but in game alpha has no lighting?
            }

            if (this.TextureAttributes.Count == 1)
                SetTexture0(ref stageID, ref texCoordID, hasLightingChannel1, alphaLighting, tevAlphaD, rasterOutput);
            else
                SetTextureList(ref stageID, ref texCoordID, hasLightingChannel1, alphaLighting, tevAlphaD, rasterOutput);

            //player viewer has extra stage for increasing brightness?
     /*       bool is_viewer_test = true;

            if (is_viewer_test)
            {
                SetTevColorIn(stageID, GX.CC.ZERO, GX.CC.ZERO, GX.CC.ZERO, GX.CC.CPREV);
                SetTevColorOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);
                SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO, GX.CA.APREV);
                SetTevAlphaOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, false, GX.Register.PREV);
                stageID++;
            }*/

            int numStages = stageID;

            var stages = new TevStage[numStages];
            for (int i = 0; i < numStages; i++)
                stages[i] = this.Stages[i];

            this.Stages = stages.ToArray();

            SetColorChannels(hasLightingChannel1, useLights, alphaLighting, lightBit);
        }

        private void SetTexture0(ref int stageID, ref int texCoordID, bool hasLightingChannel1, bool alphaLighting, GX.CA tevAlphaD, GX.ChannelID rasterOutput)
        {
            RGBA texCol = new RGBA(255, 255, 255, 255);
            RGBA firstTex = new RGBA(255, 255, 255, 255);
            RGBA secondTex = new RGBA(255, 255, 255, 255);
            var tevColor = new RGBA(255, 255, 255, (byte)((1 - InvertedTransparency) * 255));
            GXSetTevColor(1, tevColor);

            //Combine flags as render flags (by node) or material flags can be used
            var flags = this.RenderFlags | this.MaterialData.MaterialFlags;

            bool NoToonTexture = (flags & HsfGlobals.TOON_ENABLE) == 0;
            bool NoHighlightTexture = (flags & HsfGlobals.HIGHLIGHT_ENABLE) == 0;

            int highlight_no = 1;
            int reflectMap_no = 2;
            int toonMap_no = 3;

            var tex = TextureAttributes[0].AttributeData;

            //Defaults
            SetTevColorIn(0, GX.CC.ZERO, GX.CC.ZERO, GX.CC.ZERO, GX.CC.RASC);
            SetTevColorOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);

            SetTevAlphaIn(0, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO, GX.CA.RASA);
            SetTevColorOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);

            if (tex.TextureEnable == 1.0f)
            {
                UpdateTextureMatrix(0, texCoordID);

                //Animation code would go here but skip as this isn't ran per frame

                //Blends previous and next stages with texture alpha
                if (tex.BlendingFlag == CombinerBlend.TransparencyMix)
                {
                    GXSetTevOrder(1, GX.TexCoordID.TEXCOORD0, GX.TexMapID.TEXMAP1, GX.ChannelID.COLOR0A0);
                    //Mix previous previous stage and texture target with texture alpha
                    SetTevColorIn(1, GX.CC.CPREV, GX.CC.TEXC, GX.CC.TEXA, GX.CC.ZERO);
                    SetTevColorOp(1, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);

                    SetTevAlphaIn(1, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO, GX.CA.KONST);
                    SetTevAlphaOp(1, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, false, GX.Register.PREV);
                    stageID = 2;
                }
                else
                {
                    if (NoToonTexture)
                    {
                        int type = 1; //Unsure what controls this atm
                        if (type == 0)
                        {
                            SetKColorRGB(0, texCol);

                            SetTevColorIn(0, GX.CC.ZERO, GX.CC.TEXC, GX.CC.KONST, GX.CC.ZERO);
                            SetTevColorOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);

                            SetTevAlphaIn(0, GX.CA.ZERO, GX.CA.TEXA, GX.CA.KONST, GX.CA.ZERO);
                            SetTevAlphaOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, false, GX.Register.PREV);

                            GXSetTevOrder(1, GX.TexCoordID.TEXCOORD_NULL, GX.TexMapID.TEXMAP_NULL, rasterOutput);
                            SetTevColorIn(1, GX.CC.ZERO, GX.CC.CPREV, GX.CC.RASC, GX.CC.ZERO);
                            SetTevColorOp(1, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);

                            SetTevAlphaIn(1, GX.CA.ZERO, GX.CA.APREV, tevAlphaD, GX.CA.ZERO);
                            SetTevAlphaOp(1, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);
                            stageID = 2;
                        }
                        else if (type == 2)
                        {
                            //Tex swap channel

                            // 0/1 swaps
                            SetTexSwapChannel(0, GX.TevColorChan.R, GX.TevColorChan.G, GX.TevColorChan.B, GX.TevColorChan.A);
                            SetRasSwapChannel(0, GX.TevColorChan.R, GX.TevColorChan.A, GX.TevColorChan.A, GX.TevColorChan.A);

                            SetKColorRGB(0, firstTex);
                            SetTevColorIn(0, GX.CC.ZERO, GX.CC.TEXC, GX.CC.KONST, GX.CC.ZERO);
                            SetTevColorOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, false, GX.Register.PREV);
                            GXSetTevOrder(0, GX.TexCoordID.TEXCOORD0, GX.TexMapID.TEXMAP0, GX.ChannelID.COLOR_NULL);

                            // 0/2 swaps
                            SetTexSwapChannel(1, GX.TevColorChan.R, GX.TevColorChan.G, GX.TevColorChan.B, GX.TevColorChan.A);
                            SetRasSwapChannel(1, GX.TevColorChan.B, GX.TevColorChan.B, GX.TevColorChan.B, GX.TevColorChan.A);

                            SetKColorRGB(1, secondTex);
                            SetTevColorIn(0, GX.CC.ZERO, GX.CC.TEXC, GX.CC.KONST, GX.CC.CPREV);
                            SetTevAlphaIn(0, GX.CA.ZERO, GX.CA.KONST, GX.CA.TEXA, GX.CA.ZERO);

                            SetTevColorOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);
                            SetTevAlphaOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);

                            //Todo how is temap configured?
                            GXSetTevOrder(0, GX.TexCoordID.TEXCOORD0, GX.TexMapID.TEXMAP0 + 1, GX.ChannelID.COLOR_NULL);
                            stageID = 2;
                        }
                        else
                        {
                            SetTevColorIn(0, GX.CC.ZERO, GX.CC.TEXC, GX.CC.RASC, GX.CC.ZERO);
                            SetTevColorOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);
                            SetTevAlphaIn(0, GX.CA.ZERO, GX.CA.TEXA, tevAlphaD, GX.CA.ZERO);
                            SetTevAlphaOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, false, GX.Register.PREV);

                            stageID++;
                        }
                    }
                    else
                    {
                        SetTevColorIn(0, GX.CC.ZERO, GX.CC.TEXC, GX.CC.ONE, GX.CC.ZERO);
                        SetTevColorOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);
                        SetTevAlphaIn(0, GX.CA.ZERO, GX.CA.TEXA, GX.CA.KONST, GX.CA.ZERO);
                        SetTevAlphaOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, false, GX.Register.PREV);

                        stageID++;
                    }
                }
            }
            else
            {
                //Tex op 0 4 WTF?
                //Stages[0].colorStage.Op = GX.TevOp.COMP_R8_GT;

                stageID++;
            }

            texCoordID++;

            if (!NoToonTexture)
            {

            }

            if (ReflectionIntensity != 0)
            {
                SetReflectStage(ref texCoordID, ref stageID, reflectMap_no);
            }

            //Shadow stage would go here but will skip for now

            //Add lighting channel stages
            if (hasLightingChannel1)
            {
                if (NoToonTexture && NoHighlightTexture) //Map is specular texture
                    SetSpecularStage(ref texCoordID, ref stageID, tex.TextureEnable == 1.0 ? GX.TexMapID.TEXMAP_NULL : GX.TexMapID.TEXMAP0);
                else //Lightmap
                    SetupLightmapStage(ref texCoordID, ref stageID, highlight_no, true);
            }
            else if (InvertedTransparency != 0.0f) //Add another stage with A0 used for transparency output
                SetupTransparencyStage(ref stageID, false);
        }

        public void UpdateLightmapMatrix()
        {
            //50.0f == 1.0f scale
            float highlightScale = 6.0f * (this.MaterialData.HiliteScale / 300.0f);
            if (highlightScale < 0.1f)
                highlightScale = 0.1f;

            this.TextureMatrices[7].MappingMethod = TextureMapMode.TextureCoordinates;
            this.TextureMatrices[7].MatrixMode = MatrixMode.Max;
            //Center the light map
            this.TextureMatrices[7].Position = new System.Numerics.Vector2(-0.5f, 0.5f);
            //Multply by 6 as the scale seems too small
            this.TextureMatrices[7].Scale = new System.Numerics.Vector2(highlightScale, highlightScale) * 5;

            GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
        }

        public void UpdateTextureMatrix(int index, int texCoordID)
        {
            var tex = this.TextureAttributes[index].AttributeData;

            //identity matrix check. Skip for now as this isn't ran per frame and animations adjust SRT values elsewhere
            if (tex.TexAnimStart.Scale.X == 1.0f && tex.TexAnimStart.Scale.Y == 1.0f &&
                tex.TexAnimStart.Position.X == 0.0f && tex.TexAnimStart.Position.Y == 0.0f && false)
            {
                {
                    this.SetTexCoordGen2(texCoordID, GX.TexGenType.MTX2x4, GX.TexGenSrc.TEX0, GX.TexGenMatrix.IDENTITY, false, GX.PostTexGenMatrix.PTIDENTITY);
                }
            }
            else
            {
                //Todo confirm as original code has negative position values. Looks fine as it is currently.
                this.TextureMatrices[texCoordID].Position = new System.Numerics.Vector2(
                    tex.TexAnimStart.Position.X,
                    tex.TexAnimStart.Position.Y);
                this.TextureMatrices[texCoordID].Scale = new System.Numerics.Vector2(
                    1.0f / tex.TexAnimStart.Scale.X,
                    1.0f / tex.TexAnimStart.Scale.Y);
                this.SetTexCoordGen2(texCoordID, GX.TexGenType.MTX2x4, GX.TexGenSrc.TEX0, GX.TexGenMatrix.TEXMTX0 + texCoordID * 3, false, GX.PostTexGenMatrix.PTIDENTITY);
            }
        }

        private void SetTextureList(ref int stageID, ref int texCoordID, bool hasLightingChannel1, bool alphaLighting, GX.CA tevAlphaD, GX.ChannelID rasterOutput)
        {
            var tevColor = new RGBA(255, 255, 255, (byte)((1 - InvertedTransparency) * 255));
            GXSetTevColor(1, tevColor);

            //For now disable bump map display as these are not supported yet
            bool displayBumpMaps = false;

            //Combine flags as render flags (by node) or material flags can be used
            var flags = this.RenderFlags | this.MaterialData.MaterialFlags;

            bool NoToonTexture = true; //Todo. Not sure what enables using this
            bool NoHighlightTexture = (flags & HsfGlobals.HIGHLIGHT_ENABLE) == 0;

            int highlight_no = this.TextureAttributes.Count;
            int reflectMap_no = this.TextureAttributes.Count + 1;
            int toonMap_no = this.TextureAttributes.Count + 2;

            GX.TexMapID specularTexture = GX.TexMapID.TEXMAP_NULL;

            bool hasBumpMap = false;
            int bumpTextureID = -1;
            int bumpStageID = 0;

            this.Stages[0].KonstColorSel = GX.KonstColorSel.KCSEL_K0;

            var sorted = TextureAttributes.OrderBy(x => x.AttributeData.NbtEnable == 0.0f).ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var tex = sorted[i].AttributeData;
                if (tex.NbtEnable == 0)
                {
                    if (tex.TextureEnable == 1.0f)
                    {
                        UpdateTextureMatrix(i, texCoordID);
                        GXSetTevOrder(stageID, GX.TexCoordID.TEXCOORD0 + texCoordID, GX.TexMapID.TEXMAP0 + i, GX.ChannelID.COLOR0A0);

                        //Is first texture
                        if (i == 0)
                        {
                            switch (GrayscaleFlags)
                            {
                                //Blending using the tint color of grayscale textures
                                case GrayscaleTexBlendFlags.GrayscaleColorFirstTex:
                                    SetupGrayscaleTexStageFirstTex1(ref stageID, 0, rasterOutput);
                                    break;
                                //Blending using the tint color of 2 grayscale textures
                                case GrayscaleTexBlendFlags.GrayscaleColorSecondTex:
                                    SetupGrayscaleTexStageFirstTex2(ref stageID, 0, rasterOutput);
                                    break;
                                default: //Default with texture and raster color
                                    SetTevColorIn(stageID, GX.CC.ZERO, GX.CC.TEXC, GX.CC.RASC, GX.CC.ZERO);
                                    SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.TEXA, tevAlphaD, GX.CA.ZERO);
                                    break;
                            }
                        }
                        else if (!hasBumpMap)
                        {
                            //Blending with a transparent alpha channel or blend value
                            if (tex.BlendingFlag == CombinerBlend.TransparencyMix)
                            { 
                                SetupBlendingAlphaCombinerStage(ref stageID, ref texCoordID, i, tex);
                            } //Blending using the tint color of grayscale textures
                            else if (GrayscaleFlags == GrayscaleTexBlendFlags.GrayscaleColorFirstTex)
                            { 
                                SetupGrayscaleTexStage1(ref stageID, tex, i, rasterOutput);
                            } //Blending using the tint color of 2 grayscale textures
                            else if (GrayscaleFlags == GrayscaleTexBlendFlags.GrayscaleColorSecondTex)
                            {
                                SetupGrayscaleTexStage2(ref stageID, i, rasterOutput);
                            }
                            else //Default combiner stage
                            {
                                byte blend = (byte)(tex.BlendTextureAlpha * 255.0f);
                                SetKColor(stageID, blend);

                                SetTevColorIn(stageID, GX.CC.CPREV, GX.CC.TEXC, GX.CC.KONST, GX.CC.ZERO); //0 8 0xe 0xf
                                SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.TEXA, GX.CA.APREV, GX.CA.ZERO); //7 4 0 7
                            }
                        }
                        else
                        {
                            byte blend = (byte)(tex.BlendTextureAlpha * 255.0f);
                            SetKColor(stageID, blend);

                            SetTevColorIn(stageID, GX.CC.ZERO, GX.CC.CPREV, GX.CC.TEXC, GX.CC.ZERO); //0xf 0 8 0xf
                            SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO, GX.CA.TEXA); //7 7 7 4
                            hasBumpMap = false;
                        }

                        SetTevColorOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV); //0 0 0 1 0
                        SetTevAlphaOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV); //0 0 0 1 0

                        stageID++;
                        texCoordID++;
                    }
                }
                else //Bump mapping
                {
                    SetupBumpMappingStage(ref stageID, ref texCoordID, i, tex);
                    hasBumpMap = true;
                    bumpStageID = stageID;
                    bumpTextureID = i;

                    stageID++;
                    texCoordID++;
                }
            }


            if (!NoToonTexture)
            {

            }

            if (ReflectionIntensity != 0)
            {
                if (specularTexture == GX.TexMapID.TEXMAP_NULL)
                {
                    SetReflectStage(ref texCoordID, ref stageID, reflectMap_no);
                }
                else
                {
                    SetReflectStage(ref texCoordID, ref stageID, reflectMap_no);
                }
            }

            if (bumpTextureID != -1)
            {
                SetTexCoordGen2(texCoordID, GX.TexGenType.BUMP0, GX.TexGenSrc.TEXCOORD0, GX.TexGenMatrix.IDENTITY, false, GX.PostTexGenMatrix.PTIDENTITY);
                GXSetTevOrder(bumpStageID, GX.TexCoordID.TEXCOORD0 + texCoordID, GX.TexMapID.TEXMAP0 + bumpTextureID, GX.ChannelID.COLOR0A0);
            }

            //Shadow stage would go here but will skip for now

            //Add lighting channel stages
            if (hasLightingChannel1)
            {
                if (NoToonTexture && NoHighlightTexture)
                    SetSpecularStage(ref texCoordID, ref stageID, specularTexture);
                else //Lightmap
                    SetupLightmapStage(ref texCoordID, ref stageID, highlight_no, true);
            }
            else if (InvertedTransparency != 0.0f) //Add another stage with A0 used for transparency output
                SetupTransparencyStage(ref stageID, false);

            if (stageID == 0)
                throw new Exception("Failed to generate stage data!");
        }

        public void SetKColorRGB(int stageID, RGBA color)
        {
            //Colors must be divisable by 3
            if (kColorIdx % 3 != 0)
            {
                kColorIdx = (kColorIdx / 3 + 1) * 3;
            }

            TevKonstColors[kColorIdx / 3] = color;
            this.SetTevKColorSel(stageID, GX.KonstColorSel.KCSEL_K0 + kColorIdx / 3);

            kColorIdx++;
            if (kColorIdx > 0xb)
                kColorIdx = 0xb;
        }

        public void SetKColor(int stageID, byte color)
        {
            var id = kColorIdx % 3;
            if (id != 1)
            {

            }

            this.Stages[stageID].KonstColorSel = GX.KonstColorSel.KCSEL_K0_R + kColorIdx * 4;
            if (this.Stages[stageID].KonstColorSel.ToString().EndsWith("_R"))
                TevKonstColors[kColorIdx / 3].R = color;
            if (this.Stages[stageID].KonstColorSel.ToString().EndsWith("_G"))
                TevKonstColors[kColorIdx / 3].G = color;
            if (this.Stages[stageID].KonstColorSel.ToString().EndsWith("_B"))
                TevKonstColors[kColorIdx / 3].B = color;
            if (this.Stages[stageID].KonstColorSel.ToString().EndsWith("_A"))
                TevKonstColors[kColorIdx / 3].A = color;

            kColorIdx++;
            if (kColorIdx > 0xb)
                kColorIdx = 0xb;
        }

        //Lightmap highlight texture mapped via model normals
        private void SetupLightmapStage(ref int texCoordID, ref int stageID, int highlight_no, bool hasTextures)
        {
            //Map using vertex normals
            this.SetTexCoordGen2(texCoordID, GX.TexGenType.MTX2x4, GX.TexGenSrc.NRM, GX.TexGenMatrix.TEXMTX7, false, GX.PostTexGenMatrix.PTIDENTITY);

            this.HasPostTexMtx = true;

            //Use tex mtx 0 so normals transform to world space
            this.TexGens[texCoordID].Matrix = GX.TexGenMatrix.TEXMTX0;
            //Tex matrix 7 for 8th matrix
            this.TexGens[texCoordID].PostMatrix = GX.PostTexGenMatrix.PTTEXMTX7;

            //Assign light map ID.
            GXSetTevOrder(stageID, (GX.TexCoordID)texCoordID, (GX.TexMapID)highlight_no, GX.ChannelID.COLOR0A0);
            //Mix light map, one, and previous output
            if (hasTextures)
                SetTevColorIn(stageID, GX.CC.ZERO, GX.CC.TEXC, GX.CC.ONE, GX.CC.CPREV);
            else
                SetTevColorIn(stageID, GX.CC.ZERO, GX.CC.ONE, GX.CC.TEXC, GX.CC.CPREV); //0xf 0xc 8 0

            SetTevColorOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);
            SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.APREV, GX.CA.A0, GX.CA.ZERO);
            SetTevAlphaOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, false, GX.Register.PREV);

            //Update texture matrix (7)
            UpdateLightmapMatrix();

            texCoordID++;
            stageID++;
        }

        //Specular intput using the second color//alpha lighting channels
        private void SetSpecularStage(ref int texCoordID, ref int stageID, GX.TexMapID texMap)
        {
            //Check for a target specular map. If none is present, just use the raster color from color 1
            if (texMap != GX.TexMapID.TEXMAP_NULL)
            {
                this.SetTexCoordGen2(0, GX.TexGenType.MTX2x4, GX.TexGenSrc.TEX0, GX.TexGenMatrix.IDENTITY, false, GX.PostTexGenMatrix.PTIDENTITY);
                GXSetTevOrder(stageID, GX.TexCoordID.TEXCOORD0 + 0, texMap, GX.ChannelID.COLOR1A1);

                texCoordID++;

                SetTevColorIn(stageID, GX.CC.ZERO, GX.CC.TEXC, GX.CC.RASC, GX.CC.CPREV);
                SetTevColorOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);

                SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.APREV, GX.CA.A0, GX.CA.ZERO);
                SetTevAlphaOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);
            }
            else
            {
                //No texture maps to assign
                GXSetTevOrder(stageID, GX.TexCoordID.TEXCOORD_NULL, GX.TexMapID.TEXMAP_NULL, GX.ChannelID.COLOR1A1);
                //Mix previous, one and raster color
                SetTevColorIn(stageID, GX.CC.CPREV, GX.CC.ONE, GX.CC.RASC, GX.CC.ZERO);
                SetTevColorOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);

                SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.APREV, GX.CA.A0, GX.CA.ZERO);
                SetTevAlphaOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);
            }
            stageID++;
        }

        //Reflection texture mapped to model normals blended by intensity.
        private void SetReflectStage(ref int texCoordID, ref int stageID, int reflectMapNo)
        {
            var amount = (byte)(ReflectionIntensity * 255);

            this.SetKColor(stageID, amount);

            //Tex matrix 8 for reflection map
            this.SetTexCoordGen2(texCoordID, GX.TexGenType.MTX2x4, GX.TexGenSrc.NRM, GX.TexGenMatrix.TEXMTX8, false, GX.PostTexGenMatrix.PTIDENTITY);

            this.HasPostTexMtx = true;

            //Use tex mtx 0 so normals transform to world space
            this.TexGens[texCoordID].Matrix = GX.TexGenMatrix.TEXMTX0;
            //Tex matrix 8 for 9th matrix
            this.TexGens[texCoordID].PostMatrix = GX.PostTexGenMatrix.PTTEXMTX8;

            //Assign light map ID.
            GXSetTevOrder(stageID, (GX.TexCoordID)texCoordID, (GX.TexMapID)reflectMapNo, GX.ChannelID.COLOR0A0);
            //Mix previous, reflection map, and reflection color
            SetTevColorIn(stageID, GX.CC.CPREV, GX.CC.TEXC, GX.CC.KONST, GX.CC.ZERO);
            SetTevColorOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);

            SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO, GX.CA.APREV);
            SetTevAlphaOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, false, GX.Register.PREV);

            //Update texture matrix (8)

            /*  this.TextureMatrices[8].MappingMethod = TextureMapMode.TextureCoordinates;
              this.TextureMatrices[8].MatrixMode = MatrixMode.Max;
              //Center
              this.TextureMatrices[8].Position = new System.Numerics.Vector2(-0.5f, 0.5f);*/

            texCoordID++;
            stageID++;
        }

        //Stage with A0 used for transparency output
        private void SetupTransparencyStage(ref int stageID, bool noTexture)
        {
            //No texture maps to assign
            GXSetTevOrder(stageID, GX.TexCoordID.TEXCOORD_NULL, GX.TexMapID.TEXMAP_NULL, GX.ChannelID.COLOR0A0);
            //No color to mess with
            if (noTexture)
                SetTevColorIn(stageID, GX.CC.ZERO, GX.CC.ONE, GX.CC.RASC, GX.CC.ZERO);
            else
                SetTevColorIn(stageID, GX.CC.ZERO, GX.CC.ZERO, GX.CC.ZERO, GX.CC.CPREV);
            SetTevColorOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);
            //Alpha (A0 = tev color alpha 1)
            SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.APREV, GX.CA.A0, GX.CA.ZERO);
            SetTevAlphaOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, false, GX.Register.PREV);
            stageID++;
        }

        //Lighting channels used for displaying lighting, material, ambient and vertex colors
        private void SetColorChannels(bool hasLightingChannel1, bool useLights, bool alphaLighting, uint lightBit)
        {
            if (hasLightingChannel1) //2 color/alpha light channels
            {
                this.GXSetNumChans(2);

                if (this.MaterialData.VertexMode == LightingChannelFlags.VertexColorsWithAlpha) //2 channel type with vertex colors
                {
                    //Vertex colors
                    this.GXSetChanCtrl(GX.ChannelID.COLOR0, true, GX.ColorSrc.REG, GX.ColorSrc.VTX, lightBit, GX.AttenFn.NONE, GX.DiffuseFn.CLAMP);
                    this.GXSetChanCtrl(GX.ChannelID.COLOR1, true, GX.ColorSrc.REG, GX.ColorSrc.VTX, lightBit, GX.AttenFn.SPEC, GX.DiffuseFn.NONE);
                    if (alphaLighting) //Use light for second channel
                    {
                        this.GXSetChanCtrl(GX.ChannelID.ALPHA0, true, GX.ColorSrc.REG, GX.ColorSrc.VTX, lightBit, GX.AttenFn.NONE, GX.DiffuseFn.CLAMP);
                        this.GXSetChanCtrl(GX.ChannelID.ALPHA1, true, GX.ColorSrc.REG, GX.ColorSrc.VTX, lightBit, GX.AttenFn.NONE, GX.DiffuseFn.NONE);
                    }
                    else //No lights for second channel
                    {
                        this.GXSetChanCtrl(GX.ChannelID.ALPHA0, false, GX.ColorSrc.REG, GX.ColorSrc.VTX, lightBit, GX.AttenFn.NONE, GX.DiffuseFn.CLAMP);
                        this.GXSetChanCtrl(GX.ChannelID.ALPHA1, false, GX.ColorSrc.REG, GX.ColorSrc.VTX, lightBit, GX.AttenFn.NONE, GX.DiffuseFn.CLAMP);
                    }
                }
                else //2 channel type with vertex specular
                {
                    //Lights with ambient/material color
                    this.GXSetChanCtrl(GX.ChannelID.COLOR0, true, GX.ColorSrc.REG, GX.ColorSrc.REG, lightBit, GX.AttenFn.NONE, GX.DiffuseFn.CLAMP);
                    this.GXSetChanCtrl(GX.ChannelID.COLOR1, true, GX.ColorSrc.REG, GX.ColorSrc.REG, lightBit, GX.AttenFn.SPEC, GX.DiffuseFn.NONE);
                    //Alpha no lights
                    this.GXSetChanCtrl(GX.ChannelID.ALPHA0, false, GX.ColorSrc.REG, GX.ColorSrc.VTX, lightBit, GX.AttenFn.NONE, GX.DiffuseFn.CLAMP);
                    this.GXSetChanCtrl(GX.ChannelID.ALPHA1, false, GX.ColorSrc.REG, GX.ColorSrc.VTX, lightBit, GX.AttenFn.NONE, GX.DiffuseFn.CLAMP);
                }
            }
            else //Single color/alpha light channel
            {
                this.GXSetNumChans(1);

                if (this.MaterialData.VertexMode == LightingChannelFlags.VertexColorsWithAlpha)
                {
                    //Vertex colors with optional lights
                    this.GXSetChanCtrl(GX.ChannelID.COLOR0, useLights, GX.ColorSrc.REG, GX.ColorSrc.VTX, lightBit, GX.AttenFn.NONE, GX.DiffuseFn.SIGN);
                    if (alphaLighting) //Use lights in alpha channel
                        this.GXSetChanCtrl(GX.ChannelID.ALPHA0, true, GX.ColorSrc.REG, GX.ColorSrc.VTX, lightBit, GX.AttenFn.NONE, GX.DiffuseFn.SIGN);
                    else //No lighting in alpha channel
                        this.GXSetChanCtrl(GX.ChannelID.ALPHA0, false, GX.ColorSrc.REG, GX.ColorSrc.VTX, lightBit, GX.AttenFn.NONE, GX.DiffuseFn.CLAMP);
                }
                else
                {
                    //Default colors with raw material/ambient colors used and optional lighting
                    this.GXSetChanCtrl(GX.ChannelID.COLOR0, useLights, GX.ColorSrc.REG, GX.ColorSrc.REG, lightBit, GX.AttenFn.NONE, GX.DiffuseFn.SIGN);
                    this.GXSetChanCtrl(GX.ChannelID.ALPHA0, false, GX.ColorSrc.REG, GX.ColorSrc.REG, lightBit, GX.AttenFn.NONE, GX.DiffuseFn.CLAMP);
                }
            }
        }

        //Blending stage for drawing 2 texture stages, blending the stages by transparency or texture alpha
        private void SetupBlendingAlphaCombinerStage(ref int stageID, ref int texCoordID, int index, AttributeData tex)
        {
            if (tex.BlendTextureAlpha == 1.0f)
            {
                SetTevColorIn(stageID, GX.CC.ZERO, GX.CC.TEXC, GX.CC.RASC, GX.CC.ZERO); //0xf 8 10 0xf
                SetTevColorOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.REG2);
                SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO); //7 7 7 7
                SetTevAlphaOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, false, GX.Register.REG2);
                stageID++;
                GXSetTevOrder(stageID, GX.TexCoordID.TEXCOORD0 + texCoordID, GX.TexMapID.TEXMAP0 + index, GX.ChannelID.COLOR0A0);
                //Mix previous previous stage and texture target with texture alpha
                SetTevColorIn(stageID, GX.CC.CPREV, GX.CC.C2, GX.CC.TEXA, GX.CC.ZERO); //0 6 9 0xf
                SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO, GX.CA.APREV); //7 7 7 0
            }
            else
            {
                //Blend via konstant alpha
                this.TevKonstColors[3] = new RGBA(255, 255, 255, (byte)(tex.BlendTextureAlpha * 255));
                Stages[stageID].KonstAlphaSel = GX.KonstAlphaSel.KASEL_K3_A;

                SetTevColorIn(stageID, GX.CC.ZERO, GX.CC.TEXC, GX.CC.RASC, GX.CC.ZERO);
                SetTevColorOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.REG2);
                SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.TEXA, GX.CA.KONST, GX.CA.ZERO); //Konstant alpha used. Outputs to A2
                SetTevAlphaOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, false, GX.Register.REG2);
                stageID++;
                GXSetTevOrder(stageID, GX.TexCoordID.TEXCOORD_NULL, GX.TexMapID.TEXMAP_NULL, GX.ChannelID.COLOR0A0);
                //Mix previous previous stage and texture target with register alpha
                SetTevColorIn(stageID, GX.CC.CPREV, GX.CC.C2, GX.CC.A2, GX.CC.ZERO); //0 6 7 0xf
                SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO, GX.CA.APREV); //7 7 7 0
            }
        }

        //Blends grayscale image with tint color
        private void SetupGrayscaleTexStageFirstTex1(ref int stageID, int index, GX.ChannelID rasterOutput)
        {
            SetKColorRGB(0, TextureTintColor);

            SetTevColorIn(0, GX.CC.ZERO, GX.CC.TEXC, GX.CC.KONST, GX.CC.ZERO);
            SetTevColorOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.REG2);
            SetTevAlphaIn(0, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO);
            SetTevAlphaOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.REG2);

            stageID++;

            this.Stages[stageID].KonstColorSel = GX.KonstColorSel.KCSEL_K0;

            GXSetTevOrder(stageID, GX.TexCoordID.TEXCOORD0 + stageID, GX.TexMapID.TEXMAP0 + index, rasterOutput);
            SetTevColorIn(stageID, GX.CC.ZERO, GX.CC.CPREV, GX.CC.KONST, GX.CC.ZERO);
            SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.TEXA, GX.CA.KONST, GX.CA.ZERO);
            stageID++;
        }

        //Blends grayscale image with tint color for first texture map
        private void SetupGrayscaleTexStageFirstTex2(ref int stageID, int index, GX.ChannelID rasterOutput)
        {
            //Tex swap channel

            // 0/1 swaps
            SetTexSwapChannel(0, GX.TevColorChan.R, GX.TevColorChan.G, GX.TevColorChan.B, GX.TevColorChan.A);
            SetRasSwapChannel(0, GX.TevColorChan.R, GX.TevColorChan.A, GX.TevColorChan.A, GX.TevColorChan.A);

            SetKColorRGB(0, TextureTintColor1);
            SetTevColorIn(0, GX.CC.ZERO, GX.CC.TEXC, GX.CC.KONST, GX.CC.ZERO);
            SetTevColorOp(0, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, false, GX.Register.REG2);
            GXSetTevOrder(0, GX.TexCoordID.TEXCOORD0, GX.TexMapID.TEXMAP0, GX.ChannelID.COLOR_NULL);

            stageID++;

            // 0/2 swaps
            SetTexSwapChannel(stageID, GX.TevColorChan.R, GX.TevColorChan.G, GX.TevColorChan.B, GX.TevColorChan.A);
            SetRasSwapChannel(stageID, GX.TevColorChan.B, GX.TevColorChan.B, GX.TevColorChan.B, GX.TevColorChan.A);

            SetKColorRGB(stageID, TextureTintColor2);

            //Blend C2 with textue color, konst and previous
            SetTevColorIn(stageID, GX.CC.CPREV, GX.CC.TEXC, GX.CC.KONST, GX.CC.C2);
            SetTevAlphaIn(stageID, GX.CA.APREV, GX.CA.KONST, GX.CA.TEXA, GX.CA.ZERO);

            SetTevColorOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);
            SetTevAlphaOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV);

            //Todo how is temap configured?
            GXSetTevOrder(stageID, GX.TexCoordID.TEXCOORD0, GX.TexMapID.TEXMAP0 + 1, GX.ChannelID.COLOR_NULL);
        }

        //Blends grayscale image with tint color
        private void SetupGrayscaleTexStage1(ref int stageID, AttributeData tex, int index, GX.ChannelID rasterOutput)
        {
            SetKColorRGB(stageID, TextureTintColor);

            SetTevColorIn(stageID, GX.CC.ZERO, GX.CC.TEXC, GX.CC.KONST, GX.CC.ZERO);
            SetTevColorOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.REG2);
            SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO);
            SetTevAlphaOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.REG2);

            stageID++;

            byte blend = (byte)(tex.BlendTextureAlpha * 255.0f);
            SetKColor(stageID, blend);

            GXSetTevOrder(stageID, GX.TexCoordID.TEXCOORD0 + stageID, GX.TexMapID.TEXMAP0 + index, rasterOutput);
            SetTevColorIn(stageID, GX.CC.CPREV, GX.CC.C2, GX.CC.KONST, GX.CC.ZERO);
            SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.TEXA, GX.CA.APREV, GX.CA.ZERO);
        }

        //Blends 2 grayscale images using both tint colors
        private void SetupGrayscaleTexStage2(ref int stageID, int index, GX.ChannelID rasterOutput)
        {
            //Tex swap channel

            // 0/1 swaps
            SetTexSwapChannel(0, GX.TevColorChan.R, GX.TevColorChan.G, GX.TevColorChan.B, GX.TevColorChan.A);
            SetRasSwapChannel(0, GX.TevColorChan.R, GX.TevColorChan.A, GX.TevColorChan.A, GX.TevColorChan.A);

            SetKColorRGB(stageID, TextureTintColor1);
            SetTevColorIn(stageID, GX.CC.ZERO, GX.CC.TEXC, GX.CC.KONST, GX.CC.ZERO); //0xf 8 0xe 0xf
            SetTevColorOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, false, GX.Register.REG2); //0 0 0 0 3
            GXSetTevOrder(stageID, GX.TexCoordID.TEXCOORD0, GX.TexMapID.TEXMAP0 + index, GX.ChannelID.COLOR_NULL);

            stageID++;

            // 0/2 swaps
            SetTexSwapChannel(stageID, GX.TevColorChan.R, GX.TevColorChan.G, GX.TevColorChan.B, GX.TevColorChan.A);
            SetRasSwapChannel(stageID, GX.TevColorChan.B, GX.TevColorChan.B, GX.TevColorChan.B, GX.TevColorChan.A);

            SetKColorRGB(stageID, TextureTintColor2);

            //Blend C2 with textue color, konst and previous
            SetTevColorIn(stageID, GX.CC.CPREV, GX.CC.TEXC, GX.CC.KONST, GX.CC.C2); //0 8 0xe 6
            SetTevAlphaIn(stageID, GX.CA.APREV, GX.CA.KONST, GX.CA.TEXA, GX.CA.ZERO); //0 6 4 7

            GXSetTevOrder(stageID, GX.TexCoordID.TEXCOORD0, GX.TexMapID.TEXMAP0 + 1, GX.ChannelID.COLOR_NULL);
        }

        //Draws image as bump map
        private void SetupBumpMappingStage(ref int stageID, ref int texCoordID, int index, AttributeData tex)
        {
            //First texture coordinate to target bump map
            this.SetTexCoordGen2(texCoordID, GX.TexGenType.MTX2x4, GX.TexGenSrc.TEX0, GX.TexGenMatrix.IDENTITY, false, GX.PostTexGenMatrix.PTIDENTITY);
            //Additonal tex coord output
            this.SetTexCoordGen2(2, GX.TexGenType.BUMP0, GX.TexGenSrc.TEXCOORD0, GX.TexGenMatrix.IDENTITY, false, GX.PostTexGenMatrix.PTIDENTITY);

            byte blend = (byte)(tex.NbtEnable * 10.0f);
            SetKColor(stageID, blend);

            GXSetTevOrder(stageID, GX.TexCoordID.TEXCOORD0 + texCoordID, GX.TexMapID.TEXMAP0 + index, GX.ChannelID.COLOR0A0);
            SetTevColorIn(stageID, GX.CC.ZERO, GX.CC.TEXC, GX.CC.KONST, GX.CC.RASC); //0xf 8 0xe 10
            SetTevColorOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV); //0 0 0 1 0
            SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO, GX.CA.APREV); //7 7 7 7
            SetTevAlphaOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, false, GX.Register.PREV); //0 0 0 0 0

            stageID++;

            GXSetTevOrder(stageID, GX.TexCoordID.TEXCOORD0, GX.TexMapID.TEXMAP0, GX.ChannelID.COLOR0A0);
            SetTevColorIn(stageID, GX.CC.ZERO, GX.CC.TEXC, GX.CC.A1, GX.CC.CPREV); //0xf 8 5 0
            SetTevColorOp(stageID, GX.TevOp.SUB, GX.TevBias.ZERO, GX.TevScale.SCALE_1, true, GX.Register.PREV); //0 0 0 1 0

            SetTevAlphaIn(stageID, GX.CA.ZERO, GX.CA.ZERO, GX.CA.ZERO, GX.CA.APREV); //7 7 7 0
            SetTevAlphaOp(stageID, GX.TevOp.ADD, GX.TevBias.ZERO, GX.TevScale.SCALE_1, false, GX.Register.PREV); //0 0 0 0 0
        }
    }
}
