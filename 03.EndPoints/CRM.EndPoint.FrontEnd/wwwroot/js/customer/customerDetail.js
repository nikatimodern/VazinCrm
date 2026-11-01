let custId = null;
let cust = null;
let userRel = [];
let selExperts = [];

document.addEventListener("DOMContentLoaded", async () => {
    custId = getUrlParams().id;
    await GetCustomer();
    await getCustomerSelRelation();
    await getCustomerRel();
    
    document.getElementById("addExpertsBtn").addEventListener("click", getSelExperts);
});


const getCustomerRel = async () => {
    await sendApiRequest({
        url: "/Customer/GetCustomerRel",
        params: { id: custId },
        onSuccess: res => document.getElementById("custRelId").innerHTML = res.data,
    });
};

const GetCustomer = async () => {
    const loadingModal = document.getElementById("loadingModalUp");
    
    if (loadingModal) loadingModal.style.display = "flex";
    
    
    cust = await getCustomer(custId);
    
    
    document.getElementById("juridicalTxt").textContent = cust.juridical === 0 ? "حقیقی" : "حقوقی";
    document.getElementById("agentName").innerHTML = `${cust.sex === 1 ? "جناب آقای " : "سرکار خانم "}<span class="fbold">${cust.agentName ?? ""}</span>`;
    document.getElementById("agentMeliCode").textContent = cust.agentMeliCode || "-";
    document.getElementById("tel").textContent = cust.tel || "-";
    document.getElementById("mobile").textContent = cust.mobile || "-";
    document.getElementById("workSenf").textContent = cust.workSenfTxt || "-";
    document.getElementById("workSenfDescription").textContent = cust.workSenfDescription || "-";
    
    const juridicalDiv = document.getElementById("JuridicalUpDiv");
    
    if (cust.juridical === 1) {
        juridicalDiv.style.display = "flex";
        
        document.getElementById("companyName").textContent = cust.companyName || "-";
        document.getElementById("shenaseMeli").textContent = cust.shenaseMeli || "-";
        document.getElementById("eghtesadiCode").textContent = cust.eghtesadiCode || "-";
        document.getElementById("numberRegister").textContent = cust.numberRegister || "-";
    } else {
        juridicalDiv.style.display = "none";
    }
    
    document.getElementById("postalCode").textContent = cust.postalCode || "-";
    document.getElementById("province").textContent = cust.province || "-";
    document.getElementById("city").textContent = cust.city || "-";
    document.getElementById("address").textContent = cust.address || "-";
    document.getElementById("openProfileCustomerIcon").onclick = () => openCustomerProfile(cust.id);
    
    
    const status = document.getElementById("statusId");
    
    status.textContent = cust.accepted ? "تایید شده" : "در دست تایید";
    status.style.color = cust.accepted ? "green" : "red";
    
    if (loadingModal) loadingModal.style.display = "none";
    
    return cust;
    
};

const delUser = async (uId, cId) => {
    const model = {
        UserId: uId,
        CustomerId: cId,
    };
    
    await sendApiRequest({
        url: "/Customer/RemoveUserCustomer",
        method: "POST",
        data: model,
        successMsg: null,
        onSuccess: getCustomerSelRelation,
    });
};


const getCustomerSelRelation = async () => {
    userRel = await sendApiRequest({
        url: "/Customer/GetCustomerSelRelation",
        params: { id: custId },
    });
    
    const content = document.getElementById("selContentId");
    
    content.innerHTML = `
        <div class="w-100 d-flex flex-wrap gap-1">
            ${userRel.map(item => `
                <label class="fbold flex-center gap-2 px-3 rounded-4" style="border: 2px solid #02514C">
                    <button onclick="delUser('${item.id}', '${custId}')" class="flex-center">
                        <svg style="width: 16px" viewBox="0 0 16 16" fill="currentColor"><path d="M5.28 4.22a.75.75 0 0 0-1.06 1.06L6.94 8l-2.72 2.72a.75.75 0 1 0 1.06 1.06L8 9.06l2.72 2.72a.75.75 0 1 0 1.06-1.06L9.06 8l2.72-2.72a.75.75 0 0 0-1.06-1.06L8 6.94 5.28 4.22Z" /></svg>
                    </button>

                    ${item.fullName}
                </label>
            `).join("")}
        </div>
    `;
};


async function getSelExperts() {
    const modalBodyElm = document.querySelector("#expertsModal .modal-body");
    modalBodyElm.innerHTML = "";
    
    document.getElementById("loadingModalUpModal").style.display = "flex";
    
    const data = await CrmSer.getUsers(2);
    
    const filteredData = data.filter(item => !userRel.some(user => user.id === item.id));
    
    showSelExperts(filteredData);
    document.getElementById("loadingModalUpModal").style.display = "none";
}


const showSelExperts = (data) => {
    selExperts = [];
    
    const modalBodyElm = document.querySelector("#expertsModal .modal-body");
    
    const divElm = document.createElement("div");
    divElm.className = "d-flex gap-2 flex-wrap";
    
    data.forEach(item => {
        const labelElm = document.createElement("label");
        
        labelElm.style.border = "2px solid #02514C";
        labelElm.style.borderRadius = "10px";
        labelElm.style.cursor = "pointer";
        labelElm.className = "fbold p-2 text-center";
        labelElm.textContent = item.text;
        
        labelElm.addEventListener("click", () => {
            const index = selExperts.indexOf(item.id);
            
            if (index === -1) {
                selExperts.push(item.id);
                labelElm.style.backgroundColor = "#02514C";
                labelElm.classList.add("text-white");
            } else {
                selExperts.splice(index, 1);
                labelElm.style.backgroundColor = "";
                labelElm.classList.remove("text-white");
            }
        });
        
        divElm.appendChild(labelElm);
    });
    
    modalBodyElm.appendChild(divElm);
};

const addSelExperts = async () => {
    const model = {
        customerId: cust.id,
        selExperts,
    };
    
    await sendApiRequest({
        url: "/Customer/AddSelsToCustomers",
        method: "POST",
        data: model,
        onSuccess: async () => {
            ModalToggle("expertsModal");
            await getCustomerSelRelation();
        },
    });
};

