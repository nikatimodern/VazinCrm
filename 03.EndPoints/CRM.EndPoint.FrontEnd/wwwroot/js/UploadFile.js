const IMAGE_EXTENSIONS = ["jpg", "jpeg", "png", "gif", "webp", "jfif"];

let uploadButtons = document.getElementsByClassName("uploadBtn");
let imageViewer = null;
//Upload Button
Array.from(uploadButtons).forEach((item, index) => {
    const listName = item.getAttribute("data-varList");
    
    item.addEventListener("change", async () => {
        /*if (item.files?.length > 0) {
            await addToList(item.files, listName);
            ShowPreview(index);
        }*/
        await processUploadFiles(item.files, listName, index, containerUploader);
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
            
            await processUploadFiles(e.dataTransfer.files, listName, index, containerUploader);
        });
    }
});

const delay = (ms) => new Promise(resolve => setTimeout(resolve, ms));

async function processUploadFiles(files, listName, previewIndex, containerUploader) {
    if (!files?.length) return;
    
    const progress = createUploadProgress(containerUploader);
    const minDuration = 800;
    const startTime = performance.now();
    
    try {
        containerUploader.classList.add("uploading");
        await addToList(files, listName, progress);
        
        ShowPreview(previewIndex);
        
        const elapsed = performance.now() - startTime;
        if (elapsed < minDuration) {
            await delay(minDuration - elapsed);
        }
    } finally {
        containerUploader.classList.remove("uploading");
        removeUploadProgress(containerUploader);
    }
}


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
    previewContainer.classList.add("previewContainer");
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


/**
 * file type
 */
function isImageFile(fileItem) {
    return IMAGE_EXTENSIONS.includes(fileItem.fileExten?.toLowerCase());
}

function isPdfFile(fileItem) {
    return fileItem.fileExten?.toLowerCase() === "pdf";
}

/**
 * image src
 */
function getImageSrc(fileItem) {
    
    
    if (fileItem.file) {
        return `data:image/${fileItem.fileExten};base64,${fileItem.file}`;
    }
    if (fileItem.url) {
        return `..${fileItem.url.replace(/\\/g, "/")}`;
    }
    return "";
}

/**
 * background image
 */
function getBackgroundImage(fileItem, isImage, isPdf) {
    if (isImage) {
        return getImageSrc(fileItem);
    }
    
    return isPdf ? "/Img/pdf.png" : "/Img/file.png";
}

/**
 * preload image
 */
function preloadImage(src) {
    if (!src) return;
    
    const img = new Image();
    img.src = src;
}

/**
 * delete button
 */
function createDeleteButton(showDelete, key, indexEle) {
    if (!showDelete || key === null || indexEle === null)
        return "";
    
    return `
        <span class="deleteBtn" onclick="delFile(${key}, ${indexEle})">
            <svg style="width: 16px" viewBox="0 0 20 20" fill="currentColor"><path d="M6.28 5.22a.75.75 0 0 0-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 1 0 1.06 1.06L10 11.06l3.72 3.72a.75.75 0 1 0 1.06-1.06L11.06 10l3.72-3.72a.75.75 0 0 0-1.06-1.06L10 8.94 6.28 5.22Z" /></svg>
        </span>
    `;
}

/**
 * watermark
 */
function createWatermark(showWatermark, watermarkText) {
    if (!showWatermark) return "";
    
    const watermarkStatusClass =
        watermarkText === "تایید شد" || watermarkText === "ثبت شد"
            ? "watermark--success"
            : watermarkText === "در دست تایید"
                ? "watermark--pending"
                : watermarkText === "رد شد"
                    ? "watermark--rejected"
                    : "";
    
    return `<div class="watermark ${watermarkStatusClass}">${watermarkText}</div>`;
}


/**
 * image link element
 */
function createImageLinkElement(fileItem, imgSrc, backgroundImage) {
    // viewerJs
    return `
        <span class="fileAttachLink viewerImageBtn">
            <img src="${imgSrc}" alt="${fileItem.filename || "image"}" data-extension="${fileItem.fileExten || ""}" class="viewerImageSource d-none" />

            <div class="fileAttachImg" style="background-image:url(${backgroundImage})">
            </div>
        </span>
    `;
    
    // lightbox2
    /*return `
        <a href="${imgSrc}" data-lightbox="lightBox-${fileItem.entityId?.slice(0, 6)}" class="fileAttachLink">
            <div class="fileAttachImg" style="background-image:url(${backgroundImage})"></div>
        </a>
    `;*/
}

/**
 * non image link element
 */
function createFileLinkElement(fileItem, backgroundImage) {
    const srcIframe = fileItem.id
        ? `${window.location.origin}${fileItem.url}`.replace(/\\/g, "/")
        : `${fileItem.filetype},${fileItem.file}`;
    
    return `
        <span onclick="showDivFrame('${srcIframe}')" class="fileAttachLink">
            <div class="fileAttachImg" style="background-image:url(${backgroundImage})">
            </div>
        </span>
    `;
}

/**
 * setup viewer js
 */
function setupViewer(wrapper) {
    const btn = wrapper.querySelector(".viewerImageBtn");
    if (!btn) return;
    
    btn.addEventListener("click", function () {
        const previewContainer = btn.closest(".previewContainer") || btn.parentElement;
        
        if (imageViewer) {
            imageViewer.destroy();
        }
        
        imageViewer = new Viewer(previewContainer, {
            filter(image) {
                return image.classList.contains("viewerImageSource");
            },
            
            zoomRatio: 0.2,
            //title: false,
            toolbar: {
                //oneToOne: true,
                //reset: true,
                zoomIn: true,
                zoomOut: true,
                rotateRight: true,
                
                download() {
                    const currentImg = imageViewer.image;
                    
                    downloadFile({
                        url: currentImg.src,
                        filename: currentImg.alt,
                        extension: currentImg.dataset.extension,
                    });
                },
            },
            
            hidden() {
                imageViewer.destroy();
                imageViewer = null;
            },
        });
        
        
        imageViewer.view([...previewContainer.querySelectorAll(".viewerImageSource")].indexOf(wrapper.querySelector(".viewerImageSource")));
    });
}


function createImageBox(fileItem, key = null, indexEle = null, showDelete = false, showWatermark = false, watermarkText = "") {
    const isImage = isImageFile(fileItem);
    const isPdf = isPdfFile(fileItem);
    
    const imgSrc = isImage ? getImageSrc(fileItem) : "";
    const backgroundImage = getBackgroundImage(fileItem, isImage, isPdf);
    
    if (isImage) {
        preloadImage(imgSrc);
    }
    
    const wrapper = document.createElement("div");
    wrapper.className = "boxAttach";
    
    const deleteBtn = createDeleteButton(showDelete, key, indexEle);
    const watermark = createWatermark(showWatermark, watermarkText);
    
    const linkElement = isImage
        ? createImageLinkElement(fileItem, imgSrc, backgroundImage)
        : createFileLinkElement(fileItem, backgroundImage);
    
    
    wrapper.innerHTML = `
        ${deleteBtn}
        ${watermark}
        ${linkElement}
    `;
    
    
    if (isImage) {
        setupViewer(wrapper);
    }
    
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

/*
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
};*/

const addToList = async (files, listName, progressElement) => {
    const total = files.length;
    
    let current = 0;
    
    for (let file of files) {
        current++;
        updateUploadProgress(progressElement, Math.round(((current - 1) / total) * 100), `در حال پردازش ${file.name}`);
        await new Promise(resolve => requestAnimationFrame(resolve));
        
        const sizeKB = Math.round(file.size / 1024);
        
        if (sizeKB > 20480) {
            toastifyError("حجم فایل باید کمتر از 20 مگابایت باشد");
            continue;
        }
        const base64 = await toBase64(file);
        
        const fileObj = {
            filename: file.name,
            fileExten: file.name.split(".").pop().toLowerCase(),
            filetype: base64.toString().split(",")[0],
            file: base64.toString().split(",")[1],
            isDeleted: false,
        };
        window[listName].push(fileObj);
        
        updateUploadProgress(progressElement, Math.round((current / total) * 100), `فایل ${current} از ${total} پردازش شد`);
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


function downloadFile({ url, filename = "file", extension = "" }) {
    if (!extension) {
        if (url.startsWith("data:image/")) { // base64
            const match = url.match(/^data:image\/([a-zA-Z0-9+]+);base64,/);
            extension = match?.[1] || "";
            
        } else { // url
            const cleanUrl = url.split("?")[0];
            extension = cleanUrl.split(".").pop() || "";
        }
    }
    
    extension = extension.replace(".", "").toLowerCase();
    
    if (extension && !filename.endsWith(`.${extension}`)) { // اگر filename پسوند نداشت
        filename += `.${extension}`;
    }
    
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
}

function createUploadProgress(containerUploader) {
    removeUploadProgress(containerUploader);
    
    const progress = document.createElement("div");
    progress.className = "upload-progress";
    progress.innerHTML = `
        <div class="upload-progress-text">
            در حال پردازش فایل...
        </div>

        <div class="upload-progress-bar">
            <div class="upload-progress-bar-fill"></div>
        </div>
    `;
    
    containerUploader.appendChild(progress);
    
    return progress;
}


function updateUploadProgress(progressElement, percent, text) {
    const fill = progressElement.querySelector(".upload-progress-bar-fill");
    
    const txt = progressElement.querySelector(".upload-progress-text");
    
    fill.style.width = `${percent}%`;
    
    txt.innerText = text;
}


function removeUploadProgress(containerUploader) {
    containerUploader.querySelector(".upload-progress")?.remove();
}