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

function toggleNewCheckbookModal() {
    mySer.emptyForm("checkbookForm");
    ModalToggle("checkbookModal");
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
    
    toggleNewCheckbookModal();
};

function initCheckbookTable() {
    checksDataTable = $("#checksTable").DataTable({
        paging: false,
        scrollCollapse: true,
        scrollY: "calc(100dvh - 400px)",
        responsive: true,
        ordering: false,
        searching: false,
        ajax: {
            url: "",
            dataSrc: "",
        },
        columns: [
            { data: "serialNumber" },
            {
                data: "amount",
                render: data => `<span class="direction-ltr">${data.toLocaleString()}</span>`,
            },
            { data: "dueDate" },
            { data: "payee" },
            { data: "description" },
            {
                data: "statusTxt",
                render: data => `<span class="badgeGray d-inline-flex">${data || "-"}</span>`,
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
        language: {
            emptyTable: "هیچ رکوردی موجود نمی باشد.",
            info: "نمایش _START_ تا _END_ از _TOTAL_ ردیف",
            infoEmpty: "نمایش 0 تا 0 از 0 ردیف",
            infoFiltered: "(فیلتر شده از _MAX_ ردیف)",
            lengthMenu: "نمایش _MENU_ ردیف",
            loadingRecords: "در حال بارگزاری...",
            search: "",
            zeroRecords: "رکوردی با این مشخصات پیدا نشد",
            aria: {
                sortAscending: ": فعال سازی نمایش به صورت صعودی",
                sortDescending: ": فعال سازی نمایش به صورت نزولی",
            },
        },
        layout: null,
        initComplete: () => {
            document.querySelectorAll("#checksTable_wrapper > div.row").forEach((row) => {
                row.classList.add("d-flex", "flex-wrap");
                row.classList.remove("row");
            });
        },
    });
}


function showChecks(id, checkbookName) {
    checkbooksSection.classList.add("d-none");
    checkSection.querySelector("#selected-checkbook-title").innerText = "لیست چک‌های بانک " + checkbookName || "";
    checkSection.classList.remove("d-none");
    
    checkData.checkbookId = id;
    setUrlParams({ cBook: id, cBookName: checkbookName }, { replace: false });
    
    if (!checksDataTable) {
        initCheckbookTable();
    }
    
    checksDataTable.clear().draw(); // TODO
    checksDataTable.ajax.url(`/Check/GetCheckListByCheckbookId?checkbookId=${id}`).load();
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
    toggle.dropdownInstance.close();
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

let toggle = null;
checkbooksSection.addEventListener("click", function (e) {
    const clickedToggle = e.target.closest(".checkbook-dropdown__toggle");
    if (clickedToggle) {
    
    } else {
        if (toggle && toggle.dropdownInstance && toggle.dropdownInstance.isOpen) {
            toggle.dropdownInstance.close();
            toggle = null;
        }
    }
});

function openDrop(clickedToggle) {
    if (toggle && toggle !== clickedToggle && toggle.dropdownInstance && toggle.dropdownInstance.isOpen) {
        toggle.dropdownInstance.close();
        toggle = null;
    }
    
    if (!clickedToggle.dropdownInstance) {
        const currentDropdown = clickedToggle.nextElementSibling;
        clickedToggle.dropdownInstance = new Dropdown({
            button: clickedToggle,
            content: currentDropdown,
            transitionDuration: 200,
            originalDisplay: "flex",
            effect: "zoom",
        });
    }
    clickedToggle.dropdownInstance.toggle();
    
    toggle = clickedToggle.dropdownInstance.isOpen ? clickedToggle : null;
}