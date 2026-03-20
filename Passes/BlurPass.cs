using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Blur.Passes
{
    internal class PassData
    {
        public TextureHandle Source;
        public TextureHandle[] Textures;
        public Material Material;
        public float Offset;
    }

    public class BlurPass : ScriptableRenderPass
    {
        private const string PassName = "Blur Pass";
        
        private static readonly int BlurOffset = Shader.PropertyToID("_BlurOffset");
        private static readonly ProfilingSampler DownsampleProfiler = new(PassName + " Downsample");
        private static readonly ProfilingSampler UpsampleProfiler = new(PassName + " Upsample");
        
        private readonly BlurSettings _settings;
        private readonly Material _material;
        
        public BlurPass(BlurSettings settings, Material material)
        {
            _settings = settings;
            _material = material;
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var source = resourceData.activeColorTexture;
            
            var desc = renderGraph.GetTextureDesc(source);
            desc.clearBuffer = false;
            desc.depthBufferBits = 0;
                
            var maxPossibleSteps = Mathf.FloorToInt(Mathf.Log(Mathf.Min(desc.width, desc.height), 2)) - 1;
            var actualIterations = Mathf.Clamp(_settings.Iterations, 0, Mathf.Max(0, maxPossibleSteps));

            if (actualIterations == 0)
            {
                return;
            }
            
            using var builder = renderGraph.AddUnsafePass<PassData>(PassName, out var passData);
            
            passData.Source = source;
            passData.Textures = new TextureHandle[_settings.Iterations];
            passData.Textures = new TextureHandle[actualIterations];
            passData.Material = _material;
            passData.Offset = _settings.Radius;

            builder.UseTexture(source, AccessFlags.ReadWrite);
                
            for (var i = 0; i < passData.Textures.Length; i++)
            {
                desc.width = Mathf.Max(1, desc.width / 2);
                desc.height = Mathf.Max(1, desc.height / 2);
                desc.name = $"{passName}_{i}_{desc.width}x{desc.height}";

                passData.Textures[i] = renderGraph.CreateTexture(desc);
                builder.UseTexture(passData.Textures[i], AccessFlags.ReadWrite);
            }
            
            builder.AllowPassCulling(false);
            builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
        }
        
        private static void ExecutePass(PassData data, UnsafeGraphContext context)
        {
            var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
            cmd.SetGlobalFloat(BlurOffset, data.Offset);

            DownsampleProfiler.Begin(cmd);
            Blitter.BlitCameraTexture(cmd, data.Source, data.Textures[0], data.Material, 0);
            for (var i = 0; i < data.Textures.Length - 1; i++)
            {
                Blitter.BlitCameraTexture(cmd, data.Textures[i], data.Textures[i + 1], data.Material, 0);
            }
            DownsampleProfiler.End(cmd);

            UpsampleProfiler.Begin(cmd);
            for (var i = data.Textures.Length - 1; i > 0; i--)
            {
                Blitter.BlitCameraTexture(cmd, data.Textures[i], data.Textures[i - 1], data.Material, 1);
            }
            Blitter.BlitCameraTexture(cmd, data.Textures[0], data.Source, data.Material, 1);
            UpsampleProfiler.End(cmd);
        }
    }
}