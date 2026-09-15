import { pastarellaReport } from "./pastarella-report.js";

function push(hashes, hash) {
    if (hash !== null && hash !== undefined)
        hashes.add(hash);
}

function collectSessionHashes(iterable, section, lines, action) {
    const hashes = new Set();
    iterable.forEach(v => action(hashes, v));

    lines.push(`=========== ${section} ===========`)
    lines.push(...hashes)
    lines.push(` `)
}

function collectHashes(report, lines) {
    collectSessionHashes(report.Processes, "Processes", lines, (h, v) => push(h, v.ExePath?.Sha256));
    collectSessionHashes(report.Services, "Services", lines, (h, v) => push(h, v.ExePath?.Sha256));
    collectSessionHashes(report.Drivers, "Drivers", lines, (h, v) => push(h, v.ExePath?.Sha256));
    collectSessionHashes(report.Persistences, "Persistences", lines, (h, v) => {
        const action = v.Action;
        if (action === undefined)
            return;

        push(h, action.ExePath?.Sha256);
    })
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

    collectHashes(report, lines);
    return lines.join("\n");
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

function download(filename, text) {
    var blob = new Blob([text], {
        type: "text/plain;charset=utf-8",
    });

    var url = URL.createObjectURL(blob);
    var a = document.createElement("a");

    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();

    setTimeout(function() {
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }, 100);
}

function exportHashes() {
    if (!pastarellaReport.hasReport()) {
        alert("Load a report first");
        return;
    }

    download(`pastarella-hashes-${stamp()}.txt`, buildTxt(pastarellaReport.data));
}

window.exportHashes = exportHashes;
