(function () {
  "use strict";

  function makeToggle(panel) {
    var btn = document.createElement("button");
    btn.type = "button";
    btn.className = "btn-collapse";
    btn.title = "Collapse";
    btn.setAttribute("aria-label", "Collapse panel");

    var icon = document.createElement("i");
    icon.className = "bi bi-chevron-up";
    btn.appendChild(icon);

    btn.addEventListener("click", function () {
      var collapsed = panel.classList.toggle("collapsed");
      icon.className = collapsed ? "bi bi-chevron-down" : "bi bi-chevron-up";
      btn.title = collapsed ? "Expand" : "Collapse";
      btn.setAttribute("aria-label", collapsed ? "Expand panel" : "Collapse panel");
    });

    return btn;
  }

  function equipPanel(panel) {
    if (panel.dataset.collapsible === "1") return;
    panel.dataset.collapsible = "1";
    var head = panel.querySelector(":scope > .recent-head");
    if (!head) return;
    var actions = head.querySelector(":scope > .recent-actions");
    if (!actions) {
      actions = document.createElement("div");
      actions.className = "recent-actions";
      while (head.children.length > 1) {
        actions.appendChild(head.children[1]);
      }
      head.appendChild(actions);
    }
    actions.appendChild(makeToggle(panel));
  }

  /* Called by the router after each page render */
  function applyTo(container) {
    if (!container) return;
    container.querySelectorAll(".recent-panel").forEach(equipPanel);
  }

  window.PastarellaCollapse = {
    applyTo: applyTo
  };
})();
