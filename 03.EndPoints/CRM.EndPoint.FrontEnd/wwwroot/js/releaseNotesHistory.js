(() => {
    const root = document.getElementById("releaseNotesHistory");
    if (!root) return;
    const page = window.PageRuntime?.current;
    const signal = page?.signal;
    const search = root.querySelector("[data-history-search]");
    const filters = [...root.querySelectorAll("[data-history-type]")];
    const cards = [...root.querySelectorAll("[data-history-card]")];
    cards.forEach((card, index) => card.classList.add(index % 2 === 0 ? "rn-tone-teal" : "rn-tone-purple"));
    const empty = root.querySelector("[data-history-empty]");
    let type = "all";
    const normalize = value => (value || "").trim().toLocaleLowerCase("fa-IR").replace(/[يى]/g, "ی").replace(/ك/g, "ک");
    function update() {
        const term = normalize(search.value);
        let visibleCount = 0;
        for (const card of cards) {
            const headerMatch = normalize(card.querySelector(".rn-history-card-head").textContent + " " + (card.querySelector(".rn-history-note")?.textContent || "")).includes(term);
            let matchingItems = 0;
            for (const item of card.querySelectorAll("[data-history-item]")) {
                const match = (type === "all" || item.dataset.type === type) && (headerMatch || normalize(item.textContent).includes(term));
                item.hidden = !match;
                if (match) matchingItems++;
            }
            const visible = matchingItems > 0 || (type === "all" && headerMatch && !card.querySelector("[data-history-item]"));
            card.hidden = !visible;
            if (visible) {
                card.classList.toggle("rn-tone-teal", visibleCount % 2 === 0);
                card.classList.toggle("rn-tone-purple", visibleCount % 2 !== 0);
                visibleCount++;
            }
        }
        empty.hidden = visibleCount > 0 || cards.length === 0;
    }
    function toggle(card) {
        const button = card.querySelector("[data-history-expand]");
        const expanded = button.getAttribute("aria-expanded") !== "true";
        button.setAttribute("aria-expanded", String(expanded));
        card.classList.toggle("expanded", expanded);
        card.querySelectorAll(".rn-history-note,.rn-history-item-extra").forEach(element => { element.hidden = !expanded; });
    }
    search.addEventListener("input", update, { signal });
    root.addEventListener("click", event => {
        const filter = event.target.closest("[data-history-type]");
        if (filter && root.contains(filter)) {
            type = filter.dataset.historyType;
            filters.forEach(button => {
                const active = button === filter;
                button.classList.toggle("active", active);
                button.setAttribute("aria-pressed", String(active));
            });
            update();
            return;
        }
        const expand = event.target.closest("[data-history-expand]");
        if (expand && root.contains(expand)) toggle(expand.closest("[data-history-card]"));
    }, { signal });
    const cleanup = typeof enableMomentumScrollScope === "function" ? enableMomentumScrollScope(root) : null;
    if (typeof cleanup === "function" && page) page.cleanups.push(cleanup);
})();
