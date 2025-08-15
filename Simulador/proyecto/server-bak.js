// server.js
const express = require('express');
const mongoose = require('mongoose');
const cors = require('cors');
const path = require('path');

const app = express();
const PORT = process.env.PORT || 3000;

app.use(cors());
app.use(express.json());
app.use(express.static('public')); // Archivos estáticos

// URI Atlas
const MONGODB_URI = process.env.MONGODB_URI || 
    'mongodb+srv://0323105932:0323105932@rootdb.mnmbdoa.mongodb.net/RootDB';

// Conexión a MongoDB
let mongoConnected = false;
mongoose.connect(MONGODB_URI, { useNewUrlParser: true, useUnifiedTopology: true })
    .then(() => {
        mongoConnected = true;
        console.log('✅ Conectado a MongoDB Atlas');
    })
    .catch(err => {
        mongoConnected = false;
        console.error('⚠️ No se pudo conectar a MongoDB Atlas, usando simulador', err.message);
    });

// Schemas
const lecturaSchema = new mongoose.Schema({
    sensorId: { type: Number, required: true },
    tipo: { type: String, enum: ['temperatura','humedad'], required: true },
    valor: { type: Number, required: true },
    unidad: { type: String, required: true },
    fechaHora: { type: Date, default: Date.now }
});

const Lectura = mongoose.model('Lectura', lecturaSchema, 'lecturas');

// Simulador
function generarLectura(sensorId) {
    return {
        sensorId,
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

// También puedes agregar un GET más completo para health check
app.get('/api/lecturas/health', (req, res) => {
    res.json({
        status: 'ok',
        endpoint: '/api/lecturas',
        mongo: mongoConnected ? 'connected' : 'offline',
        timestamp: new Date()
    });
});

// Lecturas últimas 4 sensores
app.get('/api/simulador/sensores', async (req,res) => {
    const sensores = [1,2,3,4];
    const lecturas = [];

    for (let id of sensores) {
        if(mongoConnected){
            const doc = await Lectura.find({ sensorId: id }).sort({ fechaHora: -1 }).limit(1);
            lecturas.push(doc.length > 0 ? doc[0] : generarLectura(id));
        } else {
            lecturas.push(generarLectura(id));
        }
    }

    res.json({ data: lecturas, source: mongoConnected ? 'Atlas' : 'Simulador' });
});


// POST lecturas
app.post('/api/lecturas', async (req,res) => {
    console.log('📥 POST /api/lecturas - Datos recibidos:', req.body);
    
    // Validar datos requeridos
    const { sensorId, tipo, valor, unidad } = req.body;
    
    if (!sensorId || !tipo || valor === undefined || !unidad) {
        return res.status(400).json({
            error: 'Datos incompletos',
            required: ['sensorId', 'tipo', 'valor', 'unidad'],
            received: req.body
        });
    }

    if(!mongoConnected){
        console.log('⚠️ MongoDB desconectado - simulando guardado');
        return res.status(503).json({
            error:'No conectado a MongoDB, datos no guardados',
            simulated: true,
            data: req.body
        });
    }

    try{
        const lectura = new Lectura(req.body);
        const saved = await lectura.save();
        console.log('✅ Lectura guardada exitosamente');
        res.status(201).json({ data: saved });
    } catch(err){
        console.error('❌ Error interno del servidor:', err);
        res.status(500).json({ 
            error: err.message,
            code: err.code,
            type: 'database_error'
        });
    }
});

// Servir Index.html
app.get('/', (req,res) => res.sendFile(path.join(__dirname,'public','Index.html')));

app.listen(PORT, () => {
    console.log(`🚀 Server ejecutándose en http://localhost:${PORT}`);
});
