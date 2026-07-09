# 弱联网HTTP服务器范例

## 项目简介

这是一个用C#编写的弱联网HTTP服务器范例项目，主要用于学习弱联网服务器的实现原理。该项目采用异步非阻塞的多线程架构，支持基本的HTTP请求处理，并集成Redis作为数据存储后端。

![第一张图片](/Image/图片.png)
![第二张图片](/Image/图片2.png)

### 主要特性

- 基于.NET 9.0构建的轻量级HTTP服务器
- 异步非阻塞Socket通信
- Redis数据库集成用于数据持久化
- 支持GET/POST请求处理
- 内置简单的用户认证系统
- 道具管理系统（增删改查）
- 响应式Web界面

## 技术栈

- **编程语言**: C# (.NET 9.0)
- **数据库**: Redis (StackExchange.Redis 2.8.24)
- **网络协议**: HTTP/1.1
- **架构模式**: 异步非阻塞Socket服务器

## 环境要求

- .NET 9.0 SDK 或更高版本
- Redis 服务器 (推荐6.x及以上版本)
- Windows/Linux/macOS 操作系统

## 安装与配置

### 1. 安装Redis数据库

#### Windows系统:
1. 下载Redis for Windows: https://github.com/microsoftarchive/redis/releases
2. 解压到指定目录
3. 运行 `RedisStart.bat` 启动Redis服务

或者使用提供的批处理文件:RedisStart.bat

4. 编译项目
确保已安装.NET 9.0 SDK后，在项目根目录执行:dotnet build

5.运行服务器.
目录在---弱联网游戏HTTP协议\bin\Debug\net9.0\MyServer.exe

### 访问服务器
在浏览器中访问: http://127.0.0.1:8888
### 功能页面
* / 或 /index - 首页
* /login - 用户登录页面 (默认密码: 1234567)
* /inventory - 查看背包道具
* /add-item - 添加新道具
* /db-status - 数据库操作状态
* /notice - 公告页面
* /clear-data - 清空所有数据(危险操作)

## 项目目录结构说明

### 根目录文件

- **`README.md`** - 项目说明文档
- **`MyServer.csproj`** - .NET（C#）项目配置文件，定义项目属性、依赖包等
- **`RedisStart.bat`** - Windows下启动Redis服务的批处理脚本
- **`IserverHTTP.cs`** - HTTP服务器主程序，包含Socket监听、请求处理、路由分发等核心逻辑
- **`dbmanger.cs`** - Redis数据库管理类，提供道具数据的增删改查操作

### 子目录

#### `bin/` 
编译输出目录，存放生成的可执行文件和依赖库
- `Debug/` - 调试版本输出目录

#### `obj/`
中间编译文件目录，存放编译过程中生成的临时文件

#### `Image/`
图片资源目录
- `图片.png` - 项目相关截图或示意图
- `图片2.png` - 项目相关截图或示意图

### 主要代码文件说明

1. **`IserverHTTP.cs`** (24.4KB)
   - 主服务器类 `MainClass`
   - 客户端状态管理 `ClientState`
   - HTTP请求解析和响应生成
   - 路由处理函数（登录、背包、道具管理等）
   - 表单数据解析工具

2. **`dbmanger.cs`** (10.8KB)
   - Redis连接管理
   - 道具数据存储和检索
   - 背包系统实现
   - 数据初始化示例

### 配置文件

- **`MyServer.csproj`**
  - 目标框架: .NET 9.0
  - 输出类型: 可执行文件
  - 依赖包: StackExchange.Redis 2.8.24
  - 运行时标识: win-x64

这个弱联网HTTP服务器项目结构清晰，主要功能集中在两个核心C#文件中，通过Redis实现数据持久化，适合学习网络编程和HTTP协议实现。
