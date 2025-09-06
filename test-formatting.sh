#!/bin/bash
# Quick test script to verify formatting in Linux environment

echo "🔍 Testing formatting in Linux environment (matching CI)"
echo "================================================"

# Run dotnet format and capture the exit code
dotnet format solutions/Crud.sln --verify-no-changes --verbosity quiet
FORMAT_EXIT_CODE=$?

if [ $FORMAT_EXIT_CODE -eq 0 ]; then
    echo "✅ Formatting check passed!"
    exit 0
else
    echo "❌ Formatting check failed (exit code: $FORMAT_EXIT_CODE)"
    echo ""
    echo "Running format to see what changes are needed:"
    dotnet format solutions/Crud.sln --verbosity diagnostic
    echo ""
    echo "Checking for differences:"
    git diff --name-only
    exit 1
fi