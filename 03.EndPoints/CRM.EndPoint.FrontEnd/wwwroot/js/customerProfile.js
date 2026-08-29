document.addEventListener("DOMContentLoaded", async () => {
    
    await Promise.all([getAllPreFactor(), GetFactorPartial({ passType: 1, customerId: customerId })]);
    
    showTaminDropdown();
    getAllDetailAmount();
    removeTabFactorsDesktop();
    
    focusTargetPf();
});

function focusTargetPf() {
    const targetPf = sessionStorage.getItem("targetPf");
    if (!targetPf) return;
    sessionStorage.removeItem("targetPf");
    
    const el = document.querySelector(`[data-serialnumber="${targetPf}"]`);
    if (!el) return;
    el.scrollIntoView({ behavior: "smooth", block: "center" });
    
    flashItem(el, { duration: 2500 });
}


const removeTabFactorsDesktop = () => {
    if (window.innerWidth >= 1400) {
        document.getElementById("tabContentFactors").classList.remove("tab-content");
        document.getElementById("preFactorTab-pane").classList.remove("tab-pane", "fade");
        document.getElementById("factorTab-pane").classList.remove("tab-pane", "fade");
    } else {
        document.getElementById("tabContentFactors").classList.add("tab-content");
        document.getElementById("preFactorTab-pane").classList.add("tab-pane", "fade");
        document.getElementById("factorTab-pane").classList.add("tab-pane", "fade");
    }
    window.addEventListener("resize", removeTabFactorsDesktop);
};

const getAllDetailAmount = () => {
    axios(`/Factor/GetDetailAmount?cusId=${customerId}`).then((res) => {
        document.getElementById("AllFacAmountId").textContent = res.data.facAmount.toLocaleString();
        document.getElementById("AllFacPaymentId").textContent = res.data.allPrePayment.toLocaleString();
        document.getElementById("AllRemainedId").textContent = res.data.remained.toLocaleString();
        document.getElementById("WalletId").textContent = res.data.wallet.toLocaleString();
        
        const walletBalance = document.getElementById("wallet-balance");
        if (res.data.wallet > 0) {
            walletBalance.classList.remove("walledNotCredit");
            walletBalance.classList.add("walledCredit");
        } else {
            walletBalance.classList.remove("walledCredit");
            walletBalance.classList.add("walledNotCredit");
        }
    });
};


const getAllPreFactor = async (showDeleted = false) => {
    await sendApiRequest({
        url: "/Customer/GetAllPreFactorPartial",
        params: { customerId: customerId, showDeleted },
        onSuccess: (res) => {
            document.getElementById("PreFactorDivList").innerHTML = res.data.html;
            if (deletedToggleIcon?.isVisible && !showDeleted) {
                deletedToggleIcon.isVisible = false;
            }
        },
    });
};


const deletedToggleIcon = initEyeToggle({
    selector: ".showDeletedPrefactor-icon",
    onShow: async () => await getAllPreFactor(true),
    onHide: async () => await getAllPreFactor(false),
    showTitle: "نمایش حذف‌شده‌ها",
    hideTitle: "مخفی کردن حذف‌شده‌ها",
    titlePosition: "left",
});
