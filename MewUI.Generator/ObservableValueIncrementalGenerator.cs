using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace MewUI.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class ObservableValueIncrementalGenerator : IIncrementalGenerator
{
    // 声明式诊断定义（与业务逻辑完全分离）
    private static class Diagnostics
    {
        public static readonly DiagnosticDescriptor NotPartial = new(
            "OBS001", "缺少 partial", "类 '{0}' 必须标记为 partial", "MewUI", DiagnosticSeverity.Warning, true);

        public static readonly DiagnosticDescriptor NoSetter = new(
            "OBS002", "缺少 setter", "属性 '{0}' 必须有 setter", "MewUI", DiagnosticSeverity.Warning, true);

        public static readonly DiagnosticDescriptor NameConflict = new(
            "OBS003", "命名冲突", "类 '{0}' 中已存在 '{1}Observable' 属性，无法自动生成", "MewUI", DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor RedundantAttr = new(
            "OBS004", "冗余特性", "属性 '{0}' 上的 [AsObservableValue] 是多余的，类已标记该特性", "MewUI", DiagnosticSeverity.Warning, true);
    }

    // 注意：AsObservableValueAttribute 已迁移至共享运行时库 MewUI.Generator.Runtime，
    // 本生成器不再通过 PostInitialization 注入，避免跨程序集类型定义冲突。

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. 统一收集所有需要生成的 (类型符号, 是否由类级别触发)
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "System.AsObservableValueAttribute",
            predicate: static (node, _) => node is ClassDeclarationSyntax or PropertyDeclarationSyntax,
            transform: static (ctx, ct) =>
            {
                if (ctx.TargetSymbol is INamedTypeSymbol type)
                    return (Type: type, IsClassLevel: true);
                if (ctx.TargetSymbol is IPropertySymbol prop)
                    return (Type: prop.ContainingType, IsClassLevel: false);
                return default;
            })
            .Where(static x => x.Type != null)
            .Collect();

        // 2. 生成源码与报告诊断
        context.RegisterSourceOutput(provider, static (spc, items) =>
        {
            // 按类型分组去重（类级别优先）
            var grouped = new Dictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);
            foreach (var item in items)
            {
                if (!grouped.TryGetValue(item.Type, out var existing) || !existing)
                    grouped[item.Type] = item.IsClassLevel;
            }

            foreach (var kvp in grouped)
                GenerateForType(spc, kvp.Key, kvp.Value);
        });
    }

    private static void GenerateForType(SourceProductionContext spc, INamedTypeSymbol type, bool isClassLevel)
    {
        // 检查 partial（使用集中定义的诊断）
        bool isPartial = type.DeclaringSyntaxReferences.Any(r =>
            r.GetSyntax() is TypeDeclarationSyntax tds && tds.Modifiers.Any(SyntaxKind.PartialKeyword));

        if (!isPartial)
        {
            spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.NotPartial, Location.None, type.Name));
            return;
        }

        var ns = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString();
        var existingMembers = new HashSet<string>(type.GetMembers().Select(m => m.Name));
        var properties = new List<(string Name, string TypeName)>();

        foreach (var member in type.GetMembers())
        {
            if (member is not IPropertySymbol prop || prop.IsStatic || prop.IsIndexer || prop.IsImplicitlyDeclared)
                continue;

            bool hasSetter = prop.SetMethod != null;
            bool hasAttr = prop.GetAttributes().Any(a => a.AttributeClass?.Name == "AsObservableValueAttribute");
            bool shouldGenerate = isClassLevel ? hasSetter : hasAttr;

            if (!shouldGenerate) continue;

            // 冗余警告
            if (isClassLevel && hasAttr)
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.RedundantAttr, Location.None, prop.Name));

            // 缺少 setter
            if (!hasSetter)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.NoSetter, Location.None, prop.Name));
                continue;
            }

            // 命名冲突
            string obsName = $"{prop.Name}Observable";
            if (existingMembers.Contains(obsName))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.NameConflict, Location.None, type.Name, prop.Name));
                continue;
            }

            var typeName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));

            properties.Add((prop.Name, typeName));
        }

        if (properties.Count == 0) return;

        spc.AddSource($"{type.Name}.g.cs", EmitSource(ns, type.Name, properties));
    }

    private static string EmitSource(string? ns, string className, List<(string Name, string Type)> props)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");

        if (ns != null)
        {
            sb.AppendLine($"namespace {ns};");
            sb.AppendLine();
        }

        sb.AppendLine($"partial class {className}");
        sb.AppendLine("{");

        for (int i = 0; i < props.Count; i++)
        {
            var (name, type) = props[i];
            sb.AppendLine($"    /// <inheritdoc cref=\"{name}\"/>");
            sb.AppendLine($"    [global::System.Text.Json.Serialization.JsonIgnore]");
            sb.AppendLine($"    public global::Aprillz.MewUI.ObservableValue<{type}> {name}Observable =>");
            sb.AppendLine($"        field ??= new(this.{name}, value =>");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            On{name}Changing(value);");
            sb.AppendLine($"            {name} = value;");
            sb.AppendLine($"            On{name}Changed(value);");
            sb.AppendLine($"            return value;");
            sb.AppendLine($"        }});");
            sb.AppendLine();
            sb.AppendLine($"    partial void On{name}Changing({type} value);");
            sb.AppendLine($"    partial void On{name}Changed({type} value);");

            if (i < props.Count - 1) sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }
}