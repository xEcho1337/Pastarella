(function () {
  "use strict";

  /* id -> partial file, topbar titles and sidebar icon */
  var NAV = [
    { id: "index", file: "home.html", title: "Home", sub: "Welcome" },
    { id: "dashboard", file: "dashboard.html", title: "Dashboard", sub: "Overview" },
    { id: "services", file: "services.html", title: "Services", sub: "Enabled services" },
    { id: "network", file: "network.html", title: "Network", sub: "Open ports and hosts" },
    { id: "system", file: "system.html", title: "System", sub: "Processes, users, storage" },
    { id: "persistence", file: "persistence.html", title: "Persistence", sub: "Persistence points" },
    { id: "environment", file: "environment.html", title: "Environment", sub: "Env vars and history" },
    { id: "drivers", file: "drivers.html", title: "Drivers", sub: "Drivers and kernel modules" },
    { id: "recent-files", file: "recent-files.html", title: "Recent Files", sub: "Recently accessed files" }
  ];

  function meta(id) {
    for (var i = 0; i < NAV.length; i++) {
      if (NAV[i].id === id) return NAV[i];
    }
    return NAV[0];
  }

  function cloneTemplate(id) {
    var tpl = document.getElementById(id);
    return tpl.content.cloneNode(true);
  }

  function fillHead(frag, item) {
    frag.querySelector(".page-title").textContent = item.title;
    frag.querySelector(".page-sub").textContent = item.sub;
  }

  /* Section page-head shared by every page except the custom dashboard one */
  function pageHeadEl(item) {
    var frag = cloneTemplate("tpl-page-head");
    fillHead(frag, item);
    return frag.querySelector(".page-head");
  }

  /* Shown instead of the real content when no report is loaded yet */
  function emptyStateFrag(item) {
    var frag = cloneTemplate("tpl-empty-state");
    fillHead(frag, item);
    frag.querySelector(".empty-report-text").textContent =
      "Load an AnalysisReport JSON to inspect " + item.title.toLowerCase() + ".";
    return frag;
  }

  window.PastarellaUI = {
    NAV: NAV,
    meta: meta,
    pageHeadEl: pageHeadEl,
    emptyStateFrag: emptyStateFrag
  };
})();
