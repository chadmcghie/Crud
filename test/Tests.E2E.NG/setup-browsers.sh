#!/bin/bash

# Browser installation script with multiple fallback strategies
# Addresses the browser download issues identified in e2e testing

echo "🔧 E2E Test Browser Setup Script"
echo "================================="

# Function to check if a browser is available
check_browser() {
    local browser=$1
    if command -v "$browser" &> /dev/null; then
        echo "✅ $browser is available"
        return 0
    else
        echo "❌ $browser not found"
        return 1
    fi
}

# Function to check playwright browser installation
check_playwright_browsers() {
    echo "📋 Checking Playwright browser installation..."
    
    local chromium_path="$HOME/.cache/ms-playwright/chromium-*/chrome-linux/chrome"
    local firefox_path="$HOME/.cache/ms-playwright/firefox-*/firefox/firefox"
    
    if ls $chromium_path 1> /dev/null 2>&1; then
        echo "✅ Playwright Chromium found"
    else
        echo "❌ Playwright Chromium not found"
    fi
    
    if ls $firefox_path 1> /dev/null 2>&1; then
        echo "✅ Playwright Firefox found"
    else
        echo "❌ Playwright Firefox not found"
    fi
}

# Check system browsers
echo "🌐 Checking System Browsers..."
check_browser "google-chrome"
check_browser "chromium-browser" 
check_browser "firefox"

# Check playwright browsers
check_playwright_browsers

echo ""
echo "🚀 Attempting Browser Installation Strategies..."

# Strategy 1: Standard Playwright Install
echo "📥 Strategy 1: Standard Playwright install..."
if npx playwright install chromium firefox 2>/dev/null; then
    echo "✅ Standard installation successful"
    exit 0
else
    echo "❌ Standard installation failed"
fi

# Strategy 2: Install with dependencies
echo "📥 Strategy 2: Install with system dependencies..."
if npx playwright install-deps && npx playwright install chromium firefox 2>/dev/null; then
    echo "✅ Installation with dependencies successful"
    exit 0
else
    echo "❌ Installation with dependencies failed"
fi

# Strategy 3: Individual browser installation
echo "📥 Strategy 3: Individual browser attempts..."
for browser in chromium firefox; do
    echo "  Trying $browser..."
    if timeout 300 npx playwright install "$browser" 2>/dev/null; then
        echo "  ✅ $browser installed successfully"
    else
        echo "  ❌ $browser installation failed"
    fi
done

# Strategy 4: Alternative download mirrors (if needed)
echo "📥 Strategy 4: Check for alternative installation methods..."
echo "💡 Consider using system package manager as fallback:"
echo "   sudo apt-get install chromium-browser firefox"

# Final status check
echo ""
echo "📊 Final Browser Status:"
check_browser "google-chrome"
check_browser "chromium-browser"
check_browser "firefox"
check_playwright_browsers

echo ""
echo "💡 NEXT STEPS:"
echo "1. If system browsers are available, use playwright.config.system-browsers.ts"
echo "2. If playwright browsers failed, check network connectivity"
echo "3. Consider running tests with --project=api-tests for API-only testing"
echo "4. For webkit, install missing system dependencies first"
