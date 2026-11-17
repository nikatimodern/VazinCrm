(() => {
    "use strict";

    document.addEventListener("DOMContentLoaded", () => {
        const page = document.getElementById("qualityControlCustomerPage");
        if (!page) return;
        const queue = page.querySelector("#qcCustomerQueue");
        const queueFeedback = page.querySelector("#qcCustomerQueueFeedback");
        const queueCount = page.querySelector("#qcCustomerQueueCount");
        const summaryValues = [
            ["invalidNumberCustomers", "#qcSummaryInvalidCustomers"],
            ["noResponseCustomers", "#qcSummaryNoAnswerCustomers"],
            ["pendingCustomers", "#qcSummaryPendingCustomers"],
            ["pendingItems", "#qcSummaryPendingItems"],
            ["reviewedItems", "#qcSummaryReviewedItems"],
            ["discrepancyItems", "#qcSummaryDiscrepancyItems"],
            ["noDiscrepancyItems", "#qcSummaryNoDiscrepancyItems"],
        ].map(([property, selector]) => [property, page.querySelector(selector)]);
        const pages = page.querySelector("#qcCustomerPages");
        const modal = page.querySelector("#qcCustomerReviewModal");
        const form = page.querySelector("#qcCustomerReviewForm");
        const formFeedback = page.querySelector("#qcCustomerFormFeedback");
        const summary = page.querySelector("#qcCustomerReviewSummary");
        const content = page.querySelector("#qcCustomerReviewContent");
        const itemSearchBar = page.querySelector("#qcInvoiceItemSearchBar");
        const itemSearch = page.querySelector("#qcInvoiceItemSearch");
        const itemSearchClear = page.querySelector("#qcInvoiceItemSearchClear");
        const itemSearchCount = page.querySelector("#qcInvoiceItemSearchCount");
        const itemSearchEmpty = page.querySelector("#qcInvoiceItemSearchEmpty");
        const scroll = page.querySelector("#qcCustomerReviewScroll");
        const saveAllButton = page.querySelector("#qcCustomerSaveAll");
        const saveHint = page.querySelector("#qcCustomerSaveHint");
        const searchInput = page.querySelector("#qcCustomerSearch");
        const statusFilter = page.querySelector("#qcCustomerStatus");
        const sortControl = page.querySelector("#qcCustomerSort");
        const cardButtons = [...page.querySelectorAll("[data-qc-card-status]")];
        const pageSize = page.querySelector("#qcCustomerPageSize");
        const token = form.querySelector('[name="__RequestVerificationToken"]');
        const faNumber = new Intl.NumberFormat("fa-IR");
        const faCodeNumber = new Intl.NumberFormat("fa-IR", { useGrouping: false });
        const escape = value => String(value ?? "").replace(/[&<>"']/g, char =>
            ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[char]);
        const pageSignal = typeof PageRuntime !== "undefined" ? PageRuntime.current?.signal : null;
        const isActive = () => page.isConnected && !pageSignal?.aborted;

        // Account for the actual shell/header height rather than guessing a
        // fixed offset; only the customer queue scrolls on desktop.
        function fitQueueToViewport() {
            if (!isActive()) return;
            const top = Math.max(0, page.getBoundingClientRect().top);
            page.style.setProperty("--qc-workspace-height",
                Math.max(320, Math.floor(window.innerHeight - top - 8)) + "px");
        }
        fitQueueToViewport();
        window.addEventListener("resize", fitQueueToViewport,
            pageSignal ? { signal: pageSignal } : undefined);
        if (typeof enableMomentumScroll === "function")
            enableMomentumScroll(queue, { signal: pageSignal });

        let start = 0, total = 0, search = "", loading = false, saving = false, contactBusy = false;
        let listController = null, formController = null, formGeneration = 0;
        let review = null;
        const drafts = new Map();
        const savingItems = new Set();
        const noteTimers = new Map();
        const flatten = () => review?.orders.flatMap(order =>
            order.invoices.flatMap(invoice => invoice.items)) ?? [];
        const stateOf = item => drafts.get(item.factorItemId);

        const normalizeSearch = text => String(text ?? "").normalize("NFKC")
            .replace(/[يى]/g, "ی").replace(/ك/g, "ک")
            .replace(/[\u064B-\u065F\u0670\u200C\u200D]/g, " ")
            .replace(/[۰-۹]/g, digit => String(digit.charCodeAt(0) - 0x06F0))
            .replace(/[٠-٩]/g, digit => String(digit.charCodeAt(0) - 0x0660))
            .toLocaleLowerCase("fa-IR").replace(/\s+/g, " ").trim();

        function filterInvoiceItems() {
            if (!review) return;
            const query = normalizeSearch(itemSearch.value);
            const orders = content.querySelectorAll(".qc-client-order");
            let matches = 0;
            orders.forEach(order => {
                let orderMatches = 0;
                order.querySelectorAll(".qc-client-invoice").forEach(invoice => {
                    let invoiceMatches = 0;
                    invoice.querySelectorAll(".qc-client-item").forEach(item => {
                        const itemText = normalizeSearch(item.querySelector(".qc-client-item-meta")?.textContent);
                        const visible = !query || itemText.includes(query);
                        item.hidden = !visible;
                        if (visible) invoiceMatches++;
                    });
                    invoice.hidden = invoiceMatches === 0;
                    orderMatches += invoiceMatches;
                });
                order.hidden = orderMatches === 0;
                matches += orderMatches;
            });
            itemSearchCount.textContent = faNumber.format(matches) + " از "
                + faNumber.format(flatten().length) + " قلم";
            itemSearchEmpty.hidden = !query || matches !== 0;
            itemSearchClear.disabled = !itemSearch.value;
        }

        function isValid(item) {
            const draft = stateOf(item);
            if (!draft) return false;
            if (draft.status === 0 || draft.status === 1) return true;
            if (draft.status !== 2 || !review.questions?.length) return false;
            return review.questions.every(question => draft.answers[question.id] === 0
                || draft.answers[question.id] === 1)
                && review.questions.some(question => draft.answers[question.id] === 1);
        }

        function clearNoteTimer(itemId) {
            const timer = noteTimers.get(itemId);
            if (timer) clearTimeout(timer);
            noteTimers.delete(itemId);
        }

        function hasUnsavedWork() {
            return saving || savingItems.size > 0 || noteTimers.size > 0;
        }

        function renderPager() {
            const length = Number(pageSize.value);
            const current = Math.floor(start / length);
            const count = Math.ceil(total / length);
            pages.innerHTML = count > 1
                ? '<button type="button" data-qc-page="prev"' + (current === 0 ? " disabled" : "") + '>قبلی</button>'
                    + '<span>صفحه ' + faNumber.format(current + 1) + ' از ' + faNumber.format(count) + '</span>'
                    + '<button type="button" data-qc-page="next"' + (current + 1 >= count ? " disabled" : "") + '>بعدی</button>'
                : "";
        }

        function syncCardSelection() {
            cardButtons.forEach(button => {
                const selected = button.dataset.qcCardStatus === statusFilter.value;
                button.classList.toggle("is-selected", selected);
                button.setAttribute("aria-pressed", String(selected));
            });
        }

        async function loadCustomers(reset = false) {
            if (!isActive()) return;
            if (reset) start = 0;
            syncCardSelection();
            listController?.abort();
            const controller = listController = new AbortController();
            loading = true;
            queue.setAttribute("aria-busy", "true");
            queueFeedback.textContent = "در حال دریافت مشتریان…";
            try {
                const result = await sendApiRequest({
                    url: "/QualityControl/GetCustomers",
                    params: {
                        start, length: Number(pageSize.value), search,
                        status: statusFilter.value,
                        sort: sortControl.value,
                    },
                    signal: controller.signal,
                    showLoading: false,
                });
                if (!isActive() || controller.signal.aborted) return;
                if (!result || !Array.isArray(result.data)) {
                    queueFeedback.textContent = "دریافت صف کنترل کیفیت انجام نشد. دوباره جستجو کنید.";
                    return;
                }
                if (result.summary) {
                    summaryValues.forEach(([property, element]) => {
                        if (element) element.textContent = faNumber.format(Number(result.summary[property] ?? 0));
                    });
                }
                total = Number(result.filtered ?? 0);
                if (start >= total && start > 0) {
                    start = Math.max(0, (Math.ceil(total / Number(pageSize.value)) - 1) * Number(pageSize.value));
                    return loadCustomers();
                }
                queue.innerHTML = result.data.map(row => {
                    const pending = row.totalItems - row.reviewedItems;
                    const withoutDiscrepancy = Math.max(0, row.reviewedItems - row.discrepancyItems);
                    const contactStatus = Number(row.contactStatus) || 0;
                    const color = contactStatus === 2 ? "invalid-phone"
                        : contactStatus === 1 ? "no-answer"
                            : pending > 0 ? "pending" : row.discrepancyItems > 0 ? "discrepancy" : "complete";
                    const label = contactStatus === 2 ? "شماره نامعتبر"
                        : contactStatus === 1 ? "عدم پاسخ"
                            : pending > 0 ? "نیازمند بررسی" : row.discrepancyItems > 0 ? "دارای مغایرت" : "تکمیل‌شده";
                    return '<article role="listitem" class="qc-client-card">'
                        + '<div class="qc-client-profile"><strong>' + escape(row.customerName || "مشتری بدون نام") + '</strong>'
                        + '<small>کد مشتری: ' + faCodeNumber.format(row.customerCode) + ' · ' + escape(row.mobile || "بدون شماره") + '</small></div>'
                        + '<div class="qc-client-stats"><span>' + faNumber.format(row.orders) + ' سفارش</span>'
                        + '<span>' + faNumber.format(row.invoices) + ' فاکتور</span>'
                        + '<span class="qc-client-count-reviewed"><strong>بررسی‌شده: ' + faNumber.format(row.reviewedItems) + '</strong>'
                        + '<small>مغایرت: ' + faNumber.format(row.discrepancyItems)
                        + ' · بدون مغایرت: ' + faNumber.format(withoutDiscrepancy) + '</small></span>'
                        + '<span class="qc-client-count-pending">بررسی‌نشده: ' + faNumber.format(pending) + '</span></div>'
                        + '<span class="qc-client-badge qc-client-' + color + '">' + label + '</span>'
                        + '<div class="qc-client-contact-actions">'
                        + '<button type="button" class="qc-client-button qc-client-contact-invalid'
                        + (contactStatus === 2 ? ' is-selected' : '') + '" aria-pressed="' + (contactStatus === 2)
                        + '" data-qc-contact="2" data-qc-contact-customer="' + escape(row.customerId) + '">شماره نامعتبر</button>'
                        + '<button type="button" class="qc-client-button qc-client-contact-no-answer'
                        + (contactStatus === 1 ? ' is-selected' : '') + '" aria-pressed="' + (contactStatus === 1)
                        + '" data-qc-contact="1" data-qc-contact-customer="' + escape(row.customerId) + '">عدم پاسخ</button>'
                        + '<button type="button" class="qc-client-button qc-client-primary" data-qc-customer="' + escape(row.customerId) + '">'
                        + (pending > 0 ? "کنترل کیفیت" : "مشاهده / ویرایش") + '</button>'
                        + '</div></article>';
                }).join("");
                queueFeedback.textContent = total ? "" : "مشتری واجد شرایطی با این فیلتر یافت نشد.";
                queueCount.textContent = faNumber.format(total) + " مشتری";
                renderPager();
            } finally {
                if (listController === controller) {
                    loading = false;
                    queue.removeAttribute("aria-busy");
                }
            }
        }

        async function openCustomer(customerId) {
            if (hasUnsavedWork() || !isActive()) return;
            formController?.abort();
            const controller = formController = new AbortController();
            const generation = ++formGeneration;
            noteTimers.forEach(timer => clearTimeout(timer));
            noteTimers.clear();
            drafts.clear();
            review = null;
            saveAllButton.disabled = true;
            summary.replaceChildren();
            content.replaceChildren();
            itemSearch.value = "";
            itemSearchBar.hidden = true;
            itemSearchEmpty.hidden = true;
            itemSearchCount.textContent = "";
            itemSearchClear.disabled = true;
            page.querySelector("#qcCustomerModalTitle").textContent = "کنترل کیفیت مشتری";
            page.querySelector("#qcCustomerModalSubtitle").textContent = "";
            formFeedback.textContent = "در حال دریافت فاکتورها و اقلام سفارش‌های تحویل‌شده…";
            ModalToggle(modal, "show");
            const result = await sendApiRequest({
                url: "/QualityControl/GetCustomerForm", params: { customerId },
                signal: controller.signal, showLoading: false,
            });
            if (!isActive() || controller.signal.aborted || generation !== formGeneration) return;
            if (!result?.orders?.length) {
                formFeedback.textContent = "دریافت فاکتورهای مشتری ناموفق بود. پنجره را ببندید و دوباره تلاش کنید.";
                return;
            }
            review = result;
            page.querySelector("#qcCustomerModalTitle").textContent = "کنترل کیفیت: " + (review.customerName || "مشتری");
            const customerDetails = ["کد مشتری: " + faCodeNumber.format(review.customerCode)];
            if (review.agentName?.trim() && review.agentName.trim() !== review.customerName?.trim())
                customerDetails.push("نماینده: " + review.agentName.trim());
            if (review.mobile?.trim()) customerDetails.push("موبایل: " + review.mobile.trim());
            page.querySelector("#qcCustomerModalSubtitle").textContent = customerDetails.join(" | ");
            flatten().forEach(item => {
                const status = Number(item.status) || 0;
                drafts.set(item.factorItemId, {
                    status,
                    revision: 0,
                    failed: false,
                    description: item.description || "",
                    rowVersion: item.rowVersion || null,
                    saved: Boolean(item.rowVersion) && status !== 0,
                    answers: Object.fromEntries((item.answers || []).map(answer =>
                        [answer.id, answer.answer])),
                });
            });
            formFeedback.textContent = "";
            itemSearchBar.hidden = false;
            renderReview();
        }

        function questionMarkup(item, draft) {
            return '<div class="qc-client-questions"><h4>سؤال‌های کنترل کیفیت این قلم</h4>'
                + review.questions.map((question, index) => {
                    const option = (label, answer) => '<label class="qc-client-choice"><input type="radio"'
                        + ' name="q-' + escape(item.factorItemId) + '-' + escape(question.id) + '"'
                        + ' data-qc-answer="' + escape(item.factorItemId) + '" data-qc-question="' + escape(question.id) + '"'
                        + ' value="' + answer + '"' + (draft.answers[question.id] === answer ? " checked" : "") + '>'
                        + '<span>' + escape(label) + '</span></label>';
                    return '<fieldset class="qc-client-question"><legend>' + faNumber.format(index + 1) + '. '
                        + escape(question.title) + '</legend><p>' + escape(question.questionText) + '</p>'
                        + '<div class="qc-client-choices">' + option(question.zeroAnswerText, 0)
                        + option(question.oneAnswerText, 1) + '</div></fieldset>';
                }).join("")
                + '<label class="qc-client-note-label" for="qc-note-' + escape(item.factorItemId) + '">'
                + 'شرح مغایرت <small>(اختیاری)</small></label>'
                + '<textarea id="qc-note-' + escape(item.factorItemId)
                + '" data-qc-note="' + escape(item.factorItemId)
                + '" maxlength="4000" class="form-control" rows="2"'
                + ' placeholder="توضیحات مشتری درباره این قلم...">' + escape(draft.description) + '</textarea></div>';
        }

        function renderItem(item) {
            const draft = stateOf(item);
            const isProblem = draft.status === 2;
            const badge = draft.status === 0 ? "بررسی‌نشده" : isProblem ? "دارای مغایرت" : "بدون مغایرت";
            const badgeClass = draft.status === 0 ? "pending" : isProblem ? "discrepancy" : "complete";
            const product = item.productSmallName || item.product || "قلم بدون عنوان";
            const description = item.product && item.product !== product ? item.product : "";
            return '<article class="qc-client-item" data-qc-item="' + escape(item.factorItemId) + '">'
                + '<div class="qc-client-item-top">'
                + '<div class="qc-client-item-meta">'
                + '<strong>' + escape(product) + '</strong>'
                + (description ? '<span class="qc-client-item-spec qc-client-item-value">' + escape(description) + '</span>' : "")
                + '<span>مقدار: <span class="qc-client-item-value">' + escape(item.weightCount || "—") + ' ' + escape(item.unitTxt || "") + '</span></span>'
                + '<span class="qc-client-party' + (item.supplierNames?.trim() ? '' : ' qc-client-party-unset')
                + '">تأمین‌کننده: <span class="qc-client-item-value">' + escape(item.supplierNames?.trim() || "ثبت نشده") + '</span></span>'
                + '<span class="qc-client-party' + (item.supplyExpert?.trim() ? '' : ' qc-client-party-unset')
                + '">کارشناس تأمین: <span class="qc-client-item-value">' + escape(item.supplyExpert?.trim() || "ثبت نشده") + '</span></span>'
                + '</div>'
                + '<div class="qc-client-item-controls">'
                + '<div class="qc-client-item-actions">'
                + '<button type="button" class="qc-client-button ' + (draft.status === 1 ? "qc-client-ok-selected" : "qc-client-secondary")
                + '" data-qc-set-status="1" data-qc-item-id="' + escape(item.factorItemId) + '">بدون مغایرت</button>'
                + '<button type="button" class="qc-client-button ' + (isProblem ? "qc-client-issue-selected" : "qc-client-secondary")
                + '" data-qc-set-status="2" data-qc-item-id="' + escape(item.factorItemId) + '">دارای مغایرت</button>'
                + '</div>'
                + '<span class="qc-client-badge qc-client-' + badgeClass + '">' + badge
                + (draft.saved ? ' · ثبت‌شده' : "") + '</span>'
                + '<button type="button" class="qc-client-button qc-client-retry" data-qc-retry="' + escape(item.factorItemId)
                + '"' + (draft.failed ? "" : " hidden") + '>تلاش مجدد</button>'
                + '</div></div>'
                + (isProblem ? questionMarkup(item, draft) : "")
                + '</article>';
        }

        function renderReview() {
            if (!review) return;
            const scrollPosition = scroll.scrollTop;
            content.innerHTML = review.orders.map(order => '<section class="qc-client-order">'
                + '<div class="qc-client-order-header"><h3>سفارش شماره ' + escape(order.serialNumber)
                + '</h3><small>' + escape(order.title || "") + '</small></div>'
                + order.invoices.map(invoice => '<section class="qc-client-invoice">'
                    + '<h4>فاکتور فروش شماره ' + escape(invoice.serialNumber)
                    + (invoice.date ? '<small> · ' + escape(invoice.date) + '</small>' : "") + '</h4>'
                    + invoice.items.map(renderItem).join("")
                    + '</section>').join("")
                + '<div class="qc-client-order-footer"><button type="button" class="qc-client-button qc-client-outline"'
                + ' data-qc-mark-rest="' + escape(order.id) + '"'
                + (order.invoices.flatMap(invoice => invoice.items)
                    .every(item => stateOf(item).status !== 0) ? " disabled" : "") + '>'
                + 'ثبت بقیه اقلام این سفارش بدون مغایرت</button></div></section>').join("");
            refreshReviewState();
            filterInvoiceItems();
            scroll.scrollTop = scrollPosition;
        }

        function refreshReviewState() {
            if (!review) { saveAllButton.disabled = true; return; }
            const items = flatten();
            const itemsById = new Map(items.map(item => [item.factorItemId, item]));
            const reviewed = items.filter(item => stateOf(item).saved && stateOf(item).status !== 0).length;
            const pending = items.filter(item => stateOf(item).status === 0).length;
            const issues = items.filter(item => stateOf(item).status === 2).length;
            summary.innerHTML = '<div><strong>' + faNumber.format(reviewed) + ' از '
                + faNumber.format(items.length) + ' قلم ثبت‌شده</strong>'
                + '<small>' + faNumber.format(review.orders.length) + ' سفارش · '
                + faNumber.format(review.orders.reduce((sum, order) => sum + order.invoices.length, 0))
                + ' فاکتور</small></div><div class="qc-client-summary-tags">'
                + '<span>' + faNumber.format(pending) + ' بررسی‌نشده</span>'
                + '<span>' + faNumber.format(issues) + ' دارای مغایرت</span></div>';

            const busy = hasUnsavedWork();
            saveAllButton.disabled = busy || pending === 0;
            saveHint.textContent = busy ? "در حال ثبت خودکار اطلاعات…"
                : pending ? "ثبت کلی فقط اقلام بررسی‌نشده را بدون مغایرت ثبت می‌کند."
                    : "تمام اقلام وضعیت مشخصی دارند.";
            content.querySelectorAll("[data-qc-item]").forEach(row => {
                const draft = drafts.get(row.dataset.qcItem);
                if (!draft) return;
                const badge = row.querySelector(".qc-client-item-top .qc-client-badge");
                const text = draft.status === 2 ? "دارای مغایرت"
                    : draft.status === 1 ? "بدون مغایرت" : "بررسی‌نشده";
                const color = draft.status === 2 ? "discrepancy"
                    : draft.status === 1 ? "complete" : "pending";
                if (badge) {
                    badge.className = "qc-client-badge qc-client-" + color;
                    badge.textContent = text + (savingItems.has(row.dataset.qcItem) ? " · در حال ثبت"
                        : draft.failed ? " · خطای ثبت"
                            : draft.saved ? " · ثبت‌شده"
                                : draft.status === 2 && !isValid(itemsById.get(row.dataset.qcItem))
                                    ? " · تکمیل سؤال‌ها" : "");
                }
                const retry = row.querySelector("[data-qc-retry]");
                if (retry) {
                    retry.hidden = !draft.failed;
                    retry.disabled = busy;
                }
            });
            content.querySelectorAll("[data-qc-mark-rest]").forEach(button => {
                const order = review.orders.find(row => row.id === button.dataset.qcMarkRest);
                button.disabled = busy || !order || order.invoices.flatMap(invoice => invoice.items)
                    .every(item => stateOf(item).status !== 0);
            });
            modal.querySelectorAll("[data-bs-dismiss]").forEach(button => button.disabled = busy);
        }

        function makePayload(item) {
            const draft = stateOf(item);
            return {
                factorItemId: item.factorItemId,
                status: draft.status,
                description: draft.status === 2 ? draft.description : null,
                rowVersion: draft.rowVersion,
                answers: draft.status === 2 ? review.questions.map(question =>
                    ({ questionId: question.id, answer: draft.answers[question.id] })) : [],
            };
        }

        async function persistItem(itemId) {
            if (!review || !isActive() || saving || savingItems.has(itemId)) return;
            const item = flatten().find(item => item.factorItemId === itemId);
            if (!item) return;
            const draft = stateOf(item);
            if (!draft || !isValid(item)) return;
            // Unchecked items that have never been saved need no database record.
            if (draft.status === 0 && !draft.rowVersion) return;

            const generation = formGeneration;
            const customerId = review.customerId;
            savingItems.add(itemId);
            refreshReviewState();
            let succeeded = false;
            try {
                while (review && generation === formGeneration && isActive() && isValid(item)) {
                    const revision = draft.revision;
                    const payload = makePayload(item);
                    const result = await sendApiRequest({
                        url: "/QualityControl/SaveItems", method: "POST",
                        headers: { RequestVerificationToken: token.value },
                        data: { customerId, saveAll: false, items: [payload] },
                        showLoading: false,
                    });
                    if (!isActive() || generation !== formGeneration || review?.customerId !== customerId) return;
                    const version = result?.versions?.find(item => item.factorItemId === itemId);
                    if (!version?.rowVersion) {
                        draft.failed = true;
                        formFeedback.textContent = "ثبت خودکار یک قلم انجام نشد؛ وضعیت قلم را بررسی کنید یا «تلاش مجدد» را بزنید.";
                        break;
                    }
                    draft.rowVersion = version.rowVersion;
                    draft.failed = false;
                    succeeded = true;
                    if (draft.revision === revision) {
                        draft.saved = payload.status !== 0;
                        break;
                    }
                    // If edited during a request, submit the latest draft with the new rowversion.
                    draft.saved = false;
                    if (draft.status === 0 && !draft.rowVersion) break;
                }
            } catch (error) {
                if (generation === formGeneration && isActive()) {
                    draft.failed = true;
                    formFeedback.textContent = "ارتباط با سرور هنگام ثبت خودکار قطع شد؛ برای ثبت مجدد تلاش کنید.";
                }
            } finally {
                savingItems.delete(itemId);
                if (generation === formGeneration && isActive()) {
                    if (succeeded && !flatten().some(item => stateOf(item).failed))
                        formFeedback.textContent = "";
                    refreshReviewState();
                    if (succeeded) loadCustomers();
                }
            }
        }

        async function saveRemaining(items) {
            if (!review || !isActive() || hasUnsavedWork()) return;
            const pending = items.filter(item => stateOf(item).status === 0);
            if (!pending.length) return;
            const generation = formGeneration, customerId = review.customerId;
            const payloads = pending.map(item => ({
                ...makePayload(item), status: 1, description: null, answers: [],
            }));
            saving = true;
            refreshReviewState();
            formFeedback.textContent = "در حال ثبت اقلام باقی‌مانده…";
            try {
                const result = await sendApiRequest({
                    url: "/QualityControl/SaveItems", method: "POST",
                    headers: { RequestVerificationToken: token.value },
                    data: { customerId, saveAll: true, items: payloads },
                    showLoading: false,
                });
                if (!isActive() || generation !== formGeneration || review?.customerId !== customerId) return;
                if (!Array.isArray(result?.versions) || result.versions.length !== pending.length) {
                    formFeedback.textContent = "ثبت گروهی انجام نشد؛ دوباره تلاش کنید.";
                    return;
                }
                const versions = new Map(result.versions.map(value => [value.factorItemId, value.rowVersion]));
                pending.forEach(item => {
                    const draft = stateOf(item);
                    draft.rowVersion = versions.get(item.factorItemId);
                    draft.status = 1;
                    draft.answers = {};
                    draft.saved = true;
                    draft.failed = false;
                    draft.revision++;
                });
                formFeedback.textContent = "";
                renderReview();
                await loadCustomers();
            } catch (error) {
                if (generation === formGeneration && isActive())
                    formFeedback.textContent = "خطا در ارتباط با سرور هنگام ثبت گروهی.";
            } finally {
                saving = false;
                if (generation === formGeneration && isActive()) refreshReviewState();
            }
        }

        form.addEventListener("submit", event => event.preventDefault());
        itemSearch.addEventListener("input", filterInvoiceItems);
        itemSearchClear.addEventListener("click", () => {
            itemSearch.value = "";
            filterInvoiceItems();
            itemSearch.focus();
        });

        page.querySelector("#qcCustomerSearchForm").addEventListener("submit", event => {
            event.preventDefault();
            search = searchInput.value.trim();
            loadCustomers(true);
        });
        searchInput.addEventListener("input", () => {
            if (!searchInput.value && search) { search = ""; loadCustomers(true); }
        });
        statusFilter.addEventListener("change", () => loadCustomers(true));
        sortControl.addEventListener("change", () => loadCustomers(true));
        pageSize.addEventListener("change", () => loadCustomers(true));
        page.querySelector(".qc-client-overview").addEventListener("click", event => {
            const button = event.target.closest("[data-qc-card-status]");
            if (!button) return;
            // The overview totals are global; reset search to show every customer in the selected group.
            search = "";
            searchInput.value = "";
            statusFilter.value = button.dataset.qcCardStatus;
            loadCustomers(true);
        });
        pages.addEventListener("click", event => {
            const button = event.target.closest("[data-qc-page]");
            if (!button || button.disabled || loading) return;
            start += button.dataset.qcPage === "next" ? Number(pageSize.value) : -Number(pageSize.value);
            loadCustomers();
        });
        queue.addEventListener("click", async event => {
            const contactButton = event.target.closest("[data-qc-contact]");
            if (contactButton) {
                if (loading || contactBusy || !isActive()) return;
                contactBusy = true;
                queue.querySelectorAll("[data-qc-contact]").forEach(button => button.disabled = true);
                queueFeedback.textContent = "در حال ثبت وضعیت تماس…";
                try {
                    const result = await sendApiRequest({
                        url: "/QualityControl/SetCustomerContactStatus",
                        method: "POST",
                        headers: { RequestVerificationToken: token.value },
                        data: {
                            customerId: contactButton.dataset.qcContactCustomer,
                            status: Number(contactButton.dataset.qcContact),
                        },
                        showLoading: false,
                    });
                    if (!isActive()) return;
                    if (!result || ![0, 1, 2].includes(Number(result.status))) {
                        queueFeedback.textContent = "ثبت وضعیت تماس ناموفق بود. دوباره تلاش کنید.";
                        return;
                    }
                    await loadCustomers();
                } catch (error) {
                    if (isActive()) queueFeedback.textContent = "خطا در ثبت وضعیت تماس؛ دوباره تلاش کنید.";
                } finally {
                    contactBusy = false;
                    if (isActive()) queue.querySelectorAll("[data-qc-contact]")
                        .forEach(button => button.disabled = false);
                }
                return;
            }
            const button = event.target.closest("[data-qc-customer]");
            if (button && !loading && !contactBusy) openCustomer(button.dataset.qcCustomer);
        });
        content.addEventListener("click", event => {
            if (saving || !review) return;
            const setButton = event.target.closest("[data-qc-set-status]");
            if (setButton) {
                const id = setButton.dataset.qcItemId;
                const draft = drafts.get(id);
                if (!draft) return;
                const selected = Number(setButton.dataset.qcSetStatus);
                // Clicking the selected answer again returns the item to unchecked.
                draft.status = draft.status === selected ? 0 : selected;
                draft.saved = false;
                draft.failed = false;
                draft.revision++;
                clearNoteTimer(id);
                if (draft.status === 1) draft.answers = {};
                renderReview();
                persistItem(id);
                return;
            }
            const retry = event.target.closest("[data-qc-retry]");
            if (retry && !retry.disabled) {
                const draft = drafts.get(retry.dataset.qcRetry);
                if (draft) {
                    draft.failed = false;
                    persistItem(retry.dataset.qcRetry);
                }
                return;
            }
            const markRest = event.target.closest("[data-qc-mark-rest]");
            if (markRest && !markRest.disabled) {
                const order = review.orders.find(order => order.id === markRest.dataset.qcMarkRest);
                if (order) saveRemaining(order.invoices.flatMap(invoice => invoice.items));
            }
        });
        content.addEventListener("change", event => {
            const radio = event.target.closest("[data-qc-answer]");
            if (!radio || !review) return;
            const draft = drafts.get(radio.dataset.qcAnswer);
            if (!draft) return;
            draft.answers[radio.dataset.qcQuestion] = Number(radio.value);
            draft.saved = false;
            draft.failed = false;
            draft.revision++;
            clearNoteTimer(radio.dataset.qcAnswer);
            refreshReviewState();
            if (draft.status === 2) persistItem(radio.dataset.qcAnswer);
        });
        content.addEventListener("input", event => {
            const note = event.target.closest("[data-qc-note]");
            if (!note || !review) return;
            const draft = drafts.get(note.dataset.qcNote);
            if (!draft) return;
            draft.description = note.value;
            draft.saved = false;
            draft.failed = false;
            draft.revision++;
            clearNoteTimer(note.dataset.qcNote);
            if (draft.status === 2 && isValid(flatten().find(item => item.factorItemId === note.dataset.qcNote))) {
                noteTimers.set(note.dataset.qcNote, setTimeout(() => {
                    noteTimers.delete(note.dataset.qcNote);
                    persistItem(note.dataset.qcNote);
                }, 650));
            }
            refreshReviewState();
        });
        saveAllButton.addEventListener("click", () => {
            if (!saveAllButton.disabled) saveRemaining(flatten());
        });
        modal.addEventListener("hide.bs.modal", event => {
            if (hasUnsavedWork()) { event.preventDefault(); return; }
            formController?.abort();
            formGeneration++;
        });
        modal.addEventListener("hidden.bs.modal", () => {
            review = null;
            drafts.clear();
            content.replaceChildren();
            summary.replaceChildren();
            itemSearch.value = "";
            itemSearchBar.hidden = true;
            itemSearchEmpty.hidden = true;
            formFeedback.textContent = "";
        });
        pageSignal?.addEventListener("abort", () => {
            listController?.abort();
            formController?.abort();
        }, { once: true });
        loadCustomers();
    });
})();
