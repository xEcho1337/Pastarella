import { pastarellaReport } from "./pastarella-report.js";

(function () {
    "use strict";

    function str(v) {
        return v === null || v === undefined ? "" : String(v).trim();
    }

    function pick(d, key) {
        if (d[key] == null)
            return null;
        return d[key];
    }

    function metadataSha256(metadata) {
        if (!metadata || typeof metadata !== "object") return "";
        for (var k in metadata) {
            if (
                Object.prototype.hasOwnProperty.call(metadata, k) &&
                k.toLowerCase() === "sha256"
            ) {
                return str(metadata[k]);
            }
        }
        return "";
    }

    function collectProcesses(report) {
        return report.Processes.map(function (p) {
            console.log(p);
            return pick(p, "ExePath")?.Sha256 ?? "";
        });
    }

    function collectServices(report) {
        return report.Services.map(function (s) {
            return pick(s, "ExePath")?.Sha256 ?? "";
        });
    }

    function collectDrivers(report) {
        return report.Drivers.map(function (d) {
            return pick(d, "ExePath")?.Sha256 ?? "";
        });
    }

    function collectPersistences(report) {
        return report.Persistences.map(function (p) {
            var act = p.Action !== undefined ? p.Action : p.action;
            if (act && typeof act === "object") {
                const hash = pick(p, "ExePath")?.Sha256 ?? null;
                if (hash !== null)
                    return hash;
            }
            var md = p.Metadata !== undefined ? p.Metadata : p.metadata;
            return metadataSha256(md);
        });
    }

    function buildTxt(report) {
        var lines = [];
        lines.push("Pastarella hashes export");
        console.log(report);
        var ts = report.Timestamp || "unknown";
        try {
            if (ts !== "unknown") ts = new Date(ts).toLocaleString();
        } catch (e) {
            /* keep raw */
        }
        lines.push("Report collected at: " + ts);
        lines.push("Exported at: " + new Date().toLocaleString());
        lines.push("");

        var hashes = []
            .concat(
                collectProcesses(report),
                collectServices(report),
                collectDrivers(report),
                collectPersistences(report),
            )
            .filter(function (h) {
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
        function p(n) {
            return (n < 10 ? "0" : "") + n;
        }
        return (
            d.getFullYear() +
            p(d.getMonth() + 1) +
            p(d.getDate()) +
            "-" +
            p(d.getHours()) +
            p(d.getMinutes()) +
            p(d.getSeconds())
        );
    }

    function exportHashes() {
        if (!pastarellaReport.hasReport()) {
            alert("Load a report first");
            return;
        }
        download(`pastarella-hashes-${stamp()}.txt`, buildTxt(pastarellaReport.data));
    }

    window.exportHashes = exportHashes;
})();
