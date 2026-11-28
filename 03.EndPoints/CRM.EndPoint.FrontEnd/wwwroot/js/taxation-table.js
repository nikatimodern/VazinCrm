(() => {
    "use strict";

    const table = document.querySelector("[data-taxation-datatable]");
    if (!table || typeof DataTable !== "function" || DataTable.isDataTable(table)) return;

    const total = Number(table.dataset.total);
    const page = Number(table.dataset.page);
    const pageSize = Number(table.dataset.pageSize);
    const pagination = total > pageSize;
    const format = new Intl.NumberFormat("fa-IR");
    const pageRuntime = window.PageRuntime?.current;
    const datatable = new DataTable(table, {
        serverSide: true,
        deferLoading: total,
        displayStart: (page - 1) * pageSize,
        pageLength: pageSize,
        paging: pagination,
        ordering: false,
        searching: false,
        lengthChange: false,
        autoWidth: false,
        layout: {
            topStart: null,
            topEnd: null,
            bottomStart: "info",
            bottomEnd: pagination ? { paging: { type: "simple_numbers", numbers: 5 } } : null
        },
        language: {
            ...languageDatatable,
            paginate: { previous: "قبلی", next: "بعدی" },
            aria: { paginate: { previous: "صفحه قبلی", next: "صفحه بعدی", number: "صفحه " } }
        },
        formatNumber: value => format.format(value),
        ajax: ({ start }) => {
            const url = new URL(window.location.href);
            for (const key of [...url.searchParams.keys()]) {
                if (key.toLowerCase() === "page") url.searchParams.delete(key);
            }
            url.searchParams.set("page", String(start / pageSize + 1));
            window.location.assign(url.href);
        }
    });
    const fallback = table.closest(".taxation-page").querySelector("[data-taxation-fallback-pager]");
    if (fallback) fallback.hidden = true;
    pageRuntime?.cleanups.push(() => {
        if (DataTable.isDataTable(table)) datatable.destroy();
    });
})();
