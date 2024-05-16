using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer {
    private ScriptableRenderContext context;

    private Camera camera;

    private const string bufferName = "Render Camera";

    private CommandBuffer buffer = new CommandBuffer {
        name = bufferName
    };

    public void Render(ScriptableRenderContext context, Camera camera) {
        this.context = context;
        this.camera = camera;

        PrepareForSceneWindow();
        
        if (!Cull()) {
            return;
        }

        Setup();
        DrawVisibleGeometry();
#if UNITY_EDITOR
        DrawUnsupportedShader();
        DrawGizmos();
#endif
        Submit();
    }

    //存储剔除后的结果数据
    private CullingResults cullingResults;

    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

    bool Cull() {
        ScriptableCullingParameters p;
        if (camera.TryGetCullingParameters(out p)) {
            cullingResults = context.Cull(ref p);
            return true;
        }

        return false;
    }

    private void Setup() {
        context.SetupCameraProperties(camera);
        buffer.BeginSample(SampleName);
        CameraClearFlags flags = camera.clearFlags;
        //设置相机清除状态
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear
        );
        ExecuteBuffer();
    }

    private void DrawVisibleGeometry() {
        //设置绘制顺序和指定渲染相机
        var sortingSetting = new SortingSettings(camera) {
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

    private void Submit() {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    private void ExecuteBuffer() {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}