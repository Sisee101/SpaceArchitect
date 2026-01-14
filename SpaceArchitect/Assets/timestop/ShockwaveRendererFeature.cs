using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 冲击波 Renderer Feature
/// 在所有透明物体渲染之后执行，使用全屏 Blit 渲染冲击波效果
/// 可以读取完整的屏幕内容（包括透明物体），不会遮挡任何物体
/// </summary>
public class ShockwaveRendererFeature : ScriptableRendererFeature
{
    /// <summary>
    /// 冲击波渲染通道
    /// </summary>
    class ShockwaveRenderPass : ScriptableRenderPass
    {
        private Material shockwaveMaterial;
        private RenderTargetHandle tempTexture; // 临时纹理
        
        public ShockwaveRenderPass(Material material)
        {
            shockwaveMaterial = material;
            // 在所有透明物体渲染之后执行
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            tempTexture.Init("_TempShockwaveTexture");
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (shockwaveMaterial == null)
            {
                return;
            }
            
            // 检查强度，如果为 0 则不渲染
            float strength = shockwaveMaterial.GetFloat("_RippleStrength");
            if (strength <= 0)
            {
                return;
            }
            
            // 在 Execute 中获取源纹理（这样可以在正确的作用域内访问）
            var renderer = renderingData.cameraData.renderer;
            RenderTargetIdentifier source = renderer.cameraColorTarget;
            
            CommandBuffer cmd = CommandBufferPool.Get("Shockwave Effect");
            
            // 获取相机描述符
            RenderTextureDescriptor cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            cameraTextureDescriptor.depthBufferBits = 0; // 不需要深度缓冲
            
            // 创建临时纹理
            cmd.GetTemporaryRT(tempTexture.id, cameraTextureDescriptor, FilterMode.Bilinear);
            
            // 显式设置源纹理到 _MainTex（确保正确传递）
            cmd.SetGlobalTexture("_MainTex", source);
            
            // 使用 Blit 将源纹理（完整屏幕内容）通过冲击波材质渲染到临时纹理
            Blit(cmd, source, tempTexture.Identifier(), shockwaveMaterial, 0);
            
            // 将临时纹理 Blit 回源纹理（完成效果叠加）
            Blit(cmd, tempTexture.Identifier(), source);
            
            // 释放临时纹理
            cmd.ReleaseTemporaryRT(tempTexture.id);
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        public override void FrameCleanup(CommandBuffer cmd)
        {
            // 清理临时纹理
            if (tempTexture != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(tempTexture.id);
            }
        }
    }
    
    [SerializeField]
    private Material shockwaveMaterial;
    
    private ShockwaveRenderPass m_ShockwavePass;
    
    /// <summary>
    /// 创建渲染通道
    /// </summary>
    public override void Create()
    {
        if (shockwaveMaterial != null)
        {
            m_ShockwavePass = new ShockwaveRenderPass(shockwaveMaterial);
            m_ShockwavePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }
    }
    
    /// <summary>
    /// 添加渲染通道到渲染管线
    /// </summary>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_ShockwavePass != null && shockwaveMaterial != null)
        {
            // 不需要在这里设置源纹理，在 Execute 中获取
            renderer.EnqueuePass(m_ShockwavePass);
        }
    }
}

