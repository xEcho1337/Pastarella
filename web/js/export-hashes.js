(function () {
  "use strict";

  function str(v) {
    return v === null || v === undefined ? "" : String(v).trim();
  }

  function pick(obj, upper, lower) {
    if (!obj) return "";
    if (obj[upper] !== undefined && obj[upper] !== null) return str(obj[upper]);
    if (obj[lower] !== undefined && obj[lower] !== null) return str(obj[lower]);
    return "";
  }

  /* SHA256 hidden inside the Metadata dictionary (any key casing) */
  function metadataSha256(metadata) {
    if (!metadata || typeof metadata !== "object") return "";
    for (var k in metadata) {
      if (Object.prototype.hasOwnProperty.call(metadata, k) &&
          k.toLowerCase() === "sha256") {
        return str(metadata[k]);
      }
    }
    return "";
  }

  function collectProcesses(report) {
    return report.Processes.map(function (p) {
      return pick(p, "Sha256", "sha256");
    });
  }

  function collectServices(report) {
    return report.Services.map(function (s) {
      return pick(s, "Sha256", "sha256");
    });
  }

  function collectDrivers(report) {
    return report.Drivers.map(function (d) {
      return pick(d, "Sha256", "sha256");
    });
  }

  function collectPersistences(report) {
    return report.Persistences.map(function (p) {
      var act = p.Action !== undefined ? p.Action : p.action;
      if (act && typeof act === "object") {
        var sha = pick(act, "Sha256", "sha256");
        if (sha) return sha;
      }
      var md = p.Metadata !== undefined ? p.Metadata : p.metadata;
      return metadataSha256(md);
    });
  }

  function buildTxt(report) {
    var lines = [];
    lines.push("Pastarella hashes export");
    var ts = report.Timestamp || "unknown";
    try {
      if (ts !== "unknown") ts = new Date(ts).toLocaleString();
    } catch (e) {
      /* keep raw */
    }
    lines.push("Report collected at: " + ts);
    lines.push("Exported at: " + new Date().toLocaleString());
    lines.push("");

    var hashes = [].concat(
      collectProcesses(report),
      collectServices(report),
      collectDrivers(report),
      collectPersistences(report)
    ).filter(function (h) {
      return h !== "" && h.toUpperCase() !== "N/A";
    });

    lines = lines.concat(hashes);

    return lines.join("\n");
  }

  function download(filename, text) {
    var blob = new Blob([text], { type: "text/plain;charset=utf-8" });
    var url = URL.createObjectURL(blob);
    var a = document.createElement("a");
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    setTimeout(function () {
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    }, 100);
  }

  function stamp() {
    var d = new Date();
    function p(n) { return (n < 10 ? "0" : "") + n; }
    return d.getFullYear() + p(d.getMonth() + 1) + p(d.getDate()) +
      "-" + p(d.getHours()) + p(d.getMinutes()) + p(d.getSeconds());
  }

  function exportHashes() {
    if (!window.PastarellaReport || !window.PastarellaReport.hasReport()) {
      alert("Load a report first");
      return;
    }
    var report = window.PastarellaReport.getReport();
    download("pastarella-hashes-" + stamp() + ".txt", buildTxt(report));
  }

  /* Any element with data-action="export-hashes" triggers the export */
  document.addEventListener("click", function (e) {
    var el = e.target.closest ? e.target.closest('[data-action="export-hashes"]') : null;
    if (el) {
      e.preventDefault();
      exportHashes();
    }
  });

  window.PastarellaExport = {
    exportHashes: exportHashes,
    buildTxt: buildTxt
  };
})();
