/* ------------------------------------------------------------
   Horarios de operacion: edicion rapida (HU-CFG-005)

   La tabla sigue siendo el formulario real: cada fila tiene sus dos
   campos de hora (Dias[i].HoraApertura / HoraCierre, formato HH:mm)
   y se envia igual que antes. Las horas se escriben (1830, 18:30,
   6pm) o se eligen de la lista del navegador; al salir del campo se
   normalizan a HH:mm. Este archivo agrega ademas la seleccion de
   dias (casillas en la tabla, botones y atajos en el panel, todo
   sincronizado) y aplica un horario a los dias elegidos.
   Nada aqui reemplaza al servidor: la API vuelve a comprobar todo.
   ------------------------------------------------------------ */
(function () {
  "use strict";

  var MIN = 0; // 00:00
  var MAX = 23 * 60 + 30; // 23:30
  var PASO = 30;

  function iniciar() {
    var raiz = document.querySelector("[data-mc-horarios]");
    if (!raiz) return;

    var form = document.getElementById("horarios-operacion-form");
    var filas = Array.prototype.slice.call(raiz.querySelectorAll("[data-hor-fila]"));
    var chips = Array.prototype.slice.call(raiz.querySelectorAll("[data-hor-chip]"));
    var panel = raiz.querySelector("[data-hor-panel]");
    var todos = raiz.querySelector("[data-hor-todos]");
    var btnAplicar = raiz.querySelector("[data-hor-aplicar]");
    var btnLimpiar = raiz.querySelector("[data-hor-limpiar]");
    var errPanel = raiz.querySelector("[data-hor-panel-error]");
    var panelA = raiz.querySelector('[data-hor-panel-hora="a"]');
    var panelC = raiz.querySelector('[data-hor-panel-hora="c"]');
    var seleccion = [];
    var destacar = [];

    function esFijo(i) {
      return filas[i].classList.contains("is-fijo");
    }

    function campo(i, cual) {
      return filas[i].querySelector('[data-hor-hora="' + cual + '"]');
    }

    function fmt(m) {
      return (
        String(Math.floor(m / 60)).padStart(2, "0") + ":" + String(m % 60).padStart(2, "0")
      );
    }

    /* Acepta 1830, 18:30, 18, 6pm, 6:30 p. m. Devuelve minutos o null. */
    function leer(texto) {
      var s = (texto || "").trim().toLowerCase().replace(/\s|\./g, "");
      var pm = /pm$/.test(s);
      var am = /am$/.test(s);
      s = s.replace(/[ap]m?$/, "");
      var m = s.match(/^(\d{1,2})(?::?(\d{2}))?$/);
      if (!m) return null;
      var h = parseInt(m[1], 10);
      var min = m[2] ? parseInt(m[2], 10) : 0;
      if (pm && h < 12) h += 12;
      if (am && h === 12) h = 0;
      if (h > 23 || min > 59) return null;
      return h * 60 + min;
    }

    /* Mensaje de error de una hora escrita, o "" si es valida o esta vacia. */
    function errorDeHora(texto) {
      if (!texto.trim()) return "";
      var m = leer(texto);
      if (m === null) return "Escribe la hora como 1830 o 18:30.";
      if (m < MIN || m > MAX || m % PASO) {
        return "Las horas van de 00:00 a 23:30, de 30 en 30 minutos.";
      }
      return "";
    }

    function minutosDe(input) {
      var v = input.value.trim();
      if (!v || errorDeHora(v)) return null;
      return leer(v);
    }

    function nombreDia(i) {
      return filas[i].querySelector(".mc-hor-pick span").textContent.trim();
    }

    function duracion(min) {
      var h = Math.round(min / 6) / 10;
      return String(h).replace(".", ",") + " h";
    }

    function unir(partes) {
      if (partes.length < 2) return partes.join("");
      return partes.slice(0, -1).join(", ") + " y " + partes[partes.length - 1];
    }

    function plural(n, uno, varios) {
      return n + " " + (n === 1 ? uno : varios);
    }

    function normalizar(input) {
      var v = input.value.trim();
      if (!v) {
        input.value = "";
        return;
      }
      var m = leer(v);
      if (m !== null && !errorDeHora(v)) input.value = fmt(m);
    }

    function fijarHora(input, minutos) {
      input.value = fmt(minutos);
      input.dispatchEvent(new Event("input", { bubbles: true }));
      input.dispatchEvent(new Event("change", { bubbles: true }));
    }

    function alternar(i) {
      if (esFijo(i)) return;
      var pos = seleccion.indexOf(i);
      if (pos < 0) seleccion.push(i);
      else seleccion.splice(pos, 1);

      // Con un solo dia elegido se cargan sus horas para ajustarlas.
      if (seleccion.length === 1) {
        panelA.value = campo(seleccion[0], "a").value;
        panelC.value = campo(seleccion[0], "c").value;
      }
      pintar();
    }

    function elegibles() {
      return filas
        .map(function (_, i) {
          return i;
        })
        .filter(function (i) {
          return !esFijo(i);
        });
    }

    function pintar() {
      var abiertos = 0;
      var total = 0;
      var cambios = 0;

      filas.forEach(function (fila, i) {
        var campos = fila.querySelectorAll("[data-mc-original]");
        var modificada = Array.prototype.some.call(campos, function (c) {
          return c.value !== c.dataset.mcOriginal;
        });
        var on = seleccion.indexOf(i) >= 0;

        fila.classList.toggle("is-sel", on);
        fila.classList.toggle("is-mod", modificada);
        fila.classList.toggle("is-destacada", destacar.indexOf(i) >= 0);
        var pick = fila.querySelector("[data-hor-pick]");
        if (pick) pick.checked = on;

        if (modificada) cambios++;

        if (!esFijo(i)) {
          var ca = campo(i, "a");
          var cc = campo(i, "c");
          var errA = errorDeHora(ca.value);
          var errC = errorDeHora(cc.value);
          var ma = minutosDe(ca);
          var mc = minutosDe(cc);
          var valido = ma !== null && mc !== null && mc > ma;
          var orden = ma !== null && mc !== null && mc <= ma;

          ca.classList.toggle("is-invalid", !!errA);
          cc.classList.toggle("is-invalid", !!errC || orden);
          fila.querySelector("[data-hor-dur]").textContent = valido ? duracion(mc - ma) : "";
          fila.querySelector("[data-hor-error]").textContent =
            errA || errC || (orden ? "La hora de cierre debe ser posterior a la hora de apertura." : "");

          if (valido) {
            abiertos++;
            total += mc - ma;
          }
        }
      });

      raiz.querySelector("[data-hor-dias]").textContent = abiertos;
      raiz.querySelector("[data-hor-horas]").textContent = duracion(total);
      var sucio = raiz.querySelector("[data-hor-sucio]");
      sucio.hidden = cambios === 0;
      sucio.textContent =
        cambios === 1 ? "1 dia modificado sin guardar" : cambios + " dias modificados sin guardar";

      chips.forEach(function (chip) {
        var i = parseInt(chip.dataset.horChip, 10);
        chip.setAttribute("aria-pressed", String(seleccion.indexOf(i) >= 0));
      });

      var posibles = elegibles();
      var todosOn =
        posibles.length > 0 &&
        posibles.every(function (i) {
          return seleccion.indexOf(i) >= 0;
        });
      todos.checked = todosOn;
      todos.indeterminate = seleccion.length > 0 && !todosOn;

      var n = seleccion.length;
      panel.classList.toggle("is-sin-seleccion", n === 0);
      raiz.querySelector("[data-hor-conteo]").textContent = n
        ? plural(n, "dia seleccionado", "dias seleccionados")
        : "";
      btnLimpiar.disabled = n === 0;
      raiz.querySelector("[data-hor-mtxt]").textContent = n
        ? plural(n, "dia elegido", "dias elegidos")
        : "Elige dias en la tabla";

      // Sin dias elegidos el horario del panel no se puede tocar.
      panelA.disabled = n === 0;
      panelC.disabled = n === 0;

      var errPa = errorDeHora(panelA.value);
      var errPc = errorDeHora(panelC.value);
      var pa = minutosDe(panelA);
      var pc = minutosDe(panelC);
      var mensaje = errPa || errPc;
      if (!mensaje && pa !== null && pc !== null && pc <= pa) {
        mensaje = "El cierre debe ser posterior a la apertura.";
      }
      panelA.classList.toggle("is-invalid", !!errPa);
      panelC.classList.toggle("is-invalid", !!errPc || (!errPa && !errPc && !!mensaje));
      errPanel.textContent = mensaje;
      btnAplicar.disabled = !(n > 0 && pa !== null && pc !== null && !mensaje);
      btnAplicar.textContent = "Aplicar a " + plural(n, "dia", "dias");
    }

    function cerrarHoja() {
      panel.classList.remove("is-abierto");
    }

    function aplicar() {
      var pa = minutosDe(panelA);
      var pc = minutosDe(panelC);
      if (!seleccion.length || pa === null || pc === null || pc <= pa) return;

      var dias = seleccion.slice().sort(function (x, y) {
        return x - y;
      });
      dias.forEach(function (i) {
        fijarHora(campo(i, "a"), pa);
        fijarHora(campo(i, "c"), pc);
      });

      destacar = dias;
      seleccion = [];
      cerrarHoja();
      pintar();
      setTimeout(function () {
        destacar = [];
        pintar();
      }, 1800);

      if (window.mc && window.mc.avisar) {
        window.mc.avisar(
          "Horario aplicado a " + unir(dias.map(nombreDia)).toLowerCase() + ". Falta guardar.",
          3500,
        );
      }
    }

    // --- eventos ---------------------------------------------------
    raiz.addEventListener("input", function (e) {
      if (e.target.matches("[data-hor-hora],[data-hor-panel-hora]")) pintar();
    });

    raiz.addEventListener("change", function (e) {
      var t = e.target;
      if (t === todos) {
        seleccion = t.checked ? elegibles() : [];
        pintar();
        return;
      }
      if (t.matches("[data-hor-pick]")) {
        alternar(parseInt(t.closest("[data-hor-fila]").dataset.horFila, 10));
        return;
      }
      if (t.matches("[data-hor-hora],[data-hor-panel-hora]")) {
        normalizar(t);
        pintar();
      }
    });

    // Enter en un campo de hora: aplica (panel) o confirma el valor (fila) sin enviar el formulario.
    raiz.addEventListener("keydown", function (e) {
      if (e.key !== "Enter" || !e.target.matches("[data-hor-hora],[data-hor-panel-hora]")) return;
      e.preventDefault();
      normalizar(e.target);
      pintar();
      if (e.target.matches("[data-hor-panel-hora]") && !btnAplicar.disabled) aplicar();
    });

    raiz.addEventListener("click", function (e) {
      var chip = e.target.closest("[data-hor-chip]");
      if (chip) {
        alternar(parseInt(chip.dataset.horChip, 10));
        return;
      }

      var atajo = e.target.closest("[data-hor-atajo]");
      if (atajo) {
        var tipo = atajo.dataset.horAtajo;
        var idx = tipo === "lv" ? [0, 1, 2, 3, 4] : tipo === "fs" ? [5, 6] : [0, 1, 2, 3, 4, 5, 6];
        seleccion = idx.filter(function (i) {
          return i < filas.length && !esFijo(i);
        });
        if (seleccion.length === 1) {
          var unico = seleccion[0];
          seleccion = [];
          alternar(unico);
          return;
        }
        pintar();
        return;
      }

      if (e.target.closest("[data-hor-limpiar]")) {
        seleccion = [];
        pintar();
        return;
      }
      if (e.target.closest("[data-hor-aplicar]")) {
        aplicar();
        return;
      }
      if (e.target.closest("[data-hor-abrir]")) {
        panel.classList.add("is-abierto");
        return;
      }
      if (e.target.closest("[data-hor-cerrar]")) {
        cerrarHoja();
        return;
      }

      // Clic en la fila (fuera de controles): alterna su seleccion.
      var fila = e.target.closest("[data-hor-fila]");
      if (fila && !e.target.closest("input,a,label,button,select")) {
        alternar(parseInt(fila.dataset.horFila, 10));
      }
    });

    // No se envia el formulario con horas invalidas escritas a mano.
    if (form) {
      form.addEventListener(
        "submit",
        function (e) {
          var malo = form.querySelector(".mc-hor-hora.is-invalid");
          if (!malo) return;
          e.preventDefault();
          e.stopImmediatePropagation();
          malo.focus();
          if (window.mc && window.mc.avisar) {
            window.mc.avisar("Corrige las horas marcadas en rojo antes de guardar.", 3500);
          }
        },
        true,
      );
    }

    pintar();
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", iniciar);
  } else {
    iniciar();
  }
})();
