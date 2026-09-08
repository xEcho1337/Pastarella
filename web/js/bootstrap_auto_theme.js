function updateTheme(theme) {
	document.getElementsByTagName("html")[0].setAttribute("data-bs-theme", theme);

	let icon = document.getElementById("theme-icon");
	switch (theme) {
		case "light":
			icon.classList.remove("bi-sun-fill");
			icon.classList.add("bi-moon-fill");
			break;
		case "dark":
			icon.classList.remove("bi-moon-fill");
			icon.classList.add("bi-sun-fill");
			break;
	}
}

function toggleTheme() {
	switch (document.getElementsByTagName("html")[0].getAttribute("data-bs-theme")) {
		case "light":
			updateTheme("dark");
			break;
		case "dark":
			updateTheme("light");
			break;
	}
}

window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
	updateTheme(window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");
});

updateTheme(window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");