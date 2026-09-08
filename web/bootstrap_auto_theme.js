const htmlElement = document.querySelector("html");

if (htmlElement.getAttribute("data-bs-theme") === "auto") {
    function updateTheme() {
        htmlElement.setAttribute(
            "data-bs-theme",
            window.matchMedia("(prefers-color-scheme: dark)").matches
                ? "dark"
                : "light"
        );
    }

    const darkModeQuery = window.matchMedia(
        "(prefers-color-scheme: dark)"
    );

    darkModeQuery.addEventListener("change", updateTheme);

    updateTheme();
}
