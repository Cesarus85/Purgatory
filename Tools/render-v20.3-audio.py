"""Short fire/plasma deflection, sharing PortalRift modulation without metallic resonance."""
from pathlib import Path
import math, random, wave, array, json
root=Path(__file__).resolve().parents[1]
out=root/'Assets/QuestDemonMR/Resources/Audio/KatanaV203'
out.mkdir(parents=True,exist_ok=True)
rate=44100;duration=.48;rng=random.Random(20303);data=[];low=mid=air=0.
for i in range(int(rate*duration)):
    t=i/rate;n=rng.uniform(-1,1)
    low+=.025*(n-low);mid+=.16*(n-mid);air+=.48*(n-air)
    # Match the old projectile's modulated rift, slightly higher than its 1.55 pitch.
    rift=math.sin(t*110*1.85+math.sin(t*17*1.85)*2)*.12
    pressure=low*2.4*math.exp(-t*18)
    flame=(mid-low)*1.3*(1+.22*math.sin(t*87))*math.exp(-t*8)
    flare=(air-mid)*.23*math.exp(-t*28)
    value=pressure+flame+flare+rift*math.exp(-t*12)
    data.append(value*min(1,t/.006)*min(1,(duration-t)/.065))
gain=.79/max(abs(v) for v in data)
pcm=array.array('h',(round(v*gain*32767) for v in data))
with wave.open(str(out/'Parry.wav'),'wb') as f:
    f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes(pcm.tobytes())
report=dict(duration=duration,peak_dbfs=20*math.log10(.79),rms=math.sqrt(sum((v*gain)**2 for v in data)/len(data)),metallic_oscillator=False,loop=False)
verify=root/'Verification/V20.3';verify.mkdir(parents=True,exist_ok=True)
(verify/'audio.json').write_text(json.dumps(report,indent=2)+'\n')
print('V203_AUDIO_OK',report)
