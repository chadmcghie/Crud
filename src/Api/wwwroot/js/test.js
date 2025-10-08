/* Test JavaScript file for compression testing */
(function() {
    'use strict';
    
    /**
     * Simple utility functions for testing compression
     */
    const Utils = {
        // DOM manipulation helpers
        getElementById: function(id) {
            return document.getElementById(id);
        },
        
        getElementsByClassName: function(className) {
            return document.getElementsByClassName(className);
        },
        
        createElement: function(tagName, className, textContent) {
            const element = document.createElement(tagName);
            if (className) element.className = className;
            if (textContent) element.textContent = textContent;
            return element;
        },
        
        // Event handlers
        addEventListener: function(element, event, handler) {
            if (element && element.addEventListener) {
                element.addEventListener(event, handler, false);
            }
        },
        
        // AJAX helpers
        makeRequest: function(method, url, callback) {
            const xhr = new XMLHttpRequest();
            xhr.open(method, url, true);
            xhr.onreadystatechange = function() {
                if (xhr.readyState === 4 && xhr.status === 200) {
                    callback(xhr.responseText);
                }
            };
            xhr.send();
        },
        
        // Validation functions
        validateEmail: function(email) {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            return emailRegex.test(email);
        },
        
        validateRequired: function(value) {
            return value !== null && value !== undefined && value.trim() !== '';
        },
        
        // Formatting helpers
        formatDate: function(date) {
            return new Date(date).toLocaleDateString();
        },
        
        formatCurrency: function(amount) {
            return new Intl.NumberFormat('en-US', {
                style: 'currency',
                currency: 'USD'
            }).format(amount);
        },
        
        // Array utilities
        unique: function(array) {
            return Array.from(new Set(array));
        },
        
        groupBy: function(array, key) {
            return array.reduce(function(groups, item) {
                const group = item[key];
                groups[group] = groups[group] || [];
                groups[group].push(item);
                return groups;
            }, {});
        }
    };
    
    // Initialize when DOM is ready
    Utils.addEventListener(document, 'DOMContentLoaded', function() {
        console.log('Test JavaScript loaded and ready for compression testing');
    });
    
    // Export to global scope for testing
    window.TestUtils = Utils;
})();