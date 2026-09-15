import { pastarellaReport } from "./pastarella-report.js";
import { renderSection } from "./section-renderers.js";
import { sections } from "./sections.js";

export class Router {
    static #previousSection = null;
    static #sectionTemplate = null;

    static async #getSectionTemplate() {
        if (Router.#sectionTemplate !== null)
            return Router.#sectionTemplate;

        let template = document.createElement("template");
        template.innerHTML = (await Router.#getPageHtml("section")).trim();

        Router.#sectionTemplate = template;
        return Router.#sectionTemplate;
    }

    static #setActiveSection(sectionId) {
        if (Router.#previousSection == null) {
            let link = document.querySelectorAll(".side-link[data-page]")
                .values()
                .find((l) => l.getAttribute("data-page") === sectionId);

            if (link === undefined)
                return;

            link.classList.add("active");
            link.querySelector(".active-dot").style.visibility = "visible";
        } else {
            document.querySelectorAll(".side-link[data-page]").forEach(link => {
                let isActive = link.getAttribute("data-page") === sectionId;

                link.classList.toggle("active", isActive);
                link.querySelector(".active-dot").style.visibility = isActive ? "visible" : "hidden";
            });
        }

        Router.#previousSection = sectionId;
    }

    static async #getPageHtml(pageName) {
        const res = await fetch(`./pages/${pageName}.html`, { cache: "no-store" });
        if (!res.ok)
            throw new Error(`Page '${pageName}' not found`);
        return await res.text();
    }

    static async #loadPage(pageName) {
        let container = document.getElementById("pageContent");
        container.innerHTML = await Router.#getPageHtml(pageName);
    }

    static #renderEmptyReportState() {
        let container = document.getElementById("pageContent");

        container.innerHTML = "";
        container.appendChild(
            document.getElementById("tpl-empty-state").content.cloneNode(true),
        );
    }

    static async loadSection(sectionId) {
        switch (sectionId) {
            case "home":
                {
                    document.getElementById("topbarTitle").textContent = "Home";
                    document.getElementById("topbarSub").textContent = "Welcome";

                    await Router.#loadPage("home");
                    Router.#setActiveSection(sectionId);

                    window.initHomePage();
                }
                return;
            case "dashboard":
                {
                    document.getElementById("topbarTitle").textContent = "Dashboard";
                    document.getElementById("topbarSub").textContent = "Overview";

                    Router.#setActiveSection(sectionId);

                    if (!pastarellaReport.hasReport()) {
                        Router.#renderEmptyReportState();
                        return;
                    }

                    await Router.#loadPage("dashboard");
                    window.initDashboardPage();
                }
                return;
        }

        let section = sections.find(s => s.id === sectionId);
        if (section === undefined)
            return;

        Router.#setActiveSection(sectionId);

        if (!pastarellaReport.hasReport()) {
            Router.#renderEmptyReportState();
            return;
        }

        let container = document.getElementById("pageContent");
        container.innerHTML = "";

        renderSection(container, section, await Router.#getSectionTemplate());

        if (window.PastarellaColumns)
            window.PastarellaColumns.applyToPage(container, sectionId);
        if (window.PastarellaPager)
            window.PastarellaPager.bindHeaders(container);
        if (window.PastarellaCollapse)
            window.PastarellaCollapse.applyTo(container);
        if (window.PastarellaSearch)
            window.PastarellaSearch.bind(container);
    }
}

window.loadSection = Router.loadSection;

document.addEventListener("DOMContentLoaded", () => {
    Router.loadSection("home");
});
