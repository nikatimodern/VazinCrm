(() => {
    "use strict";
    const modal = document.getElementById("releaseNotesModal");
    if (!modal || window.ReleaseNotes) return;
    const content = modal.querySelector("[data-rn-content]"), ack = modal.querySelector("[data-rn-ack]"), error = modal.querySelector("[data-rn-error]");
    const historyButton = modal.querySelector("[data-rn-history]");
    const historyTools = modal.querySelector("[data-rn-history-tools]");
    const historySearch = modal.querySelector("[data-rn-history-search]");
    const historyFilters = [...modal.querySelectorAll("[data-rn-history-type]")];
    const historyEmpty = modal.querySelector("[data-rn-history-empty]");
    let ids = [], preview = false, busy = false, restoreFocus = null;
    let historyType = "all", historyRequest = null;
    const types = { 1: "قابلیت جدید", 2: "بهبود", 3: "رفع اشکال" };
    const typeIcons = { 1: "fa-plus-circle", 2: "fa-magic", 3: "fa-wrench" };
    function element(tag, text, className) {
        const node = document.createElement(tag);
        if (text != null) node.textContent = text;
        if (className) node.className = className;
        return node;
    }
    function safeUrl(value) {
        if (!value || /[\u0000-\u0020\\]/.test(value)) return null;
        try {
            const url = new URL(value, location.origin);
            return /^https?:$/.test(url.protocol) && !url.username && !url.password ? url.href : null;
        } catch { return null; }
    }
    function render(notes, target) {
        const fragment = document.createDocumentFragment();
        for (const [index, note] of notes.entries()) {
            const article = element("article", null, "rn-release"), header = element("header");
            article.classList.add(index % 2 === 0 ? "rn-tone-teal" : "rn-tone-purple");
            article.dataset.rnHistoryCard = "";
            const version = element("span", null, "rn-version");
            version.append(element("span", "نسخه:", "rn-version-label"), element("span", note.version, "rn-version-number"));
            version.querySelector(".rn-version-number").dir = "ltr";
            header.append(version, element("h2", note.title));
            if (note.isImportant) header.append(element("span", "مهم", "rn-tag"));
            if (note.publishDate) {
                const date = new Date(note.publishDate.endsWith("Z") ? note.publishDate : note.publishDate + "Z");
                if (!Number.isNaN(date.valueOf())) header.append(element("time", date.toLocaleDateString("fa-IR")));
            }
            article.append(header);
            if (note.description) article.append(element("p", note.description, "rn-description"));
            for (const item of note.items || []) {
                const section = element("section", null, "rn-item");
                section.dataset.rnHistoryItem = "";
                section.dataset.type = String(item.type);
                const heading = element("div", null, "rn-item-heading");
                const line = element("div", null, "rn-item-line");
                line.append(element("h3", item.title));
                if (item.description) {
                    line.append(element("span", " : ", "rn-item-separator"), element("span", item.description, "rn-item-summary"));
                }
                const typeBadge = element("span", null, "rn-tag rn-change-type rn-type-" + item.type);
                const icon = element("i", null, "fas rn-type-icon " + (typeIcons[item.type] || "fa-info-circle"));
                icon.setAttribute("aria-hidden", "true");
                typeBadge.append(icon, document.createTextNode(types[item.type] || "تغییر"));
                heading.append(typeBadge, line);
                section.append(heading);
                const imageUrl = safeUrl(item.imageUrl), learnMoreUrl = safeUrl(item.learnMoreUrl);
                if (imageUrl) { const image = element("img"); image.src = imageUrl; image.alt = item.title; image.loading = "lazy"; image.referrerPolicy = "no-referrer"; section.append(image); }
                if (learnMoreUrl) { const link = element("a", "بیشتر بدانید ↗"); link.href = learnMoreUrl; link.target = "_blank"; link.rel = "noopener noreferrer"; section.append(link); }
                article.append(section);
            }
            fragment.append(article);
        }
        target.replaceChildren(fragment);
    }
    const normalize = value => String(value || "").trim().toLocaleLowerCase("fa-IR").replace(/[يى]/g, "ی").replace(/ك/g, "ک");
    function filterHistory() {
        const term = normalize(historySearch.value);
        let visibleCount = 0;
        const cards = content.querySelectorAll("[data-rn-history-card]");
        for (const card of cards) {
            const headerText = card.querySelector("header")?.textContent || "";
            const noteText = card.querySelector(":scope > .rn-description")?.textContent || "";
            const headerMatch = normalize(headerText + " " + noteText).includes(term);
            const items = card.querySelectorAll("[data-rn-history-item]");
            let matchingItems = 0;
            for (const item of items) {
                const match = (historyType === "all" || item.dataset.type === historyType) &&
                    (headerMatch || normalize(item.textContent).includes(term));
                item.hidden = !match;
                if (match) matchingItems++;
            }
            const visible = matchingItems > 0 || (historyType === "all" && headerMatch && !items.length);
            card.hidden = !visible;
            if (visible) {
                card.classList.toggle("rn-tone-teal", visibleCount % 2 === 0);
                card.classList.toggle("rn-tone-purple", visibleCount % 2 !== 0);
                visibleCount++;
            }
        }
        historyEmpty.hidden = visibleCount > 0 || cards.length === 0;
    }
    function show(notes, isPreview = false, heading = null, isHistory = false) {
        const openModal = document.querySelector(".modal.show");
        if (busy || (!isPreview && !notes.length) || (openModal && openModal !== modal)) return false;
        preview = isPreview;
        ids = isPreview ? [] : notes.map(x => x.id);
        if (!modal.classList.contains("show")) restoreFocus = document.activeElement;
        if (notes.length) render(notes, content);
        else content.replaceChildren(element("p", "هنوز تغییری برای نمایش وجود ندارد.", "rn-empty"));
        error.hidden = true;
        ack.hidden = isPreview;
        historyButton.hidden = isHistory;
        historyTools.hidden = !isHistory;
        historyEmpty.hidden = true;
        modal.classList.toggle("rn-history", isHistory);
        modal.classList.toggle("rn-preview", isPreview && !isHistory);
        historySearch.value = "";
        historyType = "all";
        historyFilters.forEach(button => {
            const active = button.dataset.rnHistoryType === "all";
            button.classList.toggle("active", active);
            button.setAttribute("aria-pressed", String(active));
        });
        if (isHistory && notes.length) filterHistory();
        modal.querySelector("[data-bs-dismiss='modal'].rn-button").textContent = isPreview ? "بستن" : "بعداً";
        document.getElementById("releaseNotesTitle").textContent = heading || (isPreview ? "پیش‌نمایش تغییرات نسخه" : "تازه‌های سامانه");
        if (!modal.classList.contains("show")) ModalToggle(modal, "show");
        return true;
    }
    const scrollCleanup = typeof enableMomentumScrollScope === "function" ? enableMomentumScrollScope(modal) : null;
    modal.addEventListener("hide.bs.modal", event => {
        if (busy) event.preventDefault();
        else historyRequest?.abort();
    });
    modal.addEventListener("hidden.bs.modal", () => {
        ids = [];
        content.replaceChildren();
        historyTools.hidden = true;
        historyEmpty.hidden = true;
        modal.classList.remove("rn-history", "rn-preview");
        restoreFocus?.isConnected && restoreFocus.focus();
    });
    ack.addEventListener("click", async () => {
        if (preview || busy || !ids.length) return;
        busy = true; ack.disabled = true; error.hidden = true;
        try {
            const result = await sendApiRequest({ url:"/ReleaseNotes/Acknowledge", method:"POST", data:{ ids },
                headers:{ RequestVerificationToken:modal.querySelector("[name='__RequestVerificationToken']").value }, successMsg:null, showError:false, showLoading:false });
            if (result) { busy = false; ModalToggle(modal, "hide"); }
            else { error.textContent = "ثبت تأیید انجام نشد. دوباره تلاش کنید؛ در صورت تغییر وضعیت نسخه، صفحه را تازه‌سازی کنید."; error.hidden = false; }
        } finally { busy = false; ack.disabled = false; }
    });
    function syncMenuVersion(notes) {
        const latest = notes[0]; // HistoryData is sorted by numeric version, newest first.
        if (!latest?.version) return;
        const version = String(latest.version);
        let publishDate = "";
        if (latest.publishDate) {
            const value = String(latest.publishDate);
            const date = new Date(value.endsWith("Z") || /[+-]\\d{2}:\\d{2}$/.test(value) ? value : value + "Z");
            if (!Number.isNaN(date.getTime())) {
                publishDate = date.toLocaleDateString("fa-IR-u-nu-latn", { year: "numeric", month: "2-digit", day: "2-digit" });
            }
        }

        document.querySelectorAll("#desktopNavbar .menu-release-notes, #mobileDrawerNavbar .menu-release-notes").forEach(button => {
            const title = button.querySelector(".menu-item-title > span:first-child");
            if (title) title.textContent = "نسخه " + version;
            const subtitle = button.querySelector(".menu-release-date");
            if (subtitle) subtitle.textContent = publishDate;

            const compact = button.querySelector(".menu-release-compact > span:first-child");
            if (compact) compact.textContent = version;
            const compactDate = button.querySelector(".menu-release-compact > small");
            if (compactDate) compactDate.textContent = publishDate;

            const fullLabel = "نسخه " + version + (publishDate ? " - " + publishDate : "");
            button.dataset.tooltipLeft = fullLabel;
            button.setAttribute("aria-label", fullLabel);
        });
    }

    async function showHistory() {
        if (historyRequest || busy) return;
        const openModal = document.querySelector(".modal.show");
        if (openModal && openModal !== modal) return;
        const controller = new AbortController();
        historyRequest = controller;
        historyButton.disabled = true;
        error.hidden = true;
        if (!modal.classList.contains("show") && show([], true, "تغییرات نسخه‌ها", true)) {
            content.replaceChildren(element("p", "در حال دریافت تاریخچه تغییرات…", "rn-empty"));
        }
        try {
            const notes = await sendApiRequest({
                url: "/ReleaseNotes/HistoryData", showLoading: false, showError: false, signal: controller.signal
            });
            if (controller.signal.aborted) return;
            if (Array.isArray(notes)) {
                syncMenuVersion(notes);
                show(notes, true, "تغییرات نسخه‌ها", true);
            }
            else {
                error.textContent = "دریافت تاریخچه تغییرات انجام نشد. دوباره تلاش کنید.";
                error.hidden = false;
            }
        } finally {
            if (historyRequest === controller) {
                historyRequest = null;
                historyButton.disabled = false;
            }
        }
    }
    historyButton.addEventListener("click", showHistory);
    historySearch.addEventListener("input", filterHistory);
    historyTools.addEventListener("click", event => {
        const button = event.target.closest("[data-rn-history-type]");
        if (!button || !historyTools.contains(button)) return;
        historyType = button.dataset.rnHistoryType;
        historyFilters.forEach(filter => {
            const active = filter === button;
            filter.classList.toggle("active", active);
            filter.setAttribute("aria-pressed", String(active));
        });
        filterHistory();
    });
    window.ReleaseNotes = Object.freeze({ preview: notes => show(notes, true), render, safeUrl, history: showHistory });
    window.showReleaseNotesHistory = () => window.ReleaseNotes.history();
    // این کلید فقط مزاحمت تکراری را کم می‌کند؛ وضعیت دیده‌شدن همیشه از پایگاه داده می‌آید.
    const key = "release-notes:" + modal.dataset.user + ":" + (modal.dataset.session || "existing-login");
    const readChecked = () => { try { return sessionStorage.getItem(key) === "checked"; } catch { return false; } };
    const markChecked = () => { try { sessionStorage.setItem(key, "checked"); } catch { /* ذخیره محلی ممکن است غیرفعال باشد. */ } };
    if (!modal.dataset.user || readChecked()) return;
    let lastInteraction = Date.now(), requested = false, pending = null;
    const onInteraction = () => { lastInteraction = Date.now(); };
    document.addEventListener("pointerdown", onInteraction, { passive:true });
    document.addEventListener("keydown", onInteraction);
    const idle = () => document.visibilityState === "visible" && Date.now() - lastInteraction > 2500 &&
        !document.querySelector(".modal.show,.swal2-container,#mainContent[aria-busy='true']") &&
        !(typeof activeApiLoadingCount !== "undefined" && activeApiLoadingCount > 0) &&
        !document.activeElement?.matches("input,textarea,select,[contenteditable='true']");
    const timer = setInterval(async () => {
        if (!idle()) return;
        if (!requested) {
            requested = true;
            const requestPage = window.PageRuntime?.current;
            pending = await sendApiRequest({ url:"/ReleaseNotes/Unseen", showLoading:false, showError:false });
            if (!Array.isArray(pending)) {
                if (requestPage?.signal.aborted) requested = false; else stop();
                return;
            }
            if (!pending.length) { markChecked(); stop(); return; }
        }
        if (pending && idle() && show(pending)) { markChecked(); stop(); }
    }, 1000);
    function stop() { clearInterval(timer); document.removeEventListener("pointerdown", onInteraction); document.removeEventListener("keydown", onInteraction); }
    window.addEventListener("pagehide", () => { stop(); if (typeof scrollCleanup === "function") scrollCleanup(); }, { once:true });
})();
