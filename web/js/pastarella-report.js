import { Router } from "./Router.js";

export class PastarellaReport {
    data = null;

    constructor() {
        this.data = null;
    }

    hasReport() {
        return this.data !== null;
    }

    setReport(data) {
        this.data = data;

        let line = document.getElementById("homeReportLine");
        if (line !== null)
            line.classList.toggle("d-none", false);

        return true;
    }

    pickFile(callback) {
        var input = document.createElement("input");
        input.type = "file";
        input.accept = "application/json,.json";
        input.addEventListener("change", () => {
            var file = input.files && input.files[0];
            if (!file) return;
            var reader = new FileReader();
            reader.onload = () => {
                try {
                    var data = JSON.parse(reader.result);
                    if (!this.setReport(data))
                        alert("Invalid AnalysisReport JSON");
                    else if (callback) callback(this.data);
                } catch (e) {
                    console.error(e);
                    alert("Invalid AnalysisReport JSON");
                }
            };
            reader.readAsText(file);
        });
        input.click();
    }

    esc(value) {
        return value === null || value === undefined ? "" : String(value);
    }

    appendCells(tr, cells) {
        cells.forEach((cell) => {
            var td = document.createElement("td");
            if (cell && cell.__element) {
                td.appendChild(cell.__element);
            } else if (
                cell &&
                typeof cell === "object" &&
                cell.nodeType === 1
            ) {
                td.appendChild(cell);
            } else {
                td.textContent = this.esc(cell);
            }
            if (cell && cell.__mono) td.className = "cell-path";
            if (cell && cell.__preline) td.style.whiteSpace = "pre-line";
            if (cell && cell.__center) td.classList.add("icon-cell");
            tr.appendChild(td);
        });
    }

    mono(text) {
        var s = new String(this.esc(text));
        s.__mono = true;
        return s;
    }

    preline(text) {
        var s = new String(this.esc(text));
        s.__preline = true;
        return s;
    }

    centered(element) {
        var s = new String("");
        s.__center = true;
        s.__element = element;
        return s;
    }

    signerIcon(signature) {
        var i = document.createElement("i");
        if (signature) {
            i.className = "bi bi-check-circle-fill text-success";
            i.title = String(this.stringifyMetadata(signature));
        } else {
            i.className = "bi bi-x-circle-fill text-danger";
        }
        return i;
    }

    booleanIcon(bool) {
        var i = document.createElement("i");
        i.className = bool
            ? "bi bi-check-circle-fill text-success"
            : "bi bi-x-circle-fill text-danger";
        return i;
    }

    stringifyMetadata(metadata) {
        if (!metadata) return "";
        var str = "";
        var entries = Array.isArray(metadata)
            ? metadata.map((m, i) => {
                return [i, m];
            })
            : Object.entries(metadata);
        entries.forEach((entry) => {
            str += entry[0] + ": " + entry[1] + "\n";
        });
        return str.trimEnd();
    }

    fmtGB(bytes) {
        var n = Number(bytes);
        if (isNaN(n)) return "–";
        return (n / Math.pow(1024, 3)).toFixed(2) + " GB";
    }

    actionText(action) {
        if (!action) return "None";
        if (action.ExePath !== undefined) {
            var p = action.ExePath;
            if (p === null)
                return "Scheduled run executable";
            return `Scheduled run executable\nPath: ${p.NormalizedValue}\nSha256: ${p.Sha256}\n${this.stringifyMetadata(p.Signature || {})}`
        }
        if (action.ClassId !== undefined || action.classId !== undefined) {
            var id =
                action.ClassId !== undefined ? action.ClassId : action.classId;
            var name =
                action.ClassName !== undefined
                    ? action.ClassName
                    : action.className;
            return "Scheduled COM\nClass ID: " + id + "\nClass name: " + name;
        }
        if (action.Title !== undefined || action.title !== undefined) {
            var title =
                action.Title !== undefined ? action.Title : action.title;
            var msg =
                action.Message !== undefined ? action.Message : action.message;
            return "Scheduled message\nTitle: " + title + "\nMessage: " + msg;
        }
        return "Scheduled email";
    }

    riskClass(score) {
        if (score >= 70) return "status-err";
        if (score >= 40) return "status-warn";
        return "status-ok";
    }
}

export var pastarellaReport = new PastarellaReport();
document.addEventListener("click", (e) => {
    var el = e.target.closest
        ? e.target.closest('[data-action="load-report"]')
        : null;
    if (el) {
        e.preventDefault();
        pastarellaReport.pickFile();
    }
});
