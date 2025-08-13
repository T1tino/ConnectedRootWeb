// Hago toggleSimulador global para que sea visible desde HTML onclick
let intervaloSimulador = null;

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

document.addEventListener("DOMContentLoaded", function () {
  const plantasCount = 4;

  let temperaturas = [25, 26, 24, 18];
  let humedades = [37, 72, 80, 65];

  let temperaturasHistorial = [];
  for (let i = 0; i < plantasCount; i++) {
    temperaturasHistorial[i] = Array(6).fill(temperaturas[i]).map(v => v + (Math.random() * 2 - 1));
  }

  let tempCharts = [];
  let humedadCharts = [];

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
    else if (random <= 80) {
      const esBaja = Math.random() < 0.5;
      return parseFloat((esBaja ? (30 + Math.random() * 10) : (70 + Math.random() * 10)).toFixed(1));
    } else if (random <= 95) {
      const esBaja = Math.random() < 0.5;
      return parseFloat((esBaja ? (20 + Math.random() * 10) : (80 + Math.random() * 10)).toFixed(1));
    } else {
      const esBaja = Math.random() < 0.5;
      return parseFloat((esBaja ? (Math.random() * 20) : (90 + Math.random() * 10)).toFixed(1));
    }
  }

  // Crear los charts una sola vez
  for (let i = 0; i < plantasCount; i++) {
    const ctxTemp = document.getElementById(`tempChart${i + 1}`);
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
        options: {
          responsive: true,
          maintainAspectRatio: true,
          scales: {
            y: { min: 0, max: 50, ticks: { stepSize: 5 }, title: { display: true, text: '°C' } },
            x: { title: { display: true, text: 'Tiempo' } }
          },
          plugins: {
            legend: { display: false },
            tooltip: {
              callbacks: {
                label: ctx => {
                  const val = ctx.parsed.y.toFixed(1);
                  const estado = obtenerEstadoTemperatura(parseFloat(val));
                  return `Temp: ${val}°C (${estado})`;
                }
              }
            }
          }
        }
      });
    }

    const ctxHum = document.getElementById(`humedadChart${i + 1}`);
    if (ctxHum) {
      const colorHum = obtenerColorHumedad(humedades[i]);
      humedadCharts[i] = new Chart(ctxHum.getContext("2d"), {
        type: "doughnut",
        data: {
          datasets: [{
            data: [humedades[i], 100 - humedades[i]],
            backgroundColor: [colorHum, "#e9ecef"],
            borderWidth: 0,
            cutout: "75%",
            borderRadius: 4,
          }],
        },
        options: {
          responsive: true,
          maintainAspectRatio: true,
          plugins: {
            tooltip: {
              callbacks: {
                label: ctx => ctx.dataIndex === 0 ? `Humedad: ${ctx.parsed}% (${obtenerEstadoHumedad(humedades[i])})` : null,
              },
            },
            legend: { display: false },
          },
          animation: { duration: 1000, easing: "easeInOutQuart" },
        },
      });
    }
  }

  window.actualizarDatos = function actualizarDatos() {
    for (let i = 0; i < plantasCount; i++) {
      const nuevaTemp = generarTemperaturaAleatoria();
      temperaturas[i] = nuevaTemp;

      temperaturasHistorial[i].shift();
      temperaturasHistorial[i].push(nuevaTemp);

      humedades[i] = generarHumedadAleatoria();

      const tempValueElem = document.getElementById(`temp-value-${i + 1}`);
      const tempIcon = tempValueElem?.parentElement.querySelector("i.bi-thermometer-half");
      const colorTemp = obtenerColorTemperatura(temperaturas[i]);
      if (tempValueElem) {
        tempValueElem.textContent = `${temperaturas[i]}°C`;
        tempValueElem.style.color = colorTemp;
      }
      if (tempIcon) tempIcon.style.color = colorTemp;

      const humValueElem = document.getElementById(`humedad-value-${i + 1}`);
      const humIcon = humValueElem?.parentElement.querySelector("i.bi-droplet-half");
      const colorHum = obtenerColorHumedad(humedades[i]);
      if (humValueElem) {
        humValueElem.textContent = `${humedades[i]}%`;
        humValueElem.style.color = colorHum;
      }
      if (humIcon) humIcon.style.color = colorHum;

      if (tempCharts[i]) {
        tempCharts[i].data.datasets[0].data = temperaturasHistorial[i];
        tempCharts[i].data.datasets[0].borderColor = colorTemp;
        tempCharts[i].update();
      }
      if (humedadCharts[i]) {
        humedadCharts[i].data.datasets[0].data = [humedades[i], 100 - humedades[i]];
        humedadCharts[i].data.datasets[0].backgroundColor = [colorHum, "#e9ecef"];
        humedadCharts[i].update();
      }
    }
  };

  console.log("🌱 Sensores inicializados y listos para activar simulador.");
});
