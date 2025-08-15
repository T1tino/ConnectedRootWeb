// FuncionesComplementarias.js - Para integrar con el HTML existente
// Este archivo maneja las funciones que no están relacionadas directamente con el simulador

class SistemaComplementario {
  constructor() {
    this.hojasActivas = true;
    this.configurarEventListeners();
  }

  configurarEventListeners() {
    // Exponer funciones globales para el HTML
    window.toggleHojas = () => this.toggleHojas();
    window.cerrarAlert = () => this.cerrarAlert();

    document.addEventListener('DOMContentLoaded', () => {
      this.inicializarSistema();
    });
  }

  inicializarSistema() {
    console.log("🍃 Sistema complementario inicializado");
    this.actualizarBotonHojas();
  }

  // ===================
  // MANEJO DE HOJAS
  // ===================

  toggleHojas() {
    this.hojasActivas = !this.hojasActivas;

    this.aplicarEfectoHojas();
    this.actualizarBotonHojas();

    console.log(`🍃 Hojas ${this.hojasActivas ? 'activadas' : 'desactivadas'}`);
    return this.hojasActivas;
  }

  aplicarEfectoHojas() {
    const plantas = document.querySelectorAll('[data-sensor-index] img');

    plantas.forEach(planta => {
      if (this.hojasActivas) {
        planta.style.filter = 'none';
        planta.style.opacity = '1';
        planta.style.transform = 'scale(1)';
      } else {
        planta.style.filter = 'grayscale(100%) brightness(0.7)';
        planta.style.opacity = '0.6';
        planta.style.transform = 'scale(0.95)';
      }

      planta.style.transition = 'all 0.3s ease';
    });

    // Efecto adicional en las cards
    const cards = document.querySelectorAll('[data-sensor-index]');
    cards.forEach(card => {
      if (this.hojasActivas) {
        card.style.filter = 'none';
      } else {
        card.style.filter = 'contrast(0.8) saturate(0.7)';
      }
      card.style.transition = 'filter 0.3s ease';
    });
  }

  actualizarBotonHojas() {
    const btn = document.getElementById('toggleHojas');
    const icon = document.getElementById('leafIcon');
    const text = document.getElementById('leafText');

    if (!btn || !icon || !text) return;

    if (this.hojasActivas) {
      icon.className = 'bi bi-leaf';
      text.textContent = 'Desactivar Hojas';
      btn.classList.remove('btn-outline-warning');
      btn.classList.add('btn-outline-light');
    } else {
      icon.className = 'bi bi-leaf-off';
      text.textContent = 'Activar Hojas';
      btn.classList.remove('btn-outline-light');
      btn.classList.add('btn-outline-warning');
    }
  }

  // ===================
  // MANEJO DE ALERTAS
  // ===================

  cerrarAlert() {
    const alert = document.getElementById('simuladorAlert');
    if (alert && alert.classList.contains('show')) {
      alert.classList.remove('show');
      setTimeout(() => {
        alert.style.display = 'none';
      }, 150);
    }
  }

  mostrarAlertaPersonalizada(mensaje, tipo = 'info', duracion = 3000) {
    const alert = document.getElementById('simuladorAlert');
    const alertText = document.getElementById('simuladorAlertText');

    if (!alert || !alertText) return;

    const iconos = {
      'success': 'bi bi-check-circle',
      'info': 'bi bi-info-circle',
      'warning': 'bi bi-exclamation-triangle',
      'error': 'bi bi-x-circle'
    };

    const icono = iconos[tipo] || iconos.info;

    alert.className = `alert alert-${tipo === 'error' ? 'danger' : tipo} alert-dismissible fade show`;
    alertText.innerHTML = `<i class="${icono} me-2"></i>${mensaje}`;
    alert.style.display = 'block';

    if (duracion > 0) {
      setTimeout(() => {
        this.cerrarAlert();
      }, duracion);
    }
  }

  // ===================
  // EFECTOS VISUALES ADICIONALES
  // ===================

  aplicarEfectoLluvia() {
    // Efecto visual de lluvia para simular riego
    const container = document.querySelector('main.container');
    if (!container) return;

    // Crear gotas de lluvia
    for (let i = 0; i < 20; i++) {
      const gota = document.createElement('div');
      gota.style.cssText = `
        position: absolute;
        width: 2px;
        height: 15px;
        background: linear-gradient(transparent, #4FC3F7);
        left: ${Math.random() * 100}%;
        top: -20px;
        z-index: 1000;
        pointer-events: none;
        border-radius: 0 0 2px 2px;
      `;

      container.appendChild(gota);

      // Animación de caída
      const animacion = gota.animate([
        { transform: 'translateY(-20px)', opacity: 0 },
        { transform: 'translateY(0)', opacity: 1, offset: 0.1 },
        { transform: 'translateY(400px)', opacity: 0 }
      ], {
        duration: 1000 + Math.random() * 1000,
        easing: 'linear'
      });

      animacion.onfinish = () => gota.remove();
    }

    this.mostrarAlertaPersonalizada('💧 Efecto de riego aplicado', 'info', 2000);
  }

  aplicarEfectoSol() {
    // Efecto visual de sol para simular buenas condiciones
    const cards = document.querySelectorAll('[data-sensor-index]');

    cards.forEach((card, index) => {
      setTimeout(() => {
        card.style.boxShadow = '0 0 20px rgba(255, 193, 7, 0.5)';
        card.style.transform = 'scale(1.02)';
        card.style.transition = 'all 0.3s ease';

        setTimeout(() => {
          card.style.boxShadow = '';
          card.style.transform = '';
        }, 1500);
      }, index * 200);
    });

    this.mostrarAlertaPersonalizada('☀️ Condiciones óptimas simuladas', 'success', 2000);
  }

  // ===================
  // UTILIDADES DE UI
  // ===================

  resaltarPlanta(index, color = '#28a745') {
    const card = document.querySelector(`[data-sensor-index="${index}"]`);
    if (!card) return;

    const originalBorder = card.style.border;
    card.style.border = `3px solid ${color}`;
    card.style.transition = 'border 0.3s ease';

    setTimeout(() => {
      card.style.border = originalBorder;
    }, 2000);
  }

  mostrarEstadisticasGenerales() {
    // Si hay acceso al simulador global
    if (window.simulador) {
      const datos = window.simulador.obtenerDatos();
      const { temperaturas, humedades } = datos.datos;

      const tempPromedio = (temperaturas.reduce((a, b) => a + b, 0) / temperaturas.length).toFixed(1);
      const humPromedio = (humedades.reduce((a, b) => a + b, 0) / humedades.length).toFixed(1);

      const estadisticas = `
        📊 <strong>Estadísticas Generales</strong><br>
        <small>
          🌡️ Temperatura promedio: ${tempPromedio}°C<br>
          💧 Humedad promedio: ${humPromedio}%<br>
          📈 Estado: ${datos.activo ? 'Activo' : 'Inactivo'}<br>
          🔗 Conexión: ${datos.conectado ? 'Conectado' : 'Desconectado'}
        </small>
      `;

      this.mostrarAlertaPersonalizada(estadisticas, 'info', 5000);
    }
  }

  // ===================
  // API PÚBLICA
  // ===================

  obtenerEstadoHojas() {
    return this.hojasActivas;
  }

  configurarEfectosAutomaticos() {
    // Configurar efectos que se ejecuten automáticamente
    setInterval(() => {
      if (window.simulador && window.simulador.estaConectado()) {
        const random = Math.random();
        if (random < 0.1) { // 10% de probabilidad cada 30 segundos
          this.aplicarEfectoLluvia();
        } else if (random < 0.15) { // 5% de probabilidad
          this.aplicarEfectoSol();
        }
      }
    }, 30000); // Cada 30 segundos
  }
}

// ===================
// FUNCIONES ADICIONALES DE INTEGRACIÓN
// ===================

// Función para detectar y conectar con otros scripts
function integrarConSimulador() {
  if (window.simulador) {
    // Listener para eventos del simulador
    document.addEventListener('simuladorCambioEstado', (event) => {
      const { estado } = event.detail;

      if (window.sistemaComplementario) {
        if (estado === 'iniciado') {
          window.sistemaComplementario.aplicarEfectoSol();
        }
      }
    });

    console.log("🔗 Integración con simulador establecida");
  }
}

// ===================
// INICIALIZACIÓN
// ===================

let sistemaComplementarioGlobal = null;

document.addEventListener("DOMContentLoaded", () => {
  sistemaComplementarioGlobal = new SistemaComplementario();
  window.sistemaComplementario = sistemaComplementarioGlobal;

  // Intentar integrar con simulador (puede que no esté listo aún)
  setTimeout(integrarConSimulador, 1000);

  // Configurar efectos automáticos después de 5 segundos
  setTimeout(() => {
    if (sistemaComplementarioGlobal) {
      sistemaComplementarioGlobal.configurarEfectosAutomaticos();
    }
  }, 5000);

  console.log("🎨 Sistema complementario inicializado");
});

// Exponer algunas funciones útiles globalmente
window.mostrarEstadisticas = () => {
  if (sistemaComplementarioGlobal) {
    sistemaComplementarioGlobal.mostrarEstadisticasGenerales();
  }
};

window.efectoLluvia = () => {
  if (sistemaComplementarioGlobal) {
    sistemaComplementarioGlobal.aplicarEfectoLluvia();
  }
};

window.efectoSol = () => {
  if (sistemaComplementarioGlobal) {
    sistemaComplementarioGlobal.aplicarEfectoSol();
  }
};
