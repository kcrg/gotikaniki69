const $ = (id) => document.getElementById(id);

const landingBlock = $("landingBlock");
const info = $("info");
const ball = $("ball");
const goal = $("goal");

const connectionButton = $("connectionButton");
const nameInput = $("name");
const skinSelect = $("skinSelect");

const game = $("game");
const stage = $("stage");

const PITCH_W = 1600;
const PITCH_H = 800;

let stageScale = 1;
let stageLeft = 0;
let stageTop = 0;

function layoutStage() {
    const vw = window.innerWidth;
    const vh = window.innerHeight;

    stageScale = Math.min(vw / PITCH_W, vh / PITCH_H);
    stageLeft = (vw - PITCH_W * stageScale) / 2;
    stageTop = (vh - PITCH_H * stageScale) / 2;

    stage.style.transform =
        `translate3d(${stageLeft}px, ${stageTop}px, 0) scale(${stageScale})`;
}

window.addEventListener("resize", layoutStage);
window.addEventListener("orientationchange", layoutStage);

let socket = null;
let pointerHandlerAttached = false;

// audio cache
const audioHello = new Audio("/assets/samples/HUH2.mp3");
audioHello.preload = "auto";
audioHello.volume = 0.5;

const slapAudios = new Map();
function playSlap(index) {
    const i = (index ?? 2) | 0;
    const key = (i >= 1 && i <= 8) ? i : 2;

    slap.src = `/assets/samples/slap${key}.mp3`;
    slap.volume = 0.5;

    try { slap.currentTime = 0; } catch { }
    slap.play().catch(() => { });
}

const slap = new Audio();
slap.preload = "auto";
slap.volume = 0.5;

function unlockAudioIOS() {
    // musi polecieć wprost z click/tap użytkownika
    const audios = [audioHello, slap];

    for (const a of audios) {
        try {
            a.muted = true;
            a.play()
                .then(() => { a.pause(); a.currentTime = 0; })
                .catch(() => { })
                .finally(() => { a.muted = false; });
        } catch { }
    }
}

// lekkie throttle na klik (np. 30/s)
let lastSendTs = 0;
const SEND_INTERVAL_MS = 33;

function canSendNow() {
    const now = performance.now();
    if (now - lastSendTs < SEND_INTERVAL_MS) return false;
    lastSendTs = now;
    return true;
}

function getClientPoint(evt) {
    // fallback dla Safari / touch
    if (evt.touches && evt.touches[0]) {
        return { x: evt.touches[0].clientX, y: evt.touches[0].clientY };
    }
    return { x: evt.clientX, y: evt.clientY };
}

function ensurePointerHandler() {
    if (pointerHandlerAttached) return;
    pointerHandlerAttached = true;

    const handler = (evt) => {
        if (!socket || socket.readyState !== WebSocket.OPEN) return;
        if (!canSendNow()) return;

        // na touchstart blokujemy scroll/zoom
        if (evt.cancelable) evt.preventDefault();

        const p = getClientPoint(evt);

        // screen -> world
        let wx = (p.x - stageLeft) / stageScale;
        let wy = (p.y - stageTop) / stageScale;

        // Twoje offsety na gif/tekst
        wx -= 50;
        wy -= 38;

        // clamp do boiska
        wx = Math.max(0, Math.min(PITCH_W, wx));
        wy = Math.max(0, Math.min(PITCH_H, wy));

        socket.send(JSON.stringify({ x: wx, y: wy }));
    };

    // pointer events + fallback touch dla iOS/in-app browser
    game.addEventListener("pointerdown", handler, { passive: false });
    game.addEventListener("touchstart", handler, { passive: false });
}

function showInfoOnce() {
    if (!info) return;
    info.style.display = "block";
    setTimeout(() => info.remove(), 3000);
}

function connect() {
    const name = (nameInput?.value ?? "").trim();
    const skinId = skinSelect?.value ?? "1";

    if (name.length < 2) {
        alert("Wprowadź nick!");
        return;
    }

    unlockAudioIOS();     // <- ważne dla iPhone
    game.style.display = "block";
    layoutStage();

    landingBlock?.remove();
    showInfoOnce();
    ensurePointerHandler();

    // zamknij stare połączenie jeśli było
    try { socket?.close(1000, "reconnect"); } catch { }

    const wsScheme = location.protocol === "https:" ? "wss" : "ws";
    const socketUrl = `${wsScheme}://${location.host}/ws?name=${encodeURIComponent(name)}&skinId=${encodeURIComponent(skinId)}`;

    socket = new WebSocket(socketUrl);

    socket.addEventListener("open", () => {
        // opcjonalnie: console.log("WS open");
    });

    socket.addEventListener("message", (evt) => {
        // JSON.parse jest OK, ale bez console.log na hot path
        let msg;
        try { msg = JSON.parse(evt.data); }
        catch { return; }

        switch (msg.type) {
            case "Hello":
                audioHello.play().catch(() => { });
                break;

            case "Hit":
                if (msg.payload) onHit(msg.payload);
                break;

            case "BallMovement":
                if (msg.payload) showBall(msg.payload);
                break;
        }
    });

    // prosta odporność: reconnect
    socket.addEventListener("close", () => scheduleReconnect());
    socket.addEventListener("error", () => scheduleReconnect());
}

let reconnectTimer = null;
let reconnectDelay = 200; // start
function scheduleReconnect() {
    if (reconnectTimer) return;
    if (!nameInput) return;

    reconnectTimer = setTimeout(() => {
        reconnectTimer = null;
        reconnectDelay = Math.min(3000, reconnectDelay * 1.5);

        // jeżeli user jest już na etapie gry i ma nick, to próbuj
        const name = (nameInput.value ?? "").trim();
        if (name.length >= 2) connect();
    }, reconnectDelay);
}

// lepszy hit: mniej layoutu (transform zamiast top/left)
function onHit(payload) {
    const { index, nick, skinId, x, y } = payload;

    const container = document.createElement("div");
    container.className = "hit"; // dodaj CSS poniżej
    container.style.transform = `translate3d(${x}px, ${y}px, 0)`;

    const img = document.createElement("img");
    img.src = `/assets/gachi${skinId}.gif`;
    img.width = 100;

    const text = document.createElement("div");
    text.textContent = nick ?? "";
    text.className = "hit-text";

    container.appendChild(img);
    container.appendChild(text);
    stage.appendChild(container);

    // fade-out bez grzebania w inline transition
    // i pewniej niż setTimeout na opacity
    setTimeout(() => container.classList.add("fade"), 1700);  // 2s pełne
    setTimeout(() => container.remove(), 2000);               // +300ms fade

    playSlap(index);
}

function showBall(payload) {
    const { x, y } = payload;
    if (ball) {
        ball.style.transform = `translate3d(${x}px, ${y}px, 0)`;
    }
}

connectionButton?.addEventListener("click", connect);
