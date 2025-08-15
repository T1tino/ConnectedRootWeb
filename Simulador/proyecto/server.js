// Simulador/proyecto/server.js
const express = require('express');
const mongoose = require('mongoose');
const cors = require('cors');
const path = require('path');

const app = express();
const PORT = process.env.PORT || 3000;

app.use(cors());
app.use(express.json());
app.use(express.static('public'));

// URI Atlas con logging
const MONGODB_URI = process.env.MONGODB_URI || 
    'mongodb+srv://0323105932:0323105932@rootdb.mnmbdoa.mongodb.net/RootDB';

console.log('🔗 Configuración MongoDB:');
console.log('   URI:', MONGODB_URI);
console.log('   Base de datos extraída:', MONGODB_URI.split('/').pop().split('?')[0]);

// Conexión a MongoDB con logging detallado
let mongoConnected = false;

mongoose.connection.on('connecting', () => {
    console.log('🔄 Conectando a MongoDB...');
});

mongoose.connection.on('connected', () => {
    console.log('✅ Conexión establecida a MongoDB');
    console.log('   Host:', mongoose.connection.host);
    console.log('   Puerto:', mongoose.connection.port);
    console.log('   Base de datos:', mongoose.connection.name);
});

mongoose.connection.on('error', (err) => {
    console.error('❌ Error de conexión MongoDB:', err.message);
    console.error('   Código de error:', err.code);
    console.error('   Stack:', err.stack);
});

mongoose.connection.on('disconnected', () => {
    console.log('⚠️ Desconectado de MongoDB');
    mongoConnected = false;
});

mongoose.connect(MONGODB_URI, { 
    useNewUrlParser: true, 
    useUnifiedTopology: true,
    serverSelectionTimeoutMS: 5000,
    heartbeatFrequencyMS: 2000
})
.then(() => {
    mongoConnected = true;
    console.log('✅ Conectado a MongoDB Atlas exitosamente');
    console.log('📊 Estado de la conexión:', mongoose.connection.readyState);
    console.log('🏷️  Colecciones disponibles: verificando...');
    
    mongoose.connection.db.listCollections().toArray()
        .then(collections => {
            console.log('📁 Colecciones encontradas:', collections.map(c => c.name));
        })
        .catch(err => console.log('⚠️ No se pudieron listar colecciones:', err.message));
})
.catch(err => {
    mongoConnected = false;
    console.error('⚠️ No se pudo conectar a MongoDB Atlas');
    console.error('   Error:', err.message);
    console.error('   Código:', err.code);
    console.error('   URI usada:', MONGODB_URI.replace(/\/\/.*:.*@/, '//***:***@'));
    console.error('   Usando simulador local');
});

// ============================================
// CAMBIO PRINCIPAL: Schema corregido con ObjectId
// ============================================
const lecturaSchema = new mongoose.Schema({
    sensorId: { 
        type: mongoose.Schema.Types.ObjectId, // ✅ Cambiado de Number a ObjectId
        required: true 
    },
    tipo: { type: String, enum: ['temperatura','humedad'], required: true },
    valor: { type: Number, required: true },
    unidad: { type: String, required: true },
    fechaHora: { type: Date, default: Date.now }
});

console.log('📋 Schema de Lectura definido:');
console.log('   Campos requeridos: sensorId (ObjectId), tipo, valor, unidad');
console.log('   Tipos permitidos: temperatura, humedad');
console.log('   Colección destino: Lecturas');

const Lectura = mongoose.model('Lectura', lecturaSchema, 'Lecturas');

// ============================================
// MAPEO DE SENSORES: números a ObjectIds
// ============================================
const sensorMap = {
    1: new mongoose.Types.ObjectId("648a1b2c3d4e5f6789012341"),
    2: new mongoose.Types.ObjectId("648a1b2c3d4e5f6789012342"), 
    3: new mongoose.Types.ObjectId("648a1b2c3d4e5f6789012343"),
    4: new mongoose.Types.ObjectId("648a1b2c3d4e5f6789012344")
};

console.log('🗺️  Mapeo de sensores creado:');
Object.entries(sensorMap).forEach(([num, id]) => {
    console.log(`   Sensor ${num} → ${id}`);
});

// Simulador actualizado
function generarLectura(sensorId) {
    return {
        sensorId: sensorMap[sensorId], // ✅ Usar ObjectId del mapeo
        tipo: Math.random() > 0.5 ? 'temperatura' : 'humedad',
        valor: Math.round(Math.random()*100),
        unidad: Math.random() > 0.5 ? '°C' : '%',
        fechaHora: new Date()
    };
}

// Endpoints

// Health
app.get('/health', (req,res) => {
    res.json({
        status: 'healthy',
        mongo: mongoConnected ? 'connected' : 'offline',
        timestamp: new Date()
    });
});

// Health check endpoint para verificarConexion()
app.head('/api/lecturas', (req, res) => {
    res.status(200).send();
});

app.get('/api/lecturas/health', (req, res) => {
    res.json({
        status: 'ok',
        endpoint: '/api/lecturas',
        mongo: mongoConnected ? 'connected' : 'offline',
        timestamp: new Date()
    });
});

// Lecturas últimas 4 sensores - ACTUALIZADO para ObjectIds
app.get('/api/simulador/sensores', async (req,res) => {
    const sensores = [1,2,3,4];
    const lecturas = [];

    for (let id of sensores) {
        if(mongoConnected){
            // ✅ Buscar por ObjectId usando el mapeo
            const doc = await Lectura.find({ sensorId: sensorMap[id] }).sort({ fechaHora: -1 }).limit(1);
            lecturas.push(doc.length > 0 ? doc[0] : generarLectura(id));
        } else {
            lecturas.push(generarLectura(id));
        }
    }

    res.json({ data: lecturas, source: mongoConnected ? 'Atlas' : 'Simulador' });
});

// POST lecturas con conversión de números a ObjectIds
app.post('/api/lecturas', async (req,res) => {
    const timestamp = new Date().toISOString();
    console.log('\n=== POST /api/lecturas DEBUG ===');
    console.log(`⏰ Timestamp: ${timestamp}`);
    console.log('📥 Headers recibidos:', {
        'content-type': req.headers['content-type'],
        'origin': req.headers['origin'],
        'user-agent': req.headers['user-agent']?.substring(0, 50) + '...'
    });
    console.log('📊 Body completo:', JSON.stringify(req.body, null, 2));
    
    console.log('🔗 Estado MongoDB:');
    console.log('   mongoConnected:', mongoConnected);
    console.log('   connection.readyState:', mongoose.connection.readyState);
    console.log('   connection.name:', mongoose.connection.name || 'undefined');
    
    let lecturasArray = [];
    
    if (req.body.lecturas && Array.isArray(req.body.lecturas)) {
        console.log('📦 Formato detectado: Array de lecturas');
        console.log('   Cantidad de lecturas:', req.body.lecturas.length);
        
        lecturasArray = [];
        for (let i = 0; i < req.body.lecturas.length; i++) {
            const lecturaOriginal = req.body.lecturas[i];
            console.log(`🔄 Transformando lectura ${i+1}:`, JSON.stringify(lecturaOriginal, null, 2));
            
            // ✅ CONVERSIÓN CRÍTICA: número a ObjectId
            const sensorObjectId = sensorMap[lecturaOriginal.sensorId];
            if (!sensorObjectId) {
                console.error(`❌ SensorId ${lecturaOriginal.sensorId} no encontrado en el mapeo`);
                return res.status(400).json({
                    error: `SensorId ${lecturaOriginal.sensorId} no es válido`,
                    validSensorIds: Object.keys(sensorMap),
                    lecturaIndex: i
                });
            }
            
            console.log(`🔀 Convirtiendo sensorId ${lecturaOriginal.sensorId} → ${sensorObjectId}`);
            
            if (lecturaOriginal.temperatura !== undefined) {
                const tempLectura = {
                    sensorId: sensorObjectId, // ✅ ObjectId en lugar de número
                    tipo: 'temperatura',
                    valor: lecturaOriginal.temperatura,
                    unidad: '°C',
                    fechaHora: new Date(lecturaOriginal.timestamp || new Date())
                };
                lecturasArray.push(tempLectura);
                console.log(`   ✅ Agregada temperatura: Sensor ${sensorObjectId} = ${tempLectura.valor}°C`);
            }
            
            if (lecturaOriginal.humedad !== undefined) {
                const humLectura = {
                    sensorId: sensorObjectId, // ✅ ObjectId en lugar de número
                    tipo: 'humedad',
                    valor: lecturaOriginal.humedad,
                    unidad: '%',
                    fechaHora: new Date(lecturaOriginal.timestamp || new Date())
                };
                lecturasArray.push(humLectura);
                console.log(`   ✅ Agregada humedad: Sensor ${sensorObjectId} = ${humLectura.valor}%`);
            }
        }
        
    } else if (req.body.sensorId || req.body.tipo) {
        console.log('📝 Formato detectado: Lectura individual');
        
        // ✅ Convertir sensorId de número a ObjectId también para lecturas individuales
        const sensorObjectId = sensorMap[req.body.sensorId];
        if (!sensorObjectId) {
            return res.status(400).json({
                error: `SensorId ${req.body.sensorId} no es válido`,
                validSensorIds: Object.keys(sensorMap)
            });
        }
        
        lecturasArray = [{
            ...req.body,
            sensorId: sensorObjectId, // ✅ Reemplazar con ObjectId
            fechaHora: new Date(req.body.fechaHora || new Date())
        }];
        
    } else {
        console.log('❌ Formato no reconocido');
        return res.status(400).json({
            error: 'Formato de datos no válido',
            expected: 'Array de lecturas: {lecturas: [...]} o lectura individual: {sensorId, tipo, valor, unidad}',
            received: Object.keys(req.body)
        });
    }
    
    // Validación actualizada
    console.log('🔍 Validando lecturas individuales:');
    for (let i = 0; i < lecturasArray.length; i++) {
        const lectura = lecturasArray[i];
        const { sensorId, tipo, valor, unidad } = lectura;
        
        console.log(`   Lectura ${i+1}:`, {
            sensorId: `${sensorId} (${typeof sensorId})`, // Ahora debería ser ObjectId
            tipo: `${tipo} (${typeof tipo})`,
            valor: `${valor} (${typeof valor})`,
            unidad: `${unidad} (${typeof unidad})`
        });
        
        if (!sensorId || !tipo || valor === undefined || !unidad) {
            console.log(`❌ Lectura ${i+1} inválida - datos incompletos`);
            return res.status(400).json({
                error: `Lectura ${i+1} tiene datos incompletos`,
                required: ['sensorId', 'tipo', 'valor', 'unidad'],
                received: lectura,
                lecturaIndex: i
            });
        }
    }

    if(!mongoConnected || mongoose.connection.readyState !== 1){
        console.log('⚠️ MongoDB no disponible');
        return res.status(503).json({
            error:'No conectado a MongoDB, datos no guardados',
            simulated: true,
            data: req.body,
            mongoState: {
                connected: mongoConnected,
                readyState: mongoose.connection.readyState
            }
        });
    }

    try{
        const savedLecturas = [];
        console.log(`💾 Procesando ${lecturasArray.length} lecturas...`);
        
        for (let i = 0; i < lecturasArray.length; i++) {
            const lecturaData = lecturasArray[i];
            console.log(`📄 Creando documento ${i+1}:`, JSON.stringify(lecturaData, null, 2));
            
            const lectura = new Lectura(lecturaData);
            console.log(`📋 Documento Mongoose creado:`, JSON.stringify(lectura.toObject(), null, 2));
            
            console.log(`💿 Guardando lectura ${i+1} en MongoDB...`);
            
            try {
                const saved = await lectura.save();
                savedLecturas.push(saved);
                console.log(`✅ Lectura ${i+1} guardada con ID: ${saved._id}`);
            } catch (individualError) {
                console.error(`❌ ERROR en lectura ${i+1}:`);
                console.error('   Documento que falló:', JSON.stringify(lecturaData, null, 2));
                console.error('   Error:', individualError.message);
                console.error('   Código:', individualError.code);
                console.error('   Detalles:', individualError);
                continue;
            }
        }
        
        console.log('🎉 TODAS LAS LECTURAS GUARDADAS EXITOSAMENTE!');
        console.log(`📊 Total guardado: ${savedLecturas.length} lecturas`);
        
        res.status(201).json({ 
            success: true,
            count: savedLecturas.length,
            data: savedLecturas,
            meta: {
                collection: 'lecturas',
                database: mongoose.connection.name,
                timestamp: timestamp,
                simulatorId: req.body.simulatorId || null
            }
        });
        
    } catch(err){
        console.error('\n❌ ERROR AL GUARDAR:');
        console.error('   Mensaje:', err.message);
        console.error('   Nombre:', err.name);
        console.error('   Código:', err.code);
        
        if (err.errors) {
            console.error('   Errores de validación:');
            Object.keys(err.errors).forEach(key => {
                console.error(`     ${key}:`, err.errors[key].message);
            });
        }
        
        console.error('   Stack completo:', err.stack);
        
        res.status(500).json({ 
            error: err.message,
            code: err.code,
            name: err.name,
            type: 'database_error',
            validation: err.errors || null,
            timestamp: timestamp
        });
    }
    
    console.log('=== FIN DEBUG POST ===\n');
});

// Servir Index.html
app.get('/', (req,res) => res.sendFile(path.join(__dirname,'public','Index.html')));

app.listen(PORT, () => {
    console.log(`🚀 Server ejecutándose en http://localhost:${PORT}`);
});