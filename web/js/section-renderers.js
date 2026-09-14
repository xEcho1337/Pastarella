(function () {
    "use strict";

    function Report() {
        return window.PastarellaReport;
    }

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
            Report().appendCells(tr, cells);
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
                    Report().v(d, "Id", "id", ""),
                    Report().v(d, "Name", "name", ""),
                    Report().mono(
                        Report().v(d, "Path", "path", "") +
                            " " +
                            Report().v(d, "CommandArgs", "commandArgs", ""),
                    ),
                    Report().mono(R().v(d, "Sha256", "sha256", "")),
                    Report().centered(
                        R().signerIcon(value(d, "Signer", "signer")),
                    ),
                    Report().v(d, "StartTime", "startTime", ""),
                    Report().preline(
                        Report().stringifyMetadata(
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
                var args = Report().v(d, "Arguments", "arguments", []);
                var cmd =
                    Report().v(d, "ExecPath", "execPath", "") +
                    " " +
                    (Array.isArray(args) ? args.join(" ") : args);
                return [
                    Report().v(d, "Status", "status", ""),
                    Report().v(d, "ServiceType", "serviceType", ""),
                    Report().v(d, "ServiceName", "serviceName", ""),
                    Report().v(d, "DisplayName", "displayName", ""),
                    Report().mono(cmd.trim()),
                    Report().mono(R().v(d, "Sha256", "sha256", "")),
                ];
            }),
        );
    }

    /* Open Ports: Protocol, State, Local, Remote, PID, Process Name */
    function renderPorts(report, bodyId) {
        fillBody(
            bodyId || "portsBody",
            report.OpenPorts.map(function (d) {
                var local = Report().v(d, "Local", "local", {});
                var remote = Report().v(d, "Remote", "remote", null);
                var state = Report().v(d, "State", "state", "");
                return [
                    Report().v(d, "Protocol", "protocol", ""),
                    state === "–" ? "" : state,
                    Report().v(local, "Ip", "ip", "") +
                        ":" +
                        Report().v(local, "Port", "port", ""),
                    remote
                        ? Report().v(remote, "Ip", "ip", "") +
                          ":" +
                          Report().v(remote, "Port", "port", "")
                        : "",
                    Report().v(d, "ProcessId", "processId", ""),
                    Report().v(d, "ProcessName", "processName", ""),
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
                    Report().v(d, "Name", "name", ""),
                    Report().v(d, "Description", "description", ""),
                    Report().v(d, "Uid", "uid", ""),
                    Report().mono(R().v(d, "Home", "home", "")),
                    Report().centered(
                        Report().booleanIcon(
                            disabled === true || disabled === "true",
                        ),
                    ),
                    Report().preline(
                        Report().stringifyMetadata(
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
                    Report().v(d, "Ip", "ip", ""),
                    Report().v(d, "Domain", "domain", ""),
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
                    Report().v(d, "Name", "name", ""),
                    Report().v(d, "DisplayName", "displayName", ""),
                    Report().mono(R().v(d, "Identifier", "identifier", "")),
                    Report().v(d, "Type", "type", ""),
                    Report().mono(
                        R().v(d, "ExecutablePath", "executablePath", ""),
                    ),
                    Report().v(d, "Version", "version", ""),
                    Report().centered(
                        Report().booleanIcon(
                            loaded === true || loaded === "true",
                        ),
                    ),
                    Report().mono(R().v(d, "Sha256", "sha256", "")),
                    Report().centered(
                        R().signerIcon(value(d, "Signer", "signer")),
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
                Report().v(d, "Name", "name", ""),
                Report().mono(R().v(d, "Path", "path", "")),
                Report().preline(R().actionText(value(d, "Action", "action"))),
                Report().v(d, "Trigger", "trigger", ""),
                Report().v(d, "Privilege", "privilege", ""),
                Report().v(d, "Type", "type", ""),
                Report().preline(
                    Report().stringifyMetadata(
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
                    Report().v(d, "Type", "type", ""),
                    Report().v(d, "Name", "name", ""),
                    Report().fmtGB(R().v(d, "FreeSpace", "freeSpace", NaN)),
                    Report().fmtGB(R().v(d, "TotalSpace", "totalSpace", NaN)),
                ];
            }),
        );
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
            var shell = Report().v(history, "Shell", "shell", "");
            var commands = Report().v(history, "Commands", "commands", []);
            (Array.isArray(commands) ? commands : [commands]).forEach(
                function (cmd) {
                    rows.push([shell, Report().mono(cmd)]);
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
                    Report().mono(R().v(d, "FilePath", "filePath", "")),
                    Report().v(d, "CreationTime", "creationTime", ""),
                    Report().v(d, "LastWriteTime", "lastWriteTime", ""),
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
                    Report().mono(R().v(d, "Type", "type", "")),
                    Report().v(d, "ParentPID", "parentPID", ""),
                    Report().v(d, "ChildrenPIDs", "childrenPIDs", ""),
                    Report().preline(
                        Report().stringifyMetadata(
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
