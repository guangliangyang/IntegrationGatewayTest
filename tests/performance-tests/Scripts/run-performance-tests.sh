#!/bin/bash
# Integration Gateway Performance Testing Script
# Usage: ./run-performance-tests.sh [TEST_MODE] [OPTIONS]

set -euo pipefail

# 默认参数
TEST_MODE="light"
BASE_URL="https://localhost:7000"
REPORT_PATH="./Reports"
BUILD_FIRST=true
OPEN_REPORTS=false
VERBOSE=false

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# 输出函数
print_header() {
    echo -e "\n${GREEN}🚀 $1${NC}"
    echo -e "${GREEN}$(printf '=%.0s' $(seq 1 $((${#1} + 4))))${NC}"
}

print_info() {
    echo -e "${CYAN}💡 $1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}" >&2
}

# 显示帮助
show_help() {
    cat << EOF
Integration Gateway Performance Testing Script

Usage: ./run-performance-tests.sh [TEST_MODE] [OPTIONS]

Test Modes:
  smoke   - Basic functionality validation (1 min)
  light   - Light load testing (5 min)
  medium  - Medium load testing (10 min) 
  heavy   - Heavy load testing (15 min)
  stress  - Stress testing with progressive load (20 min)
  cache   - Cache performance testing (10 min)
  mixed   - Mixed workload testing (15 min)

Options:
  -u, --base-url URL      Set base URL (default: https://localhost:7000)
  -r, --report-path PATH  Set report output path (default: ./Reports)
  -n, --no-build         Skip building the project
  -o, --open-reports     Open reports after completion
  -v, --verbose          Enable verbose output
  -h, --help             Show this help message

Examples:
  ./run-performance-tests.sh smoke
  ./run-performance-tests.sh medium --base-url "https://your-api.com"
  ./run-performance-tests.sh stress --open-reports --verbose
EOF
}

# 解析参数
while [[ $# -gt 0 ]]; do
    case $1 in
        smoke|light|medium|heavy|stress|cache|mixed)
            TEST_MODE="$1"
            shift
            ;;
        -u|--base-url)
            BASE_URL="$2"
            shift 2
            ;;
        -r|--report-path)
            REPORT_PATH="$2"
            shift 2
            ;;
        -n|--no-build)
            BUILD_FIRST=false
            shift
            ;;
        -o|--open-reports)
            OPEN_REPORTS=true
            shift
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            print_error "Unknown parameter: $1"
            show_help
            exit 1
            ;;
    esac
done

# 主脚本开始
main() {
    print_header "Integration Gateway Performance Tests"
    
    # 显示测试配置
    print_info "Test Configuration:"
    echo -e "  ${WHITE}📊 Test Mode: $TEST_MODE${NC}"
    echo -e "  ${WHITE}🌐 Base URL: $BASE_URL${NC}"
    echo -e "  ${WHITE}📁 Report Path: $REPORT_PATH${NC}"
    echo -e "  ${WHITE}🔨 Build First: $BUILD_FIRST${NC}"
    
    # 设置项目路径
    SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    PROJECT_PATH="$SCRIPT_DIR/../IntegrationGateway.PerformanceTests"
    PROJECT_FILE="$PROJECT_PATH/IntegrationGateway.PerformanceTests.csproj"
    
    if [[ ! -f "$PROJECT_FILE" ]]; then
        print_error "Performance test project not found at: $PROJECT_FILE"
        exit 1
    fi
    
    # 验证目标API是否可访问
    print_info "Checking API availability..."
    if curl -s --max-time 10 --fail "$BASE_URL/health" > /dev/null 2>&1; then
        print_success "API is accessible"
    else
        print_warning "Could not reach API health endpoint. Testing may fail if API is not running."
    fi
    
    # 构建项目
    if [[ "$BUILD_FIRST" == "true" ]]; then
        print_info "Building performance test project..."
        if dotnet build "$PROJECT_FILE" --configuration Release --verbosity minimal; then
            print_success "Build completed successfully"
        else
            print_error "Build failed"
            exit 1
        fi
    fi
    
    # 创建报告目录
    FULL_REPORT_PATH="$PROJECT_PATH/$REPORT_PATH"
    if [[ ! -d "$FULL_REPORT_PATH" ]]; then
        mkdir -p "$FULL_REPORT_PATH"
        print_info "Created report directory: $FULL_REPORT_PATH"
    fi
    
    # 更新配置文件中的BaseUrl
    CONFIG_FILE="$PROJECT_PATH/Config/test-config.json"
    if [[ -f "$CONFIG_FILE" ]]; then
        print_info "Updating test configuration..."
        # 使用 jq 更新 JSON 配置文件
        if command -v jq > /dev/null 2>&1; then
            jq --arg baseUrl "$BASE_URL" --arg reportPath "$REPORT_PATH" \
               '.testSettings.baseUrl = $baseUrl | .testSettings.reportOutputPath = $reportPath' \
               "$CONFIG_FILE" > "${CONFIG_FILE}.tmp" && mv "${CONFIG_FILE}.tmp" "$CONFIG_FILE"
            print_success "Configuration updated"
        else
            print_warning "jq not found. Configuration will use default values."
        fi
    fi
    
    # 运行性能测试
    print_header "Running Performance Tests"
    
    case $TEST_MODE in
        "smoke")  TEST_DESCRIPTION="Smoke Test - Basic functionality validation" ;;
        "light")  TEST_DESCRIPTION="Light Load - 10 concurrent users for 5 minutes" ;;
        "medium") TEST_DESCRIPTION="Medium Load - 50 concurrent users for 10 minutes" ;;
        "heavy")  TEST_DESCRIPTION="Heavy Load - 100 concurrent users for 15 minutes" ;;
        "stress") TEST_DESCRIPTION="Stress Test - Progressive load increase to find limits" ;;
        "cache")  TEST_DESCRIPTION="Cache Performance - Testing caching effectiveness" ;;
        "mixed")  TEST_DESCRIPTION="Mixed Workload - Realistic 80% read / 20% write scenario" ;;
        *)        TEST_DESCRIPTION="Light Load Test" ;;
    esac
    
    print_info "Test Description: $TEST_DESCRIPTION"
    print_info "Starting test execution..."
    
    # 切换到项目目录并运行测试
    cd "$PROJECT_PATH"
    
    START_TIME=$(date +%s)
    
    # 构建运行参数
    RUN_ARGS=("$TEST_MODE")
    if [[ "$VERBOSE" == "true" ]]; then
        RUN_ARGS+=("--verbose")
    fi
    
    if dotnet run --configuration Release -- "${RUN_ARGS[@]}"; then
        END_TIME=$(date +%s)
        DURATION=$((END_TIME - START_TIME))
        
        print_success "Performance test completed successfully!"
        print_info "Test Duration: $(printf '%02d:%02d:%02d' $((DURATION/3600)) $((DURATION%3600/60)) $((DURATION%60)))"
        
        # 查找生成的报告文件
        if [[ -d "$REPORT_PATH" ]]; then
            LATEST_REPORT=$(find "$REPORT_PATH" -name "*.html" -type f -exec ls -t {} + | head -1)
            
            if [[ -n "$LATEST_REPORT" ]]; then
                print_success "Latest report: $(basename "$LATEST_REPORT")"
                
                if [[ "$OPEN_REPORTS" == "true" ]]; then
                    print_info "Opening performance report..."
                    case "$(uname -s)" in
                        Darwin*) open "$LATEST_REPORT" ;;
                        Linux*)  xdg-open "$LATEST_REPORT" 2>/dev/null || true ;;
                        *)       print_warning "Cannot auto-open reports on this platform" ;;
                    esac
                fi
            fi
        fi
        
        # 显示报告摘要
        print_header "Test Results Summary"
        print_info "Check the following reports for detailed results:"
        find "$REPORT_PATH" -name "*performance-test-$TEST_MODE*" -type f 2>/dev/null | 
            while read -r report; do
                echo -e "  ${WHITE}📊 $(basename "$report")${NC}"
            done
            
    else
        print_error "Performance test failed"
        exit 1
    fi
    
    print_header "Performance Testing Complete"
    print_success "All tests have been completed successfully!"
    print_info "Reports are available in: $FULL_REPORT_PATH"
}

# 脚本结束显示帮助信息
show_available_modes() {
    echo -e "\n${CYAN}📋 Available Test Modes:${NC}"
    echo -e "  ${GRAY}smoke   - Basic functionality validation (1 min)${NC}"
    echo -e "  ${GRAY}light   - Light load testing (5 min)${NC}"
    echo -e "  ${GRAY}medium  - Medium load testing (10 min)${NC}"
    echo -e "  ${GRAY}heavy   - Heavy load testing (15 min)${NC}"
    echo -e "  ${GRAY}stress  - Stress testing with progressive load (20 min)${NC}"
    echo -e "  ${GRAY}cache   - Cache performance testing (10 min)${NC}"
    echo -e "  ${GRAY}mixed   - Mixed workload testing (15 min)${NC}"

    echo -e "\n${CYAN}📖 Usage Examples:${NC}"
    echo -e "  ${GRAY}./run-performance-tests.sh smoke${NC}"
    echo -e "  ${GRAY}./run-performance-tests.sh medium --base-url 'https://your-api.com'${NC}"
    echo -e "  ${GRAY}./run-performance-tests.sh stress --open-reports${NC}"
}

# 执行主函数
main "$@"

# 显示可用模式
show_available_modes