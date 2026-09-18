/* ============================================================
   Monte Carlo — Comportamiento compartido de la interfaz
   ------------------------------------------------------------
   Cada bloque se activa por atributo de datos (data-mc-*), no
   por id. Asi un mismo comportamiento sirve en cualquier vista
   sin escribir JavaScript nuevo.

   Convenciones:
   - Nada aqui valida reglas de negocio: eso vive en el servidor.
   - El medidor de fortaleza es una ayuda visual; la politica de
     contrasena real la aplica la API.
   ============================================================ */
(function () {
    "use strict";

    /* --------------------------------------------------------
       Mostrar / ocultar contrasena
       Uso: <button data-mc-toggle-password="#IdDelCampo">
       -------------------------------------------------------- */
    function initPasswordToggles() {
        document.querySelectorAll("[data-mc-toggle-password]").forEach(function (button) {
            button.addEventListener("click", function () {
                var input = document.querySelector(button.dataset.mcTogglePassword);
                if (!input) {
                    return;
                }

                var revealed = input.type === "text";
                input.type = revealed ? "password" : "text";
                button.setAttribute("aria-pressed", String(!revealed));
                button.setAttribute("aria-label", revealed ? "Mostrar contrasena" : "Ocultar contrasena");

                var icon = button.querySelector("i");
                if (icon) {
                    icon.className = revealed ? "bi bi-eye" : "bi bi-eye-slash";
                }
            });
        });
    }

    /* --------------------------------------------------------
       Medidor de fortaleza de contrasena
       Uso: <input data-mc-strength="#IdDelContenedorMedidor">
       -------------------------------------------------------- */
    var STRENGTH_LEVELS = [
        { label: "Muy debil", className: "is-weak", segments: 1 },
        { label: "Debil", className: "is-weak", segments: 2 },
        { label: "Aceptable", className: "is-fair", segments: 3 },
        { label: "Fuerte", className: "is-strong", segments: 4 }
    ];

    function scorePassword(value) {
        if (!value) {
            return -1;
        }

        var score = 0;
        if (value.length >= 8) score++;
        if (value.length >= 12) score++;
        if (/[a-z]/.test(value) && /[A-Z]/.test(value)) score++;
        if (/\d/.test(value)) score++;
        if (/[^A-Za-z0-9]/.test(value)) score++;

        return Math.min(score, STRENGTH_LEVELS.length) - 1;
    }

    function initStrengthMeters() {
        document.querySelectorAll("[data-mc-strength]").forEach(function (input) {
            var meter = document.querySelector(input.dataset.mcStrength);
            if (!meter) {
                return;
            }

            var segments = meter.querySelectorAll(".mc-strength__segment");
            var label = meter.querySelector(".mc-strength__label");

            input.addEventListener("input", function () {
                var index = scorePassword(input.value);
                var level = index >= 0 ? STRENGTH_LEVELS[index] : null;

                segments.forEach(function (segment, position) {
                    segment.className = "mc-strength__segment";
                    if (level && position < level.segments) {
                        segment.classList.add(level.className);
                    }
                });

                // El texto acompana siempre al color: el color por si
                // solo no comunica el estado (WCAG 1.4.1).
                label.textContent = level ? "Seguridad: " + level.label : "";
            });
        });
    }

    /* --------------------------------------------------------
       Sidebar administrativo en pantallas chicas
       -------------------------------------------------------- */
    function initSidebar() {
        var sidebar = document.querySelector("[data-mc-sidebar]");
        var toggle = document.querySelector("[data-mc-sidebar-toggle]");
        var backdrop = document.querySelector("[data-mc-sidebar-backdrop]");

        if (!sidebar || !toggle) {
            return;
        }

        function setOpen(open) {
            sidebar.classList.toggle("is-open", open);
            toggle.setAttribute("aria-expanded", String(open));
            if (backdrop) {
                backdrop.classList.toggle("is-visible", open);
            }
        }

        toggle.addEventListener("click", function () {
            setOpen(!sidebar.classList.contains("is-open"));
        });

        if (backdrop) {
            backdrop.addEventListener("click", function () {
                setOpen(false);
            });
        }

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                setOpen(false);
            }
        });
    }

    /* --------------------------------------------------------
       Modal de confirmacion generico
       ------------------------------------------------------------
       El disparador declara que decir y a donde enviar el POST:
         <button data-mc-confirm
                 data-mc-confirm-title="Desactivar cuenta"
                 data-mc-confirm-body="..."
                 data-mc-confirm-action="/admin/usuarios/5/desactivar"
                 data-mc-confirm-label="Desactivar"
                 data-mc-confirm-variant="danger">

       La accion siempre viaja por POST con antiforgery: el modal
       reutiliza un unico formulario ya provisto del token.
       -------------------------------------------------------- */
    function initConfirmDialog() {
        var modalElement = document.getElementById("mcConfirmModal");
        if (!modalElement || typeof bootstrap === "undefined") {
            return;
        }

        var modal = new bootstrap.Modal(modalElement);
        var form = modalElement.querySelector("[data-mc-confirm-form]");
        var titleEl = modalElement.querySelector("[data-mc-confirm-title-target]");
        var bodyEl = modalElement.querySelector("[data-mc-confirm-body-target]");
        var submitEl = modalElement.querySelector("[data-mc-confirm-submit]");

        document.querySelectorAll("[data-mc-confirm]").forEach(function (trigger) {
            trigger.addEventListener("click", function () {
                var data = trigger.dataset;

                titleEl.textContent = data.mcConfirmTitle || "Confirmar accion";
                bodyEl.textContent = data.mcConfirmBody || "Esta accion requiere tu confirmacion.";
                submitEl.textContent = data.mcConfirmLabel || "Confirmar";
                submitEl.className = "btn btn-" + (data.mcConfirmVariant || "primary");
                form.setAttribute("action", data.mcConfirmAction);

                modal.show();
            });
        });
    }

    /* --------------------------------------------------------
       Cuenta regresiva de bloqueo (HU-AUT-001, escenario 3)
       Uso: <span data-mc-countdown="300"> sobre el elemento que
       muestra el tiempo restante; al llegar a cero rehabilita el
       formulario indicado en data-mc-countdown-form.
       -------------------------------------------------------- */
    function initCountdowns() {
        document.querySelectorAll("[data-mc-countdown]").forEach(function (element) {
            var remaining = parseInt(element.dataset.mcCountdown, 10);
            if (isNaN(remaining) || remaining <= 0) {
                return;
            }

            var container = element.closest("[data-mc-countdown-scope]") || document;

            function render() {
                var minutes = Math.floor(remaining / 60);
                var seconds = remaining % 60;
                element.textContent = minutes + ":" + String(seconds).padStart(2, "0");
            }

            render();

            var timer = setInterval(function () {
                remaining--;

                if (remaining <= 0) {
                    clearInterval(timer);
                    // Recargamos para que sea el servidor quien decida
                    // si el bloqueo expiro: el reloj del navegador no
                    // es una fuente de verdad confiable.
                    window.location.reload();
                    return;
                }

                render();
            }, 1000);

            void container;
        });
    }

    /* --------------------------------------------------------
       Evita el doble envio de formularios
       -------------------------------------------------------- */
    function initSubmitGuard() {
        document.querySelectorAll("form[data-mc-guard-submit]").forEach(function (form) {
            form.addEventListener("submit", function () {
                // jQuery Validation cancela el submit si hay errores;
                // solo bloqueamos cuando el formulario es valido.
                if (form.checkValidity && !form.checkValidity()) {
                    return;
                }

                var button = form.querySelector('[type="submit"]');
                if (!button) {
                    return;
                }

                button.disabled = true;
                button.insertAdjacentHTML(
                    "afterbegin",
                    '<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>'
                );
            });
        });
    }

    /* --------------------------------------------------------
       Descarta las alertas de exito pasados unos segundos.
       Solo las de exito: los errores permanecen hasta que el
       usuario los cierre.
       -------------------------------------------------------- */
    function initAutoDismiss() {
        document.querySelectorAll("[data-mc-autodismiss]").forEach(function (alert) {
            setTimeout(function () {
                if (typeof bootstrap !== "undefined") {
                    bootstrap.Alert.getOrCreateInstance(alert).close();
                }
            }, parseInt(alert.dataset.mcAutodismiss, 10) || 6000);
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        initPasswordToggles();
        initStrengthMeters();
        initSidebar();
        initConfirmDialog();
        initCountdowns();
        initSubmitGuard();
        initAutoDismiss();
    });
})();
