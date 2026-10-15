using Godot;

namespace Campus.Hud.Style;

// 临时接缝（contract-3，Opus 已认可形状）：Astra 把 IUiMaterialSource 与 UiShader 放进 Kernel 的命名空间、
// WP-04 在 Campus.Style 里实现之后，删掉本文件，UiMaterials 改用正式接口。这里不建任何材质。
public enum UiShader : byte { Panel, Screen, MapBase }

public interface IUiMaterialSource
{
    ShaderMaterial Create(UiShader shader);
}
