# MewUI.Generator

[![NuGet](https://img.shields.io/nuget/v/MewUI.Generator.svg)](https://www.nuget.org/packages/MewUI.Generator)
[![.NET](https://img.shields.io/badge/.NET-8.0%2B-512BD4)](https://dotnet.microsoft.com/download)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

**MewUI.Generator** 是一个基于 Roslyn 的 C# 源代码生成器，为 [Aprillz.MewUI](https://www.nuget.org/packages/Aprillz.MewUI) 框架提供自动化的属性绑定与可观察值生成能力。通过简单的特性标记，即可消除大量重复的样板代码。

---

## 功能特性

- **MewProperty 自动生成** — 使用 `[AsMewProperty]` 自动注册 `MewProperty<T>` 依赖属性，并生成 `GetValue`/`SetValue` 属性访问器与 `OnXxxChanged` 钩子方法。
- **ObservableValue 自动生成** — 使用 `[AsObservableValue]` 自动为属性生成 `ObservableValue<T>` 包装，支持类级别批量生成。
- **Partial 钩子方法** — 自动生成 `OnXxxChanging` / `OnXxxChanged` 等 partial 方法，方便插入自定义逻辑。
- **编译时生成** — 基于 Roslyn Incremental Generator，零运行时开销，编译期完成所有代码生成。
- **诊断提示** — 内置编译器诊断（OBS001–OBS004），对缺少 `partial`、缺少 `setter`、命名冲突等问题给出实时警告或错误。

---

## 安装

通过 NuGet 安装：

```bash
dotnet add package MewUI.Generator
```

> 该包作为 Analyzer / Source Generator 引用，不会添加到项目运行时依赖中。

---

## 快速开始

### 1. 使用 `[AsMewProperty]` 生成 MewProperty

适用于继承自 `FrameworkElement` 的控件类，自动生成依赖属性注册代码。

```csharp
using Aprillz.MewUI.Controls;

public partial class MewPropertyModel : FrameworkElement
{
    [AsMewProperty(DefaultValue = "100")]
    public partial string Name { get; set; }

    [AsMewProperty(DefaultValue = 50, MewPropertyOptions = MewPropertyOptions.BindsTwoWayByDefault)]
    public partial int Count { get; set; }

    [AsMewProperty(true)]
    public partial bool IsEnabled { get; set; }
}
```

**生成结果（等效代码）：**

```csharp
partial class MewPropertyModel
{
    public static readonly MewProperty<string> NameProperty =
        MewProperty<string>.Register<MewPropertyModel>(
            nameof(Name), "100", MewPropertyOptions.AffectsRender,
            static (self, old, newValue) => self.OnNameChanged(old, newValue));

    public partial string Name
    {
        get => GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    partial void OnNameChanged(string oldValue, string newValue);
    // ... 其他属性类似
}
```

#### `[AsMewProperty]` 参数说明

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `DefaultValue` | `object?` | `null` | 属性默认值，可通过构造函数位置参数或命名参数传入 |
| `MewPropertyOptions` | `MewPropertyOptions` | `AffectsRender` | 属性行为选项，如 `BindsTwoWayByDefault` 等 |

---

### 2. 使用 `[AsObservableValue]` 生成 ObservableValue

适用于 ViewModel 等需要属性变更通知的场景，将普通属性包装为 `ObservableValue<T>`。

#### 方式一：类级别标记（推荐）

在类上标记 `[AsObservableValue]`，自动为所有具有 `get` + `set` 的属性生成 `ObservableValue`。

```csharp
[AsObservableValue]
public partial class MyViewModel
{
    public string? Host { get; set; }
    public int Port { get; set; } = 8080;

    // 可选：实现 partial 钩子方法
    partial void OnHostChanging(string? value)
    {
        Console.WriteLine($"[Changing] Host: {Host} -> {value}");
    }

    partial void OnHostChanged(string? value)
    {
        Console.WriteLine($"[Changed]  Host = {value}");
    }
}
```

#### 方式二：属性级别标记

仅对标记了 `[AsObservableValue]` 的属性生成。

```csharp
public partial class MyViewModel
{
    [AsObservableValue]
    public string? Host { get; set; }

    [AsObservableValue]
    public int Port { get; set; } = 8080;
}
```

**生成结果（等效代码）：**

```csharp
partial class MyViewModel
{
    [JsonIgnore]
    public ObservableValue<string?> HostObservable =>
        field ??= new(this.Host, value =>
        {
            OnHostChanging(value);
            Host = value;
            OnHostChanged(value);
            return value;
        });

    partial void OnHostChanging(string? value);
    partial void OnHostChanged(string? value);

    // Port 同理...
}
```

**使用方式：**

```csharp
var vm = new MyViewModel { Host = "localhost", Port = 3000 };

var hostObs = vm.HostObservable;
Console.WriteLine($"Host: {hostObs.Value}");  // "localhost"

hostObs.Value = "example.com";  // 自动写回 vm.Host
```

---

## 编译器诊断

| 代码 | 级别 | 说明 |
|------|------|------|
| OBS001 | Warning | 类缺少 `partial` 修饰符 |
| OBS002 | Warning | 属性缺少 `setter`，无法生成代码 |
| OBS003 | Error | 类中已存在同名 `XxxObservable` 属性，产生命名冲突 |
| OBS004 | Warning | 类已标记 `[AsObservableValue]`，属性上的标记是多余的 |

---

## 项目结构

```
MewUI.Generator/
├── MewPropertyIncrementalGenerator.cs    # [AsMewProperty] 源代码生成器
├── ObservableValueIncrementalGenerator.cs # [AsObservableValue] 源代码生成器
├── AttributeDataExtensions.cs             # Attribute 参数读取工具扩展
└── MewUI.Generator.csproj                 # 项目配置（netstandard2.0 / Roslyn Component）

MewUI.Generator.Test/
├── MyViewModel.cs                         # 使用示例
├── Program.cs                             # 入口程序
└── MewUI.Generator.Test.csproj            # 测试项目配置
```

---

## 技术细节

- **目标框架**：`netstandard2.0`（兼容所有 .NET SDK）
- **Roslyn 版本**：基于 `Microsoft.CodeAnalysis.CSharp 5.6.0`
- **生成器类型**：`IIncrementalGenerator`（增量生成器，支持高性能增量编译）
- **依赖框架**：生成的代码依赖 [Aprillz.MewUI](https://www.nuget.org/packages/Aprillz.MewUI) 运行时库

---

## 许可证

本项目遵循 MIT 许可证。
