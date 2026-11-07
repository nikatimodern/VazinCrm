(() => {
    "use strict";
    const root = document.getElementById("releaseNotesAdmin");
    if (!root) return;
    const page = typeof PageRuntime !== "undefined" ? PageRuntime.current : null;
    const lifetime = new AbortController(), signal = lifetime.signal;
    const editor = root.querySelector("[data-editor]"), editorModal = root.querySelector("#releaseNoteEditor"), items = root.querySelector("[data-items]");
    const list = root.querySelector("[data-list]"), status = root.querySelector("[data-list-status]"), formError = root.querySelector("[data-form-error]");
    let current = null, saving = false, debounce, listRequest = null, listGeneration = 0;
    const roleSelect = new TomSelect(editor.elements.roles, { plugins:["remove_button"], maxItems:null, create:false });
    const cleanup = typeof enableMomentumScrollScope === "function" ? enableMomentumScrollScope(root) : null;
    const dispose = () => { lifetime.abort(); listRequest?.abort(); clearTimeout(debounce); roleSelect.destroy(); if (typeof cleanup === "function") cleanup(); bootstrap.Modal.getInstance(editorModal)?.dispose(); };
    if (page) page.cleanups.push(dispose); else window.addEventListener("pagehide", dispose, { once:true });
    const request = (action, data, id) => sendApiRequest({ url:"/ReleaseNotesAdmin/" + action, method:"POST", params:id ? { id } : null, data,
        headers:{ RequestVerificationToken:editor.querySelector("[name='__RequestVerificationToken']").value }, showLoading:false, successMsg:null,
        onError:err => { const message = err.response?.data?.message || "عملیات انجام نشد؛ دوباره تلاش کنید."; formError.textContent = message; formError.hidden = false; toastifyError(message); } });
    function node(tag, text, cls) { const el = document.createElement(tag); if (text != null) el.textContent = text; if (cls) el.className = cls; return el; }
    function button(text, action, id, danger = false) { const b = node("button", text, "rn-button" + (danger ? " rn-danger" : "")); b.type = "button"; b.dataset.action = action; if (id) b.dataset.id = id; return b; }
    function field(label, name, value, kind = "input", max = 200) {
        const wrap = node("label", label), input = node(kind); input.name = name;
        if (kind === "input") input.type = name.endsWith("Url") ? "text" : "text";
        input.maxLength = max; input.value = value || "";
        if (name === "itemTitle") input.required = true;
        if (name.endsWith("Url")) input.dir = "ltr";
        if (kind === "textarea") input.rows = 2;
        wrap.append(input); return wrap;
    }
    function addItem(item = {}) {
        const entry = node("section", null, "rn-edit-item"), toolbar = node("div", null, "rn-toolbar"), type = node("select"); type.name = "itemType"; type.setAttribute("aria-label", "نوع تغییر");
        [[1,"قابلیت جدید"],[2,"بهبود"],[3,"رفع اشکال"]].forEach(([value,text]) => { const option = node("option",text); option.value = value; type.append(option); });
        type.value = item.type || 2;
        toolbar.append(type, button("↑ بالا", "up"), button("↓ پایین", "down"), button("حذف تغییر", "remove", null, true));
        const fields = node("div", null, "rn-fields");
        const title = field("عنوان تغییر", "itemTitle", item.title); title.classList.add("rn-full");
        const description = field("توضیحات", "itemDescription", item.description, "textarea", 4000); description.classList.add("rn-full");
        fields.append(title, description, field("نشانی تصویر (اختیاری)","imageUrl",item.imageUrl,"input",2048),field("لینک بیشتر بدانید (اختیاری)","learnMoreUrl",item.learnMoreUrl,"input",2048));
        entry.append(toolbar, fields); items.append(entry); reorderButtons();
    }
    function reorderButtons() { [...items.children].forEach((item,index) => { item.querySelector("[data-action=up]").disabled = index === 0; item.querySelector("[data-action=down]").disabled = index === items.children.length-1; }); }
    function payload() {
        const f = editor.elements, date = f.publishDate.value;
        return { version:f.version.value.trim(), title:f.title.value.trim(), description:f.description.value.trim(), publishDate:date ? new Date(date).toISOString() : null,
            audience:Number(f.audience.value), isImportant:f.isImportant.checked, showPopup:f.showPopup.checked, rowVersion:current?.rowVersion || "", roles:roleSelect.getValue(),
            items:[...items.children].map((item,index) => ({ title:item.querySelector("[name=itemTitle]").value.trim(), description:item.querySelector("[name=itemDescription]").value.trim(),
                type:Number(item.querySelector("[name=itemType]").value), sortOrder:index, imageUrl:item.querySelector("[name=imageUrl]").value.trim(), learnMoreUrl:item.querySelector("[name=learnMoreUrl]").value.trim() })) };
    }
    function showEditor(note = null) {
        current = note; editor.reset(); items.replaceChildren(); formError.hidden = true;
        roleSelect.clear(); const f = editor.elements;
        if (note) {
            ["version","title","description"].forEach(key => { f[key].value = note[key] || ""; });
            f.audience.value = note.audience; f.isImportant.checked = note.isImportant; f.showPopup.checked = note.showPopup;
            if (note.publishDate) { const date = new Date(note.publishDate.endsWith("Z") ? note.publishDate : note.publishDate + "Z"); date.setMinutes(date.getMinutes() - date.getTimezoneOffset()); f.publishDate.value = date.toISOString().slice(0,16); }
            roleSelect.setValue(note.roles); note.items.forEach(addItem);
        } else addItem();
        root.querySelector("[data-roles-wrap]").hidden = f.audience.value !== "3";
        ModalToggle(editorModal,"show");
    }
    async function load() {
        listRequest?.abort(); listRequest = new AbortController(); const generation = ++listGeneration;
        status.textContent = "در حال دریافت نسخه‌ها…";
        const notes = await sendApiRequest({ url:"/ReleaseNotesAdmin/List", params:{ search:root.querySelector("[data-search]").value, status:root.querySelector("[data-status]").value || null }, signal:listRequest.signal, showLoading:false });
        if (generation !== listGeneration || signal.aborted) return;
        if (!Array.isArray(notes)) { status.textContent = "دریافت اطلاعات انجام نشد؛ تازه‌سازی را بزنید."; return; }
        status.textContent = notes.length ? notes.length.toLocaleString("fa-IR") + " نسخه" : "نسخه‌ای پیدا نشد.";
        const fragment = document.createDocumentFragment();
        for (const note of notes) {
            const row = node("article",null,"rn-list-row"), info = node("div",null,"rn-list-info"), actions = node("div",null,"rn-list-actions");
            info.append(node("span",note.version,"rn-version"),node("h2",note.title),node("span",["پیش‌نویس","منتشرشده","بایگانی"][note.status],"rn-tag"));
            info.append(node("p", (note.items?.length || 0).toLocaleString("fa-IR") + " تغییر · " + ["همه کاربران","کاربران داخلی","مشتریان","نقش‌های انتخابی"][note.audience] + (note.isImportant ? " · مهم" : ""), "rn-muted"));
            if (note.publishDate) { const date = new Date(note.publishDate.endsWith("Z") ? note.publishDate : note.publishDate+"Z"); info.append(node("p", date.toLocaleString("fa-IR") + (date > new Date() ? " — زمان‌بندی‌شده" : ""),"rn-muted")); }
            actions.append(button("پیش‌نمایش","preview",note.id));
            if (note.status === 0) actions.append(button("ویرایش","edit",note.id),button("انتشار","publish",note.id),button("حذف پیش‌نویس","delete",note.id,true));
            if (note.status === 1) actions.append(button("بایگانی","archive",note.id));
            row.append(info,actions); fragment.append(row);
        }
        list.replaceChildren(fragment);
    }
    editor.addEventListener("submit", async event => {
        event.preventDefault(); if (saving || !editor.reportValidity()) return;
        saving = true; const submit = editor.querySelector("[type=submit]"); submit.disabled = true; formError.hidden = true;
        try {
            const result = await request(current ? "Update" : "Create", payload(), current?.id);
            if (result) { saving = false; ModalToggle(editorModal,"hide"); toastifySuccess("پیش‌نویس ذخیره شد."); await load(); }
        } finally { saving = false; submit.disabled = false; }
    },{ signal });
    editorModal.addEventListener("hide.bs.modal", event => { if (saving) event.preventDefault(); },{ signal });
    root.addEventListener("click", async event => {
        const target = event.target.closest("button"); if (!target || saving) return;
        if (target.matches("[data-new]")) { showEditor(); return; }
        if (target.matches("[data-refresh]")) { load(); return; }
        if (target.matches("[data-add-item]")) { if (items.children.length < 100) addItem(); return; }
        if (target.matches("[data-preview]")) {
            if (!editor.reportValidity()) return;
            const draft = payload();
            editorModal.addEventListener("hidden.bs.modal", () => {
                const modal = document.getElementById("releaseNotesModal");
                modal.addEventListener("hidden.bs.modal", () => { if (!signal.aborted) ModalToggle(editorModal,"show"); }, { once:true, signal });
                ReleaseNotes.preview([draft]);
            }, { once:true, signal });
            ModalToggle(editorModal,"hide"); return;
        }
        const action = target.dataset.action, id = target.dataset.id;
        if (["up","down","remove"].includes(action)) {
            const item = target.closest(".rn-edit-item");
            if (action === "remove") { if (items.children.length > 1) item.remove(); else toastifyError("حداقل یک تغییر لازم است."); }
            if (action === "up" && item.previousElementSibling) items.insertBefore(item,item.previousElementSibling);
            if (action === "down" && item.nextElementSibling) items.insertBefore(item.nextElementSibling,item);
            reorderButtons(); return;
        }
        if (!id) return;
        target.disabled = true;
        try {
            if (["edit","preview"].includes(action)) {
                const note = await sendApiRequest({ url:"/ReleaseNotesAdmin/Get", params:{id}, signal, showLoading:false });
                if (!note || signal.aborted) return;
                if (action === "edit") showEditor(note); else ReleaseNotes.preview([note]);
                return;
            }
            const messages = { publish:"پس از انتشار، محتوا قابل ویرایش نیست. منتشر شود؟", archive:"این نسخه از نمایش خودکار خارج و در تاریخچه نگهداری شود؟", delete:"این پیش‌نویس حذف شود؟" };
            if (!messages[action]) return;
            const confirmed = await confirmAlert({ title:messages[action], icon:"question", confirmText:"تأیید", cancelText:"انصراف", confirmButtonColor:action==="delete"?"#C2185B":"#00665C" });
            if (!confirmed || signal.aborted) return;
            const result = await request(action[0].toUpperCase()+action.slice(1),null,id); if (result) await load();
        } finally { if (target.isConnected) target.disabled = false; }
    },{ signal });
    root.querySelector("[data-status]").addEventListener("change", load,{signal});
    editor.elements.audience.addEventListener("change", () => { root.querySelector("[data-roles-wrap]").hidden = editor.elements.audience.value !== "3"; },{signal});
    root.querySelector("[data-search]").addEventListener("input", () => { clearTimeout(debounce); debounce = setTimeout(load,300); },{signal});
    load();
})();
