namespace System
{
    /// <summary>
    /// 标注在属性上，由 MewUI.Generator 源生成器在编译期生成 MewProperty 依赖属性。
    /// 该特性位于共享运行时库，确保跨程序集使用同一份类型定义。
    /// </summary>
    /// <remarks>
    /// <see cref="MewPropertyOptions"/> 属性类型为 <see cref="object"/> 以避免运行时库
    /// 硬依赖 Aprillz.MewUI 包（该包仅支持 net8.0+）。消费方可直接传入
    /// <c>Aprillz.MewUI.MewPropertyOptions</c> 枚举值，源生成器会通过
    /// <c>TypedConstant.ToCSharpString()</c> 正确还原枚举的完全限定名。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class AsMewPropertyAttribute(object? defaultValue = null) : Attribute
    {
        public object? DefaultValue { get; set; } = defaultValue;

        /// <summary>
        /// 属性选项，传入 <c>Aprillz.MewUI.MewPropertyOptions</c> 枚举值。
        /// 默认值在源生成器中解析为 <c>MewPropertyOptions.AffectsRender</c>。
        /// </summary>
        public object? MewPropertyOptions { get; set; }
    }
}