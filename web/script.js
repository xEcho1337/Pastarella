const input = document.getElementById("json-file-to-load");

function signerIcon(signer) {
  if (signer)
    return `<i class="bi bi-check-circle-fill text-success" title="${signer}"></i>`;
  else return '<i class="bi bi-x-circle-fill text-danger"></i>';
}

function booleanIcon(bool) {
  if (bool) return '<i class="bi bi-check-circle-fill text-success"></i>';
  else return '<i class="bi bi-x-circle-fill text-danger"></i>';
}

function appendArrToTable(table, arr) {
  const tr = document.createElement("tr");

  arr.forEach((d) => {
    const td = document.createElement("td");
    td.innerHTML = d;
    tr.appendChild(td);
  });

  table.appendChild(tr);
}

function stringifyMetadata(metadata) {
  let str = "";

  for (const [key, value] of Object.entries(metadata))
    str += `${key}: ${value}\n`;

  return str.trimEnd();
}

input.addEventListener("change", async (event) => {
  input.disabled = true;

  document.getElementById("load-file-container").style.display = "none";
  document.getElementsByTagName("header")[0].style.display = "block";
  document.getElementById("report-infos").style.display = "block";

  const file = event.target.files[0];
  if (!file) {
    alert("You need to choose a file");
    return;
  }

  const data = JSON.parse(await file.text());

  data["Processes"].forEach((d) =>
    appendArrToTable(document.getElementById("processes"), [
      d["Id"],
      d["Name"],
      d["Path"],
      d["Sha256"],
      signerIcon(d["Signer"]),
      d["StartTime"],
      stringifyMetadata(d["Metadata"]),
    ]),
  );

  data["Services"].forEach((d) =>
    appendArrToTable(document.getElementById("services"), [
      d["Status"],
      d["ServiceType"],
      d["ServiceName"],
      d["DisplayName"],
      `${d["ExecPath"]} ${d["Arguments"].join(" ")}`,
      d["Sha256"],
    ]),
  );

  data["OpenPorts"].forEach((d) => {
    let state = d["State"] != null ? d["State"] : "";
    let remote =
      d["Remote"] != null ? `${d["Remote"]["Ip"]}:${d["Remote"]["Port"]}` : "";

    appendArrToTable(document.getElementById("open-ports"), [
      d["Protocol"],
      state,
      `${d["Local"]["Ip"]}:${d["Local"]["Port"]}`,
      remote,
      d["ProcessId"],
      d["ProcessName"],
    ]);
  });

  data["Users"].forEach((d) =>
    appendArrToTable(document.getElementById("users"), [
      d["Name"],
      d["Description"],
      d["Uid"],
      d["Home"],
      booleanIcon(d["Disabled"]),
      stringifyMetadata(d["Metadata"]),
    ]),
  );

  data["Hosts"].forEach((d) =>
    appendArrToTable(document.getElementById("hosts"), [d["Ip"], d["Domain"]]),
  );

  data["Drivers"].forEach((d) =>
    appendArrToTable(document.getElementById("drivers"), [
      d["Name"],
      d["DisplayName"],
      d["Identifier"],
      d["Type"],
      d["ExecutablePath"],
      d["Version"],
      booleanIcon(d["Loaded"]),
      d["Sha256"],
      signerIcon(d["Signer"]),
    ]),
  );

  data["Persistences"].forEach((d) => {
    const action = d["Action"];

    let actionStr = "None";
    // TODO: email action
    if (action.Path !== undefined)
      actionStr = `Scheduled run executable\nPath: ${action.Path}\nSHA256: ${action.Sha256}`;
    else if (action.ClassId !== undefined)
      actionStr = `Scheduled COM\nClass ID: ${action.ClassId}\nClass name: ${action.ClassName}`;
    else if (action.Title !== undefined)
      actionStr = `Scheduled message\nTitle: ${action.Title}\nMessage: ${action.Message}`;

    appendArrToTable(document.getElementById("persistences"), [
      d["RiskScore"],
      d["Name"],
      d["Path"],
      actionStr,
      d["Trigger"],
      d["Privilege"],
      d["Type"],
      stringifyMetadata(d["Metadata"]),
    ]);
  });

  data["Storages"].forEach((d) =>
    appendArrToTable(document.getElementById("storages"), [
      d["Type"],
      d["Name"],
      `${Number(d["FreeSpace"]) / Math.pow(1024, 3)} GB`,
      `${Number(d["TotalSpace"]) / Math.pow(1024, 3)} GB`,
    ]),
  );

  Object.entries(data["Envs"]).forEach(([k, v]) =>
    appendArrToTable(document.getElementById("envs"), [k, v]),
  );

  data["CommandHistories"].forEach(history => {
    history["Commands"].forEach(cmd =>
      appendArrToTable(document.getElementById("command-histories"), [history["Shell"], cmd]),
    );
  });

  data["RecentFiles"].forEach((d) =>
    appendArrToTable(document.getElementById("recent-files"), [
      d["FilePath"], d["CreationTime"], d["LastWriteTime"],
    ]),
  );
});

const headings = document.querySelectorAll("#report-infos h1");
const navLinks = document.querySelectorAll(".navbar-nav .nav-link");
const navbarHeight = 56;

function updateActiveNav() {
  let activeId = null;
  for (const h of headings) {
    if (h.getBoundingClientRect().top <= navbarHeight + 1)
      activeId = h.textContent.trim().toLowerCase().replace(/\s+/g, "-");
    else break;
  }
  navLinks.forEach((link) =>
    link.classList.toggle(
      "active",
      link.getAttribute("href") === `#${activeId}`,
    ),
  );
}

window.addEventListener("scroll", updateActiveNav, { passive: true });
updateActiveNav();

const logo = document.getElementById("logo");

if (Math.random() < 0.05) {
  logo.src = "https://static.gamberorosso.it/2024/04/pastarelle-1024x573.jpg";
} else {
  logo.src = "./logo.png";
}

document.getElementById('collapse-all').addEventListener('click', () => {
  document.querySelectorAll('details').forEach(d => d.open = false);
});

document.getElementById('uncollapse-all').addEventListener('click', () => {
  document.querySelectorAll('details').forEach(d => d.open = true);
});
