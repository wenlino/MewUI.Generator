using System;

namespace MewUI.Generator.Runtime;

/// <summary>
/// 标注在属性上，由 MewUI.Generator 源生成器在编译期生成 MewProperty 依赖属性。
/// 该特性位于共享运行时库，确保跨程序集使用同一份类型定义。
/// </summary>
/// <remarks>
/// <see cref="MewPropertyOptions"/> 使用本地枚举类型以避免运行时库
/// 硬依赖 Aprillz.MewUI 包（该包仅支持 net8.0+），同时为消费方提供
/// 编译期智能提示。源生成器会一对一匹配转换为真实的
/// <c>Aprillz.MewUI.MewPropertyOptions</c> 枚举值。
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class AsMewPropertyAttribute(object? defaultValue = null) : Attribute
{
    public object? DefaultValue { get; set; } = defaultValue;

    /// <summary>
    /// 属性选项，使用本地 <see cref="MewPropertyOptions"/> 枚举方便选择。
    /// 默认值为 <c>MewPropertyOptions.AffectsRender</c>，源生成器会一对一
    /// 匹配转换为 <c>Aprillz.MewUI.MewPropertyOptions</c> 枚举值。
    /// </summary>
    public MewPropertyOptions MewPropertyOptions { get; set; } = MewPropertyOptions.AffectsRender;
}