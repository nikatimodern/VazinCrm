(() => {
    "use strict";

    function toIso(value) {
        const date = toEnglishDigits(value.trim());
        if (!date) return "";
        const parts = /^(\d{4})\/(\d{1,2})\/(\d{1,2})$/.exec(date);
        if (!parts) return null;
        const [year, month, day] = parts.slice(1).map(Number);
        if (year < 1 || month < 1 || month > 12 || day < 1 || day > 31) return null;

        const persian = new persianDate([year, month, day]).toLeapYearMode("astronomical").toLocale("en");
        if (persian.year() !== year || persian.month() !== month || persian.date() !== day) return null;
        return persian.toCalendar("gregorian").toLocale("en").format("YYYY-MM-DD");
    }

    function validate(form) {
        for (const input of form.querySelectorAll("input.sdate")) {
            const iso = toIso(input.value);
            const expected = input.dataset.expectedIso;
            let message = "";
            if (iso === null) message = "تاریخ شمسی معتبری وارد کنید.";
            else if (expected && iso && iso !== expected) message = "تاریخ صدور باید با تاریخ فاکتور یکسان باشد.";
            input.setCustomValidity(message);
            if (message) {
                input.reportValidity();
                return false;
            }
        }
        return true;
    }

    function clearValidation(root, signal) {
        const clear = event => {
            if (event.target.matches("input.sdate")) event.target.setCustomValidity("");
        };
        root.addEventListener("input", clear, { signal });
        root.addEventListener("change", clear, { signal });
    }

    window.TaxationDates = { toIso, validate, clearValidation };
    const report = document.querySelector("[data-taxation-report-dates]");
    if (!report) return;
    const signal = window.PageRuntime?.current?.signal;
    clearValidation(report, signal);
    report.addEventListener("submit", event => {
        if (!validate(report)) {
            event.preventDefault();
            return;
        }
        for (const input of report.querySelectorAll("[data-iso-field]"))
            report.elements.namedItem(input.dataset.isoField).value = toIso(input.value);
    }, { signal });
})();
