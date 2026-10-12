// Pantalla de llamadas (turnero): refresca los llamados cada pocos segundos, resalta el llamado nuevo y
// suena un aviso. Los navegadores solo permiten sonido tras una interacción, así que el aviso se activa
// al pulsar «Pantalla completa».
(function () {
    const contenido = document.getElementById('contenido');
    const reloj = document.getElementById('reloj');
    const boton = document.getElementById('pantalla-completa');
    if (!contenido) return;

    const intervaloMs = 3000;
    let ultimo = turnos()?.dataset.ultimo ?? '';
    let audio = null;

    function turnos() {
        return document.getElementById('turnos');
    }

    function avisoSonoro() {
        if (!audio) return;
        // Dos tonos cortos, como un timbre de turnero.
        [0, 0.25].forEach(function (retraso, i) {
            const oscilador = audio.createOscillator();
            const volumen = audio.createGain();
            oscilador.frequency.value = i === 0 ? 880 : 660;
            volumen.gain.setValueAtTime(0.0001, audio.currentTime + retraso);
            volumen.gain.exponentialRampToValueAtTime(0.4, audio.currentTime + retraso + 0.02);
            volumen.gain.exponentialRampToValueAtTime(0.0001, audio.currentTime + retraso + 0.22);
            oscilador.connect(volumen).connect(audio.destination);
            oscilador.start(audio.currentTime + retraso);
            oscilador.stop(audio.currentTime + retraso + 0.25);
        });
    }

    async function refrescar() {
        try {
            const respuesta = await fetch(contenido.dataset.url, { cache: 'no-store' });
            if (respuesta.ok) {
                contenido.innerHTML = await respuesta.text();
                const actual = turnos();
                if (reloj && actual?.dataset.hora) reloj.textContent = actual.dataset.hora;
                const nuevo = actual?.dataset.ultimo ?? '';
                if (nuevo && nuevo !== ultimo) {
                    actual.querySelector('.jc-turno-actual')?.classList.add('jc-turno-nuevo');
                    avisoSonoro();
                }
                ultimo = nuevo;
            }
        } catch {
            // Sin conexión momentánea: se reintenta en el siguiente ciclo.
        } finally {
            setTimeout(refrescar, intervaloMs);
        }
    }

    if (boton) {
        const puedePantallaCompleta = !!document.documentElement.requestFullscreen;
        if (!puedePantallaCompleta) boton.textContent = 'Activar sonido';
        boton.addEventListener('click', function () {
            try {
                audio = audio || new (window.AudioContext || window.webkitAudioContext)();
                audio.resume();
            } catch {
                audio = null;
            }
            if (puedePantallaCompleta && !document.fullscreenElement) {
                document.documentElement.requestFullscreen().catch(function () { });
            }
        });
        document.addEventListener('fullscreenchange', function () {
            boton.hidden = !!document.fullscreenElement;
        });
    }

    setTimeout(refrescar, intervaloMs);
})();
