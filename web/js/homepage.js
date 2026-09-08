const input = document.getElementById("json-file-to-load");

input.addEventListener("change", async (event) => {
    input.disabled = true;

    const file = event.target.files[0];
    if (!file) {
        alert("You need to choose a file");
        return;
    }

    const data = await file.text();
    const reader = new FileReader();

    reader.onload = function () {
        sessionStorage.setItem("fileToAnalyze", data);
        window.location.href = "analyzer.html";
    };

    reader.readAsDataURL(file);
});

const logo = document.getElementById("logo");

if (Math.random() < 0.05) {
    logo.src = "https://static.gamberorosso.it/2024/04/pastarelle-1024x573.jpg";
} else {
    logo.src = "./logo.png";
}