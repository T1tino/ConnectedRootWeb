// Variables para controlar las hojas
let hojasActivas = true;
let timeoutId = null;

function crearHoja() {
  if (!hojasActivas) return;
  
  const hoja = document.createElement('img');
  // Ruta corregida para la imagen de la hoja
  hoja.src = '../PNG/Hoja.png';
  hoja.className = 'hoja';

  const leftOffset = Math.random() * (window.innerWidth - 100);
  hoja.style.left = `${leftOffset}px`;
  hoja.style.top = `-60px`;

  const size = 50 + Math.random() * 40;
  hoja.style.width = `${size}px`;

  const duration = 3 + Math.random() * 3;
  hoja.style.animationDuration = `${duration}s`;

  document.body.appendChild(hoja);

  setTimeout(() => {
    if (hoja.parentNode) {
      hoja.remove();
    }
  }, duration * 1000);
}

function iniciarHojas() {
  if (!hojasActivas) return;
  
  crearHoja();
  const intervalo = 500 + Math.random() * 3000; 
  timeoutId = setTimeout(iniciarHojas, intervalo);
}

// Función para alternar las hojas
function toggleHojas() {
  hojasActivas = !hojasActivas;
  const leafText = document.getElementById('leafText');
  const leafIcon = document.getElementById('leafIcon');
  
  if (hojasActivas) {
    leafText.textContent = 'Desactivar Hojas';
    leafIcon.className = 'bi bi-leaf';
    iniciarHojas();
  } else {
    leafText.textContent = 'Activar Hojas';
    leafIcon.className = 'bi bi-leaf text-muted';
    if (timeoutId) {
      clearTimeout(timeoutId);
    }
    // Remover hojas existentes
    const hojasExistentes = document.querySelectorAll('.hoja');
    hojasExistentes.forEach(hoja => hoja.remove());
  }
}

// Iniciar hojas automáticamente
iniciarHojas();