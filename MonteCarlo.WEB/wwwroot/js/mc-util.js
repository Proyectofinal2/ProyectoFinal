/* ------------------------------------------------------------
   Utilidades compartidas del panel (window.mc). Se carga antes que
   site.js y mesas.js para que formato de colones, avisos y llamadas
   JSON salgan de un solo lugar.
   ------------------------------------------------------------ */
(function () {
  "use strict";

  // Igual que Colones.Formato (C#): miles con separador y hasta 2 decimales.
  var formatoColones = new Intl.NumberFormat("es-CR", { maximumFractionDigits: 2 });

  function colones(monto) {
    return "₡" + formatoColones.format(monto);
  }

  /** «N min» si es menor de una hora; si no «H h» o «H h MM» (igual que CuentaViewModel.Duracion). */
  function duracion(minutos) {
    var total = Math.max(0, Math.floor(minutos));
    if (total < 60) {
      return total + " min";
    }
    var horas = Math.floor(total / 60);
    var resto = total % 60;
    return resto === 0 ? horas + " h" : horas + " h " + String(resto).padStart(2, "0");
  }

  function tokenAntiforgery() {
    var campo = document.querySelector('input[name="__RequestVerificationToken"]');
    return campo ? campo.value : "";
  }

  function avisar(mensaje, milisegundos) {
    if (!mensaje) {
      return;
    }
    var aviso = document.createElement("div");
    aviso.className = "mc-toast";
    aviso.setAttribute("role", "status");
    aviso.textContent = mensaje;
    document.body.appendChild(aviso);
    setTimeout(function () {
      aviso.remove();
    }, milisegundos || 2500);
  }

  /**
   * Llama a una accion JSON del WEB con antiforgery. Siempre resuelve con { success, message, ... }:
   * sesion vencida (redireccion al login), respuesta que no es JSON o sin conexion se convierten en un
   * mensaje para el usuario en lugar de una excepcion.
   */
  async function enviarJson(url, cuerpo, metodo) {
    var verbo = metodo || "POST";
    try {
      var respuesta = await fetch(url, {
        method: verbo,
        credentials: "same-origin",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: tokenAntiforgery(),
        },
        body: cuerpo ? JSON.stringify(cuerpo) : verbo === "POST" ? "{}" : undefined,
      });
      if (respuesta.redirected) {
        window.location.href = respuesta.url;
        return { success: false, message: "Tu sesión venció. Inicia sesión de nuevo." };
      }
      if (!(respuesta.headers.get("content-type") || "").includes("json")) {
        return {
          success: false,
          message:
            respuesta.status === 400
              ? "No se pudo validar la solicitud. Recarga la página e intenta de nuevo."
              : "No se pudo completar la operación. Intenta de nuevo.",
        };
      }
      return await respuesta.json();
    } catch (error) {
      return {
        success: false,
        message: "No se pudo conectar con el servidor. Intenta de nuevo.",
      };
    }
  }

  window.mc = {
    colones: colones,
    duracion: duracion,
    avisar: avisar,
    enviarJson: enviarJson,
  };
})();
