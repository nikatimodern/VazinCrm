async function openSupplyFinancial(sourceId) {
    const historyModal = document.getElementById("supplyFinancialModal");
    const wizard = document.getElementById("sendSupplyToExternalSoftwareModal");
    if (!historyModal || !wizard || historyModal.dataset.active === "true") return;
    historyModal.dataset.active = "true";

    const token = historyModal.querySelector('[name="__RequestVerificationToken"]').value;
    const historyList = document.getElementById("externalSoftwareSupplyFactors");
    const send = document.getElementById("sendSupplyFinancialBtn");
    const sendLabel = send.querySelector("[data-financial-history-send-label]");
    const historyHeading = historyModal.querySelector("[data-financial-history-heading]");
    const stageContainer = document.getElementById("supplyFinancialStage");
    const back = document.getElementById("backSupplyFinancialBtn");
    const confirm = document.getElementById("confirmSupplyFinancialBtn");
    const chooser = document.getElementById("chooseExternalSoftwareModal");
    const lifecycle = new AbortController();
    const { signal } = lifecycle;
    const numeral = new Intl.NumberFormat("fa-IR", { maximumFractionDigits: 4 });
    const selects = [];
    let version = 0, fieldId = 0, busy = false, choosing = false, closing = false;
    let destinations = [], position = 0, historyReceipt = false;
    let onBack, onConfirm, canConfirm;

    const make = (tag, text, classes) => {
        const node = document.createElement(tag);
        if (text != null) node.textContent = text;
        if (classes) node.className = classes;
        return node;
    };
    const format = value => numeral.format(value ?? 0);
    const label = option => [option.code, option.title].filter(Boolean).join(" - ");
    const request = async (action, data, method = "POST", quiet = false) => {
        const response = await sendApiRequest({
            url: "/Supply/" + action, method, successMsg: null, returnFullResponse: true,
            showLoading: !quiet, showError: !quiet, signal,
            ...(method === "GET" ? { params: data } : { data, headers: { RequestVerificationToken: token } }),
        });
        return response?.data ?? null;
    };
    const fields = (name, value) => {
        const entity = make("div", null, "external-mapping-entity");
        entity.append(make("span", name, "external-mapping-label"),
            make("span", value ?? "—", "external-mapping-entity-name fbold"));
        return entity;
    };
    const clearSelects = () => selects.splice(0).forEach(select => select.destroy());
    const updateButtons = () => {
        back.disabled = busy || !onBack;
        confirm.disabled = busy || !onConfirm || !canConfirm();
    };
    const stage = ({ software, title, subtitle, icon, step, content, large, backAction, confirmAction,
        confirmText = "تأیید و ادامه", backText = "برگشت", enabled = () => true }) => {
        clearSelects();
        renderExternalFinancialStage(wizard, stageContainer, {
            destination: software.title, position: "نرم‌افزار " + (position + 1) + " از " + destinations.length,
            title, subtitle, icon: '<i class="fas ' + icon + '" aria-hidden="true"></i>', step, content, large,
        });
        onBack = backAction;
        onConfirm = confirmAction;
        canConfirm = enabled;
        confirm.textContent = confirmText;
        back.querySelector("[data-external-mapping-back-label]").textContent = backText;
        updateButtons();
    };
    const field = (parent, text, control) => {
        control.id = "supply-financial-field-" + (++fieldId);
        const caption = make("label", text, "form-label mb-0");
        caption.htmlFor = control.id;
        const group = make("div");
        group.append(caption, control);
        parent.append(group);
        return control;
    };
    const descriptionField = (parent, text, value, max) => {
        const control = make("textarea", null, "form-control form-control-sm");
        control.rows = 1;
        control.maxLength = max;
        control.value = value ?? "";
        return field(parent, text, control);
    };
    const optionalDescription = (parent, label, value, max) => {
        const details = make("details", null, "supply-financial-note");
        details.open = Boolean(value?.trim());
        details.append(make("summary", details.open ? "ویرایش " + label : "افزودن " + label));
        const control = descriptionField(details, label, value, max);
        control.previousElementSibling.classList.add("visually-hidden");
        parent.append(details);
        return control;
    };
    const setupSelect = (control, placeholder, options = {}) => {
        selects.push(new TomSelect(control, { placeholder, onChange: updateButtons, ...options }));
    };
    const remote = (control, softwareId, items, preselected) => {
        const option = row => ({ value: String(row.id), text: label(row) });
        setupSelect(control, items ? "جستجو و انتخاب کالا" : "جستجو و انتخاب تأمین‌کننده", {
            searchField: [], preload: "focus",
            options: preselected ? [option(preselected)] : [],
            items: preselected ? [String(preselected.id)] : [],
            load: async (search, done) => {
                const rows = await request("FinancialOptions", { externalSoftwareId: softwareId, search, items }, "GET", true);
                if (control.tomselect) done((rows ?? []).map(option));
            },
        });
    };

    const loadHistory = async receipt => {
        historyReceipt = receipt;
        const current = ++version;
        const name = receipt ? "رسید انبار" : "فاکتور خرید";
        historyHeading.textContent = "سوابق " + name;
        sendLabel.textContent = "ارسال " + name + " به نرم‌افزار مالی";
        for (const tab of historyModal.querySelectorAll("[data-supply-financial-tab]")) {
            const active = (tab.dataset.supplyFinancialTab === "receipt") === receipt;
            tab.classList.toggle("active", active);
            tab.setAttribute("aria-pressed", String(active));
        }
        send.disabled = true;
        historyList.replaceChildren(make("div", "در حال دریافت سوابق…", "external-pf-empty"));
        const result = await request("FinancialHistory", { sourceId, receipt }, "GET");
        if (signal.aborted || current !== version) return;
        historyList.replaceChildren();
        if (!result) {
            historyList.append(make("div", "دریافت سوابق انجام نشد.", "external-pf-empty"));
            return;
        }
        historyModal.querySelector(".modal-title").textContent = "مدیریت ارسال فاکتور تأمین " + (result.serialNumber ?? "");
        const records = result.softwares.filter(row => row.document || row.hasMapping);
        for (const row of records) {
            const card = make("div", null, "external-pf-card");
            const info = make("div", null, "external-pf-info");
            const entries = [
                ["نرم‌افزار", row.title], ["تاریخ ثبت", row.dateText],
                ["شماره " + name, row.document ? format(row.document.number) : null],
                ["وضعیت", row.missing ? "سند مقصد پیدا نشد" : row.pending ? "ارتباط در CRM تکمیل نشده"
                    : row.document?.voucherId ? "دارای سند حسابداری" : "ثبت شده"],
            ];
            entries.forEach(([caption, value], index) => {
                const item = make("div", null, "external-pf-field");
                item.append(make("span", caption + ":", "external-pf-label"),
                    make("span", value ?? "—", "external-pf-value" + (index === 0 ? " external-pf-software" : "")));
                info.append(item);
            });
            card.append(info);
            historyList.append(card);
        }
        if (!records.length) {
            const hasDestination = result.softwares.some(row => !row.isDisabled);
            const message = receipt && !hasDestination ? "برای ارسال رسید انبار، ابتدا فاکتور خرید را در همان نرم‌افزار مالی ثبت کنید."
                : "این " + name + " هنوز در نرم‌افزار مالی ثبت نشده است.";
            historyList.append(make("div", message, "external-pf-empty"));
        }
        send.disabled = !result.softwares.some(row => !row.isDisabled);
        send.onclick = () => choose(result.softwares.filter(row => !row.isDisabled), receipt);
    };

    const choose = (options, receipt) => {
        if (choosing) return;
        choosing = true;
        let selected = [];
        let chooserShown = false;
        chooser.addEventListener("shown.bs.modal", () => {
            chooserShown = true;
            if (selected.length) ModalToggle(chooser, "hide");
        }, { once: true, signal });
        chooser.addEventListener("hidden.bs.modal", async () => {
            choosing = false;
            if (!selected.length) {
                send.focus();
                return;
            }
            destinations = selected.map(id => options.find(option => option.id === id)).filter(Boolean);
            position = 0;
            await prepare(destinations[0], receipt, true);
        }, { once: true, signal });
        showExternalSoftwareModal(sourceId, {
            options,
            onConfirm: ({ externalSoftwareIds }) => {
                selected = externalSoftwareIds;
                if (chooserShown) ModalToggle(chooser, "hide");
            },
        });
    };
    const next = () => {
        position++;
        if (position < destinations.length) return prepare(destinations[position], historyReceipt);
        closing = true;
        ModalToggle(wizard, "hide");
    };

    const prepare = async (software, receipt, open = false) => {
        const data = { supplyFactorId: sourceId, externalSoftwareId: software.id, receipt };
        const title = receipt ? "رسید انبار" : "فاکتور خرید";
        stage({ software, title, subtitle: "در حال دریافت اطلاعات", icon: "fa-desktop", step: "" });
        let shown;
        if (open) {
            shown = new Promise(resolve => wizard.addEventListener("shown.bs.modal", resolve, { once: true, signal }));
            ModalToggle(wizard, "show");
        }
        const pendingVersion = ++version;
        const [source] = await Promise.all([request("PrepareFinancial", data), shown]);
        if (signal.aborted || pendingVersion !== version) return;
        if (!source || source.recover || source.document) {
            const message = source?.document ? "سند شماره " + format(source.document.number) + " در سپیدار ثبت شده است."
                : "آماده‌سازی سند انجام نشد.";
            stage({ software, title, subtitle: message, icon: "fa-clipboard-check", step: "وضعیت ثبت",
                confirmText: source?.recover ? "تکمیل ارتباط" : "ادامه",
                confirmAction: async () => {
                    if (!source?.recover || await request("SubmitFinancial", data)) await next();
                },
            });
            return;
        }
        if (receipt) {
            documentStage(data, source, software);
        } else {
            vendorStage(data, source, software);
        }
    };

    const vendorStage = (data, source, software) => {
        const content = make("div", null, "d-flex flex-column gap-3");
        content.append(fields("تأمین‌کننده در CRM", source.supplierTxt));
        const select = field(content, "تأمین‌کننده معادل در نرم‌افزار مالی", make("select", null, "show-dropdown-arrow"));
        stage({ software, title: "تطبیق تأمین‌کننده", subtitle: "اتصال تأمین‌کننده CRM به نرم‌افزار مالی",
            icon: "fa-users", step: "مرحله ۱ از ۳", content,
            backAction: () => ModalToggle(wizard, "hide"), enabled: () => !!select.value,
            confirmAction: async () => {
                data.vendorId = Number(select.value);
                source.vendor = await request("SaveFinancialVendor", data);
                if (source.vendor) documentStage(data, source, software);
            },
        });
        remote(select, software.id, false, source.vendor);
        updateButtons();
    };

    const documentStage = (data, source, software) => {
        const { receipt } = data;
        const content = make("div", null, "supply-financial-matching");
        let stock, date;
        if (receipt) {
            content.append(fields("شماره فاکتور خرید", format(source.number)));
            stock = field(content, "انبار", make("select", null, "show-dropdown-arrow"));
            stock.append(new Option("", ""));
            source.stocks.forEach(row => stock.append(new Option(label(row), row.id)));
            stock.value = String(data.stockId ?? (source.stocks.length === 1 ? source.stocks[0].id : ""));
            date = make("input", null, "form-control form-control-sm mb-2");
            date.type = "date";
            date.value = data.date ?? source.date;
            date.addEventListener("input", updateButtons);
            field(content, "تاریخ رسید", date);
        }
        const list = make("div", null, "supply-financial-match-list");
        content.append(list);
        const rows = source.items.map(row => {
            const entity = make("div", null, "supply-financial-match-row");
            const identity = make("div", null, "supply-financial-match-source");
            identity.append(make("strong", row.product), make("small", [format(row.weightCount), row.unitTxt].filter(Boolean).join(" ")));
            entity.append(identity);
            const previous = data.items?.find(x => x.supplyFactorItemId === row.id);
            const target = make("div", null, "supply-financial-match-target");
            const select = receipt ? null : field(target, "کالای معادل در نرم‌افزار مالی",
                make("select", null, "show-dropdown-arrow"));
            const text = optionalDescription(target, "توضیحات قلم",
                receipt ? (data.rowDescriptions?.[row.id] ?? row.description) : previous?.description, receipt ? 4000 : 500);
            entity.append(target);
            list.append(entity);
            return { row, select, text, previous };
        });
        const description = optionalDescription(content, "توضیحات سند", data.description ?? source.description, receipt ? 4000 : 400);
        const captureDraft = () => {
            data.description = description.value;
            if (receipt) {
                data.stockId = Number(stock.value);
                data.date = date.value;
                data.rowDescriptions = Object.fromEntries(rows.map(x => [x.row.id, x.text.value]));
            } else {
                data.items = rows.map(x => ({
                    supplyFactorItemId: x.row.id, itemId: Number(x.select.value), description: x.text.value,
                    option: x.select.value ? { id: x.select.value, title: x.select.selectedOptions[0]?.textContent } : null,
                }));
            }
        };
        stage({ software, title: receipt ? "انتخاب انبار کالاها" : "تطبیق کالاها",
            subtitle: receipt ? "انبار مقصد برای دریافت همه اقلام را مشخص کنید" : "انتخاب کالای معادل برای ردیف‌های فاکتور تأمین",
            icon: receipt ? "fa-warehouse" : "fa-boxes", step: receipt ? "مرحله ۱ از ۲" : "مرحله ۲ از ۳", content, large: !receipt,
            backAction: receipt ? () => ModalToggle(wizard, "hide") : () => {
                captureDraft();
                vendorStage(data, source, software);
            },
            confirmText: receipt ? "تأیید انبار و ادامه" : "تأیید کالاها و ادامه",
            enabled: () => rows.length > 0 && (receipt ? !!stock.value && !!date.value : rows.every(row => row.select.value)),
            confirmAction: async () => {
                captureDraft();
                const preview = await request("PreviewFinancial", data);
                if (preview) previewStage(preview, data, source, software);
            },
        });
        if (receipt) setupSelect(stock, "جستجو و انتخاب انبار");
        else rows.forEach(row => remote(row.select, software.id, true, row.previous?.option));
        updateButtons();
    };

    const previewStage = (preview, data, source, software) => {
        const { receipt } = data;
        const title = receipt ? "رسید انبار" : "فاکتور خرید";
        const info = [
            { label: receipt ? "شماره فاکتور خرید" : "شماره فاکتور تأمین‌کننده",
                value: receipt ? format(source.number) : preview.serialNumber },
            { label: "تاریخ", value: preview.dateText },
            { label: "نرم‌افزار مقصد", value: software.title, accent: true },
            { label: "تأمین‌کننده", value: preview.supplierTxt, wide: true },
        ];
        if (receipt) info.push({ label: "انبار", value: preview.stock });
        if (preview.description) info.push({ label: "توضیحات", value: preview.description, wide: true });
        const columns = ["ردیف", ...(receipt ? [] : ["محصول CRM"]), "کالای مقصد", "مقدار", "مبلغ کالا",
            ...(receipt ? [] : ["تخفیف"]), "مالیات", "توضیحات", "مبلغ نهایی"];
        const content = renderExternalFinancialPreview({
            fields: info, columns,
            rows: preview.items.map((row, index) => [
                index + 1, ...(receipt ? [] : [source.items[index]?.product]), row.product, format(row.quantity), format(row.price),
                ...(receipt ? [] : [format(row.discount)]), format(row.tax), row.description || "—",
                format(Number(row.price ?? 0) - Number(row.discount ?? 0) + Number(row.tax ?? 0)),
            ]),
            totals: [["جمع مبلغ کالاها", preview.totalPrice], ["تخفیف کل", preview.discount],
                ["مالیات کل", preview.totalTax], ["مبلغ نهایی", preview.total]],
        });
        data.previewToken = preview.previewToken;
        stage({ software, title: "بررسی نهایی " + title, subtitle: "قبل از ثبت در نرم‌افزار مالی، اطلاعات را بررسی کنید",
            icon: "fa-clipboard-check", step: receipt ? "مرحله ۲ از ۲" : "مرحله ۳ از ۳", large: true,
            content: content + (receipt ? '<p class="text-muted small mb-0">' + (preview.voucher
                ? "سند حسابداری خودکار صادر می‌شود." : "صدور خودکار سند حسابداری در مقصد غیرفعال است.") + "</p>" : ""),
            backAction: () => documentStage(data, source, software),
            confirmText: "تأیید و ثبت در نرم‌افزار",
            confirmAction: async () => {
                const document = await request("SubmitFinancial", data);
                if (!document) return;
                stage({ software, title: "ثبت " + title, icon: "fa-check", step: "ثبت موفق",
                    subtitle: "سند شماره " + format(document.number) + " در نرم‌افزار مالی ثبت شد.",
                    content: receipt ? "" : make("p", "برای این فاکتور رسید انبار ثبت شود؟", "mb-0"),
                    backAction: receipt ? null : next,
                    backText: receipt ? "برگشت" : "بعداً",
                    confirmText: receipt ? "ادامه" : "ثبت رسید انبار",
                    confirmAction: receipt ? next : () => prepare(software, true),
                });
            },
        });
    };

    confirm.addEventListener("click", async () => {
        if (confirm.disabled) return;
        busy = true;
        updateButtons();
        try { await onConfirm(); }
        finally { busy = false; updateButtons(); }
    }, { signal });
    back.addEventListener("click", () => onBack?.(), { signal });
    wizard.addEventListener("hide.bs.modal", event => {
        if (busy && !closing) event.preventDefault();
    }, { signal });
    wizard.addEventListener("hidden.bs.modal", async () => {
        closing = false;
        clearSelects();
        stageContainer.replaceChildren();
        await loadHistory(historyReceipt);
        if (!signal.aborted)
            (send.disabled ? historyModal.querySelector("[data-supply-financial-tab].active") : send)?.focus();
    }, { signal });
    historyModal.querySelectorAll("[data-supply-financial-tab]").forEach(tab =>
        tab.addEventListener("click", () => loadHistory(tab.dataset.supplyFinancialTab === "receipt"), { signal }));
    historyModal.addEventListener("hidden.bs.modal", () => {
        lifecycle.abort();
        clearSelects();
        send.onclick = null;
        historyModal.dataset.active = "";
    }, { once: true });
    enableMomentumScrollScope(historyModal, { signal });
    enableMomentumScrollScope(wizard, { signal });
    ModalToggle(historyModal, "show");
    await loadHistory(false);
}
