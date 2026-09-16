import { pastarellaReport } from "./pastarella-report.js";
import { Router } from "./Router.js";

const steps = [
    {
        name: "Dashboard",
        description: "See overall statistics of your report and risk scores",
        selector: '.side-link[data-page="dashboard"]',
        page: "dashboard",
        requiresReport: true
    },
    {
        name: "Sections",
        description: "Every section contains a specific part of the report",
        selector: '.side-link[data-page="services"]',
        page: "services",
        requiresReport: true
    },
    {
        name: "Search bar",
        description: "Search for strings across all columns or specific columns",
        selector: '#pageContent .table-search-input',
        page: "services",
        requiresReport: true
    },
    {
        name: "Table actions",
        description: "Resize, move or toggle the visibility of individual columns, the way you like it!",
        selector: '#pageContent .recent-panel:first-of-type .section-headers',
        page: "services",
        requiresReport: true
    }
]

let currentStep = 0;
let totalSteps = steps.length;
let overlay = null;
let popup = null;
let highlighted = null;

function canStartTour() {
    return pastarellaReport.hasReport();
}

async function ensureStepTarget(step) {
    if (step.requiresReport && !pastarellaReport.hasReport())
        return null;

    if (step.page)
        await Router.loadSection(step.page);

    return document.querySelector(step.selector);
}

function createTourDom() {
    if (!overlay) {
        overlay = document.createElement("div");
        overlay.className = "tour-overlay";
        overlay.addEventListener("click", endTour);
    }
    if (!popup) {
        popup = document.createElement("div");
        popup.className = "tour-popup";
        popup.innerHTML = `
            <h3></h3>
            <p></p>
            <div class="tour-meta"></div>
            <div class="tour-actions">
                <button type="button" class="tour-back">Back</button>
                <button type="button" class="tour-close">Close</button>
                <button type="button" class="tour-next">Next</button>
            </div>`;
        popup.querySelector(".tour-back").addEventListener("click", prevStep);
        popup.querySelector(".tour-next").addEventListener("click", nextStep);
        popup.querySelector(".tour-close").addEventListener("click", endTour);
    }
}

function clearHighlight() {
    if (highlighted) {
        highlighted.classList.remove("tour-highlight");
        highlighted = null;
    }
}

function positionPopup(target) {
    const gap = 12;
    const rect = target.getBoundingClientRect();

    popup.style.visibility = "hidden";
    popup.style.left = "0px";
    popup.style.top = "0px";
    const popRect = popup.getBoundingClientRect();

    // default: bottom, flip up if there is no space
    let top = rect.bottom + gap;
    if (top + popRect.height > window.innerHeight - 8)
        top = rect.top - popRect.height - gap;

    let left = rect.left + rect.width / 2 - popRect.width / 2;
    left = Math.max(8, Math.min(left, window.innerWidth - popRect.width - 8));
    top = Math.max(8, top);

    popup.style.left = `${left}px`;
    popup.style.top = `${top}px`;
    popup.style.visibility = "";
}

async function showStep(index) {
    createTourDom();
    currentStep = index;
    const step = steps[index];

    const target = await ensureStepTarget(step);
    if (!target) {
        endTour();
        alert("Tour target not found. Load a report first.");
        return;
    }

    target.scrollIntoView({ block: "center", behavior: "smooth" });
    await new Promise((r) => setTimeout(r, 250));

    clearHighlight();
    highlighted = target;
    target.classList.add("tour-highlight");

    if (!overlay.isConnected) document.body.appendChild(overlay);
    if (!popup.isConnected) document.body.appendChild(popup);

    popup.querySelector("h3").textContent = step.name;
    popup.querySelector("p").textContent = step.description;
    popup.querySelector(".tour-meta").textContent = `Step ${index + 1} of ${totalSteps}`;
    popup.querySelector(".tour-back").disabled = index === 0;
    popup.querySelector(".tour-next").textContent = index === totalSteps - 1 ? "Finish" : "Next";

    positionPopup(target);
}

function repositionCurrent() {
    if (!popup || !popup.isConnected || !highlighted) return;
    positionPopup(highlighted);
}

async function nextStep() {
    if (currentStep >= totalSteps - 1) {
        endTour();
        return;
    }
    await showStep(currentStep + 1);
}

async function prevStep() {
    if (currentStep <= 0) return;
    await showStep(currentStep - 1);
}

function endTour() {
    clearHighlight();
    if (overlay && overlay.isConnected) overlay.remove();
    if (popup && popup.isConnected) popup.remove();
}

async function startTour() {
    if (!canStartTour()) {
        alert("Load a report first to take the tour");
        return false;
    }

    await showStep(0);
    return true;
}

document.addEventListener("click", (e) => {
    const btn = e.target.closest ? e.target.closest('[data-action="take-a-tour"]') : null;
    if (btn) {
        e.preventDefault();
        startTour();
    }
});

document.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && popup && popup.isConnected) endTour();
});
window.addEventListener("resize", repositionCurrent);
window.addEventListener("scroll", repositionCurrent, true);

window.PastarellaTour = { steps, startTour, ensureStepTarget, canStartTour, showStep, endTour };
