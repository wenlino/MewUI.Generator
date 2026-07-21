using System;

namespace MewUI.Generator.Runtime;

/// <summary>
/// 标注在类或属性上，由 MewUI.Generator 源生成器在编译期生成 ObservableValue 包装。
/// 标注在类上时，所有带 setter 的实例属性都会自动生成。
/// 该特性位于共享运行时库，确保跨程序集使用同一份类型定义。
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AsObservableValueAttribute : Attribute { }