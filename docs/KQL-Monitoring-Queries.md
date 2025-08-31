# KQL Monitoring Queries

A collection of monitoring and debugging queries based on existing logs in Application Insights.

## 1\. Success/Failure Rates

### ERP Service Success Rate

```kql
// ERP service success rate for the last 1 hour
dependencies
| where name contains "ErpClient"
| where timestamp > ago(1h)
| summarize 
    Total = count(),
    Success = countif(success == true),
    Failure = countif(success == false)
| extend SuccessRate = round((Success * 100.0) / Total, 2)
| project SuccessRate, Total, Success, Failure
```

### Warehouse Service Success Rate

```kql
// Warehouse service success rate for the last 1 hour
dependencies
| where name contains "WarehouseClient"  
| where timestamp > ago(1h)
| summarize 
    Total = count(),
    Success = countif(success == true),
    Failure = countif(success == false)
| extend SuccessRate = round((Success * 100.0) / Total, 2)
| project SuccessRate, Total, Success, Failure
```

### Success Rate Trend by Time Period

```kql
// Success rate trend for the last 24 hours (grouped by hour)
dependencies
| where name contains "ErpClient" or name contains "WarehouseClient"
| where timestamp > ago(24h)
| summarize 
    Total = count(),
    Success = countif(success == true)
    by bin(timestamp, 1h), name
| extend SuccessRate = round((Success * 100.0) / Total, 2)
| render timechart
```

## 2\. Circuit Breaker States

### Circuit Breaker State Inference

```kql
// Infer circuit breaker state based on error frequency
traces
| where message contains "HTTP request failed" or message contains "Request timeout" 
| where customDimensions.Operation contains "ErpClient" or customDimensions.Operation contains "WarehouseClient"
| where timestamp > ago(10m)
| summarize ErrorCount = count() by bin(timestamp, 1m), tostring(customDimensions.Operation)
| extend CircuitBreakerState = iff(ErrorCount >= 5, "Likely Open", "Closed")
| render timechart
```

### Circuit Breaker State Change Detection

```kql
// Circuit breaker opened/reset events
traces
| where message contains "Circuit breaker opened" or message contains "Circuit breaker reset"
| where timestamp > ago(24h)
| project timestamp, message, severityLevel
| order by timestamp desc
```

## 3\. Cache Hit Ratios

### Cache Hit Rate

```kql
// Cache hit rate for the last 1 hour
traces
| where message contains "Cache hit" or message contains "Cache miss"
| where timestamp > ago(1h)
| summarize 
    Hits = countif(message contains "Cache hit"),
    Misses = countif(message contains "Cache miss")
| extend 
    Total = Hits + Misses,
    HitRatio = round((Hits * 100.0) / (Hits + Misses), 2)
| project HitRatio, Total, Hits, Misses
```

### Cache Performance by Request Type

```kql
// Cache performance for different request types
traces
| where message contains "Cache hit" or message contains "Cache miss"
| where timestamp > ago(24h)
| extend RequestType = extract(@"Cache \w+ for key: (\w+)_", 1, message)
| summarize 
    Hits = countif(message contains "Cache hit"),
    Misses = countif(message contains "Cache miss")
    by RequestType
| extend HitRatio = round((Hits * 100.0) / (Hits + Misses), 2)
| order by HitRatio desc
```

## 4\. Request Duration Analysis

### Performance Monitoring - Slow Request Analysis

```kql
// Requests exceeding a threshold in the last 1 hour
traces
| where message contains "Performance:" or message contains "Slow request" or message contains "Critical slow request"
| where timestamp > ago(1h)
| extend 
    ElapsedMs = extract(@"(\d+)ms", 1, message),
    RequestName = extract(@"Performance: (\w+)", 1, message)
| where isnotnull(ElapsedMs)
| project timestamp, RequestName, ElapsedMs = toint(ElapsedMs), severityLevel
| where ElapsedMs > 500  // Requests over 500ms
| order by ElapsedMs desc
```

### Request Duration Distribution

```kql
// Request duration distribution (histogram)
traces
| where message contains "Performance:" and message contains "completed"
| where timestamp > ago(4h)
| extend ElapsedMs = toint(extract(@"completed in (\d+)ms", 1, message))
| where isnotnull(ElapsedMs)
| summarize count() by bin(ElapsedMs, 100)  // Grouped by 100ms
| render columnchart
```

## 5\. Concurrent Operation Counts

### Idempotency Concurrent Operation Statistics

```kql
// Concurrent idempotency operations
traces
| where message contains "Idempotency" 
| where timestamp > ago(1h)
| extend Operation = case(
    message contains "starting", "start",
    message contains "completed", "complete",
    message contains "failed", "failed",
    "other"
)
| summarize count() by bin(timestamp, 5m), Operation
| render timechart
```

## 6\. Retry Analysis

### Retry Pattern Analysis

```kql
// Detect retries based on multiple dependency calls with the same operation_Id
dependencies
| where timestamp > ago(2h)
| where name contains "ErpClient" or name contains "WarehouseClient"
| summarize 
    CallCount = count(),
    FirstCall = min(timestamp),
    LastCall = max(timestamp),
    SuccessCount = countif(success == true),
    FailureCount = countif(success == false)
    by operation_Id, target
| where CallCount > 1  // Multiple calls indicate a retry
| extend 
    RetryCount = CallCount - 1,
    TotalDuration = LastCall - FirstCall,
    FinalResult = iff(SuccessCount > 0, "Success", "Failed")
| summarize 
    TotalRetryOperations = count(),
    AvgRetryCount = avg(RetryCount),
    MaxRetryCount = max(RetryCount)
    by target, FinalResult
| order by TotalRetryOperations desc
```

## 7\. Timeout Analysis

### Timeout Detection and Configuration Analysis

```kql
// Timeout events and configuration information
traces
| where message contains "Request timeout"
| where timestamp > ago(4h)
| extend 
    Operation = extract(@"timeout while (.+?) -", 1, message),
    ConfiguredTimeout = toint(extract(@"configured timeout: (\d+)s", 1, message))
| summarize 
    TimeoutCount = count(),
    AvgConfiguredTimeout = avg(ConfiguredTimeout)
    by Operation
| order by TimeoutCount desc
```

## 8\. End-to-End Request Correlation

### End-to-End Request Tracing

```kql
// Correlate the full request chain using operation_Id
requests
| where timestamp > ago(1h)
| join (dependencies | where timestamp > ago(1h)) on operation_Id
| join (traces | where timestamp > ago(1h)) on operation_Id
| project 
    timestamp, 
    operation_Id,
    RequestName = name,
    DependencyName = name1, 
    DependencyDuration = duration,
    LogMessage = message
| order by timestamp desc
```

## 9\. Error Pattern Analysis

### Error Patterns and Frequency

```kql
// Error pattern analysis
traces
| where severityLevel >= 3  // Warning and above
| where timestamp > ago(24h)
| extend ErrorPattern = case(
    message contains "timeout", "Timeout",
    message contains "HTTP request failed", "Network",
    message contains "Circuit breaker", "CircuitBreaker",
    message contains "validation", "Validation",
    "Other"
)
| summarize ErrorCount = count() by bin(timestamp, 1h), ErrorPattern
| render timechart
```

## 10\. Dashboard Queries

### Real-Time Monitoring Dashboard

```kql
// Overall health metrics (suitable for dashboards)
let timeRange = 1h;
let SuccessRate = dependencies
    | where timestamp > ago(timeRange)
    | summarize SuccessRate = round(countif(success == true) * 100.0 / count(), 2);
let CacheHitRate = traces
    | where message contains "Cache hit" or message contains "Cache miss"
    | where timestamp > ago(timeRange)
    | summarize HitRate = round(countif(message contains "Cache hit") * 100.0 / count(), 2);
let AvgResponseTime = traces
    | where message contains "Performance:" and message contains "completed"
    | where timestamp > ago(timeRange)
    | extend ElapsedMs = toint(extract(@"completed in (\d+)ms", 1, message))
    | summarize AvgMs = round(avg(ElapsedMs), 0);
SuccessRate | extend Metric = "Success Rate %" 
| union (CacheHitRate | extend Metric = "Cache Hit Rate %")
| union (AvgResponseTime | extend Metric = "Avg Response Time (ms)")
```

-----

## Instructions for Use

1.  **Adjust Time Range**: `ago(1h)` in all queries can be changed to `ago(24h)`, `ago(7d)`, etc.
2.  **Customize Thresholds**: Adjust performance thresholds (e.g., 500ms) as needed.
3.  **Filter Services**: Filter specific services using conditions like `name contains "ErpClient"`.
4.  **Visualization**: Most queries support visualization using `render timechart`, `render columnchart`, etc.

## Integration Recommendations

  - Save common queries as Application Insights workbooks.
  - Set up alert rules based on these queries.
  - Regularly review and optimize query performance.