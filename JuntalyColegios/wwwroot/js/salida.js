// Pantalla de salida: refresca la lista de alumnos por entregar cada pocos segundos, para que una
// lectura de la antena aparezca sin recargar la página.
(function () {
    const contenedor = document.getElementById('pendientes');
    if (!contenedor) return;

    const url = contenedor.dataset.url;
    const intervaloMs = 3000;

    async function refrescar() {
        try {
            const respuesta = await fetch(url, { headers: { 'X-Requested-With': 'fetch' }, cache: 'no-store' });
            // Si la sesión caducó, la respuesta es una redirección al login: se deja la vista como está.
            if (respuesta.ok && !respuesta.redirected) {
                contenedor.innerHTML = await respuesta.text();
            }
        } catch {
            // Sin conexión momentánea: se reintenta en el siguiente ciclo.
        } finally {
            setTimeout(refrescar, intervaloMs);
        }
    }

    setTimeout(refrescar, intervaloMs);
})();
