(function () {
  "use strict";

  var STORAGE_KEY = "pastarellaReport";
  var report = null;

  try {
    var saved = sessionStorage.getItem(STORAGE_KEY);
    if (saved) report = JSON.parse(saved);
  } catch (e) {
    report = null;
  }

  /* Pick first defined key (PascalCase from JsonWriter, camelCase fallback) */
  function v(obj, upper, lower, fallback) {
    if (!obj) return fallback !== undefined ? fallback : "–";
    if (obj[upper] !== undefined && obj[upper] !== null) return obj[upper];
    if (lower && obj[lower] !== undefined && obj[lower] !== null) return obj[lower];
    return fallback !== undefined ? fallback : "–";
  }

  function arr(reportObj, upper, lower) {
    var value = reportObj ? (reportObj[upper] !== undefined ? reportObj[upper] : reportObj[lower]) : undefined;
    return Array.isArray(value) ? value : [];
  }

  function normalize(data) {
    if (!data || typeof data !== "object") return null;
    return {
      Timestamp: data.Timestamp || data.timestamp || null,
      Processes: arr(data, "Processes", "processes"),
      Services: arr(data, "Services", "services"),
      OpenPorts: arr(data, "OpenPorts", "openPorts"),
      Users: arr(data, "Users", "users"),
      Hosts: arr(data, "Hosts", "hosts"),
      Drivers: arr(data, "Drivers", "drivers"),
      Persistences: arr(data, "Persistences", "persistences"),
      Storages: arr(data, "Storages", "storages"),
      Envs: data.Envs || data.envs || {},
      CommandHistories: arr(data, "CommandHistories", "commandHistories"),
      RecentFiles: arr(data, "RecentFiles", "recentFiles")
    };
  }

  function hasReport() {
    return report !== null;
  }

  function getReport() {
    return report;
  }

  function setReport(data) {
    var normalized = normalize(data);
    if (!normalized) return false;
    report = normalized;
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(normalized));
    } catch (e) {
      /* storage full or unavailable: keep in-memory only */
    }
    window.dispatchEvent(new CustomEvent("pastarella:report"));
    return true;
  }

  function clearReport() {
    report = null;
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch (e) {
      /* ignore */
    }
    window.dispatchEvent(new CustomEvent("pastarella:report"));
  }

  function pickFile(callback) {
    var input = document.createElement("input");
    input.type = "file";
    input.accept = "application/json,.json";
    input.addEventListener("change", function () {
      var file = input.files && input.files[0];
      if (!file) return;
      var reader = new FileReader();
      reader.onload = function () {
        try {
          var data = JSON.parse(reader.result);
          if (!setReport(data)) alert("Invalid AnalysisReport JSON");
          else if (callback) callback(getReport());
        } catch (e) {
          alert("Invalid AnalysisReport JSON");
        }
      };
      reader.readAsText(file);
    });
    input.click();
  }

  /* Any element with data-action="load-report" opens the file picker */
  document.addEventListener("click", function (e) {
    var el = e.target.closest ? e.target.closest('[data-action="load-report"]') : null;
    if (el) {
      e.preventDefault();
      pickFile();
    }
  });

  /* Row-building helpers (same shapes as web/js/analyze.js) */

  function esc(value) {
    return value === null || value === undefined ? "" : String(value);
  }

  function appendCells(tr, cells) {
    cells.forEach(function (cell) {
      var td = document.createElement("td");
      if (cell && cell.__element) {
        td.appendChild(cell.__element);
      } else if (cell && typeof cell === "object" && cell.nodeType === 1) {
        td.appendChild(cell);
      } else {
        td.textContent = esc(cell);
      }
      if (cell && cell.__mono) td.className = "cell-path";
      if (cell && cell.__preline) td.style.whiteSpace = "pre-line";
      if (cell && cell.__center) td.classList.add("icon-cell");
      tr.appendChild(td);
    });
  }

  function mono(text) {
    var s = new String(esc(text));
    s.__mono = true;
    return s;
  }

  function preline(text) {
    var s = new String(esc(text));
    s.__preline = true;
    return s;
  }

  function centered(element) {
    var s = new String("");
    s.__center = true;
    s.__element = element;
    return s;
  }

  /* Same as signerIcon() in web/js/analyze.js */
  function signerIcon(signer) {
    var i = document.createElement("i");
    if (signer) {
      i.className = "bi bi-check-circle-fill text-success";
      i.title = String(signer);
    } else {
      i.className = "bi bi-x-circle-fill text-danger";
    }
    return i;
  }

  /* Same as booleanIcon() in web/js/analyze.js */
  function booleanIcon(bool) {
    var i = document.createElement("i");
    i.className = bool
      ? "bi bi-check-circle-fill text-success"
      : "bi bi-x-circle-fill text-danger";
    return i;
  }

  /* Same as stringifyMetadata() in web/js/analyze.js */
  function stringifyMetadata(metadata) {
    if (!metadata) return "";
    var str = "";
    var entries = Array.isArray(metadata)
      ? metadata.map(function (m, i) { return [i, m]; })
      : Object.entries(metadata);
    entries.forEach(function (entry) {
      str += entry[0] + ": " + entry[1] + "\n";
    });
    return str.trimEnd();
  }

  function fmtGB(bytes) {
    var n = Number(bytes);
    if (isNaN(n)) return "–";
    return (n / Math.pow(1024, 3)).toFixed(2) + " GB";
  }

  /* Same polymorphic Action rendering as web/js/analyze.js */
  function actionText(action) {
    if (!action) return "None";
    if (action.Path !== undefined || action.path !== undefined) {
      var p = action.Path !== undefined ? action.Path : action.path;
      var sha = action.Sha256 !== undefined ? action.Sha256 : action.sha256;
      return "Scheduled run executable\nPath: " + p + "\nSHA256: " + sha;
    }
    if (action.ClassId !== undefined || action.classId !== undefined) {
      var id = action.ClassId !== undefined ? action.ClassId : action.classId;
      var name = action.ClassName !== undefined ? action.ClassName : action.className;
      return "Scheduled COM\nClass ID: " + id + "\nClass name: " + name;
    }
    if (action.Title !== undefined || action.title !== undefined) {
      var title = action.Title !== undefined ? action.Title : action.title;
      var msg = action.Message !== undefined ? action.Message : action.message;
      return "Scheduled message\nTitle: " + title + "\nMessage: " + msg;
    }
    return "Scheduled email";
  }

  window.PastarellaReport = {
    hasReport: hasReport,
    getReport: getReport,
    setReport: setReport,
    clearReport: clearReport,
    pickFile: pickFile,
    v: v,
    arr: arr,
    appendCells: appendCells,
    mono: mono,
    preline: preline,
    centered: centered,
    signerIcon: signerIcon,
    booleanIcon: booleanIcon,
    stringifyMetadata: stringifyMetadata,
    fmtGB: fmtGB,
    actionText: actionText
  };
})();
