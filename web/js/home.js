import { pastarellaReport } from "./pastarella-report.js";

(function () {
    "use strict";

    function initHomePage() {
        var logo = document.getElementById("homeLogo");
        if (logo && !logo.dataset.done) {
            logo.dataset.done = "1";
            // 1/20 chance to get the cool logo
            if (Math.random() < 0.05) {
                logo.src =
                    "https://static.gamberorosso.it/2024/04/pastarelle-1024x573.jpg";
            }
            logo.addEventListener("error", function () {
                logo.style.display = "none";
            });
        }

        let line = document.getElementById("homeReportLine");
        if (line !== null)
            line.classList.toggle("d-none", !pastarellaReport.hasReport());
    }

    window.initHomePage = initHomePage;
})();
