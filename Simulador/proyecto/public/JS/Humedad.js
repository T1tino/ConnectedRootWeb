// Humedad.js limpio - Sin creación ni manipulación de charts o DOM

// Valores iniciales de humedad para 4 sensores
let humedades = [37, 72, 80, 65];

// Obtener IDs de sensores (desde simulador o default)
function getSensorIds() {
    return window.simuladorAPI?.sensorIds || [
        "66b8f5e4a1234567890abcd1",
        "66b8f5e4a1234567890abcd2",
        "66b8f5e4a1234567890abcd3",
        "66b8f5e4a1234567890abcd4"
    ];
}

// Obtener color según nivel de humedad
function obtenerColorHumedad(humedad) {
    if (humedad >= 40 && humedad <= 70) return '#28a745'; // Verde - Óptimo
    if ((humedad >= 30 && humedad < 40) || (humedad > 70 && humedad <= 80)) return '#ffc107'; // Amarillo - Moderado
    if ((humedad >= 20 && humedad < 30) || (humedad > 80 && humedad <= 90)) return '#fd7e14'; // Naranja - Subóptimo
    return '#dc3545'; // Rojo - Crítico
}

// Estado textual de humedad
function obtenerEstadoHumedad(humedad) {
    if (humedad >= 40 && humedad <= 70) return 'Óptimo';
    if ((humedad >= 30 && humedad < 40) || (humedad > 70 && humedad <= 80)) return 'Moderado';
    if ((humedad >= 20 && humedad < 30) || (humedad > 80 && humedad <= 90)) return 'Subóptimo';
    return 'Crítico';
}

// Enviar datos a backend vía SimuladorAPI
async function enviarDatosAMongoDB(sensorId, valor, tipo = "humedad", unidad = "%") {
    try {
        if (!window.simuladorAPI?.makeAPIRequest) {
            console.warn('⚠️ SimuladorAPI no disponible, datos no enviados');
            return { success: false, error: 'SimuladorAPI no disponible' };
        }

        const datos = {
            sensorId,
            fechaHora: new Date().toISOString(),
            tipo,
            valor,
            unidad
        };

        const response = await window.simuladorAPI.makeAPIRequest('/lecturas', {
            method: 'POST',
            body: JSON.stringify(datos)
        });

        if (response.ok) {
            const result = await response.json();
            console.log(`💧 Datos de humedad enviados para sensor ${sensorId}: ${valor}%`);
            return { success: true, data: result };
        } else {
            const errorText = await response.text();
            console.error('❌ Error al enviar datos:', response.status, errorText);
            return { success: false, error: `HTTP ${response.status}: ${errorText}` };
        }
    } catch (error) {
        console.error('❌ Error de conexión:', error);
        almacenarEnLocalStorage(sensorId, valor, tipo, unidad);
        return { success: false, error: error.message };
    }
}

// Almacenar datos offline en localStorage para reintentos
function almacenarEnLocalStorage(sensorId, valor, tipo, unidad) {
    try {
        const datosOffline = JSON.parse(localStorage.getItem('datosOffline') || '[]');
        datosOffline.push({
            sensorId,
            fechaHora: new Date().toISOString(),
            tipo,
            valor,
            unidad,
            timestamp: Date.now()
        });
        localStorage.setItem('datosOffline', JSON.stringify(datosOffline));
        console.log('📱 Datos almacenados offline para envío posterior');
    } catch (error) {
        console.error('❌ Error al almacenar datos offline:', error);
    }
}

// Reenviar datos offline almacenados
async function reenviarDatosOffline() {
    try {
        const datosOffline = JSON.parse(localStorage.getItem('datosOffline') || '[]');
        if (datosOffline.length === 0) return { success: true, enviados: 0 };

        if (!window.simuladorAPI?.makeAPIRequest) {
            console.warn('⚠️ SimuladorAPI no disponible para reenvío offline');
            return { success: false, error: 'SimuladorAPI no disponible' };
        }

        console.log(`🔄 Reenviando ${datosOffline.length} registros offline...`);
        const datosEnviados = [];
        let errores = 0;

        for (const dato of datosOffline) {
            try {
                const response = await window.simuladorAPI.makeAPIRequest('/lecturas', {
                    method: 'POST',
                    body: JSON.stringify(dato)
                });

                if (response.ok) {
                    console.log(`✅ Dato offline reenviado: ${dato.valor}${dato.unidad} del sensor ${dato.sensorId}`);
                    datosEnviados.push(dato);
                } else {
                    errores++;
                    console.warn(`⚠️ Error al reenviar dato: ${response.status}`);
                }
            } catch (error) {
                errores++;
                console.error('❌ Error al reenviar dato offline:', error);
                if (errores > 3) break;
            }
        }

        if (datosEnviados.length > 0) {
            const datosRestantes = datosOffline.filter(dato =>
                !datosEnviados.some(enviado => enviado.timestamp === dato.timestamp)
            );
            localStorage.setItem('datosOffline', JSON.stringify(datosRestantes));
            console.log(`🗂️ ${datosEnviados.length} datos offline procesados, ${datosRestantes.length} pendientes`);
        }

        return {
            success: true,
            enviados: datosEnviados.length,
            pendientes: datosOffline.length - datosEnviados.length,
            errores
        };
    } catch (error) {
        console.error('❌ Error en proceso de reenvío offline:', error);
        return { success: false, error: error.message };
    }
}

// Generar humedad con probabilidades
function generarHumedadConProbabilidades() {
    const random = Math.random() * 100;

    if (random <= 60) {
        return parseFloat((40 + Math.random() * 30).toFixed(1));
    } else if (random <= 80) {
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

// Obtener estadísticas actuales
function obtenerEstadisticasHumedad() {
    const estadisticas = {
        optimos: 0,
        moderados: 0,
        suboptimos: 0,
        criticos: 0,
        promedio: 0,
        minimo: Math.min(...humedades),
        maximo: Math.max(...humedades)
    };

    humedades.forEach(h => {
        if (h >= 40 && h <= 70) estadisticas.optimos++;
        else if ((h >= 30 && h < 40) || (h > 70 && h <= 80)) estadisticas.moderados++;
        else if ((h >= 20 && h < 30) || (h > 80 && h <= 90)) estadisticas.suboptimos++;
        else estadisticas.criticos++;
    });

    estadisticas.promedio = parseFloat((humedades.reduce((a, b) => a + b, 0) / humedades.length).toFixed(1));
    return estadisticas;
}

// Exportar funciones y datos para uso externo
window.humedadAPI = {
    obtenerColorHumedad,
    obtenerEstadoHumedad,
    enviarDatosAMongoDB,
    reenviarDatosOffline,
    almacenarEnLocalStorage,
    generarHumedadConProbabilidades,
    obtenerEstadisticasHumedad,
    humedades,
    getSensorIds
};

console.log("💧 Humedad.js cargado sin gráficos ni DOM, listo para integrar con simulador.js");
