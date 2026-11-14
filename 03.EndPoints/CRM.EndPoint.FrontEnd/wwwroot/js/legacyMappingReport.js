(() => {
    "use strict";

    function initializeMappingManager() {
        const page = document.getElementById("legacyMappingReport");
        if (!page || page.dataset.initialized) return;
        page.dataset.initialized = "1";

        const get = id => page.querySelector("#" + id);
        const softwareSelect = get("lmrSoftware");
        const savedRoot = get("lmrSavedRecords");
        const proposalRoot = get("lmrProposalRecords");
        const selectAll = get("lmrSelectPage");
        const saveSelected = get("lmrSaveSelected");
        const proposalTabs = get("lmrProposalStatusTabs");
        const mainTabs = get("lmrMainTabs");
        const proposalReason = get("lmrProposalReason");
        const token = page.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const format = new Intl.NumberFormat("fa-IR");
        const escape = value => escapeHtml(String(value ?? ""));
        const show = value => value === null || value === undefined || value === "" ? "—" : String(value);
        const money = value => value == null ? "—" : Number(value).toLocaleString("en-US");
        const validId = value => /^[1-9]\d*$/.test(String(value || ""));
        const isActive = () => page.isConnected &&
            !(typeof PageRuntime !== "undefined" && PageRuntime.current?.signal?.aborted);

        const statusNames = {
            saved:"ثبت‌شده", pending:"آماده ثبت", customerConflict:"تعارض مشتری",
            ambiguous:"نگاشت مبهم", blocked:"منتظر مشتری", notMatched:"بدون تطبیق قطعی"
        };
        const reasonNames = {
            MultipleSepidarParties:["چند مشتری سپیدار برای یک مشتری CRM","پیش‌فاکتورها یا فاکتورهای این مشتری به اشخاص متفاوت سپیدار اشاره دارند. در ارتباط‌های پایین مشخص است هر کدام متعلق به کدام مشتری است."],
            DuplicateCrmCustomersForParty:["چند مشتری CRM برای یک مشتری سپیدار","چند مشتری با کدهای جداگانه در CRM به یک مشتری سپیدار اشاره می‌کنند. باید مشخص شود نگاشت متعلق به کدام مشتری CRM است."],
            PartyAlreadyMappedToAnotherCustomer:["مشتری سپیدار قبلاً نگاشت شده","این مشتری سپیدار به مشتری دیگری در CRM متصل شده است؛ ابتدا رابطه ثبت‌شده را بررسی کنید."],
            ExistingDuplicateCustomerParty:["نگاشت مشتری تکراری از قبل وجود دارد","در نگاشت‌های ذخیره‌شده چند مشتری CRM به یک مشتری سپیدار متصل‌اند."],
            ExistingCustomerMappingMismatch:["نگاشت مشتری با ارتباط‌های فعلی مغایر است","مشتری قبلاً به شخص دیگری در سپیدار وصل شده است."],
            LegacyFinancialCustomerIdMismatch:["کد مالی قدیمی با سپیدار متفاوت است","کد مشتری قدیمی CRM با مشتری ثبت‌شده در سپیدار یکسان نیست."],
            PartyNotFoundOrNotCustomer:["معادل سپیدار معتبر نیست","این شخص در نرم‌افزار مقصد پیدا نشده یا مشتری محسوب نمی‌شود."],
            CustomerHasConflict:["ابتدا تعارض مشتری را تعیین تکلیف کنید","تا زمانی که نگاشت مشتری مشخص نباشد، پیش‌فاکتور یا فاکتور قابل نگاشت نیست."],
            CustomerMappingPendingApproval:["مشتری هنوز نگاشت نشده","پیشنهاد نگاشت مشتری آماده است اما هنوز در CRM ثبت نشده است."],
            CustomerNotMapped:["ابتدا مشتری را نگاشت کنید","پیش‌فاکتور یا فاکتور فقط بعد از ثبت نگاشت مشتری در همین نرم‌افزار قابل نگاشت است."],
            DocumentCustomerMissing:["مشتری CRM مشخص نیست","ابتدا مشتری این پیش‌فاکتور یا فاکتور را در CRM مشخص کنید."],
            DocumentCrmCustomerMismatch:["مشتری فاکتور و پیش‌فاکتور CRM فرق دارد","مشتری ثبت‌شده در فاکتور با پیش‌فاکتور مرتبط یکسان نیست."],
            DocumentSepidarCustomerMismatch:["مشتری سپیدار با نگاشت فرق دارد","کد مشتری این پیش‌فاکتور یا فاکتور با مشتری تأییدشده CRM مطابقت ندارد."],
            DuplicateProposedTarget:["چند مورد CRM برای یک مورد سپیدار","چند پیش‌فاکتور یا فاکتور CRM برای یک معادل سپیدار پیشنهاد شده‌اند. فقط ارتباط صحیح را انتخاب کنید."],
            ExternalTargetAlreadyMapped:["این معادل قبلاً استفاده شده","این مورد سپیدار قبلاً به یک مورد دیگر CRM وصل شده است."],
            QuotationNumberNotFound:["شماره پیش‌فاکتور یافت نشد","شماره پیش‌فاکتور CRM در سپیدار پیدا نشد."],
            QuotationAmountMismatch:["مبلغ پیش‌فاکتور متفاوت است","مبالغ CRM و سپیدار را قبل از انتخاب معادل بررسی کنید."],
            QuotationFiscalYearMismatch:["سال مالی پیش‌فاکتور فرق دارد","سال مالی دو طرف یکسان نیست؛ روز دقیق تاریخ مبنای تطبیق نیست."],
            QuotationCustomerMismatch:["مشتری پیش‌فاکتور فرق دارد","پیش‌فاکتور احتمالی سپیدار برای مشتری متفاوتی ثبت شده است."],
            QuotationMultipleFieldMismatch:["چند اختلاف در پیش‌فاکتور","مشتری، مبلغ، شماره و سال مالی را بررسی کنید."],
            MultipleMatchingQuotations:["چند پیش‌فاکتور احتمالی","چند پیش‌فاکتور مشابه پیدا شده؛ معادل درست را انتخاب کنید."],
            RelatedQuotationNotMapped:["پیش‌فاکتور مرتبط نگاشت نشده","معادل پیش‌فاکتور مرتبط با فاکتور CRM هنوز پیدا نشده است."],
            InvoiceIdNotFound:["شناسه فاکتور قدیمی پیدا نشد","شناسه قبلی فاکتور در سپیدار وجود ندارد."],
            InvoiceForQuotationNotFound:["فاکتور پیش‌فاکتور مرتبط پیدا نشد","در سپیدار برای پیش‌فاکتور مرتبط فاکتوری پیدا نشده است."],
            InvoiceAmountMismatch:["مبلغ فاکتور فرق دارد","مبلغ فاکتور CRM با معادل احتمالی سپیدار فرق دارد."],
            InvoiceCustomerMismatch:["مشتری فاکتور فرق دارد","مشتری فاکتور CRM با مشتری فاکتور احتمالی سپیدار یکسان نیست."],
            InvoiceFinancialNumberMismatch:["شماره مالی فاکتور فرق دارد","شماره مالی CRM با شماره فاکتور سپیدار یکسان نیست."],
            InvoiceFiscalYearMismatch:["سال مالی فاکتور فرق دارد","سال مالی دو طرف متفاوت است."],
            InvoiceQuotationMismatch:["پیش‌فاکتور فاکتور فرق دارد","فاکتور سپیدار به پیش‌فاکتور دیگری مرتبط است."],
            InvoiceMultipleFieldMismatch:["چند اختلاف در فاکتور","مشتری، مبلغ، شماره مالی و سال مالی را بررسی کنید."],
            MultipleMatchingInvoices:["چند فاکتور احتمالی","چند فاکتور مشابه پیدا شده؛ معادل درست را دستی انتخاب کنید."],
            UserExpertExactMatch:["تطبیق نام کارشناس و شخص","نام کارشناس CRM با شخص متناظر در نرم‌افزار مالی یکسان است؛ قبل از ثبت بررسی کنید."],
            UserExpertMultipleMatches:["چند شخص هم‌نام","چند شخص با همین نام در نرم‌افزار مالی پیدا شده‌اند؛ معادل درست کارشناس را انتخاب کنید."],
            UserExpertMultipleCrmUsers:["چند کارشناس CRM هم‌نام","چند کارشناس CRM نام یکسان دارند؛ نگاشت را با بررسی هویت صحیح، دستی ثبت کنید."],
            UserExpertNoMatch:["شخص هم‌نام یافت نشد","شخص متناظر قطعی پیدا نشده است؛ از ثبت دستی نگاشت کارشناس استفاده کنید."],
            UserExpertAlreadyMapped:["شخص قبلاً نگاشت شده","شخص مشابه در نرم‌افزار مالی به کارشناس دیگری نگاشت شده است."],
            ReadyForReview:["آماده ثبت",""],
            AlreadyMapped:["نگاشت ذخیره‌شده","این رابطه قبلاً در جدول ExternalMapping ثبت شده است."]
        };

        const state = {
            softwareId: "", softwareTitle: "", autoIds: [], manualIds: [],
            types: [], counts: [], selected: new Map(), targets: new Map(), crmTargets: new Map(), preview: new Map(),
            activeTab: "saved",
            saved: { search: "", type: "", page: 1, size: 10, total: 0, rows: [], request: 0 },
            proposal: { search: "", type: "", reason: "", status: "pending", page: 1, size: 10,
                loaded: false, rows: [], visible: [], summary: null, request: 0, calculating: false },
            processing: false,
            deleting: false,
            manualOpen: false,
            manualRequests: {crm:0, external:0}
        };

        function reason(row) {
            return reasonNames[row.reason] || [row.reason || "نیازمند بررسی",
                "اطلاعات مشتری، شماره، تاریخ و مبلغ هر دو طرف را بررسی کنید."];
        }

        function typeTitle(row) {
            return state.types.find(t => Number(t.value) === Number(row.mapType))?.title ||
                ({ Customer:"مشتری", PreFactor:"پیش‌فاکتور", Factor:"فاکتور",
                    Cash:"تنخواه", BaseCenterCash:"مرکز هزینه", Accountancy:"حسابداری",
                    AppUser:"کارشناسان" }[row.kind] || show(row.kind));
        }

        function canMap(row) {
            return row.status !== "saved" && row.status !== "blocked" &&
                !["PartyAlreadyMappedToAnotherCustomer", "ExistingDuplicateCustomerParty",
                    "ExistingCustomerMappingMismatch"].includes(row.reason) &&
                // Duplicate-party conflicts must be resolved by selecting one CRM customer.
                (row.reason !== "DuplicateCrmCustomersForParty" || !!row.crmCandidates?.length) &&
                [2,3,5,6].includes(Number(row.mapType)) &&
                state.manualIds.includes(state.softwareId);
        }

        function selectedCrm(row) {
            if (!row.crmCandidates?.length) return row.crm;
            const selected = state.crmTargets.get(row.key);
            return row.crmCandidates.find(item => item.id === selected)?.crm ||
                { title: "مشتری CRM را انتخاب کنید", id: "", customer: "", number: "" };
        }

        function consolidateDuplicateParties(rows) {
            const groups = new Map();
            const result = [];
            for (const row of rows) {
                if (row.status !== "customerConflict" ||
                    row.reason !== "DuplicateCrmCustomersForParty" ||
                    !validId(row.sepidar?.id)) {
                    result.push(row);
                    continue;
                }
                const partyId = String(row.sepidar.id);
                if (!groups.has(partyId)) groups.set(partyId, []);
                groups.get(partyId).push(row);
            }
            for (const [partyId, matches] of groups) {
                const uniqueCrm = new Map();
                matches.forEach(row => uniqueCrm.set(row.crm.id, {
                    id: row.crm.id,
                    crm: row.crm,
                    name: row.crm.customer || row.crm.title || "بدون نام",
                    code: row.crm.number
                }));
                if (uniqueCrm.size < 2) {
                    result.push(...matches);
                    continue;
                }
                const item = matches[0];
                result.push({
                    ...item,
                    key: "duplicate-party-" + partyId,
                    crmCandidates: [...uniqueCrm.values()],
                    sources: [...new Set(matches.flatMap(row => row.sources || []))],
                    details: "برای این مشتری نرم‌افزار مالی، چند مشتری CRM پیدا شده است. " +
                        "یک مشتری CRM را انتخاب کنید؛ مشتریان دیگر نگاشت نمی‌شوند."
                });
            }
            return result;
        }

        function targetValue(row) {
            if (row.crmCandidates?.length) return String(row.sepidar.id);
            if (state.targets.has(row.key)) return state.targets.get(row.key);
            const value = row.status === "pending" && row.options?.length === 1
                ? String(row.options[0].id) : "";
            state.targets.set(row.key,value);
            return value;
        }

        function verifiedTarget(row) {
            const value = targetValue(row);
            if (!validId(value)) return false;
            if (row.crmCandidates?.length)
                return !!state.crmTargets.get(row.key) &&
                    row.crmCandidates.some(item => item.id === state.crmTargets.get(row.key))
                    && !!row.sepidar?.customer;
            const key = state.softwareId+":"+row.mapType+":"+value;
            if (state.preview.has(key)) return true;
            return row.status === "pending" && row.options?.length === 1 &&
                String(row.sepidar?.id) === value &&
                !!row.sepidar?.title && !row.sepidar.title.includes("مشخص نیست");
        }

        function targetPreview(row) {
            if (row.crmCandidates?.length) return row.sepidar;
            const value = targetValue(row);
            if (row.status === "saved" || row.status === "pending" && !value) return row.sepidar;
            if (!value) return {title:"معادل انتخاب نشده",number:"",customer:"",id:""};
            const preview = state.preview.get(state.softwareId+":"+row.mapType+":"+value);
            if (preview) return preview;
            if (String(row.sepidar?.id) === value) return row.sepidar;
            const option = row.options?.find(x => String(x.id) === value);
            return { title:"معادل انتخابی", id:value, number:value, customer:option?.title || "" };
        }

        function sideCodeLabel(row, crm) {
            if (Number(row.mapType) === 5) return "کد مشتری";
            if (Number(row.mapType) === 6) return crm ? "نام کاربری" : "شناسه شخص";
            return "شماره";
        }

        function sideDisplayName(row, side) {
            if (!side) return "معادل مشخص نیست";
            if (Number(row.mapType) === 5 || Number(row.mapType) === 6)
                return show(side.customer || side.title);
            // The document number is already displayed on the secondary line.
            return show(side.title);
        }

        function simpleSide(row, side, crm) {
            const hasData = side && side.id && side.id !== "-" &&
                !String(side.title || "").includes("مشخص نیست");
            if (!hasData)
                return '<div class="lmr-decision-empty">موردی برای نمایش مشخص نشده است.</div>';
            const number = side.number && side.number !== "-" ? side.number : "";
            const numberText = number
                ? '<small>' + escape(sideCodeLabel(row, crm)) + ': ' + escape(number) + '</small>'
                : "";
            return '<div class="lmr-decision-single"><strong>' +
                escape(sideDisplayName(row, side)) + '</strong>' + numberText + '</div>';
        }

        function candidateChoices(row, side) {
            const crmSide = side === "crm";
            const candidates = crmSide ? row.crmCandidates || [] : row.options || [];
            const chosen = crmSide ? state.crmTargets.get(row.key) || "" : targetValue(row);
            const canChoose = canMap(row);
            const codeLabel = crmSide || [5, 6].includes(Number(row.mapType))
                ? sideCodeLabel(row, crmSide) : "شناسه";
            const name = (crmSide ? "crm-" : "external-") + row.key;
            return '<div class="lmr-choice-list" role="group" aria-label="' +
                escape(crmSide ? "انتخاب رکورد CRM" : "انتخاب معادل نرم‌افزار") + '">' +
                candidates.map(item => {
                    const id = String(item.id);
                    const title = crmSide ? item.name : item.title;
                    const code = crmSide ? item.code : item.id;
                    const selected = chosen === id;
                    const choice = canChoose
                        ? '<input type="radio" ' +
                            (crmSide ? 'data-crm-target ' : 'data-map-target ') +
                            'name="' + escape(name) + '" value="' + escape(id) + '"' +
                            (selected ? ' checked' : '') + '>'
                        : '<i class="fas fa-circle lmr-readonly-dot" aria-hidden="true"></i>';
                    return '<label class="lmr-choice' +
                        (selected && canChoose ? ' is-picked' : '') +
                        (!canChoose ? ' is-readonly' : '') + '">' +
                        choice + '<span class="lmr-choice-text"><strong>' +
                        escape(show(title)) + '</strong><small>' +
                        escape(codeLabel) + ': ' + escape(show(code)) +
                        '</small></span></label>';
                }).join("") + '</div>';
        }

        function decisionSide(row, side) {
            const crmSide = side === "crm";
            const isMultiCrm = crmSide && row.crmCandidates?.length > 1;
            const isMultiExternal = !crmSide && !row.crmCandidates?.length &&
                row.status !== "saved" &&
                !(row.status === "pending" && row.options?.length === 1) &&
                row.options?.length > 0;
            const heading = crmSide ? "CRM" : state.softwareTitle;
            const sideContent = isMultiCrm
                ? candidateChoices(row, "crm")
                : isMultiExternal
                    ? candidateChoices(row, "external")
                    : simpleSide(row, crmSide ? row.crm : targetPreview(row), crmSide);
            return '<section class="lmr-decision-side' +
                (crmSide ? ' is-crm' : ' is-external') + '">' +
                '<div class="lmr-decision-side-title"><span>' + escape(heading) + '</span>' +
                ((isMultiCrm || isMultiExternal) && canMap(row)
                    ? '<small>یک مورد انتخاب کنید</small>' : '') + '</div>' +
                sideContent + '</section>';
        }

        function expandableContent(row) {
            const evidence = [...new Set((row.sources || []).map(x => String(x).trim()).filter(Boolean))];
            const mismatch = renderMismatchDetails(row);
            const reasonText = reason(row)[1] || "";
            const raw = String(row.details || "").trim();
            const technical = /(?:Quotation|Invoice)Id=\d+[:]|(?:CustomerId|FiscalYearRef|SepidarPartyId)=/i.test(raw);
            const repeated = row.reason === "MultipleSepidarParties" ||
                row.reason === "DuplicateCrmCustomersForParty";
            const extra = row.status !== "saved" && row.status !== "pending"
                ? (raw && !technical && !(repeated && evidence.length) ? raw :
                    (!evidence.length && !mismatch ? reasonText : ""))
                : "";
            const references = evidence.length
                ? '<div class="lmr-related"><h4>ارتباط پیش‌فاکتورها، فاکتورها و مشتریان</h4>' +
                    '<ul>' + evidence.map(line => '<li>' + escape(line) + '</li>').join("") +
                    '</ul></div>' : "";
            const supplementary = extra
                ? '<div class="lmr-related lmr-detail-note"><h4>توضیحات تکمیلی</h4>' +
                    '<p>' + escape(extra) + '</p></div>' : "";
            return references + mismatch + supplementary;
        }

        // Break technical mismatch strings into readable, per-field comparisons.
        // Example: QuotationId=5: مبلغ ناخالص: CRM=351000000, Sepidar=1900000000/0000
        function formatComparisonValue(value) {
            const raw = String(value ?? "").trim();
            // Sepidar's legacy decimal formatting sometimes uses / as separator.
            const normalized = raw.replace(/[۰-۹]/g, c => String("۰۱۲۳۴۵۶۷۸۹".indexOf(c)))
                .replace(/[٠-٩]/g, c => String("٠١٢٣٤٥٦٧٨٩".indexOf(c)))
                .replace(/\//g, ".").replace(/,/g, "");
            if (/^[+-]?\d+(?:\.\d+)?$/.test(normalized)) {
                const n = Number(normalized);
                if (Number.isFinite(n)) return n.toLocaleString("fa-IR", { maximumFractionDigits: 4 });
            }
            return raw;
        }

        function renderMismatchDetails(row) {
            if (!row.details || !["notMatched", "ambiguous"].includes(row.status)) return "";

            // Different candidate invoices/quotations are separated by " | ".
            const chunks = row.details.split(/\s+\|\s+(?=(?:Quotation|Invoice)Id=\d+:)/);
            const sections = chunks.map(chunk => {
                const candidate = chunk.match(/^(Quotation|Invoice)Id=(\d+):\s*([\s\S]*)$/);
                if (!candidate) return "";

                const candidateId = candidate[2];
                const kind = candidate[1] === "Quotation" ? "پیش‌فاکتور" : "فاکتور";
                const option = (row.options || []).find(x => String(x.id) === candidateId);
                const title = option && option.title
                    ? option.title
                    : kind + " احتمالی با شناسه " + candidateId;
                const differences = candidate[3].split(/;\s*/).filter(Boolean);
                const tableRows = [];
                const other = [];

                for (const difference of differences) {
                    const pair = difference.match(/^(.+?):\s*CRM=([^,;]+),\s*Sepidar=(.+)$/);
                    const customer = difference.match(/^مشتری:\s*CRM SepidarPartyId=(\d+),\s*Sepidar=(\d+)$/);
                    const related = difference.match(/^QuotationRef:\s*expected=(\d+),\s*actual=(\d+)$/);
                    const text = pair || customer || related;
                    if (!text) {
                        other.push(difference);
                        continue;
                    }
                    const label = customer ? "کد مشتری سپیدار برای مشتری CRM" :
                        related ? "پیش‌فاکتور مرتبط" : pair[1].trim();
                    const crmValue = customer || related ? text[1] : pair[2];
                    const sepidarValue = customer || related ? text[2] : pair[3];
                    const isMoney = label.includes("مبلغ");
                    tableRows.push('<div class="lmr-compare-field">' +
                        '<span class="lmr-compare-label">' + escape(label + (isMoney ? " (ریال)" : "")) + '</span>' +
                        '<strong>' + escape(formatComparisonValue(crmValue)) + '</strong>' +
                        '<strong>' + escape(formatComparisonValue(sepidarValue)) + '</strong></div>');
                }

                if (!tableRows.length && !other.length) return "";
                const header = '<div class="lmr-compare-headers"><span>مورد اختلاف</span><span>CRM</span><span>' +
                    escape(state.softwareTitle) + '</span></div>';
                return '<section class="lmr-mismatch-candidate">' +
                    '<h5>' + escape(title) + '</h5>' +
                    (tableRows.length ? header + tableRows.join("") : "") +
                    (other.length ? '<div class="lmr-mismatch-other">' +
                        other.map(line => '<p>' + escape(line) + '</p>').join("") + '</div>' : "") +
                    '</section>';
            }).filter(Boolean);

            if (!sections.length) return "";
            return '<div class="lmr-mismatch-details"><h4>مقایسه موارد اختلاف</h4>' +
                sections.join("") + '</div>';
        }

        function card(row) {
            const mapped = canMap(row);
            const selected = mapped && verifiedTarget(row);
            const extra = expandableContent(row);
            const label = reason(row)[0];
            const stateClass = row.status === "saved" ? "saved" :
                row.status === "pending" ? "proposal" : "problem";
            const needsManual = row.status !== "saved" && row.status !== "pending" &&
                !row.crmCandidates?.length && !row.options?.length &&
                state.manualIds.includes(state.softwareId);
            const actions = (extra
                    ? '<button type="button" class="lmr-expand" data-expand aria-expanded="false">' +
                        '<i class="fas fa-chevron-down" aria-hidden="true"></i> توضیحات</button>'
                    : "") +
                (needsManual
                    ? '<button type="button" class="lmr-btn lmr-btn-soft" data-open-manual>' +
                        '<i class="fas fa-plus-circle" aria-hidden="true"></i> ثبت دستی</button>'
                    : "") +
                (mapped && (row.options?.length || row.crmCandidates?.length ||
                        row.status === "pending")
                    ? '<button type="button" class="lmr-btn lmr-btn-primary" data-map-one ' +
                        (!selected ? "disabled" : "") + '>ثبت نگاشت</button>'
                    : "") +
                (row.status === "saved" && row.mappingId
                    ? '<button type="button" class="lmr-btn lmr-btn-danger" ' +
                        'data-delete-mapping>حذف نگاشت</button>'
                    : "");
            return '<article class="lmr-item lmr-decision-card lmr-' + stateClass +
                '" data-key="' + escape(row.key) + '" role="listitem">' +
                '<header class="lmr-item-head">' +
                    '<span class="lmr-type">' + escape(typeTitle(row)) + '</span>' +
                    '<span class="lmr-state">' +
                        escape(statusNames[row.status] || row.status) + '</span>' +
                    (row.status === "saved" || row.status === "pending" ? "" :
                        '<span class="lmr-short-reason">' + escape(label) + '</span>') +
                    (mapped && selected
                        ? '<label class="lmr-item-select" title="انتخاب برای ثبت گروهی">' +
                            '<input type="checkbox" data-select-map aria-label="انتخاب برای ثبت گروهی" ' +
                            (state.selected.has(row.key) ? "checked " : "") +
                            (!selected ? "disabled " : "") + '></label>'
                        : "") +
                '</header>' +
                '<div class="lmr-decision-grid">' +
                    decisionSide(row, "crm") +
                    '<span class="lmr-decision-arrow" aria-hidden="true">' +
                        '<i class="fas fa-exchange-alt"></i></span>' +
                    decisionSide(row, "external") +
                '</div>' +
                (actions ? '<footer class="lmr-item-footer">' + actions + '</footer>' : "") +
                (extra ? '<div class="lmr-expanded" hidden>' + extra + '</div>' : "") +
                '</article>';
        }

        function refreshProposalCard(row) {
            const old = [...proposalRoot.querySelectorAll("[data-key]")]
                .find(element => element.dataset.key === row.key);
            if (!old) return;
            const expanded = !!old.querySelector(".lmr-expanded") &&
                !old.querySelector(".lmr-expanded").hidden;
            const active = old.contains(document.activeElement) ? document.activeElement : null;
            const focusField = active?.matches("[data-crm-target], [data-map-target]")
                ? { kind: active.hasAttribute("data-crm-target") ? "data-crm-target" : "data-map-target",
                    value: active.value } : null;
            old.outerHTML = card(row);
            const next = [...proposalRoot.querySelectorAll("[data-key]")]
                .find(element => element.dataset.key === row.key);
            if (!next) return;
            if (expanded && next.querySelector(".lmr-expanded")) {
                next.querySelector(".lmr-expanded").hidden = false;
                next.querySelector("[data-expand]")?.setAttribute("aria-expanded", "true");
            }
            if (focusField) {
                const input = [...next.querySelectorAll("[" + focusField.kind + "]")]
                    .find(element => element.value === focusField.value);
                input?.focus({ preventScroll: true });
            }
        }

        function feedback(section, message = "", error = false) {
            const element = get(section === "saved" ? "lmrSavedFeedback" : "lmrProposalFeedback");
            element.textContent = message;
            element.hidden = !message;
            element.classList.toggle("lmr-error", error);
        }

        function updateSummary() {
            const count = state.counts.filter(x => x.softwareId === state.softwareId)
                .reduce((sum, x) => sum + Number(x.count), 0);
            get("lmrSavedCount").textContent = format.format(count);
            const stats = state.proposal.loaded ? state.proposal.summary : null;
            get("lmrProposalStats").hidden = !stats;
            if (stats) {
                get("lmrPendingCount").textContent = format.format(
                    (stats.pendingCustomers || 0) + (stats.pendingDocuments || 0) + (stats.pendingUsers || 0));
                get("lmrCustomerConflicts").textContent = format.format(stats.customerConflicts || 0);
                get("lmrBlockedCount").textContent = format.format(
                    (stats.blockedDocuments || 0) + (stats.ambiguous || 0));
                get("lmrUnmatchedCount").textContent = format.format(stats.unmatched || 0);
            }
            const supported = state.autoIds.includes(state.softwareId);
            get("lmrCompute").disabled = !supported || state.proposal.calculating;
            get("lmrComputeLabel").textContent = state.proposal.loaded
                ? "محاسبه مجدد پیشنهادها" : "محاسبه پیشنهادها";
        }

        function fillTypes(which) {
            const select = get(which === "saved" ? "lmrSavedType" : "lmrProposalType");
            const current = state[which].type;
            const values = which === "saved"
                ? new Set(state.counts.filter(x => x.softwareId === state.softwareId)
                    .map(x => Number(x.mapType)))
                : new Set([2, 3, 5, 6]);
            if (which === "proposal" && state.proposal.loaded)
                state.proposal.rows.forEach(x => values.add(Number(x.mapType)));
            select.replaceChildren(new Option("همه انواع", ""));
            state.types.filter(x => values.has(Number(x.value)))
                .forEach(x => select.add(new Option(x.title, x.value)));
            state[which].type = current && values.has(Number(current)) ? current : "";
            select.value = state[which].type;
        }

        function updateProposalTabs() {
            proposalTabs.querySelectorAll("[data-status]").forEach(button => {
                const active = button.dataset.status === state.proposal.status;
                button.classList.toggle("is-active", active);
                button.setAttribute("aria-pressed", String(active));
            });
            get("lmrSelectionActions").hidden = !state.proposal.loaded ||
                state.proposal.status === "blocked" ||
                !state.manualIds.includes(state.softwareId);
            const showReason = state.proposal.status !== "pending";
            get("lmrProposalReasonBox").hidden = !showReason;
            proposalReason.disabled = !showReason || !state.proposal.loaded;
            updateReasonOptions();
        }

        function updateReasonOptions() {
            const p = state.proposal;
            const codes = [...new Set(p.rows
                .filter(row => row.status === p.status &&
                    (!p.type || Number(row.mapType) === Number(p.type)))
                .map(row => row.reason))];
            codes.sort((a, b) => reason({reason:a})[0].localeCompare(reason({reason:b})[0], "fa"));
            proposalReason.replaceChildren(new Option("همه علت‌ها", ""));
            codes.forEach(code => proposalReason.add(new Option(reason({reason:code})[0], code)));
            if (!codes.includes(p.reason) || p.status === "pending") p.reason = "";
            proposalReason.value = p.reason;
        }

        function switchMainTab(which) {
            if (which !== "saved" && which !== "proposal") return;
            state.activeTab = which;
            get("lmrSavedPanel").hidden = which !== "saved";
            get("lmrProposalPanel").hidden = which !== "proposal";
            mainTabs.querySelectorAll("[data-main-tab]").forEach(button => {
                const active = button.dataset.mainTab === which;
                button.classList.toggle("is-active", active);
                button.setAttribute("aria-selected", String(active));
                button.tabIndex = active ? 0 : -1;
            });
        }

        function drawPages(which, total) {
            const model = state[which];
            const prefix = which === "saved" ? "lmrSaved" : "lmrProposal";
            const pages = get(prefix + "Pagination");
            pages.replaceChildren();
            if (model.size === 0) {
                model.page = 1;
                get(prefix + "PageInfo").textContent =
                    "نمایش همه " + format.format(total) + " مورد";
                return;
            }
            const limit = Math.max(1, Math.ceil(total / model.size));
            model.page = Math.min(Math.max(1, model.page), limit);

            const add = (label, index, disabled) => {
                const button = document.createElement("button");
                button.type = "button";
                button.textContent = label;
                button.dataset.page = String(index);
                button.disabled = disabled;
                if (index === model.page && !disabled) button.classList.add("current");
                pages.append(button);
            };
            add("اول", 1, model.page === 1);
            add("قبلی", model.page - 1, model.page === 1);
            for (let i = Math.max(1, model.page - 2); i <= Math.min(limit, model.page + 2); i++)
                add(format.format(i), i, false);
            add("بعدی", model.page + 1, model.page === limit);
            add("آخر", limit, model.page === limit);
            const first = total ? (model.page - 1) * model.size + 1 : 0;
            get(prefix + "PageInfo").textContent = format.format(first) + " تا " +
                format.format(Math.min(total, model.page * model.size)) +
                " از " + format.format(total);
        }

        function updateSelection() {
            const rows = state.proposal.visible;
            const available = rows.filter(x => canMap(x) && verifiedTarget(x));
            get("lmrSelectedCount").textContent = format.format(state.selected.size) + " انتخاب";
            saveSelected.disabled = state.processing || state.selected.size === 0;
            selectAll.disabled = state.processing || available.length === 0;
            selectAll.checked = available.length > 0 &&
                available.every(x => state.selected.has(x.key));
            selectAll.indeterminate = !selectAll.checked &&
                available.some(x => state.selected.has(x.key));
        }

        function renderSaved(rows, total) {
            savedRoot.innerHTML = rows.length ? rows.map(card).join("") :
                '<div class="lmr-empty">نگاشت ثبت‌شده‌ای با این فیلتر پیدا نشد.</div>';
            feedback("saved");
            drawPages("saved", total);
        }

        function renderProposals() {
            const model = state.proposal;
            if (!model.loaded) {
                proposalRoot.replaceChildren();
                get("lmrProposalPageInfo").textContent = "—";
                get("lmrProposalPagination").replaceChildren();
                get("lmrSelectionActions").hidden = true;
                if (!state.autoIds.includes(state.softwareId))
                    feedback("proposal", "برای این نرم‌افزار هنوز الگوریتم تطبیق خودکار تعریف نشده است. نگاشت‌های ثبت‌شده در بخش بالا قابل مشاهده‌اند.");
                else
                    feedback("proposal", "برای دریافت پیشنهادهای جدید، دکمه «محاسبه پیشنهادها» را انتخاب کنید.");
                return;
            }
            const keyword = model.search.trim().toLocaleLowerCase("fa");
            const filtered = model.rows.filter(row =>
                row.status === model.status &&
                (!model.type || Number(row.mapType) === Number(model.type)) &&
                (model.status === "pending" || !model.reason || row.reason === model.reason) &&
                (!keyword || [row.crm?.customer, row.crm?.number, row.crm?.id,
                    ...(row.crmCandidates || []).flatMap(item => [item.name, item.code]),
                    row.sepidar?.customer, row.sepidar?.number,
                    row.details, ...(row.sources || [])]
                    .join(" ").toLocaleLowerCase("fa").includes(keyword)));
            model.page = model.size === 0 ? 1 :
                Math.min(model.page, Math.max(1, Math.ceil(filtered.length / model.size)));
            const start = (model.page - 1) * model.size;
            model.visible = model.size === 0 ? filtered :
                filtered.slice(start, start + model.size);
            proposalRoot.innerHTML = model.visible.length
                ? model.visible.map(card).join("")
                : '<div class="lmr-empty">موردی مطابق این فیلتر پیدا نشد.</div>';
            feedback("proposal");
            drawPages("proposal", filtered.length);
            updateProposalTabs();
            updateSelection();
        }

        async function loadSaved() {
            const model = state.saved;
            if (!state.softwareId) return;
            const request = ++model.request;
            const softwareId = state.softwareId;
            savedRoot.replaceChildren();
            feedback("saved", "در حال خواندن نگاشت‌های ثبت‌شده…");
            const result = await sendApiRequest({
                url: "/ExternalMapping/GetSavedExternalMappings",
                params: { softwareId, mapType: model.type || undefined, search: model.search,
                    page: model.page, pageSize: model.size },
                showLoading: false, timeout: 90000
            });
            if (!isActive() || request !== model.request || softwareId !== state.softwareId) return;
            if (!result) {
                feedback("saved", "دریافت نگاشت‌های ثبت‌شده ناموفق بود.", true);
                return;
            }
            model.rows = result.rows || [];
            model.total = Number(result.total || 0);
            const max = model.size === 0 ? 1 : Math.max(1, Math.ceil(model.total / model.size));
            if (model.page > max) {
                model.page = max;
                await loadSaved();
                return;
            }
            renderSaved(model.rows, model.total);
        }

        async function deleteSavedMapping(row) {
            if (state.deleting || !row?.mappingId || row.status !== "saved") return;

            const ok = await confirmAlert({
                title: "حذف نگاشت",
                text: "نگاشت «" + typeTitle(row) + "» بین CRM و «" +
                    state.softwareTitle + "» حذف شود؟ اطلاعات اصلی در CRM و نرم‌افزار مالی حذف نمی‌شوند.",
                icon: "warning", confirmText: "حذف نگاشت", cancelText: "انصراف"
            });
            if (!ok) return;

            const softwareId = state.softwareId;
            state.deleting = true;
            const savedCard = [...savedRoot.querySelectorAll("[data-key]")]
                .find(x => x.dataset.key === row.key);
            const deleteButton = savedCard?.querySelector("[data-delete-mapping]");
            if (deleteButton) deleteButton.disabled = true;

            const result = await sendApiRequest({
                url: "/ExternalMapping/DeleteExternalMapping",
                method: "POST",
                headers: { "RequestVerificationToken": token },
                data: { softwareId, mappingId: row.mappingId },
                timeout: 60000,
                successMsg: null
            });
            state.deleting = false;
            if (!isActive() || softwareId !== state.softwareId) return;

            if (!result?.deleted) {
                if (deleteButton) deleteButton.disabled = false;
                return;
            }

            toastifySuccess("نگاشت انتخاب‌شده حذف شد.");
            // Any calculated proposal may be outdated after deleting a saved mapping.
            state.proposal.loaded = false;
            state.proposal.rows = [];
            state.proposal.visible = [];
            state.proposal.summary = null;
            state.selected.clear();
            state.targets.clear();
            state.crmTargets.clear();
            state.preview.clear();
            updateProposalTabs();
            updateSummary();
            renderProposals();

            const options = await sendApiRequest({
                url: "/ExternalMapping/GetExternalMappingWorkspaceOptions",
                showLoading: false
            });
            if (!isActive() || softwareId !== state.softwareId) return;
            if (options) {
                state.counts = options.counts || [];
                fillTypes("saved");
                updateSummary();
            }
            await loadSaved();
        }

        async function calculate() {
            const model = state.proposal;
            if (model.calculating || !state.autoIds.includes(state.softwareId)) return;
            const softwareId = state.softwareId;
            const request = ++model.request;
            model.calculating = true;
            updateSummary();
            proposalRoot.replaceChildren();
            feedback("proposal", "در حال محاسبه تطبیق‌های " + state.softwareTitle + "…");
            const result = await sendApiRequest({
                url: "/ExternalMapping/GetExternalMappingReview",
                params: { softwareId },
                showLoading: false, timeout: 180000
            });
            if (!isActive() || softwareId !== state.softwareId || request !== model.request) return;
            model.calculating = false;
            if (!result) {
                updateSummary();
                feedback("proposal", "محاسبه پیشنهادها ناموفق بود.", true);
                return;
            }
            model.rows = consolidateDuplicateParties(result.rows || []);
            model.summary = result.summary || {};
            model.loaded = true;
            model.page = 1;
            state.selected.clear();
            state.targets.clear();
            state.preview.clear();
            state.crmTargets.clear();
            fillTypes("proposal");
            updateSummary();
            renderProposals();
        }

        async function changeSoftware() {
            state.softwareId = softwareSelect.value;
            state.softwareTitle = softwareSelect.selectedOptions[0]?.textContent || "نرم‌افزار خارجی";
            state.saved = { search: "", type: "", page: 1, size: 10, total: 0, rows: [], request: state.saved.request + 1 };
            state.proposal = { search: "", type: "", reason: "", status: "pending", page: 1, size: 10,
                loaded: false, rows: [], visible: [], summary: null, request: state.proposal.request + 1, calculating: false };
            state.selected.clear();
            state.targets.clear();
            state.preview.clear();
            state.crmTargets.clear();
            get("lmrSavedSearch").value = "";
            get("lmrProposalSearch").value = "";
            get("lmrSavedSize").value = "10";
            get("lmrProposalSize").value = "10";
            fillTypes("saved");
            fillTypes("proposal");
            updateSummary();
            updateProposalTabs();
            renderProposals();
            await loadSaved();
        }

        function payload(row) {
            if (!canMap(row) || !verifiedTarget(row)) return null;
            return { entityId: selectedCrm(row).id, mapType: Number(row.mapType),
                exEntityId: targetValue(row) };
        }

        async function save(items) {
            if (state.processing || !items.length) return;

            // The server accepts at most 100 mappings per request.
            // Save customers first, then quotations and invoices, in separate batches.
            const ordered = [
                ...[5, 6, 4, 0, 1, 2, 3].flatMap(type => items.filter(x => x.mapType === type))
            ];
            if (ordered.length !== items.length) {
                toastifyError("نوع بعضی از نگاشت‌های انتخاب‌شده قابل ثبت نیست.");
                return;
            }

            const agreed = await confirmAlert({
                title: "تأیید نگاشت",
                text: "ثبت " + format.format(items.length) + " نگاشت در «" +
                    state.softwareTitle + "» انجام شود؟ " +
                    (items.length > 100 ? "نگاشت‌ها به دسته‌های حداکثر ۱۰۰تایی ثبت می‌شوند. " : "") +
                    "نگاشت‌ها پس از اعتبارسنجی رابطه دو طرف در CRM ثبت می‌شوند.",
                icon: "question", confirmText: "ثبت نگاشت", cancelText: "انصراف"
            });
            if (!agreed) return;

            const softwareId = state.softwareId;
            state.processing = true;
            softwareSelect.disabled = true;
            mainTabs.querySelectorAll("[data-main-tab]").forEach(button => button.disabled = true);
            get("lmrCompute").disabled = true;
            selectAll.disabled = true;
            updateSelection();

            let savedCount = 0;
            let failed = false;
            try {
                for (const groupType of [5, 6, 4, 0, 1, 2, 3]) {
                    const group = ordered.filter(item => item.mapType === groupType);
                    for (let i = 0; i < group.length; i += 100) {
                        const batch = group.slice(i, i + 100);
                        feedback("proposal", "در حال ثبت نگاشت‌ها: " +
                            format.format(savedCount) + " از " + format.format(items.length));
                        const result = await sendApiRequest({
                            url: "/ExternalMapping/SaveSelectedExternalMappings",
                            method: "POST",
                            headers: { "RequestVerificationToken": token },
                            data: { externalSoftwareId: softwareId, items: batch },
                            timeout: 120000, successMsg: null
                        });
                        if (result?.count !== batch.length) {
                            failed = true;
                            break;
                        }
                        savedCount += result.count;
                        for (const item of batch) {
                            for (const [key, selected] of state.selected) {
                                if (selected.entityId === item.entityId &&
                                    selected.mapType === item.mapType &&
                                    selected.exEntityId === item.exEntityId)
                                    state.selected.delete(key);
                            }
                        }
                    }
                    if (failed) break;
                }
            } catch (error) {
                failed = true;
                // Earlier batches may already have been committed. Report their count.
            } finally {
                state.processing = false;
                softwareSelect.disabled = false;
                mainTabs.querySelectorAll("[data-main-tab]").forEach(button => button.disabled = false);
                updateSummary();
                updateSelection();
            }

            if (savedCount > 0) {
                toastifySuccess(format.format(savedCount) + " نگاشت ثبت شد.");
                state.selected.clear();
                state.targets.clear();
                state.preview.clear();
                state.crmTargets.clear();
                state.proposal.loaded = false;
                state.proposal.rows = [];
                state.proposal.summary = null;
                state.proposal.visible = [];
                updateProposalTabs();
                renderProposals();
                const options = await sendApiRequest({
                    url: "/ExternalMapping/GetExternalMappingWorkspaceOptions", showLoading: false
                });
                if (options) state.counts = options.counts || [];
                fillTypes("saved");
                updateSummary();
                await loadSaved();
            }

            if (failed) {
                const remaining = items.length - savedCount;
                feedback("proposal", "ثبت گروهی کامل نشد. " +
                    format.format(savedCount) + " مورد ثبت شده و " +
                    format.format(remaining) + " مورد باقی مانده است. " +
                    "پیشنهادها را دوباره محاسبه کنید و موارد باقی‌مانده را بررسی کنید.", true);
            } else if (savedCount > 0) {
                feedback("proposal", "نگاشت‌های انتخاب‌شده ثبت شدند. " +
                    "برای دریافت پیشنهادهای به‌روز، محاسبه پیشنهادها را مجدداً اجرا کنید.");
            }
            return !failed && savedCount === items.length;
        }

        async function preview(row, id) {
            if (!validId(id)) return;
            const key = state.softwareId + ":" + row.mapType + ":" + id;
            if (state.preview.has(key)) return;
            const softwareId = state.softwareId;
            const result = await sendApiRequest({
                url: "/ExternalMapping/GetExternalMappingTargetDetails",
                params: { softwareId, mapType: row.mapType, targetId: Number(id) },
                showLoading: false, showError: false, timeout: 30000
            });
            if (!isActive() || softwareId !== state.softwareId || state.targets.get(row.key) !== id) return;
            if (result?.target) state.preview.set(key, result.target);
            else toastifyError("معادل انتخاب‌شده در نرم‌افزار مقصد پیدا نشد.");
            refreshProposalCard(row);
            updateSelection();
        }

        function toggleDetails(root, event) {
            const button = event.target.closest("[data-expand]");
            if (!button || !root.contains(button)) return false;
            const panel = button.closest("[data-key]")?.querySelector(".lmr-expanded");
            if (!panel) return false;
            panel.hidden = !panel.hidden;
            button.setAttribute("aria-expanded", String(!panel.hidden));
            return true;
        }

        savedRoot.addEventListener("click", async event => {
            if (toggleDetails(savedRoot, event)) return;
            if (!event.target.closest("[data-delete-mapping]")) return;
            const savedCard = event.target.closest("[data-key]");
            const row = state.saved.rows.find(item => item.key === savedCard?.dataset.key);
            if (row) await deleteSavedMapping(row);
        });
        proposalRoot.addEventListener("click", async event => {
            if (toggleDetails(proposalRoot, event)) return;
            const panel = event.target.closest("[data-key]");
            const row = state.proposal.visible.find(x => x.key === panel?.dataset.key);
            if (!row) return;
            if (event.target.closest("[data-open-manual]")) {
                openManualMapping(Number(row.mapType));
                return;
            }
            if (event.target.closest("[data-map-one]")) {
                const item = payload(row);
                if (item) await save([item]);
            }
        });

        proposalRoot.addEventListener("change", event => {
            if (state.processing) return;
            const panel = event.target.closest("[data-key]");
            const row = state.proposal.visible.find(item => item.key === panel?.dataset.key);
            if (!row) return;
            if (event.target.matches("[data-crm-target]")) {
                state.crmTargets.set(row.key, event.target.value);
                state.selected.delete(row.key);
                refreshProposalCard(row);
                updateSelection();
            } else if (event.target.matches("[data-map-target]")) {
                const id = event.target.value.trim();
                state.targets.set(row.key, id);
                state.selected.delete(row.key);
                refreshProposalCard(row);
                updateSelection();
                // Only a verified external record may be submitted. Do not bypass
                // the existing server-side check for conflicts and ambiguous matches.
                if (validId(id)) void preview(row, id);
            } else if (event.target.matches("[data-select-map]")) {
                const item = payload(row);
                if (event.target.checked && item) state.selected.set(row.key, item);
                else state.selected.delete(row.key);
                updateSelection();
            }
        });

        selectAll.addEventListener("change", () => {
            if (state.processing) return;
            state.proposal.visible.forEach(row => {
                const item = payload(row);
                if (!item) return;
                if (selectAll.checked) state.selected.set(row.key, item);
                else state.selected.delete(row.key);
            });
            proposalRoot.querySelectorAll("[data-key]").forEach(panel => {
                const check = panel.querySelector("[data-select-map]");
                if (check) check.checked = state.selected.has(panel.dataset.key);
            });
            updateSelection();
        });
        saveSelected.addEventListener("click", () => save([...state.selected.values()]));

        proposalTabs.addEventListener("click", event => {
            const button = event.target.closest("[data-status]");
            if (!button) return;
            state.proposal.status = button.dataset.status;
            state.proposal.page = 1;
            state.proposal.reason = "";
            renderProposals();
        });

        proposalReason.addEventListener("change", () => {
            state.proposal.reason = proposalReason.value;
            state.proposal.page = 1;
            renderProposals();
        });

        mainTabs.addEventListener("click", event => {
            const button = event.target.closest("[data-main-tab]");
            if (button) switchMainTab(button.dataset.mainTab);
        });

        function changeFilter(which, field, value) {
            state[which][field] = value;
            state[which].page = 1;
            if (which === "saved") loadSaved();
            else {
                state.proposal.reason = "";
                renderProposals();
            }
        }
        get("lmrSavedType").addEventListener("change", e =>
            changeFilter("saved", "type", e.target.value));
        get("lmrProposalType").addEventListener("change", e =>
            changeFilter("proposal", "type", e.target.value));
        get("lmrSavedSearch").addEventListener("input", debounce(e =>
            changeFilter("saved", "search", e.target.value.trim()), 350));
        get("lmrProposalSearch").addEventListener("input", debounce(e =>
            changeFilter("proposal", "search", e.target.value.trim()), 250));
        get("lmrSavedSize").addEventListener("change", e =>
            changeFilter("saved", "size", Number(e.target.value)));
        get("lmrProposalSize").addEventListener("change", e =>
            changeFilter("proposal", "size", Number(e.target.value)));

        for (const which of ["saved", "proposal"]) {
            const container = get((which === "saved" ? "lmrSaved" : "lmrProposal") + "Pagination");
            container.addEventListener("click", event => {
                const button = event.target.closest("[data-page]");
                if (!button || button.disabled) return;
                state[which].page = Number(button.dataset.page);
                if (which === "saved") loadSaved();
                else renderProposals();
            });
        }

        async function refreshSaved() {
            const softwareId = state.softwareId;
            const options = await sendApiRequest({
                url: "/ExternalMapping/GetExternalMappingWorkspaceOptions", showLoading: false
            });
            if (!isActive() || softwareId !== state.softwareId) return;
            if (options) {
                state.counts = options.counts || [];
                fillTypes("saved");
                updateSummary();
            }
            await loadSaved();
        }


        // Manual mappings use two independent, server-backed TomSelect instances.
        // Every search is constrained by the selected software, entity and map type.
        const manualModal = get("lmrManualModal");
        const manualType = get("lmrManualType");
        const manualCrmSelect = get("lmrManualCrmSelect");
        const manualExternalSelect = get("lmrManualExternalSelect");
        const manualSelects = { crm: null, external: null };
        const manualEntityTypes = {
            0: { crm: ["Cash", "تنخواه‌های CRM"], external: ["PaymentHeader", "اعلامیه‌های پرداخت"] },
            1: { crm: ["Accountancy", "اسناد مالی CRM"], external: ["Voucher", "اسناد حسابداری"] },
            2: { crm: ["PreFactor", "پیش‌فاکتورهای CRM"], external: ["Quotation", "پیش‌فاکتورهای مالی"] },
            3: { crm: ["Factor", "فاکتورهای CRM"], external: ["Invoice", "فاکتورهای مالی"] },
            4: { crm: ["BaseCenterCash", "مراکز هزینه CRM"], external: ["DL", "حساب‌های تفصیلی"] },
            5: { crm: ["Customer", "مشتریان CRM"], external: ["Party", "اشخاص مالی"] },
            6: { crm: ["AppUser", "کارشناسان CRM"], external: ["Party", "کارشناسان در اشخاص مالی"] }
        };

        function manualTypeDetails() {
            const type = manualType.value === "" ? null : Number(manualType.value);
            get("lmrManualSourceKindBox").hidden = type !== 1;
            get("lmrManualTargetKindBox").hidden = type !== 0;
            return {
                mapType: type,
                sourceEntity: type === 1 ? get("lmrManualSourceKind").value : "",
                targetEntity: type === 0 ? get("lmrManualTargetKind").value : ""
            };
        }

        function manualEntityInfo(side) {
            const type = manualTypeDetails();
            const item = manualEntityTypes[type.mapType]?.[side];
            if (!item) return null;
            if (side === "crm" && type.sourceEntity === "Cash")
                return ["Cash", "تنخواه‌های CRM"];
            if (side === "external" && type.targetEntity === "SepidarCash")
                return ["SepidarCash", "تنخواه‌های سپیدار"];
            return item;
        }

        function updateManualEntityLabels() {
            for (const side of ["crm", "external"]) {
                const el = get(side === "crm" ? "lmrManualCrmEntity" : "lmrManualExternalEntity");
                const info = manualEntityInfo(side);
                el.textContent = info ? info[1] + " (" + info[0] + ")" : "ابتدا نوع نگاشت را انتخاب کنید";
            }
        }

        function manualFeedback(message = "", error = false) {
            const element = get("lmrManualFeedback");
            element.textContent = message;
            element.classList.toggle("lmr-error", error);
        }

        function manualValue(side) {
            return String(manualSelects[side]?.getValue() || "");
        }

        function manualSubmitState() {
            const validType = manualType.value !== "" &&
                Object.hasOwn(manualEntityTypes, Number(manualType.value));
            get("lmrManualConfirm").disabled = !state.manualOpen || state.processing ||
                !validType ||
                !/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(manualValue("crm")) ||
                !validId(manualValue("external"));
        }

        function destroyManualSelects() {
            for (const side of ["crm", "external"]) {
                state.manualRequests[side]++;
                manualSelects[side]?.destroy();
                manualSelects[side] = null;
            }
        }

        function initManualSide(side) {
            const element = side === "crm" ? manualCrmSelect : manualExternalSelect;
            const info = manualEntityInfo(side);
            if (!info) {
                element.replaceChildren(new Option("ابتدا نوع نگاشت را انتخاب کنید", ""));
                element.disabled = true;
                return;
            }
            element.disabled = false;
            element.replaceChildren(new Option("", ""));
            state.manualRequests[side]++;
            const softwareId = state.softwareId;
            const type = manualTypeDetails();
            const entityInfo = info[1] + " (" + info[0] + ")";
            const isCrm = side === "crm";
            const instance = new TomSelect(element, {
                valueField: "id",
                labelField: "text",
                searchField: [],
                sortField: [],
                maxItems: 1,
                maxOptions: 40,
                create: false,
                preload: false,
                dropdownParent: "#lmrManualModal",
                loadThrottle: 320,
                placeholder: "نام، شماره یا کد را جستجو کنید",
                shouldLoad(query) {
                    return query.trim().length >= 2;
                },
                // Old remote results must not be mixed with the next search term.
                onType() {
                    this.clearOptions();
                    state.manualRequests[side]++;
                },
                render: {
                    option(data, escapeOption) {
                        return '<div class="lmr-remote-option">' +
                            '<strong dir="auto">' + escapeOption(data.text || "") + '</strong>' +
                            (data.detail ? '<small>' + escapeOption(data.detail) + '</small>' : "") +
                            '<span class="lmr-option-entity">' + escapeOption(entityInfo) + '</span>' +
                            '</div>';
                    },
                    item(data, escapeOption) {
                        return '<div class="lmr-remote-selected" dir="auto">' +
                            escapeOption(data.text || "") + '</div>';
                    },
                    loading() { return '<div class="lmr-remote-note">در حال جستجو…</div>'; },
                    no_results() { return '<div class="lmr-remote-note">موردی پیدا نشد</div>'; },
                    not_loading() { return '<div class="lmr-remote-note">حداقل دو حرف یا رقم بنویسید</div>'; }
                },
                async load(query, callback) {
                    const request = ++state.manualRequests[side];
                    if (!state.manualOpen || !isActive() || manualType.value === "" ||
                        query.trim().length < 2) {
                        callback();
                        return;
                    }
                    try {
                        const result = await sendApiRequest({
                            url: "/ExternalMapping/GetManualMappingOptions",
                            params: {
                                softwareId, ...type, side,
                                search: query.trim()
                            },
                            showLoading: false,
                            showError: false,
                            timeout: 30000
                        });
                        if (!isActive() || !state.manualOpen || softwareId !== state.softwareId ||
                            request !== state.manualRequests[side] ||
                            manualSelects[side] !== instance) {
                            callback();
                            return;
                        }
                        if (!result?.options) {
                            manualFeedback("جستجوی " + (isCrm ? "CRM" : state.softwareTitle) +
                                " ناموفق بود؛ دوباره تلاش کنید.", true);
                            callback();
                            return;
                        }
                        manualFeedback();
                        callback(result.options.map(option => ({
                            id: String(option.id),
                            text: option.title || String(option.id),
                            detail: option.detail || ""
                        })));
                    } catch {
                        if (state.manualOpen && request === state.manualRequests[side]) {
                            manualFeedback("دریافت نتایج جستجو انجام نشد.", true);
                        }
                        callback();
                    }
                },
                onChange() { manualSubmitState(); }
            });
            manualSelects[side] = instance;
        }

        function resetManualOptions() {
            destroyManualSelects();
            updateManualEntityLabels();
            manualFeedback();
            initManualSide("crm");
            initManualSide("external");
            manualSubmitState();
        }

        function closeManualMapping() {
            if (!state.manualOpen || state.processing) return;
            bootstrap.Modal.getOrCreateInstance(manualModal).hide();
        }

        function openManualMapping() {
            if (state.processing || !state.manualIds.includes(state.softwareId)) {
                toastifyError("ثبت دستی برای این نرم‌افزار پشتیبانی نمی‌شود.");
                return;
            }
            manualType.replaceChildren(new Option("نوع نگاشت را انتخاب کنید", ""));
            state.types.forEach(item =>
                manualType.add(new Option(item.title, String(item.value))));
            manualType.value = "";
            get("lmrManualSourceKind").value = "Accountancy";
            get("lmrManualTargetKind").value = "PaymentHeader";
            get("lmrManualExternalTitle").textContent = "معادل در " + state.softwareTitle;
            state.manualOpen = true;
            resetManualOptions();
            bootstrap.Modal.getOrCreateInstance(manualModal).show();
        }

        get("lmrManualOpen").addEventListener("click", openManualMapping);
        get("lmrManualClose").addEventListener("click", closeManualMapping);
        get("lmrManualCancel").addEventListener("click", closeManualMapping);
        manualModal.addEventListener("shown.bs.modal", () => {
            if (state.manualOpen) manualType.focus({ preventScroll: true });
        });
        manualModal.addEventListener("hide.bs.modal", event => {
            if (state.processing) event.preventDefault();
        });
        manualModal.addEventListener("hidden.bs.modal", () => {
            state.manualOpen = false;
            destroyManualSelects();
            if (isActive()) get("lmrManualOpen").focus({ preventScroll: true });
        });
        manualType.addEventListener("change", resetManualOptions);
        get("lmrManualSourceKind").addEventListener("change", resetManualOptions);
        get("lmrManualTargetKind").addEventListener("change", resetManualOptions);

        get("lmrManualConfirm").addEventListener("click", async () => {
            if (state.processing || !state.manualOpen ||
                get("lmrManualConfirm").disabled) return;
            const entityId = manualValue("crm");
            const exEntityId = manualValue("external");
            const values = manualTypeDetails();
            const item = {
                entityId, mapType: values.mapType, exEntityId,
                entityName: values.sourceEntity || null,
                exEntityName: values.targetEntity || null
            };
            const success = await save([item]);
            if (success) closeManualMapping();
            else if (state.manualOpen)
                manualFeedback("نگاشت ثبت نشد؛ خطای ثبت را بررسی کنید.", true);
            manualSubmitState();
        });

        softwareSelect.addEventListener("change", changeSoftware);
        get("lmrRefreshSaved").addEventListener("click", refreshSaved);
        get("lmrCompute").addEventListener("click", calculate);

        (async () => {
            feedback("saved", "در حال دریافت نرم‌افزارهای خارجی…");
            const response = await sendApiRequest({
                url: "/ExternalMapping/GetExternalMappingWorkspaceOptions", showLoading: false
            });
            if (!isActive()) return;
            if (!response) {
                feedback("saved", "فهرست نرم‌افزارها دریافت نشد.", true);
                return;
            }
            state.types = response.mapTypes || [];
            state.counts = response.counts || [];
            state.autoIds = response.supportedAutomaticSoftwareIds || [];
            state.manualIds = response.supportedManualSoftwareIds || [];
            softwareSelect.replaceChildren();
            (response.software || []).forEach(x =>
                softwareSelect.add(new Option(x.title, x.id)));
            if (!softwareSelect.value) {
                feedback("saved", "نرم‌افزار خارجی ثبت نشده است.");
                return;
            }
            if (typeof enableMomentumScrollScope === "function")
                enableMomentumScrollScope(page);
            switchMainTab("saved");
            await changeSoftware();
        })();
    }

    if (document.readyState === "loading")
        document.addEventListener("DOMContentLoaded", initializeMappingManager, { once: true });
    else initializeMappingManager();
})();
