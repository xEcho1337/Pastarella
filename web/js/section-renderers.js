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
        fillBody(
            bodyId || "processesBody",
            report.Processes.map(function (d) {
                return [
                    pastarellaReport.v(d, "Id", "id", ""),
                    pastarellaReport.v(d, "Name", "name", ""),
                    pastarellaReport.mono(
                        pastarellaReport.v(d, "Path", "path", "") +
                        " " +
                        pastarellaReport.v(d, "CommandArgs", "commandArgs", ""),
                    ),
                    pastarellaReport.mono(pastarellaReport.v(d, "Sha256", "sha256", "")),
                    pastarellaReport.centered(
                        pastarellaReport.signerIcon(value(d, "Signer", "signer")),
                    ),
                    pastarellaReport.v(d, "StartTime", "startTime", ""),
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
        fillBody(
            "servicesBody",
            report.Services.map(function (d) {
                var args = pastarellaReport.v(d, "Arguments", "arguments", []);
                var cmd =
                    pastarellaReport.v(d, "ExecPath", "execPath", "") +
                    " " +
                    (Array.isArray(args) ? args.join(" ") : args);
                return [
                    pastarellaReport.v(d, "Status", "status", ""),
                    pastarellaReport.v(d, "ServiceType", "serviceType", ""),
                    pastarellaReport.v(d, "ServiceName", "serviceName", ""),
                    pastarellaReport.v(d, "DisplayName", "displayName", ""),
                    pastarellaReport.mono(cmd.trim()),
                    pastarellaReport.mono(pastarellaReport.v(d, "Sha256", "sha256", "")),
                ];
            }),
        );
    }

    /* Open Ports: Protocol, State, Local, Remote, PID, Process Name */
    function renderPorts(report, bodyId) {
        fillBody(
            bodyId || "portsBody",
            report.OpenPorts.map(function (d) {
                var local = pastarellaReport.v(d, "Local", "local", {});
                var remote = pastarellaReport.v(d, "Remote", "remote", null);
                var state = pastarellaReport.v(d, "State", "state", "");
                return [
                    pastarellaReport.v(d, "Protocol", "protocol", ""),
                    state === "–" ? "" : state,
                    pastarellaReport.v(local, "Ip", "ip", "") +
                    ":" +
                    pastarellaReport.v(local, "Port", "port", ""),
                    remote
                        ? pastarellaReport.v(remote, "Ip", "ip", "") +
                        ":" +
                        pastarellaReport.v(remote, "Port", "port", "")
                        : "",
                    pastarellaReport.v(d, "ProcessId", "processId", ""),
                    pastarellaReport.v(d, "ProcessName", "processName", ""),
                ];
            }),
        );
    }

    /* Users: Name, Description, Uid, Home, Disabled, Metadata */
    function renderUsers(report, bodyId) {
        fillBody(
            bodyId || "usersBody",
            report.Users.map(function (d) {
                var disabled = value(d, "Disabled", "disabled");
                return [
                    pastarellaReport.v(d, "Name", "name", ""),
                    pastarellaReport.v(d, "Description", "description", ""),
                    pastarellaReport.v(d, "Uid", "uid", ""),
                    pastarellaReport.mono(pastarellaReport.v(d, "Home", "home", "")),
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
        fillBody(
            bodyId || "hostsBody",
            report.Hosts.map(function (d) {
                return [
                    pastarellaReport.v(d, "Ip", "ip", ""),
                    pastarellaReport.v(d, "Domain", "domain", ""),
                ];
            }),
        );
    }

    /* Drivers: Name, Display Name, Identifier, Type, Executable Path, Version, Loaded, SHA256, Signer */
    function renderDrivers(report) {
        fillBody(
            "driversBody",
            report.Drivers.map(function (d) {
                var loaded = value(d, "Loaded", "loaded");
                return [
                    pastarellaReport.v(d, "Name", "name", ""),
                    pastarellaReport.v(d, "DisplayName", "displayName", ""),
                    pastarellaReport.mono(pastarellaReport.v(d, "Identifier", "identifier", "")),
                    pastarellaReport.v(d, "Type", "type", ""),
                    pastarellaReport.mono(
                        pastarellaReport.v(d, "ExecutablePath", "executablePath", ""),
                    ),
                    pastarellaReport.v(d, "Version", "version", ""),
                    pastarellaReport.centered(
                        pastarellaReport.booleanIcon(
                            loaded === true || loaded === "true",
                        ),
                    ),
                    pastarellaReport.mono(pastarellaReport.v(d, "Sha256", "sha256", "")),
                    pastarellaReport.centered(
                        pastarellaReport.signerIcon(value(d, "Signer", "signer")),
                    ),
                ];
            }),
        );
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
                pastarellaReport.v(d, "Name", "name", ""),
                pastarellaReport.mono(pastarellaReport.v(d, "Path", "path", "")),
                pastarellaReport.preline(pastarellaReport.actionText(value(d, "Action", "action"))),
                pastarellaReport.v(d, "Trigger", "trigger", ""),
                pastarellaReport.v(d, "Privilege", "privilege", ""),
                pastarellaReport.v(d, "Type", "type", ""),
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
        fillBody(
            bodyId || "storageBody",
            report.Storages.map(function (d) {
                return [
                    pastarellaReport.v(d, "Type", "type", ""),
                    pastarellaReport.v(d, "Name", "name", ""),
                    pastarellaReport.fmtGB(pastarellaReport.v(d, "FreeSpace", "freeSpace", NaN)),
                    pastarellaReport.fmtGB(pastarellaReport.v(d, "TotalSpace", "totalSpace", NaN)),
                ];
            }),
        );
    }

    /* Envs: Key, Value */
    function renderEnvs(report) {
        var rows = Object.entries(report.Envs || {}).map(function (entry) {
            return [pastarellaReport.mono(entry[0]), entry[1]];
        });
        fillBody("envBody", rows);
    }

    /* Command Histories: Shell, Command */
    function renderHistories(report) {
        var rows = [];
        report.CommandHistories.forEach(function (history) {
            var shell = pastarellaReport.v(history, "Shell", "shell", "");
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
        fillBody(
            bodyId || "recentFilesBody",
            report.RecentFiles.map(function (d) {
                return [
                    pastarellaReport.mono(pastarellaReport.v(d, "FilePath", "filePath", "")),
                    pastarellaReport.v(d, "CreationTime", "creationTime", ""),
                    pastarellaReport.v(d, "LastWriteTime", "lastWriteTime", ""),
                ];
            }),
        );
    }

    /* Containers: Type(s), Parent PID, Children PIDs, Metadata */
    function renderContainers(report, bodyId) {
        fillBody(
            bodyId || "containersBody",
            report.Containers.map(function (d) {
                return [
                    pastarellaReport.mono(pastarellaReport.v(d, "Type", "type", "")),
                    pastarellaReport.v(d, "ParentPID", "parentPID", ""),
                    pastarellaReport.v(d, "ChildrenPIDs", "childrenPIDs", ""),
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
