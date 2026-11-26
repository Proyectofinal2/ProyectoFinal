const { test } = require("node:test");
const assert = require("node:assert/strict");
global.window = {};
require("../../MonteCarlo.WEB/wwwroot/js/mc-util.js");
const { centesimas, estimarVenta } = window.mc;

test("El formato es-CR conserva los céntimos en importes expresados en colones", () => {
  const formato = new Intl.NumberFormat("es-CR", { maximumFractionDigits: 2 });
  for (const monto of [0.01, 4500.25, 10170]) {
    assert.equal(window.mc.colones(monto), "₡" + formato.format(monto));
  }
});

for (const [subtotal, tasa, iva, total] of [
  [10000, "13.00", 1300, 11300],
  [0.50, "1", 0.01, 0.51],
  [0.49, "1", 0, 0.49],
  [1, "7.25", 0.07, 1.07],
  [100, "0", 0, 100],
  [100, null, null, 100],
  [0, "13", 0, 0],
]) {
  test("Subtotal " + subtotal + ", tasa " + tasa, () => {
    assert.deepEqual(estimarVenta(centesimas(subtotal), tasa), { subtotal, iva, total });
  });
}

test("Dos pedidos y una anulación parcial se redondean sobre el saldo activo", () => {
  const precio = centesimas(0.04);
  assert.equal(estimarVenta(2 * precio, "13").total, 0.09);
  assert.equal(estimarVenta(precio, "13").total, 0.05);
  assert.equal(estimarVenta(0, "13").total, 0);
});
