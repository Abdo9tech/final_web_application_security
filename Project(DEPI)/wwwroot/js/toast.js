/**
 * Toast Notification System
 * Non-intrusive notifications with auto-dismiss
 */

(function() {
    'use strict';

    class ToastManager {
        constructor() {
            this.container = null;
            this.toasts = [];
            this.init();
        }

        init() {
            // Create toast container
            this.container = document.createElement('div');
            this.container.id = 'toast-container';
            this.container.className = 'fixed top-4 right-4 z-[1080] space-y-3 pointer-events-none';
            this.container.style.maxWidth = '420px';
            
            // Wait for DOM to be ready
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', () => {
                    document.body.appendChild(this.container);
                });
            } else {
                document.body.appendChild(this.container);
            }
        }

        show(message, type = 'info', duration = 5000) {
            const toast = this.createToast(message, type);
            this.container.appendChild(toast);
            this.toasts.push(toast);

            // Animate in
            setTimeout(() => {
                toast.classList.add('toast-show');
            }, 10);

            // Auto dismiss
            if (duration > 0) {
                setTimeout(() => {
                    this.dismiss(toast);
                }, duration);
            }

            return toast;
        }

        createToast(message, type) {
            const toast = document.createElement('div');
            toast.className = 'toast pointer-events-auto transform translate-x-full transition-all duration-300 ease-out';
            
            const config = this.getTypeConfig(type);
            
            toast.innerHTML = `
                <div class="glass backdrop-blur-xl ${config.bgClass} rounded-xl shadow-2xl border ${config.borderClass} p-4 flex items-start space-x-3 min-w-[320px]">
                    <div class="flex-shrink-0">
                        <div class="${config.iconBgClass} rounded-lg p-2">
                            <i class="${config.icon} ${config.iconColor} text-lg"></i>
                        </div>
                    </div>
                    <div class="flex-1 pt-0.5">
                        <p class="text-sm font-medium ${config.textClass}">
                            ${message}
                        </p>
                    </div>
                    <button class="flex-shrink-0 text-neutral-400 hover:text-neutral-600 dark:hover:text-neutral-200 transition-colors" onclick="window.Toast.dismiss(this.closest('.toast'))">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
            `;

            return toast;
        }

        getTypeConfig(type) {
            const configs = {
                success: {
                    icon: 'fas fa-check-circle',
                    iconColor: 'text-green-600 dark:text-green-400',
                    iconBgClass: 'bg-green-100 dark:bg-green-900/30',
                    bgClass: 'bg-white/90 dark:bg-neutral-900/90',
                    borderClass: 'border-green-200 dark:border-green-800',
                    textClass: 'text-neutral-900 dark:text-neutral-100'
                },
                error: {
                    icon: 'fas fa-exclamation-circle',
                    iconColor: 'text-red-600 dark:text-red-400',
                    iconBgClass: 'bg-red-100 dark:bg-red-900/30',
                    bgClass: 'bg-white/90 dark:bg-neutral-900/90',
                    borderClass: 'border-red-200 dark:border-red-800',
                    textClass: 'text-neutral-900 dark:text-neutral-100'
                },
                warning: {
                    icon: 'fas fa-exclamation-triangle',
                    iconColor: 'text-amber-600 dark:text-amber-400',
                    iconBgClass: 'bg-amber-100 dark:bg-amber-900/30',
                    bgClass: 'bg-white/90 dark:bg-neutral-900/90',
                    borderClass: 'border-amber-200 dark:border-amber-800',
                    textClass: 'text-neutral-900 dark:text-neutral-100'
                },
                info: {
                    icon: 'fas fa-info-circle',
                    iconColor: 'text-blue-600 dark:text-blue-400',
                    iconBgClass: 'bg-blue-100 dark:bg-blue-900/30',
                    bgClass: 'bg-white/90 dark:bg-neutral-900/90',
                    borderClass: 'border-blue-200 dark:border-blue-800',
                    textClass: 'text-neutral-900 dark:text-neutral-100'
                }
            };

            return configs[type] || configs.info;
        }

        dismiss(toast) {
            if (!toast) return;

            toast.classList.remove('toast-show');
            toast.classList.add('translate-x-full', 'opacity-0');

            setTimeout(() => {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
                const index = this.toasts.indexOf(toast);
                if (index > -1) {
                    this.toasts.splice(index, 1);
                }
            }, 300);
        }

        dismissAll() {
            this.toasts.forEach(toast => this.dismiss(toast));
        }

        // Convenience methods
        success(message, duration) {
            return this.show(message, 'success', duration);
        }

        error(message, duration) {
            return this.show(message, 'error', duration);
        }

        warning(message, duration) {
            return this.show(message, 'warning', duration);
        }

        info(message, duration) {
            return this.show(message, 'info', duration);
        }
    }

    // Initialize toast manager
    const toastManager = new ToastManager();

    // Expose to window
    window.Toast = {
        show: (message, type, duration) => toastManager.show(message, type, duration),
        success: (message, duration) => toastManager.success(message, duration),
        error: (message, duration) => toastManager.error(message, duration),
        warning: (message, duration) => toastManager.warning(message, duration),
        info: (message, duration) => toastManager.info(message, duration),
        dismiss: (toast) => toastManager.dismiss(toast),
        dismissAll: () => toastManager.dismissAll()
    };

})();
