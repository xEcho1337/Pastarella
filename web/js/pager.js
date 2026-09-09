(function () {
  "use strict";

  var DEFAULT_PER_PAGE = 100;
  var PER_PAGE_OPTIONS = [50, 100, 250, 500];
  var state = {}; /* tbodyId -> { rows, original, colNames, sortIdx, sortDir, page, perPage } */

  function storedPerPage() {
    try {
      var n = Number(localStorage.getItem("pastarella-perpage"));
      return PER_PAGE_OPTIONS.indexOf(n) !== -1 ? n : DEFAULT_PER_PAGE;
    } catch (e) {
      return DEFAULT_PER_PAGE;
    }
  }

  function storePerPage(n) {
    try {
      localStorage.setItem("pastarella-perpage", String(n));
    } catch (e) {
      /* ignore */
    }
  }

  function totalPages(s) {
    return Math.max(1, Math.ceil(s.rows.length / s.perPage));
  }

  function fmt(n) {
    return Number(n).toLocaleString("en-US");
  }

  function setRows(tbodyId, rows) {
    var prev = state[tbodyId];
    state[tbodyId] = {
      rows: rows,
      original: rows.slice(),
      colNames: canonicalColNames(tbodyId),
      sortIdx: -1,
      sortDir: "",
      page: 1,
      perPage: prev ? prev.perPage : storedPerPage()
    };
    render(tbodyId);
  }

  /* Header names in canonical order (captured before any reorder) */
  function canonicalColNames(tbodyId) {
    var body = document.getElementById(tbodyId);
    if (!body) return [];
    var table = body.closest("table");
    if (!table) return [];
    return Array.prototype.map.call(
      table.querySelectorAll("thead th"),
      headerName
    );
  }

  /* First text node only: ignores grips and sort indicators */
  function headerName(th) {
    for (var i = 0; i < th.childNodes.length; i++) {
      var n = th.childNodes[i];
      if (n.nodeType === 3) return n.textContent.trim();
    }
    return th.textContent.trim();
  }

  function cellText(cell) {
    if (cell && cell.__element) return cell.__element.textContent;
    if (cell && typeof cell === "object" && cell.nodeType === 1) return cell.textContent;
    if (cell && typeof cell.valueOf === "function" && typeof cell === "object") {
      return String(cell.valueOf());
    }
    return cell === null || cell === undefined ? "" : String(cell);
  }

  function cmpText(a, b) {
    return String(a).localeCompare(String(b), undefined, {
      numeric: true,
      sensitivity: "base"
    });
  }

  /* Click a header: asc -> desc -> original order */
  function sortBy(tbodyId, domIdx) {
    var s = state[tbodyId];
    if (!s) return;
    var body = document.getElementById(tbodyId);
    if (!body) return;
    var table = body.closest("table");
    if (!table) return;
    var ths = table.querySelectorAll("thead th");
    if (!ths[domIdx]) return;
    var dataIdx = s.colNames.indexOf(headerName(ths[domIdx]));
    if (dataIdx === -1) return;

    if (s.sortIdx === dataIdx) {
      if (s.sortDir === "asc") {
        s.sortDir = "desc";
      } else {
        s.rows = s.original.slice();
        s.sortIdx = -1;
        s.sortDir = "";
        s.page = 1;
        render(tbodyId);
        updateIndicators(table, -1, "");
        return;
      }
    } else {
      s.sortIdx = dataIdx;
      s.sortDir = "asc";
    }

    var dir = s.sortDir === "asc" ? 1 : -1;
    s.rows.sort(function (rowA, rowB) {
      return dir * cmpText(cellText(rowA[dataIdx]), cellText(rowB[dataIdx]));
    });
    s.page = 1;
    render(tbodyId);
    updateIndicators(table, domIdx, s.sortDir);
  }

  function updateIndicators(table, domIdx, dir) {
    var ths = table.querySelectorAll("thead th");
    Array.prototype.forEach.call(ths, function (th) {
      var old = th.querySelector(".sort-ind");
      if (old) th.removeChild(old);
    });
    if (domIdx >= 0 && ths[domIdx]) {
      var ind = document.createElement("span");
      ind.className = "sort-ind";
      ind.textContent = dir === "asc" ? "▲" : "▼";
      ths[domIdx].appendChild(ind);
    }
  }

  /* Attach header click handlers (called by the router after render) */
  function bindHeaders(container) {
    if (!container) return;
    Array.prototype.forEach.call(
      container.querySelectorAll("table.dark-table"),
      function (table) {
        if (table.dataset.sortable === "1") return;
        table.dataset.sortable = "1";
        Array.prototype.forEach.call(
          table.querySelectorAll("thead th"),
          function (th, domIdx) {
            if (!th.title) th.title = "Click to sort · drag to move";
            th.addEventListener("click", function (e) {
              if (e.target.closest && e.target.closest(".col-grip")) return;
              if (Date.now() - Number(table.dataset.reorderedAt || 0) < 350) return;
              var tbody = table.querySelector("tbody");
              if (tbody && tbody.id) sortBy(tbody.id, domIdx);
            });
          }
        );
      }
    );
  }

  function gotoPage(tbodyId, page) {
    var s = state[tbodyId];
    if (!s) return;
    s.page = Math.min(Math.max(1, page), totalPages(s));
    render(tbodyId);
  }

  function setPerPage(tbodyId, n) {
    var s = state[tbodyId];
    if (!s) return;
    s.perPage = n;
    s.page = 1;
    storePerPage(n);
    render(tbodyId);
  }

  /* Compact page list: 1 … p-1 p p+1 … last */
  function pageList(page, pages) {
    var nums = [1, page - 1, page, page + 1, pages].filter(function (n) {
      return n >= 1 && n <= pages;
    });
    nums = nums.filter(function (n, i) { return nums.indexOf(n) === i; });
    nums.sort(function (a, b) { return a - b; });
    var out = [];
    nums.forEach(function (n, i) {
      if (i > 0 && n - nums[i - 1] > 1) out.push("…");
      out.push(n);
    });
    return out;
  }

  function makeButton(label, title, disabled, onClick, current) {
    var btn = document.createElement("button");
    btn.type = "button";
    btn.className = "pager-btn" + (current ? " current" : "");
    btn.textContent = label;
    btn.title = title;
    btn.disabled = !!disabled;
    if (!disabled) {
      btn.addEventListener("click", onClick);
    }
    return btn;
  }

  function renderBar(tbodyId, body, s, pages, start, shown) {
    var resp = body.closest(".table-responsive");
    if (!resp || !resp.parentNode) return;
    var bar = resp.parentNode.querySelector(":scope > .pager-bar");
    if (!bar) {
      bar = document.createElement("div");
      bar.className = "pager-bar";
      resp.after(bar);
    }
    bar.innerHTML = "";

    /* Single page (or empty): no controls needed */
    if (pages <= 1) {
      bar.style.display = "none";
      return;
    }
    bar.style.display = "";

    var info = document.createElement("span");
    info.className = "pager-info";
    info.textContent = s.rows.length === 0
      ? "No entries"
      : fmt(start + 1) + "–" + fmt(start + shown) + " of " + fmt(s.rows.length);
    bar.appendChild(info);

    var controls = document.createElement("div");
    controls.className = "pager-controls";
    controls.appendChild(makeButton("«", "First page", s.page === 1, function () {
      gotoPage(tbodyId, 1);
    }));
    controls.appendChild(makeButton("‹", "Previous page", s.page === 1, function () {
      gotoPage(tbodyId, s.page - 1);
    }));
    pageList(s.page, pages).forEach(function (n) {
      if (n === "…") {
        var dots = document.createElement("span");
        dots.className = "pager-ellipsis";
        dots.textContent = "…";
        controls.appendChild(dots);
      } else {
        controls.appendChild(makeButton(String(n), "Page " + n, false, function () {
          gotoPage(tbodyId, n);
        }, n === s.page));
      }
    });
    controls.appendChild(makeButton("›", "Next page", s.page === pages, function () {
      gotoPage(tbodyId, s.page + 1);
    }));
    controls.appendChild(makeButton("»", "Last page", s.page === pages, function () {
      gotoPage(tbodyId, pages);
    }));

    var select = document.createElement("select");
    select.className = "pager-select";
    select.title = "Rows per page";
    PER_PAGE_OPTIONS.forEach(function (n) {
      var opt = document.createElement("option");
      opt.value = String(n);
      opt.textContent = n + " / page";
      if (n === s.perPage) opt.selected = true;
      select.appendChild(opt);
    });
    select.addEventListener("change", function () {
      setPerPage(tbodyId, Number(select.value));
    });
    controls.appendChild(select);

    bar.appendChild(controls);
  }

  function render(tbodyId) {
    var s = state[tbodyId];
    if (!s) return;
    var body = document.getElementById(tbodyId);
    if (!body) return;
    var R = window.PastarellaReport;
    if (!R) return;

    var pages = totalPages(s);
    if (s.page > pages) s.page = pages;
    var start = (s.page - 1) * s.perPage;
    var slice = s.rows.slice(start, start + s.perPage);

    body.innerHTML = "";
    slice.forEach(function (cells) {
      var tr = document.createElement("tr");
      R.appendCells(tr, cells);
      body.appendChild(tr);
    });

    renderBar(tbodyId, body, s, pages, start, slice.length);

    /* Paging replaces rows: re-apply a saved column order on top */
    var table = body.closest("table");
    if (table && window.PastarellaColumns && window.PastarellaColumns.reapplyOrder) {
      window.PastarellaColumns.reapplyOrder(table);
    }
  }

  window.PastarellaPager = {
    setRows: setRows,
    gotoPage: gotoPage,
    sortBy: sortBy,
    bindHeaders: bindHeaders,
    render: render
  };
})();
