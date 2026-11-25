(() => {
    const search = document.getElementById("prefactorTermSearch");
    const list = document.getElementById("prefactorTermList");
    if (!search || !list || search.dataset.bound) return;
    search.dataset.bound = "true";

    const rows = Array.from(list.querySelectorAll(".prefactor-term-row"));
    const count = document.getElementById("prefactorTermCount");
    const empty = document.getElementById("prefactorTermEmpty");
    const normalize = value => value.toLowerCase()
        .replace(/[يى]/g, "ی").replace(/ك/g, "ک")
        .replace(/[۰-۹]/g, digit => String(digit.charCodeAt(0) - 1776))
        .replace(/[٠-٩]/g, digit => String(digit.charCodeAt(0) - 1632))
        .replace(/\u200c/g, " ").trim();

    search.addEventListener("input", () => {
        const query = normalize(search.value);
        let visible = 0;
        for (const row of rows) {
            const text = row.querySelector(".prefactor-term-text").textContent;
            const matches = normalize(row.dataset.order + " " + text).includes(query);
            row.classList.toggle("d-none", !matches);
            if (matches) visible++;
        }
        count.textContent = visible + " بند";
        empty.hidden = visible !== 0;
    });

    if (typeof enableMomentumScrollScope === "function")
        enableMomentumScrollScope(document.getElementById("prefactorTermsWorkspace"));
})();
