// Simulador.js
let intervaloSimulador = null;
const plantasCount = 4;

// Datos iniciales
let temperaturas = [25, 26, 24, 18];
let humedades = [37, 72, 80, 65];
let temperaturasHistorial = [];
for (let i = 0; i < plantasCount; i++) {
  temperaturasHistorial[i] = Array(6).fill(temperaturas[i]).map(v => v + (Math.random() * 2 - 1));
}

let tempCharts = [];
let humedadCharts = [];
let conectado = false; // Estado de conexión

// Funciones de color y estado
function obtenerColorTemperatura(temp) {
  if (temp >= 18 && temp <= 25) return '#28a745';
  else if ((temp >= 15 && temp < 18) || (temp > 25 && temp <= 28)) return '#ffc107';
  else if ((temp >= 10 && temp < 15) || (temp > 28 && temp <= 32)) return '#fd7e14';
  else return '#dc3545';
}
function obtenerEstadoTemperatura(temp) {
  if (temp >= 18 && temp <= 25) return "Óptimo";
  else if ((temp >= 15 && temp < 18) || (temp > 25 && temp <= 28)) return "Moderado";
  else if ((temp >= 10 && temp < 15) || (temp > 28 && temp <= 32)) return "Subóptimo";
  else return "Crítico";
}
function obtenerColorHumedad(humedad) {
  if (humedad >= 40 && humedad <= 70) return '#28a745';
  else if ((humedad >= 30 && humedad < 40) || (humedad > 70 && humedad <= 80)) return '#ffc107';
  else if ((humedad >= 20 && humedad < 30) || (humedad > 80 && humedad <= 90)) return '#fd7e14';
  else return '#dc3545';
}
function obtenerEstadoHumedad(humedad) {
  if (humedad >= 40 && humedad <= 70) return 'Óptimo';
  else if ((humedad >= 30 && humedad < 40) || (humedad > 70 && humedad <= 80)) return 'Moderado';
  else if ((humedad >= 20 && humedad < 30) || (humedad > 80 && humedad <= 90)) return 'Subóptimo';
  else return 'Crítico';
}

// Generadores de datos aleatorios
function generarTemperaturaAleatoria() {
  const random = Math.random() * 100;
  if (random <= 60) return parseFloat((18 + Math.random() * 7).toFixed(1));
  else if (random <= 80) return parseFloat((Math.random() < 0.5 ? (15 + Math.random() * 3) : (25 + Math.random() * 3)).toFixed(1));
  else if (random <= 95) return parseFloat((Math.random() < 0.5 ? (10 + Math.random() * 5) : (28 + Math.random() * 4)).toFixed(1));
  else return parseFloat((Math.random() < 0.5 ? (Math.random() * 10) : (32 + Math.random() * 10)).toFixed(1));
}
function generarHumedadAleatoria() {
  const random = Math.random() * 100;
  if (random <= 60) return parseFloat((40 + Math.random() * 30).toFixed(1));
  else if (random <= 80) return parseFloat((Math.random() < 0.5 ? (30 + Math.random() * 10) : (70 + Math.random() * 10)).toFixed(1));
  else if (random <= 95) return parseFloat((Math.random() < 0.5 ? (20 + Math.random() * 10) : (80 + Math.random() * 10)).toFixed(1));
  else return parseFloat((Math.random() < 0.5 ? (Math.random() * 20) : (90 + Math.random() * 10)).toFixed(1));
}

// Crear los charts
function inicializarCharts() {
  for (let i = 0; i < plantasCount; i++) {
    const ctxTemp = document.getElementById(`tempChart${i + 1}`);
    const ctxHum = document.getElementById(`humedadChart${i + 1}`);
    if (ctxTemp) {
      const colorTemp = obtenerColorTemperatura(temperaturas[i]);
      tempCharts[i] = new Chart(ctxTemp.getContext("2d"), {
        type: "line",
        data: {
          labels: ["-5m", "-4m", "-3m", "-2m", "-1m", "Ahora"],
          datasets: [{
            label: 'Temperatura (°C)',
            data: temperaturasHistorial[i],
            borderColor: colorTemp,
            backgroundColor: 'rgba(40, 167, 69, 0.2)',
            tension: 0.3,
            fill: true,
            pointRadius: 3,
            pointHoverRadius: 6,
          }]
        },
        options: { responsive: true, maintainAspectRatio: true }
      });
    }
    if (ctxHum) {
      const colorHum = obtenerColorHumedad(humedades[i]);
      humedadCharts[i] = new Chart(ctxHum.getContext("2d"), {
        type: "doughnut",
        data: { datasets: [{ data: [humedades[i], 100 - humedades[i]], backgroundColor: [colorHum, "#e9ecef"], cutout: "75%" }] },
        options: { responsive: true, maintainAspectRatio: true }
      });
    }
  }
}

// Actualizar la UI y los charts
function actualizarUI() {
  for (let i = 0; i < plantasCount; i++) {
    temperaturasHistorial[i].shift();
    temperaturasHistorial[i].push(temperaturas[i]);

    // Temperatura
    const tempElem = document.getElementById(`temp-value-${i + 1}`);
    if (tempElem) {
      tempElem.textContent = `${temperaturas[i]}°C`;
      tempElem.style.color = obtenerColorTemperatura(temperaturas[i]);
    }
    if (tempCharts[i]) {
      tempCharts[i].data.datasets[0].data = temperaturasHistorial[i];
      tempCharts[i].data.datasets[0].borderColor = obtenerColorTemperatura(temperaturas[i]);
      tempCharts[i].update();
    }

    // Humedad
    const humElem = document.getElementById(`humedad-value-${i + 1}`);
    if (humElem) {
      humElem.textContent = `${humedades[i]}%`;
      humElem.style.color = obtenerColorHumedad(humedades[i]);
    }
    if (humedadCharts[i]) {
      humedadCharts[i].data.datasets[0].data = [humedades[i], 100 - humedades[i]];
      humedadCharts[i].data.datasets[0].backgroundColor = [obtenerColorHumedad(humedades[i]), "#e9ecef"];
      humedadCharts[i].update();
    }
  }
}

// Indicador de conexión
function actualizarConexion(estado) {
  const elem = document.getElementById("connection-status");
  if (!elem) return;
  conectado = estado;
  if (estado) {
    elem.classList.remove("connection-offline");
    elem.classList.add("connection-online");
    elem.innerHTML = `<i class="bi bi-wifi"></i> Conectado`;
  } else {
    elem.classList.remove("connection-online");
    elem.classList.add("connection-offline");
    elem.innerHTML = `<i class="bi bi-wifi-off"></i> Desconectado`;
  }
}

// Enviar datos al backend
async function enviarDatos() {
  if (!conectado) return;
  const lecturas = [];
  for (let i = 0; i < plantasCount; i++) {
    lecturas.push({
      sensorId: i + 1,
      temperatura: temperaturas[i],
      humedad: humedades[i],
      timestamp: new Date()
    });
  }

  try {
    const res = await fetch("/api/lecturas", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ lecturas })
    });
    if (!res.ok) throw new Error("Error al enviar datos");
    actualizarConexion(true);
  } catch (e) {
    console.warn("No se pudo enviar datos:", e);
    actualizarConexion(false);
  }
}

// Actualizar datos cada intervalo
window.actualizarDatos = function () {
  for (let i = 0; i < plantasCount; i++) {
    temperaturas[i] = generarTemperaturaAleatoria();
    humedades[i] = generarHumedadAleatoria();
  }
  actualizarUI();
  enviarDatos();
};

// Toggle simulador
window.toggleSimulador = function () {
  if (intervaloSimulador === null) {
    intervaloSimulador = setInterval(actualizarDatos, 5000);
    actualizarDatos();
    console.log("Simulador iniciado");
  } else {
    clearInterval(intervaloSimulador);
    intervaloSimulador = null;
    console.log("Simulador detenido");
  }
};

document.addEventListener("DOMContentLoaded", () => {
  inicializarCharts();
  actualizarUI();
  actualizarConexion(true);
  console.log("🌱 Sensores inicializados y listos para activar simulador.");
});
