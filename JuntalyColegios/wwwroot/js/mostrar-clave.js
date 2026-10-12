// Botón "ojo" para ver u ocultar una contraseña. Uso: dentro del mismo contenedor que el input,
// <button type="button" data-mostrar-clave="IdDelInput" aria-label="Mostrar contraseña">…</button>.
(function () {
    document.querySelectorAll('[data-mostrar-clave]').forEach(function (boton) {
        const campo = document.getElementById(boton.dataset.mostrarClave);
        if (!campo) return;

        boton.addEventListener('click', function () {
            const mostrar = campo.type === 'password';
            campo.type = mostrar ? 'text' : 'password';
            boton.setAttribute('aria-label', mostrar ? 'Ocultar contraseña' : 'Mostrar contraseña');
            boton.setAttribute('aria-pressed', String(mostrar));
            boton.classList.toggle('jc-clave-visible', mostrar);
            campo.focus();
        });
    });
})();
