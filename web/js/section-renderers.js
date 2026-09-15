import { pastarellaReport } from "./pastarella-report.js";

(function () {
    "use strict";

    var HEADERS = {
        servicesBody: [
            "Status",
            "Service Type",
            "Service Name",
            "Display Name",
            "Command",
            "Executable SHA256",
        ],
        portsBody: [
            "Protocol",
            "State",
            "Local",
            "Remote",
            "PID",
            "Process Name",
        ],
        hostsBody: ["IP", "Domain"],
        processesBody: [
            "Id",
            "Name",
            "Path",
            "SHA256",
            "Signature",
            "Start Time",
            "Metadata",
        ],
        usersBody: [
            "Name",
            "Description",
            "Uid",
            "Home",
            "Disabled",
            "Metadata",
        ],
        storageBody: ["Type", "Name", "Free Space", "Total Space"],
        persistenceBody: [
            "Risk Score",
            "Name",
            "Path",
            "Action",
            "Trigger",
            "Privilege",
            "Type",
            "Metadata",
        ],
        envBody: ["Key", "Value"],
        historyBody: ["Shell", "Command"],
        driversBody: [
            "Name",
            "Display Name",
            "Identifier",
            "Type",
            "Executable Path",
            "Version",
            "Loaded",
            "SHA256",
            "Signer",
        ],
        recentFilesBody: ["File Path", "Creation Time", "Last Write Time"],
        containersBody: ["Type(s)", "Parent PID", "Children PIDs", "Metadata"],
    };

    function liveHeaderNames(headRow) {
        return Array.prototype.map.call(
            headRow.querySelectorAll("th"),
            function (th) {
                for (var i = 0; i < th.childNodes.length; i++) {
                    var n = th.childNodes[i];
                    if (n.nodeType === 3) return n.textContent.trim();
                }
                return th.textContent.trim();
            },
        );
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
        console.warn(
            "Pastarella: stale table header for #" + bodyId + ", rebuilding",
        );
        headRow.innerHTML = "";
        expected.forEach(function (name) {
            var th = document.createElement("th");
            th.textContent = name;
            headRow.appendChild(th);
        });
    }

    function fillBody(id, rows) {
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
            pastarellaReport.appendCells(tr, cells);
            body.appendChild(tr);
        });
    }

    function value(d, key, defaultValue = null) {
        if (d[key] == null)
            return defaultValue;
        return d[key];
    }

    /* Processes: Id, Name, Path, SHA256, Signature, Start Time, Metadata */
    function renderProcesses(report, bodyId) {
        if (report.Processes == null)
            return;

        fillBody(
            bodyId || "processesBody",
            report.Processes.map(function (d) {
                let exePath = value(d, "ExePath");

                return [
                    value(d, "Id", "-"),
                    value(d, "Name", "-"),
                    pastarellaReport.mono(
                        `${exePath.NormalizedValue ?? ""} ${value(d, "CommandArgs", "")}`
                    ),
                    pastarellaReport.mono(exePath.Sha256 ?? "-"),
                    pastarellaReport.centered(
                        pastarellaReport.signerIcon(exePath.Signature),
                    ),
                    value(d, "StartTime", "-"),
                    pastarellaReport.preline(
                        pastarellaReport.stringifyMetadata(
                            value(d, "Metadata", {}),
                        ),
                    ),
                ];
            }),
        );
    }

    /* Services: Status, Service Type, Service Name, Display Name, Command, Executable SHA256 */
    function renderServices(report) {
        if (report.Services == null)
            return;

        fillBody(
            "servicesBody",
            report.Services.map(function (d) {
                let exePath = value(d, "ExePath");

                return [
                    value(d, "Status", "-"),
                    value(d, "ServiceType", "-"),
                    value(d, "ServiceName", "-"),
                    value(d, "DisplayName", "-"),
                    pastarellaReport.mono(
                        `${exePath?.NormalizedValue ?? ""} ${value(d, "Arguments", []).join(' ')}`.trim()
                    ),
                    pastarellaReport.mono(exePath?.Sha256 ?? "-"),
                ];
            }),
        );
    }

    /* Open Ports: Protocol, State, Local, Remote, PID, Process Name */
    function renderPorts(report, bodyId) {
        if (report.OpenPorts == null)
            return;

        fillBody(
            bodyId || "portsBody",
            report.OpenPorts.map(function (d) {
                let local = value(d, "Local");
                let remote = value(d, "Remote");

                return [
                    value(d, "Protocol", "-"),
                    value(d, "State"),
                    `${value(local, "Ip")}:${value(local, "Port")}`,
                    remote
                        ? `${value(remote, "Ip")}:${value(remote, "Port")}`
                        : "",
                    value(d, "ProcessId", "-"),
                    value(d, "ProcessName", "-"),
                ];
            }),
        );
    }

    /* Users: Name, Description, Uid, Home, Disabled, Metadata */
    function renderUsers(report, bodyId) {
        if (report.Users == null)
            return;

        fillBody(
            bodyId || "usersBody",
            report.Users.map(function (d) {
                return [
                    value(d, "Name"),
                    value(d, "Description"),
                    value(d, "Uid"),
                    pastarellaReport.mono(value(d, "Home")),
                    pastarellaReport.centered(
                        pastarellaReport.booleanIcon(value(d, "Disabled")),
                    ),
                    pastarellaReport.preline(
                        pastarellaReport.stringifyMetadata(
                            value(d, "Metadata", {}),
                        ),
                    ),
                ];
            }),
        );
    }

    /* Hosts: IP, Domain */
    function renderHosts(report, bodyId) {
        if (report.Hosts == null)
            return;

        fillBody(
            bodyId || "hostsBody",
            report.Hosts.map(function (d) {
                return [
                    value(d, "Ip"),
                    value(d, "Domain"),
                ];
            }),
        );
    }

    /* Drivers: Name, Display Name, Identifier, Type, Executable Path, Version, Loaded, SHA256, Signer */
    function renderDrivers(report) {
        if (report.Drivers == null)
            return;

        fillBody(
            "driversBody",
            report.Drivers.map(function (d) {
                let exePath = value(d, "ExePath");

                return [
                    value(d, "Name", "-"),
                    value(d, "DisplayName", "-"),
                    pastarellaReport.mono(value(d, "Identifier", "-")),
                    value(d, "Type", "-"),
                    pastarellaReport.mono(
                        exePath?.NormalizedValue ?? "",
                    ),
                    value(d, "Version", "-"),
                    pastarellaReport.centered(
                        pastarellaReport.booleanIcon(value(d, "Loaded")),
                    ),
                    pastarellaReport.mono(exePath?.Sha256 ?? "-"),
                    pastarellaReport.centered(
                        pastarellaReport.signerIcon(exePath?.Signature ?? null),
                    ),
                ];
            }),
        );
    }

    /* Persistences: Risk Score, Name, Path, Action, Trigger, Privilege, Type, Metadata */
    function renderPersistences(report, bodyId) {
        if (report.Persistences == null)
            return;

        var rows = report.Persistences.map(function (d) {
            let score = Number(value(d, "RiskScore")) || 0;
            let badge = document.createElement("span");

            badge.className = "status-badge " + riskClass(score);
            badge.textContent = String(score);

            return [
                badge,
                value(d, "Name", "-"),
                pastarellaReport.mono(value(d, "Path", "-")),
                pastarellaReport.preline(pastarellaReport.actionText(value(d, "Action"))),
                value(d, "Trigger", "-"),
                value(d, "Privilege", "-"),
                value(d, "Type", "-"),
                pastarellaReport.preline(
                    pastarellaReport.stringifyMetadata(
                        value(d, "Metadata", {}),
                    ),
                ),
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
        if (report.Storages == null)
            return;

        fillBody(
            bodyId || "storageBody",
            report.Storages.map(function (d) {
                return [
                    value(d, "Type", "-"),
                    value(d, "Name", "-"),
                    pastarellaReport.fmtGB(value(d, "FreeSpace", NaN)),
                    pastarellaReport.fmtGB(value(d, "TotalSpace", NaN)),
                ];
            }),
        );
    }

    /* Envs: Key, Value */
    function renderEnvs(report) {
        if (report.Envs == null)
            return;

        var rows = Object.entries(report.Envs).map(function (entry) {
            return [pastarellaReport.mono(entry[0]), entry[1]];
        });
        fillBody("envBody", rows);
    }

    /* Command Histories: Shell, Command */
    function renderHistories(report) {
        if (report.CommandHistories == null)
            return;

        var rows = [];
        report.CommandHistories.forEach(function (history) {
            let shell = value(history, "Shell");
            value(history, "Commands").forEach((cmd) => {
                rows.push([shell, pastarellaReport.mono(cmd)]);
            });
        });
        fillBody("historyBody", rows);
    }

    /* Recent Files: File Path, Creation Time, Last Write Time */
    function renderRecentFiles(report, bodyId) {
        if (report.RecentFiles == null)
            return;

        fillBody(
            bodyId || "recentFilesBody",
            report.RecentFiles.map(function (d) {
                return [
                    pastarellaReport.mono(value(d, "FilePath", "-")),
                    value(d, "CreationTime", "-"),
                    value(d, "LastWriteTime", "-"),
                ];
            }),
        );
    }

    /* Containers: Type(s), Parent PID, Children PIDs, Metadata */
    function renderContainers(report, bodyId) {
        if (report.Containers == null)
            return;

        fillBody(
            bodyId || "containersBody",
            report.Containers.map(function (d) {
                return [
                    pastarellaReport.mono(value(d, "Type", "-")),
                    value(d, "ParentPID", "-"),
                    value(d, "ChildrenPIDs", "-"),
                    pastarellaReport.preline(
                        pastarellaReport.stringifyMetadata(
                            value(d, "Metadata", {}),
                        ),
                    ),
                ];
            }),
        );
    }

    var renderers = {
        services: function (report) {
            renderServices(report);
        },
        network: function (report) {
            renderPorts(report);
            renderHosts(report);
        },
        system: function (report) {
            renderProcesses(report);
            renderUsers(report);
            renderStorages(report);
        },
        persistence: function (report) {
            renderPersistences(report);
        },
        environment: function (report) {
            renderEnvs(report);
            renderHistories(report);
        },
        drivers: function (report) {
            renderDrivers(report);
        },
        "recent-files": function (report) {
            renderRecentFiles(report);
        },
        containers: function (report) {
            renderContainers(report);
        },
    };

    window.PastarellaSections = {
        render: function (name, report) {
            if (renderers[name]) renderers[name](report);
        },
        riskClass: riskClass,
    };
})();
