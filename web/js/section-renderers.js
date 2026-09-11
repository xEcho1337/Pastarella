(function () {
  "use strict";

  function R() {
    return window.PastarellaReport;
  }

  /* Canonical headers per table, in cell order. The partials carry the
     same theads, but a stale cached partial would silently misalign every
     row (right values under wrong headers), so the thead is enforced here
     before any rows are rendered. Saved order/widths/visibility apply later
     on top of the corrected thead. */
  var HEADERS = {
    servicesBody: ["Status", "Service Type", "Service Name", "Display Name", "Command", "Executable SHA256"],
    portsBody: ["Protocol", "State", "Local", "Remote", "PID", "Process Name"],
    hostsBody: ["IP", "Domain"],
    processesBody: ["Id", "Name", "Path", "SHA256", "Signature", "Start Time", "Metadata"],
    usersBody: ["Name", "Description", "Uid", "Home", "Disabled", "Metadata"],
    storageBody: ["Type", "Name", "Free Space", "Total Space"],
    persistenceBody: ["Risk Score", "Name", "Path", "Action", "Trigger", "Privilege", "Type", "Metadata"],
    envBody: ["Key", "Value"],
    historyBody: ["Shell", "Command"],
    driversBody: ["Name", "Display Name", "Identifier", "Type", "Executable Path", "Version", "Loaded", "SHA256", "Signer"],
    recentFilesBody: ["File Path", "Creation Time", "Last Write Time"]
  };

  function liveHeaderNames(headRow) {
    return Array.prototype.map.call(headRow.querySelectorAll("th"), function (th) {
      for (var i = 0; i < th.childNodes.length; i++) {
        var n = th.childNodes[i];
        if (n.nodeType === 3) return n.textContent.trim();
      }
      return th.textContent.trim();
    });
  }

  function ensureHeaders(bodyId) {
    var expected = HEADERS[bodyId];
    if (!expected) return;
    var body = document.getElementById(bodyId);
    if (!body) return;
    var table = body.closest("table");
    if (!table) return;
    var headRow = table.querySelector("thead tr");
    if (!headRow) return;
    if (liveHeaderNames(headRow).join("\0") === expected.join("\0")) return;
    console.warn("Pastarella: stale table header for #" + bodyId + ", rebuilding");
    headRow.innerHTML = "";
    expected.forEach(function (name) {
      var th = document.createElement("th");
      th.textContent = name;
      headRow.appendChild(th);
    });
  }

  function fillBody(id, rows) {
    /* Paged render: only the current slice hits the DOM */
    ensureHeaders(id);
    if (window.PastarellaPager) {
      window.PastarellaPager.setRows(id, rows);
      return;
    }
    var body = document.getElementById(id);
    if (!body) return;
    body.innerHTML = "";
    rows.forEach(function (cells) {
      var tr = document.createElement("tr");
      R().appendCells(tr, cells);
      body.appendChild(tr);
    });
  }

  /* Processes: Id, Name, Path, SHA256, Signature, Start Time, Metadata */
  function value(d, upper, lower) {
    if (d[upper] !== undefined && d[upper] !== null) return d[upper];
    if (d[lower] !== undefined && d[lower] !== null) return d[lower];
    return null;
  }

  function renderProcesses(report, bodyId) {
    fillBody(bodyId || "processesBody", report.Processes.map(function (d) {
      return [
        R().v(d, "Id", "id", ""),
        R().v(d, "Name", "name", ""),
        R().mono(R().v(d, "Path", "path", "") + " " + R().v(d, "CommandArgs", "commandArgs", "")),
        R().mono(R().v(d, "Sha256", "sha256", "")),
        R().centered(R().signerIcon(value(d, "Signer", "signer"))),
        R().v(d, "StartTime", "startTime", ""),
        R().preline(R().stringifyMetadata(value(d, "Metadata", "metadata") || {}))
      ];
    }));
  }

  /* Services: Status, Service Type, Service Name, Display Name, Command, Executable SHA256 */
  function renderServices(report) {
    fillBody("servicesBody", report.Services.map(function (d) {
      var args = R().v(d, "Arguments", "arguments", []);
      var cmd = R().v(d, "ExecPath", "execPath", "") + " " + (Array.isArray(args) ? args.join(" ") : args);
      return [
        R().v(d, "Status", "status", ""),
        R().v(d, "ServiceType", "serviceType", ""),
        R().v(d, "ServiceName", "serviceName", ""),
        R().v(d, "DisplayName", "displayName", ""),
        R().mono(cmd.trim()),
        R().mono(R().v(d, "Sha256", "sha256", ""))
      ];
    }));
  }

  /* Open Ports: Protocol, State, Local, Remote, PID, Process Name */
  function renderPorts(report, bodyId) {
    fillBody(bodyId || "portsBody", report.OpenPorts.map(function (d) {
      var local = R().v(d, "Local", "local", {});
      var remote = R().v(d, "Remote", "remote", null);
      var state = R().v(d, "State", "state", "");
      return [
        R().v(d, "Protocol", "protocol", ""),
        state === "–" ? "" : state,
        (R().v(local, "Ip", "ip", "") + ":" + R().v(local, "Port", "port", "")),
        remote ? (R().v(remote, "Ip", "ip", "") + ":" + R().v(remote, "Port", "port", "")) : "",
        R().v(d, "ProcessId", "processId", ""),
        R().v(d, "ProcessName", "processName", "")
      ];
    }));
  }

  /* Users: Name, Description, Uid, Home, Disabled, Metadata */
  function renderUsers(report, bodyId) {
    fillBody(bodyId || "usersBody", report.Users.map(function (d) {
      var disabled = value(d, "Disabled", "disabled");
      return [
        R().v(d, "Name", "name", ""),
        R().v(d, "Description", "description", ""),
        R().v(d, "Uid", "uid", ""),
        R().mono(R().v(d, "Home", "home", "")),
        R().centered(R().booleanIcon(disabled === true || disabled === "true")),
        R().preline(R().stringifyMetadata(value(d, "Metadata", "metadata") || {}))
      ];
    }));
  }

  /* Hosts: IP, Domain */
  function renderHosts(report, bodyId) {
    fillBody(bodyId || "hostsBody", report.Hosts.map(function (d) {
      return [R().v(d, "Ip", "ip", ""), R().v(d, "Domain", "domain", "")];
    }));
  }

  /* Drivers: Name, Display Name, Identifier, Type, Executable Path, Version, Loaded, SHA256, Signer */
  function renderDrivers(report) {
    fillBody("driversBody", report.Drivers.map(function (d) {
      var loaded = value(d, "Loaded", "loaded");
      return [
        R().v(d, "Name", "name", ""),
        R().v(d, "DisplayName", "displayName", ""),
        R().mono(R().v(d, "Identifier", "identifier", "")),
        R().v(d, "Type", "type", ""),
        R().mono(R().v(d, "ExecutablePath", "executablePath", "")),
        R().v(d, "Version", "version", ""),
        R().centered(R().booleanIcon(loaded === true || loaded === "true")),
        R().mono(R().v(d, "Sha256", "sha256", "")),
        R().centered(R().signerIcon(value(d, "Signer", "signer")))
      ];
    }));
  }

  /* Persistences: Risk Score, Name, Path, Action, Trigger, Privilege, Type, Metadata */
  function renderPersistences(report, bodyId) {
    var rows = report.Persistences.map(function (d) {
      var score = Number(value(d, "RiskScore", "riskScore")) || 0;
      var badge = document.createElement("span");
      badge.className = "status-badge " + riskClass(score);
      badge.textContent = String(score);
      return [
        badge,
        R().v(d, "Name", "name", ""),
        R().mono(R().v(d, "Path", "path", "")),
        R().preline(R().actionText(value(d, "Action", "action"))),
        R().v(d, "Trigger", "trigger", ""),
        R().v(d, "Privilege", "privilege", ""),
        R().v(d, "Type", "type", ""),
        R().preline(R().stringifyMetadata(value(d, "Metadata", "metadata") || {}))
      ];
    });
    rows.sort(function (a, b) {
      return Number(b[0].textContent) - Number(a[0].textContent);
    });
    fillBody(bodyId || "persistenceBody", rows);
  }

  function riskClass(score) {
    if (score >= 70) return "status-err";
    if (score >= 40) return "status-warn";
    return "status-ok";
  }

  /* Storages: Type, Name, Free Space, Total Space */
  function renderStorages(report, bodyId) {
    fillBody(bodyId || "storageBody", report.Storages.map(function (d) {
      return [
        R().v(d, "Type", "type", ""),
        R().v(d, "Name", "name", ""),
        R().fmtGB(R().v(d, "FreeSpace", "freeSpace", NaN)),
        R().fmtGB(R().v(d, "TotalSpace", "totalSpace", NaN))
      ];
    }));
  }

  /* Envs: Key, Value */
  function renderEnvs(report) {
    var rows = Object.entries(report.Envs || {}).map(function (entry) {
      return [R().mono(entry[0]), entry[1]];
    });
    fillBody("envBody", rows);
  }

  /* Command Histories: Shell, Command */
  function renderHistories(report) {
    var rows = [];
    report.CommandHistories.forEach(function (history) {
      var shell = R().v(history, "Shell", "shell", "");
      var commands = R().v(history, "Commands", "commands", []);
      (Array.isArray(commands) ? commands : [commands]).forEach(function (cmd) {
        rows.push([shell, R().mono(cmd)]);
      });
    });
    fillBody("historyBody", rows);
  }

  /* Recent Files: File Path, Creation Time, Last Write Time */
  function renderRecentFiles(report, bodyId) {
    fillBody(bodyId || "recentFilesBody", report.RecentFiles.map(function (d) {
      return [
        R().mono(R().v(d, "FilePath", "filePath", "")),
        R().v(d, "CreationTime", "creationTime", ""),
        R().v(d, "LastWriteTime", "lastWriteTime", "")
      ];
    }));
  }

  var renderers = {
    services: function (report) { renderServices(report); },
    network: function (report) { renderPorts(report); renderHosts(report); },
    system: function (report) {
      renderProcesses(report);
      renderUsers(report);
      renderStorages(report);
    },
    persistence: function (report) { renderPersistences(report); },
    environment: function (report) { renderEnvs(report); renderHistories(report); },
    drivers: function (report) { renderDrivers(report); },
    "recent-files": function (report) { renderRecentFiles(report); }
  };

  window.PastarellaSections = {
    render: function (name, report) {
      if (renderers[name]) renderers[name](report);
    },
    riskClass: riskClass
  };
})();
