"""
Generates Minecraft-style ambient piano music as WAV files.
Creates 3 tracks with different melodies using sine wave synthesis with ADSR envelope.
"""
import wave
import struct
import math
import os

SAMPLE_RATE = 44100
OUTPUT_DIR = r"d:\New folder (2)\3DGame\Assets\Audio\Music"

def adsr_envelope(t, duration, attack=0.08, decay=0.1, sustain=0.6, release=0.5):
    if t < attack:
        return t / attack
    elif t < attack + decay:
        return 1.0 - (1.0 - sustain) * (t - attack) / decay
    elif t < duration - release:
        return sustain
    elif t < duration:
        return sustain * (1.0 - (t - (duration - release)) / release)
    return 0.0

def sine_wave(freq, duration, volume=0.4, sample_rate=SAMPLE_RATE):
    num_samples = int(sample_rate * duration)
    samples = []
    for i in range(num_samples):
        t = i / sample_rate
        envelope = adsr_envelope(t, duration)
        value = (
            math.sin(2 * math.pi * freq * t) * 1.0 +
            math.sin(2 * math.pi * freq * 2 * t) * 0.3 +
            math.sin(2 * math.pi * freq * 3 * t) * 0.1 +
            math.sin(2 * math.pi * freq * 4 * t) * 0.05
        )
        value = value / 1.45 * envelope * volume
        samples.append(value)
    return samples

def silence(duration, sample_rate=SAMPLE_RATE):
    return [0.0] * int(sample_rate * duration)

def note_freq(note):
    return 440.0 * (2 ** ((note - 69) / 12.0))

def save_wav(filename, samples, sample_rate=SAMPLE_RATE):
    int_samples = [int(max(-32767, min(32767, s * 32767))) for s in samples]
    with wave.open(filename, 'w') as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(sample_rate)
        for s in int_samples:
            f.writeframes(struct.pack('<h', s))
    print(f"Saved: {filename} ({len(samples)/sample_rate:.1f}s)")

def build_track(melody_notes, bass_notes=None, intro_silence=3.0):
    all_samples = silence(intro_silence)
    for note, dur, gap in melody_notes:
        all_samples += sine_wave(note_freq(note), dur, volume=0.38)
        all_samples += silence(gap)

    if bass_notes:
        bass_track = silence(intro_silence)
        for note, dur, gap in bass_notes:
            bass_track += sine_wave(note_freq(note), dur, volume=0.14)
            bass_track += silence(gap)
        max_len = max(len(all_samples), len(bass_track))
        all_samples += [0.0] * (max_len - len(all_samples))
        bass_track  += [0.0] * (max_len - len(bass_track))
        all_samples = [a + b for a, b in zip(all_samples, bass_track)]

    peak = max(abs(s) for s in all_samples) if all_samples else 1.0
    if peak > 0:
        all_samples = [s / peak * 0.85 for s in all_samples]
    all_samples += silence(4.0)
    return all_samples

# MIDI notes
C3, D3, E3, F3, G3, A3, B3 = 48, 50, 52, 53, 55, 57, 59
C4, D4, E4, F4, G4, A4, B4 = 60, 62, 64, 65, 67, 69, 71
C5, D5, E5, G5, A5 = 72, 74, 76, 79, 81

# Track 1 - Sweden inspired
track1_melody = [
    (E4,1.2,0.6),(G4,0.8,0.4),(A4,1.5,0.8),
    (G4,0.6,0.3),(E4,1.0,0.5),(C4,2.0,1.2),
    (D4,1.0,0.5),(F4,0.8,0.4),(G4,1.5,0.8),
    (F4,0.6,0.3),(D4,1.0,0.5),(C4,2.5,1.5),
    (E4,1.2,0.6),(G4,0.8,0.4),(C5,2.0,1.0),
    (B4,0.8,0.4),(A4,1.0,0.5),(G4,2.0,1.2),
    (F4,1.0,0.5),(E4,0.8,0.4),(D4,1.5,0.8),
    (C4,3.0,2.0),
]
track1_bass = [
    (C3,2.0,4.5),(G3,1.5,5.0),(A3,1.5,5.0),
    (F3,1.5,4.0),(C3,1.5,5.5),(G3,2.0,5.0),
    (E3,1.5,5.5),(C3,3.0,0.0),
]

# Track 2 - Wet Hands inspired
track2_melody = [
    (C4,0.8,0.3),(E4,0.8,0.3),(G4,1.0,0.5),
    (A4,1.5,0.8),(G4,0.6,0.3),(F4,1.0,0.5),
    (E4,1.5,0.8),(D4,0.8,0.4),(C4,2.0,1.5),
    (G4,0.8,0.3),(A4,0.8,0.3),(B4,1.0,0.5),
    (C5,2.0,1.0),(B4,0.6,0.3),(A4,0.8,0.4),
    (G4,1.5,0.8),(F4,0.6,0.3),(E4,1.0,0.5),
    (D4,1.5,0.8),(C4,3.0,2.0),
]
track2_bass = [
    (C3,2.4,2.4),(F3,1.8,3.0),(G3,1.8,3.0),
    (A3,1.8,3.0),(G3,1.8,3.0),(F3,1.8,3.0),(C3,3.0,0.0),
]

# Track 3 - Living Mice inspired (minor)
Am, Bm, Cm, Dm, Em = 57, 59, 60, 62, 64
track3_melody = [
    (Am,1.0,0.5),(Cm,0.8,0.4),(Em,1.5,0.8),
    (Dm,1.0,0.5),(Cm,0.8,0.4),(Bm,1.5,0.8),
    (Am,2.0,1.5),(Em,1.0,0.5),(Am,0.8,0.4),
    (Cm,1.5,0.8),(Dm,1.0,0.5),(Em,1.5,0.8),
    (Cm,0.8,0.4),(Bm,1.0,0.5),(Am,2.5,2.0),
]
track3_bass = [
    (A3-12,2.0,4.0),(C3,1.5,4.5),(E3,1.5,4.5),
    (D3,1.5,4.5),(A3-12,3.0,0.0),
]

os.makedirs(OUTPUT_DIR, exist_ok=True)

tracks = [
    ("calm_sweden.wav", track1_melody, track1_bass),
    ("wet_hands.wav",   track2_melody, track2_bass),
    ("living_mice.wav", track3_melody, track3_bass),
]

for filename, melody, bass in tracks:
    path = os.path.join(OUTPUT_DIR, filename)
    samples = build_track(melody, bass, intro_silence=2.0)
    save_wav(path, samples)

print("\nDone! All 3 tracks saved to:", OUTPUT_DIR)
