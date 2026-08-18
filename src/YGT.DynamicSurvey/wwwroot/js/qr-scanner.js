(() => {
    "use strict";

    const scannerModal = document.getElementById("qrScannerModal");
    const openButton = document.getElementById("openQrScannerButton");
    const closeButton = document.getElementById("closeQrScannerButton");
    const stopButton = document.getElementById("stopQrScannerButton");
    const video = document.getElementById("qrScannerVideo");
    const canvas = document.getElementById("qrScannerCanvas");
    const statusText = document.getElementById("qrScannerStatus");

    if (!scannerModal || !openButton || !video || !canvas) {
        return;
    }

    const context = canvas.getContext("2d", {
        willReadFrequently: true
    });

    let stream = null;
    let animationFrameId = null;
    let scanning = false;
    let hasHandledResult = false;


    function setStatus(message, type = "info") {
        if (!statusText) {
            return;
        }

        statusText.textContent = message;
        statusText.dataset.type = type;
    }


    function openModal() {
        scannerModal.classList.add("is-open");
        scannerModal.setAttribute("aria-hidden", "false");

        document.body.classList.add("qr-scanner-open");

        hasHandledResult = false;

        startCamera();
    }


    function closeModal() {
        stopCamera();

        scannerModal.classList.remove("is-open");
        scannerModal.setAttribute("aria-hidden", "true");

        document.body.classList.remove("qr-scanner-open");
    }


    async function startCamera() {
        if (scanning) {
            return;
        }

        if (!navigator.mediaDevices?.getUserMedia) {
            setStatus(
                "Tarayıcınız kamera erişimini desteklemiyor.",
                "error"
            );

            return;
        }

        if (typeof window.jsQR !== "function") {
            setStatus(
                "QR okuyucu yüklenemedi.",
                "error"
            );

            return;
        }

        try {
            setStatus(
                "Kamera izni bekleniyor...",
                "info"
            );

            stream =
                await navigator.mediaDevices.getUserMedia({
                    video: {
                        facingMode: {
                            ideal: "environment"
                        },

                        width: {
                            ideal: 1280
                        },

                        height: {
                            ideal: 720
                        }
                    },

                    audio: false
                });


            video.srcObject = stream;

            await video.play();

            scanning = true;


            setStatus(
                "QR kodu çerçevenin içine getir.",
                "success"
            );


            scanFrame();
        }
        catch (error) {
            console.error(
                "Kamera açılamadı:",
                error
            );


            if (error?.name === "NotAllowedError") {
                setStatus(
                    "Kamera izni verilmedi. Tarayıcıdan kamera iznini açıp tekrar deneyin.",
                    "error"
                );
            }

            else if (error?.name === "NotFoundError") {
                setStatus(
                    "Kullanılabilir bir kamera bulunamadı.",
                    "error"
                );
            }

            else {
                setStatus(
                    "Kamera açılamadı. Kamera başka bir uygulama tarafından kullanılıyor olabilir.",
                    "error"
                );
            }
        }
    }


    function stopCamera() {
        scanning = false;


        if (animationFrameId) {
            cancelAnimationFrame(
                animationFrameId
            );

            animationFrameId = null;
        }


        if (stream) {
            stream
                .getTracks()
                .forEach(
                    track => track.stop()
                );

            stream = null;
        }


        video.srcObject = null;
    }


    function scanFrame() {
        if (!scanning) {
            return;
        }


        if (
            video.readyState ===
            video.HAVE_ENOUGH_DATA &&

            video.videoWidth > 0 &&
            video.videoHeight > 0
        ) {
            canvas.width =
                video.videoWidth;

            canvas.height =
                video.videoHeight;


            context.drawImage(
                video,
                0,
                0,
                canvas.width,
                canvas.height
            );


            const imageData =
                context.getImageData(
                    0,
                    0,
                    canvas.width,
                    canvas.height
                );


            const result =
                window.jsQR(
                    imageData.data,
                    imageData.width,
                    imageData.height,
                    {
                        inversionAttempts:
                            "attemptBoth"
                    }
                );


            if (
                result?.data &&
                !hasHandledResult
            ) {
                hasHandledResult = true;

                handleQrResult(
                    result.data.trim()
                );

                return;
            }
        }


        animationFrameId =
            requestAnimationFrame(
                scanFrame
            );
    }


    function handleQrResult(rawValue) {
        const surveyCode =
            extractSurveyCode(
                rawValue
            );


        if (!surveyCode) {
            hasHandledResult = false;


            setStatus(
                "Bu QR kod geçerli bir YGT anket kodu içermiyor.",
                "error"
            );


            animationFrameId =
                requestAnimationFrame(
                    scanFrame
                );

            return;
        }


        setStatus(
            `Anket bulundu: ${surveyCode}. Yönlendiriliyorsunuz...`,
            "success"
        );


        stopCamera();


        window.setTimeout(
            () => {
                window.location.href =
                    `/Survey/Join?code=${encodeURIComponent(
                        surveyCode
                    )}`;
            },
            450
        );
    }


    function extractSurveyCode(rawValue) {

        // QR sadece 6 haneli kod içeriyorsa
        const directMatch =
            rawValue.match(
                /^\d{6}$/
            );


        if (directMatch) {
            return directMatch[0];
        }


        // QR tam URL içeriyorsa
        try {
            const parsedUrl =
                new URL(
                    rawValue,
                    window.location.origin
                );


            const code =
                parsedUrl.searchParams.get(
                    "code"
                );


            if (
                code &&
                /^\d{6}$/.test(code)
            ) {
                return code;
            }
        }
        catch {
            // URL değilse devam et
        }


        // Metin içinde code=123456 varsa
        const queryMatch =
            rawValue.match(
                /(?:\?|&|^)code=(\d{6})(?:&|$)/i
            );


        if (queryMatch) {
            return queryMatch[1];
        }


        return null;
    }


    openButton.addEventListener(
        "click",
        openModal
    );


    closeButton?.addEventListener(
        "click",
        closeModal
    );


    stopButton?.addEventListener(
        "click",
        closeModal
    );


    scannerModal.addEventListener(
        "click",
        event => {
            if (
                event.target ===
                scannerModal
            ) {
                closeModal();
            }
        }
    );


    document.addEventListener(
        "keydown",
        event => {
            if (
                event.key === "Escape" &&
                scannerModal.classList.contains(
                    "is-open"
                )
            ) {
                closeModal();
            }
        }
    );


    window.addEventListener(
        "beforeunload",
        stopCamera
    );
})();