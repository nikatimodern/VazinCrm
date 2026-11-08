(() => {
    "use strict";

    function initializeLegacyMappingReport() {
        const page = document.getElementById("legacyMappingReport");
        if (!page || page.dataset.initialized === "true") return;
        page.dataset.initialized = "true";

        const $ = selector => page.querySelector(selector);
        const records = $("#lmrRecords");
        const feedback = $("#lmrFeedback");
        const resultCount = $("#lmrResultCount");
        const pageInfo = $("#lmrPageInfo");
        const pagination = $("#lmrPagination");
        const statusTabs = $("#lmrStatusTabs");
        const searchInput = $("#lmrSearch");
        const typeInput = $("#lmrType");
        const reasonInput = $("#lmrReason");
        const pageSizeInput = $("#lmrPageSize");
        const refreshButton = $("#lmrRefresh");
        const formatNumber = new Intl.NumberFormat("fa-IR");
        const escape = value => escapeHtml(String(value ?? ""));
        const text = value => value === undefined || value === null || value === "" ? "—" : String(value);

        const statusNames = {
            customerConflict: "تعارض مشتری",
            ambiguous: "تطبیق مبهم",
            blocked: "مسدود به‌دلیل مشتری",
            notMatched: "معادل پیدا نشده",
            pending: "آماده نگاشت",
            saved: "نگاشت ثبت‌شده"
        };
        const kindNames = { Customer: "مشتری", PreFactor: "پیش‌فاکتور", Factor: "فاکتور" };
        const reasons = {
            MultipleSepidarParties: ["چند مشتری سپیدار برای یک مشتری CRM", "اسناد مرتبط با این مشتری به اشخاص مختلف سپیدار اشاره می‌کنند. شناسه صحیح را بررسی کنید."],
            PartyAlreadyMappedToAnotherCustomer: ["مشتری سپیدار قبلاً مپ شده", "یک مشتری دیگر CRM به همین شخص سپیدار متصل است. قبل از ثبت، تکراری بودن مشتریان را بررسی کنید."],
            DuplicateCrmCustomersForParty: ["چند مشتری CRM برای یک شخص سپیدار", "چند رکورد CRM به یک PartyId اشاره دارند. مشتری اصلی یا سیاست ادغام را مشخص کنید."],
            ExistingDuplicateCustomerParty: ["نگاشت تکراری در سوابق", "یک Party سپیدار در مپینگ‌های موجود برای چند مشتری CRM ثبت شده است."],
            ExistingCustomerMappingMismatch: ["مغایرت با مپینگ مشتری موجود", "مشتری CRM قبلاً به شخص دیگری مپ شده است؛ مپینگ موجود و اسناد مرتبط را بررسی کنید."],
            LegacyFinancialCustomerIdMismatch: ["شناسه مالی قدیمی مغایر است", "FinancialCustomerId در CRM با مشتری اسناد سپیدار یکسان نیست."],
            PartyNotFoundOrNotCustomer: ["شخص سپیدار معتبر نیست", "Party مربوطه یافت نشد یا در سپیدار به‌عنوان مشتری علامت‌گذاری نشده است."],
            QuotationNumberNotFound: ["شماره پیش‌فاکتور در سپیدار یافت نشد", "شماره سریال سند CRM در پیش‌فاکتورهای سپیدار ۱ وجود ندارد."],
            QuotationAmountMismatch: ["اختلاف مبلغ پیش‌فاکتور", "مبلغ ناخالص یا خالص CRM و سپیدار متفاوت است؛ جزئیات را باز کنید."],
            QuotationFiscalYearMismatch: ["سال مالی پیش‌فاکتور متفاوت است", "سال مالی CRM و سپیدار را بررسی کنید؛ روز دقیق ثبت ملاک نیست."],
            QuotationCustomerMismatch: ["مشتری پیش‌فاکتور متفاوت است", "شناسه مشتری سند CRM با CustomerPartyRef سپیدار همخوان نیست."],
            QuotationMultipleFieldMismatch: ["چند مغایرت در پیش‌فاکتور", "بیش از یک شرط تطبیق برقرار نیست. جزئیات نامزدها را بررسی کنید."],
            MultipleMatchingQuotations: ["چند پیش‌فاکتور مطابق", "بیش از یک پیش‌فاکتور با شرایط یکسان پیدا شده است. انتخاب خودکار امن نیست."],
            InvoiceIdNotFound: ["شناسه فاکتور در سپیدار یافت نشد", "Factor.SepidarId قدیمی به سند قابل شناسایی اشاره نمی‌کند."],
            InvoiceForQuotationNotFound: ["فاکتور مرتبط با پیش‌فاکتور پیدا نشد", "برای QuotationRef متناظر، فاکتوری در سپیدار پیدا نشده است."],
            RelatedQuotationNotMapped: ["پیش‌فاکتور هنوز مپ نشده", "ابتدا رابطه پیش‌فاکتور CRM و سپیدار باید مشخص شود."],
            InvoiceAmountMismatch: ["اختلاف مبلغ فاکتور", "مبلغ نهایی فاکتور CRM با فاکتور سپیدار برابر نیست."],
            InvoiceCustomerMismatch: ["مشتری فاکتور متفاوت است", "CustomerPartyRef فاکتور با شناسه مالی مشتری CRM متفاوت است."],
            InvoiceFinancialNumberMismatch: ["شماره مالی فاکتور متفاوت است", "شماره SnFinancial CRM و شماره فاکتور سپیدار برابر نیست."],
            InvoiceFiscalYearMismatch: ["سال مالی فاکتور متفاوت است", "سال مالی اسناد CRM و سپیدار را بررسی کنید."],
            InvoiceQuotationMismatch: ["پیش‌فاکتور مرتبط متفاوت است", "QuotationRef فاکتور سپیدار با مپینگ پیش‌فاکتور CRM برابر نیست."],
            InvoiceMultipleFieldMismatch: ["چند مغایرت در فاکتور", "مبلغ، مشتری، سال مالی یا شماره مالی همخوانی ندارد."],
            MultipleMatchingInvoices: ["چند فاکتور مطابق", "چند فاکتور با ویژگی‌های یکسان پیدا شده‌اند و باید دستی بررسی شوند."],
            ExternalTargetAlreadyMapped: ["سند سپیدار قبلاً استفاده شده", "این شناسه سپیدار پیش‌تر به رکورد CRM دیگری مپ شده است."],
            DuplicateProposedTarget: ["چند سند CRM برای یک سند سپیدار", "دو یا چند سند CRM در حال اشاره به یک شناسه سپیدار هستند."],
            CustomerMappingPendingApproval: ["مپینگ مشتری هنوز ثبت نشده", "ابتدا مپینگ مشتری را بررسی و ثبت کنید؛ سپس نگاشت سند را دوباره ارزیابی کنید."],
            CustomerNotMapped: ["مشتری مپ نشده", "بدون نگاشت قطعی مشتری، ثبت پیش‌فاکتور و فاکتور مجاز نیست."],
            CustomerHasConflict: ["مشتری دارای تعارض است", "تا تعیین تکلیف تعارض مشتری، اسناد مرتبط مسدود می‌مانند."],
            DocumentCustomerMissing: ["مشتری CRM نامشخص است", "در CRM برای سند مشتری معتبری مشخص نشده است."],
            DocumentCrmCustomerMismatch: ["مشتری فاکتور با پیش‌فاکتور متفاوت است", "CustomerId فاکتور و پیش‌فاکتور مرتبط یکسان نیستند."],
            DocumentSepidarCustomerMismatch: ["مشتری سند سپیدار با مپینگ متفاوت است", "PartyId سند سپیدار با مپینگ قطعی مشتری CRM برابر نیست."],
            ReadyForReview: ["آماده نگاشت", "رابطه شناسایی شده اما هنوز در ExternalMapping ذخیره نشده است."],
            AlreadyMapped: ["نگاشت ثبت شده", "این ارتباط در جدول ExternalMapping وجود دارد."]
        };

        const state = {
            rows: [],
            status: "customerConflict",
            type: "",
            reason: "",
            search: "",
            page: 1,
            pageSize: 12,
            savedRows: [],
            savedTotal: 0,
            loading: false,
            requestId: 0
        };

        function isActive() {
            return page.isConnected && !(typeof PageRuntime !== "undefined" && PageRuntime.current?.signal?.aborted);
        }

        function reasonInfo(code) {
            return reasons[code] || [code || "نیازمند بررسی", "اطلاعات دو طرف را مقایسه کنید و علت اختلاف را بررسی کنید."];
        }

        function renderSide(side, origin, kind, amountMismatch) {
            const items = kind === "Customer"
                ? [
                    ["کد / شناسه مشتری", side.number],
                    ["نام مشتری", side.customer]
                ]
                : [
                    ["شماره سند", side.number],
                    ["مبلغ نهایی (ریال)", side.amount == null ? null : Number(side.amount).toLocaleString("en-US")],
                    ["تاریخ", side.date],
                    ["مشتری", side.customer]
                ];
            return '<section class="lmr-side ' + origin + '">' +
                '<div class="lmr-side-title"><span>' + escape(side.title) + '</span><span class="lmr-side-id" title="' + escape(side.id) + '">ID: ' + escape(side.id) + '</span></div>' +
                '<div class="lmr-side-body">' +
                items.map(([label, value]) => '<div class="lmr-pair ' +
                    (amountMismatch && label === "مبلغ نهایی (ریال)" ? "is-mismatch" : "") +
                    '"><span>' + escape(label) + '</span><strong>' + escape(text(value)) + '</strong></div>').join("") +
                '<div class="lmr-pair wide"><span>اطلاعات تکمیلی</span><strong>' + escape(text(side.extra)) + '</strong></div>' +
                '</div></section>';
        }

        function renderCard(row) {
            const reason = reasonInfo(row.reason);
            const hasIssue = row.status !== "pending" && row.status !== "saved";
            const amountMismatch = row.kind !== "Customer" &&
                row.crm?.amount != null && row.sepidar?.amount != null &&
                Number(row.crm.amount) !== Number(row.sepidar.amount);
            const explanation = row.sources?.length
                ? row.details + "\n\nاسناد مرتبط:\n" + row.sources.join("\n")
                : row.details;
            return '<article class="lmr-record" role="listitem">' +
                '<div class="lmr-record-head">' +
                '<div class="lmr-record-title"><span class="lmr-record-type">' + escape(kindNames[row.kind] || row.kind) + '</span><strong>' + escape(reason[0]) + '</strong></div>' +
                '<span class="lmr-state lmr-state-' + escape(row.status === "customerConflict" ? "conflict" : row.status) + '">' +
                escape(statusNames[row.status] || row.status) + '</span></div>' +
                '<div class="lmr-compare">' + renderSide(row.crm, "crm", row.kind, amountMismatch) +
                '<div class="lmr-compare-symbol"><i class="fas fa-exchange-alt" aria-hidden="true"></i></div>' +
                renderSide(row.sepidar, "sepidar", row.kind, amountMismatch) + '</div>' +
                '<div class="lmr-reason ' + (hasIssue ? "has-issue" : "") + '">' +
                '<i class="fas fa-' + (hasIssue ? "exclamation-circle" : "check-circle") + '" aria-hidden="true"></i>' +
                '<div class="lmr-reason-content"><strong>' + escape(reason[0]) + '</strong><p>' + escape(reason[1]) + '</p></div></div>' +
                (explanation ? '<details class="lmr-details"><summary>جزئیات فنی و اسناد مرتبط</summary><pre>' + escape(explanation) + '</pre></details>' : '') +
                '</article>';
        }

        function updateSummary(summary) {
            const set = (id, value) => { const element = $(id); if (element) element.textContent = formatNumber.format(value ?? 0); };
            set("#lmrSavedCount", summary.saved);
            set("#lmrCustomerConflicts", summary.customerConflicts);
            set("#lmrBlockedCount", summary.blockedDocuments);
            set("#lmrPendingCustomers", summary.pendingCustomers);
            set("#lmrPendingDocuments", summary.pendingDocuments);
        }

        function updateReasonOptions() {
            const previous = state.reason;
            reasonInput.replaceChildren(new Option("همه علت‌ها", ""));
            reasonInput.disabled = state.status === "saved";
            if (reasonInput.disabled) return;
            const relevant = state.rows.filter(row => row.status === state.status &&
                (!state.type || row.kind === state.type));
            const distinct = [...new Set(relevant.map(row => row.reason))].sort((a, b) =>
                reasonInfo(a)[0].localeCompare(reasonInfo(b)[0], "fa"));
            distinct.forEach(reason => reasonInput.add(new Option(reasonInfo(reason)[0], reason)));
            if (distinct.includes(previous)) reasonInput.value = previous;
            else { reasonInput.value = ""; state.reason = ""; }
        }

        function renderPagination(total) {
            const maxPage = Math.max(1, Math.ceil(total / state.pageSize));
            state.page = Math.min(Math.max(1, state.page), maxPage);
            pagination.replaceChildren();
            function addButton(label, pageNumber, isCurrent, isDisabled) {
                const button = document.createElement("button");
                button.type = "button";
                button.textContent = label;
                button.dataset.page = String(pageNumber);
                button.disabled = isDisabled || state.loading;
                if (isCurrent) {
                    button.classList.add("is-current");
                    button.setAttribute("aria-current", "page");
                }
                pagination.append(button);
            }
            addButton("قبلی", state.page - 1, false, state.page === 1);
            for (let index = Math.max(1, state.page - 2); index <= Math.min(maxPage, state.page + 2); index++)
                addButton(formatNumber.format(index), index, index === state.page, false);
            addButton("بعدی", state.page + 1, false, state.page === maxPage);
            const start = total === 0 ? 0 : (state.page - 1) * state.pageSize + 1;
            const end = Math.min(total, state.page * state.pageSize);
            pageInfo.textContent = formatNumber.format(start) + " تا " +
                formatNumber.format(end) + " از " + formatNumber.format(total);
            resultCount.textContent = formatNumber.format(total) + " مورد";
        }

        function showRows(rows, total) {
            records.innerHTML = rows.length
                ? rows.map(renderCard).join("")
                : '<div class="lmr-empty"><i class="fas fa-search" aria-hidden="true"></i> موردی مطابق فیلترها پیدا نشد.</div>';
            feedback.hidden = true;
            renderPagination(total);
        }

        function renderLocal() {
            if (state.status === "saved") return;
            const search = state.search.toLocaleLowerCase("fa").trim();
            const filtered = state.rows.filter(row => {
                if (row.status !== state.status) return false;
                if (state.type && row.kind !== state.type) return false;
                if (state.reason && row.reason !== state.reason) return false;
                if (!search) return true;
                return [
                    row.crm?.number, row.crm?.customer, row.crm?.id, row.crm?.extra,
                    row.sepidar?.number, row.sepidar?.customer, row.sepidar?.id,
                    row.reason, reasonInfo(row.reason)[0], row.details
                ].join(" ").toLocaleLowerCase("fa").includes(search);
            });
            const offset = (state.page - 1) * state.pageSize;
            showRows(filtered.slice(offset, offset + state.pageSize), filtered.length);
        }

        async function loadSaved() {
            const requestId = ++state.requestId;
            state.loading = true;
            feedback.hidden = false;
            feedback.textContent = "در حال دریافت نگاشت‌های ثبت‌شده…";
            records.replaceChildren();
            const result = await sendApiRequest({
                url: "/Home/GetSavedExternalMappings",
                params: { type: state.type, search: state.search, page: state.page, pageSize: state.pageSize },
                timeout: 60000, showLoading: false
            });
            if (!isActive() || requestId !== state.requestId) return;
            state.loading = false;
            if (!result) {
                feedback.textContent = "دریافت نگاشت‌های ثبت‌شده ناموفق بود. دوباره تلاش کنید.";
                return;
            }
            state.savedTotal = result.total;
            showRows(result.rows || [], result.total);
        }

        async function loadReport() {
            if (state.loading) return;
            state.loading = true;
            refreshButton.disabled = true;
            feedback.hidden = false;
            feedback.textContent = "در حال مقایسه اطلاعات CRM و سپیدار ۱؛ این گزارش صرفاً خواندنی است…";
            records.replaceChildren();
            const result = await sendApiRequest({
                url: "/Home/GetExternalMappingReview",
                timeout: 180000, showLoading: false
            });
            if (!isActive()) return;
            state.loading = false;
            refreshButton.disabled = false;
            if (!result) {
                feedback.textContent = "گزارش دریافت نشد. ارتباط با سرور را بررسی و بازخوانی کنید.";
                return;
            }
            state.rows = result.rows || [];
            updateSummary(result.summary || {});
            updateReasonOptions();
            if (state.status === "saved") await loadSaved();
            else renderLocal();
        }

        statusTabs.addEventListener("click", event => {
            const tab = event.target.closest("[data-status]");
            if (!tab || !statusTabs.contains(tab)) return;
            state.status = tab.dataset.status;
            state.page = 1;
            state.reason = "";
            reasonInput.value = "";
            statusTabs.querySelectorAll("[data-status]").forEach(button =>
                button.classList.toggle("is-active", button === tab));
            updateReasonOptions();
            if (state.status === "saved") loadSaved();
            else { ++state.requestId; renderLocal(); }
        });

        typeInput.addEventListener("change", () => {
            state.type = typeInput.value;
            state.reason = "";
            state.page = 1;
            updateReasonOptions();
            if (state.status === "saved") loadSaved();
            else renderLocal();
        });

        reasonInput.addEventListener("change", () => {
            state.reason = reasonInput.value;
            state.page = 1;
            renderLocal();
        });

        searchInput.addEventListener("input", debounce(() => {
            state.search = searchInput.value.trim();
            state.page = 1;
            if (state.status === "saved") loadSaved();
            else renderLocal();
        }, 300));

        pageSizeInput.addEventListener("change", () => {
            state.pageSize = Number(pageSizeInput.value);
            state.page = 1;
            if (state.status === "saved") loadSaved();
            else renderLocal();
        });

        pagination.addEventListener("click", event => {
            const button = event.target.closest("[data-page]");
            if (!button || button.disabled) return;
            state.page = Number(button.dataset.page);
            if (state.status === "saved") loadSaved();
            else renderLocal();
        });

        refreshButton.addEventListener("click", loadReport);
        if (typeof enableMomentumScrollScope === "function")
            enableMomentumScrollScope(page);
        loadReport();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initializeLegacyMappingReport, { once: true });
    } else {
        initializeLegacyMappingReport();
    }
})();
