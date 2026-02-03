---
description: 编译 C# WPF 解决方案
---

## 编译整个解决方案

使用 dotnet build 编译（推荐，支持 Source Generator）：

// turbo
```powershell
dotnet build "J:\c#project\FluorescenceFullAutomatic\Main\FluorescenceFullAutomatic.sln" --configuration Debug
```

## 仅编译测试项目

// turbo
```powershell
dotnet build "J:\c#project\FluorescenceFullAutomatic\TestMain\TestMain.csproj" --configuration Debug
```

## 运行测试

// turbo
```powershell
dotnet test "J:\c#project\FluorescenceFullAutomatic\TestMain\TestMain.csproj" --no-build --verbosity normal
```

## 故障排除

如果命令行编译失败但 Visual Studio 编译成功，可能是以下原因：
1. **Source Generator 问题**: 使用 `dotnet build` 而不是 MSBuild
2. **XAML 生成错误**: 确保 XAML 文件已正确添加到项目中
3. **NuGet 包未恢复**: 运行 `dotnet restore` 先恢复包


