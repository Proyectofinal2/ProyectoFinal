(function () {
    const esAdministradorGeneral = document.getElementById("mc-mesas-root").dataset.esAdministradorGeneral === "true";
    const RADIO_ADYACENCIA = 200;

    // HU-CON-002: "Atender mesas" es el modo inicial para todos los roles.
    let modo = "asignar";
    let vista = cargarVista();
    let filtro = "todas";
    let mesas = [];
    let seleccionadaId = null;
    let recomendadas = [];

    // Utilidades compartidas (mc-util.js): una sola forma de formatear colones, avisar y llamar al servidor.
    const { colones: formatoColones, duracion: formatoDuracion, avisar: toast } = window.mc;
    const llamar = (url, metodo, cuerpo) => window.mc.enviarJson(url, cuerpo, metodo);

    function cargarVista() {
        try { return localStorage.getItem("mc.mesas.vista") === "plano" ? "plano" : "tarjetas"; } catch { return "tarjetas"; }
    }

    function hhmm(t) { return (t || "").slice(0, 5); }

    // "31 oct, 14:05": cuando se sento el cliente, para las cuentas antiguas (donde "sentados hace 103 h" no ayuda).
    function fechaCorta(iso) {
        const d = new Date(iso);
        return isNaN(d) ? "" : d.toLocaleString("es-CR", { day: "numeric", month: "short", hour: "2-digit", minute: "2-digit", hour12: false });
    }

    // Estado visual compartido por Tarjetas, Plano y panel (HU-CON-002 esc. 7 y 10).
    function estadoVisual(mesa) {
        const r = mesa.reservaActual, c = mesa.cuenta;
        const cliente = r ? `${r.cliente} · ${r.personas} pers.` : "";
        const base = { excedeTurno: 0, pie: "", antigua: false };
        if (mesa.estado === "Deshabilitada")
            return { ...base, clave: "mantenimiento", familia: "gris", badge: "Mantenimiento", principal: "Mantenimiento", secundaria: "No disponible" };
        if (mesa.estado === "Ocupada") {
            if (mesa.esperandoLlegada && r) {
                return { ...base, clave: "esperando", familia: "ambar", badge: "Esperando llegada", badgeCorto: "Esperando", principal: "Esperando", secundaria: cliente,
                    pie: `Reserva de las ${hhmm(r.horaReserva)} · ${r.minutosSentados} min de espera`, hora: hhmm(r.horaReserva) };
            }
            const sentados = r ? r.minutosSentados : 0;
            const exceso = Math.max(0, sentados - mesa.duracionReservaMinutos);
            const sentadosTxt = r ? "Sentados hace " + formatoDuracion(sentados) : "";
            const nombre = r ? r.cliente : "";
            if (c) {
                const secundaria = `${c.productos} producto${c.productos === 1 ? "" : "s"} · ${nombre}`;
                if (c.antigua) {
                    // Ya no hay nadie sentado: queda una cuenta sin cobrar de un dia anterior (o pasado el tiempo de uso mas la tolerancia).
                    // Se muestra como "por cobrar", no como "ocupada", y se resalta para que se resuelva.
                    const desde = r ? fechaCorta(r.ocupadaDesde) : "";
                    const porCobrar = c.productos > 0;
                    return { clave: "cuenta", familia: "alerta", antigua: true, excedeTurno: 0, sentados,
                        badge: porCobrar ? "Cuenta pendiente de cobro" : "Cuenta pendiente de cierre", badgeCorto: porCobrar ? "Por cobrar" : "Por cerrar",
                        principal: formatoColones(c.total), secundaria, pie: desde ? `Abierta desde el ${desde}` : "" };
                }
                return { clave: "cuenta", familia: "rojo", badge: "Ocupada · con cuenta", badgeCorto: "Con cuenta", principal: formatoColones(c.total),
                    secundaria, pie: sentadosTxt, excedeTurno: exceso, sentados, antigua: false };
            }
            return { clave: "sinpedido", familia: "rojo", badge: "Ocupada · sin pedido", badgeCorto: "Sin pedido", principal: "Sin pedido", secundaria: cliente, pie: sentadosTxt, excedeTurno: exceso, sentados };
        }
        if (mesa.esProxima) {
            const pr = mesa.proximaReserva;
            return { ...base, clave: "proxima", familia: "ambar", badge: "Reserva próxima", principal: `En ${mesa.minutosProximaReserva} min`,
                secundaria: pr ? `${pr.cliente} · ${pr.personas} pers.` : "", pie: pr ? `Reserva de las ${hhmm(pr.horaReserva)}` : "" };
        }
        return { ...base, clave: "libre", familia: "verde", badge: "Libre", principal: "Libre", secundaria: `Capacidad ${mesa.capacidad} personas` };
    }

    function sizeFor(capacidad) { return 64 + Math.min(capacidad, 10) * 9; }

    const PALETA = {
        verde: { bg: "#CFE3C7", border: "#5C9A4A", chair: "#B9D7AD" },
        rojo: { bg: "#E7B3AE", border: "#C0392B", chair: "#DFA09A" },
        ambar: { bg: "#F6DFA3", border: "#C98A1E", chair: "#F0CE86" },
        gris: { bg: "#D8D5CE", border: "#ADA99F", chair: "#CFCCC4" },
        alerta: { bg: "#F3C9C4", border: "#8E1B10", chair: "#E8AFA8" }
    };

    // Modo "Editar salon": colores segun el estado persistido, sin cuenta ni reserva.
    function colorFor(mesa) {
        if (mesa.estado === "Deshabilitada") return PALETA.gris;
        if (mesa.estado === "Ocupada") return PALETA.rojo;
        return mesa.esProxima ? PALETA.ambar : PALETA.verde;
    }

    function sonAdyacentes(a, b) {
        const sa = sizeFor(a.capacidad), sb = sizeFor(b.capacidad);
        const dx = (a.posX + sa / 2) - (b.posX + sb / 2);
        const dy = (a.posY + sa / 2) - (b.posY + sb / 2);
        return Math.hypot(dx, dy) <= RADIO_ADYACENCIA;
    }

    async function cargarMesas() {
        const r = await llamar(`/admin/mesas/listado?incluirDeshabilitadas=true`, "GET");
        mesas = r.success ? (r.data || []) : [];
        if (!r.success) toast(r.message || "No se pudieron cargar las mesas.");
        renderizar();
        renderizarAvisoLiberar();
        renderizarEstadoVacio();
    }

    // HU-MES-002, escenario 2: sin mesas registradas, mensaje informativo.
    function renderizarEstadoVacio() {
        const vacio = document.getElementById("mc-canvas-vacio");
        vacio.classList.toggle("d-none", mesas.length > 0);
    }

    function renderizarAvisoLiberar() {
        const aviso = document.getElementById("mc-aviso-liberar");
        if (modo !== "asignar") { aviso.classList.add("d-none"); return; }
        const porLiberar = mesas.filter(m => m.minutosHastaSiguienteReserva != null);
        if (porLiberar.length === 0) { aviso.classList.add("d-none"); return; }
        aviso.textContent = "⚠ " + (porLiberar.length === 1
            ? "1 mesa debe cobrarse o liberarse pronto para su próxima reserva."
            : porLiberar.length + " mesas deben cobrarse o liberarse pronto para su próxima reserva.");
        aviso.classList.remove("d-none");
    }

    // Las posiciones se guardan en pixeles (la mesa 5 de la base inicial esta en x=952), asi que en una pantalla
    // angosta quedaban fuera del lienzo y no se veian. Se reduce el plano para que quepan todas; en el modo
    // Editar salon el arrastre divide el movimiento entre esta escala.
    let escalaPlano = 1;

    function ajustarEscala(layer, svg) {
        const canvas = document.getElementById("mc-canvas");
        let ancho = 0, alto = 0;
        mesas.forEach(m => {
            const tam = sizeFor(m.capacidad);
            ancho = Math.max(ancho, m.posX + tam + 30);
            alto = Math.max(alto, m.posY + tam + 30);
        });
        const cabe = canvas.clientWidth > 0 && ancho > 0;
        escalaPlano = cabe ? Math.min(1, canvas.clientWidth / ancho, canvas.clientHeight / alto) : 1;
        for (const el of [layer, svg]) {
            el.style.transformOrigin = "0 0";
            el.style.transform = escalaPlano < 1 ? `scale(${escalaPlano})` : "";
        }
        // El SVG ocupa todo el lienzo: al reducirlo se agranda para seguir cubriendolo.
        svg.style.width = svg.style.height = escalaPlano < 1 ? `${100 / escalaPlano}%` : "100%";
    }

    function renderizarPlano() {
        const layer = document.getElementById("mc-mesas-layer");
        const svg = document.getElementById("mc-svg-lineas");
        layer.innerHTML = "";
        svg.innerHTML = "";
        ajustarEscala(layer, svg);

        const idsAdyacentes = new Set();
        if (modo === "editar") {
            for (let i = 0; i < mesas.length; i++) {
                for (let j = i + 1; j < mesas.length; j++) {
                    if (!sonAdyacentes(mesas[i], mesas[j])) continue;
                    const a = mesas[i], b = mesas[j];
                    idsAdyacentes.add(a.idMesa); idsAdyacentes.add(b.idMesa);
                    const sa = sizeFor(a.capacidad), sb = sizeFor(b.capacidad);
                    const line = document.createElementNS("http://www.w3.org/2000/svg", "line");
                    line.setAttribute("x1", a.posX + sa / 2); line.setAttribute("y1", a.posY + sa / 2);
                    line.setAttribute("x2", b.posX + sb / 2); line.setAttribute("y2", b.posY + sb / 2);
                    line.setAttribute("stroke", "#D97757"); line.setAttribute("stroke-width", "2"); line.setAttribute("stroke-dasharray", "6 6");
                    svg.appendChild(line);
                }
            }
        }

        mesas.forEach(mesa => {
            const size = sizeFor(mesa.capacidad);
            const vis = modo === "asignar" ? estadoVisual(mesa) : null;
            const color = vis ? PALETA[vis.familia] : colorFor(mesa);
            const esRecomendada = recomendadas.includes(mesa.idMesa);
            const cx = mesa.posX + size / 2, cy = mesa.posY + size / 2;
            const radioSillas = size / 2 + 13;

            for (let i = 0; i < mesa.capacidad; i++) {
                const angulo = (2 * Math.PI * i) / mesa.capacidad - Math.PI / 2;
                const chair = document.createElement("div");
                chair.className = "mc-chair";
                chair.style.left = (cx + Math.cos(angulo) * radioSillas - 7) + "px";
                chair.style.top = (cy + Math.sin(angulo) * radioSillas - 7) + "px";
                chair.style.background = color.chair;
                chair.style.border = "1px solid " + color.border;
                layer.appendChild(chair);
            }

            const div = document.createElement("div");
            div.className = "mc-mesa" + (esRecomendada ? " is-recomendada" : "");
            div.style.left = mesa.posX + "px";
            div.style.top = mesa.posY + "px";
            div.style.width = size + "px";
            div.style.height = size + "px";
            div.style.background = color.bg;
            div.style.border = (esRecomendada ? "3px" : "2px") + (vis && vis.clave === "sinpedido" ? " dashed " : " solid ") + color.border;
            div.style.cursor = modo === "editar" ? (mesa.enUnionActiva ? "not-allowed" : "grab") : "pointer";
            div.style.opacity = (vis && vis.clave === "mantenimiento") ? "0.6" : "1";
            if (mesa.idMesa === seleccionadaId) div.style.outline = "3px solid #3B6FB0";
            else if (esRecomendada) div.style.outline = "3px solid #2F6B4F";
            else if (modo === "editar" && idsAdyacentes.has(mesa.idMesa)) div.style.outline = "2px dashed #6A93C7";

            let lineas = [mesa.capacidad + "p"];
            if (vis) {
                // Funciones: solo se calcula la etiqueta del estado que corresponde (no todas a la vez).
                lineas = {
                    libre: () => ["Libre"],
                    mantenimiento: () => ["Manten."],
                    proxima: () => ["En " + mesa.minutosProximaReserva + " min"],
                    esperando: () => ["Espera", vis.hora],
                    sinpedido: () => ["Sin pedido", formatoDuracion(vis.sentados)],
                    cuenta: () => [formatoColones(mesa.cuenta.total), mesa.cuenta.productos + " prod."]
                }[vis.clave]();
            }

            div.innerHTML = `<div class="num">M${mesa.numeroMesa}</div>` + lineas.map(l => `<div class="etq">${l}</div>`).join("");
            if (vis) {
                div.setAttribute("role", "button");
                div.tabIndex = 0;
                div.setAttribute("aria-label", `Mesa ${mesa.numeroMesa}, ${vis.badge}`);
                div.addEventListener("keydown", e => { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); seleccionar(mesa.idMesa); } });
            }
            div.addEventListener("mousedown", e => iniciarInteraccion(e, mesa));
            layer.appendChild(div);

            if (vis && (vis.antigua || vis.excedeTurno > 0)) {
                // Una cuenta antigua es mas grave que pasarse del turno: la etiqueta la reemplaza (texto, no solo color).
                const exceso = document.createElement("div");
                exceso.className = "mc-mesa-tag " + (vis.antigua ? "is-antigua" : "is-exceso");
                exceso.style.left = (mesa.posX + size / 2 - (vis.antigua ? 38 : 22)) + "px";
                exceso.style.top = (mesa.posY - 24) + "px";
                exceso.textContent = vis.antigua ? "⚠ Por cobrar" : "+" + vis.excedeTurno + " min";
                layer.appendChild(exceso);
            }

            if (esRecomendada) {
                const tag = document.createElement("div");
                tag.className = "mc-mesa-tag";
                tag.style.left = mesa.posX + "px";
                tag.style.top = (mesa.posY - 22) + "px";
                tag.style.background = "#2F6B4F";
                tag.textContent = "★ Recomendada";
                layer.appendChild(tag);
            }
            if (mesa.enUnionActiva && modo === "editar") {
                const badge = document.createElement("div");
                badge.className = "mc-mesa-badge-icon";
                badge.style.left = (mesa.posX + size - 10) + "px";
                badge.style.top = (mesa.posY - 4) + "px";
                badge.style.background = "#2F6B4F";
                badge.style.color = "#fff";
                badge.textContent = "🔗";
                layer.appendChild(badge);
            }
            if (mesa.minutosHastaSiguienteReserva != null) {
                const critica = mesa.minutosHastaSiguienteReserva <= 15;
                const badge = document.createElement("div");
                badge.className = "mc-mesa-badge-icon is-reloj" + (critica ? " critica" : "");
                badge.style.left = (mesa.posX - 6) + "px";
                badge.style.top = (mesa.posY - 6) + "px";
                badge.style.background = critica ? "#C0392B" : "#C98A1E";
                badge.title = "Proxima reserva en " + mesa.minutosHastaSiguienteReserva + " min";
                badge.textContent = "⏰";
                layer.appendChild(badge);
            }
        });
    }

    function renderizar() {
        if (modo === "asignar" && vista === "tarjetas") renderizarTarjetas(); else renderizarPlano();
        renderizarResumen();
    }

    const FILTROS = [
        ["todas", "Todas", () => true],
        ["concuenta", "Con cuenta", v => v.clave === "cuenta"],
        ["sinpedido", "Sin pedido", v => v.clave === "sinpedido"],
        ["antiguas", "Antiguas", v => v.antigua],
        ["esperando", "Esperando", v => v.clave === "esperando" || v.clave === "proxima"],
        ["libres", "Libres", v => v.clave === "libre"]
    ];

    function renderizarResumen() {
        const el = document.getElementById("mc-resumen");
        if (modo !== "asignar") { document.getElementById("mc-aviso-antiguas").classList.add("d-none"); return; }
        const habilitadas = mesas.filter(m => m.estado !== "Deshabilitada");
        // Como en el prototipo, "ocupada" = sentados (con cuenta o sin pedido); esperando llegada no cuenta.
        // Una cuenta antigua tampoco: ya no hay nadie sentado, solo queda el cobro pendiente.
        const ocupadas = habilitadas.filter(m => { const v = estadoVisual(m); return (v.clave === "cuenta" && !v.antigua) || v.clave === "sinpedido"; }).length;
        // Conteos por reserva: una union de dos mesas es una sola cuenta.
        const cuentas = new Map(), sinPedido = new Set(), antiguas = new Set();
        mesas.forEach(m => {
            const v = estadoVisual(m);
            const id = m.reservaActual?.idReserva;
            if (v.clave === "cuenta" && id != null) cuentas.set(id, m.cuenta.total);
            if (v.clave === "sinpedido" && id != null) sinPedido.add(id);
            if (v.antigua && id != null) antiguas.add(id);
        });
        const total = [...cuentas.values()].reduce((a, b) => a + b, 0);
        el.innerHTML = `<span>Ocupadas ${ocupadas} de ${habilitadas.length}</span>`
            + `<span class="is-cuentas">Cuentas abiertas ${cuentas.size} · ${formatoColones(total)}</span>`
            + (sinPedido.size > 0 ? `<span class="is-sinpedido">${sinPedido.size} sin pedido</span>` : "")
            + (antiguas.size > 0 ? `<span class="is-antiguas">${antiguas.size} ${antiguas.size === 1 ? "cuenta por cobrar" : "cuentas por cobrar"}</span>` : "");
        renderizarAvisoAntiguas(antiguas, cuentas);
    }

    // Franja que pide resolver las cuentas antiguas (no bloquea la pantalla) y lleva directo al filtro «Antiguas».
    function renderizarAvisoAntiguas(antiguas, cuentas) {
        const aviso = document.getElementById("mc-aviso-antiguas");
        if (antiguas.size === 0) { aviso.classList.add("d-none"); return; }
        const n = antiguas.size;
        const total = [...antiguas].reduce((suma, id) => suma + (cuentas.get(id) || 0), 0);
        const texto = document.createElement("span");
        texto.textContent = `${n === 1 ? "1 cuenta antigua" : n + " cuentas antiguas"} sin cerrar (${formatoColones(total)}). `
            + "Cóbralas o, si no hubo consumo, libera la mesa.";
        const ver = document.createElement("button");
        ver.type = "button";
        ver.textContent = "Ver cuentas antiguas";
        ver.addEventListener("click", () => {
            filtro = "antiguas";
            vista = "tarjetas";
            try { localStorage.setItem("mc.mesas.vista", vista); } catch { /* sin almacenamiento */ }
            cerrarPanel(false);
            aplicarVista();
            renderizar();
        });
        aviso.replaceChildren(texto, ver);
        aviso.classList.remove("d-none");
    }

    function renderizarTarjetas() {
        const visuales = mesas.slice().sort((a, b) => a.numeroMesa - b.numeroMesa).map(m => ({ m, v: estadoVisual(m) }));
        const filtros = document.getElementById("mc-filtros");
        filtros.innerHTML = "";
        FILTROS.forEach(([clave, texto, pred]) => {
            const n = visuales.filter(x => pred(x.v)).length;
            const b = document.createElement("button");
            b.type = "button";
            b.className = "mc-filtro" + (filtro === clave ? " is-active" : "");
            b.setAttribute("aria-pressed", filtro === clave);
            b.textContent = `${texto} ${n}`;
            b.onclick = () => { filtro = clave; renderizarTarjetas(); };
            filtros.appendChild(b);
        });
        document.getElementById("mc-panel-vacio").classList.toggle("d-none", seleccionadaId != null);
        const pred = FILTROS.find(f => f[0] === filtro)[2];
        const cont = document.getElementById("mc-tarjetas");
        cont.innerHTML = "";
        visuales.filter(x => pred(x.v)).forEach(({ m, v }) => {
            const recomendada = recomendadas.includes(m.idMesa);
            const b = document.createElement("button");
            b.type = "button";
            b.className = `mc-tarjeta mc-fam-${v.familia} is-${v.clave}` + (m.idMesa === seleccionadaId ? " is-selected" : "") + (recomendada ? " is-recomendada" : "");
            b.setAttribute("aria-label", `Mesa ${m.numeroMesa}, ${v.badge}`);
            b.innerHTML = `<div class="mc-tarjeta__top"><span class="mc-tarjeta__num">Mesa ${m.numeroMesa}</span><span class="mc-tarjeta__badge"></span></div>`
                + `<div class="mc-tarjeta__principal"></div><div class="mc-tarjeta__sec"></div><div class="mc-tarjeta__pie"></div>`;
            // La tarjeta usa el rotulo corto para que no se parta en dos lineas; el panel y aria-label llevan el largo.
            b.querySelector(".mc-tarjeta__badge").textContent = recomendada ? "★ Recomendada" : (v.antigua ? "⚠ " : "") + (v.badgeCorto || v.badge);
            b.querySelector(".mc-tarjeta__principal").textContent = v.principal;
            const sec = b.querySelector(".mc-tarjeta__sec");
            sec.textContent = v.secundaria;
            sec.title = v.secundaria;
            const pie = b.querySelector(".mc-tarjeta__pie");
            pie.append(document.createTextNode(v.pie));
            pie.title = v.pie;
            if (v.excedeTurno > 0) {
                // En su propia linea, debajo del pie: junto a "Sentados hace..." se cortaba con puntos suspensivos.
                const ex = document.createElement("div");
                ex.className = "mc-tarjeta__exceso";
                ex.textContent = `+${v.excedeTurno} min del turno`;
                b.appendChild(ex);
            }
            b.onclick = () => seleccionar(m.idMesa);
            cont.appendChild(b);
        });
    }

    // ---------- Drag (modo Editar salon) ----------
    let arrastrando = null, inicioMouseX = 0, inicioMouseY = 0, inicioMesaX = 0, inicioMesaY = 0, seMovio = false;

    function iniciarInteraccion(e, mesa) {
        if (modo !== "editar" || mesa.enUnionActiva) {
            seleccionar(mesa.idMesa);
            return;
        }
        arrastrando = mesa;
        inicioMouseX = e.clientX; inicioMouseY = e.clientY;
        inicioMesaX = mesa.posX; inicioMesaY = mesa.posY;
        seMovio = false;
    }

    document.addEventListener("mousemove", e => {
        if (!arrastrando) return;
        const dx = (e.clientX - inicioMouseX) / escalaPlano, dy = (e.clientY - inicioMouseY) / escalaPlano;
        if (Math.abs(dx) + Math.abs(dy) > 3) seMovio = true;
        arrastrando.posX = Math.max(0, inicioMesaX + dx);
        arrastrando.posY = Math.max(0, inicioMesaY + dy);
        renderizar();
    });

    document.addEventListener("mouseup", async () => {
        if (!arrastrando) return;
        const mesa = arrastrando;
        const movio = seMovio;
        arrastrando = null;
        if (!movio) { seleccionar(mesa.idMesa); return; }
        const r = await llamar(`/admin/mesas/${mesa.idMesa}/posicion`, "PUT", { posX: Math.round(mesa.posX), posY: Math.round(mesa.posY) });
        if (!r.success) toast(r.message || "No se pudo actualizar la posicion.");
        await cargarMesas();
    });

    // ---------- Seleccion / panel ----------
    const estadoBadgeEstilo = {
        Disponible: { bg: "#E3F0DC", color: "#3F6B30" },
        Ocupada: { bg: "#F6DEDA", color: "#9C2D1F" },
        Proxima: { bg: "#FBEFD3", color: "#8A6D1F" },
        Deshabilitada: { bg: "#EAE8E2", color: "#6B6862" }
    };

    let personasSentar = 2, confirmandoNoShow = false;

    function seleccionar(idMesa) {
        seleccionadaId = seleccionadaId === idMesa ? null : idMesa;
        personasSentar = 2; confirmandoNoShow = false;
        renderizar();
        abrirPanel();
    }

    // Lo que el panel mostro la ultima vez: si el refresco trae otro estado o otra version de la cuenta, se vuelve a pintar.
    let firmaPanel = null;
    const firmaDe = mesa => estadoVisual(mesa).clave + "|" + (mesa.cuenta ? mesa.cuenta.version : "");

    // El refresco del tablero no pisa un panel con interaccion en curso (modal abierto, confirmacion en linea o foco dentro).
    function panelEnUso() {
        const panel = document.getElementById("mc-panel");
        return !!document.querySelector(".modal.show")
            || !!panel.querySelector(".mc-confirmar")
            || panel.contains(document.activeElement) && document.activeElement !== panel;
    }

    function refrescarPanelSiCambio() {
        const mesa = mesas.find(m => m.idMesa === seleccionadaId);
        if (!mesa || modo !== "asignar" || firmaDe(mesa) === firmaPanel || panelEnUso()) return;
        abrirPanel();
    }

    function abrirPanel() {
        const panel = document.getElementById("mc-panel");
        const mesa = mesas.find(m => m.idMesa === seleccionadaId);
        if (!mesa) { panel.classList.remove("is-open"); return; }
        firmaPanel = firmaDe(mesa);

        document.getElementById("mc-panel-numero").textContent = "M" + mesa.numeroMesa;

        const claveEstado = mesa.esProxima ? "Proxima" : mesa.estado;
        const estiloBadge = estadoBadgeEstilo[claveEstado] || estadoBadgeEstilo.Disponible;
        const badge = document.getElementById("mc-panel-badge");
        badge.textContent = modo === "asignar" ? estadoVisual(mesa).badge : (claveEstado === "Proxima" ? "Reserva proxima" : claveEstado);
        badge.className = "mc-panel-badge" + (modo === "asignar" ? " mc-fam-" + estadoVisual(mesa).familia : "");
        badge.style.background = modo === "asignar" ? "" : estiloBadge.bg;
        badge.style.color = modo === "asignar" ? "" : estiloBadge.color;

        const body = document.getElementById("mc-panel-body");

        if (modo === "editar") {
            body.innerHTML = `
                ${mesa.tieneReservaActiva ? '<p class="mc-panel-aviso">Esta mesa tiene una reserva activa: no se puede deshabilitar ni eliminar hasta que finalice o se cancele.</p>' : ""}
                <div class="d-flex align-items-center justify-content-between">
                    <span style="font-size:13px;color:#6B6862;">Capacidad</span>
                    <div class="mc-stepper">
                        <button type="button" id="p-dec-cap">&minus;</button>
                        <span id="p-cap">${mesa.capacidad}</span>
                        <button type="button" id="p-inc-cap">+</button>
                    </div>
                </div>
                <button type="button" class="mc-btn-mant" id="p-toggle-estado" ${mesa.tieneReservaActiva && mesa.estado !== "Deshabilitada" ? "disabled" : ""}>
                    ${mesa.estado === "Deshabilitada" ? "Habilitar mesa" : "Deshabilitar por mantenimiento"}
                </button>
                <button type="button" class="mc-btn-eliminar" id="p-eliminar" ${mesa.tieneReservaActiva ? "disabled" : ""}>Eliminar mesa</button>
            `;
            document.getElementById("p-dec-cap").onclick = () => cambiarCapacidad(mesa, -1);
            document.getElementById("p-inc-cap").onclick = () => cambiarCapacidad(mesa, 1);
            document.getElementById("p-toggle-estado").onclick = () => toggleEstado(mesa);
            document.getElementById("p-eliminar").onclick = () => eliminarMesa(mesa);
        } else {
            renderizarPanelAtender(mesa, body);
        }

        ubicarPanel();
        panel.classList.add("is-open");
    }

    // Contenido del panel en "Atender mesas" segun el estado visual (contrato de paridad 1.6).
    function renderizarPanelAtender(mesa, body) {
        const v = estadoVisual(mesa), r = mesa.reservaActual;
        if (v.clave === "mantenimiento") {
            body.innerHTML = `<p class="text-secondary small mb-0">Mesa en mantenimiento, no disponible para asignar.</p>`;
        } else if (v.clave === "proxima") {
            body.innerHTML = `<p class="mc-panel-aviso">Tiene una reserva confirmada en ${mesa.minutosProximaReserva} minutos. No se puede asignar a un walk-in.</p>`;
        } else if (v.clave === "esperando") {
            body.innerHTML = `
                <p class="mb-1"><strong></strong> · ${r.personas} personas</p>
                <p class="small text-secondary">Reserva de las ${hhmm(r.horaReserva)}. Lleva ${r.minutosSentados} min de espera.</p>
                <button type="button" class="btn btn-primary w-100" id="p-llego">Cliente llegó</button>
                <div id="p-noshow-zona" class="text-center mt-2"></div>`;
            body.querySelector("strong").textContent = r.cliente;
            document.getElementById("p-llego").onclick = () => clienteLlego(mesa, document.getElementById("p-llego"));
            renderizarNoShow(mesa);
        } else if (v.clave === "sinpedido" || v.clave === "cuenta") {
            cargarCuenta(mesa, body);
        } else {
            const max = Math.max(1, mesa.capacidad);
            personasSentar = Math.min(Math.max(personasSentar, 1), max);
            body.innerHTML = `
                <div class="d-flex align-items-center justify-content-between">
                    <span style="font-size:13px;color:#6B6862;">Personas a sentar</span>
                    <div class="mc-stepper">
                        <button type="button" id="p-dec-pers" aria-label="Menos personas">&minus;</button>
                        <span id="p-pers">${personasSentar}</span>
                        <button type="button" id="p-inc-pers" aria-label="Más personas">+</button>
                    </div>
                </div>
                <p class="small text-muted mb-0">Al sentar al cliente la mesa queda ocupada y el panel sigue abierto para tomar el primer pedido.</p>
                <button type="button" class="mc-btn-sentar" id="p-sentar">Sentar cliente</button>`;
            document.getElementById("p-dec-pers").onclick = () => { personasSentar = Math.max(1, personasSentar - 1); document.getElementById("p-pers").textContent = personasSentar; };
            document.getElementById("p-inc-pers").onclick = () => { personasSentar = Math.min(max, personasSentar + 1); document.getElementById("p-pers").textContent = personasSentar; };
            document.getElementById("p-sentar").onclick = () => sentarCliente(mesa.idMesa, personasSentar);
        }
    }

    function renderizarNoShow(mesa) {
        const zona = document.getElementById("p-noshow-zona");
        if (!zona) return;
        if (!confirmandoNoShow) {
            zona.innerHTML = `<button type="button" class="btn btn-link text-secondary" id="p-noshow">Marcar No Show</button>`;
            document.getElementById("p-noshow").onclick = () => { confirmandoNoShow = true; renderizarNoShow(mesa); };
            return;
        }
        zona.innerHTML = `
            <div class="mc-confirmar" role="alertdialog">
                <p class="mc-confirmar__texto">¿Marcar No Show? La mesa queda libre y la reserva cerrada.</p>
                <div class="mc-confirmar__acciones">
                    <button type="button" class="btn btn-sm btn-outline-secondary" id="p-noshow-no">Cancelar</button>
                    <button type="button" class="btn btn-sm btn-primary" id="p-noshow-si">Sí, No Show</button>
                </div>
            </div>`;
        document.getElementById("p-noshow-no").onclick = () => { confirmandoNoShow = false; renderizarNoShow(mesa); };
        document.getElementById("p-noshow-si").onclick = () => marcarNoShow(mesa);
    }

    // La cuenta es el parcial _Cuenta del servidor; sus acciones avisan con "mc:cuenta-cambio".
    async function cargarCuenta(mesa, body) {
        const id = mesa.idMesa;
        if (!mesa.reservaActual) { body.innerHTML = `<p class="mc-panel-aviso">Esta mesa no tiene una reserva asociada.</p>`; return; }
        body.innerHTML = `<p class="small text-secondary mb-0">Cargando cuenta…</p>`;
        try {
            const resp = await fetch(`/admin/ventas/cuenta/${mesa.reservaActual.idReserva}`, { headers: { "X-Requested-With": "XMLHttpRequest" } });
            const html = await resp.text();
            if (seleccionadaId === id && modo === "asignar") body.innerHTML = html;
        } catch {
            if (seleccionadaId === id) body.innerHTML = `<p class="mc-panel-aviso">No se pudo cargar la cuenta. Intenta de nuevo.</p>`;
        }
    }

    // Deselecciona y oculta el panel; con repintar=false el llamador repinta despues (cambio de modo o de vista).
    function cerrarPanel(repintar = true) {
        seleccionadaId = null;
        document.getElementById("mc-panel").classList.remove("is-open");
        if (repintar) renderizar();
    }

    async function clienteLlego(mesa, boton) {
        boton.disabled = true;
        const r = await llamar("/admin/mesas/cliente-llego", "POST", { codigoReserva: mesa.reservaActual.codigoReserva });
        toast(r.success ? "Llegada registrada. Ya puedes agregar el primer pedido." : r.message, r.success ? 3000 : 5000);
        await cargarMesas();
        abrirPanel();
    }

    async function marcarNoShow(mesa) {
        const r = await llamar("/admin/mesas/no-show", "POST", { codigoReserva: mesa.reservaActual.codigoReserva });
        toast(r.success ? "Reserva marcada como No Show; la mesa quedó libre." : r.message, 4000);
        if (r.success) cerrarPanel();
        await cargarMesas();
        if (!r.success) abrirPanel();
    }

    document.addEventListener("mc:cuenta-cambio", async e => {
        const d = e.detail || {};
        const cierra = d.tipo === "cobro" || d.tipo === "sin-consumo";
        toast(d.mensaje, cierra ? 5000 : 3000);
        if (cierra) cerrarPanel();
        await cargarMesas();
        if (!cierra) abrirPanel();
    });

    // Tarjetas: el panel va al lado; Plano y Editar salon: superpuesto en el canvas.
    function ubicarPanel() {
        const panel = document.getElementById("mc-panel");
        const lateral = modo === "asignar" && vista === "tarjetas";
        const destino = document.getElementById(lateral ? "mc-panel-slot" : "mc-canvas");
        if (panel.parentElement !== destino) destino.appendChild(panel);
        panel.classList.toggle("is-lateral", lateral);
        panel.classList.toggle("is-ancho", modo === "asignar");
    }

    document.getElementById("btn-cerrar-panel").onclick = () => cerrarPanel();

    async function cambiarCapacidad(mesa, delta) {
        const nueva = Math.max(1, Math.min(12, mesa.capacidad + delta));
        const r = await llamar(`/admin/mesas/${mesa.idMesa}/capacidad`, "PUT", { capacidad: nueva });
        if (!r.success) toast(r.message);
        await cargarMesas(); abrirPanel();
    }

    async function toggleEstado(mesa) {
        const nuevo = mesa.estado === "Deshabilitada" ? "Disponible" : "Deshabilitada";
        const r = await llamar(`/admin/mesas/${mesa.idMesa}/estado`, "PUT", { estado: nuevo });
        if (!r.success) toast(r.message);
        await cargarMesas(); abrirPanel();
    }

    async function eliminarMesa(mesa) {
        const r = await llamar(`/admin/mesas/${mesa.idMesa}`, "DELETE");
        if (!r.success) toast(r.message);
        cerrarPanel(false);
        await cargarMesas();
    }

    async function sentarCliente(idMesa, personas) {
        const r = await llamar("/admin/mesas/walkin", "POST", { cantidadPersonas: personas, idsMesa: [idMesa] });
        if (!r.success) toast(r.message);
        await cargarMesas();
        if (r.success) abrirPanel();
    }

    // ---------- Agregar mesa ----------
    let nuevaCapacidad = 2;
    document.getElementById("btn-fab-agregar")?.addEventListener("click", () => {
        document.getElementById("mc-add-popover").classList.toggle("is-open");
    });
    document.getElementById("btn-cancelar-nueva-mesa")?.addEventListener("click", () => {
        document.getElementById("mc-add-popover").classList.remove("is-open");
    });
    document.getElementById("btn-dec-nueva-capacidad")?.addEventListener("click", () => {
        nuevaCapacidad = Math.max(1, nuevaCapacidad - 1);
        document.getElementById("mc-nueva-capacidad").textContent = nuevaCapacidad;
    });
    document.getElementById("btn-inc-nueva-capacidad")?.addEventListener("click", () => {
        nuevaCapacidad = Math.min(12, nuevaCapacidad + 1);
        document.getElementById("mc-nueva-capacidad").textContent = nuevaCapacidad;
    });
    document.getElementById("btn-confirmar-nueva-mesa")?.addEventListener("click", async () => {
        const numeroMesa = Math.max(0, ...mesas.map(m => m.numeroMesa)) + 1;
        const r = await llamar("/admin/mesas", "POST", { numeroMesa, capacidad: nuevaCapacidad, posX: 950, posY: 420 });
        if (!r.success) toast(r.message);
        document.getElementById("mc-add-popover").classList.remove("is-open");
        nuevaCapacidad = 2;
        document.getElementById("mc-nueva-capacidad").textContent = "2";
        await cargarMesas();
    });

    // ---------- Buscar mesa / recomendacion (modo Asignar) ----------
    async function actualizarRecomendacion() {
        const personas = parseInt(document.getElementById("mc-buscar-personas").value, 10) || 1;
        const r = await llamar(`/admin/mesas/sugerencia-walkin?cantidadPersonas=${personas}`, "GET");
        const resultado = document.getElementById("mc-buscar-resultado");
        const btnAceptar = document.getElementById("btn-aceptar-recomendacion");
        recomendadas = [];
        if (r.success && r.data && r.data.numerosMesa && r.data.numerosMesa.length > 0) {
            recomendadas = mesas.filter(m => r.data.numerosMesa.includes(m.numeroMesa)).map(m => m.idMesa);
            resultado.textContent = (r.data.esCombinacionDeMesas ? "✓ Combinacion sugerida: Mesa " : "✓ Mejor opcion: Mesa ") + r.data.numerosMesa.join(" + Mesa ");
            resultado.style.color = "#2F6B4F";
            btnAceptar.classList.remove("d-none");
        } else {
            resultado.textContent = "⚠ Ninguna mesa ni combinacion cubre este grupo.";
            resultado.style.color = "#9C2D1F";
            btnAceptar.classList.add("d-none");
        }
        renderizar();
    }
    document.getElementById("mc-buscar-personas").addEventListener("input", actualizarRecomendacion);
    document.getElementById("btn-aceptar-recomendacion").addEventListener("click", async () => {
        const personas = parseInt(document.getElementById("mc-buscar-personas").value, 10) || 1;
        const r = await llamar("/admin/mesas/walkin", "POST", { cantidadPersonas: personas, idsMesa: recomendadas });
        if (!r.success) toast(r.message);
        await cargarMesas();
        await actualizarRecomendacion();
    });

    // ---------- Cambio de modo ----------
    function aplicarModo() {
        document.querySelectorAll("#mc-mode-switch button").forEach(b => b.classList.toggle("is-active", b.dataset.modo === modo));
        document.getElementById("mc-buscar-bar").classList.toggle("is-visible", modo === "asignar");
        document.getElementById("btn-fab-agregar").classList.toggle("is-visible", modo === "editar" && esAdministradorGeneral);
        const hint = document.getElementById("mc-mode-hint");
        hint.textContent = "Arrastra las mesas para ubicarlas. Las lineas punteadas indican mesas adyacentes: se combinan automaticamente para un grupo que no cabe en una sola mesa.";
        hint.classList.toggle("d-none", modo !== "editar");
        document.getElementById("mc-resumen").classList.toggle("d-none", modo !== "asignar");
        document.getElementById("mc-vista-switch").classList.toggle("d-none", modo !== "asignar");
        cerrarPanel(false);
        recomendadas = [];
        aplicarVista();
    }

    function aplicarVista() {
        const tarjetas = modo === "asignar" && vista === "tarjetas";
        document.querySelectorAll("#mc-vista-switch button").forEach(b => {
            const activo = b.dataset.vista === vista;
            b.classList.toggle("is-active", activo);
            b.setAttribute("aria-pressed", activo);
        });
        document.getElementById("mc-tarjetas-wrap").classList.toggle("d-none", !tarjetas);
        document.getElementById("mc-canvas").classList.toggle("d-none", tarjetas);
        ubicarPanel();
    }

    document.querySelectorAll("#mc-vista-switch button").forEach(b => b.addEventListener("click", () => {
        vista = b.dataset.vista;
        try { localStorage.setItem("mc.mesas.vista", vista); } catch { /* sin almacenamiento */ }
        cerrarPanel(false);
        aplicarVista();
        renderizar();
    }));

    document.getElementById("btn-modo-editar")?.addEventListener("click", () => { modo = "editar"; aplicarModo(); cargarMesas(); });
    document.getElementById("btn-modo-asignar").addEventListener("click", () => { modo = "asignar"; aplicarModo(); cargarMesas(); });

    // Refresco: la hora avanza ("En n min", esperas, tiempo sentado), asi que el tablero se vuelve a pedir.
    setInterval(() => { if (modo === "asignar" && !document.hidden && !arrastrando) cargarMesas().then(refrescarPanelSiCambio); }, 20000);
    window.addEventListener("resize", () => { if (modo === "editar" || vista === "plano") renderizar(); });
    document.addEventListener("visibilitychange", () => { if (modo === "asignar" && !document.hidden) cargarMesas().then(refrescarPanelSiCambio); });

    aplicarModo();
    cargarMesas();
})();
