// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Rendering.Materials.ComputeColors;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// A transparent blend material that also does cutoff - a combination of blend and cutoff
    /// </summary>
    [DataContract("MaterialTransparencyBlendCutoffFeature")]
    [Display("Blend And Cutoff")]
    public class MaterialTransparencyBlendCutoffFeature : MaterialFeature, IMaterialTransparencyFeature
    {
        public const int ShadingColorAlphaFinalCallbackOrder = MaterialGeneratorContext.DefaultFinalCallbackOrder;

        // blend memmbers
        private static readonly MaterialStreamDescriptor AlphaBlendStream = new MaterialStreamDescriptor("DiffuseSpecularAlphaBlend", "matDiffuseSpecularAlphaBlend", MaterialKeys.DiffuseSpecularAlphaBlendValue.PropertyType);

        private static readonly MaterialStreamDescriptor AlphaBlendColorStream = new MaterialStreamDescriptor("DiffuseSpecularAlphaBlend - Color", "matAlphaBlendColor", MaterialKeys.AlphaBlendColorValue.PropertyType);

        // cutoff members
        private static readonly MaterialStreamDescriptor AlphaDiscardStream = new MaterialStreamDescriptor("Alpha Discard", "matAlphaDiscard", MaterialKeys.AlphaDiscardValue.PropertyType);

        private const float DefaultCutoffAlpha = 0.5f;

        // hasfinalcallback
        private static readonly PropertyKey<bool> HasFinalCallback = new PropertyKey<bool>("MaterialTransparencyBlendCutoffFeature.HasFinalCallback", typeof(MaterialTransparencyAdditiveFeature));

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialTransparencyBlendCutoffFeature"/> class.
        /// </summary>
        public MaterialTransparencyBlendCutoffFeature()
        {
            // blend
            Alpha = new ComputeFloat(1f);
            Tint = new ComputeColor(Color.White);

            // cutoff
            CutoffAlpha = new ComputeFloat(DefaultCutoffAlpha);
        }
    
        /// <summary>
        /// Gets or sets the alpha for blend.
        /// </summary>
        /// <value>The alpha.</value>
        /// <userdoc>An additional factor that can be used to modulate original alpha of the material.</userdoc>
        [NotNull]
        [DataMember(10)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
        public IComputeScalar Alpha { get; set; }

        /// <summary>
        /// Gets or sets the tint color.
        /// </summary>
        /// <value>The tint.</value>
        /// <userdoc>The tint color to apply on the material during the blend.</userdoc>
        [NotNull]
        [DataMember(20)]
        public IComputeColor Tint { get; set; }

        /// <summary>
        /// Gets or sets the alpha for Cutoff.
        /// </summary>
        /// <value>The alpha.</value>
        /// <userdoc>The alpha threshold of the cutoff. All alpha values above this threshold are considered as fully transparent.
        /// All alpha values under this threshold are considered as fully opaque.</userdoc>
        [NotNull]
        [DataMember(30)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
        public IComputeScalar CutoffAlpha { get; set; }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            // blend
            var alpha = Alpha ?? new ComputeFloat(1f);
            var tint = Tint ?? new ComputeColor(Color.White);
            alpha.ClampFloat(0, 1);

            // Use pre-multiplied alpha to support both additive and alpha blending
            if (context.MaterialPass.BlendState == null)
                context.MaterialPass.BlendState = BlendStates.AlphaBlend;
            context.MaterialPass.HasTransparency = true;
            // TODO GRAPHICS REFACTOR
            //context.Parameters.SetResourceSlow(Effect.BlendStateKey, BlendState.NewFake(blendDesc));

            context.SetStream(AlphaBlendStream.Stream, alpha, MaterialKeys.DiffuseSpecularAlphaBlendMap, MaterialKeys.DiffuseSpecularAlphaBlendValue, Color.White);
            context.SetStream(AlphaBlendColorStream.Stream, tint, MaterialKeys.AlphaBlendColorMap, MaterialKeys.AlphaBlendColorValue, Color.White);

            // cutoff
            var cutoffalpha = CutoffAlpha ?? new ComputeFloat(DefaultCutoffAlpha);
            cutoffalpha.ClampFloat(0, 1);
            context.SetStream(AlphaDiscardStream.Stream, cutoffalpha, MaterialKeys.AlphaDiscardMap, MaterialKeys.AlphaDiscardValue, new Color(DefaultCutoffAlpha));

            context.MaterialPass.Parameters.Set(MaterialKeys.UsePixelShaderWithDepthPass, true);

            if (!context.Tags.Get(HasFinalCallback))
            {
                context.Tags.Set(HasFinalCallback, true);
                // blend
                context.AddFinalCallback(MaterialShaderStage.Pixel, AddDiffuseSpecularAlphaBlendCutoffColor, ShadingColorAlphaFinalCallbackOrder);
                // cutoff
                //context.AddFinalCallback(MaterialShaderStage.Pixel, AddDiscardFromLuminance);
            }
        }
    
        private void AddDiffuseSpecularAlphaBlendCutoffColor(MaterialShaderStage stage, MaterialGeneratorContext context)
        {
            context.AddShaderSource(MaterialShaderStage.Pixel, new ShaderClassSource("MaterialSurfaceDiffuseSpecularAlphaBlendCutoffColor"));
        }

    }
}
