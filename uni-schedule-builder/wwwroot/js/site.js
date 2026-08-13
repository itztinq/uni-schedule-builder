// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {
    // ------------------------------------------------------------------
    // Timetable: search filter (subject / teacher / room)
    // ------------------------------------------------------------------
    var grid = document.getElementById("weekGrid");
    var search = document.getElementById("timetableSearch");
    var countEl = document.getElementById("visibleCount");

    function refreshVisibleCount() {
        if (!countEl || !grid) return;
        var n = grid.querySelectorAll(".wg-card:not(.is-muted)").length;
        countEl.textContent = String(n);
    }

    if (search && grid) {
        search.addEventListener("input", function () {
            var q = search.value.trim().toLowerCase();
            grid.querySelectorAll(".wg-card").forEach(function (card) {
                var hay = (card.getAttribute("data-search") || "").toLowerCase();
                card.classList.toggle("is-muted", q.length > 0 && hay.indexOf(q) === -1);
            });
            refreshVisibleCount();
        });
    }

    // ------------------------------------------------------------------
    // Timetable: day pills (dim other days)
    // ------------------------------------------------------------------
    var pills = document.querySelectorAll("[data-day-filter]");
    pills.forEach(function (pill) {
        pill.addEventListener("click", function () {
            pills.forEach(function (p) { p.classList.remove("active"); });
            pill.classList.add("active");
            if (grid) grid.setAttribute("data-day", pill.getAttribute("data-day-filter") || "");
        });
    });

    // ------------------------------------------------------------------
    // Admin tables: client-side search filter
    // ------------------------------------------------------------------
    document.querySelectorAll("input[data-filter-target]").forEach(function (input) {
        var target = document.querySelector(input.getAttribute("data-filter-target"));
        if (!target) return;
        input.addEventListener("input", function () {
            var q = input.value.trim().toLowerCase();
            target.querySelectorAll("tbody tr").forEach(function (row) {
                var hay = (row.getAttribute("data-search") || "").toLowerCase();
                row.classList.toggle("is-hidden-row", q.length > 0 && hay.indexOf(q) === -1);
            });
        });
    });

    // ------------------------------------------------------------------
    // Admin tables: sortable headers
    // ------------------------------------------------------------------
    document.querySelectorAll(".dash-table").forEach(function (table) {
        var headers = Array.from(table.querySelectorAll("th[data-sort]"));
        if (!headers.length) return;

        headers.forEach(function (th) {
            th.classList.add("sortable");
            var arrow = document.createElement("span");
            arrow.className = "sort-icon";
            arrow.textContent = "\u25BE";
            th.appendChild(arrow);
            th.addEventListener("click", function () {
                var idx = Array.from(th.parentNode.children).indexOf(th);
                var isNum = th.getAttribute("data-sort") === "num";
                var asc = th.classList.contains("sorted-desc");

                headers.forEach(function (h) {
                    h.classList.remove("sorted-asc", "sorted-desc");
                    h.querySelector(".sort-icon").textContent = "\u25BE";
                });

                th.classList.add(asc ? "sorted-asc" : "sorted-desc");
                th.querySelector(".sort-icon").textContent = asc ? "\u25B4" : "\u25BE";

                var tbody = table.tBodies[0];
                var rows = Array.from(tbody.querySelectorAll("tr"));
                rows.sort(function (a, b) {
                    var av = a.children[idx].textContent.trim().toLowerCase();
                    var bv = b.children[idx].textContent.trim().toLowerCase();
                    var cmp = isNum
                        ? (parseFloat(av) || 0) - (parseFloat(bv) || 0)
                        : av.localeCompare(bv);
                    return asc ? cmp : -cmp;
                });
                rows.forEach(function (r) { tbody.appendChild(r); });
            });
        });
    });
});