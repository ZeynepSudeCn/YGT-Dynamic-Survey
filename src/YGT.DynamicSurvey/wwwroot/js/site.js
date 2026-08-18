// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// =====================================================
// YGT DYNAMIC SURVEY - SITE.JS
// =====================================================

document.addEventListener("DOMContentLoaded", function () {

    const siteHeader = document.querySelector(".site-header");
    const updateHeaderOnScroll = () => {
        siteHeader?.classList.toggle("is-scrolled", window.scrollY > 18);
    };
    updateHeaderOnScroll();
    window.addEventListener("scroll", updateHeaderOnScroll, { passive: true });

    const themeButton = document.getElementById("themeToggle");
    document.documentElement.dataset.theme = localStorage.getItem("ygt-theme") || "light";
    const updateThemeLabel = () => {
        if (!themeButton) return;
        const isDark = document.documentElement.dataset.theme === "dark";
        themeButton.textContent = isDark ? "🌙" : "☀️";
        themeButton.setAttribute("aria-label", isDark ? "Açık temaya geç" : "Koyu temaya geç");
        themeButton.title = isDark ? "Açık temaya geç" : "Koyu temaya geç";
    };
    updateThemeLabel();
    themeButton?.addEventListener("click", () => { const next = document.documentElement.dataset.theme === "dark" ? "light" : "dark"; document.documentElement.dataset.theme = next; localStorage.setItem("ygt-theme", next); updateThemeLabel(); });

    const backToTop = document.getElementById("backToTop");
    const updateBackToTop = () => backToTop?.classList.toggle("is-visible", window.scrollY > 500);
    updateBackToTop();
    window.addEventListener("scroll", updateBackToTop, { passive: true });
    backToTop?.addEventListener("click", () => window.scrollTo({ top: 0, behavior: "smooth" }));
    document.querySelectorAll("[data-password-toggle]").forEach(button => button.addEventListener("click", () => { const input = button.parentElement?.querySelector("input"); if (!input) return; input.type = input.type === "password" ? "text" : "password"; button.textContent = input.type === "password" ? "Göster" : "Gizle"; }));
    const slider = document.querySelector("[data-event-slider]");
    if (slider) { const track = slider.querySelector(".event-slider-track"); const slides = slider.querySelectorAll(".event-slide"); let index = 0; const show = value => { index = (value + slides.length) % slides.length; track.style.transform = `translateX(-${index * 100}%)`; }; slider.querySelector(".prev")?.addEventListener("click", () => show(index - 1)); slider.querySelector(".next")?.addEventListener("click", () => show(index + 1)); if (slides.length > 1) setInterval(() => show(index + 1), 6000); }

    const gallery = document.getElementById("eventGallery");
    if (gallery && Array.isArray(window.eventGalleryImages)) {
        const galleryImage = document.getElementById("galleryImage");
        const galleryCounter = document.getElementById("galleryCounter");
        let galleryIndex = 0;
        const showGalleryImage = value => {
            galleryIndex = (value + window.eventGalleryImages.length) % window.eventGalleryImages.length;
            galleryImage.src = window.eventGalleryImages[galleryIndex];
            galleryCounter.textContent = `${galleryIndex + 1} / ${window.eventGalleryImages.length}`;
        };
        const openGallery = value => { showGalleryImage(value); gallery.classList.add("is-open"); gallery.setAttribute("aria-hidden", "false"); document.body.classList.add("gallery-open"); };
        const closeGallery = () => { gallery.classList.remove("is-open"); gallery.setAttribute("aria-hidden", "true"); document.body.classList.remove("gallery-open"); };
        document.querySelectorAll("[data-gallery-open]").forEach(button => button.addEventListener("click", () => openGallery(Number(button.dataset.galleryOpen || 0))));
        gallery.querySelector(".prev")?.addEventListener("click", () => showGalleryImage(galleryIndex - 1));
        gallery.querySelector(".next")?.addEventListener("click", () => showGalleryImage(galleryIndex + 1));
        gallery.querySelector(".gallery-close")?.addEventListener("click", closeGallery);
        gallery.addEventListener("click", event => { if (event.target === gallery) closeGallery(); });
        document.addEventListener("keydown", event => { if (event.key === "Escape") closeGallery(); if (gallery.classList.contains("is-open") && event.key === "ArrowLeft") showGalleryImage(galleryIndex - 1); if (gallery.classList.contains("is-open") && event.key === "ArrowRight") showGalleryImage(galleryIndex + 1); });
    }

    // =================================================
    // BİLDİRİM PANELİ
    // =================================================

    const notificationButton =
        document.getElementById("notificationButton");

    const notificationDropdown =
        document.getElementById("notificationDropdown");


    // Gerekli elemanlar bu sayfada yoksa işlem yapma
    if (!notificationButton || !notificationDropdown) {
        return;
    }


    // =================================================
    // ZİLE TIKLAYINCA PANELİ AÇ / KAPAT
    // =================================================

    notificationButton.addEventListener("click", function (event) {

        event.stopPropagation();

        notificationDropdown.classList.toggle("show");

    });


    // =================================================
    // PANELİN İÇİNE TIKLAYINCA KAPANMASIN
    // =================================================

    notificationDropdown.addEventListener("click", function (event) {

        event.stopPropagation();

    });


    // =================================================
    // SAYFANIN BAŞKA YERİNE TIKLAYINCA KAPAT
    // =================================================

    document.addEventListener("click", function () {

        notificationDropdown.classList.remove("show");

    });


    // =================================================
    // ESC TUŞUNA BASINCA KAPAT
    // =================================================

    document.addEventListener("keydown", function (event) {

        if (event.key === "Escape") {

            notificationDropdown.classList.remove("show");

        }

    });

});
