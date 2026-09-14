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

    /* Processes: Id, Name, Path, SHA256, Signature, Start Time, Metadata */
    function value(d, upper, lower) {
        if (d[upper] !== undefined && d[upper] !== null) return d[upper];
        if (d[lower] !== undefined && d[lower] !== null) return d[lower];
        return null;
    }

    function renderProcesses(report, bodyId) {
        if (report.Processes == null)
            return;

        fillBody(
            bodyId || "processesBody",
            report.Processes.map(function (d) {
                return [
                    pastarellaReport.v(d, "Id"),
                    pastarellaReport.v(d, "Name"),
                    pastarellaReport.mono(
                        pastarellaReport.v(d, "ExePath").NormalizedValue +
                        " " +
                        pastarellaReport.v(d, "CommandArgs", ""),
                    ),
                    pastarellaReport.mono(pastarellaReport.v(d, "ExePath").Sha256),
                    pastarellaReport.centered(
                        pastarellaReport.signerIcon(value(d, "ExePath", "exePath").Signature),
                    ),
                    pastarellaReport.v(d, "StartTime"),
                    pastarellaReport.preline(
                        pastarellaReport.stringifyMetadata(
                            value(d, "Metadata", "metadata") || {},
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
                var args = pastarellaReport.v(d, "Arguments", "arguments", []);
                var cmd =
                    pastarellaReport.v(d, "ExePath").NormalizedValue +
                    " " +
                    (Array.isArray(args) ? args.join(" ") : args);
                return [
                    pastarellaReport.v(d, "Status"),
                    pastarellaReport.v(d, "ServiceType"),
                    pastarellaReport.v(d, "ServiceName"),
                    pastarellaReport.v(d, "DisplayName"),
                    pastarellaReport.mono(cmd.trim()),
                    pastarellaReport.mono(pastarellaReport.v(d, "ExePath").Sha256),
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
                var local = pastarellaReport.v(d, "Local", "local", {});
                var remote = pastarellaReport.v(d, "Remote", "remote", null);
                var state = pastarellaReport.v(d, "State");
                return [
                    pastarellaReport.v(d, "Protocol"),
                    state === "–" ? "" : state,
                    pastarellaReport.v(local, "Ip") +
                    ":" +
                    pastarellaReport.v(local, "Port"),
                    remote
                        ? pastarellaReport.v(remote, "Ip") +
                        ":" +
                        pastarellaReport.v(remote, "Port")
                        : "",
                    pastarellaReport.v(d, "ProcessId"),
                    pastarellaReport.v(d, "ProcessName"),
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
                var disabled = value(d, "Disabled", "disabled");
                return [
                    pastarellaReport.v(d, "Name"),
                    pastarellaReport.v(d, "Description"),
                    pastarellaReport.v(d, "Uid"),
                    pastarellaReport.mono(pastarellaReport.v(d, "Home")),
                    pastarellaReport.centered(
                        pastarellaReport.booleanIcon(
                            disabled === true || disabled === "true",
                        ),
                    ),
                    pastarellaReport.preline(
                        pastarellaReport.stringifyMetadata(
                            value(d, "Metadata", "metadata") || {},
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
                    pastarellaReport.v(d, "Ip"),
                    pastarellaReport.v(d, "Domain"),
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
                var loaded = value(d, "Loaded", "loaded");
                var exePath = value(d, "ExePath", "exePath");
                return [
                    pastarellaReport.v(d, "Name"),
                    pastarellaReport.v(d, "DisplayName"),
                    pastarellaReport.mono(pastarellaReport.v(d, "Identifier")),
                    pastarellaReport.v(d, "Type"),
                    pastarellaReport.mono(
                        pastarellaReport.v(d, "ExePath").NormalizedValue,
                    ),
                    pastarellaReport.v(d, "Version"),
                    pastarellaReport.centered(
                        pastarellaReport.booleanIcon(
                            loaded === true || loaded === "true",
                        ),
                    ),
                    pastarellaReport.mono(pastarellaReport.v(d, "ExePath").Sha256),
                    pastarellaReport.centered(
                        pastarellaReport.signerIcon(exePath === null ? null : exePath.Signature),
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
            var score = Number(value(d, "RiskScore", "riskScore")) || 0;
            var badge = document.createElement("span");
            badge.className = "status-badge " + riskClass(score);
            badge.textContent = String(score);
            return [
                badge,
                pastarellaReport.v(d, "Name"),
                pastarellaReport.mono(pastarellaReport.v(d, "Path")),
                pastarellaReport.preline(pastarellaReport.actionText(value(d, "Action", "action"))),
                pastarellaReport.v(d, "Trigger"),
                pastarellaReport.v(d, "Privilege"),
                pastarellaReport.v(d, "Type"),
                pastarellaReport.preline(
                    pastarellaReport.stringifyMetadata(
                        value(d, "Metadata", "metadata") || {},
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
                    pastarellaReport.v(d, "Type"),
                    pastarellaReport.v(d, "Name"),
                    pastarellaReport.fmtGB(pastarellaReport.v(d, "FreeSpace", "freeSpace", NaN)),
                    pastarellaReport.fmtGB(pastarellaReport.v(d, "TotalSpace", "totalSpace", NaN)),
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
            var shell = pastarellaReport.v(history, "Shell");
            var commands = pastarellaReport.v(history, "Commands", "commands", []);
            (Array.isArray(commands) ? commands : [commands]).forEach(
                function (cmd) {
                    rows.push([shell, pastarellaReport.mono(cmd)]);
                },
            );
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
                    pastarellaReport.mono(pastarellaReport.v(d, "FilePath")),
                    pastarellaReport.v(d, "CreationTime"),
                    pastarellaReport.v(d, "LastWriteTime"),
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
                    pastarellaReport.mono(pastarellaReport.v(d, "Type")),
                    pastarellaReport.v(d, "ParentPID"),
                    pastarellaReport.v(d, "ChildrenPIDs"),
                    pastarellaReport.preline(
                        pastarellaReport.stringifyMetadata(
                            value(d, "Metadata", "metadata") || {},
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
