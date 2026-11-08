(() => {
    "use strict";

    function initializeMappingManager() {
        const page = document.getElementById("legacyMappingReport");
        if (!page || page.dataset.initialized) return;
        page.dataset.initialized = "1";

        const get = id => page.querySelector("#" + id);
        const records = get("lmrRecords");
        const info = get("lmrFeedback");
        const softwareSelect = get("lmrSoftware");
        const typeSelect = get("lmrType");
        const reasonSelect = get("lmrReason");
        const searchInput = get("lmrSearch");
        const pageSizeSelect = get("lmrPageSize");
        const statusButtons = get("lmrStatusTabs");
        const selectBar = get("lmrSelectionActions");
        const selectAll = get("lmrSelectPage");
        const saveSelected = get("lmrSaveSelected");
        const token = page.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const format = new Intl.NumberFormat("fa-IR");
        const escape = value => escapeHtml(String(value ?? ""));
        const show = value => value === null || value === undefined || value === "" ? "—" : String(value);
        const money = value => value == null ? "—" : Number(value).toLocaleString("en-US");
        const validId = value => /^[1-9]\d*$/.test(String(value || ""));
        const isActive = () => page.isConnected &&
            !(typeof PageRuntime !== "undefined" && PageRuntime.current?.signal?.aborted);

        const statusNames = {
            saved:"ثبت‌شده", pending:"پیشنهادی", customerConflict:"تعارض مشتری",
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
            ReadyForReview:["معادل پیشنهاد شده است","این رابطه فقط از روی مقایسه محاسبه شده و هنوز ذخیره نشده است."],
            AlreadyMapped:["نگاشت ذخیره‌شده","این رابطه قبلاً در جدول ExternalMapping ثبت شده است."]
        };

        const state = {
            softwareId:"", softwareTitle:"", automaticId:"", manualIds:[],
            types:[], counts:[], status:"saved", type:"", reason:"", search:"",
            page:1, size:10, rows:[], saved:[], savedTotal:0, suggestionsLoaded:false,
            summary:null, selected:new Map(), targets:new Map(), preview:new Map(),
            currentRows:[], requestId:0, processing:false, calculating:false
        };

        function reason(row) {
            return reasonNames[row.reason] || [row.reason || "نیازمند بررسی",
                "اطلاعات مشتری، شماره، تاریخ و مبلغ هر دو طرف را بررسی کنید."];
        }

        function typeTitle(row) {
            return state.types.find(t => Number(t.value) === Number(row.mapType))?.title ||
                ({ Customer:"مشتری", PreFactor:"پیش‌فاکتور", Factor:"فاکتور",
                    Cash:"تنخواه", BaseCenterCash:"مرکز هزینه", Accountancy:"حسابداری" }[row.kind] || show(row.kind));
        }

        function canMap(row) {
            return row.status !== "saved" && row.status !== "blocked" &&
                [2,3,5].includes(Number(row.mapType)) &&
                state.manualIds.includes(state.softwareId);
        }

        function targetValue(row) {
            if (state.targets.has(row.key)) return state.targets.get(row.key);
            const value = row.status === "pending" && row.options?.length === 1
                ? String(row.options[0].id) : "";
            state.targets.set(row.key,value);
            return value;
        }

        function targetPreview(row) {
            const value = targetValue(row);
            if (row.status === "saved" || row.status === "pending" && !value) return row.sepidar;
            if (!value) return {title:"معادل انتخاب نشده",number:"",customer:"",id:""};
            const preview = state.preview.get(state.softwareId+":"+row.mapType+":"+value);
            if (preview) return preview;
            if (String(row.sepidar?.id) === value) return row.sepidar;
            const option = row.options?.find(x => String(x.id) === value);
            return { title:"معادل انتخابی", id:value, number:value, customer:option?.title || "" };
        }

        function readableExtra(row, side, isCrm) {
            let extra = side?.extra || "";
            if (!extra.trim()) return "";
            if (isCrm && row.mapType === 5) {
                const m = extra.match(/FinancialCustomerId:\s*(\d+)/);
                return m ? "کد مالی قدیمی مشتری: " + m[1] : "";
            }
            if (isCrm && extra.includes("CustomerId:")) {
                const financial = extra.match(/شماره مالی:\s*([^|]+)/);
                return financial && financial[1].trim() && financial[1].trim() !== "null"
                    ? "شماره فاکتور مالی: " + financial[1].trim() : "";
            }
            if (!isCrm && extra.includes("PartyId:")) {
                const m = extra.match(/PartyId:\s*(\d+)/);
                return m ? "کد مشتری مرتبط در نرم‌افزار: "+m[1] : "";
            }
            return extra.replaceAll("FiscalYearRef:","سال مالی:").replaceAll("SepidarId:","کد قبلی سپیدار:");
        }

        function detailSide(row, side, isCrm) {
            const label = isCrm ? "CRM" : state.softwareTitle;
            const customer = row.mapType === 5;
            const rows = [
                [customer ? "کد مشتری" : "شماره", side?.number],
                ["نام", side?.customer || side?.title],
                ...(!customer ? [["مبلغ نهایی", side?.amount == null ? "" : money(side.amount)+" ریال"],
                    ["تاریخ",side?.date]] : []),
                ["توضیح",readableExtra(row,side,isCrm)]
            ].filter(x => x[1] !== undefined && x[1] !== null && x[1] !== "");
            return '<section class="lmr-detail-side"><h4>'+escape(label)+'</h4>'+
                rows.map(x => '<div><span>'+escape(x[0])+'</span><b>'+escape(show(x[1]))+'</b></div>').join("")+
                '<small>شناسه داخلی: '+escape(side?.id || "—")+'</small></section>';
        }

        function inputTarget(row) {
            if (!canMap(row)) return "";
            const value = targetValue(row);
            const options = row.options || [];
            const control = options.length ?
                '<select data-map-target aria-label="معادل تأییدشده">'+
                    '<option value="">معادل صحیح را انتخاب کنید</option>'+
                    options.map(opt => '<option value="'+escape(opt.id)+'" '+
                        (String(opt.id)===value ? "selected" : "")+'>'+escape(opt.title)+
                        ' (کد '+escape(opt.id)+')</option>').join("")+'</select>'
                : '<input type="number" min="1" data-map-target aria-label="شناسه معادل" placeholder="شناسه معادل در نرم‌افزار" value="'+escape(value)+'" />';
            return '<div class="lmr-map-action"><label><span>معادل مورد تأیید</span>'+control+'</label>'+
                '<button type="button" class="lmr-btn lmr-btn-primary" data-map-one '+
                    (!validId(value) ? "disabled" : "")+'>ثبت این نگاشت</button></div>';
        }

        function card(row) {
            const title = reason(row);
            const isCustomer = Number(row.mapType) === 5;
            const left = row.crm || {};
            const right = targetPreview(row) || {};
            const mappable = canMap(row);
            const stateClass = row.status === "saved" ? "saved" :
                row.status === "pending" ? "proposal" : "problem";
            const related = (row.sources || []).filter(Boolean);
            const relatedPreview = row.status === "customerConflict" && related.length
                ? '<p class="lmr-source-preview">نمونه ارتباط: '+escape(related[0])+
                    (related.length>1 ? ' <strong>('+escape(format.format(related.length-1))+' مورد دیگر در توضیحات)</strong>' : '')+
                    '</p>' : '';
            const crmName = left.customer || left.title;
            const extName = right.customer || right.title;
            const leftNum = left.number;
            const rightNum = right.number;
            const leftAmount = left.amount;
            const rightAmount = right.amount;
            const amountDiff = !isCustomer && leftAmount != null && rightAmount != null &&
                Number(leftAmount) !== Number(rightAmount);
            return '<article class="lmr-item lmr-'+stateClass+'" data-key="'+escape(row.key)+'" role="listitem">'+
                '<header class="lmr-item-head">'+
                    (mappable ? '<label class="lmr-item-select"><input type="checkbox" data-select-map '+
                        (state.selected.has(row.key) ? "checked " : "")+
                        (!validId(targetValue(row)) ? "disabled " : "")+'aria-label="انتخاب نگاشت" /></label>' : "")+
                    '<b class="lmr-type">'+escape(typeTitle(row))+'</b>'+
                    '<span class="lmr-state">'+escape(statusNames[row.status] || row.status)+'</span>'+
                    '<span class="lmr-short-reason">'+escape(title[0])+'</span>'+
                    '<button type="button" class="lmr-expand" data-expand aria-expanded="false">توضیحات <i class="fas fa-chevron-down" aria-hidden="true"></i></button>'+
                '</header>'+
                '<div class="lmr-pair">'+
                    '<div class="lmr-pair-side"><span>CRM</span><strong>'+escape(show(crmName))+'</strong>'+
                        '<small>'+(isCustomer ? "کد مشتری " : "شماره ")+escape(show(leftNum))+'</small></div>'+
                    '<span class="lmr-arrows"><i class="fas fa-exchange-alt" aria-hidden="true"></i></span>'+
                    '<div class="lmr-pair-side"><span>'+escape(state.softwareTitle)+'</span><strong>'+escape(show(extName))+'</strong>'+
                        '<small>'+(isCustomer ? "کد مشتری " : "شماره ")+escape(show(rightNum))+'</small></div>'+
                    (amountDiff ? '<span class="lmr-amount-warning">اختلاف مبلغ: '+escape(money(leftAmount))+
                        ' در CRM / '+escape(money(rightAmount))+' در نرم‌افزار</span>' : '')+
                '</div>'+
                '<div class="lmr-one-line">'+escape(title[1])+'</div>'+
                relatedPreview+
                inputTarget(row)+
                '<div class="lmr-expanded" hidden>'+
                    '<div class="lmr-detail-grid">'+detailSide(row,left,true)+detailSide(row,right,false)+'</div>'+
                    (related.length ? '<div class="lmr-related"><b>ارتباط پیش‌فاکتورها، فاکتورها و مشتریان</b>'+
                        '<ul>'+related.map(line => '<li>'+escape(line)+'</li>').join("")+'</ul></div>' : '')+
                    (row.details && row.status !== "saved" ? '<details class="lmr-technical"><summary>شرح فنی برای بررسی دقیق‌تر</summary><p>'+
                        escape(row.details)+'</p></details>' : '')+
                '</div></article>';
        }

        function feedback(message="",error=false) {
            info.hidden=!message;info.textContent=message;
            info.classList.toggle("lmr-error",error);
        }

        function summary() {
            const saved = state.counts.filter(x=>x.softwareId===state.softwareId)
                .reduce((sum,x)=>sum+Number(x.count),0);
            get("lmrSavedCount").textContent=format.format(saved);
            const stats = state.suggestionsLoaded ? state.summary : null;
            get("lmrPendingCount").textContent=stats ?
                format.format((stats.pendingCustomers||0)+(stats.pendingDocuments||0)) : "—";
            get("lmrCustomerConflicts").textContent=stats ? format.format(stats.customerConflicts||0) : "—";
            get("lmrBlockedCount").textContent=stats ?
                format.format((stats.blockedDocuments||0)+(stats.ambiguous||0)) : "—";
            get("lmrSuggestedSource").hidden=state.softwareId!==state.automaticId;
            get("lmrCompute").disabled=state.softwareId!==state.automaticId || state.calculating;
            get("lmrComputeLabel").textContent=state.suggestionsLoaded
                ? "محاسبه مجدد پیشنهادها" : "محاسبه پیشنهادها";
        }

        function types() {
            const active = new Set(state.counts.filter(x=>x.softwareId===state.softwareId)
                .map(x=>Number(x.mapType)));
            if (state.softwareId===state.automaticId) [2,3,5].forEach(id=>active.add(id));
            if (state.suggestionsLoaded)
                state.rows.forEach(row=>active.add(Number(row.mapType)));
            typeSelect.replaceChildren(new Option("همه انواع", ""));
            state.types.filter(x=>active.has(Number(x.value)))
                .forEach(x=>typeSelect.add(new Option(x.title,x.value)));
            if (!state.type || active.has(Number(state.type)))typeSelect.value=state.type;
            else {state.type="";typeSelect.value="";}
        }

        function reasons() {
            const choices = [...new Set(state.rows.filter(x=>x.status===state.status &&
                (!state.type || Number(x.mapType)===Number(state.type))).map(x=>x.reason))];
            choices.sort((a,b)=>reasonNames[a]?.[0]?.localeCompare(reasonNames[b]?.[0]||b,"fa")||0);
            reasonSelect.replaceChildren(new Option("همه علت‌ها",""));
            choices.forEach(x=>reasonSelect.add(new Option(reasonNames[x]?.[0]||x,x)));
            if (!choices.includes(state.reason)) state.reason="";
            reasonSelect.value=state.reason;
            reasonSelect.disabled=state.status==="saved" || !state.suggestionsLoaded;
        }

        function tabs() {
            statusButtons.querySelectorAll("[data-status]").forEach(button=>{
                const active=button.dataset.status===state.status;
                button.classList.toggle("is-active",active);
                button.setAttribute("aria-pressed",active?"true":"false");
            });
            const label={
                saved:"نگاشت‌های ذخیره‌شده",pending:"پیشنهادهای محاسبه‌شده",
                customerConflict:"تعارض مشتریان",ambiguous:"پیش‌فاکتورها و فاکتورهای مبهم",
                blocked:"منتظر تعیین تکلیف مشتری",notMatched:"موارد بدون معادل قطعی"
            }[state.status];
            get("lmrSectionTitle").textContent=label;
            selectBar.hidden=state.status==="saved" || !state.manualIds.includes(state.softwareId);
            reasons();summary();
        }

        function pagination(total) {
            const max=Math.max(1,Math.ceil(total/state.size));
            state.page=Math.min(max,Math.max(1,state.page));
            const p=get("lmrPagination");p.replaceChildren();
            function add(label,numberValue,disabled) {
                const button=document.createElement("button");
                button.type="button";button.textContent=label;
                button.dataset.page=String(numberValue);
                button.disabled=disabled;
                if(numberValue===state.page&&!disabled)button.classList.add("current");
                p.append(button);
            }
            add("قبلی",state.page-1,state.page===1);
            for(let i=Math.max(1,state.page-2);i<=Math.min(max,state.page+2);i++)
                add(format.format(i),i,false);
            add("بعدی",state.page+1,state.page===max);
            const first=total?(state.page-1)*state.size+1:0;
            get("lmrPageInfo").textContent=format.format(first)+" تا "+
                format.format(Math.min(total,state.page*state.size))+" از "+format.format(total);
            get("lmrResultCount").textContent=format.format(total)+" مورد";
        }

        function selection() {
            get("lmrSelectedCount").textContent=format.format(state.selected.size)+" مورد انتخاب‌شده";
            saveSelected.disabled=!state.selected.size||state.processing;
            const selectable=state.currentRows.filter(x=>canMap(x)&&validId(targetValue(x)));
            selectAll.disabled=!selectable.length||state.processing;
            selectAll.checked=selectable.length>0&&selectable.every(x=>state.selected.has(x.key));
            selectAll.indeterminate=!selectAll.checked&&selectable.some(x=>state.selected.has(x.key));
        }

        function render(items,total) {
            state.currentRows=items;
            records.innerHTML=items.length?items.map(card).join(""):
                '<div class="lmr-empty">در این فیلتر موردی وجود ندارد.</div>';
            feedback();
            pagination(total);selection();
        }

        function localRows() {
            if (!state.suggestionsLoaded) {
                records.replaceChildren();
                get("lmrResultCount").textContent="—";
                feedback(state.softwareId===state.automaticId ?
                    "پیشنهادها در CRM ذخیره نمی‌شوند. برای ساخت فهرست تازه، «محاسبه دوباره پیشنهادها» را بزنید." :
                    "برای این نرم‌افزار، محاسبه خودکار پیشنهادها فعلاً پشتیبانی نمی‌شود؛ نگاشت‌های ثبت‌شده قابل مشاهده‌اند.");
                return;
            }
            const keyword=state.search.trim().toLocaleLowerCase("fa");
            const filtered=state.rows.filter(row=>{
                if(row.status!==state.status) return false;
                if(state.type&&Number(row.mapType)!==Number(state.type)) return false;
                if(state.reason&&row.reason!==state.reason) return false;
                if(!keyword) return true;
                return [row.crm?.customer,row.crm?.number,row.crm?.id,row.crm?.extra,
                    row.sepidar?.customer,row.sepidar?.number,row.sepidar?.id,
                    row.details,...(row.sources||[])].join(" ").toLocaleLowerCase("fa").includes(keyword);
            });
            state.page=Math.min(state.page,Math.max(1,Math.ceil(filtered.length/state.size)));
            const first=(state.page-1)*state.size;
            render(filtered.slice(first,first+state.size),filtered.length);
        }

        async function savedRows() {
            if(!state.softwareId)return;
            const requestId=++state.requestId;
            const softwareId=state.softwareId;
            records.replaceChildren();feedback("در حال خواندن نگاشت‌های ذخیره‌شده از دیتابیس CRM…");
            const response=await sendApiRequest({
                url:"/Home/GetSavedExternalMappings",
                params:{softwareId,mapType:state.type||undefined,search:state.search,page:state.page,pageSize:state.size},
                timeout:90000,showLoading:false
            });
            if(!isActive()||requestId!==state.requestId||softwareId!==state.softwareId)return;
            if(!response){feedback("دریافت نگاشت‌های ذخیره‌شده موفق نبود.",true);return;}
            state.saved=response.rows||[];state.savedTotal=Number(response.total||0);
            const max=Math.max(1,Math.ceil(state.savedTotal/state.size));
            if(state.page>max){state.page=max;await savedRows();return;}
            render(state.saved,state.savedTotal);
        }

        async function calculate() {
            if(state.calculating||state.softwareId!==state.automaticId)return;
            const softwareId = state.softwareId;
            state.calculating=true;state.selected.clear();summary();
            records.replaceChildren();
            feedback("در حال محاسبه تطبیق مشتریان، پیش‌فاکتورها و فاکتورها… هیچ نگاشتی ذخیره نمی‌شود.");
            const response=await sendApiRequest({url:"/Home/GetExternalMappingReview",
                showLoading:false,timeout:180000});
            if(!isActive())return;
            state.calculating=false;summary();
            if(softwareId !== state.softwareId)return;
            if(!response){feedback("محاسبه تطبیق‌ها انجام نشد. دوباره امتحان کنید.",true);return;}
            state.rows=response.rows||[];state.summary=response.summary||{};
            state.suggestionsLoaded=true;state.targets.clear();state.preview.clear();
            types();tabs();if(state.status==="saved")savedRows();else localRows();
        }

        async function switchSoftware(softwareId) {
            state.softwareId=softwareId;
            state.softwareTitle=softwareSelect.selectedOptions[0]?.textContent||"نرم‌افزار مقصد";
            state.type="";typeSelect.value="";
            state.search="";searchInput.value="";
            state.status="saved";state.reason="";state.page=1;
            state.suggestionsLoaded=false;state.rows=[];state.summary=null;
            state.selected.clear();state.targets.clear();state.preview.clear();
            ++state.requestId;types();tabs();await savedRows();
        }

        function payload(row) {
            if(!canMap(row)||!validId(targetValue(row)))return null;
            return {entityId:row.crm.id,mapType:Number(row.mapType),exEntityId:targetValue(row)};
        }

        async function save(items) {
            if(state.processing||!items.length)return;
            const customers=items.filter(x=>x.mapType===5).length;
            if(customers&&customers!==items.length){
                toastifyError("ابتدا مشتری‌ها را جداگانه نگاشت کنید و بعد سراغ فاکتورها و پیش‌فاکتورها بروید.");return;
            }
            const ok=await confirmAlert({title:"تأیید ثبت نگاشت",
                text:"آیا از ثبت "+format.format(items.length)+" نگاشت انتخاب‌شده در «"+state.softwareTitle+
                    "» مطمئن هستید؟ فقط ارتباط آن‌ها در CRM ذخیره می‌شود.",
                icon:"question",confirmText:"ثبت نگاشت",cancelText:"انصراف"});
            if(!ok)return;
            state.processing=true;selection();
            const result=await sendApiRequest({url:"/Home/SaveSelectedExternalMappings",
                method:"POST",headers:{"RequestVerificationToken":token},
                data:{externalSoftwareId:state.softwareId,items},timeout:120000,successMsg:null});
            state.processing=false;
            if(!result?.count){selection();return;}
            toastifySuccess(format.format(result.count)+" نگاشت در CRM ثبت شد.");
            state.selected.clear();state.targets.clear();
            const options=await sendApiRequest({url:"/Home/GetExternalMappingWorkspaceOptions",
                showLoading:false});
            if(options)state.counts=options.counts||[];
            state.suggestionsLoaded=false;state.summary=null;state.rows=[];state.page=1;
            types();summary();
            if(state.softwareId===state.automaticId)await calculate();
            else {state.status="saved";tabs();await savedRows();}
        }

        async function preview(row,id) {
            if(!validId(id))return;
            const key=state.softwareId+":"+row.mapType+":"+id;
            if(state.preview.has(key))return;
            const result=await sendApiRequest({url:"/Home/GetExternalMappingTargetDetails",
                params:{softwareId:state.softwareId,mapType:row.mapType,targetId:id},
                showLoading:false,showError:false,timeout:30000});
            if(!isActive()||state.targets.get(row.key)!==id)return;
            if(result?.target)state.preview.set(key,result.target);
            const existing=[...records.querySelectorAll("[data-key]")].find(x=>x.dataset.key===row.key);
            if(existing){
                const open=!existing.querySelector(".lmr-expanded").hidden;
                existing.outerHTML=card(row);
                const replaced=[...records.querySelectorAll("[data-key]")].find(x=>x.dataset.key===row.key);
                if(open&&replaced){
                    replaced.querySelector(".lmr-expanded").hidden=false;
                    replaced.querySelector("[data-expand]").setAttribute("aria-expanded","true");
                }
            }
        }

        records.addEventListener("click",async event=>{
            const cardElement=event.target.closest("[data-key]");
            if(!cardElement)return;
            const row=state.currentRows.find(x=>x.key===cardElement.dataset.key);
            if(!row)return;
            if(event.target.closest("[data-expand]")){
                const expanded=cardElement.querySelector(".lmr-expanded");
                expanded.hidden=!expanded.hidden;
                cardElement.querySelector("[data-expand]").setAttribute("aria-expanded",String(!expanded.hidden));
            }else if(event.target.closest("[data-map-one]")){
                const item=payload(row);if(item)await save([item]);
            }
        });

        records.addEventListener("change",event=>{
            const container=event.target.closest("[data-key]");
            const row=state.currentRows.find(x=>x.key===container?.dataset.key);
            if(!row)return;
            if(event.target.matches("[data-map-target]")){
                const id=event.target.value.trim();
                state.targets.set(row.key,id);state.selected.delete(row.key);
                const button=container.querySelector("[data-map-one]");
                if(button)button.disabled=!validId(id);
                const check=container.querySelector("[data-select-map]");
                if(check){check.checked=false;check.disabled=!validId(id);}
                selection();preview(row,id);
            }else if(event.target.matches("[data-select-map]")){
                const item=payload(row);
                if(event.target.checked&&item)state.selected.set(row.key,item);
                else state.selected.delete(row.key);
                selection();
            }
        });

        selectAll.addEventListener("change",()=>{
            state.currentRows.forEach(row=>{
                const item=payload(row);
                if(!item)return;
                if(selectAll.checked)state.selected.set(row.key,item);
                else state.selected.delete(row.key);
            });
            records.querySelectorAll("[data-key]").forEach(cardElement=>{
                const check=cardElement.querySelector("[data-select-map]");
                if(check)check.checked=state.selected.has(cardElement.dataset.key);
            });
            selection();
        });
        saveSelected.addEventListener("click",()=>save([...state.selected.values()]));

        statusButtons.addEventListener("click",event=>{
            const button=event.target.closest("[data-status]");if(!button)return;
            state.status=button.dataset.status;state.page=1;state.reason="";
            tabs();if(state.status==="saved")savedRows();else localRows();
        });
        get("lmrPagination").addEventListener("click",event=>{
            const button=event.target.closest("[data-page]");
            if(!button||button.disabled)return;
            state.page=Number(button.dataset.page);
            if(state.status==="saved")savedRows();else localRows();
        });
        typeSelect.addEventListener("change",()=>{
            state.type=typeSelect.value;state.page=1;state.reason="";
            reasons();if(state.status==="saved")savedRows();else localRows();
        });
        reasonSelect.addEventListener("change",()=>{
            state.reason=reasonSelect.value;state.page=1;localRows();
        });
        searchInput.addEventListener("input",debounce(()=>{
            state.search=searchInput.value.trim();state.page=1;
            if(state.status==="saved")savedRows();else localRows();
        },350));
        pageSizeSelect.addEventListener("change",()=>{
            state.size=Number(pageSizeSelect.value);state.page=1;
            if(state.status==="saved")savedRows();else localRows();
        });
        softwareSelect.addEventListener("change",()=>switchSoftware(softwareSelect.value));
        get("lmrRefreshSaved").addEventListener("click",()=>{
            state.status="saved";state.page=1;tabs();savedRows();
        });
        get("lmrCompute").addEventListener("click",calculate);

        (async()=>{
            feedback("در حال دریافت فهرست نرم‌افزارهای خارجی…");
            const result=await sendApiRequest({url:"/Home/GetExternalMappingWorkspaceOptions",
                showLoading:false});
            if(!isActive())return;
            if(!result){feedback("فهرست نرم‌افزارها دریافت نشد.",true);return;}
            state.types=result.mapTypes||[];
            state.counts=result.counts||[];
            state.automaticId=result.automaticMatchingSoftwareId;
            state.manualIds=result.supportedManualSoftwareIds||[];
            softwareSelect.replaceChildren();
            (result.software||[]).forEach(s=>softwareSelect.add(new Option(s.title,s.id)));
            types();
            if(!softwareSelect.value){feedback("هنوز نرم‌افزار خارجی در سیستم ثبت نشده است.");return;}
            if(typeof enableMomentumScrollScope==="function")enableMomentumScrollScope(page);
            await switchSoftware(softwareSelect.value);
        })();
    }

    if(document.readyState==="loading")
        document.addEventListener("DOMContentLoaded",initializeMappingManager,{once:true});
    else initializeMappingManager();
})();
