using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using Validation;

namespace Rendering
{
    public sealed class SphSweStunScreenRendererFeature : ScriptableRendererFeature
    {
        [SerializeField, Required]
        private Material stunScreenEffectMaterial;

        private StunScreenRenderPass stunScreenRenderPass;

        public override void Create()
        {
            stunScreenRenderPass = new StunScreenRenderPass
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (stunScreenEffectMaterial == null)
            {
                return;
            }

            if (renderingData.cameraData.cameraType != CameraType.Game)
            {
                return;
            }

            stunScreenRenderPass.Setup(stunScreenEffectMaterial);

            renderer.EnqueuePass(stunScreenRenderPass);
        }

        private sealed class StunScreenRenderPass : ScriptableRenderPass
        {
            private const string RenderPassName = "SPH-SWE Stun Screen Effect";
            private Material stunScreenEffectMaterial;

            public void Setup(Material material)
            {
                stunScreenEffectMaterial = material;

                // 現在の画面色を入力テクスチャとして読み取るため、
                // URPへ中間カラーテクスチャの生成を要求する。
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();

                // 現在の描画先が最終出力用のバックバッファだった場合に、画面エフェクトの処理を中止
                if (resourceData.isActiveTargetBackBuffer)
                {
                    return;
                }

                var sourceTexture = resourceData.activeColorTexture;
                // Texture設定をコピー
                var destinationTextureDescription = renderGraph.GetTextureDesc(sourceTexture);

                destinationTextureDescription.name = "Stun Screen Effect Color";
                // バッファを使用前にクリアしない
                destinationTextureDescription.clearBuffer = false;
                
                var destinationTexture = renderGraph.CreateTexture(destinationTextureDescription);

                var blitParameters =
                    new RenderGraphUtils.BlitMaterialParameters(
                        sourceTexture,
                        destinationTexture,
                        stunScreenEffectMaterial,
                        0 // 使用するShader Pass番号
                    );

                renderGraph.AddBlitPass(blitParameters, RenderPassName);

                // 以降のPost Processingが、エフェクト適用後の
                // テクスチャを画面色として使用するように差し替える。
                resourceData.cameraColor = destinationTexture;
            }
        }
    }
}