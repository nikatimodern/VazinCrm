let JuridicalFlag = false;
let CurrentStep = 1;
let allStep = 3;
let flagStep1 = false;
let flagStep2 = false;
let flagStep3 = false;

const SearchInp = document.getElementById("SearchInp");
const LoadingCustomer = document.getElementById("LoadingCusPartId");
const ShowAllBtn = document.getElementById("btnShowAllId");

(function initFiltersFromUrl() {
    const { juridicalType = -1, roleType = -1, search = "" } = getUrlParams();
    const roleTypeRadio = document.querySelector(`input[name='roleTypeChange'][value='${roleType}']`);
    if (roleTypeRadio) roleTypeRadio.checked = true;
    document.querySelector(`input[name='juridicalChange'][value='${juridicalType}']`).checked = true;
    SearchInp.value = search;
})();


document.addEventListener("DOMContentLoaded", () => {
    getAllCustomers();
    
    initSearchInput(SearchInp, (e, input) => {
        const searchValue = input.value.trim();
        setUrlParams({ search: searchValue ? searchValue : null });
        getAllCustomers();
    });
    
    initChangeRadio("juridical", (e) => {
        JuridicalFlag = Number(e.target.value);
        changeForm();
    });
    
    initChangeRadio("roleTypeChange", (e) => {
        const rType = Number(e.target.value);
        setUrlParams({ roleType: rType !== -1 ? rType : null });
        getAllCustomers();
    });
    
    initChangeRadio("juridicalChange", (e) => {
        const jType = Number(e.target.value);
        setUrlParams({ juridicalType: jType !== -1 ? jType : null });
        getAllCustomers();
    });
});


const getCountOfCustomerPartialTop = () => {
    if (window.innerWidth < 1200) return 2;
    if (window.innerWidth >= 1200 && window.innerWidth < 1500) return 3;
    if (window.innerWidth >= 1500 && window.innerWidth < 1830) return 4;
    return 5;
};


let skipCus;
let takeCus;
const ShowAllCustomer = () => {
    loadCustomers(skipCus, takeCus);
    skipCus += takeCus;
};

let isLoading = false;
function loadCustomers(skip = 0, take) {
    if (isLoading) return;
    if (take === undefined) {
        take = getCountOfCustomerPartialTop();
        takeCus = take * 12;
        skipCus = 0;
    }
    isLoading = true;
    LoadingCustomer.style.display = "flex";
    
    const { juridicalType = -1, roleType = -1 } = getUrlParams();
    
    sendApiRequest({
        url: "/Customer/GetAllCustomerPartial",
        params: { skip, take, roleType, juridical: juridicalType },
        onSuccess: (res) => {
            const trimmed = res.data.trim();
            if (trimmed.includes("no-customer-found")) {
                ShowAllBtn.style.display = "none";
                if (skip === 0) {
                    document.getElementById("CustomerPartial").innerHTML = trimmed;
                }
                return;
            }
            
            const container = document.getElementById("CustomerPartial");
            if (skip === 0) {
                container.innerHTML = trimmed;
            } else {
                const temp = document.createElement("div");
                temp.innerHTML = trimmed;
                
                Array.from(temp.children).forEach(el => container.appendChild(el));
            }
        },
    });
    
    isLoading = false;
    LoadingCustomer.style.display = "none";
    ShowAllBtn.style.display = "flex";
}


const SearchAllCustomer = (search) => {
    LoadingCustomer.style.display = "flex";
    const container = document.getElementById("CustomerPartial");
    
    const { juridicalType = -1, roleType = -1 } = getUrlParams();
    sendApiRequest({
        url: "/Customer/SearchAllCustomerPartial",
        params: { val: search, juridical: juridicalType, roleType },
        onSuccess: (res) => {
            LoadingCustomer.style.display = "none";
            ShowAllBtn.style.display = "none";
            container.innerHTML = res.data;
        },
    });
};

function getAllCustomers() {
    const { search = "" } = getUrlParams();
    if (search.length !== 0)
        SearchAllCustomer(search);
    else {
        loadCustomers();
    }
}


const addToMyCustomer = async (id) => {
    await sendApiRequest({
        url: `/Customer/AddToMyCustomer`,
        params: { id },
        onSuccess: async (res) => {
            if (res.data === 0) {
                toastifyError("در لیست مشتریان موجود است");
                return;
            }
            
            toastifySuccess("به لسیت مشتریان من اضافه شد");
            ModalToggle("AlertRegister");
            hideFormCustomer();
            getAllCustomers();
        },
    });
};

let opLoaded = false;
const showFormCustomer = () => {
    if (!opLoaded) {
        GetSenfOp();
        GetAcquaintanceWayOp();
        opLoaded = true;
    }
    
    document.getElementById("btnCancel").style.display = "";
    document.getElementById("btnNew").style.display = "none";
    document.getElementById("CuParId").style.display = "none";
    document.getElementById("formAddCustom").style.display = "";
    document.getElementById("searchCustomerWrapper").style.display = "none";
    document.getElementById("roleTypeRadios").style.display = "none";
    gotoStep(1);
};


const hideFormCustomer = () => {
    document.getElementById("btnNew").style.display = "";
    document.getElementById("CuParId").style.display = "flex";
    document.getElementById("searchCustomerWrapper").style.display = "block";
    document.getElementById("roleTypeRadios").style.display = "block";
    document.getElementById("btnCancel").style.display = "none";
    document.getElementById("formAddCustom").style.display = "none";
    document.getElementById("btnIn").innerText = "تایید و بعدی";
    
    mySer.emptyForm("Step1");
    mySer.emptyForm("Step2");
    mySer.emptyForm("Step3");
    
    flagStep1 = false;
    flagStep2 = false;
    flagStep3 = false;
};

const backStep = () => {
    CurrentStep--;
    
    if (CurrentStep <= 0) {
        CurrentStep = 1;
    }
    
    gotoStep(CurrentStep);
    
    document.getElementById("btnIn").innerText = CurrentStep === 1 || CurrentStep === 2 ? "تایید و بعدی" : "ثبت";
};

const nextStep = () => {
    CurrentStep++;
    
    if (CurrentStep >= allStep) {
        CurrentStep = allStep;
    }
    
    gotoStep(CurrentStep);
};

const gotoStep = (step) => {
    document.querySelectorAll("#hStep1, #hStep2, #hStep3").forEach(el => el.classList.remove("ac"));
    CurrentStep = step;
    document.getElementById(`hStep${step}`).classList.add("ac");
    hideDivSteps();
    showDivStep();
};

const showDivStep = () => {
    document.getElementById(`Step${CurrentStep}`).style.display = "flex";
};


const hideDivSteps = () => {
    ["Step1", "Step2", "Step3"].forEach(id => document.getElementById(id).style.display = "none");
};

const changeForm = () => {
    const selectedJuridical = document.querySelector("input[type='radio'][name='juridical']:checked");
    const juridicalInputs = document.querySelectorAll(".hog");
    
    JuridicalFlag = Number(selectedJuridical.value);
    
    // اگر True بود حقوقی هست
    if (JuridicalFlag === 1) {
        document.getElementById("JuridicalDiv").style.display = "flex";
        
        document.getElementById("nameTxt").textContent = "نام نماینده";
        document.getElementById("agentNameId").placeholder = "نام نماینده";
        
        toggleElementsRequired(juridicalInputs, true);
    } else {
        document.getElementById("nameTxt").textContent = "نام و نام خانوادگی";
        document.getElementById("agentNameId").placeholder = "نام و نام خانوادگی";
        
        document.getElementById("JuridicalDiv").style.display = "none";
        
        toggleElementsRequired(juridicalInputs, false);
    }
};

const checkDuplicate = async () => {
    const mobile = document.getElementById("mobile").value;
    const agentName = document.getElementById("agentNameId").value;
    
    if (mobile.length === 0 || agentName.length === 0) {
        return;
    }
    
    const step1Data = mySer.convertFormToJSON("#Step1");
    
    const data = await sendApiRequest({
        url: "/Customer/CheckDuplicate",
        method: "POST",
        data: step1Data,
        successMsg: null,
    });
    
    if (!data) return;
    
    ModalToggle("AlertRegister");
    document.getElementById("alertRegisterLoading").style.display = "none";
    document.getElementById("contentAlertRegister").innerHTML = data;
};


const submitFormCustomerBase = async () => {
    switch (CurrentStep) {
        case 1:
            if (await mySer.CheckValidForm("#Step1")) {
                nextStep();
                await checkDuplicate();
                flagStep1 = true;
                document.getElementById("btnIn").innerText = "تایید و بعدی";
            }
            break;
        
        case 2:
            if (await mySer.CheckValidForm("#Step2")) {
                nextStep();
                flagStep2 = true;
                document.getElementById("btnIn").innerText = "ثبت";
            }
            break;
        
        case 3:
            if (await mySer.CheckValidForm("#Step3")) {
                nextStep();
                flagStep3 = true;
            }
            break;
    }
    
    if (!flagStep1 || !flagStep2 || !flagStep3) {
        return;
    }
    
    
    const model = {
        ...mySer.convertFormToJSON("#Step1"),
        ...mySer.convertFormToJSON("#Step2"),
        ...mySer.convertFormToJSON("#Step3"),
    };
    
    const workSenf = document.getElementById("workSenf");
    const acquaintanceWay = document.getElementById("AcquaintanceWay");
    
    if (model.workSenf) {
        model.workSenfTxt = workSenf.options[workSenf.selectedIndex].text;
    }
    
    if (model.AcquaintanceWay) {
        model.AcquaintanceWayTxt = acquaintanceWay.options[acquaintanceWay.selectedIndex].text;
    }
    
    await sendApiRequest({
        url: "/Customer/AddCustomer",
        method: "POST",
        data: model,
        successMsg: "ثبت مشتری با موفقیت انجام شد",
        onSuccess: () => {
            getAllCustomers();
            hideFormCustomer();
        },
    });
};


const abortCustomer = (id) => {
    sendApiRequest({
        url: "/Customer/AbortCustomer",
        params: { id },
        successMsg: "مشتری حذف شد",
        onSuccess: getAllCustomers,
    });
};


const acceptCustomer = (id) => {
    sendApiRequest({
        url: "/Customer/AcceptCustomer",
        params: { id },
        successMsg: "تایید شد",
        onSuccess: getAllCustomers,
    });
};


let senfLoaded = false;
const ShowModalUpCustomer = async (id) => {
    if (!senfLoaded) {
        await GetUpSenfOp();
        senfLoaded = true;
    }
    customerIdUp = id;
    
    await sendApiRequest({
        url: `/Customer/GetCustomer`,
        params: { id },
        onSuccess: (res) => {
            const data = res.data;
            
            changeJur(data.juridical);
            mySer.convertJsonToForm(data, "CuInfoUpFormId");
            
            ModalToggle("updateCustomerModal");
        },
    });
};

const SubmitUpFormCustomer = async () => {
    if (!await mySer.CheckValidForm("#CuInfoUpFormId")) return;
    
    const data = mySer.convertFormToJSON("#CuInfoUpFormId");
    data.id = customerIdUp;
    
    const workSelect = document.getElementById("workSenfID");
    if (data.workSenf) data.workSenfTxt = workSelect.options[workSelect.selectedIndex].text;
    
    await sendApiRequest({
        url: `/Customer/UpCustomer`,
        method: "POST",
        data,
        successMsg: "ویرایش مشتری با موفقیت انجام شد",
        onSuccess: () => {
            closeModalUpdate();
            getAllCustomers();
        },
    });
};

const closeModalUpdate = () => {
    ModalToggle("updateCustomerModal", "hide");
    cancelUpCustomer();
    customerIdUp = null;
};

const GetSenfOp = () => {
    initBaseItemSelect({
        elementId: "workSenf",
        typeCode: 1,
    });
};

const GetAcquaintanceWayOp = () => {
    initBaseItemSelect({
        elementId: "AcquaintanceWay",
        typeCode: 2,
    });
};

const GetUpSenfOp = async () => {
    await initBaseItemSelect({
        elementId: "workSenfID",
        typeCode: 1,
        dropdownParent: $("#updateCustomerModal"),
    });
};

const initBaseItemSelect = async ({ elementId, typeCode, dropdownParent = null }) => {
    const select = document.getElementById(elementId);
    if (!select) return;
    
    const data = await sendApiRequest({
        url: "/BaseItem/GetItemsOp",
        params: { typeCode },
    });
    
    if (!data) return;
    
    const options = {
        data,
        dir: "rtl",
        placeholder: "انتخاب",
        selectionCssClass: "form-control",
    };
    
    if (dropdownParent) {
        options.dropdownParent = dropdownParent;
    }
    
    $(select).select2(options);
    $(select).val(null).trigger("change");
};