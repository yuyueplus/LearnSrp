using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer{
    private ScriptableRenderContext context;

    private Camera camera;

    private const string bufferName = "Render Camera";

    private CommandBuffer buffer = new CommandBuffer{
        name = bufferName
    };

    //SRP不支持的着色器标签类型
    private static ShaderTagId[] legacyShaderTagIds ={
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM"),
    };

    public void Render(ScriptableRenderContext context, Camera camera){
        this.context = context;
        this.camera = camera;

        if (!Cull()){
            return;
        }
        
        Setup();
        DrawVisibleGeometry();
        DrawUnsupportedShader();
        Submit();
    }

    //绘制成使用错误材质的粉红颜色2
    private static Material errorMaterial;
    
    /// <summary>
    /// 绘制SRP不支持的着色器类型
    /// </summary>
    private void DrawUnsupportedShader(){
        //不支持的ShaderTag类型使用错误材质专用Shader来绘制(粉色)
        if (errorMaterial == null){
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        
        //数组第一个元素用来构造DrawingSetting对象的时候设置
        var drawSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera)){
            overrideMaterial = errorMaterial
        };
        for (int i = 1; i < legacyShaderTagIds.Length; i++){
            //遍历数组逐个设置着色器的PassName
            drawSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
        //使用默认设置即可，反正画出来的都是不支持的
        var filteringSettings = FilteringSettings.defaultValue;
        //绘制不支持的ShaderTag类型的物体
        context.DrawRenderers(cullingResults, ref drawSettings, ref filteringSettings);
    }

    //存储剔除后的结果数据
    private CullingResults cullingResults;

    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

    bool Cull(){
        ScriptableCullingParameters p;
        if (camera.TryGetCullingParameters(out p)){
            cullingResults = context.Cull(ref p);
            return true;
        }

        return false;
    }

    private void Setup(){
        context.SetupCameraProperties(camera);
        buffer.BeginSample(bufferName);
        buffer.ClearRenderTarget(true, true, Color.clear);
        ExecuteBuffer();
    }
    
    private void DrawVisibleGeometry(){
        //设置绘制顺序和指定渲染相机
        var sortingSetting = new SortingSettings(camera){
            criteria = SortingCriteria.CommonOpaque
        };
        //设置渲染的Shader Pass和排序模式
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSetting);
        //只绘制RenderQueue为opaque不透明的物体
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        //1.绘制不透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        //2.绘制天空盒
        context.DrawSkybox(camera);
 
        sortingSetting.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSetting;
        //只绘制RenderQueue为Transparent透明的物体
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        //3.绘制透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    private void Submit(){
        buffer.EndSample(bufferName);
        ExecuteBuffer();
        context.Submit();
    }

    private void ExecuteBuffer(){
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}
