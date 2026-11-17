(() => {
    "use strict";

    document.addEventListener("DOMContentLoaded", () => {
        const page = document.getElementById("qualityControlCustomerPage");
        if (!page) return;
        const queue = page.querySelector("#qcCustomerQueue");
        const queueFeedback = page.querySelector("#qcCustomerQueueFeedback");
        const queueCount = page.querySelector("#qcCustomerQueueCount");
        const pages = page.querySelector("#qcCustomerPages");
        const modal = page.querySelector("#qcCustomerReviewModal");
        const form = page.querySelector("#qcCustomerReviewForm");
        const formFeedback = page.querySelector("#qcCustomerFormFeedback");
        const summary = page.querySelector("#qcCustomerReviewSummary");
        const content = page.querySelector("#qcCustomerReviewContent");
        const scroll = page.querySelector("#qcCustomerReviewScroll");
        const saveAllButton = page.querySelector("#qcCustomerSaveAll");
        const saveHint = page.querySelector("#qcCustomerSaveHint");
        const searchInput = page.querySelector("#qcCustomerSearch");
        const statusFilter = page.querySelector("#qcCustomerStatus");
        const pageSize = page.querySelector("#qcCustomerPageSize");
        const token = form.querySelector('[name="__RequestVerificationToken"]');
        const faNumber = new Intl.NumberFormat("fa-IR");
        const escape = value => String(value ?? "").replace(/[&<>"']/g, char =>
            ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[char]);
        const pageSignal = typeof PageRuntime !== "undefined" ? PageRuntime.current?.signal : null;
        const isActive = () => page.isConnected && !pageSignal?.aborted;

        let start = 0, total = 0, search = "", loading = false, saving = false;
        let listController = null, formController = null, formGeneration = 0;
        let review = null;
        const drafts = new Map();
        const flatten = () => review?.orders.flatMap(order =>
            order.invoices.flatMap(invoice => invoice.items)) ?? [];
        const stateOf = item => drafts.get(item.factorItemId);

        function isValid(item) {
            const draft = stateOf(item);
            if (!draft) return false;
            if (draft.status === 1) return true;
            if (draft.status !== 2 || !review.questions?.length) return false;
            return review.questions.every(question => draft.answers[question.id] === 0
                || draft.answers[question.id] === 1)
                && review.questions.some(question => draft.answers[question.id] === 1);
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

        async function loadCustomers(reset = false) {
            if (!isActive()) return;
            if (reset) start = 0;
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
                    },
                    signal: controller.signal,
                    showLoading: false,
                });
                if (!isActive() || controller.signal.aborted) return;
                if (!result || !Array.isArray(result.data)) {
                    queueFeedback.textContent = "دریافت صف کنترل کیفیت انجام نشد. دوباره جستجو کنید.";
                    return;
                }
                total = Number(result.filtered ?? 0);
                if (start >= total && start > 0) {
                    start = Math.max(0, (Math.ceil(total / Number(pageSize.value)) - 1) * Number(pageSize.value));
                    return loadCustomers();
                }
                queue.innerHTML = result.data.map(row => {
                    const pending = row.totalItems - row.reviewedItems;
                    const color = pending > 0 ? "pending" : row.discrepancyItems > 0 ? "discrepancy" : "complete";
                    const label = pending > 0 ? "نیازمند بررسی" : row.discrepancyItems > 0 ? "دارای مغایرت" : "تکمیل‌شده";
                    return '<article role="listitem" class="qc-client-card">'
                        + '<div class="qc-client-profile"><strong>' + escape(row.customerName || "مشتری بدون نام") + '</strong>'
                        + '<small>کد مشتری: ' + escape(row.customerCode) + ' · ' + escape(row.mobile || "بدون شماره") + '</small></div>'
                        + '<div class="qc-client-stats"><span>' + faNumber.format(row.orders) + ' سفارش</span>'
                        + '<span>' + faNumber.format(row.invoices) + ' فاکتور</span>'
                        + '<span>' + faNumber.format(row.reviewedItems) + ' از ' + faNumber.format(row.totalItems) + ' قلم بررسی‌شده</span></div>'
                        + '<span class="qc-client-badge qc-client-' + color + '">' + label + '</span>'
                        + '<button type="button" class="qc-client-button qc-client-primary" data-qc-customer="' + escape(row.customerId) + '">'
                        + (pending > 0 ? "کنترل کیفیت" : "مشاهده / ویرایش") + '</button></article>';
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
            if (saving || !isActive()) return;
            formController?.abort();
            const controller = formController = new AbortController();
            const generation = ++formGeneration;
            drafts.clear();
            review = null;
            saveAllButton.disabled = true;
            summary.replaceChildren();
            content.replaceChildren();
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
            page.querySelector("#qcCustomerModalSubtitle").textContent = "کد مشتری: " + faNumber.format(review.customerCode);
            flatten().forEach(item => {
                drafts.set(item.factorItemId, {
                    status: Number(item.status) || 0,
                    description: item.description || "",
                    rowVersion: item.rowVersion || null,
                    saved: Boolean(item.rowVersion),
                    answers: Object.fromEntries((item.answers || []).map(answer =>
                        [answer.id, answer.answer])),
                });
            });
            formFeedback.textContent = "";
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
                + (description ? '<span class="qc-client-item-spec">' + escape(description) + '</span>' : "")
                + '<span>مقدار: ' + escape(item.weightCount || "—") + ' ' + escape(item.unitTxt || "") + '</span>'
                + (item.supplierNames ? '<span>تأمین‌کننده: ' + escape(item.supplierNames) + '</span>' : "")
                + (item.supplyExpert ? '<span>کارشناس تأمین: ' + escape(item.supplyExpert) + '</span>' : "")
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
                + '<button type="button" class="qc-client-button qc-client-outline" data-qc-save-item="' + escape(item.factorItemId) + '"'
                + (draft.saved || !isValid(item) ? " disabled" : "") + '>'
                + (draft.saved ? "ثبت‌شده" : "ثبت این قلم") + '</button>'
                + '</div></div>'
                + (isProblem ? questionMarkup(item, draft) : "")
                + '</article>';
        }

        function renderReview() {
            if (!review) return;
            const scrollPosition = scroll.scrollTop;
            const items = flatten();
            const reviewed = items.filter(item => stateOf(item).saved).length;
            const pending = items.filter(item => stateOf(item).status === 0).length;
            const issues = items.filter(item => stateOf(item).status === 2).length;
            summary.innerHTML = '<div><strong>' + faNumber.format(reviewed) + ' از '
                + faNumber.format(items.length) + ' قلم ثبت‌شده</strong>'
                + '<small>' + faNumber.format(review.orders.length) + ' سفارش · '
                + faNumber.format(review.orders.reduce((sum, order) => sum + order.invoices.length, 0))
                + ' فاکتور</small></div><div class="qc-client-summary-tags">'
                + '<span>' + faNumber.format(pending) + ' بررسی‌نشده</span>'
                + '<span>' + faNumber.format(issues) + ' دارای مغایرت</span></div>';
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
            updateSaveActions();
            scroll.scrollTop = scrollPosition;
        }

        function updateSaveActions() {
            if (!review) { saveAllButton.disabled = true; return; }
            const items = flatten();
            const ready = items.length > 0 && items.every(isValid);
            const dirty = items.some(item => !stateOf(item).saved);
            saveAllButton.disabled = saving || !ready || !dirty;
            saveHint.textContent = ready ? dirty ? "آماده ثبت کلی تمام فاکتورها" : "تمام اقلام ثبت شده‌اند."
                : "برای ثبت کلی، وضعیت همه اقلام و پاسخ سؤال‌های مغایرت را تکمیل کنید.";
            content.querySelectorAll("[data-qc-save-item]").forEach(button => {
                const item = items.find(item => item.factorItemId === button.dataset.qcSaveItem);
                if (!item) return;
                const draft = stateOf(item);
                button.disabled = saving || !isValid(item) || draft.saved;
                button.textContent = draft.saved ? "ثبت‌شده" : "ثبت این قلم";
                const badge = button.closest("[data-qc-item]")?.querySelector(".qc-client-item-top .qc-client-badge");
                if (badge) badge.textContent = (draft.status === 2 ? "دارای مغایرت"
                    : draft.status === 1 ? "بدون مغایرت" : "بررسی‌نشده") + (draft.saved ? " · ثبت‌شده" : "");
            });
            modal.querySelectorAll("[data-bs-dismiss]").forEach(button => button.disabled = saving);
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

        async function saveItems(items, saveAll = false) {
            if (!isActive() || saving || !review || !items.length || items.some(item => !isValid(item))) return;
            saving = true;
            updateSaveActions();
            formFeedback.textContent = "در حال ثبت اطلاعات…";
            try {
                const result = await sendApiRequest({
                    url: "/QualityControl/SaveItems", method: "POST",
                    headers: { RequestVerificationToken: token.value },
                    data: { customerId: review.customerId, saveAll, items: items.map(makePayload) },
                    showLoading: false,
                    successMsg: saveAll ? "کنترل کیفیت تمام اقلام ثبت شد." : "کنترل کیفیت قلم ثبت شد.",
                });
                if (!result || !isActive()) {
                    formFeedback.textContent = "ثبت انجام نشد؛ اطلاعات را بررسی و دوباره تلاش کنید.";
                    return;
                }
                (result.versions || []).forEach(saved => {
                    const draft = drafts.get(saved.factorItemId);
                    if (draft) {
                        draft.saved = true;
                        draft.rowVersion = saved.rowVersion;
                    }
                });
                formFeedback.textContent = "";
                if (saveAll) {
                    ModalToggle(modal, "hide");
                } else {
                    renderReview();
                }
                await loadCustomers();
            } finally {
                saving = false;
                if (isActive()) updateSaveActions();
            }
        }

        page.querySelector("#qcCustomerSearchForm").addEventListener("submit", event => {
            event.preventDefault();
            search = searchInput.value.trim();
            loadCustomers(true);
        });
        searchInput.addEventListener("input", () => {
            if (!searchInput.value && search) { search = ""; loadCustomers(true); }
        });
        statusFilter.addEventListener("change", () => loadCustomers(true));
        pageSize.addEventListener("change", () => loadCustomers(true));
        pages.addEventListener("click", event => {
            const button = event.target.closest("[data-qc-page]");
            if (!button || button.disabled || loading) return;
            start += button.dataset.qcPage === "next" ? Number(pageSize.value) : -Number(pageSize.value);
            loadCustomers();
        });
        queue.addEventListener("click", event => {
            const button = event.target.closest("[data-qc-customer]");
            if (button && !loading) openCustomer(button.dataset.qcCustomer);
        });
        content.addEventListener("click", event => {
            if (saving || !review) return;
            const setButton = event.target.closest("[data-qc-set-status]");
            if (setButton) {
                const draft = drafts.get(setButton.dataset.qcItemId);
                const nextStatus = Number(setButton.dataset.qcSetStatus);
                if (draft && draft.status !== nextStatus) {
                    draft.status = nextStatus;
                    draft.saved = false;
                    if (nextStatus === 1) draft.answers = {};
                    renderReview();
                }
                return;
            }
            const markRest = event.target.closest("[data-qc-mark-rest]");
            if (markRest) {
                const order = review.orders.find(order => order.id === markRest.dataset.qcMarkRest);
                order?.invoices.flatMap(invoice => invoice.items).forEach(item => {
                    const draft = stateOf(item);
                    if (draft.status === 0) { draft.status = 1; draft.saved = false; }
                });
                renderReview();
                return;
            }
            const saveButton = event.target.closest("[data-qc-save-item]");
            if (saveButton && !saveButton.disabled) {
                const item = flatten().find(item => item.factorItemId === saveButton.dataset.qcSaveItem);
                if (item) saveItems([item]);
            }
        });
        content.addEventListener("change", event => {
            const radio = event.target.closest("[data-qc-answer]");
            if (!radio || !review) return;
            const draft = drafts.get(radio.dataset.qcAnswer);
            if (!draft) return;
            draft.answers[radio.dataset.qcQuestion] = Number(radio.value);
            draft.saved = false;
            updateSaveActions();
        });
        content.addEventListener("input", event => {
            const note = event.target.closest("[data-qc-note]");
            if (!note || !review) return;
            const draft = drafts.get(note.dataset.qcNote);
            if (!draft) return;
            draft.description = note.value;
            draft.saved = false;
            updateSaveActions();
        });
        saveAllButton.addEventListener("click", () => {
            if (!saveAllButton.disabled) saveItems(flatten(), true);
        });
        modal.addEventListener("hide.bs.modal", event => {
            if (saving) { event.preventDefault(); return; }
            formController?.abort();
            formGeneration++;
        });
        modal.addEventListener("hidden.bs.modal", () => {
            review = null;
            drafts.clear();
            content.replaceChildren();
            summary.replaceChildren();
        });
        pageSignal?.addEventListener("abort", () => {
            listController?.abort();
            formController?.abort();
        }, { once: true });
        loadCustomers();
    });
})();
