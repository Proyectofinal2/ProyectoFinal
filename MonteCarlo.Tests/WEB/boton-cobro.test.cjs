const { test } = require("node:test");
const assert = require("node:assert/strict");
const { readFileSync } = require("node:fs");
const { randomUUID } = require("node:crypto");
const vm = require("node:vm");

// DOM mínimo del ticket. Se ejecutan los manejadores reales de site.js y se
// captura el JSON de fetch sin iniciar el servidor ni registrar una venta.
function elemento() {
  const hijos = new Map();
  const eventos = new Map();
  return {
    dataset: {},
    classList: { toggle() {}, remove() {} },
    appendChild() {},
    querySelector(selector) {
      if (!hijos.has(selector)) hijos.set(selector, elemento());
      return hijos.get(selector);
    },
    addEventListener(tipo, manejar) { eventos.set(tipo, manejar); },
    emitir(tipo, evento) { return eventos.get(tipo)(evento); },
  };
}

function abrirCaja(precio, tasa) {
  const raiz = elemento();
  raiz.dataset.cuentaTotal = "0";
  const campo = nombre => raiz.querySelector("[data-mc-caja-" + nombre + "]");
  const producto = raiz.querySelector('[data-mc-caja-tile][data-id-producto="1"]');
  producto.dataset = { idProducto: "1", precio: String(precio), nombre: "Chifrijo" };
  raiz.querySelectorAll = selector => selector === "[data-mc-caja-tile]" ? [producto] : [];
  const iva = raiz.querySelector("[data-caja-iva]");
  iva.dataset.tasa = tasa;
  const consultar = raiz.querySelector;
  raiz.querySelector = selector => selector === "[data-caja-iva]" && tasa === null
    ? null : consultar(selector);
  const metodo = campo("pagos").querySelector("input:checked");
  metodo.value = "1";
  metodo.dataset.nombre = "Efectivo";

  const document = elemento();
  document.querySelector = selector => selector === "[data-mc-caja-pagina]" ? raiz : null;
  document.querySelectorAll = () => [];
  document.getElementById = () => null;
  document.createElement = elemento;
  const solicitudes = [];
  const contexto = vm.createContext({
    document, window: {}, crypto: { randomUUID },
    async fetch(url, opciones) {
      assert.equal(url, "/admin/ventas/para-llevar");
      solicitudes.push(JSON.parse(opciones.body));
      return { headers: { get: () => "application/json" }, json: () => ({ success: false }) };
    },
  });
  for (const nombre of ["mc-util.js", "site.js"]) {
    const ruta = require.resolve("../../MonteCarlo.WEB/wwwroot/js/" + nombre);
    vm.runInContext(readFileSync(ruta, "utf8"), contexto, { filename: nombre });
  }
  document.emitir("DOMContentLoaded");
  return {
    boton: campo("confirmar"), resumen: campo("total-quedara"), solicitudes,
    cambiar(accion) {
      const selector = "[data-mc-caja-tile-" + accion + "]";
      const control = { closest: () => producto };
      raiz.emitir("click", { target: { closest: s => s === selector ? control : null } });
    },
  };
}

const formato = new Intl.NumberFormat("es-CR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

async function verificarCobro(caja, esperado) {
  const importe = "₡" + formato.format(esperado);
  assert.equal(caja.resumen.textContent, importe);
  assert.equal(caja.boton.textContent, "Cobrar " + importe + " con Efectivo");
  assert.equal(caja.boton.disabled, false);
  await caja.boton.emitir("click");
  assert.equal(caja.solicitudes.at(-1).totalEsperado, esperado);
}

test("El botón sigue al carrito al agregar, aumentar, reducir y eliminar el último producto", async () => {
  const caja = abrirCaja(4500, "13");
  caja.cambiar("mas");
  await verificarCobro(caja, 5085);
  caja.cambiar("mas");
  await verificarCobro(caja, 10170);
  caja.cambiar("menos");
  await verificarCobro(caja, 5085);
  caja.cambiar("menos");
  assert.equal(caja.resumen.textContent, "₡0,00");
  assert.equal(caja.boton.textContent, "Cobrar");
  assert.equal(caja.boton.disabled, true);
  await caja.boton.emitir("click");
  assert.equal(caja.solicitudes.length, 3);
});

for (const [caso, precio, tasa, esperado] of [
  ["IVA cero", 4500, "0", 4500],
  ["IVA fraccionario", 4500, "7.25", 4826.25],
  ["precio con centavos", 19.95, "13", 22.54],
  ["sin desglose de IVA", 49.99, null, 49.99],
]) {
  test("Botón, resumen y TotalEsperado coinciden con " + caso, async () => {
    const caja = abrirCaja(precio, tasa);
    caja.cambiar("mas");
    await verificarCobro(caja, esperado);
  });
}
