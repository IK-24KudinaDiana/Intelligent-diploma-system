let room = null;
let audioElements = new Map();
let micTrack = null;

window.liveKitInterop = {
    connect: async function (url, token, dotNetRef) {
        if (!window.LivekitClient?.Room) {
            await dotNetRef.invokeMethodAsync(
                'OnConnectionError',
                'LiveKit Client library is not loaded'
            );
            return;
        }

        try {
            room = new window.LivekitClient.Room({
                adaptiveStream: true,
                dynacast: true,
                audioCaptureDefaults: {
                    noiseSuppression: true,
                    echoCancellation: true,
                    autoGainControl: true
                }
            });

            room.on('connected', async () => {
                await dotNetRef.invokeMethodAsync('OnConnected');

                try {
                    micTrack = await window.LivekitClient.createLocalAudioTrack();
                    await room.localParticipant.publishTrack(micTrack);
                } catch (e) {
                    await dotNetRef.invokeMethodAsync(
                        'OnConnectionError',
                        `Microphone error: ${e.message}`
                    );
                }
            });

            room.on('disconnected', async () => {
                await dotNetRef.invokeMethodAsync('OnDisconnected');
            });

            room.on('trackSubscribed', async (track, publication, participant) => {
                await dotNetRef.invokeMethodAsync(
                    'OnTrackSubscribed',
                    track.kind,
                    participant.identity
                );

                if (track.kind !== 'audio') {
                    return;
                }

                const audioEl = track.attach();
                audioEl.autoplay = true;
                audioEl.playsInline = true;
                audioEl.controls = false;
                audioEl.volume = 1.0;
                audioEl.muted = false;
                audioEl.style.display = 'none';

                const trackSid = publication.trackSid;
                if (audioElements.has(trackSid)) {
                    return;
                }

                document.body.appendChild(audioEl);
                audioElements.set(trackSid, audioEl);

                try {
                    await audioEl.play();
                } catch {
                    document.body.addEventListener('click', async () => {
                        try { await audioEl.play(); } catch { }
                    }, { once: true });
                }
            });

            room.on('trackUnsubscribed', (_track, publication) => {
                const audioEl = audioElements.get(publication.trackSid);
                if (audioEl) {
                    audioEl.remove();
                    audioElements.delete(publication.trackSid);
                }
            });

            await room.connect(url, token);
        } catch (e) {
            await dotNetRef.invokeMethodAsync(
                'OnConnectionError',
                e.message || e.toString()
            );
        }
    },

    disconnect: async function () {
        try {
            if (micTrack) {
                micTrack.stop();
                micTrack = null;
            }

            audioElements.forEach(el => el.remove());
            audioElements.clear();

            if (room) {
                await room.disconnect();
                room = null;
            }
        } catch {
        }
    }
};
