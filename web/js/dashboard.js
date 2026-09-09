(function () {
  "use strict";

  function store() {
    return window.PastarellaReport;
  }

  function setText(id, value) {
    var el = document.getElementById(id);
    if (el) el.textContent = String(value);
  }

  function renderStats(report) {
    setText("statProcesses", report.Processes.length);
    setText("statServices", report.Services.length);
    setText("statPorts", report.OpenPorts.length);
    setText("statUsers", report.Users.length);
    setText("statPersistences", report.Persistences.length);
    setText("statDrivers", report.Drivers.length);
    setText("statRecentFiles", report.RecentFiles.length);
    setText("statEnvs", Object.keys(report.Envs || {}).length);

    var running = report.Services.filter(function (s) {
      return s.Status === "Running";
    }).length;
    setText("statServicesSub", running + " running / " + report.Services.length + " total");
  }

  function renderChart(report) {
    var barsEl = document.getElementById("chartBars");
    var labelsEl = document.getElementById("chartLabels");
    if (!barsEl || !labelsEl) return;
    barsEl.innerHTML = "";
    labelsEl.innerHTML = "";

    var data = [
      { label: "Procs", value: report.Processes.length },
      { label: "Svcs", value: report.Services.length },
      { label: "Ports", value: report.OpenPorts.length },
      { label: "Users", value: report.Users.length },
      { label: "Persist", value: report.Persistences.length },
      { label: "Drivers", value: report.Drivers.length },
      { label: "Recent", value: report.RecentFiles.length }
    ];

    var max = Math.max.apply(null, data.map(function (d) { return d.value; }).concat([1]));

    data.forEach(function (d) {
      var col = document.createElement("div");
      col.className = "bar-col";

      var bar = document.createElement("div");
      bar.className = "bar" + (d.value > 0 ? " has-value" : "");
      bar.style.height = Math.max(3, Math.round((d.value / max) * 120)) + "px";
      bar.title = d.label + ": " + d.value;
      col.appendChild(bar);
      barsEl.appendChild(col);

      var lab = document.createElement("span");
      lab.textContent = d.label;
      labelsEl.appendChild(lab);
    });
  }

  function renderSummary(report) {
    var unsigned = report.Processes.filter(function (p) {
      return !p.Signer && !p.Sha256;
    }).length + report.Drivers.filter(function (d) {
      return !d.Signer;
    }).length;

    var highRisk = report.Persistences.filter(function (p) {
      return Number(p.RiskScore) >= 70;
    }).length;

    var ts = report.Timestamp || "–";
    if (ts !== "–") {
      try { ts = new Date(ts).toLocaleString(); } catch (e) { /* keep raw */ }
    }

    setText("sumTimestamp", ts);
    setText("sumUnsigned", unsigned + " binaries");
    setText("sumHighRisk", highRisk + " / " + report.Persistences.length);
    setText("sumHosts", report.Hosts.length + " entries");

    var sub = document.getElementById("reportSub");
    if (sub) {
      sub.textContent = "Forensic collection summary from the AnalysisReport · " +
        report.Processes.length + " processes, " + report.Persistences.length + " persistences.";
    }
  }

  function renderRiskTable(report) {
    var body = document.getElementById("riskBody");
    if (!body) return;
    body.innerHTML = "";

    var sorted = report.Persistences.slice().sort(function (a, b) {
      return Number(b.RiskScore) - Number(a.RiskScore);
    });

    sorted.slice(0, 5).forEach(function (p) {
      var tr = document.createElement("tr");
      var badge = document.createElement("span");
      badge.className = "status-badge " + window.PastarellaSections.riskClass(Number(p.RiskScore));
      badge.textContent = String(p.RiskScore);

      store().appendCells(tr, [
        store().mono(p.Name || ""),
        (function () {
          var t = document.createElement("span");
          t.className = "token-badge";
          t.textContent = p.Type || "";
          return t;
        })(),
        p.Trigger || "",
        p.Privilege || "",
        badge
      ]);
      /* risk badge cell must be right-aligned */
      tr.lastChild.className = "text-end";
      body.appendChild(tr);
    });
  }

  function renderAll() {
    if (!store() || !store().hasReport()) return;
    var report = store().getReport();
    renderStats(report);
    renderChart(report);
    renderSummary(report);
    renderRiskTable(report);
  }

  function bindRefresh() {
    var btn = document.getElementById("refreshBtn");
    if (!btn || btn.dataset.bound === "1") return;
    btn.dataset.bound = "1";
    btn.addEventListener("click", function () {
      btn.classList.add("loading");
      var original = btn.textContent;
      btn.textContent = "Refreshing…";
      setTimeout(function () {
        renderAll();
        btn.classList.remove("loading");
        btn.textContent = original;
      }, 300);
    });
  }

  function bindGoto() {
    document.querySelectorAll("[data-goto]").forEach(function (el) {
      if (el.dataset.bound === "1") return;
      el.dataset.bound = "1";
      el.addEventListener("click", function (e) {
        e.preventDefault();
        if (window.PastarellaRouter) window.PastarellaRouter.loadPage(el.getAttribute("data-goto"));
      });
    });
  }

  /* Called by js/router.js after pages/dashboard.html is injected */
  window.initDashboardPage = function () {
    renderAll();
    bindRefresh();
    bindGoto();
  };
})();
