const $ = (id) => document.getElementById(id);

const landingBlock = $("landingBlock");
const info = $("info");
const ball = $("ball");
const goal = $("goal");

const connectionButton = $("connectionButton");
const nameInput = $("name");
const skinSelect = $("skinSelect");

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

    let a = slapAudios.get(key);
    if (!a) {
        a = new Audio(`/assets/samples/slap${key}.mp3`);
        a.preload = "auto";
        a.volume = 0.5;
        slapAudios.set(key, a);
    }

    try { a.currentTime = 0; } catch { }
    a.play().catch(() => { });
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

function ensurePointerHandler() {
    if (pointerHandlerAttached) return;
    pointerHandlerAttached = true;

    document.body.addEventListener("pointerdown", (evt) => {
        if (!socket || socket.readyState !== WebSocket.OPEN) return;
        if (!canSendNow()) return;

        // minimalny payload
        const msg = { x: evt.clientX - 50, y: evt.clientY - 38 };
        socket.send(JSON.stringify(msg));
    }, { passive: true });
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
                if (ball) ball.style.display = "block";
                if (goal) goal.style.display = "block";
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
    document.body.appendChild(container);

    // fade-out bez grzebania w inline transition
    // i pewniej niż setTimeout na opacity
    setTimeout(() => container.classList.add("fade"), 1700);  // 2s pełne
    setTimeout(() => container.remove(), 2000);               // +300ms fade

    playSlap(index);
}

function showBall(payload) {
    const { x, y } = payload;
    const ball = document.getElementById('ball');
    if (ball) {
        ball.style.transform = `translate3d(${x}px, ${y}px, 0)`;
    }
}

connectionButton?.addEventListener("click", connect);
