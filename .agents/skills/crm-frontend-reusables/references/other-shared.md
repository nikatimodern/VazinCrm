# Other shared JavaScript

Paths below are under `03.EndPoints/CRM.EndPoint.FrontEnd/wwwroot/js/`. These scripts are included by specific layouts/pages; verify script loading before use. This reference is selective; omitted functions remain available when their source and dependencies fit. Feature-specific files (`order.js`, `factor.js`, `cash.js`, and similar) are not general-purpose contracts.


## `File.js`

Shared upload/preview surface: `processUploadFiles(files,listName,previewIndex,containerUploader)` is async; `FileUpReload(fUpId)`, `ShowPreview(indexEle)`, `createImageBox(fileItem,key=null,indexEle=null,showDelete=false,showWatermark=false,watermarkText='')`, `delFile(index,indexEle)`, `clearUpload(inputId)` manage the existing file UI. `addToList(files,listName,progressElement)` is async; the file contains two declarations under this name, with the later declaration taking effect. Review that implementation before calling. `openDocumentViewer(url)`, `closeDocumentViewer()`, `downloadFileViewer({url,filename='file',extension=''})`, `downloadFile(btn,url,fileName)` (async), `triggerDownload(url,fileName)` cover viewing/download. `isImageFile(fileItem)`, `isPdfFile(fileItem)`, `getImageSrc(fileItem)`, `getBackgroundImage(fileItem,isImage,isPdf)`, `preloadImage(src)`, `setupViewer(wrapper)` support previews. `createUploadProgress(containerUploader)`, `updateUploadProgress(progressElement,percent,text)`, `removeUploadProgress(containerUploader)` support progress UI. These rely on the file uploader's global state and DOM; inspect the page setup.

## `Layout.js`

This script holds both shared layout behavior and feature-bound workflows. Shared candidates include `dateTimePicker(container=document)` and `Mask()` for date/input initialization; `getAllBankAccountsJson(rerender,type)` and `getAllBankAccounts(selects,rerender=false)` for bank selectors; `buildBankAccountText(item)` / `buildBankAccountUi(item)` for their display;  and `showAttachments(id,typeAtt)` for attachment UI. `ShowFilterPopup(filterBtn,filterItemBlackList=[])` implement the existing filter system. Inspect their required global state/DOM before reuse.

## `bottom-sheet.js`

`new BottomSheet({title='',content='',closeOnOverlay=true,closeOnEsc=true,showCloseButton=true,destroyOnClose=false,className='',snapPoints=[],actions=[]})` creates a sheet. Actions accept `{text,html,className,close=true,onClick}`. Instance methods `open`, `close`, `destroy`, `setTitle`, `setContent`, `setActions`, and `on` are synchronous; `on(event,callback)` registers lifecycle events `beforeOpen`, `open`, `beforeClose`, `close`, `drag`, `snap`, and `contentChanged`. Only one active sheet is tracked through `BottomSheet.activeSheet`. The file and its page CSS must be loaded before use; inspect current methods before calling.
