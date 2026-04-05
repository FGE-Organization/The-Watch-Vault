(() => {
    const popup = document.getElementById("cookieNotice");
    const button = document.getElementById("cookieAcceptBtn");

    if (!popup || !button) return;

    const accepted = localStorage.getItem("watchvault_cookies_accepted");

    if (accepted === "true") {
        popup.classList.add("cookie-lux-hidden");
        return;
    }

    setTimeout(() => {
        popup.classList.add("cookie-lux-show");
    }, 500);

    button.addEventListener("click", () => {
        localStorage.setItem("watchvault_cookies_accepted", "true");
        popup.classList.remove("cookie-lux-show");
        popup.classList.add("cookie-lux-hidden");
    });
})();
