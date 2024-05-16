using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public partial class CameraRenderer {
    partial void DrawUnsupportedShader();
    partial void DrawGizmos();
    partial void PrepareForSceneWindow();
    partial void PrepareBuffer();

#if UNITY_EDITOR
    //SRP不支持的着色器标签类型
    private static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM"),
    };

    //绘制成使用错误材质的粉红颜色2
    private static Material errorMaterial;

    /// <summary>
    /// 绘制SRP不支持的着色器类型
    /// </summary>
    partial void DrawUnsupportedShader() {
        //不支持的ShaderTag类型使用错误材质专用Shader来绘制(粉色)
        if (errorMaterial == null) {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        //数组第一个元素用来构造DrawingSetting对象的时候设置
        var drawSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera)) {
            overrideMaterial = errorMaterial
        };
        for (int i = 1; i < legacyShaderTagIds.Length; i++) {
            //遍历数组逐个设置着色器的PassName
            drawSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }

        //使用默认设置即可，反正画出来的都是不支持的
        var filteringSettings = FilteringSettings.defaultValue;
        //绘制不支持的ShaderTag类型的物体
        context.DrawRenderers(cullingResults, ref drawSettings, ref filteringSettings);
    }

    /// <summary>
    /// 绘制DrawGizmos
    /// </summary>
    partial void DrawGizmos() {
        if (Handles.ShouldRenderGizmos()) {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void PrepareForSceneWindow() {
        if (camera.cameraType == CameraType.SceneView) {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }

    private string SampleName { get; set; }
    
    partial void PrepareBuffer() {
        //设置只有在编辑器模式下才分配内存
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }
#else
    const string SampleName = bufferName;
#endif
}