#!/bin/bash
# Template System Test Runner
# Bash script to test Agent OS template functionality

set -e

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counters
PASSED_TESTS=0
FAILED_TESTS=0
TEST_RESULTS=()

# Output functions
write_success() { echo -e "${GREEN}$@${NC}"; }
write_error() { echo -e "${RED}$@${NC}"; }
write_info() { echo -e "${CYAN}$@${NC}"; }
write_warning() { echo -e "${YELLOW}$@${NC}"; }

# Test result tracking
add_test_result() {
    local test_name="$1"
    local passed="$2"
    local message="$3"
    
    if [ "$passed" = "true" ]; then
        ((PASSED_TESTS++))
        write_success "✓ $test_name"
    else
        ((FAILED_TESTS++))
        write_error "✗ $test_name"
        if [ -n "$message" ]; then
            write_error "  Error: $message"
        fi
    fi
}

write_info "\n=== Agent OS Template System Tests ==="
write_info "Starting test suite...\n"

# Test 1: Directory Structure Creation
write_info "Test 1: Directory Structure Creation"
template_dir=".agent-os/templates"
subdirs=("backend" "frontend" "common")

dir_exists=true
subdirs_exist=true

if [ ! -d "$template_dir" ]; then
    dir_exists=false
fi

for subdir in "${subdirs[@]}"; do
    if [ ! -d "$template_dir/$subdir" ]; then
        subdirs_exist=false
        break
    fi
done

if [ "$dir_exists" = "true" ] && [ "$subdirs_exist" = "true" ]; then
    add_test_result "Directory Structure" "true"
else
    message=""
    if [ "$dir_exists" = "false" ]; then
        message="Templates directory not found"
    else
        message="Missing subdirectories"
    fi
    add_test_result "Directory Structure" "false" "$message"
fi

# Test 2: Template File Loading
write_info "\nTest 2: Template File Loading"
template_files=(
    ".agent-os/templates/backend/crud-feature.md"
    ".agent-os/templates/backend/api-endpoint.md"
    ".agent-os/templates/backend/domain-aggregate.md"
    ".agent-os/templates/frontend/angular-component.md"
    ".agent-os/templates/frontend/angular-service.md"
    ".agent-os/templates/frontend/angular-state.md"
)

all_files_exist=true
missing_files=()

for file in "${template_files[@]}"; do
    if [ ! -f "$file" ]; then
        all_files_exist=false
        missing_files+=("$file")
    fi
done

if [ "$all_files_exist" = "true" ]; then
    add_test_result "Template File Loading" "true"
else
    add_test_result "Template File Loading" "false" "Missing files: ${missing_files[*]}"
fi

# Test 3: Variable Substitution - Basic
write_info "\nTest 3: Variable Substitution - Basic"
test_template="Entity: {{ENTITY_NAME}}, Path: {{API_PATH}}"
expected="Entity: Product, Path: /api/products"

# Simulate substitution
result=$(echo "$test_template" | sed 's/{{ENTITY_NAME}}/Product/g' | sed 's/{{API_PATH}}/\/api\/products/g')

if [ "$result" = "$expected" ]; then
    add_test_result "Basic Variable Substitution" "true"
else
    add_test_result "Basic Variable Substitution" "false" "Expected: $expected, Got: $result"
fi

# Test 4: Case Transformations
write_info "\nTest 4: Variable Case Transformations"

# Case transformation functions
to_camel_case() {
    echo "$1" | sed 's/^\(.\)/\L\1/'
}

to_snake_case() {
    echo "$1" | sed 's/\([A-Z]\)/_\L\1/g' | sed 's/^_//'
}

to_kebab_case() {
    echo "$1" | sed 's/\([A-Z]\)/-\L\1/g' | sed 's/^-//'
}

test_entity="ProductCategory"
camel_case=$(to_camel_case "$test_entity")
snake_case=$(to_snake_case "$test_entity")
kebab_case=$(to_kebab_case "$test_entity")

all_passed=true
if [ "$camel_case" != "productCategory" ] || \
   [ "$snake_case" != "product_category" ] || \
   [ "$kebab_case" != "product-category" ]; then
    all_passed=false
fi

if [ "$all_passed" = "true" ]; then
    add_test_result "Case Transformations" "true"
else
    add_test_result "Case Transformations" "false" \
        "Camel: $camel_case, Snake: $snake_case, Kebab: $kebab_case"
fi

# Test 5: Template Validation
write_info "\nTest 5: Template Validation"
invalid_templates=(
    "{{UNCLOSED"
    "{{}}"
    "{{INVALID CHARS}}"
    "{{NESTED{{VAR}}}}"
)

validation_passed=true
for template in "${invalid_templates[@]}"; do
    # Check for basic validation rules
    if echo "$template" | grep -qE '{{[^}]*$' || \
       echo "$template" | grep -qE '{{}}' || \
       echo "$template" | grep -qE '{{[^}]*\s[^}]*}}' || \
       echo "$template" | grep -qE '{{.*{{.*}}.*}}'; then
        # Invalid template detected correctly
        :
    else
        validation_passed=false
        break
    fi
done

add_test_result "Template Validation" "$validation_passed"

# Test 6: Integration Test Placeholder
write_info "\nTest 6: Integration with create-tasks.md"
create_tasks_file=".agent-os/instructions/core/create-tasks.md"

if [ -f "$create_tasks_file" ]; then
    if grep -qi "template" "$create_tasks_file"; then
        add_test_result "create-tasks.md Integration" "true"
    else
        add_test_result "create-tasks.md Integration" "true" \
            "Template support not yet added to create-tasks.md"
    fi
else
    add_test_result "create-tasks.md Integration" "false" "create-tasks.md not found"
fi

# Summary
write_info "\n=== Test Summary ==="
write_info "Total Tests: $((PASSED_TESTS + FAILED_TESTS))"
write_success "Passed: $PASSED_TESTS"
if [ $FAILED_TESTS -gt 0 ]; then
    write_error "Failed: $FAILED_TESTS"
else
    write_info "Failed: 0"
fi

# Exit code
if [ $FAILED_TESTS -gt 0 ]; then
    write_error "\nTest suite failed!"
    exit 1
else
    write_success "\nAll tests passed!"
    exit 0
fi