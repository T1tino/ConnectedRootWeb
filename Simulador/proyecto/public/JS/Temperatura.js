// Temperatura.js limpio - Sin creación de charts ni manipulación de canvas

// Datos iniciales para cada planta (últimas 7 lecturas)
let datosTemperatura = [
    [20.5, 21.2, 22.8, 21.6, 20.9, 22.1, 21.8], 
    [21.1, 22.0, 21.7, 22.3, 21.9, 20.8, 22.5],  
    [19.8, 20.5, 21.2, 20.7, 21.5, 22.0, 21.3],  
    [18.2, 19.1, 18.8, 19.5, 18.9, 19.3, 18.7]
];

// Obtener colores para temperatura según rango
function obtenerColorTemperatura(temp) {
    if (temp >= 20 && temp <= 23) {
        return { border: '#28a745', background: 'rgba(40, 167, 69, 0.2)' }; // Verde - Óptimo
    } else if ((temp >= 18 && temp < 20) || (temp > 23 && temp <= 27)) {
        return { border: '#ffc107', background: 'rgba(255, 193, 7, 0.2)' }; // Amarillo - Moderado
    } else if ((temp >= 15 && temp < 18) || (temp > 27 && temp <= 30)) {
        return { border: '#fd7e14', background: 'rgba(253, 126, 20, 0.2)' }; // Naranja - Subóptimo
    } else {
        return { border: '#dc3545', background: 'rgba(220, 53, 69, 0.2)' }; // Rojo - Crítico
    }
}

// Obtener estado textual de temperatura
function obtenerEstadoTemperatura(temp) {
    if (temp >= 20 && temp <= 23) return 'Óptimo';
    if ((temp >= 18 && temp < 20) || (temp > 23 && temp <= 27)) return 'Moderado';
    if ((temp >= 15 && temp < 18) || (temp > 27 && temp <= 30)) return 'Subóptimo';
    return 'Crítico';
}

// Generar temperatura aleatoria con distribución de probabilidades
function generarTemperatura() {
    const random = Math.random() * 100;
    if (random <= 70) return parseFloat((20 + Math.random() * 3).toFixed(1));          // 70% óptima (20-23)
    if (random <= 85) return parseFloat((23 + Math.random() * 4).toFixed(1));          // 15% moderada alta (23-27)
    if (random <= 95) return parseFloat((18 + Math.random() * 2).toFixed(1));          // 10% moderada baja (18-20)
    if (random <= 98) return parseFloat((27 + Math.random() * 8).toFixed(1));          // 3% extrema alta (27-35)
    return parseFloat((10 + Math.random() * 8).toFixed(1));                            // 2% extrema baja (10-18)
}

// Exportar funciones y datos útiles, sin charts
window.temperaturaAPI = {
    obtenerColorTemperatura,
    obtenerEstadoTemperatura,
    generarTemperatura,
    datosTemperatura
};

console.log("🌡️ Temperatura.js cargado sin gráficos, listo para usar junto a simulador.js");
