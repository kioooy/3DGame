"""
Generates 8-bit style sound effects for Minigames (Caro, Arm Wrestling).
Outputs to: Assets/Audio/SFX/
"""
import wave, struct, math, os, random

SAMPLE_RATE = 44100
OUTPUT_DIR = r"d:\New folder (2)\3DGame\Assets\Audio\SFX"

def square_wave(freq, duration, volume=0.5):
    n = int(SAMPLE_RATE * duration)
    return [volume if (i * freq / SAMPLE_RATE) % 1.0 < 0.5 else -volume for i in range(n)]

def sine_wave(freq, duration, volume=0.5):
    n = int(SAMPLE_RATE * duration)
    return [math.sin(2 * math.pi * freq * i / SAMPLE_RATE) * volume for i in range(n)]

def noise_burst(duration, volume=0.5):
    n = int(SAMPLE_RATE * duration)
    return [(random.random() * 2 - 1) * volume for i in range(n)]

def apply_envelope(samples, attack_time, decay_time, sustain_level, release_time):
    # Dạng ADSR cơ bản
    n = len(samples)
    dur = n / SAMPLE_RATE
    attack_samples = int(attack_time * SAMPLE_RATE)
    decay_samples = int(decay_time * SAMPLE_RATE)
    release_samples = int(release_time * SAMPLE_RATE)
    
    for i in range(n):
        if i < attack_samples:
            env = i / max(1, attack_samples)
        elif i < attack_samples + decay_samples:
            env = 1.0 - (1.0 - sustain_level) * ((i - attack_samples) / max(1, decay_samples))
        elif i < n - release_samples:
            env = sustain_level
        else:
            env = sustain_level * (n - i) / max(1, release_samples)
        samples[i] *= env
    return samples

def save_wav(name, samples):
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    path = os.path.join(OUTPUT_DIR, name)
    # Normalize
    peak = max(abs(s) for s in samples) if samples else 1.0
    if peak > 0:
        samples = [s / peak * 0.8 for s in samples]
        
    int_s = [int(max(-32767, min(32767, s * 32767))) for s in samples]
    with wave.open(path, 'w') as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(SAMPLE_RATE)
        for s in int_s:
            f.writeframes(struct.pack('<h', s))
    print(f"Saved: {path}")

# 1. Click (Caro, UI) - Short pop
click = square_wave(800, 0.05, 0.5)
click = apply_envelope(click, 0.005, 0.045, 0.0, 0.0)
save_wav("sfx_ui_click.wav", click)

# 2. Correct (Vật tay) - Ping/Coin sound (B5 -> E6)
part1 = square_wave(987.77, 0.08, 0.5) # B5
part2 = square_wave(1318.51, 0.2, 0.5) # E6
correct = apply_envelope(part1 + part2, 0.01, 0.1, 0.5, 0.1)
save_wav("sfx_ui_correct.wav", correct)

# 3. Wrong (Vật tay) - Buzzy low tone (F3)
wrong = square_wave(174.61, 0.3, 0.6)
wrong = apply_envelope(wrong, 0.05, 0.1, 0.8, 0.1)
# Mix with noise for buzzy effect
noise = noise_burst(0.3, 0.2)
wrong = [w + n for w, n in zip(wrong, noise)]
save_wav("sfx_ui_wrong.wav", wrong)

# 4. Win - Tada! (C -> E -> G -> C)
w1 = square_wave(523.25, 0.1, 0.5) # C5
w2 = square_wave(659.25, 0.1, 0.5) # E5
w3 = square_wave(783.99, 0.1, 0.5) # G5
w4 = square_wave(1046.50, 0.4, 0.5) # C6
w1 = apply_envelope(w1, 0.01, 0.09, 0, 0)
w2 = apply_envelope(w2, 0.01, 0.09, 0, 0)
w3 = apply_envelope(w3, 0.01, 0.09, 0, 0)
w4 = apply_envelope(w4, 0.01, 0.1, 0.8, 0.2)
save_wav("sfx_ui_win.wav", w1 + w2 + w3 + w4)

# 5. Lose - Wah wah wah (Descending G -> Gb -> F -> E)
l1 = square_wave(392.00, 0.3, 0.5)
l2 = square_wave(369.99, 0.3, 0.5)
l3 = square_wave(349.23, 0.3, 0.5)
l4 = square_wave(329.63, 0.6, 0.5)
# Slide effect on l4
l4_slide = []
for i in range(len(l4)):
    freq = 329.63 - (i / len(l4)) * 50 # Pitch drop
    l4_slide.append(math.sin(2 * math.pi * freq * i / SAMPLE_RATE) * 0.5)
l4_slide = [s if (s > 0) else -0.5 for s in l4_slide] # Convert to square

win_samples = l1 + [0]*int(SAMPLE_RATE*0.05) + l2 + [0]*int(SAMPLE_RATE*0.05) + l3 + [0]*int(SAMPLE_RATE*0.05) + l4_slide
lose = apply_envelope(win_samples, 0.05, 0.0, 1.0, 0.3)
save_wav("sfx_ui_lose.wav", lose)

# 6. Impact (Racing) - Low thump
thump = sine_wave(100, 0.2, 0.8)
thump = apply_envelope(thump, 0.01, 0.1, 0.1, 0.05)
save_wav("sfx_racing_impact.wav", thump)

# 7. Jump (Racing) - Slide up sine wave
jump_samples = []
for i in range(int(SAMPLE_RATE * 0.15)):
    freq = 300 + (i / (SAMPLE_RATE * 0.15)) * 400 # Pitch slide up from 300 to 700
    jump_samples.append(math.sin(2 * math.pi * freq * i / SAMPLE_RATE) * 0.5)
jump_samples = apply_envelope(jump_samples, 0.02, 0.05, 0.2, 0.08)
save_wav("sfx_racing_jump.wav", jump_samples)

print("SFX generation complete.")
