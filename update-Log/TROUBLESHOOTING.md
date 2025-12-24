# RimWorld 游戏框架 - 故障排除指南

## 🚨 run-demo.bat 启动失败解决方案

### ⚠️ 主要问题：.NET SDK 未安装

根据诊断结果，主要问题是系统中没有安装 .NET 8.0 SDK。这是运行所有 C# 项目的必要条件。

### 🔧 立即解决方案

#### 方案 1: 安装 .NET SDK (推荐)
1. 访问 [.NET 8.0 下载页面](https://dotnet.microsoft.com/download/dotnet/8.0)
2. 下载并安装 ".NET 8.0 SDK" (不是运行时)
3. 重启命令提示符
4. 验证安装: `dotnet --version`
5. 运行: `run-demo.bat`

#### 方案 2: GUI构建失败修复
如果.NET SDK已安装但GUI项目构建失败：
```bash
fix-gui-build.bat
```
这个脚本会：
- 清理构建缓存
- 恢复NuGet包
- 检查文件完整性
- 尝试重新构建

#### 方案 3: 详细构建诊断
```bash
diagnose-build-error.bat
```
获取详细的构建错误信息和解决建议。

#### 方案 4: 使用预编译版本 (如果可用)
```bash
# 检查是否有预编译的可执行文件
run-precompiled.bat
```

#### 方案 5: 在线演示
如果无法安装 .NET SDK，可以查看项目文档和截图：
- `README-GameWorld.md` - 游戏世界功能说明
- `README-GUI.md` - 图形界面说明  
- `docs/` 目录 - 完整技术文档

### 第一步：运行诊断
```bash
debug-gui.bat
```
这会检查所有必要的文件和环境。

### 第二步：尝试不同的启动方式

#### 选项 1: 最小化测试
```bash
run-minimal.bat
```
- 最简单的启动方式
- 直接运行，无额外检查

#### 选项 2: 基础功能测试  
```bash
test-basic.bat
```
- 测试WPF基础功能
- 确认图形界面能否工作

#### 选项 3: 简化GUI版本
```bash
run-simple-gui.bat
```
- 使用简化的界面
- 更稳定的版本

#### 选项 4: 游戏世界演示
```bash
run-game-world.bat
```
- 直接启动可视化世界
- 最新功能展示

#### 选项 5: 控制台版本
```bash
run-console-demo.bat
```
- 纯文本界面
- 最兼容的版本

### 常见错误和解决方案

#### 错误 1: "未找到 .NET SDK"
**解决方案:**
1. 下载安装 [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. 重启命令提示符
3. 验证: `dotnet --version`

#### 错误 2: "找不到项目文件"
**解决方案:**
1. 确保在正确目录运行 (包含 RimWorldFramework.sln 的目录)
2. 检查文件路径: `src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj`

#### 错误 3: "构建失败"
**解决方案:**
```bash
# 方法1: 使用修复工具
fix-gui-build.bat

# 方法2: 手动清理并重新构建
dotnet clean
dotnet restore
dotnet build

# 方法3: 详细诊断
diagnose-build-error.bat
```

#### 错误 3.1: "GUI项目构建失败"
**具体解决方案:**
1. 检查文件完整性: `check-gui-files.bat`
2. 运行修复工具: `fix-gui-build.bat`
3. 查看详细错误: `diagnose-build-error.bat`
4. 确认XAML文件语法正确
5. 检查所有引用的类是否存在

#### 错误 4: "WPF 不支持"
**解决方案:**
- 确保使用 Windows 10+ 
- 确保安装了完整的 .NET SDK (不是运行时)
- 尝试控制台版本: `run-console-demo.bat`

#### 错误 5: "权限被拒绝"
**解决方案:**
- 以管理员身份运行命令提示符
- 检查防病毒软件设置
- 确保项目文件夹有写入权限

### 详细诊断步骤

#### 步骤 1: 环境检查
```bash
# 检查 .NET 版本
dotnet --version

# 检查项目文件
dir src\RimWorldFramework.GUI\*.csproj

# 检查当前目录
echo %CD%
```

#### 步骤 2: 手动构建测试
```bash
# 恢复包
dotnet restore src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj

# 构建项目
dotnet build src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --verbosity normal

# 运行项目
dotnet run --project src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj
```

#### 步骤 3: 检查依赖项
```bash
# 列出项目引用
dotnet list src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj reference

# 检查包引用
dotnet list src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj package
```

### 启动脚本优先级

按推荐顺序尝试：

1. **launcher.bat** - 智能启动器，检查系统状态
2. **debug-gui.bat** - 运行完整系统诊断
3. **fix-gui-build.bat** - 修复GUI构建问题
4. **test-gui-build.bat** - 快速构建测试
5. **run-minimal.bat** - 最简单测试
6. **test-basic.bat** - WPF功能测试  
7. **run-simple-gui.bat** - 简化GUI
8. **run-game-world.bat** - 游戏世界
9. **run-console-demo.bat** - 控制台备用

### GUI构建问题专门解决方案

如果遇到"GUI项目构建失败"错误：

1. **快速测试**: `test-gui-build.bat`
2. **文件检查**: `check-gui-files.bat`
3. **自动修复**: `fix-gui-build.bat`
4. **详细诊断**: `diagnose-build-error.bat`

### 系统要求检查

#### 最低要求
- ✅ Windows 10 或更高版本
- ✅ .NET 8.0 SDK
- ✅ 至少 4GB RAM
- ✅ 支持 WPF 的显卡

#### 验证命令
```bash
# 检查 Windows 版本
ver

# 检查 .NET 版本  
dotnet --version

# 检查内存
wmic computersystem get TotalPhysicalMemory
```

### 高级故障排除

#### 清理项目
```bash
# 删除构建缓存
rmdir /s /q src\RimWorldFramework.GUI\bin
rmdir /s /q src\RimWorldFramework.GUI\obj

# 重新构建
dotnet restore
dotnet build
```

#### 检查日志
```bash
# 详细构建日志
dotnet build --verbosity diagnostic > build.log 2>&1

# 查看日志文件
notepad build.log
```

#### 重置环境
```bash
# 清理 NuGet 缓存
dotnet nuget locals all --clear

# 重新安装工具
dotnet tool restore
```

### 获取帮助

如果所有方法都失败：

1. **运行完整诊断**: `debug-gui.bat`
2. **保存输出**: 复制所有错误信息
3. **检查系统**: 确认 Windows 版本和 .NET 安装
4. **尝试备用方案**: 使用 `run-console-demo.bat`

### 成功指标

程序正常启动的标志：
- ✅ 出现图形界面窗口
- ✅ 可以点击按钮
- ✅ 没有错误对话框
- ✅ 可以进入游戏世界或演示

---

**记住**: 如果图形界面有问题，控制台版本 (`run-console-demo.bat`) 总是可用的备选方案！