(function () {
  "use strict";

  var WIDTHS_PREFIX = "pastarella-cols:";
  var ORDER_PREFIX = "pastarella-colorder:";
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

  function readWidthMap(table) {
    var map = {};
    headerCells(table).forEach(function (th) {
      map[colKey(th)] = Math.round(parseFloat(th.style.width) || th.offsetWidth);
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
  function freezeLayout(table, widthsByName) {
    table.classList.add("resizable");
    headerCells(table).forEach(function (th) {
      var w = widthsByName[colKey(th)] || th.offsetWidth;
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
    var widths = loadWidthMap(page, index, table);
    if (widths) {
      freezeLayout(table, widths);
    }
  }

  /* ---- Resize (grip drag) ---- */

  function onGripDown(e, table, page, index, th) {
    e.preventDefault();
    e.stopPropagation();
    /* First interaction: freeze layout so the drag starts from what you see */
    if (!table.classList.contains("resizable")) {
      freezeLayout(table, {});
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
      saveWidthMap(page, index, readWidthMap(table));
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
      saveWidthMap(page, index, readWidthMap(table));
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

    /* Restore the user's order + widths before adding grips */
    applySaved(table, page, index);

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

  /* Re-apply a saved order (used after the pager replaces rows) */
  function reapplyOrder(table) {
    if (!table.dataset.page) return;
    var order = loadOrder(table.dataset.page, Number(table.dataset.tindex));
    if (order && sameSet(order, colNames(table))) applyOrder(table, order);
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
