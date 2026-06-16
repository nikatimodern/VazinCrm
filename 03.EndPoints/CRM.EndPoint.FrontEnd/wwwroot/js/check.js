let checksDataTable = null;
let checkData = {};

let checkbooksSection = document.getElementById("checkbooks-section");
let checkSection = document.getElementById("checks-section");
document.addEventListener("DOMContentLoaded", () => {
    const { cBook, cBookName } = getUrlParams();
    if (cBook) showChecks(cBook, decodeURIComponent(cBookName || "دسته چک"));
    
    window.addEventListener("popstate", () => {
        const { cBook, cBookName } = getUrlParams();
        if (cBook) {
            showChecks(cBook, decodeURIComponent(cBookName || "دسته چک"));
        } else {
            checkbooksSection.classList.remove("d-none");
            checkSection.classList.add("d-none");
        }
    });
});

const GetAllCheckbooks = async () => {
    try {
        FLshowLoading();
        const res = await axios(`/Check/GetAllCheckbooksPartial`);
        document.getElementById("checkbooksContainer").innerHTML = res.data;
        
    } catch (err) {
        console.log(err);
        document.getElementById("checkbooksContainer").innerHTML = "خطای دریافت اطلاعات";
    } finally {
        FLshowLoading(false);
    }
};

async function showNewCheckbookModal() {
    await getAllBankAccounts([{ element: "#checkbookModal select[name='bankAccountId']", type: [1] }]);
    ModalToggle("checkbookModal", "show");
}
function cancelNewCheckbookModal() {
    mySer.emptyForm("checkbookForm");
    ModalToggle("checkbookModal", "hide");
}

const submitCheckbookModal = async () => {
    if (!await mySer.CheckValidForm("checkbookForm")) return;
    const model = mySer.convertFormToJSON("checkbookForm");
    
    try {
        FLshowLoading();
        const res = await axios.post(`/Check/AddCheckbook`, model);
        if (res.status === 200) {
            toastifySuccess("با موفقیت ثبت شد");
            await GetAllCheckbooks();
        }
    } catch (err) {
        console.log(err);
        toastifyError("خطای سرور");
    } finally {
        FLshowLoading(false);
    }
    
    cancelNewCheckbookModal();
};

function initCheckbookTable(id) {
    let checkStatuses;
    checksDataTable = $("#checksTable").DataTable({
        paging: false,
        scrollCollapse: true,
        scrollY: "calc(100dvh - 400px)",
        responsive: true,
        ordering: false,
        searching: false,
        ajax: {
            url: `/Check/GetCheckListByCheckbookId?checkbookId=${id}`,
            dataSrc: json => {
                checkStatuses = json.statuses || [];
                return json.data;
            },
        },
        columns: [
            { data: "serialNumber" },
            { data: "sayyadNumber" },
            {
                data: "amount",
                render: data => `<span class="direction-ltr">${data.toLocaleString()}</span>`,
            },
            { data: "dueDate" },
            { data: "payee" },
            { data: "description", render: data => data || "-" },
            /*{
                data: "statusTxt",
                render: data => `<span class="badge badge-gray d-inline-flex shadow-none">${data || "-"}</span>`,
            },*/
            {
                data: "statusTxt",
                render: (data, type, row) => `
                    <div class="check-status-wrapper d-inline-flex justify-content-center" data-id="${row.id}">
                        <button type="button" class="check-status-btn badge ${getCheckStatusBadgeClass(row.status)} align-items-center">
                            <span class="check-status-text">${data || "-"}</span>
                            <svg style="width: 14px" viewBox="0 0 16 16" fill="currentColor"><path d="M13.488 2.513a1.75 1.75 0 0 0-2.475 0L6.75 6.774a2.75 2.75 0 0 0-.596.892l-.848 2.047a.75.75 0 0 0 .98.98l2.047-.848a2.75 2.75 0 0 0 .892-.596l4.261-4.262a1.75 1.75 0 0 0 0-2.474Z" /><path d="M4.75 3.5c-.69 0-1.25.56-1.25 1.25v6.5c0 .69.56 1.25 1.25 1.25h6.5c.69 0 1.25-.56 1.25-1.25V9A.75.75 0 0 1 14 9v2.25A2.75 2.75 0 0 1 11.25 14h-6.5A2.75 2.75 0 0 1 2 11.25v-6.5A2.75 2.75 0 0 1 4.75 2H7a.75.75 0 0 1 0 1.5H4.75Z" /></svg>
                        </button>
            
                        <select class="check-status-select form-select form-select-sm d-none">
                            ${checkStatuses.map(status => `
                                <option
                                    value="${status.value}"
                                    ${status.value === row.status ? "selected" : ""}>
                                    ${status.text}
                                </option>
                            `).join("")}
                        </select>
                    </div>
                `,
            },
            {
                data: "id",
                render: data => `
                    <div class="d-inline-flex align-items-center justify-content-center gap-1">
                        <button type="button" style="padding: 6px 6px;box-shadow: none;font-size: 12px" class="buttonGreen3 flex-center" onclick="editCheck('${data}')"><i class="fa fa-pencil"></i></button>
                        <button type="button" style="padding: 6px 7px;box-shadow: none;font-size: 12px" class="buttonCancel2 flex-center" onclick="deleteCheck('${data}')"><i class="fa fa-trash"></i></button>
                    </div>
                `,
            },
        ],
        language: languageDatatable,
        layout: null,
        initComplete: () => {
            document.querySelectorAll("#checksTable_wrapper > div.row").forEach((row) => {
                row.classList.add("d-flex", "flex-wrap");
                row.classList.remove("row");
            });
        },
    });
    
    
}

function getCheckStatusBadgeClass(status) {
    switch (status) {
        case 1:
            return "badge-gray";
        case 2:
            return "badge-purple";
        case 3:
            return "badge-green";
        case 4:
            return "badge-red";
        case 5:
            return "badge-orange";
        default:
            return "badge-gray";
    }
}
document.addEventListener("click", e => {
    const btn = e.target.closest(".check-status-btn");
    
    if (btn) {
        document.querySelectorAll(".check-status-select:not(.d-none)").forEach(select => {
            select.classList.add("d-none");
            select.previousElementSibling.classList.remove("d-none");
        });
        
        btn.classList.add("d-none");
        
        const select = btn.nextElementSibling;
        
        select.classList.remove("d-none");
        select.focus();
        
        return;
    }
    
    document.querySelectorAll(".check-status-select:not(.d-none)").forEach(select => {
        
        if (select.contains(e.target)) return;
        
        select.classList.add("d-none");
        select.previousElementSibling.classList.remove("d-none");
    });
});


document.addEventListener("change", async e => {
    
    if (!e.target.classList.contains("check-status-select")) return;
    
    const select = e.target;
    const wrapper = select.closest(".check-status-wrapper");
    
    const id = wrapper.dataset.id;
    const status = Number(select.value);
    
    select.disabled = true;
    try {
        const res = await axios(`/Check/ChangeStatus?id=${id}&status=${status}`);
        if (res.status !== 200) return;
        
        const text = select.options[select.selectedIndex].text;
        
        const badge = wrapper.querySelector(".check-status-btn");
        
        badge.querySelector(".check-status-text").textContent = text;
        
        badge.classList.remove("badge-gray", "badge-blue", "badge-purple", "badge-green", "badge-red", "badge-orange");
        
        badge.classList.add(getCheckStatusBadgeClass(status));
        
        select.classList.add("d-none");
        badge.classList.remove("d-none");
        
    } catch (err) {
        console.log(err);
        toastifyError("خطا در ثبت وضعیت");
        
    } finally {
        select.disabled = false;
    }
});


function showChecks(id, checkbookName) {
    checkbooksSection.classList.add("d-none");
    checkSection.querySelector("#selected-checkbook-title").innerText = "لیست چک‌های بانک " + checkbookName || "";
    checkSection.classList.remove("d-none");
    
    checkData.checkbookId = id;
    setUrlParams({ cBook: id, cBookName: checkbookName }, { replace: false });
    
    if (!checksDataTable) {
        initCheckbookTable(id);
    } else {
        checksDataTable.clear().draw();
        checksDataTable.ajax.url(`/Check/GetCheckListByCheckbookId?checkbookId=${id}`).load();
    }
}


function backToCheckbooks() {
    checkData = {};
    setUrlParams({ cBook: null, cBookName: null }, { replace: false });
    checkSection.classList.add("d-none");
    checkbooksSection.classList.remove("d-none");
}


function openNewCheckModal() {
    ModalToggle("newCheckModal");
}

async function submitCheckForm() {
    if (!await mySer.CheckValidForm("newCheckForm")) return;
    const model = {
        ...checkData,
        ...mySer.convertFormToJSON("newCheckForm"),
    };
    try {
        FLshowLoading();
        const res = await axios.post(`/Check/AddCheck`, model);
        if (res.status === 200) {
            toastifySuccess("با موفقیت ثبت شد");
            cancelNewCheckModal();
            checksDataTable.ajax.reload();
            await GetAllCheckbooks();
        }
    } catch (err) {
        console.log(err);
        toastifyError("خطای سرور");
    } finally {
        FLshowLoading(false);
    }
}


function cancelNewCheckModal() {
    ModalToggle("newCheckModal", "hide");
    mySer.emptyForm("newCheckForm", [{ name: "status", val: "1" }]);
}

const editCheck = async (id) => {
    checkData = await getCheck(id);
    if (!checkData) return;
    
    mySer.convertJsonToForm(checkData, "newCheckForm");
    openNewCheckModal();
};
const getCheck = async (id) => {
    try {
        const res = await axios(`/Check/GetCheck/${id}`);
        
        return res.status === 200 ? res.data : null;
    } catch (err) {
        console.log(err);
        toastifyError("خطا در دریافت اطلاعات");
        return null;
    }
};

const deleteCheck = async (id) => {
    Swal.fire({
        title: "حذف!",
        text: "آیا مطمئن هستید که میخواهید حذف کنید؟",
        icon: "warning",
        showCancelButton: true,
        cancelButtonColor: "#adadad",
        confirmButtonColor: "#e64942",
        cancelButtonText: "انصراف",
        confirmButtonText: "بله، حذف شود",
    }).then(async (willDelete) => {
        if (willDelete.isConfirmed) {
            FLshowLoading();
            const res = await axios(`/Check/DeleteCheck/${id}`);
            if (res.status === 200) {
                toastifySuccess("با موفقیت حذف شد");
                checksDataTable.ajax.reload();
                await GetAllCheckbooks();
            }
            FLshowLoading(false);
        }
    });
};
const deleteCheckbook = async (id) => {
    checkbookDropdown.close();
    Swal.fire({
        title: "حذف!",
        text: "آیا مطمئن هستید که میخواهید حذف کنید؟",
        icon: "warning",
        showCancelButton: true,
        cancelButtonColor: "#adadad",
        confirmButtonColor: "#e64942",
        cancelButtonText: "انصراف",
        confirmButtonText: "بله، حذف شود",
    }).then(async (willDelete) => {
        if (willDelete.isConfirmed) {
            FLshowLoading();
            const res = await axios(`/Check/DeleteCheckbook/${id}`);
            if (res.status === 200) {
                toastifySuccess("با موفقیت حذف شد");
                await GetAllCheckbooks();
            }
            FLshowLoading(false);
        }
    });
};


document.addEventListener("click", (e) => {
    const card = e.target.closest(".checkbook-card");
    if (!card) return;
    
    if (e.target.closest(".checkbook-dropdown__toggle")) return;
    const { id, bankName } = card.dataset;
    if (!id) return;
    
    showChecks(id, bankName);
});

const checkbookDropdown = Dropdown.setupDelegate({
    container: document,
    toggleSelector: ".checkbook-dropdown__toggle",
    options: {
        transitionDuration: 200,
        originalDisplay: "flex",
        effect: "zoom",
        placement: "right",
    },
});