// Temperatura.js Mejorado - Sistema de temperatura con colores y tiempo real

// Datos iniciales para cada planta (últimas 7 lecturas)
let datosTemperatura = [
    [20.5, 21.2, 22.8, 21.6, 20.9, 22.1, 21.8], 
    [21.1, 22.0, 21.7, 22.3, 21.9, 20.8, 22.5],  
    [19.8, 20.5, 21.2, 20.7, 21.5, 22.0, 21.3],  
    [18.2, 19.1, 18.8, 19.5, 18.9, 19.3, 18.7]
];

// Array para almacenar las instancias de los charts
let tempCharts = [];

// ✅ REMOVIDO: sensorIds (ya está en Simulador.js y Humedad.js)
// IDs de sensores disponibles desde el simulador
function getSensorIds() {
    return window.simuladorAPI?.sensorIds || [
        "66b8f5e4a1234567890abcd1",
        "66b8f5e4a1234567890abcd2", 
        "66b8f5e4a1234567890abcd3",
        "66b8f5e4a1234567890abcd4"
    ];
}

// Función para generar etiquetas de tiempo dinámicas
function generarEtiquetasTiempo() {
    const ahora = new Date();
    const etiquetas = [];
    
    for (let i = 6; i >= 0; i--) {
        const tiempo = new Date(ahora.getTime() - (i * 5000)); // 10 segundos hacia atrás
        etiquetas.push(tiempo.toLocaleTimeString('es-ES', { 
            hour: '2-digit', 
            minute: '2-digit',
            second: '2-digit'
        }));
    }
    
    return etiquetas;
}

// Función para obtener color de temperatura basado en rangos
function obtenerColorTemperatura(temp) {
    if (temp >= 20 && temp <= 23) {
        return {
            border: '#28a745',
            background: 'rgba(40, 167, 69, 0.2)'
        }; // Verde - Óptimo (20-23°C)
    } else if ((temp >= 18 && temp < 20) || (temp > 23 && temp <= 27)) {
        return {
            border: '#ffc107',
            background: 'rgba(255, 193, 7, 0.2)'
        }; // Amarillo - Moderado
    } else if ((temp >= 15 && temp < 18) || (temp > 27 && temp <= 30)) {
        return {
            border: '#fd7e14',
            background: 'rgba(253, 126, 20, 0.2)'
        }; // Naranja - Subóptimo
    } else {
        return {
            border: '#dc3545',
            background: 'rgba(220, 53, 69, 0.2)'
        }; // Rojo - Crítico
    }
}

// Función para obtener estado textual de la temperatura
function obtenerEstadoTemperatura(temp) {
    if (temp >= 20 && temp <= 23) {
        return 'Óptimo';
    } else if ((temp >= 18 && temp < 20) || (temp > 23 && temp <= 27)) {
        return 'Moderado';
    } else if ((temp >= 15 && temp < 18) || (temp > 27 && temp <= 30)) {
        return 'Subóptimo';
    } else {
        return 'Crítico';
    }
}

// Función para generar temperatura con distribución de probabilidades mejorada
function generarTemperatura() {
    const random = Math.random() * 100;
    
    if (random <= 70) {
        // 70% chance: temperatura ideal (20-23°C)
        return parseFloat((20 + (Math.random() * 3)).toFixed(1));
    } else if (random <= 85) {
        // 15% chance: temperatura moderada alta (23-27°C)
        return parseFloat((23 + (Math.random() * 4)).toFixed(1));
    } else if (random <= 95) {
        // 10% chance: temperatura moderada baja (18-20°C)
        return parseFloat((18 + (Math.random() * 2)).toFixed(1));
    } else if (random <= 98) {
        // 3% chance: temperatura extrema alta (27-35°C)
        return parseFloat((27 + (Math.random() * 8)).toFixed(1));
    } else {
        // 2% chance: temperatura extrema baja (10-18°C)
        return parseFloat((10 + (Math.random() * 8)).toFixed(1));
    }
}

// ✅ MEJORADO: Función para enviar datos usando la API del simulador
async function enviarDatosTemperatura(sensorId, valor, tipo = "temperatura", unidad = "°C") {
    try {
        // Usar la función del simulador si está disponible
        if (window.simuladorAPI && window.simuladorAPI.makeAPIRequest) {
            const response = await window.simuladorAPI.makeAPIRequest('/lecturas', {
                method: 'POST',
                body: JSON.stringify({
                    sensorId: sensorId,
                    fechaHora: new Date().toISOString(),
                    tipo: tipo,
                    valor: valor,
                    unidad: unidad
                })
            });

            if (response.ok) {
                console.log(`🌡️ Datos de temperatura enviados exitosamente para sensor ${sensorId}: ${valor}°C`);
                return { success: true };
            }
        }
        
        // Fallback manual (no debería usar si el simulador está corriendo)
        console.warn('⚠️ SimuladorAPI no disponible, datos no enviados');
        return { success: false, error: 'SimuladorAPI no disponible' };
        
    } catch (error) {
        console.error('❌ Error de conexión al enviar temperatura:', error);
        return { success: false, error: error.message };
    }
}

// Crear los charts iniciales
document.addEventListener("DOMContentLoaded", function() {
    const etiquetasIniciales = generarEtiquetasTiempo();
    
    for (let i = 1; i <= 4; i++) {
        const canvas = document.getElementById(`tempChart${i}`);
        
        if (!canvas) {
            console.warn(`⚠️ Canvas tempChart${i} no encontrado`);
            continue;
        }

        // Establecer dimensiones fijas del canvas
        canvas.style.height = '120px';
        canvas.style.width = '100%';
        canvas.height = 120;
        canvas.width = 300;
        
        const ctx = canvas.getContext('2d');
        const temperaturaActual = datosTemperatura[i - 1][datosTemperatura[i - 1].length - 1];
        const colores = obtenerColorTemperatura(temperaturaActual);
        
        const chart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: etiquetasIniciales,
                datasets: [{
                    label: 'Temp °C',
                    data: [...datosTemperatura[i - 1]],
                    backgroundColor: colores.background,
                    borderColor: colores.border,
                    borderWidth: 2,
                    fill: true,
                    tension: 0.4,
                    pointBackgroundColor: colores.border,
                    pointBorderColor: '#fff',
                    pointBorderWidth: 1,
                    pointRadius: 3,
                    pointHoverRadius: 5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                aspectRatio: 2.5,
                plugins: {
                    legend: { 
                        display: false 
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const temp = context.parsed.y;
                                const estado = obtenerEstadoTemperatura(temp);
                                return `Temperatura: ${temp}°C (${estado})`;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        suggestedMin: 10,
                        suggestedMax: 30,
                        ticks: {
                            stepSize: 5,
                            callback: function(value) {
                                return value + '°C';
                            },
                            font: {
                                size: 10
                            }
                        },
                        grid: {
                            color: 'rgba(0,0,0,0.1)'
                        }
                    },
                    x: {
                        display: true,
                        ticks: {
                            maxTicksLimit: 3,
                            font: {
                                size: 9
                            }
                        },
                        grid: {
                            color: 'rgba(0,0,0,0.1)'
                        }
                    }
                },
                animation: {
                    duration: 750,
                    easing: 'easeInOutQuart'
                },
                layout: {
                    padding: {
                        top: 5,
                        bottom: 5,
                        left: 5,
                        right: 5
                    }
                }
            }
        });
        
        tempCharts.push(chart);
        
        // Actualizar valor inicial en la tarjeta con color
        actualizarValorTarjeta(i, temperaturaActual);
    }
    
    console.log(`🌡️ ${tempCharts.length}/4 gráficas de temperatura inicializadas`);
});

// Función para actualizar el valor en la tarjeta
function actualizarValorTarjeta(plantaIndex, temperatura) {
    const tempElement = document.getElementById(`temp-value-${plantaIndex}`);
    if (tempElement) {
        tempElement.textContent = `${temperatura}°C`;
        
        // Actualizar color del texto basado en la temperatura
        const color = obtenerColorTemperatura(temperatura).border;
        tempElement.style.color = color;
        
        // Actualizar color del icono también
        const iconElement = tempElement.parentElement?.querySelector('.bi-thermometer-half');
        if (iconElement) {
            iconElement.style.color = color;
        }
        
        // Añadir clase CSS para animación
        tempElement.classList.add('valor-actualizado');
        setTimeout(() => {
            tempElement.classList.remove('valor-actualizado');
        }, 500);
    }
}

// Función para actualizar un gráfico específico (llamada desde Simulador.js)
function actualizarGraficoTemperatura(sensorIndex, nuevaTemperatura) {
    if (sensorIndex >= 0 && sensorIndex < tempCharts.length && tempCharts[sensorIndex]) {
        // Validar temperatura
        const temp = Math.max(-50, Math.min(50, parseFloat(nuevaTemperatura)));
        
        const chart = tempCharts[sensorIndex];
        
        // FIFO: Remover el primer elemento y agregar el nuevo al final
        datosTemperatura[sensorIndex].shift();
        datosTemperatura[sensorIndex].push(temp);
        
        // Actualizar etiquetas de tiempo
        const nuevasEtiquetas = generarEtiquetasTiempo();
        
        // Obtener colores basados en la nueva temperatura
        const colores = obtenerColorTemperatura(temp);
        
        // Actualizar los datos del chart
        chart.data.labels = nuevasEtiquetas;
        chart.data.datasets[0].data = [...datosTemperatura[sensorIndex]];
        chart.data.datasets[0].borderColor = colores.border;
        chart.data.datasets[0].backgroundColor = colores.background;
        chart.data.datasets[0].pointBackgroundColor = colores.border;
        
        chart.update('active');
        
        // Actualizar valor en la tarjeta
        actualizarValorTarjeta(sensorIndex + 1, temp);
        
        console.log(`🌡️ Temperatura actualizada - Planta ${sensorIndex + 1}: ${temp}°C (${obtenerEstadoTemperatura(temp)})`);
        
        return {
            success: true,
            sensorIndex,
            valor: temp,
            color: colores.border,
            estado: obtenerEstadoTemperatura(temp)
        };
    } else {
        console.warn(`⚠️ Chart de temperatura ${sensorIndex + 1} no disponible`);
        return { success: false, error: 'Chart no disponible' };
    }
}

// Función para actualizar todas las temperaturas (modo standalone sin simulador)
async function actualizarTodasLasTemperaturas() {
    try {
        const nuevasEtiquetas = generarEtiquetasTiempo();
        const resultados = [];
        const sensorIdsActuales = getSensorIds();
        
        for (let i = 0; i < 4; i++) {
            // Generar nueva temperatura
            const nuevaTemp = generarTemperatura();
            
            // Actualizar usando la función de gráficos
            const resultado = actualizarGraficoTemperatura(i, nuevaTemp);
            resultados.push(resultado);
            
            // Solo enviar a MongoDB si el simulador NO está activo
            if (typeof window !== 'undefined' && 
                (!window.simuladorAPI || !window.simuladorAPI.simuladorActivo) && 
                sensorIdsActuales[i]) {
                await enviarDatosTemperatura(sensorIdsActuales[i], nuevaTemp);
            }
        }
        
        console.log('🌡️ Temperaturas actualizadas:', datosTemperatura.map(arr => `${arr[arr.length - 1]}°C`));
        return { success: true, resultados };
    } catch (error) {
        console.error('❌ Error al actualizar temperaturas:', error);
        return { success: false, error: error.message };
    }
}

// Función para obtener estadísticas de temperatura
function obtenerEstadisticasTemperatura() {
    const temperaturas = datosTemperatura.map(arr => arr[arr.length - 1]);
    
    const estadisticas = {
        optimos: 0,
        moderados: 0,
        suboptimos: 0,
        criticos: 0,
        promedio: 0,
        minimo: Math.min(...temperaturas),
        maximo: Math.max(...temperaturas)
    };

    temperaturas.forEach(temp => {
        if (temp >= 20 && temp <= 23) estadisticas.optimos++;
        else if ((temp >= 18 && temp < 20) || (temp > 23 && temp <= 27)) estadisticas.moderados++;
        else if ((temp >= 15 && temp < 18) || (temp > 27 && temp <= 30)) estadisticas.suboptimos++;
        else estadisticas.criticos++;
    });

    estadisticas.promedio = parseFloat((temperaturas.reduce((a, b) => a + b, 0) / temperaturas.length).toFixed(1));

    return estadisticas;
}

// Exportar funciones para uso en otros módulos
window.temperaturaAPI = {
    actualizarGraficoTemperatura,
    actualizarTodasLasTemperaturas,
    obtenerColorTemperatura,
    obtenerEstadoTemperatura,
    obtenerEstadisticasTemperatura,
    generarTemperatura,
    enviarDatosTemperatura,
    // Propiedades exportadas
    charts: tempCharts,
    datosTemperatura: datosTemperatura,
    getSensorIds: getSensorIds
};

console.log("🌡️ Sistema de temperatura inicializado:");
console.log("- Rango óptimo: 20-23°C (Verde)");
console.log("- Rango moderado: 18-20°C y 23-27°C (Amarillo)");
console.log("- Rango subóptimo: 15-18°C y 27-30°C (Naranja)");
console.log("- Rango crítico: <15°C y >30°C (Rojo)");
console.log("- Intervalo de actualización: 10 segundos");
console.log("- Etiquetas dinámicas de tiempo en tiempo real");
console.log("- Dimensiones de charts: 120px altura, aspecto 2.5:1");
console.log("- ✅ Sin duplicación de sensorIds");