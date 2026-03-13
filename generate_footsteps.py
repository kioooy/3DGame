"""
Generates footstep sound effects as WAV files (grass/ground style).
Uses noise burst + low-pass filter to simulate footstep thuds.
"""
import wave, struct, math, os, random

SAMPLE_RATE = 44100
OUTPUT_DIR = r"d:\New folder (2)\3DGame\Assets\Audio\SFX"

def lowpass(samples, cutoff_hz, sr=SAMPLE_RATE):
    rc = 1.0 / (2 * math.pi * cutoff_hz)
    dt = 1.0 / sr
    alpha = dt / (rc + dt)
    out = [0.0] * len(samples)
    prev = 0.0
    for i, s in enumerate(samples):
        prev = prev + alpha * (s - prev)
        out[i] = prev
    return out

def generate_footstep(duration=0.18, volume=0.75):
    n = int(SAMPLE_RATE * duration)
    # White noise burst with fast decay
    noise = [(random.random() * 2 - 1) * math.exp(-10 * i / n) for i in range(n)]
    # Low-frequency thud component
    thud_freq = 120
    thud = [math.sin(2 * math.pi * thud_freq * i / SAMPLE_RATE) *
            math.exp(-25 * i / n) * 0.7 for i in range(n)]
    combined = [noise[i] + thud[i] for i in range(n)]
    filtered = lowpass(combined, 1200)
    peak = max(abs(s) for s in filtered)
    if peak > 0:
        filtered = [s / peak * volume for s in filtered]
    # Pad with short silence
    filtered += [0.0] * int(SAMPLE_RATE * 0.05)
    return filtered

def save_wav(path, samples):
    int_s = [int(max(-32767, min(32767, s * 32767))) for s in samples]
    with wave.open(path, 'w') as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(SAMPLE_RATE)
        for s in int_s:
            f.writeframes(struct.pack('<h', s))
    print(f"Saved: {path}")

os.makedirs(OUTPUT_DIR, exist_ok=True)

# Generate 4 slightly different footstep variations
for i in range(1, 5):
    random.seed(i * 42)
    samples = generate_footstep(duration=0.16 + i * 0.01, volume=0.7 + i * 0.02)
    save_wav(os.path.join(OUTPUT_DIR, f"footstep_{i}.wav"), samples)

print("Done! Footstep SFX saved to:", OUTPUT_DIR)
