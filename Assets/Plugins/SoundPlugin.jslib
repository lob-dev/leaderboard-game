var SoundPlugin = {

    $SoundState: {
        ctx: null,
        ambientGain: null,
        ambientOscs: [],
        ambientPlaying: false,
        masterGain: null
    },

    SoundPlugin_Init: function() {
        if (SoundState.ctx) return;
        try {
            SoundState.ctx = new (window.AudioContext || window.webkitAudioContext)();
            SoundState.masterGain = SoundState.ctx.createGain();
            SoundState.masterGain.gain.value = 0.6;
            SoundState.masterGain.connect(SoundState.ctx.destination);
        } catch(e) {
            console.warn('[SoundPlugin] No Web Audio API');
        }
    },

    SoundPlugin_Resume: function() {
        if (SoundState.ctx && SoundState.ctx.state === 'suspended') {
            SoundState.ctx.resume();
        }
    },

    // --- TAP SOUND: short percussive click with pitch variation ---
    SoundPlugin_PlayTap: function(comboLevel) {
        if (!SoundState.ctx) return;
        var ctx = SoundState.ctx;
        var t = ctx.currentTime;

        // Base freq rises with combo
        var baseFreq = 800 + comboLevel * 120;

        var osc = ctx.createOscillator();
        var gain = ctx.createGain();
        osc.type = 'sine';
        osc.frequency.setValueAtTime(baseFreq, t);
        osc.frequency.exponentialRampToValueAtTime(baseFreq * 0.5, t + 0.08);
        gain.gain.setValueAtTime(0.3, t);
        gain.gain.exponentialRampToValueAtTime(0.001, t + 0.08);
        osc.connect(gain);
        gain.connect(SoundState.masterGain);
        osc.start(t);
        osc.stop(t + 0.1);

        // Click transient
        var click = ctx.createOscillator();
        var clickGain = ctx.createGain();
        click.type = 'square';
        click.frequency.setValueAtTime(1200 + comboLevel * 200, t);
        click.frequency.exponentialRampToValueAtTime(200, t + 0.03);
        clickGain.gain.setValueAtTime(0.15, t);
        clickGain.gain.exponentialRampToValueAtTime(0.001, t + 0.03);
        click.connect(clickGain);
        clickGain.connect(SoundState.masterGain);
        click.start(t);
        click.stop(t + 0.05);
    },

    // --- COMBO ESCALATION: rising arpeggio ---
    SoundPlugin_PlayComboEscalation: function(comboLevel) {
        if (!SoundState.ctx) return;
        var ctx = SoundState.ctx;
        var t = ctx.currentTime;

        // Play a quick rising note sequence
        var notes = [523, 659, 784, 1047, 1319]; // C5 E5 G5 C6 E6
        var idx = Math.min(comboLevel - 1, notes.length - 1);

        for (var i = 0; i <= idx; i++) {
            var osc = ctx.createOscillator();
            var gain = ctx.createGain();
            osc.type = 'triangle';
            osc.frequency.value = notes[i];
            var noteStart = t + i * 0.04;
            gain.gain.setValueAtTime(0, noteStart);
            gain.gain.linearRampToValueAtTime(0.15, noteStart + 0.02);
            gain.gain.exponentialRampToValueAtTime(0.001, noteStart + 0.12);
            osc.connect(gain);
            gain.connect(SoundState.masterGain);
            osc.start(noteStart);
            osc.stop(noteStart + 0.15);
        }
    },

    // --- RANK UP CELEBRATION: fanfare chord ---
    SoundPlugin_PlayRankUp: function(milestone) {
        if (!SoundState.ctx) return;
        var ctx = SoundState.ctx;
        var t = ctx.currentTime;

        // Major chord fanfare, bigger for bigger milestones
        var chords;
        if (milestone <= 1) {
            // #1 - triumphant!
            chords = [261, 329, 392, 523, 659, 784, 1047];
        } else if (milestone <= 3) {
            chords = [329, 415, 523, 659, 784];
        } else if (milestone <= 5) {
            chords = [392, 494, 587, 784];
        } else {
            chords = [523, 659, 784];
        }

        var duration = milestone <= 1 ? 1.0 : (milestone <= 3 ? 0.7 : 0.5);

        for (var i = 0; i < chords.length; i++) {
            var osc = ctx.createOscillator();
            var gain = ctx.createGain();
            osc.type = i < 3 ? 'triangle' : 'sine';
            osc.frequency.value = chords[i];
            var delay = i * 0.05;
            gain.gain.setValueAtTime(0, t + delay);
            gain.gain.linearRampToValueAtTime(0.12, t + delay + 0.05);
            gain.gain.setValueAtTime(0.12, t + delay + duration * 0.6);
            gain.gain.exponentialRampToValueAtTime(0.001, t + delay + duration);
            osc.connect(gain);
            gain.connect(SoundState.masterGain);
            osc.start(t + delay);
            osc.stop(t + delay + duration + 0.05);
        }

        // Sparkle sweep on top
        for (var s = 0; s < 5; s++) {
            var sparkle = ctx.createOscillator();
            var sGain = ctx.createGain();
            sparkle.type = 'sine';
            var sTime = t + 0.1 + s * 0.08;
            sparkle.frequency.setValueAtTime(2000 + s * 400, sTime);
            sparkle.frequency.exponentialRampToValueAtTime(1000 + s * 200, sTime + 0.1);
            sGain.gain.setValueAtTime(0.06, sTime);
            sGain.gain.exponentialRampToValueAtTime(0.001, sTime + 0.1);
            sparkle.connect(sGain);
            sGain.connect(SoundState.masterGain);
            sparkle.start(sTime);
            sparkle.stop(sTime + 0.15);
        }
    },

    // --- OVERTAKE SWOOSH: frequency sweep ---
    SoundPlugin_PlaySwoosh: function(isPositive) {
        if (!SoundState.ctx) return;
        var ctx = SoundState.ctx;
        var t = ctx.currentTime;

        // Noise-based swoosh using oscillator trick
        var osc = ctx.createOscillator();
        var gain = ctx.createGain();
        osc.type = 'sawtooth';

        if (isPositive) {
            // Rising swoosh (you overtook someone)
            osc.frequency.setValueAtTime(200, t);
            osc.frequency.exponentialRampToValueAtTime(1200, t + 0.15);
            osc.frequency.exponentialRampToValueAtTime(800, t + 0.25);
        } else {
            // Falling swoosh (someone overtook you)
            osc.frequency.setValueAtTime(1000, t);
            osc.frequency.exponentialRampToValueAtTime(200, t + 0.2);
        }

        gain.gain.setValueAtTime(0.12, t);
        gain.gain.setValueAtTime(0.12, t + 0.1);
        gain.gain.exponentialRampToValueAtTime(0.001, t + 0.3);

        // Low-pass filter for a smoother swoosh
        var filter = ctx.createBiquadFilter();
        filter.type = 'lowpass';
        filter.frequency.value = 2000;
        filter.Q.value = 2;

        osc.connect(filter);
        filter.connect(gain);
        gain.connect(SoundState.masterGain);
        osc.start(t);
        osc.stop(t + 0.35);

        // Additional white noise burst
        var bufferSize = ctx.sampleRate * 0.3;
        var buffer = ctx.createBuffer(1, bufferSize, ctx.sampleRate);
        var data = buffer.getChannelData(0);
        for (var i = 0; i < bufferSize; i++) {
            data[i] = (Math.random() * 2 - 1) * 0.3;
        }
        var noise = ctx.createBufferSource();
        noise.buffer = buffer;
        var noiseGain = ctx.createGain();
        var noiseFilter = ctx.createBiquadFilter();
        noiseFilter.type = 'bandpass';
        noiseFilter.frequency.value = isPositive ? 3000 : 1000;
        noiseFilter.Q.value = 1;
        noiseGain.gain.setValueAtTime(0.08, t);
        noiseGain.gain.exponentialRampToValueAtTime(0.001, t + 0.25);
        noise.connect(noiseFilter);
        noiseFilter.connect(noiseGain);
        noiseGain.connect(SoundState.masterGain);
        noise.start(t);
        noise.stop(t + 0.3);
    },

    // --- AMBIENT MUSIC: generative drone + slow arpeggios ---
    SoundPlugin_StartAmbient: function() {
        if (!SoundState.ctx || SoundState.ambientPlaying) return;
        var ctx = SoundState.ctx;
        SoundState.ambientPlaying = true;

        SoundState.ambientGain = ctx.createGain();
        SoundState.ambientGain.gain.setValueAtTime(0, ctx.currentTime);
        SoundState.ambientGain.gain.linearRampToValueAtTime(0.08, ctx.currentTime + 2.0);
        SoundState.ambientGain.connect(SoundState.masterGain);

        // Drone layers
        var droneNotes = [65.41, 98.0, 130.81]; // C2, G2, C3
        for (var i = 0; i < droneNotes.length; i++) {
            var osc = ctx.createOscillator();
            osc.type = 'sine';
            osc.frequency.value = droneNotes[i];
            var droneGain = ctx.createGain();
            droneGain.gain.value = i === 0 ? 0.5 : 0.3;
            osc.connect(droneGain);
            droneGain.connect(SoundState.ambientGain);
            osc.start();
            SoundState.ambientOscs.push(osc);
        }

        // Slow LFO on drone pitch for subtle movement
        var lfo = ctx.createOscillator();
        var lfoGain = ctx.createGain();
        lfo.type = 'sine';
        lfo.frequency.value = 0.05; // Very slow
        lfoGain.gain.value = 2;
        lfo.connect(lfoGain);
        for (var j = 0; j < SoundState.ambientOscs.length; j++) {
            lfoGain.connect(SoundState.ambientOscs[j].frequency);
        }
        lfo.start();
        SoundState.ambientOscs.push(lfo);

        // Arpeggio layer - plays random notes from scale
        var arpeggioNotes = [261.63, 293.66, 329.63, 392.00, 440.00, 523.25];
        function playArpNote() {
            if (!SoundState.ambientPlaying) return;
            var note = arpeggioNotes[Math.floor(Math.random() * arpeggioNotes.length)];
            var t = ctx.currentTime;
            var noteOsc = ctx.createOscillator();
            var noteGain = ctx.createGain();
            noteOsc.type = 'sine';
            noteOsc.frequency.value = note;
            noteGain.gain.setValueAtTime(0, t);
            noteGain.gain.linearRampToValueAtTime(0.15, t + 0.3);
            noteGain.gain.setValueAtTime(0.15, t + 1.5);
            noteGain.gain.exponentialRampToValueAtTime(0.001, t + 3.0);
            noteOsc.connect(noteGain);
            noteGain.connect(SoundState.ambientGain);
            noteOsc.start(t);
            noteOsc.stop(t + 3.2);

            // Schedule next note with some randomness
            var nextDelay = 2000 + Math.random() * 4000;
            setTimeout(playArpNote, nextDelay);
        }
        setTimeout(playArpNote, 1000);
    },

    SoundPlugin_StopAmbient: function() {
        if (!SoundState.ambientPlaying) return;
        SoundState.ambientPlaying = false;

        if (SoundState.ambientGain) {
            var ctx = SoundState.ctx;
            SoundState.ambientGain.gain.linearRampToValueAtTime(0, ctx.currentTime + 1.0);
        }

        setTimeout(function() {
            for (var i = 0; i < SoundState.ambientOscs.length; i++) {
                try { SoundState.ambientOscs[i].stop(); } catch(e) {}
            }
            SoundState.ambientOscs = [];
        }, 1200);
    },

    SoundPlugin_SetMasterVolume: function(vol) {
        if (SoundState.masterGain) {
            SoundState.masterGain.gain.value = vol;
        }
    }
};

autoAddDeps(SoundPlugin, '$SoundState');
mergeInto(LibraryManager.library, SoundPlugin);
