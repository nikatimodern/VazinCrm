(() => {
    const root = document.getElementById("hrEmployeeWorkspace");
    if (!root) return;
    enableMomentumScrollScope(root);

    const modal = root.querySelector("#hrContractEditor");
    if (!modal || modal.dataset.initialized) return;
    modal.dataset.initialized = "true";
    const form = modal.querySelector("#hrContractForm");
    const terms = modal.querySelector("#hrContractTerms");
    const resolution = modal.querySelector("#hrContractResolution");
    const termination = modal.querySelector("#hrTerminationField");
    const note = modal.querySelector("#hrContractOperationNote");
    const submit = modal.querySelector("#hrContractSubmit");
    const field = name => form.elements.namedItem(name);
    const captions = { 1: "افزودن قرارداد", 2: "ویرایش قرارداد", 3: "تمدید قرارداد", 4: "تأیید قرارداد", 5: "لغو پیش‌نویس", 6: "فسخ قرارداد" };
    function updateEndDate() {
        const input = field("EndDateText");
        const fixed = field("Term").value === "1";
        input.disabled = terms.disabled || !fixed;
        input.required = !terms.disabled && fixed;
        if (!fixed) input.value = "";
    }
    field("Term").addEventListener("change", updateEndDate);
    modal.addEventListener("show.bs.modal", event => {
        const trigger = event.relatedTarget;
        const operation = Number(trigger?.dataset.operation);
        if (!Object.hasOwn(captions, operation)) { event.preventDefault(); return; }
        form.reset();
        submit.disabled = false;
        field("Operation").value = String(operation);
        field("ContractId").value = trigger.dataset.contractId || "";
        field("RowVersion").value = trigger.dataset.rowVersion || "";
        modal.querySelector("#hrContractEditorTitle").textContent = captions[operation];
        submit.textContent = operation === 4 ? "تأیید نهایی" : operation === 5 ? "لغو پیش‌نویس" : operation === 6 ? "ثبت فسخ" : "ذخیره پیش‌نویس";
        const hasTerms = operation <= 3;
        const hasResolution = operation === 5 || operation === 6;
        terms.hidden = !hasTerms;
        terms.disabled = !hasTerms;
        resolution.hidden = !hasResolution;
        resolution.disabled = !hasResolution;
        termination.hidden = operation !== 6;
        field("TerminationDateText").disabled = operation !== 6;
        field("TerminationDateText").required = operation === 6;
        field("Number").value = operation === 2 ? trigger.dataset.number || "" : "";
        field("Term").value = operation === 2 ? trigger.dataset.term || "1" : "1";
        field("AgreementDateText").value = operation === 2 ? trigger.dataset.agreement || "" : "";
        field("StartDateText").value = operation === 2 || operation === 3 ? trigger.dataset.start || "" : "";
        field("EndDateText").value = operation === 2 ? trigger.dataset.end || "" : "";
        note.hidden = operation < 3;
        const number = trigger.dataset.number || "";
        note.textContent = operation === 3 ? "تمدید قرارداد " + number + " با ایجاد پیش‌نویس جدید؛ قرارداد قبلی حفظ می‌شود."
            : operation === 4 ? "قرارداد " + number + " پس از تأیید مستقیماً قابل ویرایش نیست."
            : operation === 5 ? "پیش‌نویس " + number + " لغو می‌شود و در سوابق باقی می‌ماند."
            : operation === 6 ? "قرارداد " + number + " با ثبت تاریخ مؤثر و دلیل فسخ تعیین تکلیف می‌شود." : "";
        updateEndDate();
    });
    form.addEventListener("submit", () => { submit.disabled = true; });
    window.addEventListener("pageshow", () => { submit.disabled = false; }, { signal: window.PageRuntime?.current?.signal });
})();
