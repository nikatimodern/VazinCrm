(() => {
    "use strict";

    const root = document.getElementById("accessCatalog");
    if (!root || root.dataset.initialized === "1") return;
    root.dataset.initialized = "1";

    const page = typeof PageRuntime !== "undefined" ? PageRuntime.current : null;
    const lifetime = new AbortController();
    const signal = lifetime.signal;
    const entityForm = root.querySelector("[data-entity-form]");
    const permissionForm = root.querySelector("[data-permission-form]");
    const roleForm = root.querySelector("[data-role-form]");
    const roleModal = root.querySelector("#acRoleModal");
    const roleDeleteModal = root.querySelector("#acRoleDeleteModal");
    const roleDeleteForm = root.querySelector("[data-role-delete-form]");
    const userProfileModal = root.querySelector("#acUserProfileModal");
    const userProfileForm = root.querySelector("[data-user-profile-form]");
    const entityModal = root.querySelector("#acEntityModal");
    const permissionModal = root.querySelector("#acPermissionModal");
    const entityList = root.querySelector("[data-entities]");
    const permissionList = root.querySelector("[data-permissions]");
    const modelInput = root.querySelector("[data-model-select]");
    const csrf = root.querySelector("input[name=__RequestVerificationToken]").value;
    let entities = [], permissions = [], currentEntityId = null, entityEditId = null, permissionEditId = null;
    let saving = false, requestGeneration = 0, modelSelect = null;
    const rolesList = root.querySelector("[data-roles-list]");
    const roleGroups = root.querySelector("[data-role-groups]");
    let roles = [], roleCatalog = [], currentRoleId = null, activeTab = "catalog";
    let roleChosen = new Set(), roleOriginal = new Set();
    let roleReady = false, roleSaving = false, roleMutating = false, roleEditId = null;
    let roleLoadGeneration = 0, roleSelectionGeneration = 0, roleDeleteGeneration = 0;
    let roleDeletePreview = null, roleDeleteChoices = new Map();
    const usersList = root.querySelector("[data-users-list]");
    const userTypeSelect = root.querySelector("[data-users-type]");
    const userPermissions = root.querySelector("[data-user-permissions]");
    let userAvailableRoles = [], userCatalog = [], userGrantSources = new Map(), userPageItems = [];
    let userPage = 1, userPages = 1, userCount = 0, userSearchTerm = "";
    let userType = userTypeSelect.value;
    let currentUserId = null, userRoleIds = new Set(), userOriginalRoles = new Set();
    let userOverrides = new Map(), userOriginalOverrides = new Map();
    let userReady = false, userSaving = false, userLoading = false;
    let userTabGeneration = 0, userPageGeneration = 0, userSelectionGeneration = 0, userSearchTimer = null;
    let userProfileSaving = false, userProfileGeneration = 0, userProfileId = null, userProfileStamp = null;
    let userPositions = null;
    const cleanupScroll = typeof enableMomentumScroll === "function" ? enableMomentumScroll(entityList) : null;
    const cleanupRoleListScroll = typeof enableMomentumScroll === "function" ? enableMomentumScroll(rolesList) : null;
    const cleanupRoleGroupsScroll = typeof enableMomentumScroll === "function" ? enableMomentumScroll(roleGroups) : null;
    const cleanupUsersScroll = typeof enableMomentumScroll === "function" ? enableMomentumScroll(usersList) : null;
    const cleanupUserPermissionsScroll = typeof enableMomentumScroll === "function" ? enableMomentumScroll(userPermissions) : null;

    const dispose = () => {
        lifetime.abort();
        requestGeneration++;
        modelSelect?.destroy();
        if (typeof cleanupScroll === "function") cleanupScroll();
        if (typeof cleanupRoleListScroll === "function") cleanupRoleListScroll();
        if (typeof cleanupRoleGroupsScroll === "function") cleanupRoleGroupsScroll();
        if (typeof cleanupUsersScroll === "function") cleanupUsersScroll();
        if (typeof cleanupUserPermissionsScroll === "function") cleanupUserPermissionsScroll();
        if (userSearchTimer !== null) clearTimeout(userSearchTimer);
        bootstrap.Modal.getInstance(entityModal)?.dispose();
        bootstrap.Modal.getInstance(permissionModal)?.dispose();
        bootstrap.Modal.getInstance(roleModal)?.dispose();
        bootstrap.Modal.getInstance(roleDeleteModal)?.dispose();
        bootstrap.Modal.getInstance(userProfileModal)?.dispose();
    };
    if (page) page.cleanups.push(dispose);
    else window.addEventListener("pagehide", dispose, { once: true });

    const field = (form, name) => form.elements.namedItem(name);
    const text = (tag, value, cls) => {
        const el = document.createElement(tag);
        if (value !== undefined && value !== null) el.textContent = String(value);
        if (cls) el.className = cls;
        return el;
    };
    const actionButton = (label, action, id, danger = false) => {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "ac-icon-button" + (danger ? " is-danger" : "");
        btn.dataset.action = action;
        btn.dataset.id = id;
        btn.setAttribute("aria-label", label);
        btn.title = label;
        const icon = text("i");
        icon.className = danger ? "fas fa-trash-alt" : "fas fa-pen";
        icon.setAttribute("aria-hidden", "true");
        btn.append(icon);
        return btn;
    };
    const selectedEntity = () => entities.find(x => x.id === currentEntityId);
    const showEmpty = (target, message, icon = "fa-shield-alt") => {
        const container = text("div", null, "ac-empty");
        container.append(text("i", null, "fas " + icon), text("p", message));
        target.replaceChildren(container);
    };
    const showError = (formType, message) => {
        const el = root.querySelector("[data-" + formType + "-error]");
        el.textContent = message;
        el.hidden = !message;
    };
    const post = async (action, data, id) => sendApiRequest({
        url: "/AccessCatalogAdmin/" + action,
        method: "POST",
        params: id ? { id } : null,
        data,
        headers: { RequestVerificationToken: csrf },
        successMsg: null,
        showLoading: false,
        showError: false,
        onError: err => {
            const message = err.response?.data?.message || "عملیات انجام نشد. دوباره تلاش کنید.";
            toastifyError(message);
            showError(action === "SaveUserProfile" ? "user-profile" :
                action === "SaveRole" ? "role-form" :
                action === "DeleteRole" ? "role-delete" :
                action === "SaveRolePermissions" ? "role" :
                action === "SaveUserAccess" ? "user" :
                action.includes("Permission") ? "permission" : "entity", message);
        }
    });

    function renderEntities() {
        const search = root.querySelector("[data-entity-search]").value.trim().toLocaleLowerCase();
        const filtered = entities.filter(x => [x.title, x.key, x.modelFullName].some(t => (t || "").toLocaleLowerCase().includes(search)));
        root.querySelector("[data-entity-count]").textContent = String(entities.length);
        entityList.replaceChildren();
        if (!filtered.length) {
            showEmpty(entityList, entities.length ? "انتیتی مطابق جست‌وجو پیدا نشد." : "هنوز انتیتی تعریف نشده است.", "fa-folder-open");
            return;
        }
        for (const item of filtered) {
            const row = text("div", null, "ac-entity-item" + (item.id === currentEntityId ? " is-selected" : ""));
            const select = document.createElement("button");
            select.type = "button";
            select.className = "ac-entity-select";
            select.dataset.selectEntity = item.id;
            select.setAttribute("aria-pressed", String(item.id === currentEntityId));
            select.setAttribute("aria-label", "نمایش دسترسی‌های " + item.title);
            const meta = text("span", null, "ac-entity-meta");
            meta.append(text("strong", item.title), text("small", item.key + " · " + item.permissionCount + " دسترسی"));
            const badge = text("span", item.isActive ? "فعال" : "غیرفعال", "ac-pill" + (item.isActive ? "" : " is-off"));
            select.append(meta, badge);
            const actions = text("div", null, "ac-entity-actions");
            actions.append(
                actionButton("ویرایش انتیتی " + item.title, "edit-entity", item.id),
                actionButton("حذف انتیتی " + item.title, "delete-entity", item.id, true)
            );
            row.append(select, actions);
            entityList.append(row);
        }
    }

    function renderPermissions() {
        permissionList.replaceChildren();
        const item = selectedEntity();
        const hasEntity = !!item;
        root.querySelector("[data-selected-title]").textContent = hasEntity ? "دسترسی‌های " + item.title : "دسترسی‌های انتیتی";
        root.querySelector("[data-selected-key]").textContent = hasEntity ? item.key + " · " + item.modelFullName : "برای شروع یک انتیتی انتخاب کنید.";
        root.querySelector("[data-action='new-permission']").disabled = !hasEntity;
        if (!hasEntity) { showEmpty(permissionList, "یک انتیتی را از فهرست انتخاب کنید."); return; }
        if (!permissions.length) {
            showEmpty(permissionList, "هنوز دسترسی تعریف نشده است. می‌توانید یکی از مجوزهای پیشنهادی را انتخاب کنید.", "fa-key");
            const presets = text("div", null, "ac-presets");
            for (const item of [{ key: "View", title: "مشاهده" }, { key: "Create", title: "ایجاد" },
                                { key: "Edit", title: "ویرایش" }, { key: "Delete", title: "حذف" }]) {
                const btn = text("button", "+ " + item.title, "ac-btn ac-btn-muted");
                btn.type = "button";
                btn.dataset.action = "preset-permission";
                btn.dataset.key = item.key;
                btn.dataset.title = item.title;
                presets.append(btn);
            }
            permissionList.append(presets);
            return;
        }
        for (const permission of permissions) {
            const card = text("div", null, "ac-permission-item");
            const info = text("div");
            info.append(text("strong", permission.title), text("small", permission.fullKey));
            if (permission.description) info.append(text("p", permission.description, "ac-muted"));
            const actions = text("div", null, "ac-permission-actions");
            if (!permission.isActive) actions.append(text("span", "غیرفعال", "ac-pill is-off"));
            actions.append(actionButton("ویرایش دسترسی", "edit-permission", permission.id),
                           actionButton("حذف دسترسی", "delete-permission", permission.id, true));
            card.append(info, actions);
            permissionList.append(card);
        }
    }

    async function loadModels() {
        const data = await sendApiRequest({ url: "/AccessCatalogAdmin/Models", showLoading: false });
        if (!Array.isArray(data) || signal.aborted) return;
        modelInput.replaceChildren(new Option("انتخاب مدل...", ""));
        for (const model of data) {
            const option = new Option(model.modelName + "  (" + model.dbContextName + ")", model.dbContextName + "|" + model.modelFullName);
            modelInput.append(option);
        }
        if (typeof TomSelect === "function") {
            modelSelect = new TomSelect(modelInput, {
                create: false, allowEmptyOption: true, maxItems: 1, searchField: ["text", "value"]
            });
        }
    }

    async function loadEntities(preserve = true) {
        const previous = preserve ? currentEntityId : null;
        const data = await sendApiRequest({ url: "/AccessCatalogAdmin/Entities", showLoading: false });
        if (!Array.isArray(data) || signal.aborted) return;
        entities = data;
        const id = entities.some(x => x.id === previous) ? previous : (entities[0]?.id || null);
        currentEntityId = null;
        permissions = [];
        if (id) await selectEntity(id);
        else { renderEntities(); renderPermissions(); }
    }

    async function selectEntity(id) {
        currentEntityId = id;
        permissions = [];
        renderEntities();
        renderPermissions();
        if (!id) return;
        const generation = ++requestGeneration;
        const data = await sendApiRequest({ url: "/AccessCatalogAdmin/Permissions", params: { entityId: id }, showLoading: false });
        if (signal.aborted || generation !== requestGeneration || currentEntityId !== id) return;
        permissions = Array.isArray(data) ? data : [];
        renderPermissions();
    }

    function setModel(value, locked) {
        if (modelSelect) {
            modelSelect.enable();
            modelSelect.setValue(value || "", true);
            if (locked) modelSelect.disable();
        } else {
            modelInput.disabled = false;
            modelInput.value = value || "";
            modelInput.disabled = locked;
        }
    }

    function openEntityEditor(entity = null) {
        entityEditId = entity?.id || null;
        entityForm.reset();
        showError("entity", "");
        root.querySelector("#acEntityModalTitle").textContent = entity ? "ویرایش انتیتی" : "انتیتی جدید";
        setModel(entity ? entity.dbContextName + "|" + entity.modelFullName : "", !!entity);
        field(entityForm, "key").value = entity?.key || "";
        field(entityForm, "key").readOnly = !!entity;
        field(entityForm, "title").value = entity?.title || "";
        field(entityForm, "description").value = entity?.description || "";
        field(entityForm, "isActive").checked = entity ? entity.isActive : true;
        ModalToggle(entityModal, "show");
    }

    function updateKeyPreview() {
        root.querySelector("[data-key-preview]").textContent = (selectedEntity()?.key || "") + "." + field(permissionForm, "key").value.trim();
    }

    function openPermissionEditor(permission = null) {
        const entity = selectedEntity();
        if (!entity) return;
        permissionEditId = permission?.id || null;
        permissionForm.reset();
        showError("permission", "");
        root.querySelector("#acPermissionModalTitle").textContent = permission ? "ویرایش دسترسی" : "دسترسی جدید";
        root.querySelector("[data-permission-entity]").textContent = entity.title + " (" + entity.key + ")";
        field(permissionForm, "key").value = permission?.key || "";
        field(permissionForm, "key").readOnly = !!permission;
        field(permissionForm, "title").value = permission?.title || "";
        field(permissionForm, "description").value = permission?.description || "";
        field(permissionForm, "isActive").checked = permission ? permission.isActive : true;
        updateKeyPreview();
        ModalToggle(permissionModal, "show");
    }

    async function saveEntity(event) {
        event.preventDefault();
        if (saving || !entityForm.reportValidity()) return;
        const model = modelInput.value.split("|");
        if (model.length !== 2) { showError("entity", "یک مدل معتبر انتخاب کنید."); return; }
        saving = true;
        const submit = entityForm.querySelector("[type=submit]");
        submit.disabled = true;
        try {
            const response = await post("SaveEntity", {
                dbContextName: model[0], modelFullName: model[1],
                key: field(entityForm, "key").value.trim(),
                title: field(entityForm, "title").value.trim(),
                description: field(entityForm, "description").value.trim(),
                isActive: field(entityForm, "isActive").checked
            }, entityEditId);
            if (response?.success) {
                ModalToggle(entityModal, "hide");
                toastifySuccess("انتیتی ذخیره شد.");
                currentEntityId = response.id;
                await loadEntities();
            } else if (response === null && !root.querySelector("[data-entity-error]").textContent)
                showError("entity", "ذخیره انجام نشد.");
        } finally { saving = false; submit.disabled = false; }
    }

    async function savePermission(event) {
        event.preventDefault();
        if (saving || !permissionForm.reportValidity() || !selectedEntity()) return;
        saving = true;
        const submit = permissionForm.querySelector("[type=submit]");
        submit.disabled = true;
        try {
            const response = await post("SavePermission", {
                accessEntityId: currentEntityId,
                key: field(permissionForm, "key").value.trim(),
                title: field(permissionForm, "title").value.trim(),
                description: field(permissionForm, "description").value.trim(),
                isActive: field(permissionForm, "isActive").checked
            }, permissionEditId);
            if (response?.success) {
                ModalToggle(permissionModal, "hide");
                toastifySuccess("دسترسی ذخیره شد.");
                await loadEntities();
            } else if (response === null && !root.querySelector("[data-permission-error]").textContent)
                showError("permission", "ذخیره انجام نشد.");
        } finally { saving = false; submit.disabled = false; }
    }

    async function deleteItem(kind, id) {
        if (saving) return;
        const isPermission = kind === "Permission";
        const allowed = await confirmAlert({
            title: isPermission ? "حذف دسترسی" : "حذف انتیتی",
            text: isPermission ? "آیا این دسترسی حذف شود؟" : "انتیتی فقط زمانی حذف می‌شود که هیچ دسترسی مرتبطی نداشته باشد.",
            confirmText: "حذف", cancelText: "انصراف", icon: "warning"
        });
        if (!allowed || signal.aborted) return;
        saving = true;
        try {
            const response = await post("Delete" + kind, null, id);
            if (response?.success) {
                toastifySuccess("حذف انجام شد.");
                await loadEntities();
            }
        } finally { saving = false; }
    }


    // This tab only stores role assignments; enforcing them is intentionally a separate phase.
    const currentRole = () => roles.find(x => x.id === currentRoleId);
    const roleDirty = () => roleChosen.size !== roleOriginal.size ||
        [...roleChosen].some(id => !roleOriginal.has(id));

    function renderRoles() {
        const query = root.querySelector("[data-role-search]").value.trim().toLocaleLowerCase();
        rolesList.replaceChildren();
        root.querySelector("[data-role-count]").textContent = String(roles.length);
        const visible = roles.filter(x => (x.name || "").toLocaleLowerCase().includes(query));
        if (!visible.length) {
            showEmpty(rolesList, roles.length ? "نقشی با این نام پیدا نشد." : "نقشی تعریف نشده است.", "fa-user-tag");
            return;
        }
        for (const role of visible) {
            const row = text("div", null, "ac-role-list-row");
            const button = text("button", null, "ac-role-item" + (role.id === currentRoleId ? " is-selected" : ""));
            button.type = "button";
            button.dataset.selectRole = role.id;
            button.setAttribute("aria-pressed", String(role.id === currentRoleId));
            button.append(text("strong", role.name));
            row.append(button);
            const actions = text("div", null, "ac-role-list-actions");
            actions.append(
                actionButton("ویرایش نقش " + role.name, "edit-role", role.id),
                actionButton("حذف نقش " + role.name, "delete-role", role.id, true)
            );
            for (const action of actions.querySelectorAll("button")) action.disabled = roleSaving || roleMutating;
            row.append(actions);
            rolesList.append(row);
        }
    }

    function syncRoleState() {
        const total = roleChosen.size;
        root.querySelector("[data-role-selected]").textContent = total + " دسترسی انتخاب شده";
        root.querySelector("[data-action='save-role']").disabled = !roleReady || roleSaving || roleMutating || !roleDirty();
        root.querySelector("[data-action='reset-role']").disabled = !roleReady || roleSaving || roleMutating || !roleDirty();
        root.querySelector("[data-action='new-role']").disabled = roleSaving || roleMutating;
        root.querySelector("[data-action='reload-roles']").disabled = roleSaving || roleMutating;
        for (const button of rolesList.querySelectorAll("[data-action='edit-role'], [data-action='delete-role']"))
            button.disabled = roleSaving || roleMutating;
        for (const input of roleGroups.querySelectorAll("[data-role-permission-id], [data-role-entity-toggle]"))
            input.disabled = !roleReady || roleSaving || roleMutating;
        root.querySelector("[data-role-subtitle]").textContent = !currentRole() ? "برای شروع یک نقش انتخاب کنید." :
            !roleReady ? "در حال دریافت مجوزها..." :
            roleDirty() ? "تغییرات ذخیره‌نشده" : "همه تغییرات ذخیره شده‌اند.";
        for (const group of roleGroups.querySelectorAll("[data-role-entity]")) {
            const entity = roleCatalog.find(x => x.id === group.dataset.roleEntity);
            if (!entity) continue;
            const count = entity.permissions.filter(p => roleChosen.has(p.id)).length;
            const selectAll = group.querySelector("[data-role-entity-toggle]");
            selectAll.checked = entity.permissions.length > 0 && count === entity.permissions.length;
            selectAll.indeterminate = count > 0 && count < entity.permissions.length;
            group.querySelector("[data-role-group-count]").textContent = count + " از " + entity.permissions.length;
        }
    }

    function renderRolePermissions() {
        roleGroups.replaceChildren();
        root.querySelector("[data-role-title]").textContent = currentRole() ? "دسترسی‌های " + currentRole().name : "دسترسی‌های نقش";
        if (!currentRole()) {
            showEmpty(roleGroups, "یک نقش را انتخاب کنید.", "fa-user-shield");
            syncRoleState();
            return;
        }
        if (!roleReady) {
            showEmpty(roleGroups, "در حال دریافت دسترسی‌های نقش...", "fa-spinner");
            syncRoleState();
            return;
        }
        const term = root.querySelector("[data-role-permission-search]").value.trim().toLocaleLowerCase();
        const visible = roleCatalog.map(entity => {
            const matchedEntity = [entity.title, entity.key].some(x => x.toLocaleLowerCase().includes(term));
            const selected = matchedEntity ? entity.permissions :
                entity.permissions.filter(p => [p.title, p.key].some(x => x.toLocaleLowerCase().includes(term)));
            return { entity, selected };
        }).filter(x => x.selected.length > 0);
        if (!visible.length) {
            showEmpty(roleGroups, roleCatalog.length ? "دسترسی مطابق جست‌وجو پیدا نشد." :
                "هنوز انتیتی و دسترسی فعالی تعریف نشده است.", "fa-key");
            syncRoleState();
            return;
        }
        for (const {entity, selected} of visible) {
            const section = text("section", null, "ac-role-group");
            section.dataset.roleEntity = entity.id;
            const header = text("header", null, "ac-role-group-header");
            const allLabel = text("label", null, "ac-role-group-all");
            const toggle = document.createElement("input");
            toggle.type = "checkbox";
            toggle.dataset.roleEntityToggle = entity.id;
            toggle.setAttribute("aria-label", "انتخاب همه مجوزهای " + entity.title);
            const heading = text("span");
            heading.append(text("strong", entity.title), text("small", entity.key));
            allLabel.append(toggle, heading);
            const groupCount = text("span", null, "ac-role-group-count");
            groupCount.dataset.roleGroupCount = entity.id;
            header.append(allLabel, groupCount);
            const grid = text("div", null, "ac-role-choices");
            for (const permission of selected) {
                const label = text("label", null, "ac-role-choice");
                const input = document.createElement("input");
                input.type = "checkbox";
                input.dataset.rolePermissionId = permission.id;
                input.checked = roleChosen.has(permission.id);
                const description = text("span");
                description.append(text("strong", permission.title), text("small", permission.key));
                label.append(input, description);
                grid.append(label);
            }
            section.append(header, grid);
            roleGroups.append(section);
        }
        syncRoleState();
    }

    async function selectRole(roleId) {
        if (!roles.some(x => x.id === roleId)) return;
        const generation = ++roleSelectionGeneration;
        currentRoleId = roleId;
        roleReady = false;
        roleChosen = new Set();
        roleOriginal = new Set();
        showError("role", "");
        renderRoles();
        renderRolePermissions();
        const ids = await sendApiRequest({
            url: "/AccessCatalogAdmin/RolePermissions",
            params: { roleId },
            showLoading: false
        });
        if (signal.aborted || generation !== roleSelectionGeneration || currentRoleId !== roleId) return;
        if (!Array.isArray(ids)) {
            showEmpty(roleGroups, "دریافت دسترسی‌های نقش موفق نبود. مجدداً تلاش کنید.", "fa-exclamation-circle");
            return;
        }
        const permitted = new Set(roleCatalog.flatMap(x => x.permissions.map(p => p.id)));
        roleChosen = new Set(ids.filter(id => permitted.has(id)));
        roleOriginal = new Set(roleChosen);
        roleReady = true;
        renderRolePermissions();
    }

    async function confirmDiscardRoleChanges() {
        if (!roleReady || !roleDirty()) return true;
        return !!(await confirmAlert({
            title: "تغییرات ذخیره نشده",
            text: "تغییرات مجوزهای این نقش ذخیره نشده‌اند. بدون ذخیره ادامه می‌دهید؟",
            confirmText: "ادامه بدون ذخیره",
            cancelText: "انصراف",
            icon: "warning"
        }));
    }

    async function loadRoleTab(preferredRoleId = currentRoleId) {
        const generation = ++roleLoadGeneration;
        ++roleSelectionGeneration;
        roleReady = false;
        roleChosen = new Set();
        roleOriginal = new Set();
        currentRoleId = null;
        root.querySelector("[data-role-title]").textContent = "دسترسی‌های نقش";
        showEmpty(rolesList, "در حال دریافت نقش‌ها...", "fa-spinner");
        showEmpty(roleGroups, "در حال دریافت انتیتی‌ها و دسترسی‌ها...", "fa-spinner");
        syncRoleState();
        const [loadedRoles, catalog] = await Promise.all([
            sendApiRequest({ url: "/AccessCatalogAdmin/Roles", showLoading: false }),
            sendApiRequest({ url: "/AccessCatalogAdmin/RoleCatalog", showLoading: false })
        ]);
        if (signal.aborted || generation !== roleLoadGeneration) return;
        if (!Array.isArray(loadedRoles) || !Array.isArray(catalog)) {
            showEmpty(rolesList, "دریافت نقش‌ها یا دسترسی‌ها موفق نبود.", "fa-exclamation-circle");
            showEmpty(roleGroups, "برای دریافت دوباره از دکمه تازه‌سازی استفاده کنید.", "fa-exclamation-circle");
            return;
        }
        roles = loadedRoles;
        roleCatalog = catalog;
        renderRoles();
        if (roles.length) await selectRole(roles.find(x => x.id === preferredRoleId)?.id || roles[0].id);
        else renderRolePermissions();
    }

    async function switchMainTab(next) {
        if (next === activeTab || roleSaving || roleMutating || userSaving || userProfileSaving) return;
        if (activeTab === "roles" && !await confirmDiscardRoleChanges()) {
            root.querySelector("[data-main-tab='roles']").focus();
            return;
        }
        if (activeTab === "users" && !await confirmDiscardUserChanges()) {
            root.querySelector("[data-main-tab='users']").focus();
            return;
        }
        if (activeTab === "users" && userSearchTimer !== null) {
            clearTimeout(userSearchTimer);
            userSearchTimer = null;
        }
        activeTab = next;
        for (const tab of root.querySelectorAll("[data-main-tab]")) {
            const active = tab.dataset.mainTab === next;
            tab.classList.toggle("is-active", active);
            tab.setAttribute("aria-selected", String(active));
            tab.tabIndex = active ? 0 : -1;
        }
        for (const panel of ["catalog", "roles", "users"]) {
            root.querySelector("[id='ac" + (panel === "catalog" ? "Catalog" : panel === "roles" ? "Roles" : "Users") + "Panel']").hidden = panel !== next;
        }
        if (next === "roles") await loadRoleTab();
        if (next === "users") await loadUserTab();
    }

    async function switchRole(roleId) {
        if (roleSaving || roleMutating || roleId === currentRoleId || !await confirmDiscardRoleChanges()) return;
        await selectRole(roleId);
    }

    function editRolePermission(input) {
        if (!roleReady || roleSaving || roleMutating) {
            input.checked = !input.checked;
            return;
        }
        if (input.dataset.rolePermissionId) {
            if (input.checked) roleChosen.add(input.dataset.rolePermissionId);
            else roleChosen.delete(input.dataset.rolePermissionId);
        } else if (input.dataset.roleEntityToggle) {
            const entity = roleCatalog.find(x => x.id === input.dataset.roleEntityToggle);
            if (!entity) return;
            for (const permission of entity.permissions) {
                if (input.checked) roleChosen.add(permission.id);
                else roleChosen.delete(permission.id);
            }
            for (const checkbox of roleGroups.querySelectorAll("[data-role-permission-id]")) {
                if (entity.permissions.some(p => p.id === checkbox.dataset.rolePermissionId))
                    checkbox.checked = input.checked;
            }
        }
        syncRoleState();
    }

    async function saveRolePermissions() {
        if (roleSaving || roleMutating || !roleReady || !roleDirty() || !currentRoleId) return;
        roleSaving = true;
        const roleId = currentRoleId;
        try {
            syncRoleState();
            showError("role", "");
            const result = await post("SaveRolePermissions", {
                roleId, permissionIds: [...roleChosen]
            });
            if (signal.aborted || currentRoleId !== roleId) return;
            if (result?.success) {
                roleOriginal = new Set(roleChosen);
                toastifySuccess("دسترسی‌های نقش ذخیره شدند.");
            } else if (!root.querySelector("[data-role-error]").textContent) {
                showError("role", "ذخیره دسترسی‌های نقش انجام نشد.");
            }
        } finally {
            roleSaving = false;
            if (!signal.aborted) syncRoleState();
        }
    }


    async function openRoleEditor(role = null) {
        if (roleSaving || roleMutating || !await confirmDiscardRoleChanges()) return;
        roleEditId = role?.id || null;
        roleForm.reset();
        showError("role-form", "");
        root.querySelector("#acRoleModalTitle").textContent = role ? "ویرایش نام نقش" : "افزودن نقش";
        field(roleForm, "name").value = role?.name || "";
        ModalToggle(roleModal, "show");
    }

    async function saveRole(event) {
        event.preventDefault();
        if (roleSaving || roleMutating || !roleForm.reportValidity()) return;
        const name = field(roleForm, "name").value.trim();
        if (!name || name.length > 100 || name.includes(",") || name.includes("،")) {
            showError("role-form", "نام نقش باید بین ۱ تا ۱۰۰ کاراکتر و بدون ویرگول باشد.");
            return;
        }
        const existing = roleEditId ? roles.find(x => x.id === roleEditId) : null;
        if (roleEditId && !existing) return;
        roleMutating = true;
        const submit = roleForm.querySelector("[type=submit]");
        submit.disabled = true;
        syncRoleState();
        try {
            const response = await post("SaveRole", {
                roleId: roleEditId, expectedName: existing?.name || null, name
            });
            if (signal.aborted) return;
            if (response?.success) {
                ModalToggle(roleModal, "hide");
                toastifySuccess(existing ? "نام نقش ویرایش شد." : "نقش جدید ایجاد شد.");
                await loadRoleTab(response.roleId || existing?.id || null);
            } else if (!root.querySelector("[data-role-form-error]").textContent) {
                showError("role-form", "ذخیره نقش انجام نشد.");
            }
        } finally {
            roleMutating = false;
            submit.disabled = false;
            if (!signal.aborted) syncRoleState();
        }
    }

    // Role deletion decisions are collected per (user, permission), with optional
    // bulk shortcuts. No choice is preselected: the administrator must decide.
    const roleDeleteKey = (userId, permissionId) => JSON.stringify([userId, permissionId]);
    const roleDeletePermissions = () => roleDeletePreview?.users.flatMap(user =>
        user.permissions.map(permission => ({
            userId: user.id, permissionId: permission.id, key: roleDeleteKey(user.id, permission.id)
        }))) || [];

    function updateRoleDeleteSummary() {
        if (!roleDeletePreview) return;
        const entries = roleDeletePermissions();
        const chosen = entries.filter(x => roleDeleteChoices.has(x.key));
        const kept = chosen.filter(x => roleDeleteChoices.get(x.key) === "keep").length;
        const lost = chosen.length - kept;
        root.querySelector("[data-role-delete-progress]").textContent =
            chosen.length + " از " + entries.length + " مجوز تعیین تکلیف شده — " +
            kept + " حفظ، " + lost + " حذف";
        root.querySelector("[data-role-delete-confirm]").disabled =
            roleMutating || chosen.length !== entries.length;
        for (const card of root.querySelectorAll("[data-delete-user]")) {
            const user = roleDeletePreview.users.find(x => x.id === card.dataset.deleteUser);
            if (!user) continue;
            const decided = user.permissions.filter(p =>
                roleDeleteChoices.has(roleDeleteKey(user.id, p.id))).length;
            const count = card.querySelector("[data-delete-user-count]");
            if (count) count.textContent = decided + " از " + user.permissions.length + " تعیین تکلیف شده";
        }
    }

    // Each permission has three states: keep, remove, and not decided.
    function syncRoleDeleteDecisionRow(row) {
        const userId = row.dataset.deleteChoiceUser;
        const permissionId = row.dataset.deleteChoicePermission;
        const state = roleDeleteChoices.get(roleDeleteKey(userId, permissionId)) || "none";
        row.dataset.decision = state;
        for (const button of row.querySelectorAll("[data-delete-choice]"))
            button.setAttribute("aria-pressed", String(button.dataset.deleteChoice === state));
    }

    function chooseRoleDeletePermission(button) {
        if (!roleDeletePreview || roleMutating || button.disabled) return;
        const userId = button.dataset.deleteChoiceUser;
        const permissionId = button.dataset.deleteChoicePermission;
        const person = roleDeletePreview.users.find(user => user.id === userId);
        if (!person?.permissions.some(permission => permission.id === permissionId)) return;
        const key = roleDeleteKey(userId, permissionId);
        const choice = button.dataset.deleteChoice;
        if (roleDeleteChoices.get(key) === choice) roleDeleteChoices.delete(key);
        else roleDeleteChoices.set(key, choice);
        syncRoleDeleteDecisionRow(button.closest(".ac-role-delete-perm"));
        updateRoleDeleteSummary();
    }

    function renderRoleDeleteUsers() {
        const container = root.querySelector("[data-role-delete-users]");
        container.replaceChildren();
        if (!roleDeletePreview) return;
        const term = root.querySelector("[data-role-delete-search]").value.trim().toLocaleLowerCase();
        const visible = roleDeletePreview.users.filter(u =>
            [u.name, u.userName, ...u.permissions.flatMap(p => [p.key, p.title])]
                .some(x => (x || "").toLocaleLowerCase().includes(term)));
        if (!visible.length) {
            showEmpty(container, roleDeletePreview.users.length ?
                "کاربر یا مجوزی مطابق جست‌وجو پیدا نشد." : "هیچ کاربری به این نقش متصل نیست.", "fa-user-shield");
            updateRoleDeleteSummary();
            return;
        }
        for (const user of visible) {
            const card = text("section", null, "ac-role-delete-user");
            card.dataset.deleteUser = user.id;
            const head = text("div", null, "ac-role-delete-user-head");
            const identity = text("div", null, "ac-role-delete-user-identity");
            const avatar = text("span", null, "ac-role-delete-avatar");
            const personIcon = text("i", null, "fas fa-user");
            personIcon.setAttribute("aria-hidden", "true");
            avatar.append(personIcon);
            const titles = text("div", null, "ac-role-delete-user-title");
            titles.append(
                text("span", "کاربر", "ac-role-delete-user-label"),
                text("strong", user.name),
                text("small", user.userName)
            );
            identity.append(avatar, titles);
            const actions = text("div", null, "ac-role-delete-user-actions");
            const count = text("span", null, "ac-role-delete-user-count");
            count.dataset.deleteUserCount = user.id;
            actions.append(count);
            if (user.permissions.length) {
                const buttons = text("div", null, "ac-role-delete-buttons");
                for (const [mode, title] of [["keep", "حفظ همه"], ["remove", "حذف همه"]]) {
                    const button = text("button", title, "ac-btn ac-btn-muted");
                    button.type = "button";
                    button.dataset.deleteUserMode = mode;
                    button.dataset.deleteUserId = user.id;
                    buttons.append(button);
                }
                actions.append(buttons);
            }
            head.append(identity, actions);
            card.append(head);
            if (!user.permissions.length) {
                card.append(text("p", "با حذف این نقش، هیچ‌یک از دسترسی‌های فعلی این کاربر از بین نمی‌رود. تمام دسترسی‌های مشابه این نقش را از نقش دیگری یا به‌صورت اختصاصی در اختیار دارد.", "ac-role-delete-no-loss"));
            } else {
                const grid = text("div", null, "ac-role-delete-perms");
                for (const permission of user.permissions) {
                    const line = text("div", null, "ac-role-delete-perm");
                    line.dataset.deleteChoiceUser = user.id;
                    line.dataset.deleteChoicePermission = permission.id;
                    const desc = text("span", null, "ac-role-delete-perm-text");
                    desc.append(text("strong", permission.title), text("small", permission.key));
                    const actions = text("div", null, "ac-role-delete-choice-actions");
                    actions.setAttribute("role", "group");
                    actions.setAttribute("aria-label", "تعیین تکلیف " + permission.title + " برای " + user.name);
                    for (const [mode, title, iconClass] of [
                        ["keep", "حفظ", "fa-check"], ["remove", "حذف", "fa-times"]
                    ]) {
                        const button = text("button", null, "ac-role-delete-choice");
                        button.type = "button";
                        button.dataset.deleteChoice = mode;
                        button.dataset.deleteChoiceUser = user.id;
                        button.dataset.deleteChoicePermission = permission.id;
                        button.setAttribute("aria-label", title + " مجوز " + permission.title + " برای " + user.name);
                        const icon = text("i", null, "fas " + iconClass);
                        icon.setAttribute("aria-hidden", "true");
                        button.append(icon, text("span", title));
                        actions.append(button);
                    }
                    line.append(desc, actions);
                    syncRoleDeleteDecisionRow(line);
                    grid.append(line);
                }
                card.append(grid);
            }
            container.append(card);
        }
        updateRoleDeleteSummary();
    }

    function assignRoleDeleteChoices(mode, userId = null) {
        if (!roleDeletePreview || roleMutating) return;
        for (const item of roleDeletePermissions()) {
            if (userId && item.userId !== userId) continue;
            if (mode === "reset") roleDeleteChoices.delete(item.key);
            else roleDeleteChoices.set(item.key, mode);
        }
        for (const row of root.querySelectorAll(".ac-role-delete-perm")) {
            if (userId && row.dataset.deleteChoiceUser !== userId) continue;
            syncRoleDeleteDecisionRow(row);
        }
        updateRoleDeleteSummary();
    }

    async function deleteRole(roleId) {
        if (roleSaving || roleMutating) return;
        const role = roles.find(x => x.id === roleId);
        if (!role || !await confirmDiscardRoleChanges()) return;
        const generation = ++roleDeleteGeneration;
        roleMutating = true;
        syncRoleState();
        try {
            const preview = await sendApiRequest({
                url: "/AccessCatalogAdmin/RoleRemovalPreview",
                params: { roleId: role.id }, showLoading: false
            });
            if (signal.aborted || generation !== roleDeleteGeneration) return;
            if (!preview || preview.roleId !== role.id || !Array.isArray(preview.users)) {
                toastifyError("پیش‌نمایش حذف نقش دریافت نشد.");
                return;
            }
            roleDeletePreview = preview;
            roleDeleteChoices = new Map();
            roleDeleteForm.reset();
            showError("role-delete", "");
            root.querySelector("#acRoleDeleteTitle").textContent = "حذف نقش «" + preview.roleName + "»";
            root.querySelector("[data-role-delete-impact]").textContent =
                preview.assignedUserCount + " کاربر دارای این نقش؛ " +
                preview.affectedPermissionCount + " مجوز واقعاً وابسته به آن";
            renderRoleDeleteUsers();
            ModalToggle(roleDeleteModal, "show");
        } finally {
            roleMutating = false;
            if (!signal.aborted) {
                syncRoleState();
                updateRoleDeleteSummary();
            }
        }
    }

    async function submitRoleDeletion(event) {
        event.preventDefault();
        if (roleMutating || !roleDeletePreview) return;
        const entries = roleDeletePermissions();
        if (entries.some(x => !roleDeleteChoices.has(x.key))) {
            showError("role-delete", "ابتدا تمام مجوزهای وابسته را تعیین تکلیف کنید.");
            return;
        }
        const preview = roleDeletePreview;
        roleMutating = true;
        syncRoleState();
        updateRoleDeleteSummary();
        showError("role-delete", "");
        try {
            const result = await post("DeleteRole", {
                roleId: preview.roleId, expectedName: preview.roleName, revision: preview.revision,
                decisions: entries.map(x => ({
                    userId: x.userId, permissionId: x.permissionId,
                    keep: roleDeleteChoices.get(x.key) === "keep"
                }))
            });
            if (signal.aborted) return;
            if (result?.success) {
                ModalToggle(roleDeleteModal, "hide");
                toastifySuccess("نقش حذف شد؛ مجوزهای انتخاب‌شده به کاربران منتقل شدند.");
                await loadRoleTab(preview.roleId === currentRoleId ? null : currentRoleId);
            } else if (!root.querySelector("[data-role-delete-error]").textContent) {
                showError("role-delete", "حذف نقش انجام نشد. مجوزها را بررسی و مجدداً تلاش کنید.");
            }
        } finally {
            roleMutating = false;
            if (!signal.aborted) {
                syncRoleState();
                updateRoleDeleteSummary();
            }
        }
    }

    function renderUserProfileRoles(selectedIds = []) {
        const target = root.querySelector("[data-user-profile-roles]");
        target.replaceChildren();
        const chosen = new Set(selectedIds);
        if (!userAvailableRoles.length) {
            showEmpty(target, "هنوز نقشی تعریف نشده است.", "fa-user-shield");
        } else {
            for (const role of userAvailableRoles) {
                const label = text("label", null, "ac-user-profile-role");
                const input = document.createElement("input");
                input.type = "checkbox";
                input.value = role.id;
                input.dataset.userProfileRole = role.id;
                input.checked = chosen.has(role.id);
                label.append(input, text("span", role.name));
                target.append(label);
            }
        }
        root.querySelector("[data-user-profile-role-count]").textContent =
            target.querySelectorAll("input:checked").length + " انتخاب";
    }

    async function openUserProfileEditor(userId = null) {
        if (userSaving || userLoading || userProfileSaving || activeTab !== "users" ||
            !await confirmDiscardUserChanges()) return;
        const generation = ++userProfileGeneration;
        userProfileId = userId;
        userProfileStamp = null;
        userProfileForm.reset();
        showError("user-profile", "");
        root.querySelector("#acUserProfileModalTitle").textContent = userId ? "ویرایش کاربر" : "افزودن کاربر";
        const password = field(userProfileForm, "password");
        password.required = !userId;
        root.querySelector("[data-user-password-hint]").textContent =
            userId ? "(در صورت عدم تغییر خالی بماند)" : "(برای حساب جدید الزامی است)";
        const [positions, profile] = await Promise.all([
            userPositions ?? sendApiRequest({
                url: "/AccessCatalogAdmin/Positions", showLoading: false
            }),
            userId ? sendApiRequest({
                url: "/AccessCatalogAdmin/UserProfile", params: { userId }, showLoading: false
            }) : Promise.resolve(null)
        ]);
        if (signal.aborted || generation !== userProfileGeneration || activeTab !== "users") return;
        if (!Array.isArray(positions) || (userId && (!profile || profile.id !== userId))) {
            toastifyError("دریافت اطلاعات کاربر یا سمت‌های کاری ناموفق بود.");
            return;
        }
        userPositions = positions;
        const positionSelect = root.querySelector("[data-user-profile-position]");
        positionSelect.replaceChildren(new Option("بدون سمت کاری", ""));
        for (const pos of positions)
            positionSelect.add(new Option(pos.title, pos.id));
        if (profile?.positionId && !positions.some(p => p.id === profile.positionId))
            positionSelect.add(new Option(profile.positionTxt || "سمت ثبت‌شده قبلی", profile.positionId));
        for (const name of ["firstName", "lastName", "userName", "phoneNumber", "email", "internalTels"])
            field(userProfileForm, name).value = profile?.[name] || "";
        positionSelect.value = profile?.positionId || "";
        field(userProfileForm, "isEmployee").checked = profile ? profile.isEmployee : userType === "employee" || userType === "all";
        field(userProfileForm, "isCustomer").checked = profile ? profile.isCustomer : userType === "customer";
        field(userProfileForm, "isSupplier").checked = profile ? profile.isSupplier : userType === "supplier";
        userProfileStamp = profile?.concurrencyStamp || null;
        renderUserProfileRoles(profile?.roleIds || []);
        ModalToggle(userProfileModal, "show");
    }

    async function saveUserProfile(event) {
        event.preventDefault();
        if (userSaving || userProfileSaving || !userProfileForm.reportValidity()) return;
        const isEmployee = field(userProfileForm, "isEmployee").checked;
        const isCustomer = field(userProfileForm, "isCustomer").checked;
        const isSupplier = field(userProfileForm, "isSupplier").checked;
        if (!isEmployee && !isCustomer && !isSupplier) {
            showError("user-profile", "حداقل یکی از انواع حساب را انتخاب کنید.");
            return;
        }
        const userName = field(userProfileForm, "userName").value.trim();
        const firstName = field(userProfileForm, "firstName").value.trim();
        const lastName = field(userProfileForm, "lastName").value.trim();
        const phoneNumber = field(userProfileForm, "phoneNumber").value.trim();
        const roleIds = [...userProfileForm.querySelectorAll("[data-user-profile-role]:checked")].map(x => x.value);
        userProfileSaving = true;
        const submit = userProfileForm.querySelector("[type='submit']");
        submit.disabled = true;
        syncUserState();
        showError("user-profile", "");
        try {
            const response = await post("SaveUserProfile", {
                userId: userProfileId, expectedConcurrencyStamp: userProfileStamp,
                userName, firstName, lastName, phoneNumber,
                email: field(userProfileForm, "email").value.trim(),
                internalTels: field(userProfileForm, "internalTels").value.trim(),
                password: field(userProfileForm, "password").value,
                isEmployee, isCustomer, isSupplier,
                positionId: field(userProfileForm, "positionId").value || null,
                roleIds
            });
            if (signal.aborted) return;
            if (response?.success) {
                ModalToggle(userProfileModal, "hide");
                userProfileForm.reset();
                toastifySuccess(userProfileId ? "اطلاعات کاربر و نقش‌ها ویرایش شد." : "کاربر جدید ثبت شد.");
                // Show the saved user even when their account type or position changed.
                if ((userType === "employee" && !isEmployee) ||
                    (userType === "customer" && !isCustomer) ||
                    (userType === "supplier" && !isSupplier)) {
                    userType = "all";
                    userTypeSelect.value = "all";
                }
                root.querySelector("[data-users-search]").value = userName;
                await loadUsersPage(1, response.userId || null);
            } else if (!root.querySelector("[data-user-profile-error]").textContent) {
                showError("user-profile", "ثبت اطلاعات کاربر انجام نشد.");
            }
        } finally {
            userProfileSaving = false;
            submit.disabled = false;
            if (!signal.aborted) syncUserState();
        }
    }

    // User permissions are NOT copied from roles. Effective access is computed from
    // current role memberships + the live role grants, then overridden per user.
    const selectedUser = () => userPageItems.find(x => x.id === currentUserId);
    const userDirty = () =>
        userRoleIds.size !== userOriginalRoles.size ||
        [...userRoleIds].some(id => !userOriginalRoles.has(id)) ||
        userOverrides.size !== userOriginalOverrides.size ||
        [...userOverrides].some(([id, allowed]) => userOriginalOverrides.get(id) !== allowed);

    function effectivePermission(permissionId) {
        const fromRoles = userAvailableRoles.filter(role =>
            userRoleIds.has(role.id) && userGrantSources.get(permissionId)?.has(role.id));
        const custom = userOverrides.get(permissionId);
        return {
            allowed: custom === undefined ? fromRoles.length > 0 : custom,
            state: custom === false ? "محدودیت اختصاصی" :
                   custom === true ? "اجازه اختصاصی" :
                   fromRoles.length ? "مجاز از نقش" : "بدون دسترسی",
            sources: fromRoles.map(role => role.name),
            isCustom: custom !== undefined
        };
    }

    function renderUsers() {
        const list = usersList;
        list.replaceChildren();
        root.querySelector("[data-users-count]").textContent = String(userCount);
        root.querySelector("[data-users-page]").textContent = "صفحه " + userPage + " از " + userPages;
        root.querySelector("[data-action='user-prev']").disabled = userLoading || userSaving || userPage <= 1;
        root.querySelector("[data-action='user-next']").disabled = userLoading || userSaving || userPage >= userPages;
        if (!userPageItems.length) {
            showEmpty(list, userCount ? "کاربری در این صفحه یافت نشد." : "کاربری مطابق جست‌وجو یافت نشد.", "fa-users");
            return;
        }
        for (const person of userPageItems) {
            const button = text("button", null, "ac-user-item" + (person.id === currentUserId ? " is-selected" : ""));
            button.type = "button";
            button.dataset.selectUser = person.id;
            button.setAttribute("aria-pressed", String(person.id === currentUserId));
            button.append(text("strong", person.name),
                text("small", [person.position || (person.isEmployee ? "سمت کاری ثبت نشده" : ""), person.userName].filter(Boolean).join(" · ")));
            const kinds = [
                person.isEmployee && "کارمند",
                person.isCustomer && "مشتری",
                person.isSupplier && "تأمین‌کننده"
            ].filter(Boolean);
            if (kinds.length) {
                const tags = text("span", null, "ac-user-kind-tags");
                for (const kind of kinds) tags.append(text("span", kind, "ac-user-kind-tag"));
                button.append(tags);
            }
            list.append(button);
        }
    }

    function renderUserRoles() {
        const target = root.querySelector("[data-user-roles]");
        target.replaceChildren();
        if (!userReady) {
            showEmpty(target, currentUserId ? "در حال دریافت نقش‌های کاربر..." : "یک کاربر انتخاب کنید.", "fa-user-tag");
            return;
        }
        if (!userAvailableRoles.length) {
            showEmpty(target, "نقشی در سیستم تعریف نشده است.", "fa-user-tag");
            return;
        }
        for (const role of userAvailableRoles) {
            const label = text("label", null, "ac-user-role-choice" + (userRoleIds.has(role.id) ? " is-selected" : ""));
            const input = document.createElement("input");
            input.type = "checkbox";
            input.dataset.userRoleId = role.id;
            input.checked = userRoleIds.has(role.id);
            input.disabled = userSaving;
            label.append(input, text("span", role.name));
            target.append(label);
        }
    }

    function syncUserPermissionRows() {
        for (const row of userPermissions.querySelectorAll("[data-user-permission-id]")) {
            const id = row.dataset.userPermissionId;
            const effective = effectivePermission(id);
            const status = row.querySelector("[data-user-effective]");
            status.textContent = effective.state;
            status.classList.toggle("is-allowed", effective.allowed);
            status.classList.toggle("is-denied", !effective.allowed);
            row.querySelector("[data-user-sources]").textContent =
                effective.sources.length ? "از نقش: " + effective.sources.join("، ") : "بدون مجوز از نقش‌ها";
            row.classList.toggle("is-custom", effective.isCustom);
            row.querySelector("[data-user-override-id]").disabled = !userReady || userSaving;
        }
        for (const group of userPermissions.querySelectorAll("[data-user-entity]")) {
            const entity = userCatalog.find(x => x.id === group.dataset.userEntity);
            if (!entity) continue;
            const granted = entity.permissions.filter(p => effectivePermission(p.id).allowed).length;
            group.querySelector("[data-user-entity-count]").textContent =
                granted + " از " + entity.permissions.length + " مجوز مؤثر";
        }
    }

    function syncUserState() {
        const person = selectedUser();
        root.querySelector("[data-user-title]").textContent = person ? "دسترسی‌های " + person.name : "دسترسی‌های کاربر";
        root.querySelector("[data-user-position]").textContent = person ?
            "سمت کاری: " + (person.position || "ثبت نشده") : "برای شروع یک کاربر انتخاب کنید.";
        root.querySelector("[data-user-subtitle]").textContent = !person ? "" :
            !userReady ? "در حال دریافت تنظیمات..." :
            userDirty() ? "تغییرات ذخیره‌نشده" : "تغییرات ذخیره شده‌اند.";
        root.querySelector("[data-user-roles-count]").textContent = userRoleIds.size + " نقش";
        const total = userCatalog.reduce((sum, e) =>
            sum + e.permissions.filter(p => effectivePermission(p.id).allowed).length, 0);
        root.querySelector("[data-user-effective-count]").textContent = total + " مجوز مؤثر";
        root.querySelector("[data-action='save-user']").disabled = !userReady || userSaving || !userDirty();
        root.querySelector("[data-action='reset-user']").disabled = !userReady || userSaving || !userDirty();
        root.querySelector("[data-action='user-clear-overrides']").disabled = !userReady || userSaving || !userOverrides.size;
        root.querySelector("[data-users-search]").disabled = userSaving || userProfileSaving;
        root.querySelector("[data-action='new-user']").disabled = userSaving || userProfileSaving;
        root.querySelector("[data-action='edit-user']").disabled = !person || userSaving || userProfileSaving;
        userTypeSelect.disabled = userSaving || userLoading || userProfileSaving;
        for (const checkbox of root.querySelectorAll("[data-user-role-id]"))
            checkbox.disabled = !userReady || userSaving;
        for (const label of root.querySelectorAll(".ac-user-role-choice"))
            label.classList.toggle("is-selected", userRoleIds.has(label.querySelector("[data-user-role-id]").dataset.userRoleId));
        syncUserPermissionRows();
    }

    function renderUserPermissions() {
        userPermissions.replaceChildren();
        if (!currentUserId) {
            showEmpty(userPermissions, "یک کاربر را از فهرست انتخاب کنید.", "fa-user-shield");
            syncUserState();
            return;
        }
        if (!userReady) {
            showEmpty(userPermissions, "در حال دریافت مجوزهای کاربر...", "fa-spinner");
            syncUserState();
            return;
        }
        const term = root.querySelector("[data-user-permission-search]").value.trim().toLocaleLowerCase();
        const onlyCustom = root.querySelector("[data-user-only-overrides]").checked;
        let shown = 0;
        for (const entity of userCatalog) {
            const wholeEntity = [entity.title, entity.key].some(x => x.toLocaleLowerCase().includes(term));
            const visible = entity.permissions.filter(permission =>
                (!onlyCustom || userOverrides.has(permission.id)) &&
                (wholeEntity || [permission.title, permission.key].some(x => x.toLocaleLowerCase().includes(term))));
            if (!visible.length) continue;
            shown++;
            const section = text("section", null, "ac-role-group");
            section.dataset.userEntity = entity.id;
            const header = text("div", null, "ac-role-group-header");
            const name = text("span", null, "ac-role-group-all");
            name.append(text("strong", entity.title), text("small", entity.key));
            const count = text("span", null, "ac-role-group-count");
            count.dataset.userEntityCount = entity.id;
            header.append(name, count);
            const grid = text("div", null, "ac-user-permission-grid");
            for (const permission of visible) {
                const item = text("div", null, "ac-user-permission-item");
                item.dataset.userPermissionId = permission.id;
                const title = text("div", null, "ac-user-permission-title");
                title.append(text("strong", permission.title), text("small", permission.key));
                const controls = text("div", null, "ac-user-permission-row");
                const select = document.createElement("select");
                select.className = "ac-user-override-select";
                select.dataset.userOverrideId = permission.id;
                select.setAttribute("aria-label", "وضعیت اختصاصی " + permission.title + " در " + entity.title);
                select.append(new Option("از نقش‌ها", "inherit"),
                    new Option("اجازه اختصاصی", "allow"),
                    new Option("محدودیت اختصاصی", "deny"));
                const override = userOverrides.get(permission.id);
                select.value = override === undefined ? "inherit" : override ? "allow" : "deny";
                const effective = text("span", null, "ac-user-effective");
                effective.dataset.userEffective = permission.id;
                controls.append(select, effective);
                const sources = text("small", null, "ac-user-sources");
                sources.dataset.userSources = permission.id;
                item.append(title, controls, sources);
                grid.append(item);
            }
            section.append(header, grid);
            userPermissions.append(section);
        }
        if (!shown) {
            showEmpty(userPermissions,
                onlyCustom && !userOverrides.size ? "برای این کاربر هنوز دسترسی اختصاصی تعریف نشده است." :
                "مجوز مطابق فیلتر پیدا نشد.", "fa-key");
        }
        syncUserState();
    }

    async function selectUser(userId) {
        if (!userPageItems.some(x => x.id === userId)) return;
        const generation = ++userSelectionGeneration;
        currentUserId = userId;
        userReady = false;
        userRoleIds = new Set();
        userOverrides = new Map();
        userOriginalRoles = new Set();
        userOriginalOverrides = new Map();
        showError("user", "");
        renderUsers();
        renderUserRoles();
        renderUserPermissions();
        const result = await sendApiRequest({
            url: "/AccessCatalogAdmin/UserAccess",
            params: { userId }, showLoading: false
        });
        if (signal.aborted || generation !== userSelectionGeneration || currentUserId !== userId || activeTab !== "users") return;
        if (!result || !Array.isArray(result.roleIds) || !Array.isArray(result.overrides)) {
            showEmpty(userPermissions, "دریافت تنظیمات کاربر ناموفق بود. دوباره تلاش کنید.", "fa-exclamation-circle");
            return;
        }
        const available = new Set(userAvailableRoles.map(x => x.id));
        const active = new Set(userCatalog.flatMap(x => x.permissions.map(p => p.id)));
        userRoleIds = new Set(result.roleIds.filter(id => available.has(id)));
        userOverrides = new Map(result.overrides.filter(x => active.has(x.accessPermissionId))
            .map(x => [x.accessPermissionId, x.isAllowed]));
        userOriginalRoles = new Set(userRoleIds);
        userOriginalOverrides = new Map(userOverrides);
        userReady = true;
        renderUserRoles();
        renderUserPermissions();
    }

    async function loadUsersPage(pageNumber, keepUser = currentUserId) {
        const generation = ++userPageGeneration;
        ++userSelectionGeneration;
        userLoading = true;
        userReady = false;
        userTypeSelect.disabled = true;
        root.querySelector("[data-action='user-prev']").disabled = true;
        root.querySelector("[data-action='user-next']").disabled = true;
        showEmpty(usersList, "در حال دریافت کاربران...", "fa-spinner");
        const data = await sendApiRequest({
            url: "/AccessCatalogAdmin/Users",
            params: { search: root.querySelector("[data-users-search]").value.trim(), page: pageNumber, type: userType },
            showLoading: false
        });
        if (signal.aborted || generation !== userPageGeneration || activeTab !== "users") return;
        userLoading = false;
        userTypeSelect.disabled = userSaving;
        if (!data || !Array.isArray(data.items)) {
            showEmpty(usersList, "دریافت کاربران ناموفق بود. دوباره تلاش کنید.", "fa-exclamation-circle");
            syncUserState();
            return;
        }
        userPageItems = data.items;
        userPage = data.page;
        userPages = Math.max(1, data.totalPages);
        userCount = data.total;
        userSearchTerm = root.querySelector("[data-users-search]").value;
        const id = userPageItems.some(x => x.id === keepUser) ? keepUser : userPageItems[0]?.id;
        currentUserId = null;
        renderUsers();
        if (id) await selectUser(id);
        else {
            userRoleIds = new Set();
            userOverrides = new Map();
            userOriginalRoles = new Set();
            userOriginalOverrides = new Map();
            renderUserRoles();
            renderUserPermissions();
        }
    }

    async function loadUserTab() {
        const generation = ++userTabGeneration;
        ++userPageGeneration;
        ++userSelectionGeneration;
        userReady = false;
        showError("user", "");
        renderUserRoles();
        syncUserState();
        showEmpty(usersList, "در حال دریافت کاربران...", "fa-spinner");
        showEmpty(userPermissions, "در حال دریافت دسترسی‌ها...", "fa-spinner");
        const [allRoles, catalog, grants] = await Promise.all([
            sendApiRequest({ url: "/AccessCatalogAdmin/Roles", showLoading: false }),
            sendApiRequest({ url: "/AccessCatalogAdmin/RoleCatalog", showLoading: false }),
            sendApiRequest({ url: "/AccessCatalogAdmin/RoleGrants", showLoading: false })
        ]);
        if (signal.aborted || generation !== userTabGeneration || activeTab !== "users") return;
        if (!Array.isArray(allRoles) || !Array.isArray(catalog) || !Array.isArray(grants)) {
            showEmpty(userPermissions, "دریافت نقش‌ها یا مجوزها ناموفق بود. دوباره تلاش کنید.", "fa-exclamation-circle");
            return;
        }
        userAvailableRoles = allRoles;
        userCatalog = catalog;
        userGrantSources = new Map();
        for (const grant of grants) {
            if (!userGrantSources.has(grant.accessPermissionId))
                userGrantSources.set(grant.accessPermissionId, new Set());
            userGrantSources.get(grant.accessPermissionId).add(grant.roleId);
        }
        await loadUsersPage(userPage);
    }

    async function confirmDiscardUserChanges() {
        if (!userReady || !userDirty()) return true;
        return !!(await confirmAlert({
            title: "تغییرات ذخیره نشده",
            text: "تغییرات نقش‌ها و مجوزهای این کاربر ذخیره نشده‌اند. ادامه می‌دهید؟",
            confirmText: "ادامه بدون ذخیره", cancelText: "انصراف", icon: "warning"
        }));
    }

    async function switchUser(userId) {
        if (userSaving || userLoading || currentUserId === userId) return;
        if (!await confirmDiscardUserChanges()) return;
        await selectUser(userId);
    }

    async function turnUsersPage(direction) {
        if (userSaving || userLoading || !await confirmDiscardUserChanges()) return;
        const next = userPage + direction;
        if (next < 1 || next > userPages) return;
        await loadUsersPage(next, null);
    }

    async function reloadUsers() {
        if (userSaving || !await confirmDiscardUserChanges()) return;
        await loadUserTab();
    }

    function setUserOverride(id, value) {
        if (!userReady || userSaving) return;
        if (value === "inherit") userOverrides.delete(id);
        else userOverrides.set(id, value === "allow");
        if (root.querySelector("[data-user-only-overrides]").checked)
            renderUserPermissions();
        else
            syncUserState();
    }

    async function saveUserAccess() {
        if (userSaving || !userReady || !userDirty() || !currentUserId) return;
        userSaving = true;
        const userId = currentUserId;
        try {
            syncUserState();
            showError("user", "");
            const result = await post("SaveUserAccess", {
                userId, roleIds: [...userRoleIds],
                overrides: [...userOverrides].map(([accessPermissionId, isAllowed]) =>
                    ({ accessPermissionId, isAllowed }))
            });
            if (signal.aborted || currentUserId !== userId) return;
            if (result?.success) {
                userOriginalRoles = new Set(userRoleIds);
                userOriginalOverrides = new Map(userOverrides);
                const person = selectedUser();
                if (person) person.roles = userAvailableRoles.filter(r => userRoleIds.has(r.id)).map(r => r.name);
                renderUsers();
                toastifySuccess("نقش‌ها و دسترسی‌های اختصاصی کاربر ذخیره شدند.");
            } else if (!root.querySelector("[data-user-error]").textContent) {
                showError("user", "ذخیره تنظیمات کاربر انجام نشد.");
            }
        } finally {
            userSaving = false;
            if (!signal.aborted) {
                renderUsers();
                syncUserState();
            }
        }
    }

    root.addEventListener("click", event => {
        const tab = event.target.closest("[data-main-tab]");
        if (tab) { void switchMainTab(tab.dataset.mainTab); return; }
        const role = event.target.closest("[data-select-role]");
        if (role) { void switchRole(role.dataset.selectRole); return; }
        const person = event.target.closest("[data-select-user]");
        if (person) { void switchUser(person.dataset.selectUser); return; }
        const all = event.target.closest("[data-delete-all]");
        if (all) { assignRoleDeleteChoices(all.dataset.deleteAll); return; }
        const userBulk = event.target.closest("[data-delete-user-mode]");
        if (userBulk) {
            assignRoleDeleteChoices(userBulk.dataset.deleteUserMode, userBulk.dataset.deleteUserId);
            return;
        }
        const permissionChoice = event.target.closest(".ac-role-delete-choice[data-delete-choice]");
        if (permissionChoice) { chooseRoleDeletePermission(permissionChoice); return; }
        const target = event.target.closest("[data-action]");
        if (!target) {
            const select = event.target.closest("[data-select-entity]");
            if (select) void selectEntity(select.dataset.selectEntity);
            return;
        }
        if (target.disabled) return;
        const action = target.dataset.action;
        if (action === "new-user") void openUserProfileEditor();
        else if (action === "edit-user") void openUserProfileEditor(currentUserId);
        else if (action === "reload-users") void reloadUsers();
        else if (action === "user-prev") void turnUsersPage(-1);
        else if (action === "user-next") void turnUsersPage(1);
        else if (action === "save-user") void saveUserAccess();
        else if (action === "reset-user") {
            userRoleIds = new Set(userOriginalRoles);
            userOverrides = new Map(userOriginalOverrides);
            renderUserRoles();
            renderUserPermissions();
        }
        else if (action === "user-clear-overrides") {
            userOverrides.clear();
            renderUserPermissions();
        }
        else if (action === "reload-roles") {
            void (async () => {
                if (!roleSaving && !roleMutating && await confirmDiscardRoleChanges()) await loadRoleTab();
            })();
        }
        else if (action === "new-role") void openRoleEditor();
        else if (action === "edit-role") void openRoleEditor(roles.find(x => x.id === target.dataset.id));
        else if (action === "delete-role") void deleteRole(target.dataset.id);
        else if (action === "save-role") void saveRolePermissions();
        else if (action === "reset-role") {
            roleChosen = new Set(roleOriginal);
            renderRolePermissions();
        }
        else if (action === "new-entity") openEntityEditor();
        else if (action === "edit-entity") openEntityEditor(entities.find(x => x.id === target.dataset.id));
        else if (action === "delete-entity") void deleteItem("Entity", target.dataset.id);
        else if (action === "new-permission") openPermissionEditor();
        else if (action === "preset-permission") {
            openPermissionEditor();
            field(permissionForm, "key").value = target.dataset.key;
            field(permissionForm, "title").value = target.dataset.title;
            updateKeyPreview();
        }
        else if (action === "edit-permission") openPermissionEditor(permissions.find(x => x.id === target.dataset.id));
        else if (action === "delete-permission") void deleteItem("Permission", target.dataset.id);
        else if (action === "reload") void loadEntities();
    }, { signal });
    root.querySelector(".ac-main-tabs").addEventListener("keydown", event => {
        const tab = event.target.closest("[data-main-tab]");
        if (!tab || !["ArrowRight", "ArrowLeft", "Home", "End"].includes(event.key)) return;
        event.preventDefault();
        const tabs = [...root.querySelectorAll("[data-main-tab]")];
        const position = tabs.indexOf(tab);
        const nextIndex = event.key === "Home" ? 0 :
            event.key === "End" ? tabs.length - 1 :
            (position + (event.key === "ArrowLeft" ? 1 : -1) + tabs.length) % tabs.length;
        tabs[nextIndex].focus();
        void switchMainTab(tabs[nextIndex].dataset.mainTab);
    }, { signal });

    root.querySelector("[data-entity-search]").addEventListener("input", renderEntities, { signal });
    root.querySelector("[data-role-search]").addEventListener("input", renderRoles, { signal });
    root.querySelector("[data-role-permission-search]").addEventListener("input", renderRolePermissions, { signal });
    root.querySelector("[data-role-delete-search]").addEventListener("input", renderRoleDeleteUsers, { signal });
    roleDeleteForm.addEventListener("submit", event => { void submitRoleDeletion(event); }, { signal });
    roleDeleteModal.addEventListener("hidden.bs.modal", () => {
        ++roleDeleteGeneration;
        roleDeletePreview = null;
        roleDeleteChoices = new Map();
        roleDeleteForm.reset();
    }, { signal });
    userTypeSelect.addEventListener("change", () => {
        const nextType = userTypeSelect.value;
        if (nextType === userType) return;
        if (userSearchTimer !== null) {
            clearTimeout(userSearchTimer);
            userSearchTimer = null;
        }
        void (async () => {
            if (userSaving || userLoading || !await confirmDiscardUserChanges()) {
                userTypeSelect.value = userType;
                return;
            }
            userType = nextType;
            await loadUsersPage(1, null);
        })();
    }, { signal });
    root.querySelector("[data-users-search]").addEventListener("input", () => {
        if (userSearchTimer !== null) clearTimeout(userSearchTimer);
        userSearchTimer = setTimeout(() => {
            userSearchTimer = null;
            if (activeTab !== "users" || signal.aborted) return;
            void (async () => {
                if (!await confirmDiscardUserChanges()) {
                    root.querySelector("[data-users-search]").value = userSearchTerm;
                    return;
                }
                await loadUsersPage(1, null);
            })();
        }, 260);
    }, { signal });
    root.querySelector("[data-user-permission-search]").addEventListener("input", renderUserPermissions, { signal });
    root.querySelector("[data-user-only-overrides]").addEventListener("change", renderUserPermissions, { signal });
    root.addEventListener("change", event => {
        if (event.target.matches("[data-role-permission-id], [data-role-entity-toggle]"))
            editRolePermission(event.target);
        if (event.target.matches("[data-user-role-id]")) {
            if (!userReady || userSaving) return;
            const roleId = event.target.dataset.userRoleId;
            if (event.target.checked) userRoleIds.add(roleId);
            else userRoleIds.delete(roleId);
            syncUserState();
        }
        if (event.target.matches("[data-user-override-id]"))
            setUserOverride(event.target.dataset.userOverrideId, event.target.value);
    }, { signal });
    modelInput.addEventListener("change", () => {
        if (entityEditId || field(entityForm, "key").value) return;
        const name = modelInput.value.split("|")[1]?.split(".").pop();
        if (name) field(entityForm, "key").value = name;
    }, { signal });
    field(permissionForm, "key").addEventListener("input", updateKeyPreview, { signal });
    entityForm.addEventListener("submit", event => { void saveEntity(event); }, { signal });
    permissionForm.addEventListener("submit", event => { void savePermission(event); }, { signal });
    roleForm.addEventListener("submit", event => { void saveRole(event); }, { signal });
    userProfileForm.addEventListener("submit", event => { void saveUserProfile(event); }, { signal });
    root.querySelector("[data-user-profile-roles]").addEventListener("change", () => {
        root.querySelector("[data-user-profile-role-count]").textContent =
            userProfileForm.querySelectorAll("[data-user-profile-role]:checked").length + " انتخاب";
    }, { signal });
    userProfileModal.addEventListener("hidden.bs.modal", () => {
        ++userProfileGeneration;
        userProfileForm.reset();
        userProfileId = null;
        userProfileStamp = null;
    }, { signal });

    void (async () => {
        await loadModels();
        if (!signal.aborted) await loadEntities(false);
    })();
})();
