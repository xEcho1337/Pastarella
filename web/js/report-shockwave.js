const DURATION_MS = 1050;
let isPlaying = false;

export function playReportShockwave() {
    if (isPlaying || typeof document === "undefined") return false;
    isPlaying = true;

    const ring = document.createElement("div");
    ring.className = "shock-ring";
    const flash = document.createElement("div");
    flash.className = "shock-flash";
    document.body.appendChild(ring);
    document.body.appendChild(flash);
    document.body.classList.add("shock-pulse");

    setTimeout(() => {
        ring.remove();
        flash.remove();
        document.body.classList.remove("shock-pulse");
        isPlaying = false;
    }, DURATION_MS);

    return true;
}
