/**
 * Theme Switcher Module
 * Handles light/dark mode switching with localStorage persistence
 */

(function() {
    'use strict';

    const THEME_KEY = 'bookify-theme';
    const THEME_LIGHT = 'light';
    const THEME_DARK = 'dark';

    class ThemeSwitcher {
        constructor() {
            this.currentTheme = this.getStoredTheme() || this.getSystemTheme();
            this.init();
        }

        init() {
            // Apply theme immediately to prevent flash
            this.applyTheme(this.currentTheme);

            // Wait for DOM to be ready
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', () => this.setupListeners());
            } else {
                this.setupListeners();
            }

            // Listen for system theme changes
            this.watchSystemTheme();
        }

        setupListeners() {
            // Find all theme toggle buttons
            const toggleButtons = document.querySelectorAll('[data-theme-toggle]');
            
            toggleButtons.forEach(button => {
                button.addEventListener('click', () => this.toggleTheme());
                this.updateButtonState(button);
            });
        }

        getSystemTheme() {
            if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
                return THEME_DARK;
            }
            return THEME_LIGHT;
        }

        getStoredTheme() {
            try {
                return localStorage.getItem(THEME_KEY);
            } catch (e) {
                console.warn('localStorage not available:', e);
                return null;
            }
        }

        setStoredTheme(theme) {
            try {
                localStorage.setItem(THEME_KEY, theme);
            } catch (e) {
                console.warn('localStorage not available:', e);
            }
        }

        applyTheme(theme) {
            this.currentTheme = theme;
            
            // Update data attribute on html element
            document.documentElement.setAttribute('data-theme', theme);
            
            // Store preference
            this.setStoredTheme(theme);
            
            // Update all toggle buttons
            const toggleButtons = document.querySelectorAll('[data-theme-toggle]');
            toggleButtons.forEach(button => this.updateButtonState(button));
            
            // Dispatch custom event for other components
            window.dispatchEvent(new CustomEvent('themechange', { 
                detail: { theme } 
            }));
        }

        toggleTheme() {
            const newTheme = this.currentTheme === THEME_LIGHT ? THEME_DARK : THEME_LIGHT;
            this.applyTheme(newTheme);
        }

        updateButtonState(button) {
            const isDark = this.currentTheme === THEME_DARK;
            
            // Update aria-label
            button.setAttribute('aria-label', isDark ? 'Switch to light mode' : 'Switch to dark mode');
            
            // Update icon if using icon elements
            const lightIcon = button.querySelector('[data-theme-icon="light"]');
            const darkIcon = button.querySelector('[data-theme-icon="dark"]');
            
            if (lightIcon && darkIcon) {
                if (isDark) {
                    lightIcon.style.display = 'block';
                    darkIcon.style.display = 'none';
                } else {
                    lightIcon.style.display = 'none';
                    darkIcon.style.display = 'block';
                }
            }
        }

        watchSystemTheme() {
            if (!window.matchMedia) return;

            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
            
            // Modern browsers
            if (mediaQuery.addEventListener) {
                mediaQuery.addEventListener('change', (e) => {
                    // Only auto-switch if user hasn't set a preference
                    if (!this.getStoredTheme()) {
                        this.applyTheme(e.matches ? THEME_DARK : THEME_LIGHT);
                    }
                });
            }
            // Older browsers
            else if (mediaQuery.addListener) {
                mediaQuery.addListener((e) => {
                    if (!this.getStoredTheme()) {
                        this.applyTheme(e.matches ? THEME_DARK : THEME_LIGHT);
                    }
                });
            }
        }

        // Public API
        getTheme() {
            return this.currentTheme;
        }

        setTheme(theme) {
            if (theme === THEME_LIGHT || theme === THEME_DARK) {
                this.applyTheme(theme);
            }
        }
    }

    // Initialize theme switcher
    const themeSwitcher = new ThemeSwitcher();

    // Expose to window for external access
    window.ThemeSwitcher = {
        getTheme: () => themeSwitcher.getTheme(),
        setTheme: (theme) => themeSwitcher.setTheme(theme),
        toggle: () => themeSwitcher.toggleTheme()
    };

})();
