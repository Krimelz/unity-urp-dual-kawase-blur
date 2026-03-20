using System;
using Blur.Passes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Blur
{
    [Serializable]
    public class BlurSettings
    {
        [Range(0f, 8f)]
        public float Radius = 1f;
        [Range(0, 6)]
        public int Iterations = 2;
    }
    
    public class BlurRenderFeature : ScriptableRendererFeature
    {
        private const string ShaderName = "Hidden/Blur";
        
        [SerializeField] 
        private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
        [SerializeField] 
        private BlurSettings _settings = new();
        
        private BlurPass _pass;
        private Shader _shader;
        private Material _material;

        public override void Create()
        {
#if UNITY_EDITOR
            _shader = Shader.Find(ShaderName);
#endif
            CoreUtils.Destroy(_material);

            _material = CoreUtils.CreateEngineMaterial(_shader);

            if (_material == null)
            {
                Debug.LogError($"Failed to get material {_shader}");
                return;
            }
            
            _settings ??= new BlurSettings();
            
            _pass = new BlurPass(_settings, _material);
            _pass.renderPassEvent = _renderPassEvent;
            _pass.ConfigureInput(ScriptableRenderPassInput.Color);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_settings.Iterations == 0 || _settings.Radius <= 0f + Mathf.Epsilon)
            {
                return;
            }

            if (renderingData.cameraData.cameraType is not (CameraType.Game or CameraType.SceneView))
            {
                return;
            }
            
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
        }
    }
}
