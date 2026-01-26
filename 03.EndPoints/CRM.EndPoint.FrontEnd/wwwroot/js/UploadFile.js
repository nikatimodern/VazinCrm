let uploadButtons = document.getElementsByClassName("uploadBtn");

//Upload Button
Array.from(uploadButtons).forEach((item, index) => {
    const listName = item.getAttribute("data-varList");
    
    item.addEventListener("change", async () => {
        if (item.files?.length > 0) {
            await addToList(item.files, listName);
            ShowPreview(index);
        }
    });
    
    // Drag & Drop
    const containerUploader = item.closest(".container-uploader") || item.parentElement;
    
    if (containerUploader) {
        let isInternalDrag = false;
        
        containerUploader.addEventListener("dragstart", (e) => {
            if (e.target.closest(".container-uploader")) {
                isInternalDrag = true;
            }
        });
        
        containerUploader.addEventListener("dragend", () => {
            isInternalDrag = false;
        });
        
        containerUploader.addEventListener("dragenter", (e) => {
            if (isInternalDrag) return;
            e.preventDefault();
        });
        
        containerUploader.addEventListener("dragover", (e) => {
            if (isInternalDrag) return;
            e.preventDefault();
            containerUploader.classList.add("dragover");
        });
        
        containerUploader.addEventListener("dragleave", () => {
            if (isInternalDrag) return;
            containerUploader.classList.remove("dragover");
        });
        
        containerUploader.addEventListener("drop", async (e) => {
            if (isInternalDrag) return;
            e.preventDefault();
            
            containerUploader.classList.remove("dragover");
            const files = e.dataTransfer.files;
            if (files.length > 0) {
                await addToList(files, listName);
                ShowPreview(index);
            }
        });
    }
});


const FileUpReload = (fUpId) => {
    Array.from(uploadButtons).forEach((item, index) => {
        if (item.id === fUpId) {
            ShowPreview(index);
        }
    });
};


const ShowPreview = (indexEle) => {
    const item = uploadButtons[indexEle];
    if (!item) return;
    
    const listName = item.getAttribute("data-varList");
    const divPre = item.getAttribute("data-showList");
    const ListAttach = window[listName] || [];
    
    const previewContainer = document.getElementById(divPre);
    previewContainer.innerHTML = "";
    ListAttach.forEach((fileItem, key) => {
        if (!fileItem.isDeleted) {
            const showDelete = !fileItem.filename.includes("_AcPfactorByCustomer_")
                && !(fileItem.filename.includes("_AcPfactorBySalesExpert_") && listName === "paymentReceiptList")
                && !(fileItem.filename.includes("_APayPrefactor_") && listName === "paymentReceiptList");
            const showWatermark =
                (listName === "paymentReceiptList" || listName === "attachInvoiceList") &&
                (fileItem._AttachType === 4 || fileItem._AttachType === 5 || fileItem._AttachType === 6);
            const watermarkText =
                (listName === "paymentReceiptList" || listName === "attachInvoiceList") &&
                (fileItem._AttachType === 5 || fileItem._AttachType === 6) ? "در دست تایید" : "تایید شد";
            const imageBox = createImageBox(fileItem, key, indexEle, showDelete, showWatermark, watermarkText);
            previewContainer.appendChild(imageBox);
        }
    });
};


function createImageBox(fileItem, key = null, indexEle = null, showDelete = false, showWatermark = false, watermarkText) {
    let imgSrc = "";
    let backgroundImage = "";
    
    const isImage = ["jpg", "jpeg", "png", "gif", "webp", "jfif"].includes(fileItem.fileExten?.toLowerCase());
    const isPdf = fileItem.fileExten?.toLowerCase() === "pdf";
    
    if (isImage) {
        imgSrc = fileItem.url ? `..${fileItem.url.replace(/\\/g, "/")}`
            : fileItem.file ? `data:image/${fileItem.fileExten};base64,${fileItem.file}` : "";
        backgroundImage = imgSrc;
    } else if (isPdf) {
        backgroundImage = "/Img/pdf.png";
    } else {
        backgroundImage = "/Img/file.png";
    }
    
    
    const wrapper = document.createElement("div");
    wrapper.className = "boxAttach";
    
    const deleteBtn = (showDelete && key !== null && indexEle !== null)
        ? `<span class="deleteBtn" onclick="delFile(${key}, ${indexEle})">
              <svg style="width: 16px" viewBox="0 0 20 20" fill="currentColor"><path d="M6.28 5.22a.75.75 0 0 0-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 1 0 1.06 1.06L10 11.06l3.72 3.72a.75.75 0 1 0 1.06-1.06L11.06 10l3.72-3.72a.75.75 0 0 0-1.06-1.06L10 8.94 6.28 5.22Z" /></svg>
            </span>`
        : "";
    
    let watermark = "";
    if (showWatermark) {
        const watermarkStatusClass =
            watermarkText === "تایید شد" || watermarkText === "ثبت شد" ? "watermark--success"
                : watermarkText === "در دست تایید" ? "watermark--pending"
                    : watermarkText === "رد شد" ? "watermark--rejected"
                        : "";
        
        watermark = `<div class="watermark ${watermarkStatusClass}">${watermarkText}</div>`;
    }
    
    
    let linkElement;
    if (backgroundImage === imgSrc) {
        linkElement = `
            <a href="${imgSrc}" data-lightbox="lightBox-${fileItem.entityId?.slice(0, 6)}" class="fileAttachLink">
              <div class="fileAttachImg" style="background-image:url(${backgroundImage})"></div>
            </a>
        `;
    } else {
        const srcIframe = fileItem.id ? `${window.location.origin}${fileItem.url}`.replace(/\\/g, "/") : `${fileItem.filetype},${fileItem.file}`;
        linkElement = `
            <span onclick="showDivFrame('${srcIframe}')" class="fileAttachLink">
              <div class="fileAttachImg" style="background-image:url(${backgroundImage})"></div>
            </span>
        `;
    }
    
    
    wrapper.innerHTML = `
        ${deleteBtn}
        ${watermark}
        ${linkElement}
      `;
    
    return wrapper;
}


const delFile = (index, indexEle) => {
    const item = uploadButtons[indexEle];
    const listName = item.getAttribute("data-varList");
    const list = window[listName];
    if (!list) return;
    
    if (list[index]?.id) {
        list[index].isDeleted = true;
    } else {
        list.splice(index, 1);
    }
    item.value = "";
    ShowPreview(indexEle);
};


const addToList = async (files, listName) => {
    for (let file of files) {
        const base64 = await toBase64(file);
        const sizeKB = Math.round(file.size / 1024);
        if (sizeKB > 20480) {
            toastifyError("حجم فایل باید کمتر از 20 مگابایت باشد");
            continue;
        }
        
        const fileObj = {
            filename: file.name,
            fileExten: file.name.split(".").pop().toLowerCase(),
            filetype: base64.toString().split(",")[0],
            file: base64.toString().split(",")[1],
            isDeleted: false,
        };
        window[listName].push(fileObj);
    }
};

const toBase64 = file => new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.readAsDataURL(file);
    reader.onload = () => resolve(reader.result);
    reader.onerror = error => reject(error);
});

const divFrameElm = document.getElementById("divFrame");

const showDivFrame = (src) => {
    document.getElementById("IframeId").src = src;
    divFrameElm.style.display = "flex";
};

const hideDivFrame = () => {
    divFrameElm.style.display = "none";
    document.getElementById("IframeId").src = "";
};


document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") {
        const isDivOpen = getComputedStyle(divFrameElm).display !== "none";
        if (isDivOpen) {
            hideDivFrame();
            e.preventDefault();
            e.stopPropagation();
        }
    }
}, true);

