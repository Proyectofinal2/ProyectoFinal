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

    /* --------------------------------------------------------
       Calendario de nueva reserva (HU-RES-002, andamiaje visual)
       Solo conserva seleccion y navegacion local. La API sera la
       unica fuente de fechas y horarios realmente disponibles.
       -------------------------------------------------------- */
    function initReservationCalendars() {
        document.querySelectorAll("[data-mc-reservation-calendar]").forEach(function (calendar) {
            var peopleOutput = calendar.querySelector("[data-mc-reservation-people]");
            var decreaseButton = calendar.querySelector("[data-mc-reservation-decrease]");
            var increaseButton = calendar.querySelector("[data-mc-reservation-increase]");
            var previousButton = calendar.querySelector("[data-mc-reservation-previous-month]");
            var nextButton = calendar.querySelector("[data-mc-reservation-next-month]");
            var monthLabel = calendar.querySelector("[data-mc-reservation-month]");
            var daysContainer = calendar.querySelector("[data-mc-reservation-days]");
            var timesEmpty = calendar.querySelector("[data-mc-reservation-times-empty]");

            if (!peopleOutput || !decreaseButton || !increaseButton || !previousButton ||
                !nextButton || !monthLabel || !daysContainer) {
                return;
            }

            var people = parseInt(calendar.dataset.mcInitialPeople, 10);
            if (isNaN(people) || people < 1) {
                people = 1;
            }

            var displayedMonth = new Date();
            displayedMonth = new Date(displayedMonth.getFullYear(), displayedMonth.getMonth(), 1);
            var selectedDate = null;
            var monthFormatter = new Intl.DateTimeFormat("es-CR", {
                month: "long",
                year: "numeric"
            });

            function renderPeople() {
                peopleOutput.textContent = String(people);
                decreaseButton.disabled = people === 1;
            }

            function formatDateKey(date) {
                return date.getFullYear() + "-" +
                    String(date.getMonth() + 1).padStart(2, "0") + "-" +
                    String(date.getDate()).padStart(2, "0");
            }

            function selectDate(button) {
                selectedDate = button.dataset.mcReservationDate;
                daysContainer.querySelectorAll("[data-mc-reservation-date]").forEach(function (day) {
                    var selected = day === button;
                    day.classList.toggle("is-selected", selected);
                    day.setAttribute("aria-pressed", String(selected));
                });

                if (timesEmpty) {
                    timesEmpty.querySelector("p").textContent =
                        "Los horarios disponibles aparecerán aquí.";
                }
            }

            function renderCalendar() {
                monthLabel.textContent = monthFormatter.format(displayedMonth);
                daysContainer.replaceChildren();

                var firstWeekday = displayedMonth.getDay();
                var daysInMonth = new Date(
                    displayedMonth.getFullYear(),
                    displayedMonth.getMonth() + 1,
                    0
                ).getDate();

                for (var blank = 0; blank < firstWeekday; blank++) {
                    var spacer = document.createElement("span");
                    spacer.setAttribute("aria-hidden", "true");
                    daysContainer.appendChild(spacer);
                }

                for (var dayNumber = 1; dayNumber <= daysInMonth; dayNumber++) {
                    var date = new Date(displayedMonth.getFullYear(), displayedMonth.getMonth(), dayNumber);
                    var button = document.createElement("button");
                    var dateKey = formatDateKey(date);

                    button.type = "button";
                    button.className = "mc-reservation__day";
                    button.dataset.mcReservationDate = dateKey;
                    button.textContent = String(dayNumber);
                    button.setAttribute("aria-label", date.toLocaleDateString("es-CR", {
                        weekday: "long",
                        day: "numeric",
                        month: "long",
                        year: "numeric"
                    }));
                    button.setAttribute("aria-pressed", String(dateKey === selectedDate));

                    if (dateKey === selectedDate) {
                        button.classList.add("is-selected");
                    }

                    (function (dayButton) {
                        dayButton.addEventListener("click", function () {
                            selectDate(dayButton);
                        });
                    })(button);

                    daysContainer.appendChild(button);
                }
            }

            decreaseButton.addEventListener("click", function () {
                if (people > 1) {
                    people--;
                    renderPeople();
                }
            });

            increaseButton.addEventListener("click", function () {
                people++;
                renderPeople();
            });

            previousButton.addEventListener("click", function () {
                displayedMonth = new Date(displayedMonth.getFullYear(), displayedMonth.getMonth() - 1, 1);
                renderCalendar();
            });

            nextButton.addEventListener("click", function () {
                displayedMonth = new Date(displayedMonth.getFullYear(), displayedMonth.getMonth() + 1, 1);
                renderCalendar();
            });

            renderPeople();
            renderCalendar();
        });
    }

    /* --------------------------------------------------------
       Selector de horarios de operacion
       Uso: contenedor [data-mc-time-picker] con valor oculto,
       boton disparador y lista. El valor enviado siempre es HH:mm.
       -------------------------------------------------------- */
    function initTimePickers() {
        function formatTime(value) {
            var parts = value.split(":");
            var hours = parseInt(parts[0], 10);
            var minutes = parseInt(parts[1], 10);

            if (isNaN(hours) || isNaN(minutes) || hours < 0 || hours > 23 || minutes < 0 || minutes > 59) {
                return "Selecciona una hora";
            }

            var period = hours < 12 ? "a. m." : "p. m.";
            var displayHour = hours % 12 || 12;
            return String(displayHour).padStart(2, "0") + ":" + String(minutes).padStart(2, "0") + " " + period;
        }

        function createStandardValues() {
            var values = [];
            for (var hour = 0; hour < 24; hour++) {
                [0, 30].forEach(function (minute) {
                    values.push(String(hour).padStart(2, "0") + ":" + String(minute).padStart(2, "0"));
                });
            }
            return values;
        }

        document.querySelectorAll("[data-mc-time-picker]").forEach(function (picker) {
            var valueInput = picker.querySelector("[data-mc-time-picker-value]");
            var trigger = picker.querySelector("[data-mc-time-picker-trigger]");
            var label = picker.querySelector("[data-mc-time-picker-label]");
            var menu = picker.querySelector("[data-mc-time-picker-menu]");

            if (!valueInput || !trigger || !label || !menu || trigger.disabled) {
                return;
            }

            var values = createStandardValues();
            // Los datos anteriores pueden contener una hora valida fuera de
            // los intervalos nuevos. Se muestran sin habilitar minutos libres.
            if (valueInput.value && values.indexOf(valueInput.value) === -1 && formatTime(valueInput.value) !== "Selecciona una hora") {
                values.push(valueInput.value);
                values.sort();
            }

            values.forEach(function (value) {
                var option = document.createElement("button");
                option.type = "button";
                option.className = "mc-time-picker__option";
                option.dataset.mcTimePickerOption = value;
                option.setAttribute("role", "option");
                option.setAttribute("aria-selected", "false");
                option.tabIndex = -1;
                option.textContent = formatTime(value);
                menu.appendChild(option);
            });

            function options() {
                return Array.prototype.slice.call(menu.querySelectorAll("[data-mc-time-picker-option]"));
            }

            function selectedOption() {
                return menu.querySelector("[data-mc-time-picker-option='" + valueInput.value + "']");
            }

            function updateSelection(value) {
                valueInput.value = value;
                label.textContent = formatTime(value);
                options().forEach(function (option) {
                    var selected = option.dataset.mcTimePickerOption === value;
                    option.classList.toggle("is-selected", selected);
                    option.setAttribute("aria-selected", String(selected));
                    option.tabIndex = selected ? 0 : -1;
                });
            }

            function close() {
                menu.hidden = true;
                trigger.setAttribute("aria-expanded", "false");
            }

            function open(focusOption) {
                menu.hidden = false;
                trigger.setAttribute("aria-expanded", "true");

                if (focusOption) {
                    var option = selectedOption() || options()[0];
                    if (option) {
                        option.focus();
                        option.scrollIntoView({ block: "nearest" });
                    }
                }
            }

            function choose(option) {
                updateSelection(option.dataset.mcTimePickerOption);
                close();
                trigger.focus();
            }

            updateSelection(valueInput.value);

            trigger.addEventListener("click", function () {
                if (menu.hidden) {
                    open(false);
                } else {
                    close();
                }
            });

            trigger.addEventListener("keydown", function (event) {
                if (event.key === "ArrowDown" || event.key === "ArrowUp") {
                    event.preventDefault();
                    open(true);
                } else if (event.key === "Escape") {
                    close();
                }
            });

            menu.addEventListener("click", function (event) {
                var option = event.target.closest("[data-mc-time-picker-option]");
                if (option) {
                    choose(option);
                }
            });

            menu.addEventListener("keydown", function (event) {
                var option = event.target.closest("[data-mc-time-picker-option]");
                if (!option) {
                    return;
                }

                var allOptions = options();
                var index = allOptions.indexOf(option);
                var nextIndex = index;

                if (event.key === "ArrowDown") nextIndex = Math.min(index + 1, allOptions.length - 1);
                else if (event.key === "ArrowUp") nextIndex = Math.max(index - 1, 0);
                else if (event.key === "Home") nextIndex = 0;
                else if (event.key === "End") nextIndex = allOptions.length - 1;
                else if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    choose(option);
                    return;
                } else if (event.key === "Escape") {
                    event.preventDefault();
                    close();
                    trigger.focus();
                    return;
                } else if (event.key === "Tab") {
                    close();
                    return;
                } else {
                    return;
                }

                event.preventDefault();
                allOptions[nextIndex].focus();
                allOptions[nextIndex].scrollIntoView({ block: "nearest" });
            });

            document.addEventListener("click", function (event) {
                if (!picker.contains(event.target)) {
                    close();
                }
            });
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
        initReservationCalendars();
        initTimePickers();
    });
})();
