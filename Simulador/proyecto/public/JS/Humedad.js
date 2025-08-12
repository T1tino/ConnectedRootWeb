// Humedad.js Mejorado - Sistema de humedad con colores y integración con backend

document.addEventListener("DOMContentLoaded", function () {
    // Valores iniciales
    let humedades = [37, 72, 80, 65];
    let charts = []; // Array para almacenar las instancias de los charts
    
    // ✅ CORREGIDO: Obtener IDs de sensores desde el simulador
    function getSensorIds() {
        return window.simuladorAPI?.sensorIds || [
            "66b8f5e4a1234567890abcd1", // Sensor 1
            "66b8f5e4a1234567890abcd2", // Sensor 2
            "66b8f5e4a1234567890abcd3", // Sensor 3
            "66b8f5e4a1234567890abcd4"  // Sensor 4
        ];
    }

    // Función para obtener color basado en el valor de humedad
    function obtenerColorHumedad(humedad) {
        if (humedad >= 40 && humedad <= 70) {
            return '#28a745'; // Verde - Óptimo (40-70%)
        } else if ((humedad >= 30 && humedad < 40) || (humedad > 70 && humedad <= 80)) {
            return '#ffc107'; // Amarillo - Moderado
        } else if ((humedad >= 20 && humedad < 30) || (humedad > 80 && humedad <= 90)) {
            return '#fd7e14'; // Naranja - Subóptimo
        } else {
            return '#dc3545'; // Rojo - Crítico (<20% o >90%)
        }
    }

    // ✅ CORREGIDO: Función para enviar datos usando simulador API
    async function enviarDatosAMongoDB(sensorId, valor, tipo = "humedad", unidad = "%") {
        try {
            // Verificar si el simulador API está disponible
            if (!window.simuladorAPI || !window.simuladorAPI.makeAPIRequest) {
                console.warn('⚠️ SimuladorAPI no disponible, datos no enviados');
                return { success: false, error: 'SimuladorAPI no disponible' };
            }

            const datos = {
                sensorId: sensorId,
                fechaHora: new Date().toISOString(),
                tipo: tipo,
                valor: valor,
                unidad: unidad
            };

            const response = await window.simuladorAPI.makeAPIRequest('/lecturas', {
                method: 'POST',
                body: JSON.stringify(datos)
            });

            if (response.ok) {
                const result = await response.json();
                console.log(`💧 Datos de humedad enviados exitosamente para sensor ${sensorId}: ${valor}%`);
                return { success: true, data: result };
            } else {
                const errorText = await response.text();
                console.error('❌ Error al enviar datos:', response.status, errorText);
                return { success: false, error: `HTTP ${response.status}: ${errorText}` };
            }
        } catch (error) {
            console.error('❌ Error de conexión:', error);
            // En caso de error, almacenar en localStorage para reintento posterior
            almacenarEnLocalStorage(sensorId, valor, tipo, unidad);
            return { success: false, error: error.message };
        }
    }

    // Función de respaldo para almacenar en localStorage si falla la conexión
    function almacenarEnLocalStorage(sensorId, valor, tipo, unidad) {
        try {
            const datosOffline = JSON.parse(localStorage.getItem('datosOffline') || '[]');
            datosOffline.push({
                sensorId: sensorId,
                fechaHora: new Date().toISOString(),
                tipo: tipo,
                valor: valor,
                unidad: unidad,
                timestamp: Date.now()
            });
            localStorage.setItem('datosOffline', JSON.stringify(datosOffline));
            console.log('📱 Datos almacenados offline para envío posterior');
        } catch (error) {
            console.error('❌ Error al almacenar datos offline:', error);
        }
    }

    // ✅ CORREGIDO: Función para reenviar datos offline usando simulador API
    async function reenviarDatosOffline() {
        try {
            const datosOffline = JSON.parse(localStorage.getItem('datosOffline') || '[]');
            if (datosOffline.length === 0) {
                return { success: true, enviados: 0 };
            }

            // Verificar si el simulador API está disponible
            if (!window.simuladorAPI || !window.simuladorAPI.makeAPIRequest) {
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
                    // Si falla mucho, parar para no saturar
                    if (errores > 3) break;
                }
            }
            
            // Remover solo los datos que se enviaron exitosamente
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
                errores: errores 
            };
        } catch (error) {
            console.error('❌ Error en proceso de reenvío offline:', error);
            return { success: false, error: error.message };
        }
    }

    // Crear los charts iniciales
    for (let i = 0; i < humedades.length; i++) {
        const humedad = humedades[i];
        const ctx = document.getElementById(`humedadChart${i + 1}`);
        
        if (!ctx) {
            console.warn(`⚠️ Canvas humedadChart${i + 1} no encontrado`);
            continue;
        }
        
        const colorHumedad = obtenerColorHumedad(humedad);

        const chart = new Chart(ctx.getContext('2d'), {
            type: 'doughnut',
            data: {
                datasets: [{
                    data: [humedad, 100 - humedad],
                    backgroundColor: [colorHumedad, '#e9ecef'],
                    borderWidth: 0,
                    cutout: '75%',
                    borderRadius: 4,
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    tooltip: { 
                        enabled: true,
                        callbacks: {
                            label: function(context) {
                                if (context.dataIndex === 0) {
                                    const valor = context.parsed;
                                    let estado = '';
                                    if (valor >= 40 && valor <= 70) estado = ' (Óptimo)';
                                    else if ((valor >= 30 && valor < 40) || (valor > 70 && valor <= 80)) estado = ' (Moderado)';
                                    else if ((valor >= 20 && valor < 30) || (valor > 80 && valor <= 90)) estado = ' (Subóptimo)';
                                    else estado = ' (Crítico)';
                                    
                                    return `Humedad: ${valor}%${estado}`;
                                }
                                return null;
                            }
                        }
                    },
                    legend: { display: false },
                },
                animation: {
                    duration: 1000,
                    easing: 'easeInOutQuart'
                }
            }
        });

        // Guardar la instancia del chart
        charts.push(chart);

        // Actualizar el valor al centro con color correspondiente
        const labelElement = document.getElementById(`humedad-label${i + 1}`);
        if (labelElement) {
            labelElement.textContent = `${humedad}%`;
            labelElement.style.color = colorHumedad;
        }
        
        // Actualizar también en la tarjeta
        actualizarValorTarjeta(i + 1, humedad);
    }

    // Función para actualizar el valor en la tarjeta
    function actualizarValorTarjeta(plantaIndex, humedad) {
        const humedadElement = document.getElementById(`humedad-value-${plantaIndex}`);
        if (humedadElement) {
            humedadElement.textContent = `${humedad}%`;
            
            // Actualizar color del texto
            const color = obtenerColorHumedad(humedad);
            humedadElement.style.color = color;
            
            // Actualizar color del icono también
            const iconElement = humedadElement.parentElement?.querySelector('.bi-droplet-half');
            if (iconElement) {
                iconElement.style.color = color;
            }
            
            // Añadir clase CSS para animación
            humedadElement.classList.add('valor-actualizado');
            setTimeout(() => {
                humedadElement.classList.remove('valor-actualizado');
            }, 500);
        }
    }

    // Función para generar humedad con probabilidades proporcionales mejorada
    function generarHumedadConProbabilidades() {
        const random = Math.random() * 100;
        
        if (random <= 60) {
            // 60% chance: humedad óptima (40-70%)
            return parseFloat((40 + (Math.random() * 30)).toFixed(1));
        } else if (random <= 80) {
            // 20% chance: humedad moderada (30-40% o 70-80%)
            const esBaja = Math.random() < 0.5;
            return parseFloat(esBaja ? 
                (30 + (Math.random() * 10)).toFixed(1) : 
                (70 + (Math.random() * 10)).toFixed(1));
        } else if (random <= 95) {
            // 15% chance: humedad subóptima (20-30% o 80-90%)
            const esBaja = Math.random() < 0.5;
            return parseFloat(esBaja ? 
                (20 + (Math.random() * 10)).toFixed(1) : 
                (80 + (Math.random() * 10)).toFixed(1));
        } else {
            // 5% chance: humedad crítica (<20% o >90%)
            const esBaja = Math.random() < 0.5;
            return parseFloat(esBaja ? 
                (Math.random() * 20).toFixed(1) : 
                (90 + (Math.random() * 10)).toFixed(1));
        }
    }

    // Función para actualizar un gráfico específico (llamada desde Simulador.js)
    function actualizarGraficoHumedad(sensorIndex, nuevaHumedad) {
        if (sensorIndex >= 0 && sensorIndex < charts.length && charts[sensorIndex]) {
            // Validar valor de humedad
            const humedad = Math.max(0, Math.min(100, parseFloat(nuevaHumedad)));
            
            // Actualizar el array de humedades
            humedades[sensorIndex] = humedad;
            
            // Obtener el color correspondiente
            const colorHumedad = obtenerColorHumedad(humedad);
            
            // Actualizar los datos del chart con el nuevo color
            charts[sensorIndex].data.datasets[0].data = [humedad, 100 - humedad];
            charts[sensorIndex].data.datasets[0].backgroundColor = [colorHumedad, '#e9ecef'];
            charts[sensorIndex].update('active');
            
            // Actualizar el texto del centro del gráfico con color
            const labelElement = document.getElementById(`humedad-label${sensorIndex + 1}`);
            if (labelElement) {
                labelElement.textContent = `${humedad}%`;
                labelElement.style.color = colorHumedad;
            }
            
            // Actualizar valor en la tarjeta
            actualizarValorTarjeta(sensorIndex + 1, humedad);
            
            console.log(`💧 Humedad actualizada - Planta ${sensorIndex + 1}: ${humedad}%`);
            
            return {
                success: true,
                sensorIndex,
                valor: humedad,
                color: colorHumedad,
                estado: obtenerEstadoHumedad(humedad)
            };
        } else {
            console.warn(`⚠️ Chart de humedad ${sensorIndex + 1} no disponible`);
            return { success: false, error: 'Chart no disponible' };
        }
    }

    // Función para obtener estado textual de la humedad
    function obtenerEstadoHumedad(humedad) {
        if (humedad >= 40 && humedad <= 70) {
            return 'Óptimo';
        } else if ((humedad >= 30 && humedad < 40) || (humedad > 70 && humedad <= 80)) {
            return 'Moderado';
        } else if ((humedad >= 20 && humedad < 30) || (humedad > 80 && humedad <= 90)) {
            return 'Subóptimo';
        } else {
            return 'Crítico';
        }
    }

    // ✅ CORREGIDO: Función para actualizar los valores de humedad (modo standalone sin simulador)
    async function actualizarHumedad() {
        try {
            // Intentar reenviar datos offline al inicio
            const resultadoOffline = await reenviarDatosOffline();
            if (resultadoOffline.enviados > 0) {
                console.log(`📤 ${resultadoOffline.enviados} datos offline reenviados`);
            }

            const resultados = [];
            const sensorIdsActuales = getSensorIds();

            for (let i = 0; i < humedades.length; i++) {
                // Generar nueva humedad con probabilidades proporcionales
                const nuevaHumedad = generarHumedadConProbabilidades();
                
                // Actualizar usando la función de gráficos
                const resultado = actualizarGraficoHumedad(i, nuevaHumedad);
                resultados.push(resultado);

                // Solo enviar a MongoDB si el simulador NO está activo
                if (nuevaHumedad !== undefined && 
                    (!window.simuladorAPI || !window.simuladorAPI.simuladorActivo) && 
                    sensorIdsActuales[i]) {
                    const envioResultado = await enviarDatosAMongoDB(sensorIdsActuales[i], nuevaHumedad, "humedad", "%");
                    if (!envioResultado.success) {
                        console.warn(`⚠️ Error al enviar datos del sensor ${i + 1}:`, envioResultado.error);
                    }
                }
            }
            
            console.log('💧 Humedades actualizadas:', humedades.map(h => `${h}%`));
            return { success: true, resultados };
        } catch (error) {
            console.error('❌ Error al actualizar humedades:', error);
            return { success: false, error: error.message };
        }
    }

    // Función para obtener estadísticas de humedad
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

    // ✅ CORREGIDO: Verificar conexión cada 30 segundos para reenviar datos offline
    // Solo si el simulador API está disponible
    setInterval(async () => {
        if (window.simuladorAPI && window.simuladorAPI.makeAPIRequest) {
            await reenviarDatosOffline();
        }
    }, 5000);

    // Exportar funciones para uso en otros módulos
    window.humedadAPI = {
        actualizarGraficoHumedad,
        actualizarHumedad,
        obtenerColorHumedad,
        obtenerEstadoHumedad,
        generarHumedadConProbabilidades,
        enviarDatosAMongoDB,
        reenviarDatosOffline,
        obtenerEstadisticasHumedad,
        // Propiedades exportadas
        charts: charts,
        humedades: humedades,
        getSensorIds: getSensorIds
    };

    console.log("💧 Sistema de humedad inicializado:");
    console.log("- Rango óptimo: 40-70% (Verde)");
    console.log("- Rango moderado: 30-40% y 70-80% (Amarillo)");
    console.log("- Rango subóptimo: 20-30% y 80-90% (Naranja)");
    console.log("- Rango crítico: <20% y >90% (Rojo)");
    console.log("- Conexión MongoDB: A través de SimuladorAPI");
    console.log("- Sistema offline: Almacenamiento local en caso de falla de conexión");
    console.log("- Integración con simulador: Activada");
    console.log(`- Charts inicializados: ${charts.length}/4`);
    console.log("- ✅ URLs corregidas para usar SimuladorAPI");
    
    // Mostrar estadísticas iniciales
    const statsIniciales = obtenerEstadisticasHumedad();
    console.log("📊 Estadísticas iniciales:", statsIniciales);
});