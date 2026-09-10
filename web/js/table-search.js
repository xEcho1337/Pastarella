(function () {
  "use strict";

  var DEBOUNCE_MS = 150;

  function pager() {
    return window.PastarellaPager;
  }

  function cardTitle(table) {
    var panel = table.closest ? table.closest(".recent-panel") : null;
    var title = panel ? panel.querySelector(".recent-head .panel-title") : null;
    return title ? title.textContent.trim() : "";
  }

  /* Search filters the full dataset (every pager page). The select
     picks one canonical column (indexes match the pager, pre-reorder). */
  function buildBar(table, tbodyId, columns) {
    var bar = document.createElement("div");
    bar.className = "table-search";

    var icon = document.createElement("i");
    icon.className = "bi bi-search table-search-icon";
    bar.appendChild(icon);

    var input = document.createElement("input");
    input.type = "text";
    input.className = "table-search-input";
    var name = cardTitle(table);
    input.placeholder = name ? "Search " + name + "\u2026" : "Search\u2026";
    input.setAttribute("aria-label", "Search table");
    bar.appendChild(input);

    var select = document.createElement("select");
    select.className = "table-search-select";
    select.title = "Column to search in";
    var all = document.createElement("option");
    all.value = "-1";
    all.textContent = "All columns";
    select.appendChild(all);
    columns.forEach(function (col, i) {
      var opt = document.createElement("option");
      opt.value = String(i);
      opt.textContent = col;
      select.appendChild(opt);
    });
    bar.appendChild(select);

    var clear = document.createElement("button");
    clear.type = "button";
    clear.className = "table-search-clear";
    clear.title = "Clear search";
    clear.textContent = "\u00d7";
    clear.style.display = "none";
    bar.appendChild(clear);

    var timer = null;
    function apply() {
      pager().setFilter(tbodyId, input.value, Number(select.value));
      clear.style.display = input.value ? "" : "none";
    }
    input.addEventListener("input", function () {
      if (timer) clearTimeout(timer);
      timer = setTimeout(apply, DEBOUNCE_MS);
    });
    select.addEventListener("change", apply);
    clear.addEventListener("click", function () {
      input.value = "";
      apply();
      input.focus();
    });
    return bar;
  }

  /* One searchbar per card: inside the header, between the
     title and the actions (collapse toggle) */
  function bindSearch(container) {
    if (!container || !pager()) return;
    Array.prototype.forEach.call(
      container.querySelectorAll("table.dark-table"),
      function (table) {
        if (table.dataset.searchbound === "1") return;
        var tbody = table.querySelector("tbody");
        if (!tbody || !tbody.id) return;
        var columns = pager().colNames(tbody.id);
        if (!columns.length) return;
        table.dataset.searchbound = "1";
        var bar = buildBar(table, tbody.id, columns);
        var panel = table.closest ? table.closest(".recent-panel") : null;
        var head = panel ? panel.querySelector(":scope > .recent-head") : null;
        if (head) {
          var actions = head.querySelector(":scope > .recent-actions");
          if (actions) head.insertBefore(bar, actions);
          else head.appendChild(bar);
        } else {
          var resp = table.closest(".table-responsive");
          if (resp && resp.parentNode) resp.parentNode.insertBefore(bar, resp);
        }
      }
    );
  }

  window.PastarellaSearch = {
    bind: bindSearch
  };
})();
