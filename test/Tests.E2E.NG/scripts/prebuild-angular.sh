#!/bin/bash

# Pre-build Angular to speed up test startup
echo "🔨 Pre-building Angular application for faster test startup..."

cd ../../src/Angular

# Install dependencies if needed
if [ ! -d "node_modules" ]; then
  echo "📦 Installing Angular dependencies..."
  npm ci
fi

# Clean previous build
echo "🧹 Cleaning previous build..."
rm -rf dist

# Run optimized test build
echo "🚀 Building Angular with test configuration..."
npm run build -- --configuration=test

if [ $? -eq 0 ]; then
  echo "✅ Angular pre-build complete. Tests should start much faster now."
  echo "📁 Build output: $(pwd)/dist/angular/browser"
else
  echo "❌ Angular build failed. Tests will fall back to development server."
  exit 1
fi