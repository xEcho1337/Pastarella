(function () {
  "use strict";

  function initHomePage() {
    var logo = document.getElementById("homeLogo");
    if (logo && !logo.dataset.done) {
      logo.dataset.done = "1";
      if (Math.random() < 0.05) {
        logo.src = "https://static.gamberorosso.it/2024/04/pastarelle-1024x573.jpg";
      }
      logo.addEventListener("error", function () {
        logo.style.display = "none";
      });
    }

    var line = document.getElementById("homeReportLine");
    if (line) {
      var has = window.PastarellaReport && window.PastarellaReport.hasReport();
      line.classList.toggle("d-none", !has);
      var openBtn = line.querySelector("[data-goto-dashboard]");
      if (openBtn && !openBtn.dataset.bound) {
        openBtn.dataset.bound = "1";
        openBtn.addEventListener("click", function () {
          if (window.PastarellaRouter) window.PastarellaRouter.loadPage("dashboard");
        });
      }
    }
  }

  window.initHomePage = initHomePage;
})();
