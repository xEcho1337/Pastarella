import { pastarellaReport } from "./pastarella-report.js";

function fillBody(tbody, rows) {
    if (window.PastarellaPager) {
        window.PastarellaPager.setRows(tbody, rows);
        return;
    }

    rows.forEach(function (cells) {
        var tr = document.createElement("tr");
        pastarellaReport.appendCells(tr, cells);
        tbody.appendChild(tr);
    });
}

function value(d, key, defaultValue = null) {
    if (d[key] == null)
        return defaultValue;
    return d[key];
}

/* Processes: Id, Path, SHA256, Signature, Start Time, Metadata */
function renderProcesses(report, tbody) {
    if (report.Processes == null)
        return;

    fillBody(
        tbody,
        report.Processes.map(function (d) {
            let exePath = value(d, "ExePath");

            return [
                value(d, "Id", "-"),
                pastarellaReport.mono(
                    `${exePath.NormalizedValue ?? ""} ${value(d, "CommandArgs", [""]).join(' ')}`
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
function renderServices(report, tbody) {
    if (report.Services == null)
        return;

    fillBody(
        tbody,
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

/* Sockets: Protocol, State, Local, Remote, PID, Process Name */
function renderSockets(report, tbody) {
    if (report.Sockets == null)
        return;

    fillBody(
        tbody,
        report.Sockets.map(function (d) {
            const protocol = value(d, "Protocol");
            const local = value(d, "Local");
            const remote = value(d, "Remote");

            return [
                protocol.Name,
                protocol.State ?? "-",
                pastarellaReport.stringifyAddress(local),
                remote
                    ? pastarellaReport.stringifyAddress(remote)
                    : "-",
                value(d, "PID", "-"),
                value(d, "ProcessName", "-"),
            ];
        }),
    );
}

/* Users: Name, Description, Uid, Home, Disabled, Metadata */
function renderUsers(report, tbody) {
    if (report.Users == null)
        return;

    fillBody(
        tbody,
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
function renderHosts(report, tbody) {
    if (report.Hosts == null)
        return;

    fillBody(
        tbody,
        report.Hosts.map(function (d) {
            return [
                value(d, "Ip"),
                value(d, "Domain"),
            ];
        }),
    );
}

/* Drivers: Name, Display Name, Identifier, Type, Executable Path, Version, Loaded, SHA256, Signer */
function renderDrivers(report, tbody) {
    if (report.Drivers == null)
        return;

    fillBody(
        tbody,
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
function renderPersistences(report, tbody) {
    if (report.Persistences == null)
        return;

    var rows = report.Persistences.map(function (d) {
        let score = Number(value(d, "RiskScore")) || 0;
        let badge = document.createElement("span");

        badge.className = "status-badge " + pastarellaReport.riskClass(score);
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
    fillBody(tbody, rows);
}

/* Storages: Type, Name, Free Space, Total Space */
function renderStorage(report, tbody) {
    if (report.Storages == null)
        return;

    fillBody(
        tbody,
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
function renderEnvs(report, tbody) {
    if (report.Envs == null)
        return;

    var rows = Object.entries(report.Envs).map(function (entry) {
        return [pastarellaReport.mono(entry[0]), entry[1]];
    });
    fillBody(tbody, rows);
}

/* Command Histories: Shell, Command */
function renderCommandsHistories(report, tbody) {
    if (report.CommandHistories == null)
        return;

    var rows = [];
    report.CommandHistories.forEach(function (history) {
        let shell = value(history, "Shell");
        value(history, "Commands").forEach((cmd) => {
            rows.push([shell, pastarellaReport.mono(cmd)]);
        });
    });
    fillBody(tbody, rows);
}

/* Recent Files: File Path, Creation Time, Last Write Time */
function renderRecentFiles(report, tbody) {
    if (report.RecentFiles == null)
        return;

    fillBody(
        tbody,
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
function renderContainers(report, tbody) {
    if (report.Containers == null)
        return;

    fillBody(
        tbody,
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

function renderTable(templateElement, table) {
    const panel = templateElement.querySelector(".recent-panel");

    panel.querySelector(".recent-head .panel-title").textContent = table.name;
    panel.querySelector(".recent-head .panel-sub").textContent = table.description;

    const theadTr = panel.querySelector(".table-responsive .section-headers");
    const tbody = panel.querySelector(".table-responsive .section-body");
    tbody.id = `${table.id}Body`;

    table.tableHeaders.forEach(header => {
        let th = document.createElement("th");
        th.textContent = header;

        theadTr.appendChild(th);
    });

    table.renderer(pastarellaReport.data, tbody);
}

export function renderSection(parentElement, section, template) {
    let sectionTitle = document.getElementById("topbarTitle");
    let sectionDescription = document.getElementById("topbarSub");

    sectionTitle.textContent = section.name;
    sectionDescription.textContent = section.description;

    if (section.tables !== undefined) {
        section.tables.forEach((t) => {
            const templateNode = template.content.cloneNode(true);
            renderTable(templateNode, t);
            parentElement.appendChild(templateNode);
        });
    }
    else {
        const templateNode = template.content.cloneNode(true);
        renderTable(templateNode, section);
        parentElement.appendChild(templateNode);
    }
};

export var renderers = {
    services: (report, tbody) => renderServices(report, tbody),
    hosts: (report, tbody) => renderHosts(report, tbody),
    sockets: (report, tbody) => renderSockets(report, tbody),
    users: (report, tbody) => renderUsers(report, tbody),
    storage: (report, tbody) => renderStorage(report, tbody),
    processes: (report, tbody) => renderProcesses(report, tbody),
    persistences: (report, tbody) => renderPersistences(report, tbody),
    envs: (report, tbody) => renderEnvs(report, tbody),
    commandsHistories: (report, tbody) => renderCommandsHistories(report, tbody),
    containers: (report, tbody) => renderContainers(report, tbody),
    drivers: (report, tbody) => renderDrivers(report, tbody),
    recentFiles: (report, tbody) => renderRecentFiles(report, tbody),
};
