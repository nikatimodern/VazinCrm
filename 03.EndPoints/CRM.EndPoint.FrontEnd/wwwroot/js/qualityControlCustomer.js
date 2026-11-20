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
            ["noResponseCustomers", "#qcSummaryTomorrow"],
            ["dueFollowUpsToday", "#qcSummaryDueToday"],
            ["totalNoResponseAttempts", "#qcSummaryNoAnswerAttempts"],
            ["pendingItems", "#qcSummaryPendingItems"],
            ["reviewedItems", "#qcSummaryReviewedItems"],
            ["discrepancyItems", "#qcSummaryDiscrepancyItems"],
            ["totalItems", "#qcSummaryDiscrepancyTotalItems"],
            ["noDiscrepancyItems", "#qcSummaryNoDiscrepancyItems"],
            ["completedCustomers", "#qcSummaryCompletedCustomers"],
            ["totalItems", "#qcSummaryTotalItems"],
        ].map(([property, selector]) => [property, page.querySelector(selector)]);
        const progressPercent = page.querySelector("#qcSummaryProgressPercent");
        const progressFill = page.querySelector("#qcSummaryProgressFill");
        const progressTrack = page.querySelector("#qcSummaryProgressTrack");
        const unreviewedCount = page.querySelector("#qcSummaryUnreviewedItems");
        const pausedCount = page.querySelector("#qcSummaryPausedItems");
        const pendingTotalItems = page.querySelector("#qcSummaryPendingTotalItems");
        const pendingTotalCustomers = page.querySelector("#qcSummaryPendingTotalCustomers");
        const reviewedTotalCustomers = page.querySelector("#qcSummaryReviewedTotalCustomers");
        const unfinishedCustomersCount = page.querySelector("#qcSummaryUnfinishedCustomers");
        const pages = page.querySelector("#qcCustomerPages");
        const modal = page.querySelector("#qcCustomerReviewModal");
        const historyModal = page.querySelector("#qcContactHistoryModal");
        const notesModal = page.querySelector("#qcCustomerNotesModal");
        const notesSubtitle = page.querySelector("#qcCustomerNotesModalSubtitle");
        const historyCustomer = page.querySelector("#qcContactHistoryCustomer");
        const historyEntries = page.querySelector("#qcContactHistoryEntries");
        const historyFeedback = page.querySelector("#qcContactHistoryFeedback");
        const historyPages = page.querySelector("#qcContactHistoryPages");
        const historyDeletedFilter = page.querySelector("#qcContactHistoryDeletedFilter");
        const historyShowDeleted = page.querySelector("#qcContactHistoryShowDeleted");
        const form = page.querySelector("#qcCustomerReviewForm");
        const formFeedback = page.querySelector("#qcCustomerFormFeedback");
        const customerNotesList = page.querySelector("#qcCustomerNotesList");
        const customerNotesCount = page.querySelector("#qcCustomerNotesCount");
        const customerNotesMore = page.querySelector("#qcCustomerNotesMore");
        const customerNoteText = page.querySelector("#qcCustomerNoteText");
        const customerNoteSave = page.querySelector("#qcCustomerNoteSave");
        const customerNoteClear = page.querySelector("#qcCustomerNoteClear");
        const customerNoteCancelEdit = page.querySelector("#qcCustomerNoteCancelEdit");
        const customerNoteSaveLabel = page.querySelector("#qcCustomerNoteSaveLabel");
        const customerNoteLabel = page.querySelector("#qcCustomerNoteLabel");
        const customerNoteFeedback = page.querySelector("#qcCustomerNoteFeedback");
        const reviewToolbar = page.querySelector("#qcCustomerReviewToolbar");
        const summary = page.querySelector("#qcCustomerReviewSummary");
        const content = page.querySelector("#qcCustomerReviewContent");
        const itemSearchBar = page.querySelector("#qcInvoiceItemSearchBar");
        const itemSearch = page.querySelector("#qcInvoiceItemSearch");
        const itemSearchCount = page.querySelector("#qcInvoiceItemSearchCount");
        const itemSearchEmpty = page.querySelector("#qcInvoiceItemSearchEmpty");
        const itemStatusFilters = page.querySelector("#qcInvoiceItemStatusFilters");
        const itemPageSize = page.querySelector("#qcInvoiceItemPageSize");
        const itemPaging = page.querySelector("#qcInvoiceItemPaging");
        const itemPages = page.querySelector("#qcInvoiceItemPages");
        const itemPagingCount = page.querySelector("#qcInvoiceItemPagingCount");
        const issueOverlay = page.querySelector("#qcItemDiscrepancyDialog");
        const issueForm = page.querySelector("#qcIssueForm");
        const issueQuestions = page.querySelector("#qcIssueQuestions");
        const issueTitle = page.querySelector("#qcIssueTitle");
        const issueSubtitle = page.querySelector("#qcIssueSubtitle");
        const issueFeedback = page.querySelector("#qcIssueFeedback");
        const saveAllButton = page.querySelector("#qcCustomerSaveAll");
        const saveHint = page.querySelector("#qcCustomerSaveHint");
        const searchInput = page.querySelector("#qcCustomerSearch");
        const statusFilter = page.querySelector("#qcCustomerStatus");
        const sortControl = page.querySelector("#qcCustomerSort");
        const cardButtons = [...page.querySelectorAll("[data-qc-card-status]")];
        const pageSize = page.querySelector("#qcCustomerPageSize");
        const token = form.querySelector('[name="__RequestVerificationToken"]');
        const notesToken = notesModal.querySelector('[name="__RequestVerificationToken"]');
        const faNumber = new Intl.NumberFormat("fa-IR");
        const faCodeNumber = new Intl.NumberFormat("fa-IR", { useGrouping: false });
        const escape = value => String(value ?? "").replace(/[&<>"']/g, char =>
            ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[char]);
        const pageSignal = typeof PageRuntime !== "undefined" ? PageRuntime.current?.signal : null;
        const isActive = () => page.isConnected && !pageSignal?.aborted;

        // Keep independent list scrolling when there is enough room. When
        // the sidebar / wrapped dashboard makes the queue too short, let the
        // whole page scroll instead of hiding nearly every customer card.
        function fitQueueToViewport() {
            if (!isActive()) return;
            const top = Math.max(0, page.getBoundingClientRect().top);
            const available = Math.max(0, Math.floor(window.innerHeight - top - 8));
            page.style.setProperty("--qc-workspace-height", available + "px");

            if (window.innerWidth < 768) {
                page.classList.remove("qc-client-natural-scroll");
                return;
            }

            const outerHeight = element => {
                const style = getComputedStyle(element);
                return element.getBoundingClientRect().height
                    + (parseFloat(style.marginTop) || 0)
                    + (parseFloat(style.marginBottom) || 0);
            };
            const fixedHeight = [...page.children]
                .filter(element => element !== queue && !element.classList.contains("modal"))
                .reduce((sum, element) => sum + outerHeight(element), 0);
            const pageStyle = getComputedStyle(page);
            const padding = (parseFloat(pageStyle.paddingTop) || 0)
                + (parseFloat(pageStyle.paddingBottom) || 0);
            const remainingQueueHeight = available - fixedHeight - padding;
            page.classList.toggle("qc-client-natural-scroll", remainingQueueHeight < 200);
        }
        fitQueueToViewport();
        window.addEventListener("resize", fitQueueToViewport,
            pageSignal ? { signal: pageSignal } : undefined);
        // Responsive text wrapping also changes the available height.
        const queueLayoutObserver = typeof ResizeObserver === "undefined" ? null
            : new ResizeObserver(fitQueueToViewport);
        if (queueLayoutObserver) {
            [...page.children].filter(element =>
                element !== queue && !element.classList.contains("modal"))
                .forEach(element => queueLayoutObserver.observe(element));
        }
        if (typeof enableMomentumScroll === "function")
            enableMomentumScroll(queue, { signal: pageSignal });
        if (typeof enableMomentumScrollScope === "function")
            enableMomentumScrollScope(content, { signal: pageSignal });

        let start = 0, total = 0, search = "", loading = false, saving = false, contactBusy = false;
        let lastCheckedCustomerId = null; // Badge stays until another successfully saved review.
        let lastReviewedHighlightTimer = null;
        let reviewHasSavedChanges = false;
        let listController = null, formController = null, formGeneration = 0;
        let historyCustomerId = null, historyStart = 0, historyTotal = 0;
        let historyController = null, historyBusy = false, historyCanManage = false;
        let historyDeleteHold = null;
        const historyDeleteHoldMs = 500;
        let customerNotesController = null, customerNotesStart = 0, customerNotesTotal = 0;
        let customerNotesLoading = false, customerNoteSaving = false, customerNoteDeleting = false;
        let notesCustomerId = null, notesGeneration = 0;
        let editingCustomerNote = null;
        const customerNoteRows = new Map();
        let midnightRefreshTimer = null;

        // The server is authoritative; the timer only visualizes its 60-second lock.
        const cooldownDurationMs = 60_000;
        const faTwoDigits = new Intl.NumberFormat("fa-IR", {
            minimumIntegerDigits: 2, useGrouping: false,
        });
        const formatCooldown = seconds =>
            faTwoDigits.format(Math.floor(seconds / 60)) + ":" + faTwoDigits.format(seconds % 60);
        function contactCountBadge(count) {
            const value = Math.max(0, Number(count) || 0);
            if (!value) return "";
            const text = value > 9 ? "۹+" : faNumber.format(value);
            return '<span class="qc-client-contact-count" aria-hidden="true">' + text + '</span>';
        }
        function refreshNoResponseCooldowns() {
            const now = Date.now();
            queue.querySelectorAll('[data-qc-contact="1"]').forEach(button => {
                const remainingMs = Math.max(0, Number(button.dataset.qcCooldownUntil || 0) - now);
                const remaining = Math.ceil(remainingMs / 1000);
                const complete = button.closest("[data-qc-complete]")?.dataset.qcComplete === "true";
                const attempts = Math.max(0, Number(button.dataset.qcNoAnswerCount) || 0);
                const showTimer = remaining > 0 && !complete;
                // Preserve the local saving indicator until this request finishes.
                if (button.classList.contains("is-saving")) {
                    button.disabled = true;
                    return;
                }
                button.disabled = contactBusy || complete || remaining > 0;

                const track = button.parentElement?.querySelector("[data-qc-no-answer-progress]");
                if (track) {
                    track.hidden = !showTimer;
                    if (showTimer) {
                        const fill = track.querySelector("[data-qc-no-answer-progress-fill]");
                        if (fill) fill.style.width = (remainingMs / cooldownDurationMs * 100) + "%";
                    }
                }
                button.title = complete ? "عدم پاسخ؛ تمام اقلام بررسی شده‌اند"
                    : showTimer ? "عدم پاسخ؛ " + faNumber.format(attempts)
                        + " تلاش ثبت‌شده؛ ثبت مجدد تا " + faNumber.format(remaining) + " ثانیه دیگر"
                    : attempts > 0 ? "عدم پاسخ؛ " + faNumber.format(attempts)
                        + " تلاش ثبت‌شده؛ ثبت تلاش تماس جدید"
                    : "عدم پاسخ؛ ثبت یک تلاش تماس بدون پاسخ جدید";
                button.setAttribute("aria-label", button.title);
            });
        }
        const noResponseCooldownTimer = setInterval(() => {
            if (isActive()) refreshNoResponseCooldowns();
        }, 1000);

        function scheduleNextDayRefresh() {
            clearTimeout(midnightRefreshTimer);
            const now = new Date();
            const midnight = new Date(now);
            midnight.setHours(24, 0, 0, 0);
            midnightRefreshTimer = setTimeout(() => {
                if (!isActive()) return;
                loadCustomers(true);
                scheduleNextDayRefresh();
            }, Math.max(1000, midnight.getTime() - now.getTime() + 1500));
        }
        let review = null;
        let itemStatusFilter = "0", itemSortField = "order", itemSortDirection = -1;
        let itemPageStart = 0, itemPageLoading = false, itemSearchTimer = null;
        let issueItemId = null, issueReturnFocus = null;
        const drafts = new Map();
        const savingItems = new Set();
        const noteTimers = new Map();
        const flatten = () => review?.data?.map(row => row.item) ?? [];
        const stateOf = item => drafts.get(item.factorItemId);

        const normalizeSearch = text => String(text ?? "").normalize("NFKC")
            .replace(/[يى]/g, "ی").replace(/ك/g, "ک")
            .replace(/[\u064B-\u065F\u0670\u200C\u200D]/g, " ")
            .replace(/[۰-۹]/g, digit => String(digit.charCodeAt(0) - 0x06F0))
            .replace(/[٠-٩]/g, digit => String(digit.charCodeAt(0) - 0x0660))
            .toLocaleLowerCase("fa-IR").replace(/\s+/g, " ").trim();

        // Server returns page data, filtered totals and sort order. Never filter visible DOM rows.
        function filterInvoiceItems() {
            if (!review) return;
            itemSearchCount.textContent = faNumber.format(review.filteredItems) + " از "
                + faNumber.format(review.totalItems) + " قلم";
            itemSearchEmpty.hidden = review.filteredItems !== 0;
            itemStatusFilters.querySelectorAll("[data-qc-item-filter]").forEach(button => {
                const selected = button.dataset.qcItemFilter === itemStatusFilter;
                button.classList.toggle("is-active", selected);
                button.setAttribute("aria-pressed", String(selected));
            });
        }

        function reviewParams(customerId) {
            return {
                customerId, start: itemPageStart, length: Number(itemPageSize.value),
                status: itemStatusFilter === "all" ? "" : itemStatusFilter,
                search: itemSearch.value.trim()
                    .replace(/[۰-۹]/g, digit => String(digit.charCodeAt(0) - 0x06F0))
                    .replace(/[٠-٩]/g, digit => String(digit.charCodeAt(0) - 0x0660)),
                sort: itemSortField,
                descending: itemSortDirection < 0,
            };
        }

        function applyReviewPage(result) {
            review = result;
            itemPageStart = result.start;
            drafts.clear();
            flatten().forEach(item => {
                const status = Number(item.status) || 0;
                drafts.set(item.factorItemId, {
                    status, revision: 0, failed: false,
                    description: item.description || "",
                    rowVersion: item.rowVersion || null,
                    saved: Boolean(item.rowVersion) && status !== 0,
                    answers: Object.fromEntries((item.answers || []).map(answer =>
                        [answer.id, answer.answer])),
                });
            });
            renderReview();
            itemPaging.hidden = false;
            updateReviewPager();
        }

        async function loadReviewPage(reset = false) {
            if (!review || !isActive() || hasUnsavedWork() || issueItemId) return;
            if (reset) itemPageStart = 0;
            formController?.abort();
            const controller = formController = new AbortController();
            const generation = formGeneration, customerId = review.customerId;
            itemPageLoading = true;
            content.classList.add("is-refreshing");
            content.setAttribute("aria-busy", "true");
            refreshReviewState();
            try {
                const result = await sendApiRequest({
                    url: "/QualityControl/GetCustomerForm", params: reviewParams(customerId),
                    signal: controller.signal, showLoading: false,
                });
                if (!isActive() || controller.signal.aborted || generation !== formGeneration
                    || review?.customerId !== customerId) return;
                if (!result?.customerId) throw new Error("Invalid response");
                applyReviewPage(result);
                const viewport = content.querySelector(".qc-client-review-table-wrap");
                if (viewport) viewport.scrollTop = 0;
                formFeedback.textContent = "";
            } catch (error) {
                if (isActive() && !controller.signal.aborted && generation === formGeneration)
                    formFeedback.textContent = "دریافت اقلام ناموفق بود؛ دوباره تلاش کنید.";
            } finally {
                if (generation === formGeneration && !controller.signal.aborted && isActive()) {
                    itemPageLoading = false;
                    content.classList.remove("is-refreshing");
                    content.removeAttribute("aria-busy");
                    refreshReviewState();
                }
            }
        }

        function updateReviewPager() {
            if (!review) return;
            const length = Number(itemPageSize.value), totalRows = review.filteredItems;
            const pages = length ? Math.ceil(totalRows / length) : (totalRows ? 1 : 0);
            const current = length ? Math.floor(review.start / length) + 1 : 1;
            const first = totalRows ? review.start + 1 : 0;
            const last = length ? Math.min(review.start + length, totalRows) : totalRows;
            itemPagingCount.textContent = faNumber.format(first) + " تا "
                + faNumber.format(last) + " از " + faNumber.format(totalRows) + " قلم";
            if (length === 0 || pages <= 1) { itemPages.replaceChildren(); return; }
            const button = (label, target, disabled, active = false) =>
                '<button type="button" data-qc-item-page="' + target
                + '"' + (disabled ? " disabled" : "")
                + (active ? ' aria-current="page" class="is-active"' : "") + '>'
                + label + '</button>';
            let html = button("قبلی", current - 1, current === 1);
            const from = Math.max(1, Math.min(current - 2, pages - 4));
            for (let i = from; i <= Math.min(pages, from + 4); i++)
                html += button(faNumber.format(i), i, false, i === current);
            html += button("بعدی", current + 1, current >= pages);
            itemPages.innerHTML = html;
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

        function showCustomerQueueSkeleton() {
            const card = '<article class="qc-client-card qc-client-skeleton" aria-hidden="true">'
                + '<div class="qc-client-skeleton-profile">'
                + '<span class="qc-client-skeleton-bar is-name"></span>'
                + '<span class="qc-client-skeleton-bar is-detail"></span></div>'
                + '<div class="qc-client-skeleton-stats">'
                + '<span class="qc-client-skeleton-bar is-orders"></span>'
                + '<span class="qc-client-skeleton-bar is-review"></span></div>'
                + '<div class="qc-client-skeleton-actions">'
                + '<span class="qc-client-skeleton-bar is-contacts"></span>'
                + '<span class="qc-client-skeleton-bar is-action"></span></div></article>';
            queue.innerHTML = card.repeat(3);
        }

        function reviewSummaryMarkup(reviewed, count, discrepancy) {
            const complete = reviewed === count;
            const icon = complete ? "fa-check-circle" : "fa-clock";
            const status = complete ? "تکمیل‌شده" : "نیازمند بررسی";
            return '<strong><i class="fa ' + icon + '" aria-hidden="true"></i> ' + status
                + ' · ' + faNumber.format(reviewed) + ' از ' + faNumber.format(count) + ' قلم</strong>'
                + '<small><span class="qc-client-discrepancy-count'
                + (discrepancy > 0 ? ' has-discrepancy' : '') + '">مغایرت: '
                + faNumber.format(discrepancy) + '</span> · بدون مغایرت: '
                + faNumber.format(Math.max(0, reviewed - discrepancy)) + '</small>';
        }

        function clearReviewFocus() {
            clearTimeout(lastReviewedHighlightTimer);
            lastReviewedHighlightTimer = null;
            queue.querySelectorAll(".qc-client-last-reviewed").forEach(card => {
                card.classList.remove("qc-client-last-reviewed");
            });
        }

        // summaryOnly refreshes server totals, without changing any visible card or its position.
        async function loadCustomers(reset = false, summaryOnly = false) {
            if (!isActive()) return;
            if (reset) start = 0;
            if (!summaryOnly) syncCardSelection();
            // A summary-only refresh can interrupt an in-flight list request.
            // Never leave its loading overlay or busy state stuck on the queue.
            if (summaryOnly && loading) {
                loading = false;
                queue.classList.remove("is-refreshing");
                queue.removeAttribute("aria-busy");
                queue.removeAttribute("aria-label");
            }
            listController?.abort();
            const controller = listController = new AbortController();
            if (!summaryOnly) {
                loading = true;
                queue.setAttribute("aria-busy", "true");
                queue.setAttribute("aria-label", "در حال دریافت مشتریان");
                queueFeedback.textContent = "";
                if (queue.querySelector(".qc-client-card:not(.qc-client-skeleton)")) {
                    queue.classList.add("is-refreshing");
                } else {
                    queue.classList.remove("is-refreshing");
                    showCustomerQueueSkeleton();
                }
            }
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
                    if (!summaryOnly) {
                        queue.querySelectorAll(".qc-client-skeleton").forEach(card => card.remove());
                        queueFeedback.textContent = "دریافت صف کنترل کیفیت انجام نشد. دوباره جستجو کنید.";
                    }
                    return;
                }
                if (result.summary) {
                    summaryValues.forEach(([property, element]) => {
                        if (element) element.textContent = faNumber.format(Number(result.summary[property] ?? 0));
                    });
                    // Rate includes ALL eligible invoice items, including those
                    // awaiting another call or correction of an invalid number.
                    const allItems = Math.max(0, Number(result.summary.totalItems) || 0);
                    const checkedItems = Math.max(0, Number(result.summary.reviewedItems) || 0);
                    const actionableItems = Math.max(0, Number(result.summary.pendingItems) || 0);
                    const unreviewedItems = Math.max(0, allItems - checkedItems);
                    // Some unreviewed items belong to clients temporarily out of the
                    // actionable queue because of a no-answer or invalid-number flag.
                    const pausedItems = Math.max(0, unreviewedItems - actionableItems);
                    unreviewedCount.textContent = faNumber.format(unreviewedItems);
                    pausedCount.textContent = faNumber.format(pausedItems);
                    pendingTotalItems.textContent = faNumber.format(allItems);
                    // The global total includes every eligible customer regardless of
                    // the selected status filter, search or current page.
                    const allCustomers = Math.max(0, Number(result.total) || 0);
                    const completedCustomers = Math.max(0, Number(result.summary.completedCustomers) || 0);
                    const unfinishedCustomers = Math.max(0, allCustomers - completedCustomers);
                    unfinishedCustomersCount.textContent = faNumber.format(unfinishedCustomers);
                    pendingTotalCustomers.textContent = faNumber.format(allCustomers);
                    reviewedTotalCustomers.textContent = faNumber.format(allCustomers);
                    const percent = allItems > 0
                        ? Math.min(100, Math.round(100 * checkedItems / allItems))
                        : 0;
                    progressPercent.textContent = faNumber.format(percent) + "٪";
                    progressFill.style.width = percent + "%";
                    progressTrack.setAttribute("aria-valuenow", String(percent));
                    progressTrack.setAttribute("aria-valuetext",
                        faNumber.format(checkedItems) + " از " + faNumber.format(allItems) + " قلم");
                }
                total = Number(result.filtered ?? 0);
                if (summaryOnly) {
                    queueCount.textContent = faNumber.format(total) + " مشتری";
                    renderPager();
                    return;
                }
                if (start >= total && start > 0) {
                    start = Math.max(0, (Math.ceil(total / Number(pageSize.value)) - 1) * Number(pageSize.value));
                    return loadCustomers();
                }
                queue.innerHTML = result.data.map(row => {
                    const pending = row.totalItems - row.reviewedItems;
                    const contactStatus = Number(row.contactStatus) || 0;
                    const noResponseAttempts = Math.max(0, Number(row.noResponseAttempts) || 0);
                    const contactHistoryCount = Math.max(0, Number(row.contactHistoryCount) || 0);
                    const complete = pending === 0;
                    const cooldownSeconds = Math.min(60, Math.max(0, Number(row.noResponseCooldownSeconds) || 0));
                    // Sorting is only applied on explicit list loads, never after a QC save.
                    // Review completion remains independent from contact history.
                    return '<article role="listitem" class="qc-client-card'
                        + (lastCheckedCustomerId === String(row.customerId).toLowerCase()
                            ? ' qc-client-last-checked' : '')
                        + '" data-qc-complete="' + complete
                        + '" data-qc-invalid-number="' + (contactStatus === 2) + '">'
                        + '<div class="qc-client-profile"><div class="qc-client-name-line"><strong>'
                        + escape(row.customerName || "مشتری بدون نام") + '</strong></div>'
                        + '<small>کد مشتری: ' + faCodeNumber.format(row.customerCode) + ' · ' + escape(row.mobile || "بدون شماره") + '</small>'
                        + '</div>'
                        + '<div class="qc-client-stats">'
                        + '<span class="qc-client-segment-group qc-client-order-group" role="group" aria-label="سفارش‌ها و فاکتورهای مشتری">'
                        + '<span class="qc-client-stat-segment"><i class="fa fa-shopping-cart" aria-hidden="true"></i> ' + faNumber.format(row.orders) + ' سفارش</span>'
                        + '<span class="qc-client-stat-segment"><i class="fa fa-file-invoice" aria-hidden="true"></i> ' + faNumber.format(row.invoices) + ' فاکتور</span></span>'
                        + '<span class="qc-client-segment-group qc-client-review-group qc-client-review-' + (complete ? 'complete' : 'pending')
                        + '" role="group" aria-label="آمار و وضعیت بررسی مشتری">'
                        + '<span class="qc-client-count-reviewed">'
                        + reviewSummaryMarkup(row.reviewedItems, row.totalItems, Number(row.discrepancyItems) || 0)
                        + '</span></span>'
                        + '</div>'
                        + '<div class="qc-client-contact-actions">'
                        + '<span class="qc-client-call-group" role="group" aria-label="پیگیری تماس مشتری">'
                        + '<span class="qc-client-no-answer-action">'
                        + '<button type="button" class="qc-client-button qc-client-contact-no-answer'
                        + (contactStatus === 1 ? ' is-selected' : '')
                        + (noResponseAttempts > 0 ? ' has-count' : '')
                        + (noResponseAttempts > 0 || contactStatus === 1 ? ' has-value' : '') + '"'
                        + (complete ? ' disabled' : '')
                        + ' title="عدم پاسخ" aria-label="عدم پاسخ"'
                        + ' data-qc-contact="1" data-qc-contact-customer="' + escape(row.customerId)
                        + '" data-qc-cooldown-until="' + (Date.now() + cooldownSeconds * 1000)
                        + '" data-qc-no-answer-count="' + noResponseAttempts + '">'
                        + '<i class="fa fa-phone-slash" aria-hidden="true"></i>'
                        + contactCountBadge(noResponseAttempts) + '</button>'
                        + '<span class="qc-client-no-answer-progress" data-qc-no-answer-progress hidden aria-hidden="true">'
                        + '<span class="qc-client-no-answer-progress-fill" data-qc-no-answer-progress-fill></span></span></span>'
                        + '<button type="button" class="qc-client-button qc-client-history-open'
                        + (contactHistoryCount > 0 ? ' has-count has-value' : '')
                        + '" data-qc-history="' + escape(row.customerId) + '"'
                        + ' title="تاریخچه تماس؛ ' + faNumber.format(contactHistoryCount) + ' سابقه"'
                        + ' aria-label="مشاهده تاریخچه تماس؛ ' + faNumber.format(contactHistoryCount) + ' سابقه">'
                        + '<i class="fa fa-history" aria-hidden="true"></i>'
                        + contactCountBadge(contactHistoryCount) + '</button>'
                        + '<button type="button" class="qc-client-button qc-client-contact-invalid'
                        + (contactStatus === 2 ? ' is-selected has-value' : '') + '" aria-pressed="' + (contactStatus === 2)
                        + '" title="' + (complete ? 'شماره نامعتبر؛ تمام اقلام بررسی شده‌اند'
                            : contactStatus === 2 ? 'شماره نامعتبر (ثبت شده)' : 'شماره نامعتبر') + '"'
                        + ' aria-label="' + (contactStatus === 2 ? 'شماره نامعتبر (ثبت شده)' : 'شماره نامعتبر') + '"'
                        + (complete ? ' disabled' : '')
                        + ' data-qc-contact="2" data-qc-contact-customer="' + escape(row.customerId)
                        + '"><i class="fa fa-exclamation-triangle" aria-hidden="true"></i></button></span>'
                        + '<button type="button" class="qc-client-button qc-client-notes-button'
                        + (row.hasNotes ? ' has-notes' : '') + '" data-qc-customer-notes="' + escape(row.customerId)
                        + '" title="توضیحات مشتری" aria-label="نمایش و ثبت توضیحات مشتری">'
                        + '<i class="fa fa-sticky-note" aria-hidden="true"></i> توضیحات</button>'
                        + '<button type="button" class="qc-client-button qc-client-primary" data-qc-customer="' + escape(row.customerId) + '">'
                        + '<i class="fa fa-check-circle" aria-hidden="true"></i> '
                        + (pending > 0 ? "کنترل کیفیت" : "مشاهده / ویرایش") + '</button>'
                        + '</div></article>';
                }).join("");
                refreshNoResponseCooldowns();
                queueFeedback.textContent = total ? "" : "مشتری واجد شرایطی با این فیلتر یافت نشد.";
                queueCount.textContent = faNumber.format(total) + " مشتری";
                renderPager();
            } catch (error) {
                if (!isActive() || controller.signal.aborted) return;
                if (summaryOnly) {
                    console.error("QC summary refresh failed:", error);
                } else {
                    queue.querySelectorAll(".qc-client-skeleton").forEach(card => card.remove());
                    queueFeedback.textContent = "دریافت صف کنترل کیفیت انجام نشد. دوباره جستجو کنید.";
                }
            } finally {
                if (listController === controller && !summaryOnly) {
                    loading = false;
                    queue.classList.remove("is-refreshing");
                    queue.removeAttribute("aria-busy");
                    queue.removeAttribute("aria-label");
                }
            }
        }

        function renderHistoryPager() {
            const size = 20;
            const current = Math.floor(historyStart / size);
            const totalPages = Math.ceil(historyTotal / size);
            historyPages.innerHTML = totalPages > 1
                ? '<button type="button" data-qc-history-page="prev"' + (current === 0 ? " disabled" : "") + '>قبلی</button>'
                    + '<span>صفحه ' + faNumber.format(current + 1) + ' از ' + faNumber.format(totalPages) + '</span>'
                    + '<button type="button" data-qc-history-page="next"' + (current + 1 >= totalPages ? " disabled" : "") + '>بعدی</button>'
                : "";
        }

        function renderContactHistory(rows) {
            if (!rows.length) {
                historyEntries.innerHTML = '<div class="qc-client-feedback">سابقه تماسی برای این مشتری یافت نشد.</div>';
                return;
            }
            historyEntries.innerHTML = rows.map(row => {
                const status = Number(row.status);
                const label = status === 1 ? "عدم پاسخ" : status === 2 ? "شماره نامعتبر" : "لغو وضعیت تماس";
                const labelClass = status === 2 ? " is-invalid" : status === 0 ? " is-cancelled" : "";
                const meta = [
                    '<span>تاریخ: <strong>' + escape(row.createdAt) + '</strong></span>',
                    '<span>کارشناس: <strong>' + escape(row.createdBy || "ثبت نشده") + '</strong></span>',
                ];
                if (Number(row.attemptCount) > 0)
                    meta.push('<span>تلاش بی‌پاسخ: <strong>' + faNumber.format(row.attemptCount) + '</strong></span>');
                if (row.mobileAtAttempt)
                    meta.push('<span>شماره تماس: <strong>' + escape(row.mobileAtAttempt) + '</strong></span>');
                if (row.isLegacy) meta.push('<span class="qc-client-history-label">سابقه تجمیعی قدیمی</span>');
                const audit = row.isDeleted
                    ? '<div>حذف‌شده توسط <strong>' + escape(row.deletedBy || "نامشخص") + '</strong>'
                        + (row.deletedAt ? ' در <strong>' + escape(row.deletedAt) + '</strong>' : '')
                        + (row.deleteReason ? ' · دلیل: <strong>' + escape(row.deleteReason) + '</strong>' : '') + '</div>'
                    : (row.restoredAt ? '<div>بازیابی توسط <strong>' + escape(row.restoredBy || "نامشخص")
                        + '</strong> در <strong>' + escape(row.restoredAt) + '</strong>'
                        + (row.deleteReason ? ' · دلیل حذف قبلی: <strong>' + escape(row.deleteReason) + '</strong>' : '')
                        + '</div>' : '');
                const action = !historyCanManage ? "" : row.isDeleted
                    ? '<button type="button" class="qc-client-button qc-client-history-restore"'
                        + ' data-qc-history-action="restore" data-qc-history-id="' + escape(row.id)
                        + '" data-qc-history-version="' + escape(row.rowVersion) + '">'
                        + '<i class="fa fa-undo" aria-hidden="true"></i> بازیابی</button>'
                    : '<button type="button" class="qc-client-button qc-client-history-delete"'
                        + ' data-qc-history-action="delete" data-qc-history-id="' + escape(row.id)
                        + '" data-qc-history-version="' + escape(row.rowVersion)
                        + '" title="برای حذف، نیم‌ثانیه نگه دارید" aria-label="حذف سابقه؛ دکمه را نیم‌ثانیه نگه دارید">'
                        + '<i class="fa fa-trash" aria-hidden="true"></i> حذف</button>';
                return '<article role="listitem" class="qc-client-history-entry' + (row.isDeleted ? ' is-deleted' : '') + '">'
                    + '<div class="qc-client-history-info">'
                    + '<div class="qc-client-history-heading"><span class="qc-client-history-label' + labelClass + '">'
                    + label + '</span>' + (row.isDeleted ? '<span class="qc-client-history-label is-invalid">حذف‌شده</span>' : '') + '</div>'
                    + '<div class="qc-client-history-meta">' + meta.join("") + '</div>'
                    + (row.description ? '<div>' + escape(row.description) + '</div>' : '')
                    + audit + '</div>'
                    + (action ? '<div class="qc-client-history-actions">' + action + '</div>' : '')
                    + '</article>';
            }).join("");
        }

        async function loadContactHistory() {
            cancelHistoryDeleteHold();
            if (!historyCustomerId || !isActive()) return;
            historyController?.abort();
            const controller = historyController = new AbortController();
            historyFeedback.textContent = "در حال دریافت تاریخچه…";
            try {
                const result = await sendApiRequest({
                    url: "/QualityControl/GetContactHistory",
                    params: {
                        customerId: historyCustomerId,
                        includeDeleted: historyShowDeleted.checked,
                        start: historyStart, length: 20,
                    },
                    signal: controller.signal,
                    showLoading: false,
                });
                if (!isActive() || controller.signal.aborted || !result || !Array.isArray(result.data)) return;
                historyTotal = Number(result.total) || 0;
                historyCanManage = Boolean(result.canManage);
                historyDeletedFilter.hidden = !historyCanManage;
                historyCustomer.textContent = result.customerName || "مشتری";
                if (historyStart >= historyTotal && historyStart > 0) {
                    historyStart = Math.max(0, (Math.ceil(historyTotal / 20) - 1) * 20);
                    return loadContactHistory();
                }
                renderContactHistory(result.data);
                renderHistoryPager();
                historyFeedback.textContent = faNumber.format(historyTotal) + " سابقه تماس";
            } catch (error) {
                if (isActive() && !controller.signal.aborted)
                    historyFeedback.textContent = "دریافت تاریخچه تماس ناموفق بود.";
            }
        }

        function openContactHistory(customerId) {
            cancelHistoryDeleteHold();
            if (!customerId || !isActive()) return;
            historyCustomerId = customerId;
            historyStart = 0;
            historyCanManage = false;
            historyShowDeleted.checked = false;
            historyDeletedFilter.hidden = true;
            historyEntries.replaceChildren();
            historyCustomer.textContent = "";
            ModalToggle(historyModal, "show");
            loadContactHistory();
        }

        function cancelHistoryDeleteHold() {
            if (!historyDeleteHold) return;
            clearTimeout(historyDeleteHold.timer);
            historyDeleteHold.button.classList.remove("is-holding");
            historyDeleteHold = null;
        }

        function startHistoryDeleteHold(button, input, inputId) {
            if (historyBusy || !historyCanManage || !historyCustomerId || !isActive()
                || button.disabled || historyDeleteHold) return;
            const hold = { button, input, inputId, timer: null };
            historyDeleteHold = hold;
            button.classList.add("is-holding");
            hold.timer = setTimeout(() => {
                if (historyDeleteHold !== hold || !isActive() || !button.isConnected) return;
                cancelHistoryDeleteHold();
                // Keep the completed outline visible until the server responds.
                button.classList.add("is-hold-complete");
                changeContactHistory(button);
            }, historyDeleteHoldMs);
        }

        async function playHistoryDeleteFade(button) {
            const card = button.closest(".qc-client-history-entry");
            if (!card?.isConnected || !isActive()) return;
            card.classList.add("is-fading");
            if (!window.matchMedia("(prefers-reduced-motion: reduce)").matches)
                await new Promise(resolve => setTimeout(resolve, 550));
            // Keep the faded card hidden until the refreshed history replaces it.
        }

        async function changeContactHistory(button) {
            if (historyBusy || !historyCanManage || !historyCustomerId || !isActive()) return;
            const restore = button.dataset.qcHistoryAction === "restore";
            historyBusy = true;
            historyEntries.querySelectorAll("[data-qc-history-action]").forEach(action => action.disabled = true);
            historyFeedback.textContent = restore ? "در حال بازیابی سابقه…" : "در حال حذف سابقه…";
            try {
                const result = await sendApiRequest({
                    url: "/QualityControl/ChangeContactHistory",
                    method: "POST",
                    headers: { RequestVerificationToken: token.value },
                    data: {
                        customerId: historyCustomerId,
                        historyId: button.dataset.qcHistoryId,
                        rowVersion: button.dataset.qcHistoryVersion,
                        restore,
                    },
                    showLoading: false,
                    successMsg: restore ? "سابقه تماس بازیابی شد" : "سابقه تماس حذف شد",
                });
                if (!result || !result.message) {
                    historyFeedback.textContent = "تغییر سابقه تماس ناموفق بود؛ دوباره تلاش کنید.";
                    return;
                }
                // Fade the whole card only after the server confirms soft-deletion.
                if (!restore) await playHistoryDeleteFade(button);
                if (!isActive()) return;
                await loadContactHistory();
                await loadCustomers();
            } catch (error) {
                if (isActive()) historyFeedback.textContent = "خطا هنگام تغییر سابقه؛ تاریخچه را تازه کنید.";
            } finally {
                historyBusy = false;
                if (button.isConnected) {
                    button.classList.remove("is-hold-complete");
                    button.closest(".qc-client-history-entry")?.classList.remove("is-fading");
                }
                if (isActive()) historyEntries.querySelectorAll("[data-qc-history-action]")
                    .forEach(action => action.disabled = false);
            }
        }


        // Notes are customer-level history; they never mutate invoice-item QC results.
        async function loadCustomerNotes(reset = true) {
            if (!notesCustomerId || !isActive() || customerNotesLoading) return;
            const customerId = notesCustomerId;
            const generation = notesGeneration;
            if (reset) {
                customerNotesStart = 0;
                customerNotesList.replaceChildren();
                customerNoteRows.clear();
            }
            customerNotesController?.abort();
            const controller = customerNotesController = new AbortController();
            customerNotesLoading = true;
            customerNotesMore.disabled = true;
            if (reset) customerNotesList.innerHTML =
                '<span class="qc-client-notes-placeholder">در حال دریافت یادداشت‌ها…</span>';
            try {
                const data = await sendApiRequest({
                    url: "/QualityControl/GetCustomerNotes",
                    params: { customerId, start: customerNotesStart, length: 10 },
                    signal: controller.signal,
                    showLoading: false,
                });
                if (!isActive() || controller.signal.aborted || generation !== notesGeneration
                    || notesCustomerId !== customerId) return;
                if (!data || !Array.isArray(data.data)) {
                    customerNoteFeedback.textContent = "دریافت یادداشت‌ها انجام نشد؛ دوباره تلاش کنید.";
                    return;
                }
                if (reset) customerNotesList.replaceChildren();
                const markup = data.data.map(note => {
                    customerNoteRows.set(note.id, note);
                    const source = Number(note.source);
                    const provenance = source === 2 ? " · سابقه قدیمی"
                        : source === 1 ? " · ثبت از تماس AI" : "";
                    return '<article class="qc-client-notes-entry" role="listitem" data-qc-note-entry="' + escape(note.id) + '">'
                        + '<div class="qc-client-notes-entry-top"><div class="qc-client-notes-entry-meta">'
                        + '<strong>' + escape(note.createdBy || "کارشناس") + '</strong>'
                        + '<span>' + escape(note.createdAt || "") + '</span>'
                        + (provenance ? '<span>' + provenance + '</span>' : "")
                        + '</div><div class="qc-client-notes-entry-actions">'
                        + '<button type="button" class="qc-client-button qc-note-edit" data-qc-note-action="edit"'
                        + ' data-qc-note-id="' + escape(note.id) + '" title="ویرایش یادداشت" aria-label="ویرایش یادداشت">'
                        + '<i class="fa fa-pencil-alt" aria-hidden="true"></i></button>'
                        + '<button type="button" class="qc-client-button qc-note-delete" data-qc-note-action="delete"'
                        + ' data-qc-note-id="' + escape(note.id) + '" title="حذف یادداشت" aria-label="حذف یادداشت">'
                        + '<i class="fa fa-trash-alt" aria-hidden="true"></i></button>'
                        + '</div></div><p>' + escape(note.description || "") + '</p></article>';
                }).join("");
                customerNotesList.insertAdjacentHTML("beforeend", markup);
                customerNotesStart += data.data.length;
                customerNotesTotal = Math.max(0, Number(data.total) || 0);
                customerNotesCount.textContent = faNumber.format(customerNotesTotal) + " یادداشت";
                if (!customerNotesTotal)
                    customerNotesList.innerHTML =
                        '<span class="qc-client-notes-placeholder">هنوز یادداشتی برای این مشتری ثبت نشده است.</span>';
                customerNotesMore.hidden = customerNotesStart >= customerNotesTotal;
                const noteButton = [...queue.querySelectorAll("[data-qc-customer-notes]")]
                    .find(button => button.dataset.qcCustomerNotes === customerId);
                noteButton?.classList.toggle("has-notes", customerNotesTotal > 0);
                customerNoteFeedback.textContent = "";
            } catch (error) {
                if (!controller.signal.aborted && isActive())
                    customerNoteFeedback.textContent = "خطا در دریافت یادداشت‌ها؛ مجدداً تلاش کنید.";
            } finally {
                if (customerNotesController === controller) {
                    customerNotesLoading = false;
                    customerNotesMore.disabled = false;
                }
            }
        }

        function resetCustomerNoteEditor() {
            editingCustomerNote = null;
            customerNoteText.value = "";
            customerNoteLabel.textContent = "یادداشت جدید";
            customerNoteSaveLabel.textContent = "ثبت یادداشت";
            customerNoteCancelEdit.hidden = true;
            customerNotesList.querySelectorAll(".is-editing").forEach(entry => entry.classList.remove("is-editing"));
            customerNoteText.dispatchEvent(new Event("input"));
        }

        function hasUnsubmittedCustomerNote() {
            const text = customerNoteText.value.trim();
            return Boolean(text) && (!editingCustomerNote || text !== editingCustomerNote.description.trim());
        }

        customerNoteCancelEdit.addEventListener("click", () => {
            if (!customerNoteSaving && !customerNoteDeleting) {
                resetCustomerNoteEditor();
                customerNoteFeedback.textContent = "";
            }
        });
        customerNotesList.addEventListener("click", async event => {
            const button = event.target.closest("[data-qc-note-action]");
            if (!button || customerNoteSaving || customerNoteDeleting || !isActive() || !notesCustomerId) return;
            const note = customerNoteRows.get(button.dataset.qcNoteId);
            if (!note) {
                customerNoteFeedback.textContent = "یادداشت تغییر کرده است؛ فهرست را دوباره دریافت کنید.";
                return;
            }
            if (hasUnsubmittedCustomerNote()) {
                customerNoteFeedback.textContent = "ابتدا متن فعلی را ثبت کنید یا آن را پاک کنید.";
                return;
            }
            if (button.dataset.qcNoteAction === "edit") {
                editingCustomerNote = note;
                customerNoteText.value = note.description;
                customerNoteLabel.textContent = "ویرایش یادداشت";
                customerNoteSaveLabel.textContent = "ذخیره ویرایش";
                customerNoteCancelEdit.hidden = false;
                customerNoteText.dispatchEvent(new Event("input"));
                customerNotesList.querySelectorAll(".is-editing").forEach(entry => entry.classList.remove("is-editing"));
                button.closest(".qc-client-notes-entry")?.classList.add("is-editing");
                customerNoteText.focus();
                customerNoteText.scrollIntoView({ block: "nearest", behavior: "auto" });
                return;
            }
            if (button.dataset.qcNoteAction !== "delete") return;

            const customerId = notesCustomerId, generation = notesGeneration;
            customerNoteDeleting = true;
            button.disabled = true;
            try {
                const confirmed = await confirmAlert({
                    title: "حذف یادداشت",
                    text: "این یادداشت از فهرست حذف شود؟",
                    confirmText: "حذف یادداشت",
                });
                if (!confirmed || !isActive() || generation !== notesGeneration || notesCustomerId !== customerId) return;
                const result = await sendApiRequest({
                    url: "/QualityControl/DeleteCustomerNote", method: "POST",
                    headers: { RequestVerificationToken: notesToken.value },
                    data: { customerId, noteId: note.id, rowVersion: note.rowVersion },
                    showLoading: false,
                });
                if (!isActive() || generation !== notesGeneration || notesCustomerId !== customerId) return;
                if (!result?.message) {
                    customerNoteFeedback.textContent = "حذف یادداشت انجام نشد؛ فهرست را تازه کنید.";
                    return;
                }
                resetCustomerNoteEditor();
                customerNotesController?.abort();
                customerNotesLoading = false;
                await loadCustomerNotes(true);
                customerNoteFeedback.textContent = "یادداشت حذف شد.";
            } finally {
                customerNoteDeleting = false;
                if (button.isConnected) button.disabled = false;
            }
        });

        customerNoteText.addEventListener("input", () => {
            const filled = Boolean(customerNoteText.value.trim());
            customerNoteSave.disabled = customerNoteSaving || customerNoteDeleting || !filled || !notesCustomerId;
            customerNoteClear.disabled = customerNoteSaving || customerNoteDeleting || !filled;
            customerNoteFeedback.textContent = "";
        });
        customerNoteClear.addEventListener("click", () => {
            if (customerNoteSaving) return;
            customerNoteText.value = "";
            customerNoteText.dispatchEvent(new Event("input"));
        });
        customerNotesMore.addEventListener("click", () => {
            if (!customerNotesLoading) void loadCustomerNotes(false);
        });
        customerNoteSave.addEventListener("click", async () => {
            if (!notesCustomerId || customerNoteSaving || customerNoteDeleting || !isActive()) return;
            const description = customerNoteText.value.trim();
            if (!description || description.length > 4000) {
                customerNoteFeedback.textContent = "متن یادداشت باید بین ۱ تا ۴۰۰۰ کاراکتر باشد.";
                return;
            }
            const customerId = notesCustomerId;
            const generation = notesGeneration;
            const editedNote = editingCustomerNote;
            customerNoteSaving = true;
            customerNoteSave.disabled = true;
            customerNoteClear.disabled = true;
            customerNoteText.disabled = true;
            try {
                const result = await sendApiRequest({
                    url: editedNote ? "/QualityControl/UpdateCustomerNote" : "/QualityControl/AddCustomerNote",
                    method: "POST",
                    headers: { RequestVerificationToken: notesToken.value },
                    data: editedNote
                        ? { customerId, noteId: editedNote.id, rowVersion: editedNote.rowVersion, description }
                        : { customerId, description },
                    showLoading: false,
                });
                if (!isActive() || generation !== notesGeneration || notesCustomerId !== customerId) return;
                if (!result?.message) {
                    customerNoteFeedback.textContent = editedNote
                        ? "ویرایش یادداشت انجام نشد؛ فهرست را تازه کنید."
                        : "ثبت یادداشت انجام نشد؛ دوباره تلاش کنید.";
                    return;
                }
                resetCustomerNoteEditor();
                customerNotesController?.abort();
                customerNotesLoading = false;
                await loadCustomerNotes(true);
                customerNoteFeedback.textContent = editedNote ? "یادداشت ویرایش شد." : "یادداشت ثبت شد.";
            } catch (error) {
                if (isActive() && generation === notesGeneration)
                    customerNoteFeedback.textContent = "ثبت یادداشت ناموفق بود؛ متن حفظ شده است.";
            } finally {
                customerNoteSaving = false;
                if (generation === notesGeneration && isActive()) {
                    customerNoteText.disabled = false;
                    const filled = Boolean(customerNoteText.value.trim());
                    customerNoteSave.disabled = !filled || !notesCustomerId;
                    customerNoteClear.disabled = !filled;
                }
            }
        });

        function openCustomerNotes(customerId, customerName) {
            if (!customerId || !isActive() || customerNoteSaving || customerNoteDeleting) return;
            customerNotesController?.abort();
            notesCustomerId = customerId;
            notesGeneration++;
            customerNotesLoading = false;
            customerNotesStart = 0;
            customerNotesTotal = 0;
            customerNotesList.innerHTML = '<span class="qc-client-notes-placeholder">در حال دریافت یادداشت‌ها…</span>';
            customerNotesCount.textContent = "";
            customerNotesMore.hidden = true;
            resetCustomerNoteEditor();
            customerNoteText.disabled = false;
            customerNoteFeedback.textContent = "";
            notesSubtitle.textContent = customerName || "";
            ModalToggle(notesModal, "show");
            void loadCustomerNotes(true);
        }

        notesModal.addEventListener("hide.bs.modal", event => {
            if (customerNoteSaving || customerNoteDeleting || hasUnsubmittedCustomerNote()) {
                event.preventDefault();
                customerNoteFeedback.textContent = customerNoteSaving || customerNoteDeleting
                    ? "تغییر یادداشت در حال انجام است."
                    : "ابتدا متن را ثبت کنید یا ویرایش را لغو کنید.";
                return;
            }
            customerNotesController?.abort();
            notesGeneration++;
        });
        notesModal.addEventListener("hidden.bs.modal", () => {
            notesCustomerId = null;
            customerNotesLoading = false;
            customerNoteRows.clear();
            resetCustomerNoteEditor();
            customerNotesList.replaceChildren();
            customerNoteFeedback.textContent = "";
        });

        async function openCustomer(customerId) {
            if (hasUnsavedWork() || !isActive()) return;
            formController?.abort();
            const controller = formController = new AbortController();
            const generation = ++formGeneration;
            noteTimers.forEach(timer => clearTimeout(timer));
            noteTimers.clear();
            drafts.clear();
            review = null;
            reviewHasSavedChanges = false;
            issueItemId = null;
            issueReturnFocus = null;
            issueOverlay.hidden = true;
            itemStatusFilter = "0";
            itemSortField = "order";
            itemSortDirection = -1;
            itemPageStart = 0;
            itemPageSize.value = "5";
            itemPageLoading = false;
            clearTimeout(itemSearchTimer);
            itemPaging.hidden = true;
            saveAllButton.disabled = true;
            reviewToolbar.hidden = true;
            summary.replaceChildren();
            content.replaceChildren();
            itemSearch.value = "";
            itemSearchBar.hidden = true;
            itemSearchEmpty.hidden = true;
            itemSearchCount.textContent = "";
            page.querySelector("#qcCustomerModalTitle").textContent = "کنترل کیفیت مشتری";
            page.querySelector("#qcCustomerModalSubtitle").textContent = "";
            formFeedback.textContent = "در حال دریافت فاکتورها و اقلام سفارش‌های تحویل‌شده…";
            ModalToggle(modal, "show");
            const result = await sendApiRequest({
                url: "/QualityControl/GetCustomerForm", params: reviewParams(customerId),
                signal: controller.signal, showLoading: false,
            });
            if (!isActive() || controller.signal.aborted || generation !== formGeneration) return;
            if (!result?.customerId) {
                formFeedback.textContent = "دریافت فاکتورهای مشتری ناموفق بود. پنجره را ببندید و دوباره تلاش کنید.";
                return;
            }
            applyReviewPage(result);
            page.querySelector("#qcCustomerModalTitle").textContent = "کنترل کیفیت: " + (review.customerName || "مشتری");
            const customerDetails = [
                '<span class="qc-client-customer-meta">کد مشتری: <strong>'
                + escape(faCodeNumber.format(review.customerCode)) + '</strong></span>',
            ];
            if (review.agentName?.trim() && review.agentName.trim() !== review.customerName?.trim())
                customerDetails.push('<span class="qc-client-customer-meta">نماینده: <strong>'
                    + escape(review.agentName.trim()) + '</strong></span>');
            if (review.mobile?.trim())
                customerDetails.push('<span class="qc-client-customer-meta"><i class="fa fa-phone" aria-hidden="true"></i> موبایل: <strong dir="ltr">'
                    + escape(review.mobile.trim()) + '</strong></span>');
            page.querySelector("#qcCustomerModalSubtitle").innerHTML = customerDetails.join("");
            formFeedback.textContent = "";
            reviewToolbar.hidden = false;
            itemSearchBar.hidden = false;
            renderReview();
        }

        function questionMarkup(item, draft) {
            return '<div class="qc-client-questions"><h4>سؤال‌های کنترل کیفیت این قلم</h4>'
                + '<div class="qc-client-questions-grid">'
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
                + '</div>'
                + '<label class="qc-client-note-label" for="qc-note-' + escape(item.factorItemId) + '">'
                + 'شرح مغایرت <small>(اختیاری)</small></label>'
                + '<textarea id="qc-note-' + escape(item.factorItemId)
                + '" data-qc-note="' + escape(item.factorItemId)
                + '" maxlength="4000" class="form-control" rows="2"'
                + ' placeholder="توضیحات مشتری درباره این قلم...">' + escape(draft.description) + '</textarea></div>';
        }

        function reviewRows() {
            return review.data.map(row => ({
                item: row.item,
                order: { serialNumber: row.orderSerialNumber },
                invoice: { serialNumber: row.invoiceSerialNumber, date: row.invoiceDate },
            }));
        }

        function renderReviewRow(entry, index) {
            const { item, order, invoice } = entry;
            const draft = stateOf(item);
            const statusClass = draft.status === 2 ? "discrepancy"
                : draft.status === 1 ? "complete" : "pending";
            const product = item.productSmallName || item.product || "قلم بدون عنوان";
            const description = item.product && item.product !== product ? item.product : "";
            const searchText = [
                order.serialNumber, invoice.serialNumber, product, description,
                item.weightCount, item.unitTxt, item.supplierNames, item.supplyExpert,
            ].join(" ");
            return '<tr class="qc-client-review-row qc-client-row-' + statusClass
                + '" data-qc-item="' + escape(item.factorItemId)
                + '" data-qc-search="' + escape(normalizeSearch(searchText)) + '">'
                + '<td class="qc-client-review-index">' + faNumber.format(review.start + index + 1) + '</td>'
                + '<td class="qc-client-review-code">' + escape(order.serialNumber || "—") + '</td>'
                + '<td class="qc-client-review-code">' + escape(invoice.serialNumber || "—")
                + (invoice.date ? '<small>' + escape(invoice.date) + '</small>' : "") + '</td>'
                + '<td class="qc-client-review-product"><strong>' + escape(product) + '</strong>'
                + (description ? '<small title="' + escape(description) + '">' + escape(description) + '</small>' : "")
                + '</td>'
                + '<td class="qc-client-review-quantity">' + escape(item.weightCount || "—")
                + ' ' + escape(item.unitTxt || "") + '</td>'
                + '<td>' + escape(item.supplierNames?.trim() || "ثبت نشده") + '</td>'
                + '<td>' + escape(item.supplyExpert?.trim() || "ثبت نشده") + '</td>'
                + '<td class="qc-client-review-state"><span class="qc-client-review-status qc-client-'
                + statusClass + '">' + (draft.status === 2 ? "دارای مغایرت"
                    : draft.status === 1 ? "بدون مغایرت" : "بررسی‌نشده")
                + '</span><button type="button" class="qc-client-button qc-client-retry" data-qc-retry="'
                + escape(item.factorItemId) + '"' + (draft.failed ? "" : " hidden")
                + '>تلاش مجدد</button></td>'
                + '<td class="qc-client-review-actions">'
                + '<button type="button" class="qc-client-button '
                + (draft.saved && !draft.failed && draft.status === 1 ? "qc-client-ok-selected" : "qc-client-secondary")
                + '" data-qc-set-status="1" data-qc-item-id="' + escape(item.factorItemId)
                + '" aria-pressed="' + (draft.saved && !draft.failed && draft.status === 1) + '"><i class="fa fa-check-circle" aria-hidden="true"></i> بدون مغایرت</button>'
                + '<button type="button" class="qc-client-button '
                + (draft.saved && !draft.failed && draft.status === 2 ? "qc-client-issue-selected" : "qc-client-secondary")
                + '" data-qc-set-status="2" data-qc-item-id="' + escape(item.factorItemId)
                + '" aria-pressed="' + (draft.saved && !draft.failed && draft.status === 2) + '"><i class="fa fa-exclamation-triangle" aria-hidden="true"></i> دارای مغایرت</button>'
                + '</td></tr>';
        }

        function renderReview() {
            if (!review) return;
            const previousTableViewport = content.querySelector(".qc-client-review-table-wrap");
            const scrollPosition = {
                top: previousTableViewport?.scrollTop ?? 0,
                left: previousTableViewport?.scrollLeft ?? 0,
            };
            const entries = reviewRows();
            const sortHeading = (field, label) =>
                '<button type="button" class="qc-client-table-sort" data-qc-item-sort="'
                + field + '" aria-label="مرتب‌سازی بر اساس ' + label + '">'
                + label + ' <i class="fa '
                + (itemSortField === field
                    ? (itemSortDirection < 0 ? 'fa-sort-down' : 'fa-sort-up') : 'fa-sort')
                + '" aria-hidden="true"></i></button>';
            content.innerHTML = '<div class="qc-client-review-table-wrap" role="region"'
                + ' aria-label="جدول اقلام کنترل کیفیت" tabindex="0">'
                + '<table class="qc-client-review-table"><thead><tr>'
                + '<th scope="col">' + sortHeading("row", "ردیف")
                + '</th><th scope="col">' + sortHeading("order", "شماره سفارش")
                + '</th><th scope="col">' + sortHeading("invoice", "شماره فاکتور")
                + '</th><th scope="col">' + sortHeading("product", "نام کالا / مشخصات")
                + '</th><th scope="col">' + sortHeading("quantity", "مقدار")
                + '</th><th scope="col">' + sortHeading("supplier", "تأمین‌کننده")
                + '</th><th scope="col">' + sortHeading("expert", "کارشناس تأمین")
                + '</th><th scope="col">' + sortHeading("status", "وضعیت کنترل کیفیت")
                + '<th scope="col">عملیات</th></tr></thead><tbody>'
                + entries.map((entry, index) => renderReviewRow(entry, index)).join("")
                + '</tbody></table></div>';
            // Keep the empty-state notice within the table viewport, beneath the sticky header.
            // Reattach the same node after each render so its aria-live/hidden state is preserved.
            content.querySelector(".qc-client-review-table-wrap")?.appendChild(itemSearchEmpty);
            refreshReviewState();
            const tableViewport = content.querySelector(".qc-client-review-table-wrap");
            if (tableViewport) {
                tableViewport.scrollTop = scrollPosition.top;
                tableViewport.scrollLeft = scrollPosition.left;
            }
        }

        function refreshReviewState() {
            if (!review) { saveAllButton.disabled = true; return; }
            const items = flatten();
            const reviewed = review.reviewedItems;
            const pending = review.pendingItems;
            const withoutIssues = review.noDiscrepancyItems;
            const issues = review.discrepancyItems;
            const progress = review.totalItems ? Math.round(100 * reviewed / review.totalItems) : 0;
            const pendingOnPage = items.filter(item => stateOf(item)?.status === 0).length;
            summary.innerHTML = '<div class="qc-client-review-overview">'
                + '<span class="qc-client-review-overview-icon" aria-hidden="true"><i class="fa fa-box-open"></i></span>'
                + '<div class="qc-client-review-overview-text"><strong>'
                + faNumber.format(review.totalItems) + ' قلم ثبت‌شده</strong><small>'
                + faNumber.format(review.ordersCount) + ' سفارش · '
                + faNumber.format(review.invoicesCount)
                + ' فاکتور</small></div></div>'
                + '<div class="qc-client-summary-tags">'
                + '<span>' + faNumber.format(reviewed) + ' بررسی‌شده</span>'
                + '<span class="qc-client-summary-ok">' + faNumber.format(withoutIssues) + ' بدون مغایرت</span>'
                + '<span class="qc-client-summary-issues">' + faNumber.format(issues) + ' دارای مغایرت</span></div>'
                + '<div class="qc-client-review-progress"><span>پیشرفت بررسی اقلام</span>'
                + '<div class="qc-client-review-progress-track" role="progressbar"'
                + ' aria-label="پیشرفت بررسی اقلام" aria-valuemin="0" aria-valuemax="100" aria-valuenow="'
                + progress + '"><span style="width:' + progress + '%"></span></div>'
                + '<strong>' + faNumber.format(progress) + '٪</strong></div>';
            const counts = { all: review.totalItems, 0: pending, 1: withoutIssues, 2: issues };
            itemStatusFilters.querySelectorAll("[data-qc-filter-count]").forEach(element => {
                element.textContent = faNumber.format(counts[element.dataset.qcFilterCount] ?? 0);
            });
            const busy = hasUnsavedWork();
            saveAllButton.disabled = busy || itemPageLoading || pendingOnPage === 0 || Boolean(issueItemId);
            saveHint.textContent = busy ? "در حال ثبت خودکار اطلاعات…"
                : pendingOnPage ? "ثبت گروهی فقط اقلام بررسی‌نشده صفحه فعلی را ثبت می‌کند."
                    : "در این صفحه قلم بررسی‌نشده‌ای وجود ندارد.";
            content.querySelectorAll("[data-qc-item]").forEach(row => {
                const id = row.dataset.qcItem, draft = drafts.get(id);
                if (!draft) return;
                const state = draft.status === 2 ? "discrepancy" : draft.status === 1 ? "complete" : "pending";
                row.classList.remove("qc-client-row-discrepancy", "qc-client-row-complete", "qc-client-row-pending");
                row.classList.add("qc-client-row-" + state);
                const badge = row.querySelector(".qc-client-review-status");
                if (badge) {
                    badge.className = "qc-client-review-status qc-client-" + state;
                    badge.textContent = (draft.status === 2 ? "دارای مغایرت" : draft.status === 1 ? "بدون مغایرت" : "بررسی‌نشده")
                        + (savingItems.has(id) ? " · در حال ثبت"
                            : draft.failed ? " · خطای ثبت" : draft.saved ? " · ثبت‌شده" : "");
                }
                row.querySelectorAll("[data-qc-set-status]").forEach(button => {
                    const selected = draft.saved && !draft.failed && !savingItems.has(id)
                        && Number(button.dataset.qcSetStatus) === draft.status;
                    button.classList.toggle("qc-client-ok-selected", selected && draft.status === 1);
                    button.classList.toggle("qc-client-issue-selected", selected && draft.status === 2);
                    button.classList.toggle("qc-client-secondary", !selected);
                    button.setAttribute("aria-pressed", String(selected));
                    button.disabled = busy || itemPageLoading || Boolean(issueItemId);
                });
                const retry = row.querySelector("[data-qc-retry]");
                if (retry) {
                    retry.hidden = !draft.failed;
                    retry.disabled = busy || itemPageLoading || Boolean(issueItemId);
                }
            });
            modal.querySelectorAll("[data-bs-dismiss]").forEach(button =>
                button.disabled = busy || Boolean(issueItemId));
            filterInvoiceItems();
            updateReviewPager();
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
                    if (succeeded) {
                        reviewHasSavedChanges = true;
                        await loadReviewPage();
                    }
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
                reviewHasSavedChanges = true;
            } catch (error) {
                if (generation === formGeneration && isActive())
                    formFeedback.textContent = "خطا در ارتباط با سرور هنگام ثبت گروهی.";
            } finally {
                saving = false;
                if (generation === formGeneration && isActive()) {
                    refreshReviewState();
                    if (reviewHasSavedChanges) await loadReviewPage();
                }
            }
        }

        form.addEventListener("submit", event => event.preventDefault());
        itemSearch.addEventListener("input", () => {
            clearTimeout(itemSearchTimer);
            itemSearchTimer = setTimeout(() => { void loadReviewPage(true); }, 300);
        });
        itemPageSize.addEventListener("change", () => { void loadReviewPage(true); });
        itemPages.addEventListener("click", event => {
            const button = event.target.closest("[data-qc-item-page]");
            if (!button || button.disabled || itemPageLoading || hasUnsavedWork()) return;
            itemPageStart = (Number(button.dataset.qcItemPage) - 1) * Number(itemPageSize.value);
            void loadReviewPage();
        });
        page.querySelector("#qcCustomerSearchForm").addEventListener("submit", event => {
            event.preventDefault();
            clearReviewFocus();
            search = searchInput.value.trim();
            loadCustomers(true);
        });
        searchInput.addEventListener("input", () => {
            if (!searchInput.value && search) { clearReviewFocus(); search = ""; loadCustomers(true); }
        });
        statusFilter.addEventListener("change", () => { clearReviewFocus(); loadCustomers(true); });
        sortControl.addEventListener("change", () => { clearReviewFocus(); loadCustomers(true); });
        pageSize.addEventListener("change", () => { clearReviewFocus(); loadCustomers(true); });
        page.querySelector("#qcCustomerClearFilters").addEventListener("click", () => {
            clearReviewFocus();
            search = "";
            searchInput.value = "";
            statusFilter.value = "";
            sortControl.value = "pending";
            pageSize.value = "5";
            loadCustomers(true);
        });
        page.querySelector(".qc-client-overview").addEventListener("click", event => {
            const button = event.target.closest("[data-qc-card-status]");
            if (!button) return;
            // The overview totals are global; reset search to show every customer in the selected group.
            clearReviewFocus();
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
            const notesButton = event.target.closest("[data-qc-customer-notes]");
            if (notesButton) {
                if (!loading && !contactBusy && !hasUnsavedWork()) {
                    const customerName = notesButton.closest(".qc-client-card")
                        ?.querySelector(".qc-client-name-line strong")?.textContent;
                    openCustomerNotes(notesButton.dataset.qcCustomerNotes, customerName);
                }
                return;
            }
            const historyButton = event.target.closest("[data-qc-history]");
            if (historyButton) {
                if (!loading && !contactBusy) openContactHistory(historyButton.dataset.qcHistory);
                return;
            }
            const contactButton = event.target.closest("[data-qc-contact]");
            if (contactButton) {
                if (loading || contactBusy || !isActive() || contactButton.disabled) return;
                contactBusy = true;
                const originalIcon = contactButton.innerHTML;
                const originalTitle = contactButton.title;
                const originalLabel = contactButton.getAttribute("aria-label");
                queue.querySelectorAll("[data-qc-contact]").forEach(button => button.disabled = true);
                queueFeedback.textContent = "";
                contactButton.classList.add("is-saving");
                contactButton.setAttribute("aria-busy", "true");
                contactButton.title = "در حال ثبت وضعیت تماس";
                contactButton.setAttribute("aria-label", "در حال ثبت وضعیت تماس");
                contactButton.innerHTML = '<i class="fa fa-spinner fa-spin" aria-hidden="true"></i>';
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
                    if (isActive()) {
                        if (Number(error?.response?.status) === 429) {
                            await loadCustomers();
                            queueFeedback.textContent = error.response.data?.message
                                || "برای ثبت دوباره عدم پاسخ، یک دقیقه فاصله لازم است.";
                        } else queueFeedback.textContent = "خطا در ثبت وضعیت تماس؛ دوباره تلاش کنید.";
                    }
                } finally {
                    contactBusy = false;
                    contactButton.classList.remove("is-saving");
                    contactButton.removeAttribute("aria-busy");
                    contactButton.innerHTML = originalIcon;
                    contactButton.title = originalTitle;
                    if (originalLabel === null) contactButton.removeAttribute("aria-label");
                    else contactButton.setAttribute("aria-label", originalLabel);
                    if (isActive()) {
                        queue.querySelectorAll('[data-qc-contact="2"]').forEach(button => {
                            button.disabled = button.closest("[data-qc-complete]")
                                ?.dataset.qcComplete === "true";
                        });
                        refreshNoResponseCooldowns();
                    }
                }
                return;
            }
            const button = event.target.closest("[data-qc-customer]");
            if (!button || loading || contactBusy || !isActive()) return;

            const customerId = button.dataset.qcCustomer;
            const card = button.closest(".qc-client-card");
            if (card?.dataset.qcInvalidNumber !== "true") {
                void openCustomer(customerId);
                return;
            }

            contactBusy = true;
            try {
                const confirmed = await confirmAlert({
                    title: "شماره تماس نیاز به اصلاح دارد",
                    text: "شماره این مشتری قبلاً نامعتبر ثبت شده است. آیا اصلاح شماره انجام شده است؟",
                    icon: "warning",
                    confirmText: "بله، اصلاح شده است",
                    cancelText: "خیر، هنوز اصلاح نشده",
                    confirmButtonColor: "#00695c",
                });
                if (!confirmed || !isActive()) return;

                queueFeedback.textContent = "";
                const result = await sendApiRequest({
                    url: "/QualityControl/SetCustomerContactStatus",
                    method: "POST",
                    headers: { RequestVerificationToken: token.value },
                    data: { customerId, status: 2, confirmCorrectedNumber: true },
                    showLoading: false,
                });
                if (!isActive()) return;
                if (Number(result?.status) !== 0) {
                    queueFeedback.textContent = "تأیید اصلاح شماره ثبت نشد؛ فهرست را تازه کنید.";
                    await loadCustomers();
                    return;
                }

                // Update the card immediately before refreshing the server-backed queue.
                card.dataset.qcInvalidNumber = "false";
                const invalidButton = card.querySelector('[data-qc-contact="2"]');
                if (invalidButton) {
                    invalidButton.classList.remove("is-selected", "has-value");
                    invalidButton.setAttribute("aria-pressed", "false");
                    invalidButton.title = "شماره نامعتبر";
                    invalidButton.setAttribute("aria-label", "شماره نامعتبر");
                }
                await loadCustomers();
                if (isActive()) await openCustomer(customerId);
            } catch (error) {
                if (isActive()) {
                    queueFeedback.textContent = "تأیید اصلاح شماره انجام نشد؛ دوباره تلاش کنید.";
                    await loadCustomers();
                }
            } finally {
                contactBusy = false;
                if (isActive()) refreshNoResponseCooldowns();
            }
        });
        historyShowDeleted.addEventListener("change", () => {
            historyStart = 0;
            loadContactHistory();
        });
        historyPages.addEventListener("click", event => {
            const button = event.target.closest("[data-qc-history-page]");
            if (!button || button.disabled || historyBusy) return;
            historyStart = Math.max(0, historyStart
                + (button.dataset.qcHistoryPage === "next" ? 20 : -20));
            loadContactHistory();
        });
        // Delete requires a continuous half-second hold, not a click or a confirmation modal.
        historyEntries.addEventListener("pointerdown", event => {
            const button = event.target.closest('[data-qc-history-action="delete"]');
            if (!button || event.button !== 0 || !event.isPrimary) return;
            event.preventDefault();
            button.focus({ preventScroll: true });
            startHistoryDeleteHold(button, "pointer", event.pointerId);
        });
        window.addEventListener("pointerup", event => {
            if (historyDeleteHold?.input === "pointer"
                && historyDeleteHold.inputId === event.pointerId) cancelHistoryDeleteHold();
        }, pageSignal ? { signal: pageSignal } : undefined);
        window.addEventListener("pointercancel", event => {
            if (historyDeleteHold?.input === "pointer"
                && historyDeleteHold.inputId === event.pointerId) cancelHistoryDeleteHold();
        }, pageSignal ? { signal: pageSignal } : undefined);
        window.addEventListener("pointermove", event => {
            const hold = historyDeleteHold;
            if (hold?.input !== "pointer" || hold.inputId !== event.pointerId) return;
            const rect = hold.button.getBoundingClientRect();
            if (event.clientX < rect.left || event.clientX > rect.right
                || event.clientY < rect.top || event.clientY > rect.bottom)
                cancelHistoryDeleteHold();
        }, pageSignal ? { signal: pageSignal } : undefined);
        historyEntries.addEventListener("pointerout", event => {
            const hold = historyDeleteHold;
            if (hold?.input === "pointer" && hold.inputId === event.pointerId
                && event.target === hold.button && !hold.button.contains(event.relatedTarget))
                cancelHistoryDeleteHold();
        });
        historyEntries.addEventListener("keydown", event => {
            const button = event.target.closest('[data-qc-history-action="delete"]');
            if (!button || !["Enter", " "].includes(event.key)) return;
            event.preventDefault();
            if (!event.repeat) startHistoryDeleteHold(button, "keyboard", event.key);
        });
        historyEntries.addEventListener("keyup", event => {
            const button = event.target.closest('[data-qc-history-action="delete"]');
            if (!button || !["Enter", " "].includes(event.key)) return;
            event.preventDefault();
            if (historyDeleteHold?.input === "keyboard"
                && historyDeleteHold.inputId === event.key) cancelHistoryDeleteHold();
        });
        historyEntries.addEventListener("focusout", event => {
            if (historyDeleteHold?.input === "keyboard"
                && historyDeleteHold.button === event.target) cancelHistoryDeleteHold();
        });
        historyEntries.addEventListener("click", event => {
            const button = event.target.closest("[data-qc-history-action]");
            if (!button || button.disabled) return;
            if (button.dataset.qcHistoryAction === "delete") {
                event.preventDefault();
                if (!historyBusy) historyFeedback.textContent = "برای حذف، دکمه را نیم‌ثانیه نگه دارید.";
                return;
            }
            changeContactHistory(button);
        });
        historyEntries.addEventListener("contextmenu", event => {
            if (event.target.closest('[data-qc-history-action="delete"]')) event.preventDefault();
        });
        historyModal.querySelector(".modal-body")?.addEventListener("scroll", cancelHistoryDeleteHold);
        historyModal.addEventListener("hide.bs.modal", cancelHistoryDeleteHold);
        window.addEventListener("blur", cancelHistoryDeleteHold,
            pageSignal ? { signal: pageSignal } : undefined);
        document.addEventListener("visibilitychange", () => {
            if (document.hidden) cancelHistoryDeleteHold();
        }, pageSignal ? { signal: pageSignal } : undefined);
        historyModal.addEventListener("hidden.bs.modal", () => {
            cancelHistoryDeleteHold();
            historyController?.abort();
            historyCustomerId = null;
            historyEntries.replaceChildren();
            historyFeedback.textContent = "";
        });
        itemStatusFilters.addEventListener("click", event => {
            const button = event.target.closest("[data-qc-item-filter]");
            if (!button || !review) return;
            if (hasUnsavedWork() || itemPageLoading) return;
            itemStatusFilter = button.dataset.qcItemFilter;
            void loadReviewPage(true);
        });

        function closeIssueDialog() {
            if (!issueItemId) return;
            issueOverlay.hidden = true;
            issueItemId = null;
            issueFeedback.textContent = "";
            issueQuestions.replaceChildren();
            const returnFocus = issueReturnFocus;
            issueReturnFocus = null;
            if (returnFocus?.isConnected) returnFocus.focus({ preventScroll: true });
            refreshReviewState();
        }

        function openIssueDialog(id, button) {
            if (!review || !isActive() || hasUnsavedWork() || issueItemId) return;
            const item = flatten().find(row => row.factorItemId === id);
            const draft = item && stateOf(item);
            if (!draft || !review.questions?.length) {
                formFeedback.textContent = "سؤال‌های کنترل کیفیت این قلم در دسترس نیست.";
                return;
            }
            issueItemId = id;
            issueReturnFocus = button;
            issueTitle.textContent = draft.status === 2 ? "ویرایش مغایرت قلم" : "ثبت مغایرت قلم";
            issueSubtitle.textContent = item.productSmallName || item.product || "قلم بدون عنوان";
            issueQuestions.innerHTML = questionMarkup(item, draft);
            issueFeedback.textContent = "";
            issueOverlay.hidden = false;
            refreshReviewState();
            issueOverlay.querySelector('input[type="radio"]')?.focus({ preventScroll: true });
        }

        issueOverlay.addEventListener("click", event => {
            if (event.target === issueOverlay || event.target.closest("[data-qc-issue-close]"))
                closeIssueDialog();
        });
        // Keep Escape and focus inside the nested discrepancy dialog instead
        // of allowing Bootstrap to dismiss the customer review underneath.
        modal.addEventListener("keydown", event => {
            if (!issueItemId || event.key !== "Escape") return;
            event.preventDefault();
            event.stopImmediatePropagation();
            closeIssueDialog();
        }, true);
        issueOverlay.addEventListener("keydown", event => {
            if (event.key !== "Tab") return;
            const controls = [...issueOverlay.querySelectorAll("button:not([disabled]), input:not([disabled]), textarea:not([disabled])")]
                .filter(element => element.getClientRects().length > 0);
            if (!controls.length) return;
            const first = controls[0], last = controls[controls.length - 1];
            if (event.shiftKey && document.activeElement === first) {
                event.preventDefault();
                last.focus();
            } else if (!event.shiftKey && document.activeElement === last) {
                event.preventDefault();
                first.focus();
            }
        });

        issueForm.addEventListener("submit", event => {
            event.preventDefault();
            if (!review || !issueItemId || hasUnsavedWork()) return;
            const id = issueItemId;
            const item = flatten().find(row => row.factorItemId === id);
            const draft = item && stateOf(item);
            if (!draft) return;
            const answers = Object.fromEntries([...issueQuestions.querySelectorAll("[data-qc-answer]:checked")]
                .map(input => [input.dataset.qcQuestion, Number(input.value)]));
            if (!review.questions.every(question => answers[question.id] === 0 || answers[question.id] === 1)
                || !review.questions.some(question => answers[question.id] === 1)) {
                issueFeedback.textContent = "به همه سؤال‌ها پاسخ دهید و حداقل یک مورد مغایرت را مشخص کنید.";
                return;
            }
            const description = issueQuestions.querySelector("[data-qc-note]")?.value || "";
            draft.status = 2;
            draft.answers = answers;
            draft.description = description;
            draft.saved = false;
            draft.failed = false;
            draft.revision++;
            clearNoteTimer(id);
            closeIssueDialog();
            renderReview();
            [...content.querySelectorAll('[data-qc-set-status="2"]')]
                .find(button => button.dataset.qcItemId === id)?.focus({ preventScroll: true });
            void persistItem(id);
        });

        content.addEventListener("click", event => {
            if (saving || itemPageLoading || !review || issueItemId) return;
            const sortButton = event.target.closest("[data-qc-item-sort]");
            if (sortButton) {
                const selectedField = sortButton.dataset.qcItemSort;
                if (hasUnsavedWork() || itemPageLoading) return;
                if (selectedField === itemSortField) itemSortDirection *= -1;
                else { itemSortField = selectedField; itemSortDirection = 1; }
                void loadReviewPage(true);
                return;
            }
            const setButton = event.target.closest("[data-qc-set-status]");
            if (setButton && !setButton.disabled) {
                const id = setButton.dataset.qcItemId;
                const draft = drafts.get(id);
                if (!draft || savingItems.has(id)) return;
                const selected = Number(setButton.dataset.qcSetStatus);
                if (selected === 2) {
                    openIssueDialog(id, setButton);
                    return;
                }
                // Clicking an already selected "without discrepancy" clears the review.
                draft.status = draft.status === 1 ? 0 : 1;
                draft.saved = false;
                draft.failed = false;
                draft.revision++;
                clearNoteTimer(id);
                if (draft.status === 1) draft.answers = {};
                renderReview();
                void persistItem(id);
                return;
            }
            const retry = event.target.closest("[data-qc-retry]");
            if (retry && !retry.disabled) {
                const draft = drafts.get(retry.dataset.qcRetry);
                if (draft) {
                    draft.failed = false;
                    void persistItem(retry.dataset.qcRetry);
                }
            }
        });

        saveAllButton.addEventListener("click", () => {
            if (!saveAllButton.disabled) saveRemaining(flatten());
        });
        modal.addEventListener("hide.bs.modal", event => {
            if (issueItemId) {
                event.preventDefault();
                closeIssueDialog();
                return;
            }
            if (hasUnsavedWork()) {
                event.preventDefault();
                return;
            }
            formController?.abort();
            clearTimeout(itemSearchTimer);
            formGeneration++;
        });
        modal.addEventListener("hidden.bs.modal", () => {
            const editedCustomerId = reviewHasSavedChanges ? String(review?.customerId || "").toLowerCase() : null;
            // Derive the displayed counts from confirmed saves, before clearing modal state.
            const itemCount = review?.totalItems ?? 0;
            const reviewed = review?.reviewedItems ?? 0;
            const discrepancy = review?.discrepancyItems ?? 0;
            const complete = itemCount > 0 && reviewed === itemCount;
            reviewHasSavedChanges = false;
            issueItemId = null;
            issueReturnFocus = null;
            issueOverlay.hidden = true;
            issueQuestions.replaceChildren();
            review = null;
            drafts.clear();
            content.replaceChildren();
            summary.replaceChildren();
            reviewToolbar.hidden = true;
            itemSearch.value = "";
            itemSearchBar.hidden = true;
            itemSearchEmpty.hidden = true;
            itemPaging.hidden = true;
            itemPages.replaceChildren();
            itemPageLoading = false;
            formFeedback.textContent = "";

            if (!editedCustomerId || !isActive()) return;
            // Do not reload or re-sort the customer list after saving a review.
            // Only update the edited card in-place, retaining other cards and scroll position.
            clearReviewFocus();
            lastCheckedCustomerId = editedCustomerId;
            queue.querySelectorAll(".qc-client-last-checked").forEach(card =>
                card.classList.remove("qc-client-last-checked"));
            const card = [...queue.querySelectorAll(".qc-client-card")].find(element =>
                element.querySelector("[data-qc-customer]")?.dataset.qcCustomer?.toLowerCase()
                    === editedCustomerId);
            if (card) {
                const reviewGroup = card.querySelector(".qc-client-review-group");
                if (reviewGroup) {
                    reviewGroup.classList.toggle("qc-client-review-complete", complete);
                    reviewGroup.classList.toggle("qc-client-review-pending", !complete);
                    const summary = reviewGroup.querySelector(".qc-client-count-reviewed");
                    if (summary) summary.innerHTML = reviewSummaryMarkup(reviewed, itemCount, discrepancy);
                }
                card.dataset.qcComplete = String(complete);
                const button = card.querySelector("[data-qc-customer]");
                if (button) button.innerHTML = complete
                    ? '<i class="fa fa-check-circle" aria-hidden="true"></i> مشاهده / ویرایش'
                    : '<i class="fa fa-check-circle" aria-hidden="true"></i> کنترل کیفیت';
                // Reviewing an item does not reset the customer's recorded contact state.
                // Only the dedicated contact-status endpoint may change these indicators.
                card.querySelectorAll("[data-qc-contact]").forEach(button => {
                    button.disabled = complete;
                });
                card.classList.add("qc-client-last-checked");
                // Restart the green fade when this same customer is reviewed again.
                card.classList.remove("qc-client-last-reviewed");
                void card.offsetWidth;
                card.classList.add("qc-client-last-reviewed");
                refreshNoResponseCooldowns();
                // The card stays in its original slot for the entire five-second fade.
                // Only remove it afterwards when the selected status no longer includes it.
                const selectedStatus = statusFilter.value;
                const leavesFilter = (complete && ["0", "3", "4"].includes(selectedStatus))
                    || (selectedStatus === "1" && !complete)
                    || (selectedStatus === "2" && discrepancy === 0)
                    || (selectedStatus === "5" && reviewed === 0);
                lastReviewedHighlightTimer = setTimeout(() => {
                    if (!isActive() || !card.isConnected) return;
                    card.classList.remove("qc-client-last-reviewed");
                    if (leavesFilter) {
                        card.remove();
                        if (!queue.querySelector(".qc-client-card") && !total)
                            queueFeedback.textContent = "مشتری واجد شرایطی با این فیلتر یافت نشد.";
                    }
                    lastReviewedHighlightTimer = null;
                }, 5000);
            }
            // Refresh the KPI totals and filtered count without replacing or moving any card.
            void loadCustomers(false, true);
        });
        pageSignal?.addEventListener("abort", () => {
            clearReviewFocus();
            cancelHistoryDeleteHold();
            queueLayoutObserver?.disconnect();
            clearTimeout(midnightRefreshTimer);
            clearInterval(noResponseCooldownTimer);
            listController?.abort();
            formController?.abort();
            clearTimeout(itemSearchTimer);
            historyController?.abort();
            customerNotesController?.abort();
        }, { once: true });
        window.addEventListener("pagehide", () => {
            clearReviewFocus();
            cancelHistoryDeleteHold();
            queueLayoutObserver?.disconnect();
            clearTimeout(midnightRefreshTimer);
            clearInterval(noResponseCooldownTimer);
        }, { once: true });
        scheduleNextDayRefresh();
        loadCustomers();
    });
})();
