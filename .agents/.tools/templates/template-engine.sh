#!/bin/bash
# Agent OS Template Engine
# Bash implementation for template variable substitution

set -e

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Case transformation functions
to_pascal_case() {
    echo "$1" | sed -r 's/(^|[-_ ])([a-z])/\U\2/g'
}

to_camel_case() {
    local pascal=$(to_pascal_case "$1")
    echo "${pascal,}"  # Lowercase first character
}

to_snake_case() {
    echo "$1" | sed -r 's/([A-Z])([A-Z][a-z])/\1_\2/g' | \
                sed -r 's/([a-z0-9])([A-Z])/\1_\2/g' | \
                sed 's/[-\s]/_/g' | \
                tr '[:upper:]' '[:lower:]'
}

to_kebab_case() {
    echo "$1" | sed -r 's/([A-Z])([A-Z][a-z])/\1-\2/g' | \
                sed -r 's/([a-z0-9])([A-Z])/\1-\2/g' | \
                sed 's/[_\s]/-/g' | \
                tr '[:upper:]' '[:lower:]'
}

to_upper_case() {
    echo "$1" | tr '[:lower:]' '[:upper:]'
}

to_lower_case() {
    echo "$1" | tr '[:upper:]' '[:lower:]'
}

# Process template with variables
process_template() {
    local template="$1"
    local result="$template"
    
    # Find all variable placeholders
    while IFS= read -r match; do
        # Extract variable name and transformation
        local var_full=$(echo "$match" | sed 's/{{//' | sed 's/}}//')
        local var_name=$(echo "$var_full" | cut -d':' -f1 | tr -d ' ')
        local transform=$(echo "$var_full" | grep ':' | cut -d':' -f2 | tr -d ' ' || echo "")
        
        # Get variable value
        local var_key="VAR_${var_name}"
        local value="${!var_key:-}"
        
        if [ -z "$value" ]; then
            echo -e "${YELLOW}Warning: Variable not found: $var_name${NC}" >&2
            continue
        fi
        
        # Apply transformation
        case "$transform" in
            "PascalCase"|"pascalCase")
                value=$(to_pascal_case "$value")
                ;;
            "camelCase")
                value=$(to_camel_case "$value")
                ;;
            "snake_case")
                value=$(to_snake_case "$value")
                ;;
            "kebab-case")
                value=$(to_kebab_case "$value")
                ;;
            "UPPERCASE")
                value=$(to_upper_case "$value")
                ;;
            "lowercase")
                value=$(to_lower_case "$value")
                ;;
            "")
                # No transformation
                ;;
            *)
                echo -e "${YELLOW}Warning: Unknown transformation: $transform${NC}" >&2
                ;;
        esac
        
        # Replace in template
        result=$(echo "$result" | sed "s|{{$var_full}}|$value|g")
    done < <(echo "$template" | grep -o '{{[^}]*}}' | sort -u)
    
    echo "$result"
}

# Validate template
validate_template() {
    local template="$1"
    local errors=()
    
    # Check for unclosed placeholders
    if echo "$template" | grep -q '{{[^}]*$'; then
        errors+=("Unclosed placeholder detected")
    fi
    
    # Check for empty placeholders
    if echo "$template" | grep -q '{{}}'; then
        errors+=("Empty placeholder detected")
    fi
    
    # Check for nested placeholders
    if echo "$template" | grep -q '{{.*{{'; then
        errors+=("Nested placeholders are not supported")
    fi
    
    if [ ${#errors[@]} -gt 0 ]; then
        for error in "${errors[@]}"; do
            echo -e "${RED}Error: $error${NC}" >&2
        done
        return 1
    fi
    
    return 0
}

# Usage function
usage() {
    cat << EOF
Usage: $0 -t TEMPLATE_FILE [-o OUTPUT_FILE] [-v NAME=VALUE ...]

Options:
    -t TEMPLATE_FILE    Path to template file (required)
    -o OUTPUT_FILE      Path to output file (optional, defaults to stdout)
    -v NAME=VALUE       Variable definition (can be used multiple times)
    -h                  Show this help message

Examples:
    $0 -t template.md -v ENTITY_NAME=Product
    $0 -t template.md -o output.md -v ENTITY_NAME=Product -v API_PATH=/api/products
EOF
}

# Parse command line arguments
TEMPLATE_FILE=""
OUTPUT_FILE=""
declare -A VARIABLES

while getopts "t:o:v:h" opt; do
    case $opt in
        t)
            TEMPLATE_FILE="$OPTARG"
            ;;
        o)
            OUTPUT_FILE="$OPTARG"
            ;;
        v)
            # Parse NAME=VALUE
            if [[ "$OPTARG" =~ ^([^=]+)=(.*)$ ]]; then
                VAR_NAME="${BASH_REMATCH[1]}"
                VAR_VALUE="${BASH_REMATCH[2]}"
                # Export as VAR_NAME for template processing
                export "VAR_${VAR_NAME}=${VAR_VALUE}"
            else
                echo -e "${RED}Error: Invalid variable format. Use NAME=VALUE${NC}" >&2
                exit 1
            fi
            ;;
        h)
            usage
            exit 0
            ;;
        \?)
            echo -e "${RED}Invalid option: -$OPTARG${NC}" >&2
            usage
            exit 1
            ;;
    esac
done

# Check required arguments
if [ -z "$TEMPLATE_FILE" ]; then
    echo -e "${RED}Error: Template file is required${NC}" >&2
    usage
    exit 1
fi

# Check if template file exists
if [ ! -f "$TEMPLATE_FILE" ]; then
    echo -e "${RED}Error: Template file not found: $TEMPLATE_FILE${NC}" >&2
    exit 1
fi

# Read template content
template_content=$(cat "$TEMPLATE_FILE")

# Validate template
if ! validate_template "$template_content"; then
    exit 1
fi

# Process template
processed_content=$(process_template "$template_content")

# Output result
if [ -n "$OUTPUT_FILE" ]; then
    # Create directory if needed
    output_dir=$(dirname "$OUTPUT_FILE")
    if [ -n "$output_dir" ] && [ ! -d "$output_dir" ]; then
        mkdir -p "$output_dir"
    fi
    
    # Write to file
    echo "$processed_content" > "$OUTPUT_FILE"
    echo -e "${GREEN}Template processed and saved to: $OUTPUT_FILE${NC}"
else
    # Output to stdout
    echo "$processed_content"
fi