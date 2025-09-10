#!/bin/bash
# Test suite for Agent OS Command Dashboard
# Tests the functionality of aos-commands.sh and dashboard.md

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Test counters
PASSED=0
FAILED=0
ERRORS=()

# Get the directory of this script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
TOOLS_DIR="$( cd "$SCRIPT_DIR/.." && pwd )"

# Test result function
write_test_result() {
  local test_name="$1"
  local success="$2"
  local error_msg="$3"
  
  if [ "$success" = "true" ]; then
    echo -e "${GREEN}✅ $test_name${NC}"
    ((PASSED++))
  else
    echo -e "${RED}❌ $test_name${NC}"
    if [ -n "$error_msg" ]; then
      echo -e "   ${YELLOW}Error: $error_msg${NC}"
    fi
    ((FAILED++))
    ERRORS+=("$test_name: $error_msg")
  fi
}

# Test if file exists
test_file_exists() {
  local file_path="$1"
  local description="$2"
  
  if [ -f "$file_path" ]; then
    write_test_result "$description" "true"
    return 0
  else
    write_test_result "$description" "false" "File not found: $file_path"
    return 1
  fi
}

# Test if function exists in script
test_function_exists() {
  local function_name="$1"
  local script_path="$2"
  
  if [ ! -f "$script_path" ]; then
    write_test_result "Function $function_name exists" "false" "Script not found"
    return 1
  fi
  
  # Source the script and check if function exists
  (
    source "$script_path" 2>/dev/null
    if type -t "$function_name" | grep -q 'function'; then
      exit 0
    else
      exit 1
    fi
  )
  
  if [ $? -eq 0 ]; then
    write_test_result "Function $function_name exists" "true"
    return 0
  else
    write_test_result "Function $function_name exists" "false" "Function not defined"
    return 1
  fi
}

# Test dashboard content
test_dashboard_content() {
  local dashboard_path="$1"
  
  if [ ! -f "$dashboard_path" ]; then
    write_test_result "Dashboard content validation" "false" "Dashboard file not found"
    return 1
  fi
  
  local all_sections_found=true
  
  # Check for required sections
  local required_sections=(
    "# Agent OS Command Dashboard"
    "## Quick Commands"
    "## Installation"
    "## Command Reference"
  )
  
  for section in "${required_sections[@]}"; do
    if grep -q "$section" "$dashboard_path"; then
      write_test_result "Dashboard contains '$section'" "true"
    else
      write_test_result "Dashboard contains '$section'" "false" "Section missing"
      all_sections_found=false
    fi
  done
  
  if [ "$all_sections_found" = "true" ]; then
    return 0
  else
    return 1
  fi
}

# Test shell script syntax
test_shell_syntax() {
  local script_path="$1"
  
  if [ ! -f "$script_path" ]; then
    write_test_result "Shell script syntax check" "false" "Script not found"
    return 1
  fi
  
  # Check bash syntax
  if bash -n "$script_path" 2>/dev/null; then
    write_test_result "Bash script syntax valid" "true"
    return 0
  else
    local error_msg=$(bash -n "$script_path" 2>&1)
    write_test_result "Bash script syntax valid" "false" "$error_msg"
    return 1
  fi
}

# Test command alias
test_command_alias() {
  local alias_name="$1"
  local script_path="$2"
  
  if [ ! -f "$script_path" ]; then
    write_test_result "Command '$alias_name' exists" "false" "Script not found"
    return 1
  fi
  
  # Source the script and check if alias/function exists
  (
    source "$script_path" 2>/dev/null
    # Check if it's defined as an alias or function
    if alias "$alias_name" >/dev/null 2>&1 || type -t "$alias_name" >/dev/null 2>&1; then
      exit 0
    else
      exit 1
    fi
  )
  
  if [ $? -eq 0 ]; then
    write_test_result "Command '$alias_name' exists" "true"
    return 0
  else
    write_test_result "Command '$alias_name' exists" "false" "Neither alias nor function found"
    return 1
  fi
}

# Main test execution
echo -e "\n${CYAN}🧪 Running Agent OS Command Dashboard Tests${NC}"
echo -e "${CYAN}$(printf '=%.0s' {1..50})${NC}"

# Test 1: Check if dashboard.md exists
echo -e "\n${YELLOW}📋 Testing Dashboard Documentation:${NC}"
dashboard_path="$TOOLS_DIR/dashboard.md"
test_file_exists "$dashboard_path" "dashboard.md exists"

# Test 2: Validate dashboard content
if [ -f "$dashboard_path" ]; then
  test_dashboard_content "$dashboard_path"
fi

# Test 3: Check if Bash script exists
echo -e "\n${YELLOW}🐧 Testing Bash Script:${NC}"
sh_path="$TOOLS_DIR/aos-commands.sh"
test_file_exists "$sh_path" "aos-commands.sh exists"

# Test 4: Validate Bash script syntax
if [ -f "$sh_path" ]; then
  test_shell_syntax "$sh_path"
  
  # Test 5: Check for required functions
  echo -e "\n${YELLOW}📦 Testing Bash Functions:${NC}"
  required_functions=(
    "aos-spec"
    "aos-tasks"
    "aos-execute"
    "aos-review"
    "aos-git"
    "aos-help"
  )
  
  for func in "${required_functions[@]}"; do
    test_function_exists "$func" "$sh_path"
  done
  
  # Test 6: Check for aliases
  echo -e "\n${YELLOW}🔗 Testing Command Aliases:${NC}"
  test_command_alias "aos" "$sh_path"
fi

# Test 7: Check if PowerShell script exists
echo -e "\n${YELLOW}🔧 Testing PowerShell Script:${NC}"
ps1_path="$TOOLS_DIR/aos-commands.ps1"
test_file_exists "$ps1_path" "aos-commands.ps1 exists"

# Test 8: Check installation instructions
echo -e "\n${YELLOW}📝 Testing Installation Instructions:${NC}"
if [ -f "$dashboard_path" ]; then
  if grep -q -E "PowerShell|Windows|\.ps1" "$dashboard_path"; then
    write_test_result "Windows installation instructions present" "true"
  else
    write_test_result "Windows installation instructions present" "false"
  fi
  
  if grep -q -E "bash|zsh|\.bashrc|\.zshrc" "$dashboard_path"; then
    write_test_result "Unix/Linux installation instructions present" "true"
  else
    write_test_result "Unix/Linux installation instructions present" "false"
  fi
fi

# Summary
echo -e "\n${CYAN}$(printf '=%.0s' {1..50})${NC}"
echo -e "${CYAN}📊 Test Summary:${NC}"
echo -e "   ${GREEN}Passed: $PASSED${NC}"
if [ $FAILED -gt 0 ]; then
  echo -e "   ${RED}Failed: $FAILED${NC}"
else
  echo -e "   ${GREEN}Failed: $FAILED${NC}"
fi

if [ $FAILED -gt 0 ]; then
  echo -e "\n${RED}❌ Test suite failed with $FAILED error(s)${NC}"
  if [ "${VERBOSE:-false}" = "true" ]; then
    echo -e "\n${YELLOW}Error Details:${NC}"
    for error in "${ERRORS[@]}"; do
      echo -e "  ${RED}- $error${NC}"
    done
  fi
  exit 1
else
  echo -e "\n${GREEN}✅ All tests passed!${NC}"
  exit 0
fi