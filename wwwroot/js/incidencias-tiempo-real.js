// C: Conexión al canal WebSocket de PieHost y actualización de la lista sin recargar
(function () {
    "use strict";
    var cfg = document.getElementById("piehost-config");
    if (!cfg) return;
    var dominio = cfg.dataset.dominio, apiKey = cfg.dataset.apikey, canal = cfg.dataset.canal;
    var estadoEl = document.getElementById("ws-estado");
    var avisos = document.getElementById("avisos-tiempo-real");
    var reintento = 1000, primeraConexion = true;

    function mostrarEstado(texto, clase) {
        if (!estadoEl) return;
        estadoEl.textContent = "PieHost: " + texto;
        estadoEl.className = "badge " + clase;
    }

    function aviso(texto) {
        if (!avisos) return;
        var d = document.createElement("div");
        d.className = "alert alert-success shadow";
        d.textContent = texto;
        avisos.appendChild(d);
        setTimeout(function () { d.remove(); }, 8000);
    }

    // Quita de la lista una incidencia que ya no está Abierta
    function aplicar(id, estado) {
        var fila = document.querySelector('tr[data-id="' + id + '"]');
        if (!fila) return false;
        if (estado !== "Abierta") {
            var badge = fila.querySelector("[data-estado]");
            if (badge) { badge.textContent = estado; badge.className = "badge bg-danger"; }
            fila.style.transition = "opacity .6s";
            fila.style.opacity = "0.2";
            setTimeout(function () { fila.remove(); }, 700);
            return true;
        }
        return false;
    }

    // Al (re)conectar: consultar el estado vigente en el servidor
    async function sincronizar() {
        try {
            var r = await fetch("/Operaciones/Incidencias/Abiertas", { headers: { "Accept": "application/json" }, credentials: "same-origin" });
            if (!r.ok) return;
            var abiertas = await r.json(); // [ids]
            document.querySelectorAll("tr[data-id]").forEach(function (fila) {
                var id = parseInt(fila.getAttribute("data-id"), 10);
                if (abiertas.indexOf(id) === -1 && aplicar(id, "Cerrada")) aviso("Incidencia #" + id + " cerrada (recuperado al reconectar)");
            });
        } catch (e) { console.warn("No se pudo sincronizar", e); }
    }

    function conectar() {
        mostrarEstado("conectando…", "bg-secondary");
        var ws = new WebSocket("wss://" + dominio + "/v3/" + encodeURIComponent(canal) + "?api_key=" + encodeURIComponent(apiKey) + "&notify_self=1");
        ws.onopen = function () {
            reintento = 1000;
            mostrarEstado("conectado", "bg-success");
            sincronizar();
            primeraConexion = false;
        };
        ws.onmessage = function (m) {
            var msg;
            try { msg = JSON.parse(m.data); } catch (e) { return; }
            if (!msg || msg.event !== "IncidenciaActualizada" || !msg.data) return;
            console.log("IncidenciaActualizada", msg.data);
            var id = msg.data.Id !== undefined ? msg.data.Id : msg.data.id;
            var estado = msg.data.Estado || msg.data.estado;
            if (aplicar(id, estado)) aviso("Incidencia #" + id + " → " + estado);
        };
        ws.onclose = function () {
            mostrarEstado("reconectando…", "bg-warning text-dark");
            setTimeout(conectar, reintento);
            reintento = Math.min(reintento * 2, 15000);
        };
        ws.onerror = function () { ws.close(); };
    }

    conectar();
})();
