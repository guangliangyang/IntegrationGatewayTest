# Integration Gateway Performance Tests

这个文件夹包含了Integration Gateway项目的完整性能测试方案，使用NBomber进行负载测试。

## 🏗️ 测试架构

```
performance-tests/
├── IntegrationGateway.PerformanceTests/  # NBomber测试项目
│   ├── Scenarios/                        # 测试场景
│   │   ├── BaseScenarios.cs             # 基础API测试场景
│   │   └── ProductsApiScenarios.cs      # 产品API专项测试
│   ├── Config/                          # 配置文件
│   │   ├── test-config.json             # 测试配置
│   │   └── load-profiles.json           # 负载配置文件
│   ├── Utils/                           # 工具类
│   │   ├── TestConfiguration.cs         # 配置管理
│   │   └── HttpClientExtensions.cs      # HTTP客户端扩展
│   ├── Reports/                         # 测试报告输出
│   └── Program.cs                       # 主程序入口
└── Scripts/                             # 运行脚本
    ├── run-performance-tests.ps1        # PowerShell运行脚本
    └── run-performance-tests.sh         # Bash运行脚本
```

## 🎯 测试场景

### 基础测试场景

1. **GET Products List** - 获取产品列表
2. **GET Product by ID** - 根据ID获取单个产品
3. **POST Product** - 创建新产品
4. **PUT Product** - 更新产品信息

### 高级测试场景

1. **缓存性能测试** - 重复请求测试缓存效果
2. **混合工作负载** - 80%读取 + 20%写入的真实业务场景
3. **API版本对比** - V1和V2 API性能对比

## 🚀 快速开始

### 1. 启动Integration Gateway API

确保你的API服务正在运行：

```bash
# 在项目根目录
cd src/IntegrationGateway.Api
dotnet run
```

默认运行在 `https://localhost:7000`

### 2. 运行性能测试

#### 使用PowerShell（推荐Windows用户）

```powershell
# 基本测试
./Scripts/run-performance-tests.ps1

# 指定测试模式
./Scripts/run-performance-tests.ps1 medium

# 自定义API地址
./Scripts/run-performance-tests.ps1 heavy -BaseUrl "https://your-api.com"

# 完成后自动打开报告
./Scripts/run-performance-tests.ps1 light -OpenReports
```

#### 使用Bash（推荐Linux/macOS用户）

```bash
# 基本测试
./Scripts/run-performance-tests.sh

# 指定测试模式
./Scripts/run-performance-tests.sh medium

# 自定义API地址
./Scripts/run-performance-tests.sh heavy --base-url "https://your-api.com"

# 完成后自动打开报告
./Scripts/run-performance-tests.sh light --open-reports
```

#### 手动运行

```bash
cd performance-tests/IntegrationGateway.PerformanceTests
dotnet run -- [test-mode]
```

## 📊 测试模式详解

| 模式 | 并发用户数 | 测试时长 | 适用场景 |
|------|------------|----------|----------|
| `smoke` | 1 | 1分钟 | 基础功能验证 |
| `light` | 5-10 | 5分钟 | 开发环境快速测试 |
| `medium` | 25-50 | 10分钟 | 标准负载测试 |
| `heavy` | 50-100 | 15分钟 | 高负载测试 |
| `stress` | 10→150 | 20分钟 | 压力测试，找出系统极限 |
| `cache` | 10-20 | 10分钟 | 缓存效果专项测试 |
| `mixed` | 50 | 15分钟 | 真实业务场景混合负载 |

## 📈 关键指标解读

### 响应时间指标

- **P50 (中位数)**: 50%的请求响应时间低于此值
- **P95**: 95%的请求响应时间低于此值
- **P99**: 99%的请求响应时间低于此值

### 性能基准参考

| API操作 | 理想P95响应时间 | 可接受P95响应时间 |
|---------|----------------|-------------------|
| GET Products List | < 200ms | < 500ms |
| GET Product by ID | < 100ms | < 300ms |
| POST Product | < 300ms | < 800ms |
| PUT Product | < 300ms | < 800ms |

### 吞吐量指标

- **RPS**: 每秒请求数 (Requests Per Second)
- **目标RPS**: 根据业务需求确定
- **错误率**: 应保持在 < 0.1%

## 🔧 配置自定义

### 修改测试配置

编辑 `Config/test-config.json`:

```json
{
  "testSettings": {
    "baseUrl": "https://your-api-endpoint.com",
    "testDuration": "00:05:00",
    "reportOutputPath": "./Reports"
  },
  "testData": {
    "sampleProductIds": [
      "your-actual-product-id-1",
      "your-actual-product-id-2"
    ]
  },
  "authentication": {
    "enableAuth": true,
    "bearerToken": "your-jwt-token",
    "subscriptionKey": "your-apim-subscription-key"
  }
}
```

### 自定义负载配置

编辑 `Config/load-profiles.json` 来调整并发数和测试时长。

## 📊 报告分析

### HTML报告

- 📍 位置: `Reports/performance-test-[mode]-[timestamp].html`
- 📈 包含图表: 响应时间趋势、吞吐量分析、错误率统计
- 🔍 详细数据: 每个场景的详细指标

### CSV报告

- 📍 位置: `Reports/performance-test-[mode]-[timestamp].csv`
- 📊 用途: 数据分析、历史对比、集成到监控系统

## 🚨 故障排除

### 常见问题

1. **连接失败**
   ```
   解决方案: 确认API服务正在运行，检查防火墙设置
   ```

2. **高错误率**
   ```
   解决方案: 减少并发数，检查API日志，确认测试数据有效性
   ```

3. **内存不足**
   ```
   解决方案: 减少测试持续时间，降低并发数，增加系统内存
   ```

### 调试技巧

1. **先运行smoke测试**确保基本功能正常
2. **逐步增加负载**从light → medium → heavy
3. **监控系统资源**CPU、内存、网络使用率
4. **检查API日志**了解后端性能瓶颈

## 🎯 最佳实践

### 测试环境准备

1. **独立的测试环境**，避免影响生产
2. **稳定的网络连接**，减少网络抖动影响
3. **充足的系统资源**，确保测试结果准确

### 测试策略

1. **基准测试**：建立性能基线
2. **回归测试**：每次代码变更后运行
3. **容量规划**：根据业务增长预测进行测试
4. **监控集成**：将测试结果集成到监控系统

## 🔄 CI/CD集成

### GitHub Actions示例

```yaml
- name: Run Performance Tests
  run: |
    cd performance-tests
    ./Scripts/run-performance-tests.sh smoke
    ./Scripts/run-performance-tests.sh light
```

### 性能门禁

设置性能阈值，超过阈值时阻止部署：

```json
{
  "performanceThresholds": {
    "p95ResponseTime": 500,
    "errorRate": 0.1,
    "minThroughput": 100
  }
}
```

## 📞 支持

如果遇到问题：

1. 📖 查看本文档
2. 🔍 检查测试报告中的详细错误信息
3. 📊 分析API服务的监控指标
4. 💬 联系开发团队获取支持

---

**Happy Testing! 🚀**