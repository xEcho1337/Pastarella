import { renderers } from "./section-renderers.js";

export var sections = [
    {
        id: "services",
        name: "Services",
        description: "Enabled services",
        tableHeaders: [
            "Status",
            "Service Type",
            "Service Name",
            "Display Name",
            "Command",
            "Executable Sha256",
        ],
        renderer: renderers.services,
    },
    {
        id: "network",
        name: "Network",
        description: "Open ports and hosts",
        tables: [
            {
                id: "hosts",
                name: "Hosts",
                description: "Hosts file content",
                tableHeaders: [
                    "IP",
                    "Domain",
                ],
                renderer: renderers.hosts,
            },
            {
                id: "openPorts",
                name: "Open ports",
                description: "What ports are opened and by who",
                tableHeaders: [
                    "Protocol",
                    "State",
                    "Local",
                    "Remote",
                    "PID",
                    "Process Name",
                ],
                renderer: renderers.openPorts,
            },
        ]
    },
    {
        id: "system",
        name: "System",
        description: "Users, storage, processes",
        tables: [
            {
                id: "users",
                name: "Users",
                description: "Local users",
                tableHeaders: [
                    "Name",
                    "Description",
                    "Uid",
                    "Home",
                    "Disabled",
                    "Metadata",
                ],
                renderer: renderers.users,
            },
            {
                id: "storage",
                name: "Storage",
                description: "Drives and storage information",
                tableHeaders: [
                    "Type",
                    "Name",
                    "Free Space",
                    "Total Space",
                ],
                renderer: renderers.storage,
            },
            {
                id: "processes",
                name: "Active processes",
                description: "Running processes",
                tableHeaders: [
                    "Id",
                    "Name",
                    "Path",
                    "Sha256",
                    "Signature",
                    "Start Time",
                    "Metadata",
                ],
                renderer: renderers.processes,
            },
        ]
    },
    {
        id: "persistence",
        name: "Persistence",
        description: "Persistence points",
        tableHeaders: [
            "Risk Score",
            "Name",
            "Path",
            "Action",
            "Trigger",
            "Privilege",
            "Type",
            "Metadata",
        ],
        renderer: renderers.persistences,
    },
    {
        id: "environment",
        name: "Environment",
        description: "Env vars and shell history",
        tables: [
            {
                id: "envs",
                name: "Environment variables",
                description: "The process environ",
                tableHeaders: [
                    "Key",
                    "Value",
                ],
                renderer: renderers.envs,
            },
            {
                id: "commandsHistories",
                name: "Commands history",
                description: "Saved commands histories",
                tableHeaders: [
                    "Shell",
                    "Command",
                ],
                renderer: renderers.commandsHistories,
            },
        ]
    },
    {
        id: "containers",
        name: "Containers",
        description: "Running containers",
        tableHeaders: [
            "Type(s)",
            "Parent PID",
            "Children PIDs",
            "Metadata",
        ],
        renderer: renderers.containers,
    },
    {
        id: "drivers",
        name: "Drivers",
        description: "Drivers and kernel modules present on the system",
        tableHeaders: [
            "Name",
            "Display Name",
            "Identifier",
            "Type",
            "Executable Path",
            "Version",
            "Loaded",
            "Sha256",
            "Signer",
        ],
        renderer: renderers.drivers,
    },
    {
        id: "recentFiles",
        name: "Recent files",
        description: "Last 30 days",
        tableHeaders: [
            "File Path",
            "Creation Time",
            "Last Write Time",
        ],
        renderer: renderers.recentFiles,
    },
];
