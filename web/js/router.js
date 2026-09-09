(function () {
  "use strict";

  var requested = "dashboard";

  function UI() {
    return window.PastarellaUI;
  }

  function hasReport() {
    return window.PastarellaReport && window.PastarellaReport.hasReport();
  }

  function setActiveLink(name) {
    var links = document.querySelectorAll(".side-link[data-page]");
    links.forEach(function (a) {
      var isActive = a.getAttribute("data-page") === name;
      a.classList.toggle("active", isActive);
      var dot = a.querySelector(".active-dot");
      if (dot) dot.style.visibility = isActive ? "visible" : "hidden";
    });
  }

  function setTopbar(item) {
    var titleEl = document.getElementById("topbarTitle");
    var subEl = document.getElementById("topbarSub");
    if (titleEl) titleEl.textContent = item.title;
    if (subEl) subEl.textContent = item.sub;
  }

  function renderPage(name) {
    if (name === "dashboard") {
      if (typeof window.initDashboardPage === "function") window.initDashboardPage();
    } else if (name === "index") {
      if (typeof window.initHomePage === "function") window.initHomePage();
    } else if (window.PastarellaSections && hasReport()) {
      window.PastarellaSections.render(name, window.PastarellaReport.getReport());
    }
  }

  function loadPage(name, force) {
    var item = UI().meta(name);
    name = item.id;
    var container = document.getElementById("pageContent");
    if (!force && name === requested && container && container.innerHTML !== "") return;
    requested = name;

    setActiveLink(name);
    setTopbar(item);

    /* The landing page is always visible; every other page without a
       loaded report shows icon + Load Report instead of real content */
    if (!hasReport() && name !== "index") {
      container.innerHTML = "";
      container.appendChild(UI().emptyStateFrag(item));
      return;
    }

    fetch("./pages/" + item.file, { cache: "no-store" })
      .then(function (res) {
        if (!res.ok) throw new Error("Page not found: " + name);
        return res.text();
      })
      .then(function (html) {
        /* Section partials hold tables only; the shared head is prepended
           (dashboard and index bring their own header) */
        container.innerHTML = html;
        if (name !== "dashboard" && name !== "index") container.prepend(UI().pageHeadEl(item));
        renderPage(name);
        /* Resize/reorder/sort/collapse only on section pages */
        if (name !== "dashboard" && name !== "index") {
          if (window.PastarellaColumns) window.PastarellaColumns.applyToPage(container, name);
          if (window.PastarellaPager) window.PastarellaPager.bindHeaders(container);
          if (window.PastarellaCollapse) window.PastarellaCollapse.applyTo(container);
        }
      })
      .catch(function () {
        container.innerHTML = '<div class="panel"><div class="panel-title">Error</div><div class="panel-sub">Could not load page: ' + name + '</div></div>';
      });
  }

  function bindNav() {
    document.querySelectorAll(".side-link[data-page]").forEach(function (a) {
      a.addEventListener("click", function (e) {
        e.preventDefault();
        loadPage(a.getAttribute("data-page"));
      });
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    bindNav();
    loadPage(hasReport() ? "dashboard" : "index", true);
  });

  /* A newly loaded report reveals the real content of the current page */
  window.addEventListener("pastarella:report", function () {
    loadPage(requested, true);
  });

  window.PastarellaRouter = {
    loadPage: function (name) { loadPage(name); },
    reload: function () { loadPage(requested, true); },
    pages: UI().NAV.map(function (item) { return item.id; })
  };
})();
