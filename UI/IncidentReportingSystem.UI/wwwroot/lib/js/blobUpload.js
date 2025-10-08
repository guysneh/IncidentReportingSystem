// wwwroot/lib/js/blobUpload.js
export function clickInput(inputEl) {
    if (!inputEl) throw new Error("inputEl is null");
    inputEl.click();
}

export function getSelectedFileMeta(inputEl) {
    if (!inputEl || !inputEl.files || inputEl.files.length === 0) return null;
    const f = inputEl.files[0];
    return {
        name: f.name || "unnamed",
        type: f.type || "application/octet-stream",
        size: (typeof f.size === "number" ? f.size : 0)
    };
}

// מעלה את הקובץ הראשון שב-<input type="file"> ל-SAS URL.
// headers צפוי להיות { [name: string]: string }
export async function uploadFileToSas(inputEl, url, method, headers) {
    if (!inputEl || !inputEl.files || inputEl.files.length === 0) {
        throw new Error("No file selected");
    }
    const file = inputEl.files[0];
    const init = {
        method: method || "PUT",
        headers: headers || {},
        body: file
    };
    const res = await fetch(url, init);
    if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(`SAS upload failed: ${res.status} ${res.statusText} ${text}`);
    }
}
