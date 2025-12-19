document.getElementById('connectionButton').addEventListener('click', () => connect())
const slap = document.createElement('audio')
slap.src = '/assets/samples/HUH2.mp3'
let socket;

function connect() {
    var name = document.getElementById('name').value;
    var skinSelectElement = document.getElementById('skinSelect');

    if (name) {
        const landingBlock = document.getElementById('landingBlock');
        landingBlock.parentElement.removeChild(landingBlock);

        const info = document.getElementById('info');
        info.style.display = 'block';
        setTimeout(() => info.parentElement.removeChild(info), 3000);

        const wsScheme = location.protocol === 'https:' ? 'wss' : 'ws';
        var socketUrl = `${wsScheme}://${location.host}/ws?name=${encodeURIComponent(name)}&skinId=${encodeURIComponent(skinSelectElement.value)}`;
        socket = new WebSocket(socketUrl);

        socket.addEventListener('open', () => {
            document.body.addEventListener('pointerdown', (evt) => {
                console.log(evt);
                socket.send(JSON.stringify({ x: evt.clientX - 50, y: evt.clientY - 38 }));
            });
        });

        socket.addEventListener('message', (evt) => {
            const msg = JSON.parse(evt.data);
            console.log(msg);
            switch (msg.type) {
                case 'Hello': {
                    const ball = document.getElementById('ball');
                    if (ball) {
                        ball.style.display = `block`;
                    }
                    const goal = document.getElementById('goal');
                    if (goal) {
                        goal.style.display = `block`;
                    }
                    slap.play();
                    break;
                }
                case 'Hit': onHit(msg.payload); break;
                case 'BallMovement': showBall(msg.payload); break;
            }
        });
    }
    else {
        alert("Wprowadź nick!");
    }
}

function onHit(payload) {
    const { index, nick, skinId, x, y } = payload;
    console.log(payload);

    // Create a container for the image and text
    const container = document.createElement('div');
    container.style.position = 'absolute';
    container.style.top = `${y}px`;
    container.style.left = `${x}px`;
    container.style.pointerEvents = 'none';
    container.style.userSelect = 'none';

    const img = document.createElement('img');
    img.src = `/assets/gachi${skinId}.gif`;
    img.style.width = '100px';
    img.style.height = 'auto';

    // Create a text element
    const text = document.createElement('div');
    text.textContent = nick;
    text.style.textAlign = 'center';
    text.style.marginTop = '5px';
    text.style.fontFamily = 'GothicFont';
    text.style.fontSize = '16px';
    text.style.color = 'grey';

    // Append img and text to the container
    container.appendChild(img);
    container.appendChild(text);

    document.body.appendChild(container);

    // Adjusted for container fade out and removal
    container.style.opacity = '1';
    container.style.transition = 'opacity 300ms ease-out';
    setTimeout(() => container.style.opacity = '0', 2000 - 300);
    setTimeout(() => document.body.removeChild(container), 2300);

    // Audio playback logic
    slap.src = `/assets/samples/slap${index ?? 2}.mp3`;
    slap.volume = 0.5;
    slap.play();
}

function showBall(payload) {
    const { x, y } = payload;
    const ball = document.getElementById('ball');
    if (ball) {
        ball.style.left = `${x}px`;
        ball.style.top = `${y}px`;
    }
}