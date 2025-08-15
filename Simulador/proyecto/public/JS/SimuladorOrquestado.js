// SimuladorOrquestado.js - Arquitectura cohesiva y orquestada

class SensorSimulator {
  constructor() {
    this.config = {
      plantasCount: 4,
      intervaloActualizacion: 5000,
      intentosReconexion: 3,
      tiempoEsperaReconexion: 2000,
      endpoints: {
        lecturas: '/api/lecturas',
        configuracion: '/api/config'
      }
    };

    this.estado = {
      activo: false,
      conectado: false,
      ultimaActualizacion: null,
      intentosConexion: 0,
      erroresConsecutivos: 0
    };

    this.datos = {
      temperaturas: [25, 26, 24, 18],
      humedades: [37, 72, 80, 65],
      temperaturasHistorial: []
    };

    this.ui = {
      tempCharts: [],
      humedadCharts: []
    };

    this.intervalo = null;
    this.timeoutReconexion = null;

    this.inicializar();
  }

  // ===================
  // INICIALIZACIÓN
  // ===================

  async inicializar() {
    try {
      this.inicializarHistorial();
      await this.inicializarUI();
      this.configurarEventListeners();
      this.verificarConexionInicial();

      console.log("🌱 Sistema de simulación inicializado correctamente");
    } catch (error) {
      console.error("❌ Error en inicialización:", error);
      this.manejarError(error, 'inicializacion');
    }
  }

  inicializarHistorial() {
    this.datos.temperaturasHistorial = [];
    for (let i = 0; i < this.config.plantasCount; i++) {
      this.datos.temperaturasHistorial[i] = Array(6)
        .fill(this.datos.temperaturas[i])
        .map(v => v + (Math.random() * 2 - 1));
    }
  }

  async inicializarUI() {
    // Esperar a que el DOM esté listo
    if (document.readyState === 'loading') {
      await new Promise(resolve => {
        document.addEventListener('DOMContentLoaded', resolve);
      });
    }

    this.crearCharts();
    this.actualizarInterfaz();
    this.actualizarEstadoConexion(false);
  }

  configurarEventListeners() {
    // Exponer funciones globales para el HTML
    window.toggleSimulador = () => this.toggleSimulador();
    window.reiniciarSimulador = () => this.reiniciar();
    window.cambiarConfiguracion = (nuevaConfig) => this.actualizarConfiguracion(nuevaConfig);

    // Función para cerrar alertas (ya existe en HTML)
    window.cerrarAlert = () => {
      const alert = document.getElementById('simuladorAlert');
      if (alert) {
        alert.classList.remove('show');
        setTimeout(() => {
          alert.style.display = 'none';
        }, 150);
      }
    };

    // Listeners internos
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible' && this.estado.activo) {
        this.verificarEstado();
      }
    });

    // Event listeners para cards de plantas (hover effects, etc.)
    this.configurarEventListenersCards();
  }

  configurarEventListenersCards() {
    for (let i = 0; i < this.config.plantasCount; i++) {
      const card = document.querySelector(`[data-sensor-index="${i}"]`);
      if (card) {
        card.addEventListener('mouseenter', () => {
          this.resaltarDatosPlanta(i, true);
        });

        card.addEventListener('mouseleave', () => {
          this.resaltarDatosPlanta(i, false);
        });

        card.addEventListener('click', () => {
          this.mostrarDetallesPlanta(i);
        });
      }
    }
  }

  resaltarDatosPlanta(index, resaltar) {
    const tempChart = this.ui.tempCharts[index];
    const humChart = this.ui.humedadCharts[index];

    if (tempChart) {
      tempChart.data.datasets[0].pointRadius = resaltar ? 5 : 3;
      tempChart.data.datasets[0].pointHoverRadius = resaltar ? 8 : 6;
      tempChart.update('none'); // Sin animación para mejor performance
    }

    if (humChart) {
      humChart.data.datasets[0].borderWidth = resaltar ? 3 : 0;
      humChart.update('none');
    }
  }

  mostrarDetallesPlanta(index) {
    const temp = this.datos.temperaturas[index];
    const humedad = this.datos.humedades[index];
    const estadoTemp = this.obtenerEstadoTemperatura(temp);
    const estadoHum = this.obtenerEstadoHumedad(humedad);

    // Crear modal o alert con detalles (puedes expandir esto)
    const detalles = `
      🌱 Planta ${index + 1}
      🌡️ Temperatura: ${temp}°C (${estadoTemp})
      💧 Humedad: ${humedad}% (${estadoHum})
      ⏰ Última actualización: ${this.estado.ultimaActualizacion ? this.estado.ultimaActualizacion.toLocaleTimeString() : 'N/A'}
    `;

    console.log(detalles);

    // Opcional: mostrar en alert
    const alert = document.getElementById('simuladorAlert');
    const alertText = document.getElementById('simuladorAlertText');

    if (alert && alertText) {
      alert.className = 'alert alert-info alert-dismissible fade show';
      alertText.innerHTML = `
        <strong>🌱 Planta ${index + 1}</strong><br>
        <small>🌡️ ${temp}°C (${estadoTemp}) | 💧 ${humedad}% (${estadoHum})</small>
      `;
      alert.style.display = 'block';
    }
  }

  // ===================
  // CONTROL PRINCIPAL
  // ===================

  toggleSimulador() {
    const wasActive = this.estado.activo;

    if (this.estado.activo) {
      this.detener();
    } else {
      this.iniciar();
    }

    // Actualizar UI del botón
    this.actualizarBotonSimulador();

    // Mostrar alerta
    this.mostrarAlertaSimulador(!wasActive);

    return this.estado.activo;
  }

  async iniciar() {
    try {
      console.log("🚀 Iniciando simulador...");

      this.estado.activo = true;
      this.estado.erroresConsecutivos = 0;

      // Primera ejecución inmediata
      await this.ejecutarCicloCompleto();

      // Programar ejecuciones periódicas
      this.intervalo = setInterval(() => {
        this.ejecutarCicloCompleto().catch(error => {
          console.warn("⚠️ Error en ciclo periódico:", error);
        });
      }, this.config.intervaloActualizacion);

      console.log("✅ Simulador iniciado correctamente");
      this.notificarCambioEstado('iniciado');

    } catch (error) {
      console.error("❌ Error al iniciar simulador:", error);
      this.manejarError(error, 'inicio');
      this.estado.activo = false;
    }
  }

  detener() {
    console.log("⏹️ Deteniendo simulador...");

    this.estado.activo = false;

    if (this.intervalo) {
      clearInterval(this.intervalo);
      this.intervalo = null;
    }

    if (this.timeoutReconexion) {
      clearTimeout(this.timeoutReconexion);
      this.timeoutReconexion = null;
    }

    console.log("✅ Simulador detenido");
    this.notificarCambioEstado('detenido');
  }

  async reiniciar() {
    console.log("🔄 Reiniciando simulador...");
    this.detener();
    await new Promise(resolve => setTimeout(resolve, 1000));
    this.iniciar();
  }

  // ===================
  // CICLO PRINCIPAL ORQUESTADO
  // ===================

  async ejecutarCicloCompleto() {
    try {
      // 1. Generar nuevos datos
      this.generarNuevosDatos();

      // 2. Actualizar interfaz
      this.actualizarInterfaz();

      // 3. Intentar enviar a base de datos
      await this.procesarEnvioDatos();

      // 4. Actualizar estado
      this.estado.ultimaActualizacion = new Date();
      this.estado.erroresConsecutivos = 0;

    } catch (error) {
      this.estado.erroresConsecutivos++;
      console.warn(`⚠️ Error en ciclo ${this.estado.erroresConsecutivos}:`, error);

      if (this.estado.erroresConsecutivos >= 3) {
        console.error("❌ Demasiados errores consecutivos, pausando...");
        this.manejarError(error, 'ciclo_critico');
      }
    }
  }

  // ===================
  // GENERACIÓN DE DATOS
  // ===================

  generarNuevosDatos() {
    for (let i = 0; i < this.config.plantasCount; i++) {
      this.datos.temperaturas[i] = this.generarTemperaturaAleatoria();
      this.datos.humedades[i] = this.generarHumedadAleatoria();

      // Actualizar historial
      this.datos.temperaturasHistorial[i].shift();
      this.datos.temperaturasHistorial[i].push(this.datos.temperaturas[i]);
    }
  }

  generarTemperaturaAleatoria() {
    const random = Math.random() * 100;
    if (random <= 60) return parseFloat((18 + Math.random() * 7).toFixed(1));
    else if (random <= 80) return parseFloat((Math.random() < 0.5 ? (15 + Math.random() * 3) : (25 + Math.random() * 3)).toFixed(1));
    else if (random <= 95) return parseFloat((Math.random() < 0.5 ? (10 + Math.random() * 5) : (28 + Math.random() * 4)).toFixed(1));
    else return parseFloat((Math.random() < 0.5 ? (Math.random() * 10) : (32 + Math.random() * 10)).toFixed(1));
  }

  generarHumedadAleatoria() {
    const random = Math.random() * 100;
    if (random <= 60) return parseFloat((40 + Math.random() * 30).toFixed(1));
    else if (random <= 80) return parseFloat((Math.random() < 0.5 ? (30 + Math.random() * 10) : (70 + Math.random() * 10)).toFixed(1));
    else if (random <= 95) return parseFloat((Math.random() < 0.5 ? (20 + Math.random() * 10) : (80 + Math.random() * 10)).toFixed(1));
    else return parseFloat((Math.random() < 0.5 ? (Math.random() * 20) : (90 + Math.random() * 10)).toFixed(1));
  }

  // ===================
  // COMUNICACIÓN CON BD
  // ===================

  async procesarEnvioDatos() {
    if (!this.estado.conectado && this.estado.intentosConexion >= this.config.intentosReconexion) {
      console.log("⏳ Esperando reconexión...");
      return;
    }

    try {
      const lecturas = this.prepararDatosParaEnvio();
      await this.enviarDatos(lecturas);
      await this.manejarEnvioExitoso();

    } catch (error) {
      await this.manejarErrorEnvio(error);
    }
  }

  prepararDatosParaEnvio() {
    const lecturas = [];
    for (let i = 0; i < this.config.plantasCount; i++) {
      lecturas.push({
        sensorId: i + 1,
        temperatura: this.datos.temperaturas[i],
        humedad: this.datos.humedades[i],
        timestamp: new Date(),
        estadoTemperatura: this.obtenerEstadoTemperatura(this.datos.temperaturas[i]),
        estadoHumedad: this.obtenerEstadoHumedad(this.datos.humedades[i])
      });
    }
    return lecturas;
  }

  async enviarDatos(lecturas) {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 10000); // 10s timeout

    try {
      const response = await fetch(this.config.endpoints.lecturas, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "X-Simulator-Version": "2.0"
        },
        body: JSON.stringify({
          lecturas,
          timestamp: new Date(),
          simulatorId: 'sensor-simulator-v2'
        }),
        signal: controller.signal
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      return await response.json();
    } finally {
      clearTimeout(timeoutId);
    }
  }

  async manejarEnvioExitoso() {
    if (!this.estado.conectado) {
      console.log("✅ Reconectado a la base de datos");
    }

    this.actualizarEstadoConexion(true);
    this.estado.intentosConexion = 0;

    if (this.timeoutReconexion) {
      clearTimeout(this.timeoutReconexion);
      this.timeoutReconexion = null;
    }
  }

  async manejarErrorEnvio(error) {
    console.warn("⚠️ Error al enviar datos:", error.message);

    this.actualizarEstadoConexion(false);
    this.estado.intentosConexion++;

    if (this.estado.intentosConexion <= this.config.intentosReconexion) {
      console.log(`🔄 Intento de reconexión ${this.estado.intentosConexion}/${this.config.intentosReconexion}`);

      this.timeoutReconexion = setTimeout(() => {
        this.verificarConexion();
      }, this.config.tiempoEsperaReconexion * this.estado.intentosConexion);
    }
  }

  async verificarConexion() {
    try {
      const response = await fetch(this.config.endpoints.lecturas, {
        method: 'HEAD',
        headers: { 'X-Health-Check': 'true' }
      });

      if (response.ok) {
        this.estado.intentosConexion = 0;
        this.actualizarEstadoConexion(true);
        console.log("✅ Conexión restablecida");
      }
    } catch (error) {
      console.log("❌ Servidor aún no disponible");
    }
  }

  async verificarConexionInicial() {
    try {
      await this.verificarConexion();
    } catch (error) {
      console.log("⚠️ Servidor no disponible al inicio");
      this.actualizarEstadoConexion(false);
    }
  }

  // ===================
  // INTERFAZ DE USUARIO
  // ===================

  crearCharts() {
    for (let i = 0; i < this.config.plantasCount; i++) {
      this.crearChartTemperatura(i);
      this.crearChartHumedad(i);
    }
  }

  crearChartTemperatura(index) {
    const ctx = document.getElementById(`tempChart${index + 1}`);
    if (!ctx) return;

    const color = this.obtenerColorTemperatura(this.datos.temperaturas[index]);
    this.ui.tempCharts[index] = new Chart(ctx.getContext("2d"), {
      type: "line",
      data: {
        labels: ["-5m", "-4m", "-3m", "-2m", "-1m", "Ahora"],
        datasets: [{
          label: 'Temperatura (°C)',
          data: this.datos.temperaturasHistorial[index],
          borderColor: color,
          backgroundColor: 'rgba(40, 167, 69, 0.2)',
          tension: 0.3,
          fill: true,
          pointRadius: 3,
          pointHoverRadius: 6,
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: true,
        animation: { duration: 750 }
      }
    });
  }

  crearChartHumedad(index) {
    const ctx = document.getElementById(`humedadChart${index + 1}`);
    if (!ctx) return;

    const color = this.obtenerColorHumedad(this.datos.humedades[index]);
    this.ui.humedadCharts[index] = new Chart(ctx.getContext("2d"), {
      type: "doughnut",
      data: {
        datasets: [{
          data: [this.datos.humedades[index], 100 - this.datos.humedades[index]],
          backgroundColor: [color, "#e9ecef"],
          cutout: "75%"
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: true,
        animation: { duration: 750 }
      }
    });
  }

  actualizarInterfaz() {
    for (let i = 0; i < this.config.plantasCount; i++) {
      this.actualizarElementoTemperatura(i);
      this.actualizarElementoHumedad(i);
      this.actualizarChartTemperatura(i);
      this.actualizarChartHumedad(i);
    }

    // Actualizar estado del botón si está activo
    if (this.estado.activo) {
      this.actualizarBotonSimulador();
    }
  }

  actualizarBotonSimulador() {
    const btn = document.getElementById('toggleSimulador');
    const icon = document.getElementById('simuladorIcon');
    const text = document.getElementById('simuladorText');

    if (!btn || !icon || !text) return;

    if (this.estado.activo) {
      icon.className = 'bi bi-stop-circle';
      text.textContent = 'Detener';
      btn.classList.remove('btn-outline-light');
      btn.classList.add('btn-outline-danger');
    } else {
      icon.className = 'bi bi-play-circle';
      text.textContent = 'Simular';
      btn.classList.remove('btn-outline-danger');
      btn.classList.add('btn-outline-light');
    }
  }

  mostrarAlertaSimulador(iniciado) {
    const alert = document.getElementById('simuladorAlert');
    const alertText = document.getElementById('simuladorAlertText');

    if (!alert || !alertText) return;

    if (iniciado) {
      alert.className = 'alert alert-success alert-dismissible fade show';
      alertText.innerHTML = '<i class="bi bi-check-circle me-2"></i>Simulador iniciado correctamente';
    } else {
      alert.className = 'alert alert-info alert-dismissible fade show';
      alertText.innerHTML = '<i class="bi bi-pause-circle me-2"></i>Simulador detenido';
    }

    alert.style.display = 'block';

    // Auto-ocultar después de 3 segundos
    setTimeout(() => {
      if (alert.classList.contains('show')) {
        alert.classList.remove('show');
        setTimeout(() => {
          alert.style.display = 'none';
        }, 150);
      }
    }, 3000);
  }

  actualizarElementoTemperatura(index) {
    const elem = document.getElementById(`temp-value-${index + 1}`);
    if (elem) {
      elem.textContent = `${this.datos.temperaturas[index]}°C`;
      elem.style.color = this.obtenerColorTemperatura(this.datos.temperaturas[index]);
    }
  }

  actualizarElementoHumedad(index) {
    const elem = document.getElementById(`humedad-value-${index + 1}`);
    if (elem) {
      elem.textContent = `${this.datos.humedades[index]}%`;
      elem.style.color = this.obtenerColorHumedad(this.datos.humedades[index]);
    }
  }

  actualizarChartTemperatura(index) {
    if (this.ui.tempCharts[index]) {
      const chart = this.ui.tempCharts[index];
      chart.data.datasets[0].data = this.datos.temperaturasHistorial[index];
      chart.data.datasets[0].borderColor = this.obtenerColorTemperatura(this.datos.temperaturas[index]);
      chart.update();
    }
  }

  actualizarChartHumedad(index) {
    if (this.ui.humedadCharts[index]) {
      const chart = this.ui.humedadCharts[index];
      const humedad = this.datos.humedades[index];
      chart.data.datasets[0].data = [humedad, 100 - humedad];
      chart.data.datasets[0].backgroundColor = [this.obtenerColorHumedad(humedad), "#e9ecef"];
      chart.update();
    }
  }

  actualizarEstadoConexion(conectado) {
    const elem = document.getElementById("connection-status");
    if (!elem) return;

    this.estado.conectado = conectado;

    if (conectado) {
      elem.classList.remove("connection-offline");
      elem.classList.add("connection-online");
      elem.innerHTML = `<i class="bi bi-wifi"></i> Conectado`;
    } else {
      elem.classList.remove("connection-online");
      elem.classList.add("connection-offline");
      elem.innerHTML = `<i class="bi bi-wifi-off"></i> Desconectado`;
    }
  }

  // ===================
  // UTILIDADES Y HELPERS
  // ===================

  obtenerColorTemperatura(temp) {
    if (temp >= 18 && temp <= 25) return '#28a745';
    else if ((temp >= 15 && temp < 18) || (temp > 25 && temp <= 28)) return '#ffc107';
    else if ((temp >= 10 && temp < 15) || (temp > 28 && temp <= 32)) return '#fd7e14';
    else return '#dc3545';
  }

  obtenerEstadoTemperatura(temp) {
    if (temp >= 18 && temp <= 25) return "Óptimo";
    else if ((temp >= 15 && temp < 18) || (temp > 25 && temp <= 28)) return "Moderado";
    else if ((temp >= 10 && temp < 15) || (temp > 28 && temp <= 32)) return "Subóptimo";
    else return "Crítico";
  }

  obtenerColorHumedad(humedad) {
    if (humedad >= 40 && humedad <= 70) return '#28a745';
    else if ((humedad >= 30 && humedad < 40) || (humedad > 70 && humedad <= 80)) return '#ffc107';
    else if ((humedad >= 20 && humedad < 30) || (humedad > 80 && humedad <= 90)) return '#fd7e14';
    else return '#dc3545';
  }

  obtenerEstadoHumedad(humedad) {
    if (humedad >= 40 && humedad <= 70) return 'Óptimo';
    else if ((humedad >= 30 && humedad < 40) || (humedad > 70 && humedad <= 80)) return 'Moderado';
    else if ((humedad >= 20 && humedad < 30) || (humedad > 80 && humedad <= 90)) return 'Subóptimo';
    else return 'Crítico';
  }

  // ===================
  // MANEJO DE ERRORES Y CONFIGURACIÓN
  // ===================

  manejarError(error, contexto) {
    const errorInfo = {
      contexto,
      error: error.message,
      timestamp: new Date(),
      estado: {...this.estado}
    };

    console.error(`❌ Error en ${contexto}:`, errorInfo);

    // Aquí podrías enviar el error a un sistema de logging
    this.enviarLogError(errorInfo);
  }

  async enviarLogError(errorInfo) {
    try {
      await fetch('/api/logs/error', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(errorInfo)
      });
    } catch (e) {
      // Silenciar errores de logging para evitar loops
    }
  }

  actualizarConfiguracion(nuevaConfig) {
    this.config = { ...this.config, ...nuevaConfig };

    if (this.estado.activo) {
      console.log("🔄 Configuración actualizada, reiniciando...");
      this.reiniciar();
    }
  }

  verificarEstado() {
    if (this.estado.ultimaActualizacion) {
      const tiempoTranscurrido = Date.now() - this.estado.ultimaActualizacion.getTime();
      if (tiempoTranscurrido > this.config.intervaloActualizacion * 2) {
        console.warn("⚠️ Sistema parece estar detenido, reiniciando...");
        this.reiniciar();
      }
    }
  }

  notificarCambioEstado(nuevoEstado) {
    // Event system para notificar cambios de estado
    const evento = new CustomEvent('simuladorCambioEstado', {
      detail: {
        estado: nuevoEstado,
        timestamp: new Date(),
        datos: this.obtenerEstadoCompleto()
      }
    });
    document.dispatchEvent(evento);
  }

  obtenerEstadoCompleto() {
    return {
      activo: this.estado.activo,
      conectado: this.estado.conectado,
      ultimaActualizacion: this.estado.ultimaActualizacion,
      datos: {
        temperaturas: [...this.datos.temperaturas],
        humedades: [...this.datos.humedades]
      },
      config: {...this.config}
    };
  }

  // ===================
  // API PÚBLICA
  // ===================

  obtenerDatos() {
    return this.obtenerEstadoCompleto();
  }

  obtenerEstado() {
    return this.estado.activo ? 'activo' : 'inactivo';
  }

  estaConectado() {
    return this.estado.conectado;
  }
}

// ===================
// INICIALIZACIÓN GLOBAL
// ===================

let simuladorGlobal = null;

document.addEventListener("DOMContentLoaded", () => {
  simuladorGlobal = new SensorSimulator();

  // Event listeners para el sistema
  document.addEventListener('simuladorCambioEstado', (event) => {
    console.log('📊 Estado del simulador:', event.detail);
  });

  // Exponer instancia globalmente para debugging
  window.simulador = simuladorGlobal;

  console.log("🌱 Sistema de sensores completamente inicializado");
});

// Cleanup al cerrar la página
window.addEventListener('beforeunload', () => {
  if (simuladorGlobal) {
    simuladorGlobal.detener();
  }
});
