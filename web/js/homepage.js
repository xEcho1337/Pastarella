const input = document.getElementById("json-file-to-load");

input.addEventListener("change", async (event) => {
    input.disabled = true;

    const file = event.target.files[0];
    if (!file) {
        alert("You need to choose a file");
        return;
    }

    const request = window.indexedDB.open("files", 1);

    request.onupgradeneeded = () => {
        request.result.createObjectStore("files");
    };

    request.onerror = () => {
        alert("An error has occurred (IndexedDB)");
    };

    request.onsuccess = async () => {
        const db = request.result;

        const jsonObj = JSON.parse(await file.text());

        const tx = db.transaction("files", "readwrite");
        tx.objectStore("files").put(jsonObj, "file");

        tx.oncomplete = () => {
            db.close();
            location.href = "analyzer.html";
        };
    };
});

const logo = document.getElementById("logo");

if (Math.random() < 0.05) {
    logo.src = "https://static.gamberorosso.it/2024/04/pastarelle-1024x573.jpg";
} else {
    logo.src = "./logo.png";
}