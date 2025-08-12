// Simulador.js - Control del simulador para proyectos separados (SIN credentials)

// Variables globales del simulador
let simuladorActivo = false;
let connectionStatus = false;
let intervalos = [];

// IDs de sensores simulados
const sensorIds = [
    "66b8f5e4a1234567890abcd1", // Sensor 1
    "66b8f5e4a1234567890abcd2", // Sensor 2
    "66b8f5e4a1234567890abcd3", // Sensor 3
    "66b8f5e4a1234567890abcd4"  // Sensor 4
];

// ✅ CLAVE: Configuración para proyectos separados
const BACKEND_CONFIG = {
    baseUrl: 'http://localhost:777', // ✅ YA DETECTADO CORRECTAMENTE
    apiPath: '/api'
};

// Construir URL completa del API
const API_BASE_URL = `${BACKEND_CONFIG.baseUrl}${BACKEND_CONFIG.apiPath}`;

console.log(`🔗 Configuración API: ${API_BASE_URL}`);

// Función para detectar automáticamente el puerto del backend
async function detectarBackend() {
    const puertosPosibles = [777, 5000, 5001, 7000, 7001, 44300, 44301]; // ✅ Agregado 777 al inicio
    
    for (const puerto of puertosPosibles) {
        try {
            const url = `http://localhost:${puerto}/health`;
            const response = await fetch(url, { 
                method: 'GET',
                mode: 'cors'
                // ✅ REMOVIDO: credentials: 'include'
            });
            
            if (response.ok) {
                const data = await response.json();
                if (data.status === 'healthy') {
                    BACKEND_CONFIG.baseUrl = `http://localhost:${puerto}`;
                    console.log(`✅ Backend detectado en puerto ${puerto}`);
                    return puerto;
                }
            }
        } catch (error) {
            // Continuar con el siguiente puerto
        }
    }
    
    console.warn('⚠️ No se pudo detectar automáticamente el backend');
    return null;
}

// Inicialización cuando se carga la página
document.addEventListener("DOMContentLoaded", async function () {
    console.log("🚀 Simulador Connected Root inicializado (modo separado)");
    
    // Intentar detectar el backend automáticamente
    const puertoDetectado = await detectarBackend();
    
    if (puertoDetectado) {
        // Actualizar la URL base del API
        const API_BASE_URL_UPDATED = `${BACKEND_CONFIG.baseUrl}${BACKEND_CONFIG.apiPath}`;
        window.API_BASE_URL = API_BASE_URL_UPDATED;
        
        // Verificar conexión inicial
        verificarConexion();
        
        // Verificar conexión cada 30 segundos
        setInterval(verificarConexion, 30000);
        
        // Obtener estado inicial del simulador
        verificarEstadoSimulador();
    } else {
        mostrarAlert('No se pudo conectar al backend. Verifica que ConnectedRoot esté corriendo.', 'danger');
        actualizarEstadoConexion(false);
    }
});

// ✅ CORREGIDO: Función para hacer requests SIN credentials
async function makeAPIRequest(endpoint, options = {}) {
    const url = `${BACKEND_CONFIG.baseUrl}${BACKEND_CONFIG.apiPath}${endpoint}`;
    
    const defaultOptions = {
        mode: 'cors',
        // ✅ REMOVIDO: credentials: 'include'
        headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        }
    };
    
    const finalOptions = { ...defaultOptions, ...options };
    
    console.log(`🌐 API Request: ${finalOptions.method || 'GET'} ${url}`);
    
    try {
        const response = await fetch(url, finalOptions);
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        return response;
    } catch (error) {
        console.error(`❌ API Error for ${endpoint}:`, error);
        throw error;
    }
}

// Función para alternar el simulador
async function toggleSimulador() {
    if (simuladorActivo) {
        await detenerSimulador();
    } else {
        await iniciarSimulador();
    }
}

// Función para iniciar el simulador
async function iniciarSimulador() {
    try {
        actualizarUISimulador('iniciando');
        
        const response = await makeAPIRequest('/simulador/iniciar-todos', {
            method: 'POST'
        });

        if (response.ok) {
            const data = await response.json();
            
            if (data.success) {
                simuladorActivo = true;
                actualizarUISimulador('activo');
                mostrarAlert(`Simulador iniciado: ${data.exitosos}/${data.totalSensores} sensores activos`, 'success');
                console.log("✅ Simulador iniciado para todos los sensores:", data);
                
                iniciarObtenerDatos();
            } else {
                throw new Error(data.message || 'Error desconocido');
            }
        } else {
            // Fallback al método individual
            await iniciarSimuladorIndividual();
        }
        
    } catch (error) {
        console.error('❌ Error al iniciar simulador:', error);
        mostrarAlert('Error al iniciar el simulador: ' + error.message, 'danger');
        actualizarUISimulador('error');
    }
}

// Método de fallback para iniciar sensores individualmente
async function iniciarSimuladorIndividual() {
    try {
        const promesas = sensorIds.map(sensorId => 
            makeAPIRequest('/simulador/iniciar', {
                method: 'POST',
                body: JSON.stringify({ sensorId })
            })
        );

        const respuestas = await Promise.all(promesas);
        const exitosos = respuestas.filter(r => r.ok).length;
        
        if (exitosos > 0) {
            simuladorActivo = true;
            actualizarUISimulador('activo');
            mostrarAlert(`Simulador iniciado: ${exitosos}/${sensorIds.length} sensores activos`, 'success');
            console.log(`✅ Simulador iniciado para ${exitosos} sensores`);
            iniciarObtenerDatos();
        } else {
            throw new Error('No se pudo iniciar ningún sensor');
        }
    } catch (error) {
        throw new Error(`Fallback falló: ${error.message}`);
    }
}

// Función para detener el simulador
async function detenerSimulador() {
    try {
        actualizarUISimulador('deteniendo');
        
        const response = await makeAPIRequest('/simulador/detener-todos', {
            method: 'POST'
        });

        if (response.ok) {
            const data = await response.json();
            
            simuladorActivo = false;
            actualizarUISimulador('detenido');
            mostrarAlert(data.message || 'Simulador detenido', 'warning');
            console.log("⏹️ Simulador detenido:", data);
            
            detenerObtenerDatos();
        } else {
            await detenerSimuladorIndividual();
        }
        
    } catch (error) {
        console.error('❌ Error al detener simulador:', error);
        mostrarAlert('Error al detener el simulador: ' + error.message, 'danger');
        await detenerSimuladorIndividual();
    }
}

// Método de fallback para detener sensores individualmente
async function detenerSimuladorIndividual() {
    try {
        const promesas = sensorIds.map(sensorId => 
            makeAPIRequest('/simulador/detener', {
                method: 'POST',
                body: JSON.stringify({ sensorId })
            })
        );

        await Promise.all(promesas);
        
        simuladorActivo = false;
        actualizarUISimulador('detenido');
        mostrarAlert('Simulador detenido (método individual)', 'warning');
        console.log("⏹️ Simulador detenido usando método individual");
        
        detenerObtenerDatos();
    } catch (error) {
        console.error('❌ Error en fallback de detener:', error);
        simuladorActivo = false;
        actualizarUISimulador('detenido');
        detenerObtenerDatos();
    }
}

// Función para obtener datos desde el backend
function iniciarObtenerDatos() {
    detenerObtenerDatos();
    
    const intervalo = setInterval(async () => {
        if (!simuladorActivo) {
            detenerObtenerDatos();
            return;
        }
        
        await obtenerUltimasLecturas();
    }, 10000);
    
    intervalos.push(intervalo);
    obtenerUltimasLecturas();
}

function detenerObtenerDatos() {
    intervalos.forEach(intervalo => clearInterval(intervalo));
    intervalos = [];
}

// Función para obtener las últimas lecturas
async function obtenerUltimasLecturas() {
    try {
        let lecturasObtenidas = 0;
        
        for (let i = 0; i < sensorIds.length; i++) {
            const sensorId = sensorIds[i];
            
            try {
                const response = await makeAPIRequest(`/lecturas/ultimas/${sensorId}?limit=1`);
                
                if (response.ok) {
                    const data = await response.json();
                    
                    if (data.success && data.data && data.data.length > 0) {
                        const lectura = data.data[0];
                        lecturasObtenidas++;
                        
                        if (lectura.tipo === 'temperatura') {
                            actualizarTemperaturaUI(i, lectura.valor);
                        } else if (lectura.tipo === 'humedad') {
                            actualizarHumedadUI(i, lectura.valor);
                        }
                    }
                }
            } catch (sensorError) {
                console.warn(`⚠️ Error en sensor ${i + 1}: ${sensorError.message}`);
            }
        }
        
        actualizarEstadoConexion(lecturasObtenidas > 0);
        
        if (lecturasObtenidas > 0) {
            console.log(`📊 Lecturas obtenidas: ${lecturasObtenidas}/${sensorIds.length}`);
        }
        
    } catch (error) {
        console.error('❌ Error al obtener lecturas:', error);
        actualizarEstadoConexion(false);
    }
}

// Función para verificar conexión
async function verificarConexion() {
    try {
        const response = await makeAPIRequest('/simulador/sensores');
        
        if (response.ok) {
            const data = await response.json();
            actualizarEstadoConexion(true);
            
            if (data.success && data.sensores) {
                console.log(`🔗 Conexión OK - ${data.total} sensores, ${data.activos} simuladores activos`);
            }
        } else {
            console.warn(`⚠️ Error de conexión: ${response.status}`);
            actualizarEstadoConexion(false);
        }
    } catch (error) {
        console.warn('⚠️ Error de conexión:', error.message);
        actualizarEstadoConexion(false);
    }
}

// Función para verificar estado inicial
async function verificarEstadoSimulador() {
    try {
        const response = await makeAPIRequest('/simulador/estado');
        
        if (response.ok) {
            const data = await response.json();
            
            if (data.success && data.simuladoresActivos > 0) {
                simuladorActivo = true;
                actualizarUISimulador('activo');
                iniciarObtenerDatos();
                console.log(`✅ Estado inicial: ${data.simuladoresActivos} simuladores activos`);
                return;
            }
        }
        
        // Fallback
        const responseSensores = await makeAPIRequest('/simulador/sensores');
        
        if (responseSensores.ok) {
            const data = await responseSensores.json();
            
            if (data.success && data.sensores) {
                const algunSimuladorActivo = data.sensores.some(s => s.SimuladorActivo);
                
                if (algunSimuladorActivo) {
                    simuladorActivo = true;
                    actualizarUISimulador('activo');
                    iniciarObtenerDatos();
                } else {
                    simuladorActivo = false;
                    actualizarUISimulador('detenido');
                }
            }
        }
    } catch (error) {
        console.warn('⚠️ No se pudo verificar el estado del simulador:', error.message);
        actualizarUISimulador('detenido');
    }
}

// [Resto de las funciones UI se mantienen igual]

function actualizarUISimulador(estado) {
    const toggleBtn = document.getElementById('toggleSimulador');
    const icon = document.getElementById('simuladorIcon');
    const text = document.getElementById('simuladorText');
    const status = document.getElementById('simuladorStatus');
    
    if (!toggleBtn || !icon || !text || !status) {
        console.warn('⚠️ Elementos UI del simulador no encontrados');
        return;
    }
    
    switch (estado) {
        case 'activo':
            toggleBtn.className = 'btn btn-outline-danger';
            icon.className = 'bi bi-stop-circle';
            text.textContent = 'Detener Simulador';
            status.className = 'badge bg-success';
            status.textContent = 'Activo';
            break;
            
        case 'detenido':
            toggleBtn.className = 'btn btn-outline-light';
            icon.className = 'bi bi-play-circle';
            text.textContent = 'Iniciar Simulador';
            status.className = 'badge bg-secondary';
            status.textContent = 'Detenido';
            break;
            
        case 'iniciando':
            toggleBtn.className = 'btn btn-outline-warning';
            toggleBtn.disabled = true;
            icon.className = 'bi bi-hourglass-split';
            text.textContent = 'Iniciando...';
            status.className = 'badge bg-warning';
            status.textContent = 'Iniciando';
            break;
            
        case 'deteniendo':
            toggleBtn.className = 'btn btn-outline-warning';
            toggleBtn.disabled = true;
            icon.className = 'bi bi-hourglass-split';
            text.textContent = 'Deteniendo...';
            status.className = 'badge bg-warning';
            status.textContent = 'Deteniendo';
            break;
            
        case 'error':
            toggleBtn.className = 'btn btn-outline-danger';
            icon.className = 'bi bi-exclamation-triangle';
            text.textContent = 'Error - Reintentar';
            status.className = 'badge bg-danger';
            status.textContent = 'Error';
            break;
    }
    
    if (estado !== 'iniciando' && estado !== 'deteniendo') {
        toggleBtn.disabled = false;
    }
}

function mostrarAlert(mensaje, tipo) {
    const alert = document.getElementById('simuladorAlert');
    const alertText = document.getElementById('simuladorAlertText');
    
    if (!alert || !alertText) {
        console.warn('⚠️ Elementos de alerta no encontrados');
        return;
    }
    
    alert.className = `alert alert-${tipo} alert-dismissible fade show`;
    alert.style.display = 'block';
    alertText.textContent = mensaje;
    
    setTimeout(() => {
        cerrarAlert();
    }, 5000);
}

function cerrarAlert() {
    const alert = document.getElementById('simuladorAlert');
    if (alert) {
        alert.style.display = 'none';
    }
}

function actualizarEstadoConexion(conectado) {
    const statusElement = document.getElementById('connection-status');
    
    if (!statusElement) {
        console.warn('⚠️ Elemento connection-status no encontrado');
        return;
    }
    
    if (conectado) {
        statusElement.className = 'connection-status connection-online';
        statusElement.innerHTML = '<i class="bi bi-wifi"></i> Conectado';
        connectionStatus = true;
    } else {
        statusElement.className = 'connection-status connection-offline';
        statusElement.innerHTML = '<i class="bi bi-wifi-off"></i> Desconectado';
        connectionStatus = false;
    }
}

function actualizarTemperaturaUI(sensorIndex, valor) {
    const tempElement = document.getElementById(`temp-value-${sensorIndex + 1}`);
    if (tempElement) {
        tempElement.textContent = `${valor}°C`;
        tempElement.style.color = obtenerColorTemperatura(valor);
        
        tempElement.classList.add('valor-actualizado');
        setTimeout(() => {
            tempElement.classList.remove('valor-actualizado');
        }, 500);
    }
    
    if (typeof window.temperaturaAPI !== 'undefined' && 
        typeof window.temperaturaAPI.actualizarGraficoTemperatura === 'function') {
        window.temperaturaAPI.actualizarGraficoTemperatura(sensorIndex, valor);
    }
}

function actualizarHumedadUI(sensorIndex, valor) {
    const humedadElement = document.getElementById(`humedad-value-${sensorIndex + 1}`);
    if (humedadElement) {
        humedadElement.textContent = `${valor}%`;
        humedadElement.style.color = obtenerColorHumedad(valor);
        
        humedadElement.classList.add('valor-actualizado');
        setTimeout(() => {
            humedadElement.classList.remove('valor-actualizado');
        }, 500);
    }
    
    if (typeof window.humedadAPI !== 'undefined' && 
        typeof window.humedadAPI.actualizarGraficoHumedad === 'function') {
        window.humedadAPI.actualizarGraficoHumedad(sensorIndex, valor);
    }
}

function obtenerColorTemperatura(temp) {
    if (temp >= 20 && temp <= 23) {
        return '#28a745';
    } else if ((temp >= 18 && temp < 20) || (temp > 23 && temp <= 27)) {
        return '#ffc107';
    } else if ((temp >= 15 && temp < 18) || (temp > 27 && temp <= 30)) {
        return '#fd7e14';
    } else {
        return '#dc3545';
    }
}

function obtenerColorHumedad(humedad) {
    if (humedad >= 40 && humedad <= 70) {
        return '#28a745';
    } else if ((humedad >= 30 && humedad < 40) || (humedad > 70 && humedad <= 80)) {
        return '#ffc107';
    } else if ((humedad >= 20 && humedad < 30) || (humedad > 80 && humedad <= 90)) {
        return '#fd7e14';
    } else {
        return '#dc3545';
    }
}

// Limpiar recursos al cerrar la ventana
window.addEventListener('beforeunload', async () => {
    if (simuladorActivo) {
        try {
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 1000);
            
            await makeAPIRequest('/simulador/limpiar', {
                method: 'POST',
                signal: controller.signal
            });
            
            clearTimeout(timeoutId);
            console.log('🧹 Recursos limpiados al cerrar');
        } catch (error) {
            console.warn('⚠️ Error al limpiar recursos:', error.message);
        }
    }
});

// Exportar funciones
window.simuladorAPI = {
    iniciarSimulador,
    detenerSimulador,
    verificarConexion,
    obtenerUltimasLecturas,
    actualizarTemperaturaUI,
    actualizarHumedadUI,
    obtenerColorTemperatura,
    obtenerColorHumedad,
    verificarEstadoSimulador,
    mostrarAlert,
    cerrarAlert,
    makeAPIRequest, // ✅ NUEVO: Exponer para uso en otros módulos
    config: BACKEND_CONFIG,
    sensorIds: sensorIds
};

console.log('✅ Simulador.js cargado completamente (modo proyectos separados SIN credentials)');