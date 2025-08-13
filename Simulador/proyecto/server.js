// server.js
const express = require('express');
const mongoose = require('mongoose');
const cors = require('cors');
const path = require('path');

const app = express();
const PORT = process.env.PORT || 3000;

// Middleware
app.use(cors());
app.use(express.json());
app.use(express.static('public')); // Sirve archivos estáticos

// Conexión a MongoDB
const MONGODB_URI = process.env.MONGODB_URI || 'mongodb://localhost:27017/connected_root';

mongoose.connect(MONGODB_URI, {
    useNewUrlParser: true,
    useUnifiedTopology: true,
})
.then(() => {
    console.log('✅ Conectado a MongoDB exitosamente');
})
.catch((error) => {
    console.error('❌ Error al conectar con MongoDB:', error);
});

// Esquema de Lecturas basado en tu documentación
const lecturaSchema = new mongoose.Schema({
    sensorId: {
        type: mongoose.Schema.Types.ObjectId,
        required: true,
        index: true
    },
    fechaHora: {
        type: Date,
        required: true,
        default: Date.now,
        index: true
    },
    tipo: {
        type: String,
        required: true,
        enum: ['temperatura', 'humedad'],
        index: true
    },
    valor: {
        type: Number,
        required: true,
        min: 0,
        max: 100
    },
    unidad: {
        type: String,
        required: true,
        enum: ['°C', '%']
    }
}, {
    timestamps: true // Agrega createdAt y updatedAt automáticamente
});

// Índices compuestos para optimizar consultas
lecturaSchema.index({ sensorId: 1, fechaHora: -1 });
lecturaSchema.index({ tipo: 1, fechaHora: -1 });

const Lectura = mongoose.model('Lectura', lecturaSchema, 'lecturas');

// Esquema de Sensores para referencia
const sensorSchema = new mongoose.Schema({
    zonaId: {
        type: mongoose.Schema.Types.ObjectId,
        required: true
    },
    tipo: {
        type: String,
        required: true,
        enum: ['temperatura', 'humedad']
    },
    modelo: String,
    estado: {
        type: String,
        enum: ['activo', 'inactivo'],
        default: 'activo'
    },
    fechaInstalacion: {
        type: Date,
        default: Date.now
    },
    descripcion: String
});

const Sensor = mongoose.model('Sensor', sensorSchema, 'sensores');

// RUTAS DE LA API

// POST - Crear nueva lectura
app.post('/api/lecturas', async (req, res) => {
    try {
        const { sensorId, fechaHora, tipo, valor, unidad } = req.body;

        // Validaciones adicionales
        if (!sensorId || !tipo || valor === undefined || !unidad) {
            return res.status(400).json({
                error: 'Faltan campos requeridos: sensorId, tipo, valor, unidad'
            });
        }

        // Validar que el valor esté en el rango correcto
        if (tipo === 'humedad' && (valor < 0 || valor > 100)) {
            return res.status(400).json({
                error: 'Valor de humedad debe estar entre 0 y 100'
            });
        }

        if (tipo === 'temperatura' && (valor < -50 || valor > 70)) {
            return res.status(400).json({
                error: 'Valor de temperatura debe estar entre -50 y 70°C'
            });
        }

        const nuevaLectura = new Lectura({
            sensorId: new mongoose.Types.ObjectId(sensorId),
            fechaHora: fechaHora || new Date(),
            tipo,
            valor,
            unidad
        });

        const lecturaGuardada = await nuevaLectura.save();
        
        console.log(`📊 Nueva lectura guardada: ${tipo} = ${valor}${unidad} del sensor ${sensorId}`);
        
        res.status(201).json({
            success: true,
            data: lecturaGuardada,
            message: 'Lectura guardada exitosamente'
        });

    } catch (error) {
        console.error('❌ Error al guardar lectura:', error);
        res.status(500).json({
            error: 'Error interno del servidor',
            details: error.message
        });
    }
});

// GET - Obtener lecturas con filtros
app.get('/api/lecturas', async (req, res) => {
    try {
        const { 
            sensorId, 
            tipo, 
            fechaInicio, 
            fechaFin, 
            limit = 100, 
            page = 1 
        } = req.query;

        // Construir filtros
        const filtros = {};
        
        if (sensorId) {
            filtros.sensorId = new mongoose.Types.ObjectId(sensorId);
        }
        
        if (tipo) {
            filtros.tipo = tipo;
        }
        
        if (fechaInicio || fechaFin) {
            filtros.fechaHora = {};
            if (fechaInicio) {
                filtros.fechaHora.$gte = new Date(fechaInicio);
            }
            if (fechaFin) {
                filtros.fechaHora.$lte = new Date(fechaFin);
            }
        }

        const skip = (parseInt(page) - 1) * parseInt(limit);

        const lecturas = await Lectura.find(filtros)
            .sort({ fechaHora: -1 })
            .limit(parseInt(limit))
            .skip(skip);

        const total = await Lectura.countDocuments(filtros);

        res.json({
            success: true,
            data: lecturas,
            pagination: {
                currentPage: parseInt(page),
                totalPages: Math.ceil(total / parseInt(limit)),
                totalRecords: total,
                hasNext: skip + lecturas.length < total,
                hasPrev: parseInt(page) > 1
            }
        });

    } catch (error) {
        console.error('❌ Error al obtener lecturas:', error);
        res.status(500).json({
            error: 'Error interno del servidor',
            details: error.message
        });
    }
});

// GET - Obtener últimas lecturas por sensor
app.get('/api/lecturas/ultimas/:sensorId', async (req, res) => {
    try {
        const { sensorId } = req.params;
        const { limit = 10 } = req.query;

        const lecturas = await Lectura.find({ 
            sensorId: new mongoose.Types.ObjectId(sensorId) 
        })
            .sort({ fechaHora: -1 })
            .limit(parseInt(limit));

        res.json({
            success: true,
            data: lecturas
        });

    } catch (error) {
        console.error('❌ Error al obtener últimas lecturas:', error);
        res.status(500).json({
            error: 'Error interno del servidor',
            details: error.message
        });
    }
});

// GET - Estadísticas de sensores
app.get('/api/estadisticas/sensores', async (req, res) => {
    try {
        const estadisticas = await Lectura.aggregate([
            {
                $group: {
                    _id: {
                        sensorId: '$sensorId',
                        tipo: '$tipo'
                    },
                    ultimaLectura: { $max: '$fechaHora' },
                    valorPromedio: { $avg: '$valor' },
                    valorMinimo: { $min: '$valor' },
                    valorMaximo: { $max: '$valor' },
                    totalLecturas: { $sum: 1 }
                }
            },
            {
                $sort: { ultimaLectura: -1 }
            }
        ]);

        res.json({
            success: true,
            data: estadisticas
        });

    } catch (error) {
        console.error('❌ Error al obtener estadísticas:', error);
        res.status(500).json({
            error: 'Error interno del servidor',
            details: error.message
        });
    }
});

// POST - Crear múltiples lecturas (para datos offline)
app.post('/api/lecturas/batch', async (req, res) => {
    try {
        const { lecturas } = req.body;

        if (!Array.isArray(lecturas) || lecturas.length === 0) {
            return res.status(400).json({
                error: 'Se requiere un array de lecturas'
            });
        }

        const lecturasFormateadas = lecturas.map(lectura => ({
            ...lectura,
            sensorId: new mongoose.Types.ObjectId(lectura.sensorId),
            fechaHora: lectura.fechaHora || new Date()
        }));

        const resultado = await Lectura.insertMany(lecturasFormateadas);

        console.log(`📊 ${resultado.length} lecturas guardadas en lote`);

        res.status(201).json({
            success: true,
            data: resultado,
            message: `${resultado.length} lecturas guardadas exitosamente`
        });

    } catch (error) {
        console.error('❌ Error al guardar lecturas en lote:', error);
        res.status(500).json({
            error: 'Error interno del servidor',
            details: error.message
        });
    }
});

// Ruta para servir el archivo HTML principal
app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, 'public', 'Index.html'));
});

// Middleware de manejo de errores
app.use((error, req, res, next) => {
    console.error('❌ Error no manejado:', error);
    res.status(500).json({
        error: 'Error interno del servidor',
        message: error.message
    });
});

// Iniciar servidor
app.listen(PORT, () => {
    console.log(`🚀 Servidor Connected Root ejecutándose en http://localhost:${PORT}`);
    console.log(`📡 API MongoDB disponible en http://localhost:${PORT}/api/lecturas`);
});

// Manejo de cierre graceful
process.on('SIGINT', async () => {
    console.log('\n🔄 Cerrando servidor...');
    await mongoose.connection.close();
    console.log('✅ Conexión a MongoDB cerrada');
    process.exit(0);
});