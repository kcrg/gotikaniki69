document.getElementById('connectionButton').addEventListener('click', () => connect())
const slap = document.createElement('audio')
slap.src = '/assets/samples/HUH2.mp3'
let socket; // Declare the socket variable in the outer scope for broader accessibility

function connect() {
    var name = document.getElementById('name').value;
    var skinId = document.getElementById('skinId').value;

    if (name && skinId) {
        const landingBlock = document.getElementById('landingBlock');
        landingBlock.parentElement.removeChild(landingBlock);

        const info = document.getElementById('info');
        info.style.display = 'block';
        setTimeout(() => info.parentElement.removeChild(info), 3000);

        var socketUrl = 'ws://gotikaniki69.com:8081?name=' + encodeURIComponent(name) + '&skinId=' + encodeURIComponent(skinId);
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
                case 'Hello': slap.play(); break;
                case 'Hit': onHit(msg.payload); break;
            }
        });
    }
    else {
        alert("Wprowadź nick!");
    }
}

// Listen for the beforeunload event to close the WebSocket connection
window.addEventListener('beforeunload', () => {
    if (socket && socket.readyState === WebSocket.OPEN) {
        socket.close(); // Close the WebSocket connection
    }
});

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
    img.src = `/assets/gachi${skinId}.mp3`;
    img.style.width = '100px';
    img.style.height = 'auto';

    // Create a text element
    const text = document.createElement('div'); // or 'span'
    text.textContent = nick; // Set the nick as text
    text.style.textAlign = 'center'; // Center the text below the image
    text.style.marginTop = '5px'; // Adjust spacing between image and text
    text.style.fontFamily = 'GothicFont';
    text.style.fontSize = '16px';
    text.style.color = 'grey';

    // Append img and text to the container
    container.appendChild(img);
    container.appendChild(text);

    document.body.appendChild(container);

    // Adjusted for container fade out and removal
    container.style.opacity = '1';
    container.style.transition = 'opacity 300ms ease-out'; // Fade out effect
    setTimeout(() => container.style.opacity = '0', 2000 - 300); // Start fade out
    setTimeout(() => document.body.removeChild(container), 2300); // Remove container

    // Audio playback logic remains the same
    slap.src = `/assets/samples/slap${index ?? 2}.mp3`;
    slap.volume = 0.5;
    slap.play();
}