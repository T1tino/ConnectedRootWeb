// JavaScript específico para las páginas de Usuario

// ============================================================================
// FUNCIONES ORIGINALES MEJORADAS
// ============================================================================

// Función para copiar texto al portapapeles
function copyToClipboard(text) {
    if (navigator.clipboard && window.isSecureContext) {
        // Método moderno
        navigator.clipboard.writeText(text).then(function() {
            showToast('ID copiado al portapapeles', 'success');
        }).catch(function(err) {
            console.error('Error al copiar: ', err);
            showToast('Error al copiar ID', 'error');
        });
    } else {
        // Método alternativo para navegadores más antiguos
        const textArea = document.createElement('textarea');
        textArea.value = text;
        textArea.style.position = 'fixed';
        textArea.style.left = '-999999px';
        textArea.style.top = '-999999px';
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();
        
        try {
            document.execCommand('copy');
            showToast('ID copiado al portapapeles', 'success');
        } catch (err) {
            console.error('Error al copiar: ', err);
            showToast('Error al copiar ID', 'error');
        } finally {
            textArea.remove();
        }
    }
}

// Función para mostrar notificaciones toast (mejorada)
function showToast(message, type = 'info', duration = 3000) {
    const toastContainer = document.querySelector('.toast-container') || createToastContainer();
    
    const toastId = 'toast_' + Date.now();
    const iconClass = getToastIcon(type);
    const bgClass = getToastBgClass(type);
    
    const toast = document.createElement('div');
    toast.id = toastId;
    toast.className = `toast align-items-center text-white ${bgClass} border-0`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    toast.style.animation = 'slideInRight 0.3s ease-out';
    
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                <i class="fas ${iconClass} me-2"></i>
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Cerrar"></button>
        </div>
    `;
    
    toastContainer.appendChild(toast);
    
    // Inicializar el toast de Bootstrap
    const bsToast = new bootstrap.Toast(toast, { 
        delay: duration,
        autohide: true 
    });
    
    bsToast.show();
    
    // Remover del DOM después de ocultar
    toast.addEventListener('hidden.bs.toast', function() {
        toast.style.animation = 'slideOutRight 0.3s ease-in';
        setTimeout(() => toast.remove(), 300);
    });
}

// Función auxiliar para crear el contenedor de toasts
function createToastContainer() {
    const container = document.createElement('div');
    container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
    container.style.zIndex = '9999';
    document.body.appendChild(container);
    return container;
}

// Función auxiliar para obtener el icono del toast
function getToastIcon(type) {
    const icons = {
        'success': 'fa-check-circle',
        'error': 'fa-exclamation-triangle',
        'warning': 'fa-exclamation-circle',
        'info': 'fa-info-circle'
    };
    return icons[type] || icons['info'];
}

// Función auxiliar para obtener la clase de color del toast
function getToastBgClass(type) {
    const bgClasses = {
        'success': 'bg-success',
        'error': 'bg-danger',
        'warning': 'bg-warning',
        'info': 'bg-info'
    };
    return bgClasses[type] || bgClasses['info'];
}

// Función para confirmar acciones peligrosas (mejorada)
function confirmAction(message, callback, options = {}) {
    const defaultOptions = {
        title: '¿Estás seguro?',
        confirmText: 'Sí, continuar',
        cancelText: 'Cancelar',
        type: 'warning'
    };
    
    const config = { ...defaultOptions, ...options };
    
    // Si no hay SweetAlert2, usar confirm nativo
    if (typeof Swal === 'undefined') {
        if (confirm(message)) {
            if (typeof callback === 'function') {
                callback();
            }
            return true;
        }
        return false;
    }
    
    // Usar SweetAlert2 si está disponible
    Swal.fire({
        title: config.title,
        text: message,
        icon: config.type,
        showCancelButton: true,
        confirmButtonText: config.confirmText,
        cancelButtonText: config.cancelText,
        reverseButtons: true
    }).then((result) => {
        if (result.isConfirmed && typeof callback === 'function') {
            callback();
        }
    });
}

// ============================================================================
// NUEVAS FUNCIONES PARA BOTONES MEJORADOS
// ============================================================================

// Función mejorada para confirmación de toggle status
function confirmToggle(button, userName, isActive) {
    const action = isActive ? 'desactivar' : 'activar';
    const icon = isActive ? '⚠️' : '✅';
    const message = `${icon} ¿Estás seguro de ${action} a ${userName}?`;
    
    const confirmed = confirm(message);
    
    if (confirmed) {
        // Agregar estado de carga
        addLoadingState(button);
        
        // Mostrar toast de proceso
        showToast(`${isActive ? 'Desactivando' : 'Activando'} usuario...`, 'info', 2000);
        
        // Restaurar botón si la submisión del formulario falla (fallback)
        setTimeout(() => {
            removeLoadingState(button);
        }, 5000);
        
        return true;
    }
    return false;
}

// Función para agregar estado de carga a un botón
function addLoadingState(button) {
    const form = button.closest('form');
    if (form) form.classList.add('loading');
    
    button.disabled = true;
    button.dataset.originalText = button.innerHTML;
    
    const icon = button.querySelector('i');
    if (icon) {
        icon.dataset.originalClass = icon.className;
        icon.className = 'fas fa-spinner fa-spin';
    }
}

// Función para remover estado de carga de un botón
function removeLoadingState(button) {
    const form = button.closest('form');
    if (form) form.classList.remove('loading');
    
    button.disabled = false;
    
    if (button.dataset.originalText) {
        button.innerHTML = button.dataset.originalText;
        delete button.dataset.originalText;
    }
    
    const icon = button.querySelector('i');
    if (icon && icon.dataset.originalClass) {
        icon.className = icon.dataset.originalClass;
        delete icon.dataset.originalClass;
    }
}

// Función para agregar efecto ripple a los botones
function addRippleEffect(button, event) {
    const existingRipple = button.querySelector('.ripple');
    if (existingRipple) existingRipple.remove();
    
    const ripple = document.createElement('span');
    const rect = button.getBoundingClientRect();
    const size = Math.max(rect.width, rect.height);
    const x = event.clientX - rect.left - size / 2;
    const y = event.clientY - rect.top - size / 2;
    
    ripple.style.width = ripple.style.height = size + 'px';
    ripple.style.left = x + 'px';
    ripple.style.top = y + 'px';
    ripple.classList.add('ripple');
    
    button.appendChild(ripple);
    
    setTimeout(() => {
        if (ripple.parentNode) {
            ripple.remove();
        }
    }, 600);
}

// ============================================================================
// FUNCIONES DE VALIDACIÓN Y UTILIDADES
// ============================================================================

// Función para validar formularios antes del envío (mejorada)
function validateForm(formElement) {
    const requiredFields = formElement.querySelectorAll('[required]');
    let isValid = true;
    let firstInvalidField = null;
    
    requiredFields.forEach(function(field) {
        const value = field.type === 'checkbox' ? field.checked : field.value.trim();
        
        if (!value) {
            field.classList.add('is-invalid');
            if (!firstInvalidField) firstInvalidField = field;
            isValid = false;
            
            // Agregar mensaje de error si no existe
            let errorMsg = field.parentNode.querySelector('.invalid-feedback');
            if (!errorMsg) {
                errorMsg = document.createElement('div');
                errorMsg.className = 'invalid-feedback';
                errorMsg.textContent = 'Este campo es obligatorio.';
                field.parentNode.appendChild(errorMsg);
            }
        } else {
            field.classList.remove('is-invalid');
            
            // Remover mensaje de error
            const errorMsg = field.parentNode.querySelector('.invalid-feedback');
            if (errorMsg) errorMsg.remove();
        }
    });
    
    // Enfocar el primer campo inválido
    if (firstInvalidField) {
        firstInvalidField.focus();
        firstInvalidField.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
    
    return isValid;
}

// Función para limpiar formulario
function clearForm(formElement) {
    const inputs = formElement.querySelectorAll('input, select, textarea');
    inputs.forEach(input => {
        if (input.type === 'checkbox' || input.type === 'radio') {
            input.checked = false;
        } else {
            input.value = '';
        }
        input.classList.remove('is-invalid', 'is-valid');
    });
    
    // Remover mensajes de error
    const errorMessages = formElement.querySelectorAll('.invalid-feedback');
    errorMessages.forEach(msg => msg.remove());
}

// ============================================================================
// INICIALIZACIÓN Y EVENT LISTENERS
// ============================================================================

// Inicialización cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', function() {
    
    // Agregar estilos CSS dinámicos para animaciones
    addDynamicStyles();
    
    // Inicializar tooltips de Bootstrap
    initializeTooltips();
    
    // Configurar efectos hover para enlaces de email
    setupEmailHoverEffects();
    
    // Configurar botones de copia
    setupCopyButtons();
    
    // Configurar efectos para botones de acción
    setupActionButtons();
    
    // Configurar efectos hover para filas de tabla
    setupTableRowEffects();
    
    // Auto-dismiss para alertas
    setupAutoAlertDismiss();
    
    // Configurar validación en tiempo real para formularios
    setupRealTimeValidation();
});

// Función para agregar estilos CSS dinámicos
function addDynamicStyles() {
    if (document.getElementById('dynamic-usuario-styles')) return;
    
    const styles = `
        <style id="dynamic-usuario-styles">
            .ripple {
                position: absolute;
                border-radius: 50%;
                background: rgba(255, 255, 255, 0.6);
                transform: scale(0);
                animation: ripple-animation 0.6s linear;
                pointer-events: none;
            }
            
            @keyframes ripple-animation {
                to {
                    transform: scale(4);
                    opacity: 0;
                }
            }
            
            @keyframes slideInRight {
                from {
                    opacity: 0;
                    transform: translateX(100%);
                }
                to {
                    opacity: 1;
                    transform: translateX(0);
                }
            }
            
            @keyframes slideOutRight {
                from {
                    opacity: 1;
                    transform: translateX(0);
                }
                to {
                    opacity: 0;
                    transform: translateX(100%);
                }
            }
            
            .btn-group form.loading .btn {
                pointer-events: none;
                opacity: 0.7;
            }
            
            .table-hover tbody tr {
                transition: all 0.2s ease;
            }
            
            .alert-dismissible {
                animation: slideInDown 0.5s ease-out;
            }
            
            @keyframes slideInDown {
                from {
                    opacity: 0;
                    transform: translateY(-20px);
                }
                to {
                    opacity: 1;
                    transform: translateY(0);
                }
            }
        </style>
    `;
    
    document.head.insertAdjacentHTML('beforeend', styles);
}

// Función para inicializar tooltips
function initializeTooltips() {
    if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"], [title]'));
        tooltipTriggerList.forEach(function (tooltipTriggerEl) {
            if (!tooltipTriggerEl.hasAttribute('data-bs-original-title')) {
                new bootstrap.Tooltip(tooltipTriggerEl);
            }
        });
    }
}

// Función para configurar efectos hover en enlaces de email
function setupEmailHoverEffects() {
    const emailLinks = document.querySelectorAll('a[href^="mailto:"]');
    emailLinks.forEach(function(link) {
        link.addEventListener('mouseenter', function() {
            this.style.transform = 'scale(1.05)';
            this.style.transition = 'transform 0.2s ease-in-out';
        });
        
        link.addEventListener('mouseleave', function() {
            this.style.transform = 'scale(1)';
        });
    });
}

// Función para configurar botones de copia
function setupCopyButtons() {
    const copyButtons = document.querySelectorAll('[onclick*="copyToClipboard"]');
    copyButtons.forEach(function(button) {
        button.addEventListener('keydown', function(e) {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                button.click();
            }
        });
        
        // Agregar efecto hover
        button.addEventListener('mouseenter', function() {
            this.style.opacity = '1';
        });
        
        button.addEventListener('mouseleave', function() {
            this.style.opacity = '0.7';
        });
    });
}

// Función para configurar botones de acción
function setupActionButtons() {
    const actionButtons = document.querySelectorAll('.btn-group .btn');
    
    actionButtons.forEach(button => {
        // Agregar efecto ripple
        button.addEventListener('click', function(e) {
            addRippleEffect(this, e);
        });
        
        // Mejorar accesibilidad
        button.addEventListener('keydown', function(e) {
            if (e.key === 'Enter' || e.key === ' ') {
                addRippleEffect(this, e);
            }
        });
    });
}

// Función para configurar efectos en filas de tabla
function setupTableRowEffects() {
    const tableRows = document.querySelectorAll('.table-hover tbody tr');
    
    tableRows.forEach(row => {
        row.addEventListener('mouseenter', function() {
            this.style.transform = 'scale(1.01)';
            this.style.transition = 'transform 0.2s ease';
            this.style.zIndex = '1';
        });
        
        row.addEventListener('mouseleave', function() {
            this.style.transform = 'scale(1)';
            this.style.zIndex = 'auto';
        });
    });
}

// Función para configurar auto-dismiss de alertas
function setupAutoAlertDismiss() {
    setTimeout(function() {
        const alerts = document.querySelectorAll('.alert-dismissible');
        alerts.forEach(alert => {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            if (bsAlert) {
                alert.style.animation = 'slideOutRight 0.5s ease-in';
                setTimeout(() => bsAlert.close(), 500);
            } else {
                alert.style.animation = 'slideOutRight 0.5s ease-in';
                setTimeout(() => alert.remove(), 500);
            }
        });
    }, 5000);
}

// Función para configurar validación en tiempo real
function setupRealTimeValidation() {
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        const inputs = form.querySelectorAll('input[required], select[required], textarea[required]');
        
        inputs.forEach(input => {
            input.addEventListener('blur', function() {
                validateSingleField(this);
            });
            
            input.addEventListener('input', function() {
                if (this.classList.contains('is-invalid')) {
                    validateSingleField(this);
                }
            });
        });
    });
}

// Función para validar un solo campo
function validateSingleField(field) {
    const value = field.type === 'checkbox' ? field.checked : field.value.trim();
    
    if (!value) {
        field.classList.add('is-invalid');
        field.classList.remove('is-valid');
    } else {
        field.classList.remove('is-invalid');
        field.classList.add('is-valid');
    }
}

// ============================================================================
// EXPORTAR FUNCIONES PARA USO GLOBAL
// ============================================================================

// Exportar funciones para uso global
window.UsuarioHelper = {
    copyToClipboard,
    showToast,
    confirmAction,
    confirmToggle,
    validateForm,
    clearForm,
    addLoadingState,
    removeLoadingState,
    addRippleEffect
};

// Hacer funciones específicas disponibles globalmente
window.confirmToggle = confirmToggle;
window.copyToClipboard = copyToClipboard;
window.showToast = showToast;