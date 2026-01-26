//developer MohammadJavadEskandari in date 1402/03/29
//Company Natix
//WebSite www.natix.ir

const CheckAllDate = async (_form = null) => {
    
    //if (_form != null) {
    
    //    var inDa = Array.from(document.getElementById(_form).elements).filter(f => f.getAttribute("data-slots")=="ymd");
    //    console.log(inDa)
    
    //    inDa.map((item, index) => {
    //        console.log(item)
    
    //    })
    
    //}
    return new Promise(async (resolve, reject) => {
        
        var flag = true;
        var AllDateInput = document.querySelectorAll("[type = text][data-slots=ymd]");
        
        
        var count = AllDateInput.length;
        for (var i = 0; i < count; i++) {
            var st = await checkdate(AllDateInput[i]);
            if (!st) {
                flag = false;
            }
            if (i == (count - 1))
                resolve(flag);
        }
    });
    
};
const checkdate = async (item) => {
    
    return new Promise(async (resolve, reject) => {
        
        
        if (item.value != "") {
            
            if (!await _checkdate(item.value)) {
                
                resolve(false);
            } else {
                resolve(true);
            }
        } else {
            resolve(true);
        }
    });
};
const _checkdate = async (date) => {
    
    return new Promise((resolve, reject) => {
        
        try {
            var y = date.substring(0, 4);
            var m = date.substring(5, 7);
            var d = date.substring(8, 10);
            if (m == "00" || d == "00") {
                //alert("ورود تاریخ اشتباه" + " " + date)
                resolve(false);
            }
            var Ny = Number(y);
            var Nm = Number(m);
            var Nd = Number(d);
            
            if (Ny == 0) {
                
                //alert("ورود تاریخ اشتباه" + " " + date)
                resolve(false);
            }
            if (Nm >= 13 || Nm < 0) {
                
                //alert("ورود تاریخ اشتباه" + date)
                resolve(false);
            }
            if (Nd > 31 || Nd < 0) {
                
                //alert("ورود تاریخ اشتباه" + date)
                resolve(false);
            }
            switch (Nm) {
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                    if (Nd > 30) {
                        
                        //alert("ورود تاریخ اشتباه" + date)
                        resolve(false);
                    }
                    break;
                default:
                    resolve(true);
            }
            resolve(true);
        } catch (err) {
            resolve(false);
        }
        
    });
};

function checkDate(date) {
    try {
        const y = Number(date.substring(0, 4));
        const m = Number(date.substring(5, 7));
        const d = Number(date.substring(8, 10));
        
        if (!y || m < 1 || m > 12 || d < 1 || d > 31) return false;
        if (m >= 7 && m <= 12 && d > 30) return false;
        return true;
    } catch {
        return false;
    }
}

function normalizeDigits(str) {
    return str.replace(/[۰-۹]/g, d => String.fromCharCode(d.charCodeAt(0) - 1728))
              .replace(/[٠-٩]/g, d => String.fromCharCode(d.charCodeAt(0) - 1584));
}

function isValidDate(dateStr) {
    const normalizedDate = normalizeDigits(dateStr);
    if (!/^\d{4}\/\d{2}\/\d{2}$/.test(normalizedDate)) return false;
    
    const [y, m, d] = normalizedDate.split("/").map(Number);
    
    if (!Number.isInteger(y) || y <= 0) return false;
    if (!Number.isInteger(m) || m < 1 || m > 12) return false;
    if (!Number.isInteger(d) || d < 1) return false;
    
    let maxDay = m <= 6 ? 31 : 30;
    
    return d <= maxDay;
}


