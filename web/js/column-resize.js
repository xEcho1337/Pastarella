(function () {
  "use strict";

  var WIDTHS_PREFIX = "pastarella-cols:";
  var ORDER_PREFIX = "pastarella-colorder:";
  var VIS_PREFIX = "pastarella-colvis:";
  var MIN_WIDTH = 48;
  var DRAG_THRESHOLD = 6;

  function widthsKey(page, index) {
    return WIDTHS_PREFIX + page + ":" + index;
  }

  function orderKey(page, index) {
    return ORDER_PREFIX + page + ":" + index;
  }

  /* Column identity: first header text node (ignores grips and sort
     indicators appended later) */
  function colKey(th) {
    for (var i = 0; i < th.childNodes.length; i++) {
      var n = th.childNodes[i];
      if (n.nodeType === 3) return n.textContent.trim();
    }
    return th.textContent.trim();
  }

  function headerCells(table) {
    return Array.prototype.slice.call(table.querySelectorAll("thead th"));
  }

  function colNames(table) {
    return headerCells(table).map(colKey);
  }

  /* ---- Widths (object keyed by column name) ---- */

  function loadWidthMap(page, index, table) {
    try {
      var raw = localStorage.getItem(widthsKey(page, index));
      if (!raw) return null;
      var parsed = JSON.parse(raw);
      if (Array.isArray(parsed)) {
        /* legacy positional format: map onto current headers, then re-save */
        var map = {};
        headerCells(table).forEach(function (th, i) {
          if (parsed[i]) map[colKey(th)] = parsed[i];
        });
        saveWidthMap(page, index, map);
        return map;
      }
      return parsed && typeof parsed === "object" ? parsed : null;
    } catch (e) {
      return null;
    }
  }

  function saveWidthMap(page, index, map) {
    try {
      localStorage.setItem(widthsKey(page, index), JSON.stringify(map));
    } catch (e) {
      /* storage unavailable: resize still works for this session */
    }
  }

  function readWidthMap(table, keep) {
    var map = {};
    keep = keep || {};
    headerCells(table).forEach(function (th) {
      var name = colKey(th);
      if (th.style.display === "none") {
        if (keep[name]) map[name] = keep[name]; /* hidden: keep last known width */
        return;
      }
      map[name] = Math.round(parseFloat(th.style.width) || th.offsetWidth);
    });
    return map;
  }

  /* ---- Order ---- */

  function loadOrder(page, index) {
    try {
      var raw = localStorage.getItem(orderKey(page, index));
      if (!raw) return null;
      var order = JSON.parse(raw);
      return Array.isArray(order) ? order : null;
    } catch (e) {
      return null;
    }
  }

  function saveOrder(page, index, table) {
    try {
      localStorage.setItem(orderKey(page, index), JSON.stringify(colNames(table)));
    } catch (e) {
      /* ignore */
    }
  }

  function clearTableStorage(page, index) {
    try {
      localStorage.removeItem(widthsKey(page, index));
      localStorage.removeItem(orderKey(page, index));
    } catch (e) {
      /* ignore */
    }
  }

  function sameSet(a, b) {
    if (a.length !== b.length) return false;
    return a.every(function (name) { return b.indexOf(name) !== -1; });
  }

  /* ---- Visibility (array of hidden column names) ---- */

  function visKey(page, index) {
    return VIS_PREFIX + page + ":" + index;
  }

  function loadHidden(page, index) {
    try {
      var raw = localStorage.getItem(visKey(page, index));
      if (!raw) return [];
      var hidden = JSON.parse(raw);
      return Array.isArray(hidden) ? hidden : [];
    } catch (e) {
      return [];
    }
  }

  function saveHidden(page, index, hidden) {
    try {
      localStorage.setItem(visKey(page, index), JSON.stringify(hidden));
    } catch (e) {
      /* ignore */
    }
  }

  /* Hide header + body cells by DOM position (mirrors any saved order) */
  function applyVisibility(table, page, index) {
    var names = colNames(table);
    var hidden = loadHidden(page, index).filter(function (n) {
      return names.indexOf(n) !== -1;
    });
    saveHidden(page, index, hidden); /* drop stale names after schema changes */
    headerCells(table).forEach(function (th, i) {
      var hide = hidden.indexOf(colKey(th)) !== -1;
      th.style.display = hide ? "none" : "";
      table.querySelectorAll("tbody tr").forEach(function (row) {
        var cell = row.children[i];
        if (cell) cell.style.display = hide ? "none" : "";
      });
    });
    return hidden;
  }

  function toggleColumn(table, page, index, name) {
    var hidden = loadHidden(page, index);
    var at = hidden.indexOf(name);
    if (at !== -1) {
      hidden.splice(at, 1);
    } else {
      /* Never hide the last visible column: with no header left there
         would be nothing to right-click to bring columns back */
      var visible = colNames(table).filter(function (n) {
        return hidden.indexOf(n) === -1;
      });
      if (visible.length <= 1) return false;
      hidden.push(name);
    }
    saveHidden(page, index, hidden);
    applyVisibility(table, page, index);
    return true;
  }

  function showAllColumns(table, page, index) {
    saveHidden(page, index, []);
    applyVisibility(table, page, index);
  }

  /* ---- Right-click column menu ---- */

  var openMenu = null;

  function closeMenu() {
    if (openMenu && openMenu.parentNode) openMenu.parentNode.removeChild(openMenu);
    openMenu = null;
  }

  /* One set of global closers for every table menu */
  document.addEventListener("click", function (e) {
    if (openMenu && !openMenu.contains(e.target)) closeMenu();
  });
  document.addEventListener("keydown", function (e) {
    if (e.key === "Escape") closeMenu();
  });
  window.addEventListener("scroll", closeMenu, true);
  window.addEventListener("resize", closeMenu);

  /* Canonical names (stable listing even after a reorder) */
  function originalNames(table) {
    try {
      var orig = JSON.parse(table.dataset.origOrder || "null");
      if (orig && sameSet(orig, colNames(table))) return orig;
    } catch (e) {
      /* fall through */
    }
    return colNames(table);
  }

  /* Checkbox menu: checked = visible. Hidden columns stay listed
     so they can always be brought back, plus a Show all shortcut. */
  function openColumnMenu(table, page, index, x, y) {
    closeMenu();
    var names = originalNames(table);
    var menu = document.createElement("div");
    menu.className = "col-menu";

    names.forEach(function (name) {
      var item = document.createElement("label");
      item.className = "col-menu-item";
      var box = document.createElement("input");
      box.type = "checkbox";
      var text = document.createElement("span");
      text.textContent = name;
      item.appendChild(box);
      item.appendChild(text);
      item.addEventListener("click", function (e) {
        e.preventDefault();
        if (box.disabled) return;
        toggleColumn(table, page, index, name);
        refreshMenuState();
      });
      menu.appendChild(item);
    });

    var sep = document.createElement("div");
    sep.className = "col-menu-sep";
    menu.appendChild(sep);

    var allBtn = document.createElement("button");
    allBtn.type = "button";
    allBtn.className = "col-menu-all";
    allBtn.textContent = "Show all";
    allBtn.addEventListener("click", function () {
      showAllColumns(table, page, index);
      refreshMenuState();
    });
    menu.appendChild(allBtn);

    function refreshMenuState() {
      var hidden = loadHidden(page, index);
      var visible = names.filter(function (n) { return hidden.indexOf(n) === -1; });
      Array.prototype.forEach.call(menu.querySelectorAll(".col-menu-item"), function (item, i) {
        var isHidden = hidden.indexOf(names[i]) !== -1;
        var box = item.querySelector("input");
        box.checked = !isHidden;
        box.disabled = !isHidden && visible.length <= 1;
        item.classList.toggle("is-hidden", isHidden);
        item.title = isHidden ? "Hidden — click to show" : "Visible — click to hide";
      });
      allBtn.disabled = hidden.length === 0;
    }
    refreshMenuState();

    document.body.appendChild(menu);
    var rect = menu.getBoundingClientRect(); /* clamp inside the viewport */
    menu.style.left = Math.max(4, Math.min(x, window.innerWidth - rect.width - 4)) + "px";
    menu.style.top = Math.max(4, Math.min(y, window.innerHeight - rect.height - 4)) + "px";
    openMenu = menu;
  }

  /* Move one column (header + every body cell) from fromIdx to toIdx */
  function moveColumnCells(table, fromIdx, toIdx) {
    if (fromIdx === toIdx) return;
    table.querySelectorAll("tr").forEach(function (row) {
      var cells = row.children;
      var cell = cells[fromIdx];
      if (!cell) return;
      row.removeChild(cell);
      var ref = cells[toIdx]; /* live collection, already shifted */
      if (ref) row.insertBefore(cell, ref);
      else row.appendChild(cell);
    });
  }

  function applyOrder(table, order) {
    order.forEach(function (name, targetIdx) {
      var fromIdx = colNames(table).indexOf(name);
      if (fromIdx !== -1) moveColumnCells(table, fromIdx, targetIdx);
    });
  }

  /* ---- Fixed layout without visual jumps ---- */

  /* Freeze the current automatic layout into explicit widths so that
     switching to fixed layout changes nothing visually. */
  function freezeLayout(table, widthsByName, hidden) {
    table.classList.add("resizable");
    hidden = hidden || [];
    headerCells(table).forEach(function (th) {
      var name = colKey(th);
      if (hidden.indexOf(name) !== -1) return; /* measured width is 0 while hidden */
      var w = widthsByName[name] || th.offsetWidth;
      th.style.width = Math.round(w) + "px";
    });
  }

  function applySaved(table, page, index) {
    var cells = headerCells(table);
    if (cells.length < 2) return;
    table.dataset.origOrder = JSON.stringify(colNames(table));
    var order = loadOrder(page, index);
    if (order && sameSet(order, colNames(table))) {
      applyOrder(table, order);
    }
    var hidden = applyVisibility(table, page, index);
    var widths = loadWidthMap(page, index, table);
    if (widths) {
      freezeLayout(table, widths, hidden);
    }
  }

  /* ---- Resize (grip drag) ---- */

  function onGripDown(e, table, page, index, th) {
    e.preventDefault();
    e.stopPropagation();
    /* First interaction: freeze layout so the drag starts from what you see */
    if (!table.classList.contains("resizable")) {
      freezeLayout(table, {}, loadHidden(page, index));
    }

    var startX = e.clientX;
    var startWidth = th.offsetWidth;
    document.body.classList.add("col-resizing");

    function onMove(ev) {
      var next = Math.max(MIN_WIDTH, startWidth + (ev.clientX - startX));
      th.style.width = next + "px";
    }

    function onUp() {
      document.removeEventListener("pointermove", onMove);
      document.removeEventListener("pointerup", onUp);
      document.body.classList.remove("col-resizing");
      saveWidthMap(page, index, readWidthMap(table, loadWidthMap(page, index, table)));
    }

    document.addEventListener("pointermove", onMove);
    document.addEventListener("pointerup", onUp);
  }

  function onGripReset(table, page, index) {
    clearTableStorage(page, index);
    try {
      var orig = JSON.parse(table.dataset.origOrder || "null");
      if (orig && sameSet(orig, colNames(table))) applyOrder(table, orig);
    } catch (e) {
      /* keep current order */
    }
    table.classList.remove("resizable");
    headerCells(table).forEach(function (th) { th.style.width = ""; });
  }

  /* ---- Reorder (header drag) ---- */

  function clearIndicators(table) {
    headerCells(table).forEach(function (th) {
      th.classList.remove("drop-before", "drop-after");
    });
  }

  /* Insertion reference among live header cells, excluding the dragged one */
  function dropTarget(table, dragTh, clientX) {
    var cells = headerCells(table).filter(function (th) { return th !== dragTh; });
    for (var i = 0; i < cells.length; i++) {
      var rect = cells[i].getBoundingClientRect();
      if (clientX < rect.left + rect.width / 2) return cells[i];
    }
    return null; /* append at the end */
  }

  function onHeaderDown(e, table, page, index, th) {
    if (e.target.closest && e.target.closest(".col-grip")) return;
    if (e.button !== undefined && e.button !== 0) return;

    var startX = e.clientX;
    var startY = e.clientY;
    var fromIdx = headerCells(table).indexOf(th);
    var dragging = false;
    var ghost = null;

    function startDrag(ev) {
      dragging = true;
      document.body.classList.add("col-reordering");
      if (window.getSelection) window.getSelection().removeAllRanges();
      ghost = document.createElement("div");
      ghost.className = "col-ghost";
      ghost.textContent = colKey(th);
      ghost.style.width = th.offsetWidth + "px";
      document.body.appendChild(ghost);
      moveGhost(ev);
      th.classList.add("col-dragging");
    }

    function moveGhost(ev) {
      ghost.style.left = (ev.clientX + 8) + "px";
      ghost.style.top = (ev.clientY - 20) + "px";
    }

    function onMove(ev) {
      if (!dragging) {
        if (Math.abs(ev.clientX - startX) < DRAG_THRESHOLD &&
            Math.abs(ev.clientY - startY) < DRAG_THRESHOLD) return;
        startDrag(ev);
      }
      moveGhost(ev);
      clearIndicators(table);
      var ref = dropTarget(table, th, ev.clientX);
      if (ref) ref.classList.add("drop-before");
      else {
        var cells = headerCells(table).filter(function (c) { return c !== th; });
        if (cells.length) cells[cells.length - 1].classList.add("drop-after");
      }
    }

    function cleanup() {
      document.removeEventListener("pointermove", onMove);
      document.removeEventListener("pointerup", onUp);
      document.removeEventListener("pointercancel", onCancel);
      document.body.classList.remove("col-reordering");
      th.classList.remove("col-dragging");
      clearIndicators(table);
      if (ghost && ghost.parentNode) ghost.parentNode.removeChild(ghost);
    }

    function onUp(ev) {
      var wasDragging = dragging;
      var ref = wasDragging ? dropTarget(table, th, ev.clientX) : null;
      cleanup();
      if (!wasDragging) return;
      /* Reorder header + body cells, then persist order (widths are name-keyed) */
      var headRow = th.parentNode;
      headRow.removeChild(th);
      if (ref) headRow.insertBefore(th, ref);
      else headRow.appendChild(th);
      var newIdx = headerCells(table).indexOf(th);
      table.querySelectorAll("tbody tr").forEach(function (row) {
        var cell = row.children[fromIdx];
        if (!cell) return;
        row.removeChild(cell);
        var refCell = row.children[newIdx];
        if (refCell) row.insertBefore(cell, refCell);
        else row.appendChild(cell);
      });
      saveOrder(page, index, table);
      saveWidthMap(page, index, readWidthMap(table, loadWidthMap(page, index, table)));
      table.dataset.reorderedAt = String(Date.now()); /* let sort clicks ignore this drop */
    }

    function onCancel() {
      cleanup();
    }

    document.addEventListener("pointermove", onMove);
    document.addEventListener("pointerup", onUp);
    document.addEventListener("pointercancel", onCancel);
  }

  function makeResizable(table, page, index) {
    if (table.dataset.resizable === "1") return;
    table.dataset.resizable = "1";
    table.dataset.page = page;
    table.dataset.tindex = String(index);

    var cells = headerCells(table);
    if (cells.length < 2) return;

    /* Restore the user's order + widths + visibility before adding grips */
    applySaved(table, page, index);

    /* Right-click a header: checkbox menu to show/hide columns */
    table.addEventListener("contextmenu", function (e) {
      var th = e.target && e.target.closest ? e.target.closest("thead th") : null;
      if (!th || !table.contains(th)) return;
      e.preventDefault();
      openColumnMenu(table, page, index, e.clientX, e.clientY);
    });

    cells.forEach(function (th, i) {
      th.classList.add("reorderable");
      th.addEventListener("pointerdown", function (e) {
        onHeaderDown(e, table, page, index, th);
      });
      if (i === cells.length - 1) return; /* last column fills the rest */
      var grip = document.createElement("span");
      grip.className = "col-grip";
      grip.title = "Drag to resize · double-click to reset";
      grip.addEventListener("pointerdown", function (e) {
        onGripDown(e, table, page, index, th);
      });
      grip.addEventListener("dblclick", function (e) {
        e.preventDefault();
        onGripReset(table, page, index);
      });
      th.appendChild(grip);
    });
  }

  /* Re-apply a saved order + visibility (used after the pager replaces rows) */
  function reapplyOrder(table) {
    if (!table.dataset.page) return;
    var page = table.dataset.page;
    var index = Number(table.dataset.tindex);
    var order = loadOrder(page, index);
    if (order && sameSet(order, colNames(table))) applyOrder(table, order);
    applyVisibility(table, page, index);
  }

  /* Apply to every dark-table inside container (called by the router) */
  function applyToPage(container, page) {
    if (!container) return;
    var tables = container.querySelectorAll("table.dark-table");
    tables.forEach(function (table, index) {
      makeResizable(table, page, index);
    });
  }

  window.PastarellaColumns = {
    applyToPage: applyToPage,
    makeResizable: makeResizable,
    reapplyOrder: reapplyOrder
  };
})();
