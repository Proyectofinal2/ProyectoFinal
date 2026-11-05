/* ============================================================
   MonteCarlo — Comportamiento compartido de la interfaz
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
    document
      .querySelectorAll("[data-mc-toggle-password]")
      .forEach(function (button) {
        button.addEventListener("click", function () {
          var input = document.querySelector(button.dataset.mcTogglePassword);
          if (!input) {
            return;
          }

          var revealed = input.type === "text";
          input.type = revealed ? "password" : "text";
          button.setAttribute("aria-pressed", String(!revealed));
          button.setAttribute(
            "aria-label",
            revealed ? "Mostrar contrasena" : "Ocultar contrasena",
          );

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
    { label: "Fuerte", className: "is-strong", segments: 4 },
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
        bodyEl.textContent =
          data.mcConfirmBody || "Esta accion requiere tu confirmacion.";
        submitEl.textContent = data.mcConfirmLabel || "Confirmar";
        submitEl.className = "btn btn-" + (data.mcConfirmVariant || "primary");
        form.setAttribute("action", data.mcConfirmAction);

        modal.show();
      });
    });
  }

  /* --------------------------------------------------------
       Panel lateral con contenido por fetch (HU-CON-001)
       ------------------------------------------------------------
       El disparador pide el formulario al servidor y lo muestra
       en un Offcanvas de Bootstrap:
         <button data-mc-drawer
                 data-mc-drawer-target="#mcProductoDrawer"
                 data-mc-drawer-url="/admin/productos/5/editar">
       El panel debe tener [data-mc-drawer-body] (donde se inyecta
       el HTML) y, opcionalmente, [data-mc-drawer-titulo]; el HTML
       recibido puede traer [data-mc-drawer-titulo-valor] para
       cambiar el titulo. Con [data-mc-drawer-autoopen] el panel se
       abre solo al cargar la pagina (formulario con errores).
       -------------------------------------------------------- */
  function activarContenidoDelPanel(panel, cuerpo) {
    var titulo = panel.querySelector("[data-mc-drawer-titulo]");
    var valor = cuerpo.querySelector("[data-mc-drawer-titulo-valor]");
    if (titulo && valor) {
      titulo.textContent = valor.textContent;
    }

    // El formulario llega despues de cargar la pagina: hay que enlazar su validacion.
    if (
      typeof jQuery !== "undefined" &&
      jQuery.validator &&
      jQuery.validator.unobtrusive
    ) {
      jQuery.validator.unobtrusive.parse(cuerpo);
    }

    // Tras mostrarse, el foco va al primer campo.
    var primerCampo = cuerpo.querySelector(
      "input:not([type=hidden]):not(.btn-check), select, textarea",
    );
    if (primerCampo) {
      panel.addEventListener(
        "shown.bs.offcanvas",
        function () {
          primerCampo.focus();
        },
        { once: true },
      );
    }
  }

  /* Tooltips de Bootstrap (data-bs-toggle="tooltip"): aparecen al instante con hover o foco
     y se cierran al hacer clic, para no quedar flotando sobre un dialogo. */
  function initTooltips() {
    if (!window.bootstrap) return;
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
      var tip = new bootstrap.Tooltip(el, { trigger: "hover focus", delay: { show: 150, hide: 0 } });
      el.addEventListener("click", function () { tip.hide(); });
    });
  }

  /* [data-mc-autosubmit]: al cambiar el valor se envia el formulario (filtros por GET). */
  function initAutoSubmit() {
    document.querySelectorAll("[data-mc-autosubmit]").forEach(function (el) {
      el.addEventListener("change", function () {
        if (el.form) {
          el.form.dispatchEvent(new Event("mc:autosubmit"));
          el.form.submit();
        }
      });
    });
  }

  function initDrawers() {
    if (typeof bootstrap === "undefined") {
      return;
    }

    document.querySelectorAll("[data-mc-drawer]").forEach(function (trigger) {
      trigger.addEventListener("click", function () {
        var panel = document.querySelector(trigger.dataset.mcDrawerTarget);
        var cuerpo = panel && panel.querySelector("[data-mc-drawer-body]");
        if (!panel || !cuerpo) {
          return;
        }

        var instancia = bootstrap.Offcanvas.getOrCreateInstance(panel);
        cuerpo.innerHTML =
          '<p class="text-secondary mb-0" role="status">Cargando...</p>';
        instancia.show();

        fetch(trigger.dataset.mcDrawerUrl, {
          credentials: "same-origin",
          headers: { "X-Requested-With": "XMLHttpRequest" },
        })
          .then(function (response) {
            // Sesion vencida: la API de login redirige; se sigue esa redireccion en la pagina completa.
            if (response.redirected) {
              window.location.href = response.url;
              return null;
            }
            if (!response.ok) {
              throw new Error("No se pudo cargar el formulario.");
            }
            return response.text();
          })
          .then(function (html) {
            if (html === null) {
              return;
            }
            cuerpo.innerHTML = html;
            activarContenidoDelPanel(panel, cuerpo);
          })
          .catch(function () {
            cuerpo.innerHTML =
              '<div class="alert alert-danger mb-0" role="alert">No se pudo cargar el formulario. Cierra el panel e intenta de nuevo.</div>';
          });
      });
    });

    document
      .querySelectorAll("[data-mc-drawer-autoopen]")
      .forEach(function (panel) {
        var cuerpo = panel.querySelector("[data-mc-drawer-body]");
        if (cuerpo) {
          activarContenidoDelPanel(panel, cuerpo);
        }
        bootstrap.Offcanvas.getOrCreateInstance(panel).show();
      });
  }

  /* --------------------------------------------------------
       Cuenta regresiva de bloqueo (HU-AUT-001, escenario 3)
       Uso: <span data-mc-countdown="300"> sobre el elemento que
       muestra el tiempo restante; al llegar a cero rehabilita el
       formulario indicado en data-mc-countdown-form.
       -------------------------------------------------------- */
  function initCountdowns() {
    document
      .querySelectorAll("[data-mc-countdown]")
      .forEach(function (element) {
        var remaining = parseInt(element.dataset.mcCountdown, 10);
        if (isNaN(remaining) || remaining <= 0) {
          return;
        }

        var container =
          element.closest("[data-mc-countdown-scope]") || document;

        function render() {
          var minutes = Math.floor(remaining / 60);
          var seconds = remaining % 60;
          element.textContent =
            minutes + ":" + String(seconds).padStart(2, "0");
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
       Campo que debe igualar a otro (ej. confirmar contrasena)
       Uso: <input data-mc-equal-to="#IdDelOtroCampo">
       Se apoya en la Constraint Validation API nativa
       (setCustomValidity) para integrarse con checkValidity()
       sin reimplementar la politica de contrasena en JS.
       -------------------------------------------------------- */
  function initEqualToFields() {
    document.querySelectorAll("[data-mc-equal-to]").forEach(function (campo) {
      var otro = document.querySelector(campo.dataset.mcEqualTo);
      if (!otro) {
        return;
      }

      function validar() {
        var coincide = campo.value.length > 0 && campo.value === otro.value;
        campo.setCustomValidity(coincide ? "" : "Las contrasenas no coinciden.");
      }

      campo.addEventListener("input", validar);
      otro.addEventListener("input", validar);
      validar();
    });
  }

  /* --------------------------------------------------------
       Deshabilita el boton de envio hasta que el formulario
       sea valido (requisitos nativos: required, minlength,
       pattern, y data-mc-equal-to via setCustomValidity).
       Uso: <form data-mc-enable-submit>
       -------------------------------------------------------- */
  function initSubmitEnableOnValid() {
    document
      .querySelectorAll("form[data-mc-enable-submit]")
      .forEach(function (form) {
        var boton =
          form.querySelector('[type="submit"]') ||
          (form.id &&
            document.querySelector('[form="' + form.id + '"][type="submit"]'));
        if (!boton) {
          return;
        }

        function actualizar() {
          boton.disabled = !form.checkValidity();
        }

        form.addEventListener("input", actualizar);
        form.addEventListener("change", actualizar);
        actualizar();
      });
  }

  /* --------------------------------------------------------
       Deshabilita el boton de envio hasta que algun campo con
       data-mc-original cambie respecto a su valor original
       (pantalla de Parametros: no reenviar si nada cambio).
       Uso: <form data-mc-enable-on-change> ... <input data-mc-original="valor-inicial">
       -------------------------------------------------------- */
  function initSubmitEnableOnChange() {
    document
      .querySelectorAll("form[data-mc-enable-on-change]")
      .forEach(function (form) {
        var boton =
          form.querySelector('[type="submit"]') ||
          (form.id &&
            document.querySelector('[form="' + form.id + '"][type="submit"]'));
        var campos = form.querySelectorAll("[data-mc-original]");
        if (!boton || campos.length === 0) {
          return;
        }

        function hayCambios() {
          return Array.prototype.some.call(campos, function (campo) {
            return campo.value !== campo.dataset.mcOriginal;
          });
        }

        function actualizar() {
          boton.disabled = !hayCambios();
        }

        form.addEventListener("input", actualizar);
        form.addEventListener("change", actualizar);
        actualizar();
      });
  }

  /* --------------------------------------------------------
       Evita el doble envio de formularios

       form.checkValidity() (Constraint Validation API nativa) no
       ve las reglas que genera asp-for + DataAnnotations, porque
       esas se expresan como atributos data-val-* que interpreta
       jQuery Validation Unobtrusive, no como atributos nativos
       (required, pattern, etc.). Por eso se consulta primero al
       validador de jQuery si el formulario tiene uno adjunto, y
       solo se usa checkValidity() como respaldo si no lo tiene.
       -------------------------------------------------------- */
  function formEsValido(form) {
    if (window.jQuery && jQuery(form).data("validator")) {
      return jQuery(form).valid();
    }

    return !form.checkValidity || form.checkValidity();
  }

  function initSubmitGuard() {
    document
      .querySelectorAll("form[data-mc-guard-submit]")
      .forEach(function (form) {
        form.addEventListener("submit", function () {
          if (!formEsValido(form)) {
            return;
          }

          var button =
            form.querySelector('[type="submit"]') ||
            (form.id &&
              document.querySelector('[form="' + form.id + '"][type="submit"]'));
          if (!button) {
            return;
          }

          button.disabled = true;
          button.insertAdjacentHTML(
            "afterbegin",
            '<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>',
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
    document
      .querySelectorAll("[data-mc-autodismiss]")
      .forEach(function (alert) {
        setTimeout(
          function () {
            if (typeof bootstrap !== "undefined") {
              bootstrap.Alert.getOrCreateInstance(alert).close();
            }
          },
          parseInt(alert.dataset.mcAutodismiss, 10) || 6000,
        );
      });
  }

  /* --------------------------------------------------------
       Calendario de nueva reserva (HU-RES-002, andamiaje visual)
       Solo conserva seleccion y navegacion local. La API sera la
       unica fuente de fechas y horarios realmente disponibles.

       HU-RES-003: al pulsar "Continuar", navega al paso 2
       (/Reserva/Datos) enviando fecha, hora y cantidad de personas
       como parametros de consulta.
       -------------------------------------------------------- */
  function initReservationCalendars() {
    document
      .querySelectorAll("[data-mc-reservation-calendar]")
      .forEach(function (calendar) {
        var peopleOutput = calendar.querySelector(
          "[data-mc-reservation-people]",
        );
        var decreaseButton = calendar.querySelector(
          "[data-mc-reservation-decrease]",
        );
        var increaseButton = calendar.querySelector(
          "[data-mc-reservation-increase]",
        );
        var previousButton = calendar.querySelector(
          "[data-mc-reservation-previous-month]",
        );
        var nextButton = calendar.querySelector(
          "[data-mc-reservation-next-month]",
        );
        var monthLabel = calendar.querySelector("[data-mc-reservation-month]");
        var daysContainer = calendar.querySelector(
          "[data-mc-reservation-days]",
        );
        var timesEmpty = calendar.querySelector(
          "[data-mc-reservation-times-empty]",
        );
        var timeList = calendar.querySelector(
          "[data-mc-reservation-time-list]",
        );
        var continueButton = calendar.querySelector(
          "[data-mc-reservation-continue]",
        );
        var selectedPeople = calendar.querySelector(
          "[data-mc-reservation-selected-people]",
        );
        var selectedDateInput = calendar.querySelector(
          "[data-mc-reservation-selected-date]",
        );
        var selectedTimeInput = calendar.querySelector(
          "[data-mc-reservation-selected-time]",
        );
        var availabilityUrl = calendar.dataset.mcAvailabilityUrl;
        var availabilityMonthUrl = calendar.dataset.mcAvailabilityMonthUrl;
        var datosUrl = calendar.dataset.mcDatosUrl; // HU-RES-003

        if (
          !peopleOutput ||
          !decreaseButton ||
          !increaseButton ||
          !previousButton ||
          !nextButton ||
          !monthLabel ||
          !daysContainer ||
          !timesEmpty ||
          !timeList ||
          !continueButton ||
          !availabilityUrl ||
          !availabilityMonthUrl
        ) {
          return;
        }

        var people = parseInt(calendar.dataset.mcInitialPeople, 10);
        if (isNaN(people) || people < 1) {
          people = 1;
        }

        var displayedMonth = new Date();
        displayedMonth = new Date(
          displayedMonth.getFullYear(),
          displayedMonth.getMonth(),
          1,
        );
        var selectedDate = null;
        var selectedTime = null;
        var availabilityByDate = {};
        var monthRequestVersion = 0;
        var dateRequestVersion = 0;
        var monthFormatter = new Intl.DateTimeFormat("es-CR", {
          month: "long",
          year: "numeric",
        });

        function formatDateKey(date) {
          return (
            date.getFullYear() +
            "-" +
            String(date.getMonth() + 1).padStart(2, "0") +
            "-" +
            String(date.getDate()).padStart(2, "0")
          );
        }

        function formatTime(time) {
          var parts = time.split(":");
          return parts[0] + ":" + parts[1];
        }

        function updateContinue() {
          var valid = people > 0 && selectedDate && selectedTime;
          continueButton.disabled = !valid;
          if (selectedPeople) {
            selectedPeople.value = String(people);
          }
          if (selectedDateInput) {
            selectedDateInput.value = selectedDate || "";
          }
          if (selectedTimeInput) {
            selectedTimeInput.value = selectedTime || "";
          }
        }

        function renderPeople() {
          peopleOutput.textContent = String(people);
          decreaseButton.disabled = people === 1;
          updateContinue();
        }

        function clearTimes(message) {
          selectedTime = null;
          timeList.replaceChildren();
          timeList.hidden = true;
          timesEmpty.hidden = false;
          timesEmpty.querySelector("p").textContent = message;
          updateContinue();
        }

        function renderTimes(horarios) {
          selectedTime = null;
          timeList.replaceChildren();
          horarios.forEach(function (hora) {
            var button = document.createElement("button");
            button.type = "button";
            button.className = "mc-reservation__time is-available";
            button.textContent = formatTime(hora);
            button.dataset.mcReservationTime = hora;
            button.setAttribute("aria-pressed", "false");
            button.addEventListener("click", function () {
              selectedTime = hora;
              timeList
                .querySelectorAll("[data-mc-reservation-time]")
                .forEach(function (timeButton) {
                  var selected = timeButton === button;
                  timeButton.classList.toggle("is-selected", selected);
                  timeButton.setAttribute("aria-pressed", String(selected));
                });
              updateContinue();
            });
            timeList.appendChild(button);
          });
          timesEmpty.hidden = true;
          timeList.hidden = false;
          updateContinue();
        }

        function renderCalendar() {
          monthLabel.textContent = monthFormatter.format(displayedMonth);
          daysContainer.replaceChildren();
          var firstWeekday = displayedMonth.getDay();
          var daysInMonth = new Date(
            displayedMonth.getFullYear(),
            displayedMonth.getMonth() + 1,
            0,
          ).getDate();

          for (var blank = 0; blank < firstWeekday; blank++) {
            var spacer = document.createElement("span");
            spacer.setAttribute("aria-hidden", "true");
            daysContainer.appendChild(spacer);
          }

          for (var dayNumber = 1; dayNumber <= daysInMonth; dayNumber++) {
            var date = new Date(
              displayedMonth.getFullYear(),
              displayedMonth.getMonth(),
              dayNumber,
            );
            var dateKey = formatDateKey(date);
            var available = availabilityByDate[dateKey] === true;
            var button = document.createElement("button");
            button.type = "button";
            button.className = "mc-reservation__day";
            button.dataset.mcReservationDate = dateKey;
            button.textContent = String(dayNumber);
            button.disabled = !available;
            button.classList.toggle("is-unavailable", !available);
            button.classList.toggle(
              "is-selected",
              dateKey === selectedDate && available,
            );
            button.setAttribute(
              "aria-pressed",
              String(dateKey === selectedDate && available),
            );
            button.setAttribute(
              "aria-label",
              date.toLocaleDateString("es-CR", {
                weekday: "long",
                day: "numeric",
                month: "long",
                year: "numeric",
              }),
            );
            (function (dayButton, dayKey) {
              dayButton.addEventListener("click", function () {
                if (dayButton.disabled) {
                  return;
                }
                selectedDate = dayKey;
                clearTimes("Consultando los horarios disponibles...");
                renderCalendar();
                loadDateAvailability();
              });
            })(button, dateKey);
            daysContainer.appendChild(button);
          }
        }

        function loadDateAvailability() {
          if (!selectedDate) {
            return;
          }
          var requestVersion = ++dateRequestVersion;
          var fechaSolicitada = selectedDate;
          fetch(
            availabilityUrl +
              "?" +
              new URLSearchParams({
                cantidadPersonas: String(people),
                fecha: fechaSolicitada,
              }),
          )
            .then(function (response) {
              return response.json();
            })
            .then(function (payload) {
              if (
                requestVersion !== dateRequestVersion ||
                fechaSolicitada !== selectedDate
              ) {
                return;
              }
              if (
                !payload.success ||
                !payload.data ||
                !payload.data.disponible
              ) {
                selectedDate = null;
                clearTimes(
                  payload.message ||
                    (payload.data && payload.data.motivoNoDisponible) ||
                    "La fecha ya no tiene disponibilidad.",
                );
                renderCalendar();
                return;
              }
              renderTimes(payload.data.horariosDisponibles || []);
            })
            .catch(function () {
              if (
                requestVersion !== dateRequestVersion ||
                fechaSolicitada !== selectedDate
              ) {
                return;
              }
              clearTimes(
                "No se pudo consultar la disponibilidad. Intente de nuevo.",
              );
            });
        }

        function loadMonthAvailability() {
          var requestVersion = ++monthRequestVersion;
          availabilityByDate = {};
          clearTimes(
            selectedDate
              ? "Actualizando disponibilidad..."
              : "Seleccione una fecha para consultar los horarios disponibles.",
          );
          renderCalendar();
          fetch(
            availabilityMonthUrl +
              "?" +
              new URLSearchParams({
                cantidadPersonas: String(people),
                anio: String(displayedMonth.getFullYear()),
                mes: String(displayedMonth.getMonth() + 1),
              }),
          )
            .then(function (response) {
              return response.json();
            })
            .then(function (payload) {
              if (requestVersion !== monthRequestVersion) {
                return;
              }
              if (!payload.success || !payload.data) {
                clearTimes(
                  payload.message || "No se pudo consultar la disponibilidad.",
                );
                renderCalendar();
                return;
              }
              (payload.data.fechas || []).forEach(function (fecha) {
                availabilityByDate[fecha.fecha] = fecha.disponible === true;
              });
              if (selectedDate && availabilityByDate[selectedDate] !== true) {
                selectedDate = null;
                clearTimes(
                  "Seleccione una fecha para consultar los horarios disponibles.",
                );
              }
              renderCalendar();
              if (selectedDate) {
                loadDateAvailability();
              }
            })
            .catch(function () {
              if (requestVersion !== monthRequestVersion) {
                return;
              }
              clearTimes(
                "No se pudo consultar la disponibilidad. Intente de nuevo.",
              );
              renderCalendar();
            });
        }

        decreaseButton.addEventListener("click", function () {
          if (people > 1) {
            people--;
            selectedTime = null;
            renderPeople();
            loadMonthAvailability();
          }
        });

        increaseButton.addEventListener("click", function () {
          people++;
          selectedTime = null;
          renderPeople();
          loadMonthAvailability();
        });

        previousButton.addEventListener("click", function () {
          displayedMonth = new Date(
            displayedMonth.getFullYear(),
            displayedMonth.getMonth() - 1,
            1,
          );
          selectedDate = null;
          selectedTime = null;
          loadMonthAvailability();
        });

        nextButton.addEventListener("click", function () {
          displayedMonth = new Date(
            displayedMonth.getFullYear(),
            displayedMonth.getMonth() + 1,
            1,
          );
          selectedDate = null;
          selectedTime = null;
          loadMonthAvailability();
        });

        // HU-RES-003: navega al paso 2 con la seleccion actual.
        continueButton.addEventListener("click", function () {
          if (
            continueButton.disabled ||
            !selectedDate ||
            !selectedTime ||
            !datosUrl
          ) {
            return;
          }

          var url = new URL(datosUrl, window.location.origin);
          url.searchParams.set("fechaReserva", selectedDate);
          url.searchParams.set("horaReserva", selectedTime);
          url.searchParams.set("cantidadPersonas", String(people));

          window.location.href = url.toString();
        });

        renderPeople();
        loadMonthAvailability();
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

      if (
        isNaN(hours) ||
        isNaN(minutes) ||
        hours < 0 ||
        hours > 23 ||
        minutes < 0 ||
        minutes > 59
      ) {
        return "Selecciona una hora";
      }

      var period = hours < 12 ? "a. m." : "p. m.";
      var displayHour = hours % 12 || 12;
      return (
        String(displayHour).padStart(2, "0") +
        ":" +
        String(minutes).padStart(2, "0") +
        " " +
        period
      );
    }

    function createStandardValues(minimum, maximum) {
      var values = [];
      for (var hour = 0; hour < 24; hour++) {
        [0, 30].forEach(function (minute) {
          var value =
            String(hour).padStart(2, "0") +
              ":" +
              String(minute).padStart(2, "0");
          if ((!minimum || value >= minimum) && (!maximum || value <= maximum)) {
            values.push(value);
          }
        });
      }
      return values;
    }

    document
      .querySelectorAll("[data-mc-time-picker]")
      .forEach(function (picker) {
        var valueInput = picker.querySelector("[data-mc-time-picker-value]");
        var trigger = picker.querySelector("[data-mc-time-picker-trigger]");
        var label = picker.querySelector("[data-mc-time-picker-label]");
        var menu = picker.querySelector("[data-mc-time-picker-menu]");

        if (!valueInput || !trigger || !label || !menu || trigger.disabled) {
          return;
        }

        var minimum = picker.dataset.mcTimePickerMin;
        var maximum = picker.dataset.mcTimePickerMax;
        var tieneRango = !!minimum || !!maximum;
        var values = createStandardValues(minimum, maximum);
        // Los datos anteriores pueden contener una hora valida fuera de
        // los intervalos nuevos. En selectores sin rango se conservan como
        // opcion; en Horarios se mantienen visibles en el trigger, pero no
        // se ofrecen como una nueva opcion seleccionable.
        if (
          !tieneRango &&
          valueInput.value &&
          values.indexOf(valueInput.value) === -1 &&
          formatTime(valueInput.value) !== "Selecciona una hora"
        ) {
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
          return Array.prototype.slice.call(
            menu.querySelectorAll("[data-mc-time-picker-option]"),
          );
        }

        function selectedOption() {
          return menu.querySelector(
            "[data-mc-time-picker-option='" + valueInput.value + "']",
          );
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
          valueInput.dispatchEvent(new Event("input", { bubbles: true }));
          valueInput.dispatchEvent(new Event("change", { bubbles: true }));
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

          if (event.key === "ArrowDown")
            nextIndex = Math.min(index + 1, allOptions.length - 1);
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

  /* --------------------------------------------------------
       Cuenta de la mesa y caja de pedido (HU-CON-002)
       ------------------------------------------------------------
       El HTML de la cuenta (_Cuenta) y de la caja (_CajaPedido) llega
       por fetch desde admin/ventas, asi que todo va por delegacion de
       eventos en document. Nada aqui decide precios ni totales: el
       servidor los calcula; la pantalla solo los muestra.

       Eventos para la pantalla anfitriona (en document):
         mc:cuenta-cambio  detail: { tipo, mensaje, idReserva, data }
           tipo = "pedido" | "quitar" | "cobro" | "sin-consumo" | "sin-cobro" | "desactualizada"
       -------------------------------------------------------- */
  var colones = window.mc.colones;
  var enviarJson = window.mc.enviarJson;

  function emitirCambio(origen, detalle) {
    (origen || document).dispatchEvent(
      new CustomEvent("mc:cuenta-cambio", { bubbles: true, detail: detalle }),
    );
  }

  function esPlural(etiquetaMesa) {
    return /^Mesas/.test(etiquetaMesa || "");
  }

  function mostrarErrorCuenta(raiz, mensaje) {
    var caja = raiz.querySelector("[data-mc-cuenta-error]");
    if (!caja) {
      return;
    }
    caja.textContent = mensaje;
    caja.hidden = false;
  }

  function limpiarErrorCuenta(raiz) {
    var caja = raiz.querySelector("[data-mc-cuenta-error]");
    if (caja) {
      caja.hidden = true;
    }
  }

  function bloquearControles(raiz, bloquear) {
    raiz.querySelectorAll("button, input").forEach(function (el) {
      el.disabled = bloquear;
    });
  }

  /* Resuelve el resultado de una accion sobre la cuenta. */
  function resolverAccion(raiz, tipo, respuesta, mensajeExito) {
    var idReserva = Number(raiz.dataset.idReserva);
    bloquearControles(raiz, false);

    if (respuesta.success) {
      emitirCambio(raiz, {
        tipo: tipo,
        mensaje: mensajeExito,
        idReserva: idReserva,
        data: respuesta.data,
      });
      return;
    }

    mostrarErrorCuenta(raiz, respuesta.message);
    // La cuenta cambio mientras tanto (otro administrador, otra pantalla): hay que volver a pedirla.
    if (respuesta.cuenta) {
      emitirCambio(raiz, {
        tipo: "desactualizada",
        mensaje: respuesta.message,
        idReserva: idReserva,
        data: respuesta.cuenta,
      });
    }
  }

  function metodoElegido(raiz) {
    return raiz.querySelector("[data-mc-cuenta-pagos] input:checked");
  }

  function textoBotonCobrar(raiz) {
    var texto = raiz.querySelector("[data-mc-cuenta-cobrar-texto]");
    if (!texto) {
      return;
    }
    var metodo = metodoElegido(raiz);
    var total = colones(Number(raiz.dataset.total));
    texto.textContent = metodo
      ? "Cobrar " + total + " con " + metodo.dataset.nombre
      : "Cobrar " + total;
  }

  function cuentaDe(elemento) {
    return elemento.closest("[data-mc-cuenta]");
  }

  function abrirConfirmacionQuitar(raiz, boton) {
    var cantidad = Number(boton.dataset.cantidad);
    var bloque = raiz.querySelector("[data-mc-cuenta-confirm-quitar]");
    if (!bloque) {
      return;
    }
    // Un producto puede estar repartido en varias lineas (pedidos distintos): se quitan de la mas reciente a la mas antigua.
    raiz._quitar = { lineas: JSON.parse(boton.dataset.lineas || "[]"), nombre: boton.dataset.nombre };
    bloque.querySelector("[data-mc-cuenta-quitar-texto]").textContent =
      "Quitar «" + boton.dataset.nombre + "» de la cuenta. Queda registrado quién lo quitó.";
    bloque.querySelector("[data-mc-cuenta-quitar-uno]").hidden = cantidad <= 1;
    bloque.querySelector("[data-mc-cuenta-quitar-todo]").textContent =
      cantidad > 1 ? "Quitar todo" : "Quitar";
    bloque.hidden = false;
    bloque.querySelector("button:not([hidden])").focus();
  }

  /* Reparte `cantidad` unidades entre las lineas de un producto, de la mas reciente a la mas antigua
     (el orden en que llegan). Devuelve [{ idDetalle, cantidad, valor }]; es la unica regla de "quitar". */
  function repartirQuitar(lineas, cantidad) {
    var resto = cantidad;
    var operaciones = [];
    lineas.forEach(function (l) {
      var tomar = Math.min(resto, l.cantidad);
      if (tomar > 0) {
        operaciones.push({ idDetalle: l.idDetalle, cantidad: tomar, valor: tomar * (l.precio || 0) });
        resto -= tomar;
      }
    });
    return operaciones;
  }

  /* Anula las lineas una tras otra (la API no tiene una operacion unica) y se detiene en el primer fallo.
     Devuelve { fallo, aplicadas, cuenta }: si fallo existe y aplicadas > 0, la cuenta quedo a medias. */
  async function anularEnSecuencia(operaciones) {
    var resultado = { fallo: null, aplicadas: 0, cuenta: null };
    for (var i = 0; !resultado.fallo && i < operaciones.length; i += 1) {
      var respuesta = await enviarJson("/admin/ventas/cuenta/lineas/" + operaciones[i].idDetalle + "/anular", {
        cantidad: operaciones[i].cantidad,
      });
      if (respuesta.success) {
        resultado.aplicadas += 1;
        resultado.cuenta = respuesta.data || resultado.cuenta;
      } else {
        resultado.fallo = respuesta;
      }
    }
    return resultado;
  }

  async function ejecutarQuitar(raiz, cantidad) {
    var pendiente = raiz._quitar;
    if (!pendiente) {
      return;
    }
    var unidades = pendiente.lineas.reduce(function (suma, l) {
      return suma + l.cantidad;
    }, 0);
    var idReserva = Number(raiz.dataset.idReserva);

    limpiarErrorCuenta(raiz);
    bloquearControles(raiz, true);
    var resultado = await anularEnSecuencia(repartirQuitar(pendiente.lineas, cantidad || unidades));

    if (resultado.fallo && resultado.aplicadas > 0) {
      // Se quito una parte: la cuenta que se ve ya no coincide, hay que volver a pedirla.
      bloquearControles(raiz, false);
      emitirCambio(raiz, {
        tipo: "desactualizada",
        mensaje: "No se pudo quitar todo: " + resultado.fallo.message,
        idReserva: idReserva,
        data: resultado.fallo.cuenta,
      });
      return;
    }
    resolverAccion(
      raiz,
      "quitar",
      resultado.fallo || { success: true, data: resultado.cuenta },
      "Producto quitado de la cuenta.",
    );
  }

  function ejecutarCobro(raiz) {
    var metodo = metodoElegido(raiz);
    var pagos = raiz.querySelector("[data-mc-cuenta-pagos]");
    var error = raiz.querySelector("[data-mc-cuenta-pago-error]");
    if (!metodo) {
      if (pagos) {
        pagos.classList.add("is-invalid");
      }
      if (error) {
        error.hidden = false;
      }
      return;
    }

    limpiarErrorCuenta(raiz);
    var etiquetaMesa = raiz.dataset.mesa;
    var nombreMetodo = metodo.dataset.nombre;
    bloquearControles(raiz, true);
    enviarJson("/admin/ventas/cuenta/" + raiz.dataset.idReserva + "/cobrar", {
      idMetodoPago: Number(metodo.value),
      totalEsperado: Number(raiz.dataset.total),
      version: raiz.dataset.version || null,
    }).then(function (respuesta) {
      var mensaje = "";
      if (respuesta.success && respuesta.data) {
        mensaje =
          etiquetaMesa +
          (esPlural(etiquetaMesa) ? " cobradas y liberadas" : " cobrada y liberada") +
          " · " + respuesta.data.folio +
          " · " + colones(respuesta.data.total) +
          " con " + nombreMetodo;
      }
      resolverAccion(raiz, "cobro", respuesta, mensaje);
    });
  }

  var MOTIVO_MIN = 3;

  function motivoSinCobro(raiz) {
    var campo = raiz.querySelector("[data-mc-cuenta-sincobro-motivo]");
    return campo ? campo.value.trim() : "";
  }

  function ejecutarCerrarSinCobro(raiz) {
    var motivo = motivoSinCobro(raiz);
    if (motivo.length < MOTIVO_MIN) {
      return;
    }
    limpiarErrorCuenta(raiz);
    var etiquetaMesa = raiz.dataset.mesa;
    bloquearControles(raiz, true);
    enviarJson("/admin/ventas/cuenta/" + raiz.dataset.idReserva + "/cerrar-sin-cobro", {
      motivo: motivo,
      version: raiz.dataset.version || null,
    }).then(function (respuesta) {
      var mensaje = "";
      if (respuesta.success && respuesta.data) {
        mensaje =
          etiquetaMesa +
          (esPlural(etiquetaMesa) ? " liberadas" : " liberada") +
          " · " + respuesta.data.folio +
          " cerrada sin cobro · " + colones(respuesta.data.total) +
          " · motivo: " + motivo;
      }
      resolverAccion(raiz, "sin-cobro", respuesta, mensaje);
    });
  }

  function ejecutarLiberar(raiz) {
    limpiarErrorCuenta(raiz);
    var etiquetaMesa = raiz.dataset.mesa;
    bloquearControles(raiz, true);
    enviarJson("/admin/ventas/cuenta/" + raiz.dataset.idReserva + "/liberar-sin-consumo").then(
      function (respuesta) {
        var mensaje = "";
        if (respuesta.success && respuesta.data) {
          mensaje =
            etiquetaMesa +
            (esPlural(etiquetaMesa) ? " liberadas" : " liberada") +
            " sin consumo · no se registró una venta";
        }
        resolverAccion(raiz, "sin-consumo", respuesta, mensaje);
      },
    );
  }

  function abrirCaja(raiz, boton) {
    if (typeof bootstrap === "undefined") {
      return;
    }
    limpiarErrorCuenta(raiz);
    boton.disabled = true;
    fetch("/admin/ventas/cuenta/" + raiz.dataset.idReserva + "/caja", {
      credentials: "same-origin",
      headers: { "X-Requested-With": "XMLHttpRequest" },
    })
      .then(function (respuesta) {
        if (respuesta.redirected) {
          window.location.href = respuesta.url;
          return null;
        }
        return respuesta.text().then(function (html) {
          return { ok: respuesta.ok, html: html };
        });
      })
      .then(function (resultado) {
        boton.disabled = false;
        if (!resultado) {
          return;
        }
        if (!resultado.ok) {
          var temporal = document.createElement("div");
          temporal.innerHTML = resultado.html;
          mostrarErrorCuenta(raiz, temporal.textContent || "No se pudo abrir la caja.");
          return;
        }
        montarCaja(resultado.html);
      })
      .catch(function () {
        boton.disabled = false;
        mostrarErrorCuenta(raiz, "No se pudo abrir la caja. Intenta de nuevo.");
      });
  }

  /* ---------------- Caja de pedido ---------------- */
  function montarCaja(html) {
    var anterior = document.getElementById("mcCajaModal");
    if (anterior) {
      bootstrap.Modal.getOrCreateInstance(anterior).dispose();
      anterior.remove();
    }

    var contenedor = document.createElement("div");
    contenedor.innerHTML = html.trim();
    var modalEl = contenedor.firstElementChild;
    document.body.appendChild(modalEl);

    var carrito = []; // pedido nuevo: [{ id, nombre, precio, cantidad }]
    var clave = crypto.randomUUID(); // un reintento del mismo pedido no lo duplica
    var enviando = false;
    var totalCuenta = Number(modalEl.dataset.cuentaTotal);
    var categoriaActiva = 0;

    // Lo que ya esta en la cuenta, por producto (lineas activas, la mas reciente primero), y las unidades que
    // el administrador quita en esta caja. Un solo ticket: "+" y "-" actuan igual sobre lo nuevo y lo existente;
    // al guardar se registra el pedido nuevo y se anulan (con usuario y fecha) las unidades quitadas.
    var yaPorProducto = {};
    JSON.parse(modalEl.dataset.lineas || "[]").forEach(function (l) {
      var p = yaPorProducto[l.idProducto];
      if (!p) {
        p = yaPorProducto[l.idProducto] = { id: l.idProducto, nombre: l.nombre, lineas: [], cantidad: 0, valor: 0 };
      }
      p.lineas.push(l);
      p.cantidad += l.cantidad;
      p.valor += l.cantidad * l.precio;
    });
    var quitar = {}; // idProducto -> unidades de la cuenta que se quitaran

    var lineasEl = modalEl.querySelector("[data-mc-caja-lineas]");
    var vacioEl = modalEl.querySelector("[data-mc-caja-vacio]");
    var totalPedidoEl = modalEl.querySelector("[data-mc-caja-total-pedido]");
    var quedaraEl = modalEl.querySelector("[data-mc-caja-total-quedara]");
    var quitadoFilaEl = modalEl.querySelector("[data-mc-caja-quitado-fila]");
    var quitadoEl = modalEl.querySelector("[data-mc-caja-total-quitado]");
    var avisoQuitarEl = modalEl.querySelector("[data-mc-caja-aviso-quitar]");
    var resumenEl = modalEl.querySelector("[data-mc-caja-resumen]");
    var confirmarEl = modalEl.querySelector("[data-mc-caja-confirmar]");
    var errorEl = modalEl.querySelector("[data-mc-caja-error]");
    var buscarEl = modalEl.querySelector("[data-mc-caja-buscar]");

    function buscar(id) {
      return carrito.find(function (l) {
        return l.id === id;
      });
    }

    function tilePorId(id) {
      return modalEl.querySelector('[data-mc-caja-tile][data-id-producto="' + id + '"]');
    }

    function cantidadYa(id) {
      return yaPorProducto[id] ? yaPorProducto[id].cantidad : 0;
    }

    function cantidadNueva(id) {
      var linea = buscar(id);
      return linea ? linea.cantidad : 0;
    }

    function cantidadQuitar(id) {
      return quitar[id] || 0;
    }

    // Valor de lo que se quita (misma regla que se aplica al guardar).
    function valorQuitado(id) {
      if (!yaPorProducto[id]) {
        return 0;
      }
      return repartirQuitar(yaPorProducto[id].lineas, cantidadQuitar(id)).reduce(function (suma, op) {
        return suma + op.valor;
      }, 0);
    }

    function operacionesQuitar() {
      var operaciones = [];
      Object.keys(quitar).forEach(function (id) {
        operaciones = operaciones.concat(repartirQuitar(yaPorProducto[id].lineas, quitar[id]));
      });
      return operaciones;
    }

    function pintar() {
      var ids = Object.keys(yaPorProducto).map(Number);
      carrito.forEach(function (l) {
        if (ids.indexOf(l.id) === -1) {
          ids.push(l.id);
        }
      });

      var totalNuevo = 0;
      var totalQuitado = 0;
      var nuevas = 0;
      var quitadas = 0;
      lineasEl.innerHTML = "";
      ids.forEach(function (id) {
        var ya = yaPorProducto[id];
        var nueva = buscar(id);
        var nombre = ya ? ya.nombre : nueva.nombre;
        var cNueva = nueva ? nueva.cantidad : 0;
        var cQuitar = cantidadQuitar(id);
        var cantidad = cantidadYa(id) - cQuitar + cNueva;
        var subtotalNuevo = nueva ? nueva.cantidad * nueva.precio : 0;
        var quitado = valorQuitado(id);
        totalNuevo += subtotalNuevo;
        totalQuitado += quitado;
        nuevas += cNueva;
        quitadas += cQuitar;

        var marcas = [];
        // "Nuevo": producto que no estaba en la cuenta; "+N nuevo": mas unidades de uno que ya estaba.
        if (cNueva > 0) {
          marcas.push(cantidadYa(id) > 0 ? "+" + cNueva + " nuevo" : "Nuevo");
        }
        if (cQuitar > 0) {
          marcas.push("\u2212" + cQuitar + " se quitar\u00e1");
        }
        // Sin precio vigente (producto ya no disponible) solo se puede quitar o deshacer lo quitado.
        var puedeSumar = cQuitar > 0 || !!tilePorId(id);

        var li = document.createElement("li");
        li.className = "mc-ticket__linea" + (cQuitar > 0 ? " is-quitar" : "");
        li.innerHTML =
          '<span class="mc-ticket__nombre"><span data-nombre></span><small class="mc-ticket__marca"></small></span>' +
          '<span class="mc-ticket__cantidad" role="group">' +
          '<button type="button" class="mc-icon-btn" data-mc-caja-menos aria-label="Quitar uno"><i class="bi bi-dash-lg" aria-hidden="true"></i></button>' +
          '<span class="mc-ticket__num" aria-live="polite"></span>' +
          '<button type="button" class="mc-icon-btn" data-mc-caja-mas aria-label="Agregar uno"><i class="bi bi-plus-lg" aria-hidden="true"></i></button>' +
          "</span>" +
          '<span class="mc-ticket__sub"></span>';
        li.dataset.id = id;
        li.querySelector("[data-nombre]").textContent = nombre;
        li.querySelector(".mc-ticket__marca").textContent = marcas.join(" \u00b7 ");
        li.querySelector(".mc-ticket__marca").hidden = marcas.length === 0;
        li.querySelector(".mc-ticket__num").textContent = cantidad;
        li.querySelector(".mc-ticket__sub").textContent = colones(
          (ya ? ya.valor : 0) - quitado + subtotalNuevo,
        );
        li.querySelector("[data-mc-caja-menos]").disabled = cantidad <= 0;
        li.querySelector("[data-mc-caja-mas]").disabled = !puedeSumar || cNueva >= 99;
        lineasEl.appendChild(li);
      });

      vacioEl.hidden = ids.length > 0;
      totalPedidoEl.textContent = colones(totalNuevo);
      if (quedaraEl) {
        quedaraEl.textContent = colones(totalCuenta - totalQuitado + totalNuevo);
      }
      if (quitadoFilaEl) {
        quitadoFilaEl.hidden = quitadas === 0;
        quitadoEl.textContent = "\u2212" + colones(totalQuitado);
      }
      avisoQuitarEl.hidden = quitadas === 0;

      var partes = [];
      if (nuevas > 0) {
        partes.push(nuevas + (nuevas === 1 ? " producto nuevo" : " productos nuevos"));
      }
      if (quitadas > 0) {
        partes.push(quitadas + (quitadas === 1 ? " se quitar\u00e1" : " se quitar\u00e1n"));
      }
      resumenEl.textContent = partes.length > 0 ? partes.join(" \u00b7 ") : "Usa + y \u2212 para armar el pedido";
      confirmarEl.textContent = nuevas > 0 && quitadas === 0 ? "Agregar a la cuenta" : "Guardar cambios";
      confirmarEl.disabled = (nuevas === 0 && quitadas === 0) || enviando;

      // Menu: la cantidad de cada producto es lo que quedara en la cuenta (lo que ya tenia, mas lo nuevo, menos lo quitado).
      var seleccionados = 0;
      modalEl.querySelectorAll("[data-mc-caja-tile]").forEach(function (tile) {
        var id = Number(tile.dataset.idProducto);
        var cantidad = cantidadYa(id) - cantidadQuitar(id) + cantidadNueva(id);
        var cambiado = cantidadNueva(id) > 0 || cantidadQuitar(id) > 0;
        tile.querySelector("[data-mc-caja-burbuja]").textContent = cantidad;
        tile.querySelector("[data-mc-caja-tile-menos]").disabled = cantidad <= 0;
        tile.querySelector("[data-mc-caja-tile-mas]").disabled = cantidadNueva(id) >= 99;
        tile.classList.toggle("is-en-cuenta", cantidad > 0);
        tile.classList.toggle("is-elegido", cambiado);
        var seleccionado = cantidadYa(id) > 0 || cantidadNueva(id) > 0;
        tile.dataset.sel = seleccionado ? "1" : "";
        if (seleccionado) {
          seleccionados += 1;
        }
      });

      // Categoria "Seleccionados": solo existe mientras haya algo en la cuenta o en el pedido nuevo.
      var chipSel = modalEl.querySelector("[data-mc-caja-seleccionados]");
      chipSel.hidden = seleccionados === 0;
      chipSel.querySelector("[data-mc-caja-seleccionados-n]").textContent = seleccionados;
      if (seleccionados === 0 && categoriaActiva === -1) {
        categoriaActiva = 0;
        modalEl.querySelectorAll("[data-mc-caja-categoria]").forEach(function (chip) {
          var activo = Number(chip.dataset.mcCajaCategoria) === 0;
          chip.classList.toggle("is-active", activo);
          chip.setAttribute("aria-pressed", String(activo));
        });
      }
      filtrar();
    }

    function filtrar() {
      var texto = (buscarEl.value || "").trim().toLowerCase();
      var visibles = 0;
      modalEl.querySelectorAll("[data-mc-caja-tile]").forEach(function (tile) {
        var coincideTexto = !texto || tile.dataset.nombre.toLowerCase().indexOf(texto) !== -1;
        var coincideCategoria =
          categoriaActiva === 0 ||
          (categoriaActiva === -1 ? tile.dataset.sel === "1" : Number(tile.dataset.categoria) === categoriaActiva);
        tile.hidden = !(coincideTexto && coincideCategoria);
        if (!tile.hidden) {
          visibles += 1;
        }
      });
      modalEl.querySelector("[data-mc-caja-sin-resultados]").hidden = visibles > 0;
    }

    // "+" suma al pedido nuevo (o deshace una unidad quitada); "-" resta del pedido nuevo y, si no queda nada
    // nuevo, marca una unidad de la cuenta para quitar.
    function cambiar(id, delta) {
      if (delta > 0) {
        if (cantidadQuitar(id) > 0) {
          quitar[id] -= 1;
          if (quitar[id] <= 0) {
            delete quitar[id];
          }
        } else {
          var tile = tilePorId(id);
          if (!tile) {
            return;
          }
          var linea = buscar(id);
          if (!linea) {
            linea = {
              id: id,
              nombre: tile.dataset.nombre,
              precio: Number(tile.dataset.precio),
              cantidad: 0,
            };
            carrito.push(linea);
          }
          linea.cantidad = Math.min(99, linea.cantidad + 1);
        }
      } else {
        var nueva = buscar(id);
        if (nueva) {
          nueva.cantidad -= 1;
          if (nueva.cantidad <= 0) {
            carrito.splice(carrito.indexOf(nueva), 1);
          }
        } else if (cantidadYa(id) - cantidadQuitar(id) > 0) {
          quitar[id] = cantidadQuitar(id) + 1;
        }
      }
      errorEl.hidden = true;
      pintar();
    }

    modalEl.addEventListener("click", function (event) {
      var tileMas = event.target.closest("[data-mc-caja-tile-mas]");
      var tileMenos = event.target.closest("[data-mc-caja-tile-menos]");
      if (tileMas || tileMenos) {
        var tile = (tileMas || tileMenos).closest("[data-mc-caja-tile]");
        cambiar(Number(tile.dataset.idProducto), tileMas ? 1 : -1);
        return;
      }

      var categoria = event.target.closest("[data-mc-caja-categoria]");
      if (categoria) {
        categoriaActiva = Number(categoria.dataset.mcCajaCategoria);
        modalEl.querySelectorAll("[data-mc-caja-categoria]").forEach(function (chip) {
          var activo = chip === categoria;
          chip.classList.toggle("is-active", activo);
          chip.setAttribute("aria-pressed", String(activo));
        });
        filtrar();
        return;
      }

      var mas = event.target.closest("[data-mc-caja-mas]");
      var menos = event.target.closest("[data-mc-caja-menos]");
      if (mas || menos) {
        var id = Number((mas || menos).closest("li").dataset.id);
        cambiar(id, mas ? 1 : -1);
      }
    });

    buscarEl.addEventListener("input", filtrar);

    // Enter agrega 1 del primer producto que coincide con la busqueda y la deja lista para el siguiente.
    buscarEl.addEventListener("keydown", function (event) {
      if (event.key !== "Enter") {
        return;
      }
      event.preventDefault();
      if (!buscarEl.value.trim()) {
        return;
      }
      var primero = modalEl.querySelector("[data-mc-caja-tile]:not([hidden])");
      if (primero) {
        cambiar(Number(primero.dataset.idProducto), 1);
        buscarEl.select();
      }
    });

    confirmarEl.addEventListener("click", async function () {
      var operaciones = operacionesQuitar();
      if (enviando || (carrito.length === 0 && operaciones.length === 0)) {
        return;
      }
      enviando = true;
      confirmarEl.disabled = true;
      errorEl.hidden = true;
      var idReserva = Number(modalEl.dataset.idReserva);
      var etiquetaMesa = modalEl.dataset.mesa;
      var hayNuevo = carrito.length > 0;
      var unidadesQuitadas = operaciones.reduce(function (suma, op) {
        return suma + op.cantidad;
      }, 0);
      var cuenta = null;
      var aplicado = false;
      var fallo = null;

      // Primero el pedido nuevo y despues las unidades quitadas: no son una sola transaccion, por eso
      // si algo falla a medias se cierra la caja y se vuelve a pedir la cuenta.
      if (hayNuevo) {
        var r = await enviarJson("/admin/ventas/cuenta/" + idReserva + "/pedidos", {
          claveIdempotencia: clave,
          lineas: carrito.map(function (l) {
            return { idProducto: l.id, cantidad: l.cantidad };
          }),
        });
        if (r.success) {
          cuenta = r.data || cuenta;
          aplicado = true;
        } else {
          fallo = r;
        }
      }
      if (!fallo) {
        var anulacion = await anularEnSecuencia(operaciones);
        if (anulacion.aplicadas > 0) {
          cuenta = anulacion.cuenta || cuenta;
          aplicado = true;
        }
        fallo = anulacion.fallo;
      }
      enviando = false;

      function cerrarYAvisar(detalle) {
        modalEl.addEventListener(
          "hidden.bs.modal",
          function () {
            emitirCambio(document, detalle);
          },
          { once: true },
        );
        bootstrap.Modal.getInstance(modalEl).hide();
      }

      if (!fallo) {
        var de = esPlural(etiquetaMesa) ? "las " : "la ";
        var total = colones((cuenta && cuenta.total) || 0);
        var quitadas = unidadesQuitadas + (unidadesQuitadas === 1 ? " producto quitado" : " productos quitados");
        var mensaje;
        if (hayNuevo) {
          mensaje =
            "Pedido " + (cuenta && cuenta.pedidos ? cuenta.pedidos.length : "") + " agregado" +
            (unidadesQuitadas > 0 ? " y " + quitadas : "") +
            " en la cuenta de " + de + etiquetaMesa + ": " + total + " en total.";
        } else {
          mensaje = "Cuenta de " + de + etiquetaMesa + " actualizada (" + quitadas + "): " + total + " en total.";
        }
        cerrarYAvisar({ tipo: hayNuevo ? "pedido" : "quitar", mensaje: mensaje, idReserva: idReserva, data: cuenta });
        return;
      }

      if (!aplicado) {
        errorEl.textContent = fallo.message;
        errorEl.hidden = false;
        pintar();
        if (fallo.cuenta) {
          emitirCambio(document, {
            tipo: "desactualizada",
            mensaje: fallo.message,
            idReserva: idReserva,
            data: fallo.cuenta,
          });
        }
        return;
      }

      cerrarYAvisar({
        tipo: "desactualizada",
        mensaje: "No se pudieron aplicar todos los cambios: " + fallo.message + " Revisa la cuenta.",
        idReserva: idReserva,
        data: fallo.cuenta || cuenta,
      });
    });

    modalEl.addEventListener("hidden.bs.modal", function () {
      bootstrap.Modal.getOrCreateInstance(modalEl).dispose();
      modalEl.remove();
    });

    pintar();
    bootstrap.Modal.getOrCreateInstance(modalEl).show();
    modalEl.addEventListener("shown.bs.modal", function () {
      buscarEl.focus();
    }, { once: true });
  }

  function initCuentas() {
    document.addEventListener("click", function (event) {
      var objetivo = event.target;
      var raiz = cuentaDe(objetivo);
      if (!raiz) {
        return;
      }

      var quitar = objetivo.closest("[data-mc-cuenta-quitar]");
      if (quitar) {
        abrirConfirmacionQuitar(raiz, quitar);
      } else if (objetivo.closest("[data-mc-cuenta-quitar-no]")) {
        raiz.querySelector("[data-mc-cuenta-confirm-quitar]").hidden = true;
      } else if (objetivo.closest("[data-mc-cuenta-quitar-uno]")) {
        ejecutarQuitar(raiz, 1);
      } else if (objetivo.closest("[data-mc-cuenta-quitar-todo]")) {
        ejecutarQuitar(raiz, null);
      } else if (objetivo.closest("[data-mc-cuenta-agregar]")) {
        abrirCaja(raiz, objetivo.closest("[data-mc-cuenta-agregar]"));
      } else if (objetivo.closest("[data-mc-cuenta-cobrar]")) {
        ejecutarCobro(raiz);
      } else if (objetivo.closest("[data-mc-cuenta-sincobro]")) {
        var confirmar = raiz.querySelector("[data-mc-cuenta-confirm-sincobro]");
        confirmar.hidden = false;
        confirmar.querySelector("[data-mc-cuenta-sincobro-motivo]").focus();
      } else if (objetivo.closest("[data-mc-cuenta-sincobro-no]")) {
        raiz.querySelector("[data-mc-cuenta-confirm-sincobro]").hidden = true;
      } else if (objetivo.closest("[data-mc-cuenta-sincobro-si]")) {
        ejecutarCerrarSinCobro(raiz);
      } else if (objetivo.closest("[data-mc-cuenta-liberar]")) {
        raiz.querySelector("[data-mc-cuenta-confirm-liberar]").hidden = false;
      } else if (objetivo.closest("[data-mc-cuenta-liberar-no]")) {
        raiz.querySelector("[data-mc-cuenta-confirm-liberar]").hidden = true;
      } else if (objetivo.closest("[data-mc-cuenta-liberar-si]")) {
        ejecutarLiberar(raiz);
      }
    });

    // El motivo es obligatorio: el boton de confirmar se habilita con tres caracteres o mas.
    document.addEventListener("input", function (event) {
      var campo = event.target.closest && event.target.closest("[data-mc-cuenta-sincobro-motivo]");
      var raiz = campo && cuentaDe(campo);
      if (raiz) {
        raiz.querySelector("[data-mc-cuenta-sincobro-si]").disabled = motivoSinCobro(raiz).length < MOTIVO_MIN;
      }
    });

    document.addEventListener("change", function (event) {
      var raiz = cuentaDe(event.target);
      if (!raiz || !event.target.closest("[data-mc-cuenta-pagos]")) {
        return;
      }
      raiz.querySelector("[data-mc-cuenta-pagos]").classList.remove("is-invalid");
      var error = raiz.querySelector("[data-mc-cuenta-pago-error]");
      if (error) {
        error.hidden = true;
      }
      textoBotonCobrar(raiz);
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    initPasswordToggles();
    initStrengthMeters();
    initSidebar();
    initConfirmDialog();
    initDrawers();
    initTooltips();
    initAutoSubmit();
    initCountdowns();
    initEqualToFields();
    initSubmitEnableOnValid();
    initSubmitEnableOnChange();
    initSubmitGuard();
    initAutoDismiss();
    initReservationCalendars();
    initTimePickers();
    initCuentas();
  });
})();
