# CI/CD (持续集成/持续部署) 指南

这份文档旨在介绍 CI/CD 的基本概念，分析当前的 GitHub Actions 工作流，并展示一个更高级、更完整的自动化流程示例。

> **说明**: 根据您的请求，本文档旨在介绍 CI/CD。原文件名请求为 `cicd.mk`，但通常文档使用 Markdown 格式 (`.md`)，因此生成为 `CICD.md` 以便获得更好的阅读体验。

---

## 1. 什么是 CI/CD？

*   **CI (Continuous Integration - 持续集成)**:  每当开发人员提交代码时，自动构建并测试项目，尽早发现集成错误。
*   **CD (Continuous Delivery/Deployment - 持续交付/部署)**:  自动将经过测试的代码发布到生产环境或生成可发布的安装包。

**核心价值**:
*   减少手动重复劳动。
*   防止“在我机器上能运行”的问题。
*   加快发布周期。

---

## 2. 当前工作流分析 (`.github/workflows/main.yml`)

您当前的工作流主要包含以下步骤：
1.  **触发器 (Triggers)**: `push` 和 `pull_request` 事件。
2.  **环境准备**: 定义了 Windows 环境 (`windows-latest`)，配置 MSBuild, NuGet, VSTest。
3.  **构建与测试**: 还原包 -> 编译 (Release) -> 运行测试。
4.  **产物保存**: 使用 `upload-artifact` 保存编译后的文件。

这是一个非常标准的**CI (持续集成)** 流程，确保了代码能编译且通过测试。

---

## 3. 进阶工作流：更复杂的场景

一个更成熟、面向生产环境的工作流通常会包含以下增强特性：

### 3.1 性能优化：缓存 (Caching)
MSBuild 和 NuGet 还原可能会很慢。通过缓存 `~/.nuget/packages`，可以显著减少下载依赖的时间。

### 3.2 代码质量检查 (Linting & Analysis)
在编译之前，先检查代码风格（如 `dotnet format`）或进行静态分析，确保代码质量。

### 3.3 自动版本号 (Versioning)
根据 Git Tag 或 Commit 自动生成版本号（例如使用 `MinVer` 或 `GitVersion`），而不是手动修改 `Assemblyinfo.cs`。

### 3.4 自动化发布 (Release Automation)
当向仓库推送特定 Tag（如 `v1.0.0`）时，自动：
*   构建 Release 版本。
*   打包 (Zip/Installer)。
*   创建 GitHub Release 页面。
*   上传构建产物到 Release 中供用户下载。

### 3.5 矩阵构建 (Matrix Builds) [可选]
如果您的库需要支持多个 .NET 版本或操作系统，可以并行运行多个构建任务。

---

## 4. 进阶工作流示例

以下是一个更完整的 `advanced_cicd.yml` 示例。它增加了**缓存**、**版本控制**和**自动发布**的功能。

```yaml
name: Advanced Build and Release

on:
  push:
    branches: [ "master", "main" ]
    tags: [ "v*" ] # 仅在推送 v 开头的 tag 时触发发布流程
  pull_request:
    branches: [ "master", "main" ]

env:
  SOLUTION_NAME: Main\FluorescenceFullAutomatic.sln
  TEST_DLL: TestMain\bin\Release\TestMain.dll

jobs:
  build-and-test:
    name: Build & Test
    runs-on: windows-latest
    
    outputs: # 将版本号传递给下一个 job
      version: ${{ steps.get_version.outputs.VERSION }}

    steps:
    - name: Checkout Code
      uses: actions/checkout@v4
      with:
        fetch-depth: 0 # 获取完整历史以生成正确的版本号

    # 1. 缓存 NuGet 包 (加速构建)
    - name: Cache NuGet packages
      uses: actions/cache@v3
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.packages.config', '**/*.csproj') }}
        restore-keys: |
          ${{ runner.os }}-nuget-

    - name: Setup MSBuild
      uses: microsoft/setup-msbuild@v2

    - name: Setup NuGet
      uses: NuGet/setup-nuget@v2

    - name: Setup VSTest
      uses: darenm/Setup-VSTest@v1

    - name: Restore NuGet Packages
      run: nuget restore ${{ env.SOLUTION_NAME }}

    # 2. 提取版本号 (简单示例: 从 Tag 提取，或者默认年月日)
    - name: Get Version
      id: get_version
      shell: pwsh
      run: |
        if ($env:GITHUB_REF -match 'refs/tags/v(.+)') {
          $version = $matches[1]
        } else {
          $version = "1.0.0-build." + $env:GITHUB_RUN_NUMBER
        }
        echo "VERSION=$version" >> $env:GITHUB_OUTPUT
        echo "Detected Version: $version"

    # 3. 编译 (注入版本号) - 注意: 此时需要在 csproj 中支持 Version 属性，或使用 AssemblyInfo 修改工具
    - name: Build Solution
      run: msbuild ${{ env.SOLUTION_NAME }} /p:Configuration=Release /p:Platform="Any CPU" /p:Version=${{ steps.get_version.outputs.VERSION }}

    - name: Run Tests
      run: vstest.console.exe ${{ env.TEST_DLL }}

    # 4. 打包产物
    - name: Zip Release
      run: |
        powershell Compress-Archive -Path Main\bin\Release\* -DestinationPath FluorescenceFullAutomatic-${{ steps.get_version.outputs.VERSION }}.zip

    - name: Upload Build Artifact
      uses: actions/upload-artifact@v4
      with:
        name: binary-package
        path: FluorescenceFullAutomatic-${{ steps.get_version.outputs.VERSION }}.zip

  # 5. 发布 Job (仅在 Tag 时运行)
  create-release:
    name: Create GitHub Release
    needs: build-and-test
    if: startsWith(github.ref, 'refs/tags/v') # 仅 Tag 触发
    runs-on: ubuntu-latest # 发布步骤通常在 Linux 上更快且便宜，除非必须用 Windows 命令
    permissions:
      contents: write # 需要写权限来创建 Release

    steps:
    - name: Download Artifact
      uses: actions/download-artifact@v4
      with:
        name: binary-package

    - name: Create Release
      uses: softprops/action-gh-release@v1
      with:
        files: |
          *.zip
        draft: false
        prerelease: false
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

## 总结

从**基础**到**进阶**，主要的区别在于：
1.  **自动化程度**: 包含了发布步骤，不再需要人工打包上传。
2.  **效率**: 通过缓存加快速度。
3.  **规范性**: 通过 Tag 触发发布，明确版本管理。

您可以根据项目的实际需求，逐步将这些特性添加到您的 `main.yml` 中。
