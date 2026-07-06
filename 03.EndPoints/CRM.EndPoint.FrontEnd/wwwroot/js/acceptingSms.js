
var paymentReceiptList = [];
const mySer = new MyService();
let signaturePad;
let selectedCustomerUserId;



const preFactorTerms = [
    {
        id: 1,
        text: "این پیش‌فاکتور پس از واریز 'مبلغ پیش‌پرداخت توافق‌شده' به منزله تأیید نهایی خریدار محسوب و لازم‌الاجرا بوده و مبنای انجام سفارش خواهد بود.",
    },
    {
        id: 2,
        text: "فروشنده در قبال تأخیرات ناشی از هر یک از موارد زیر مسئولیتی نخواهد داشت: عدم واریز به‌موقع پیش‌پرداخت یا سایر مبالغ توافق‌شده توسط خریدار، عدم ارائه به‌موقع اطلاعات، مدارک، مشخصات فنی یا تأییدات لازم از سوی خریدار، تغییرات درخواستی خریدار پس از تأیید سفارش، مشکلات یا تأخیرات مربوط به حمل‌ونقل، افزایش شدید قیمت، کمیابی، محدودیت یا اختلال در تأمین مواد اولیه، جنگ، آشوب، اعتصاب، حوادث غیرمترقبه، تصمیمات و محدودیت‌های دولتی، یا هر عامل دیگری که خارج از کنترل متعارف فروشنده باشد. در چنین مواردی، زمان تحویل به میزان مدت تأخیر ایجادشده و آثار ناشی از آن تمدید خواهد شد و خریدار حق مطالبه خسارت، جریمه یا فسخ قرارداد به استناد تأخیر مذکور را نخواهد داشت.",
    },
    {
        id: 3,
        text: "کلیه مشخصات فنی، ابعاد، طرح، رنگ، متن، کلیشه، مقدار، وزن و سایر مشخصات سفارش باید توسط خریدار تأیید گردد و مسئولیت صحت اطلاعات و تأییدات ارائه‌شده بر عهده خریدار خواهد بود.",
    },
    {
        id: 4,
        text: "هرگونه تغییر در مشخصات سفارش پس از تأیید نهایی خریدار صرفاً درصورت موافقت فروشنده امکان‌پذیر بوده و کلیه آثار ناشی از تغییرات شامل هزینه‌های اضافی، تغییر زمان تحویل، تغییر قیمت یا سایر تبعات بر عهده خریدار خواهد بود.",
    },
    {
        id: 5,
        text: "خریدار موظف است حداکثر ظرف مدت 10 روز تقویمی پس از تحویل، نسبت به بررسی کالای تحویلی اقدام و هرگونه مغایرت احتمالی با پیش‌فاکتور را به صورت کتبی به اطلاع فروشنده برساند. عدم اعلام کتبی مغایرت در مهلت مقرر، به منزله تأیید کامل کالا و پذیرش مطابقت آن با سفارش خواهد بود.",
    },
    {
        id: 6,
        text: "محصولات تولیدی با توجه به ماهیت فرآیند تولید، دارای تلرانس‌های متعارف تولید و چاپ بوده و خریدار با اطلاع از این موضوع، تلرانس‌های اعلامی از جمله رنگ، چاپ، وزن، ضخامت، ابعاد و مشخصات فنی را پذیرفته است.",
    },
    { id: 7, text: "هزینه حمل و نقل کالا بر عهده خریدار بوده مگر آنکه خلاف آن به‌صورت کتبی توافق شده باشد." },
    {
        id: 8,
        text: "در صورت آماده بودن سفارش و عدم دریافت کالا توسط خریدار، پس از گذشت 5 روز تقویمی از اعلام آمادگی تحویل، فروشنده مجاز خواهد بود قیمت کالا را بر اساس قیمت روز محاسبه و نسبت به صدور صورتحساب جدید اقدام نماید.",
    },
    { id: 9, text: "محصولات تولیدی و سفارشی پس از شروع فرآیند تولید، در صورت عدم وجود مغایرت اثبات‌شده، قابل لغو یا مرجوعی نمی باشد." },
    {
        id: 10,
        text: "مالکیت کالا تا زمان تسویه کامل مبلغ پیش‌فاکتور متعلق به فروشنده بوده و مالکیت کالا با تسویه حساب کامل و صدور فاکتور منتقل خواهد شد.",
    },
    {
        id: 11,
        text: "هرگونه تأیید انجام‌شده توسط خریدار از طریق امضا، مهر، مکاتبات رسمی، پیام‌رسان‌ها، ایمیل یا سایر روش‌های ارتباطی مورد توافق طرفین، به منزله تأیید سفارش و مشخصات آن تلقی خواهد شد.",
    },
    {
        id: 12,
        text: "در صورت بروز هرگونه اختلاف، ادعا یا دعوای ناشی از یا مرتبط با این پیش‌فاکتور، از جمله در خصوص تفسیر، اجرا، عدم اجرا، تأخیر، کیفیت، کمیت، پرداخت، تحویل، فسخ یا آثار آن، طرفین مکلف‌اند ابتدا موضوع را از طریق مذاکره ظرف مدت ۱۰ روز تقویمی از تاریخ اعلام کتبی اختلاف پیگیری نمایند. در صورت عدم حصول توافق، رسیدگی به اختلاف در اتحادیه نایلون و پلاستیک تهران خواهد بود و خریدار با تأیید این پیش‌فاکتور، صلاحیت مرجع مذکور را پذیرفته و حق ایراد نسبت به آن را از خود سلب و ساقط می‌نماید.",
    },
    { id: 13, text: "با تأیید این پیش‌فاکتور، خریدار کلیه شرایط و توضیحات مندرج در آن را مطالعه، قبول و تأیید نموده است." },
];

const generateTermsHtml = (termsArray) => {
    const listItems = termsArray.map(item => `
            <li class="mb-3" style="padding-right: 4px;">
                ${item.text}
            </li>
        `).join("");
    
    return `
            <div class="text-justify" style="font-size: 13px; line-height: 1.8; color: #333;">
                <ol style="padding-right: 20px; margin: 0;">
                    ${listItems}
                </ol>
            </div>
        `;
};

const termsSheet = new BottomSheet({
    title: "شرایط و توضیحات تکمیلی",
    content: generateTermsHtml(preFactorTerms),
    snapPoints: [0.8, 1],
    className: "terms-bottom-sheet",
});

termsSheet.setActions([
    {
        text: "بندها را مطالعه کردم و قبول دارم",
        className: "w-100 btn-accept-terms",
        primary: true,
        close: true,
        onClick: function (e, button) {
            const chkAccept = document.getElementById("chkAccept");
            if (chkAccept) {
                chkAccept.checked = true;
                document.getElementById("btnSubmitAcceptForm").disabled = false;
            }
        },
    },
]);


document.addEventListener("DOMContentLoaded", async () => {
    // signature
    initSignature();
    const chkAccept = document.getElementById("chkAccept");
    chkAccept?.addEventListener("change", (e) => document.getElementById("btnSubmitAcceptForm").disabled = !e.target.checked);
    
    document.getElementById("acceptForm")?.addEventListener("submit", async (e) => {
        e.preventDefault();
        
        if (signaturePad.isEmpty()) {
            Swal.fire({ icon: "error", text: "لطفاً امضاء خود را وارد کنید.", confirmButtonText: "باشه", confirmButtonColor: "#0d6efd" });
            return;
        }
        
        if (!chkAccept.checked) {
            Swal.fire({ icon: "error", text: "لطفا پیش‌فاکتور را تایید نمایید", confirmButtonText: "باشه", confirmButtonColor: "#0d6efd" });
            return;
        }
        
        if (customerCanChooseInvoiceType) {
            const result = await Swal.fire({
                icon: "question",
                title: "انتخاب نوع فاکتور",
                text: "لطفاً نوع فاکتور مورد نظر خود را انتخاب کنید:",
                showDenyButton: true,
                confirmButtonText: "فاکتور رسمی",
                denyButtonText: "فاکتور غیررسمی",
                confirmButtonColor: "var(--DarkColor)",
                denyButtonColor: "var(--DarkColorLight)",
            });
            
            if (result.isConfirmed || result.isDenied) {
                const officialType = result.isConfirmed ? 1 : 0;
                await sendSignature(officialType);
            }
        } else {
            await sendSignature();
        }
    });
    
    const toggleBtn = document.getElementById("toggleTermsBtn");
    if (toggleBtn) {
        toggleBtn.addEventListener("click", (e) => {
            e.preventDefault();
            termsSheet.open();
        });
    }
    
    // attach Receipt
    //ShowPaymentReceiptAttach('@pfId');
    
    const showLoginCustomerModalBtn = document.getElementById("showLoginCustomerModalBtn");
    const paymentReceiptContainer = document.getElementById("paymentReceiptContainer");
    if (showLoginCustomerModalBtn && paymentReceiptContainer) {
        showLoginCustomerModalBtn.style.display = verified === 1 ? "none" : "";
        paymentReceiptContainer.style.display = verified === 1 ? "flex" : "none";
    }
    
    const input = document.querySelector("#otpCustomerSection input[autocomplete='one-time-code']");
    const btn = document.getElementById("verifyOtpCustomerBtn");
    await readOtp(input, btn);
});


async function sendSignature(officialType = null) {
    try {
        FLshowLoading();
        
        const trimmedCanvas = trimCanvas(signaturePad.canvas, { copy: true });
        const emzaData = trimmedCanvas.toDataURL();
        
        const data = {
            id: '@pfId',
            emza: emzaData,
            agentName: document.getElementById("agentNameCustomer").value,
        };
        
        if (officialType !== null) {
            data.officialType = officialType;
        }
        
        const res = await fetch(`/Factor/AcceptingSMS`, {
            headers: { "Content-Type": "application/json" },
            method: "POST",
            body: JSON.stringify(data),
        });
        
        if (res.ok) {
            FLshowLoading();
            window.location.reload();
        }
    } catch (err) {
        console.log(err);
    }
}


// attach Receipt
function submitPaymentReceipt() {
    if (paymentReceiptList.every(item => item.isDeleted)) {
        toastifyError("سند واریزی اجباریست");
        return;
    }
    
    const model = {
        List: paymentReceiptList,
        pFactorId: '@pfId',
        type: 5,
    };
    
    FLshowLoading();
    $.post("/Factor/UploadAttachmentInvoice", model, (res) => {
        toastifySuccess("ثبت با موفقیت انجام شد");
        paymentReceiptList = [];
        document.getElementById("paymentReceipt").value = "";
        ShowPaymentReceiptAttach(model.pFactorId);
        
        FLshowLoading(false);
    }).fail(() => {
        setTimeout(() => submitPaymentReceipt(), 3000);
    });
    
}

const ShowPaymentReceiptAttach = (id) => {
    $.get(`/Factor/GetAttachmentsInvoiceUploaded/${id}`, (res) => {
        paymentReceiptList = res;
        FileUpReload("paymentReceipt");
    });
};

let targetAfterLogin;
const otpCustomerLoginMsgElm = document.getElementById("otpCustomerLoginMsg");
function showLoginCustomerModal(target = "panel") {
    targetAfterLogin = target;
    if (verified === 1) {
        if (targetAfterLogin === "panel") {
            goTo("/Account/Home", true);
        } else if (targetAfterLogin === "chat") {
            goTo(`/Chat/Main`, true);
        }
        return;
    }
    
    
    FLshowLoading();
    fetch(`/Account/RequestOtp?type=Login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ mobile: userMobile }),
    }).then(res => res.json())
      .then(data => {
          if (data.status === 200) {
              otpCustomerLoginMsgElm.innerText = "";
              document.getElementById("phoneNumberOtpCustomerLogin").textContent = userMobile;
              document.getElementById("verifyOtpCustomerBtnWrapper").style.display = "flex";
              document.getElementById("otpCustomerLoginInp").value = "";
              document.getElementById("otpCustomerLoginInp").focus();
              
              toastifySuccess(data.message);
              startOtpCustomerTimer();
          } else {
              otpCustomerLoginMsgElm.innerText = data.message;
              document.getElementById("verifyOtpCustomerBtnWrapper").style.display = "none";
          }
          FLshowLoading(false);
      });
    
    $("#loginCustomerModal").modal("show");
}


document.getElementById("verifyOtpCustomerBtn").addEventListener("click", () => verifyCustomerOtp());
document.getElementById("otpCustomerLoginInp").addEventListener("keyup", (e) => {
    if (e.target.value.length >= 4 || e.key === "Enter") {
        verifyCustomerOtp();
    }
});

const showLoginCustomerModalBtn = document.getElementById("showLoginCustomerModalBtn");
const paymentReceiptContainer = document.getElementById("paymentReceiptContainer");
function verifyCustomerOtp() {
    const OtpCode = document.querySelector("#loginCustomerContainer input[name='otp']").value;
    
    fetch(`/Account/VerifyOtpGeneric`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ mobile: userMobile, OtpCode }),
    }).then(res => res.json())
      .then(data => {
          if (data.status === 300) {
              showAccountCustomerModal(data.accounts);
          } else if (data.status === 200) {
              otpCustomerLoginMsgElm.innerText = "";
              toastifySuccess("احراز موفقیت با موفقیت انجام شد");
              $("#loginCustomerModal").modal("hide");
              verified = 1;
              
              if (targetAfterLogin === "panel") {
                  goTo(data.url, true);
              } else if (targetAfterLogin === "chat") {
                  goTo(`/Chat/Main`, true);
              }
              
              if (showLoginCustomerModalBtn && paymentReceiptContainer) {
                  showLoginCustomerModalBtn.style.display = "none";
                  paymentReceiptContainer.style.display = "flex";
              }
          } else {
              if (showLoginCustomerModalBtn && paymentReceiptContainer) {
                  showLoginCustomerModalBtn.style.display = "";
                  paymentReceiptContainer.style.display = "none";
              }
              otpCustomerLoginMsgElm.innerText = data.message;
          }
      });
}

let otpIntervalId;
function startOtpCustomerTimer() {
    otpIntervalId = setInterval(() => {
        fetch(`/Account/CheckTimerCode?type=Login`)
            .then(response => response.json())
            .then(data => {
                if (data.status) {
                    document.getElementById("remainingSecondsOtpCustomerLogin").innerText = data.elsapsTime + " ثانیه";
                    document.getElementById("resendOtpCustomerBtn").style.display = "none";
                    document.getElementById("verifyOtpCustomerBtnWrapper").style.display = "flex";
                    document.getElementById("otpCustomerLoginInp").focus();
                } else {
                    clearInterval(otpIntervalId);
                    document.getElementById("resendOtpCustomerBtn").style.display = "flex";
                    document.getElementById("verifyOtpCustomerBtnWrapper").style.display = "none";
                }
            })
            .catch(error => {
                console.log("Error", error);
            });
    }, 1000);
}


document.getElementById("resendOtpCustomerBtn").addEventListener("click", () => {
    fetch(`/Account/RequestOtp?type=Login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ mobile: userMobile, userId: selectedCustomerUserId }),
    })
        .then(res => res.json())
        .then(data => {
            if (data.status === 200) {
                otpCustomerLoginMsgElm.innerText = "";
                document.getElementById("otpCustomerLoginInp").value = "";
                toastifySuccess("کد یکبار مصرف مجددا ارسال شد");
                startOtpCustomerTimer();
            } else {
                otpCustomerLoginMsgElm.innerText = data.message;
            }
        });
});

function showAccountCustomerModal(accounts) {
    const listContainer = document.getElementById("accountCustomerList");
    listContainer.innerHTML = "";
    
    accounts.forEach(acc => {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "list-group-item list-group-item-action";
        btn.innerHTML = `
                <span class="fbold" style="font-size: 14px">${acc.userName}</span>
                <br>
                <span style="font-size: 11px">${acc.fullName || ""}</span>
            `;
        
        btn.addEventListener("click", () => {
            FLshowLoading();
            fetch(`/Account/SelectAccount?userId=${acc.id}&type=Login`)
                .then(res => res.json())
                .then(data => {
                    if (data.status === 200) {
                        selectedCustomerUserId = acc.id;
                        
                        document.getElementById("selectAccountCustomerContainer").style.display = "none";
                        document.getElementById("loginOtpCustomerContainer").style.display = "flex";
                        
                        otpCustomerLoginMsgElm.innerText = "";
                        toastifySuccess("احراز موفقیت با موفقیت انجام شد");
                        $("#loginCustomerModal").modal("hide");
                        verified = 1;
                        
                        if (targetAfterLogin === "panel") {
                            goTo(data.url, true);
                        }
                        if (showLoginCustomerModalBtn && paymentReceiptContainer) {
                            showLoginCustomerModalBtn.style.display = "none";
                            paymentReceiptContainer.style.display = "flex";
                        }
                        FLshowLoading(false);
                        
                    } else {
                        toastifyError(data.message);
                    }
                });
        });
        
        listContainer.appendChild(btn);
    });
    
    document.getElementById("selectAccountCustomerContainer").style.display = "block";
    document.getElementById("loginOtpCustomerContainer").style.display = "none";
}


// signature
function initSignature() {
    const canvas = document.getElementById("signatureCanvas");
    if (!canvas) return null;
    
    signaturePad = new SignaturePad(canvas, {
        minWidth: 1,
        maxWidth: 1,
        penColor: "darkblue",
    });
    
    
    function resizeCanvas() {
        const ratio = Math.max(window.devicePixelRatio || 1, 1);
        canvas.width = canvas.offsetWidth * ratio;
        canvas.height = canvas.offsetHeight * ratio;
        canvas.getContext("2d").scale(ratio, ratio);
        signaturePad.clear();
    }
    resizeCanvas();
    
    canvas.addEventListener("pointerdown", () => {
        if (document.activeElement && document.activeElement.tagName === "INPUT") {
            document.activeElement.blur();
        }
    });
    
    
    document.getElementById("clearBtn").addEventListener("click", () => signaturePad.clear());
}


