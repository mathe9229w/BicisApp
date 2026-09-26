// Tema Pastel - interacciones visuales (no toca la lógica de la app)
(function () {
  "use strict";
  var raiz = document.documentElement;
  var reducido = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  // 1) Modo oscuro / claro recordado por el navegador
  function leerTema() { try { return localStorage.getItem("tema"); } catch (e) { return null; } }
  function guardarTema(t) { try { localStorage.setItem("tema", t); } catch (e) { } }
  if (leerTema() === "oscuro") raiz.setAttribute("data-tema", "oscuro");

  document.addEventListener("DOMContentLoaded", function () {
    var nav = document.querySelector("header .navbar-collapse");
    if (nav) {
      var b = document.createElement("button");
      b.type = "button"; b.className = "btn-tema ms-2"; b.title = "Cambiar tema";
      b.textContent = raiz.getAttribute("data-tema") === "oscuro" ? "☀️" : "🌙";
      b.addEventListener("click", function () {
        var oscuro = raiz.getAttribute("data-tema") === "oscuro";
        if (oscuro) raiz.removeAttribute("data-tema"); else raiz.setAttribute("data-tema", "oscuro");
        guardarTema(oscuro ? "claro" : "oscuro");
        b.textContent = oscuro ? "🌙" : "☀️";
      });
      nav.appendChild(b);
    }

    // 2) Aparición suave al hacer scroll
    var elementos = document.querySelectorAll("main .card, main .table, main .alert, main .list-group, main h1");
    if ("IntersectionObserver" in window && !reducido) {
      var obs = new IntersectionObserver(function (entradas) {
        entradas.forEach(function (en) { if (en.isIntersecting) { en.target.classList.add("visible"); obs.unobserve(en.target); } });
      }, { threshold: 0.08 });
      elementos.forEach(function (el, i) { el.classList.add("aparecer"); el.style.transitionDelay = Math.min(i * 60, 400) + "ms"; obs.observe(el); });
    }

    // 3) Efecto onda al hacer clic en botones
    document.addEventListener("click", function (ev) {
      var btn = ev.target.closest && ev.target.closest(".btn");
      if (!btn || reducido) return;
      var r = btn.getBoundingClientRect(), s = Math.max(r.width, r.height);
      var o = document.createElement("span");
      o.className = "onda"; o.style.width = o.style.height = s + "px";
      o.style.left = (ev.clientX - r.left - s / 2) + "px"; o.style.top = (ev.clientY - r.top - s / 2) + "px";
      btn.appendChild(o); setTimeout(function () { o.remove(); }, 650);
    });

    // 4) Botón "subir"
    var subir = document.createElement("button");
    subir.type = "button"; subir.className = "btn-subir"; subir.textContent = "↑"; subir.title = "Subir";
    subir.addEventListener("click", function () { window.scrollTo({ top: 0, behavior: "smooth" }); });
    document.body.appendChild(subir);
    window.addEventListener("scroll", function () { subir.classList.toggle("visible", window.scrollY > 300); }, { passive: true });

    // 5) Confeti cuando hay un mensaje de éxito (p. ej. solicitud registrada)
    if (!reducido && document.querySelector("main .alert-success")) confeti();

    // 6) Confeti también cuando llega un aviso "Aprobado" por WebSocket
    var avisos = document.getElementById("avisos-tiempo-real");
    if (avisos && "MutationObserver" in window) {
      new MutationObserver(function (m) {
        m.forEach(function (x) { x.addedNodes.forEach(function (n) { if (n.classList && n.classList.contains("alert-success") && !reducido) confeti(); }); });
      }).observe(avisos, { childList: true });
    }
  });

  function confeti() {
    var colores = ["#a8455a", "#6e2436", "#e8c4c8", "#f1cfd3", "#c97b84", "#f3e3cf"];
    for (var i = 0; i < 60; i++) {
      var c = document.createElement("span");
      c.className = "confeti";
      c.style.left = Math.random() * 100 + "vw";
      c.style.background = colores[i % colores.length];
      c.style.animationDuration = (1.8 + Math.random() * 1.6) + "s";
      c.style.animationDelay = (Math.random() * 0.4) + "s";
      document.body.appendChild(c);
      (function (el) { setTimeout(function () { el.remove(); }, 4000); })(c);
    }
  }
})();
