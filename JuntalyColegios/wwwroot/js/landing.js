(function () {
    'use strict';

    // ── Datos de la demo (mismo contenido que el diseño original) ──────────────
    var POOL = [
        { name: 'Andrés Molina', grade: '3er grado · A' },
        { name: 'Camila Rojas', grade: '6to grado · B' },
        { name: 'Lucía Pérez', grade: '6to grado · A' },
        { name: 'Samuel Torres', grade: '2do año · A' },
        { name: 'Isabella Díaz', grade: '1er grado · C' },
        { name: 'Gabriel Suárez', grade: '4to grado · A' }
    ];
    var NEXT = { 'LLAMANDO': 'EN CAMINO', 'EN CAMINO': 'ENTREGADO', 'ENTREGADO': 'ENTREGADO' };
    var LOOK = {
        'LLAMANDO': { bg: '#F9E1A6', chipFg: '#F9E1A6', opacity: 1 },
        'EN CAMINO': { bg: '#98D3C1', chipFg: '#98D3C1', opacity: 1 },
        'ENTREGADO': { bg: '#FFFFFF', chipFg: '#FFFFFF', opacity: 0.6 }
    };
    var FAQS = [
        { q: '¿Qué pasa si un padre llega sin tag?', a: 'El personal hace un retiro manual verificado desde la plataforma y queda registrado igual que los demás.' },
        { q: '¿Pueden retirar varias personas al mismo alumno?', a: 'Sí. Cada familia puede tener varios autorizados, cada uno con su propio tag vehicular.' },
        { q: '¿Qué necesita instalar el colegio?', a: 'Una antena RFID en el acceso, pantallas en salones o pasillos y tablets o móviles para los docentes. La plataforma Juntaly funciona en web, tablet y móvil.' },
        { q: '¿Cuánto tarda la implementación?', a: 'Se hace por etapas: visita técnica, instalación de antena y pantallas, carga de alumnos y entrega de tags, y un piloto antes de la puesta en marcha.' },
        { q: '¿Cuánto cuesta?', a: '$1 por alumno al mes. Incluye pantalla de llamado, acceso web, tablet y móvil, historial de entregas y soporte técnico.' }
    ];

    var esc = function (s) {
        return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    };

    var state = {
        tab: 'pantalla',
        n: 0,
        detected: 4,
        tickets: [
            { code: 'A-015', name: 'Mateo Pérez', grade: '2do grado · A', status: 'LLAMANDO' },
            { code: 'A-014', name: 'Sofía Ramírez', grade: '4to grado · B', status: 'LLAMANDO' },
            { code: 'A-012', name: 'Diego Castillo', grade: '1er año · A', status: 'EN CAMINO' },
            { code: 'A-011', name: 'Valentina Gómez', grade: '5to grado · A', status: 'ENTREGADO' }
        ],
        faqOpen: -1,
        students: 400
    };

    // ── Demo: pestañas ──────────────────────────────────────────────────────
    function renderTabs() {
        document.querySelectorAll('.jc-tab').forEach(function (btn) {
            btn.classList.toggle('jc-tab-active', btn.dataset.tab === state.tab);
        });
        ['pantalla', 'tablet', 'app'].forEach(function (id) {
            var panel = document.getElementById('jcPanel-' + id);
            if (panel) panel.style.display = state.tab === id ? '' : 'none';
        });
    }

    function renderPantalla() {
        var el = document.getElementById('jcTicketsPantalla');
        if (!el) return;
        el.innerHTML = state.tickets.map(function (t) {
            var look = LOOK[t.status];
            return '<div style="background:' + look.bg + ';opacity:' + look.opacity + ';border:3px solid #fff;border-radius:10px;padding:14px;display:flex;flex-direction:column;gap:8px;transition:background .3s,opacity .3s;">'
                + '<div class="jc-mono" style="font-weight:800;font-size:26px;line-height:1;">' + esc(t.code) + '</div>'
                + '<div style="font-weight:900;font-size:16px;">' + esc(t.name) + '</div>'
                + '<div style="font-size:13px;">' + esc(t.grade) + '</div>'
                + '<div class="jc-mono" style="align-self:flex-start;background:#163D5A;color:' + look.chipFg + ';font-weight:800;font-size:11px;padding:3px 8px;">' + t.status + '</div>'
                + '</div>';
        }).join('');
    }

    function renderTablet() {
        var el = document.getElementById('jcTicketsTablet');
        var count = document.getElementById('jcCallingCount');
        if (count) count.textContent = state.tickets.filter(function (t) { return t.status === 'LLAMANDO'; }).length;
        if (!el) return;
        el.innerHTML = state.tickets.map(function (t) {
            var look = LOOK[t.status];
            var btn = t.status === 'LLAMANDO'
                ? '<button type="button" class="jc-btn-enviar" data-send="' + esc(t.code) + '">Enviar</button>'
                : '';
            return '<div style="display:flex;justify-content:space-between;align-items:center;gap:12px;border:3px solid #163D5A;border-radius:12px;padding:12px 14px;background:' + look.bg + ';">'
                + '<div><div style="font-weight:900;font-size:17px;">' + esc(t.name) + '</div><div class="jc-mono" style="font-size:12px;">' + esc(t.code) + ' · ' + t.status + '</div></div>'
                + btn + '</div>';
        }).join('');
    }

    function renderApp() {
        var t = state.tickets[0];
        var code = document.getElementById('jcAppCode');
        var name = document.getElementById('jcAppName');
        var grade = document.getElementById('jcAppGrade');
        if (code) code.textContent = t.code;
        if (name) name.textContent = t.name;
        if (grade) grade.textContent = t.grade;

        var idx = { 'LLAMANDO': 1, 'EN CAMINO': 2, 'ENTREGADO': 3 }[t.status];
        var steps = document.getElementById('jcAppSteps');
        if (!steps) return;
        var labels = ['Carro detectado', 'Alumno en camino', 'Entregado'];
        steps.innerHTML = labels.map(function (label, i) {
            var bg = i < idx ? (i === idx - 1 ? '#F9E1A6' : '#98D3C1') : '#fff';
            var opacity = i < idx ? 1 : 0.45;
            return '<div style="display:flex;align-items:center;gap:12px;">'
                + '<div style="width:28px;height:28px;flex:none;border:3px solid #163D5A;border-radius:50%;background:' + bg + ';"></div>'
                + '<div style="font-weight:800;font-size:16px;opacity:' + opacity + ';">' + label + '</div>'
                + '</div>';
        }).join('');
    }

    function renderDemo() {
        renderTabs();
        renderPantalla();
        renderTablet();
        renderApp();
        var detected = document.getElementById('jcDetected');
        if (detected) detected.textContent = state.detected;
    }

    function simulate() {
        var p = POOL[state.n % POOL.length];
        var code = 'A-0' + (16 + state.n);
        var envejecidos = state.tickets.map(function (t) { return Object.assign({}, t, { status: NEXT[t.status] }); });
        state.n += 1;
        state.detected += 1;
        state.tickets = [{ code: code, name: p.name, grade: p.grade, status: 'LLAMANDO' }].concat(envejecidos).slice(0, 4);
        renderDemo();
    }

    function send(code) {
        state.tickets = state.tickets.map(function (t) {
            return t.code === code ? Object.assign({}, t, { status: 'EN CAMINO' }) : t;
        });
        renderDemo();
    }

    // ── Precio ──────────────────────────────────────────────────────────────
    function renderPrecio() {
        var val = document.getElementById('jcStudentsVal');
        var total = document.getElementById('jcTotal');
        if (val) val.textContent = state.students;
        if (total) total.textContent = '$' + state.students.toLocaleString('es-VE');
    }

    // ── FAQ ─────────────────────────────────────────────────────────────────
    function renderFaq() {
        var el = document.getElementById('jcFaqList');
        if (!el) return;
        el.innerHTML = FAQS.map(function (f, i) {
            var open = state.faqOpen === i;
            var bg = open ? '#F9E1A6' : '#fff';
            var shadow = open ? '6px 6px 0 #163D5A' : '3px 3px 0 #163D5A';
            var body = open
                ? '<div style="padding:0 22px 22px;font-size:16px;line-height:1.55;">' + esc(f.a) + '</div>'
                : '';
            return '<div style="background:' + bg + ';border:3px solid #163D5A;border-radius:16px;box-shadow:' + shadow + ';transition:box-shadow .2s,background .2s;">'
                + '<button type="button" data-faq="' + i + '" style="width:100%;display:flex;justify-content:space-between;align-items:center;gap:16px;font:inherit;font-weight:900;font-size:18px;color:#163D5A;background:transparent;border:none;padding:20px 22px;cursor:pointer;text-align:left;">'
                + '<span>' + esc(f.q) + '</span>'
                + '<span style="flex:none;width:34px;height:34px;border:3px solid #163D5A;border-radius:50%;background:#fff;display:flex;align-items:center;justify-content:center;font-size:20px;line-height:1;">' + (open ? '–' : '+') + '</span>'
                + '</button>' + body + '</div>';
        }).join('');
    }

    // ── Formulario de contacto ──────────────────────────────────────────────
    function initForm() {
        var form = document.getElementById('jcForm');
        if (!form) return;
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            var boton = form.querySelector('button[type="submit"]');
            if (boton) boton.disabled = true;
            fetch(form.action, { method: 'POST', body: new FormData(form), credentials: 'same-origin' })
                .then(function (r) { return r.json(); })
                .then(function () {
                    form.style.display = 'none';
                    document.getElementById('jcFormSent').style.display = 'flex';
                })
                .catch(function () {
                    if (boton) boton.disabled = false;
                    alert('No se pudo enviar la solicitud. Intenta de nuevo en un momento.');
                });
        });
    }

    // ── Menú hamburguesa (tablet/móvil) ──────────────────────────────────────
    function initNavToggle() {
        var toggle = document.getElementById('jcNavToggle');
        var nav = document.getElementById('jcNav');
        if (!toggle || !nav) return;

        function setOpen(open) {
            nav.classList.toggle('jc-nav-open', open);
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
            toggle.setAttribute('aria-label', open ? 'Cerrar menú' : 'Abrir menú');
        }

        toggle.addEventListener('click', function () {
            setOpen(!nav.classList.contains('jc-nav-open'));
        });
        // Cerrar al elegir una opción (ancla o "Ingresar"/"Solicitar demo").
        nav.addEventListener('click', function (e) {
            if (e.target.closest('a')) setOpen(false);
        });
        // Si la ventana crece hasta el layout de escritorio, no dejar el panel abierto oculto.
        window.addEventListener('resize', function () {
            if (window.innerWidth > 860) setOpen(false);
        });
    }

    // ── Reveal on scroll ─────────────────────────────────────────────────────
    function initReveal() {
        if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
        var els = [];
        document.querySelectorAll('[data-reveal]').forEach(function (el) { els.push([el, 0]); });
        document.querySelectorAll('[data-stagger]').forEach(function (g) {
            Array.prototype.forEach.call(g.children, function (c, i) { els.push([c, i * 100]); });
        });
        els.forEach(function (pair) {
            var el = pair[0], d = pair[1];
            el.classList.add('jc-reveal');
            el.style.transitionDelay = d + 'ms';
        });
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('jc-revealed');
                    io.unobserve(entry.target);
                }
            });
        }, { threshold: 0.12 });
        els.forEach(function (pair) { io.observe(pair[0]); });
    }

    document.addEventListener('DOMContentLoaded', function () {
        renderDemo();
        renderPrecio();
        renderFaq();
        initForm();
        initReveal();
        initNavToggle();

        document.querySelectorAll('.jc-tab').forEach(function (btn) {
            btn.addEventListener('click', function () { state.tab = btn.dataset.tab; renderDemo(); });
        });
        var simBtn = document.getElementById('jcSimulate');
        if (simBtn) simBtn.addEventListener('click', simulate);

        var tabletList = document.getElementById('jcTicketsTablet');
        if (tabletList) tabletList.addEventListener('click', function (e) {
            var btn = e.target.closest('[data-send]');
            if (btn) send(btn.dataset.send);
        });

        var slider = document.getElementById('jcStudents');
        if (slider) slider.addEventListener('input', function () {
            state.students = +slider.value;
            renderPrecio();
        });

        var faqList = document.getElementById('jcFaqList');
        if (faqList) faqList.addEventListener('click', function (e) {
            var btn = e.target.closest('[data-faq]');
            if (!btn) return;
            var i = +btn.dataset.faq;
            state.faqOpen = state.faqOpen === i ? -1 : i;
            renderFaq();
        });
    });
})();
