"""Deterministic authored katana Foley and a derived, chain-laden creature bank.
No network assets. Original A6 voice provenance remains in ExternalSource.
"""
from pathlib import Path
import math, random, wave, array, json
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/QuestDemonMR/Resources/Audio/KatanaV20';OUT.mkdir(parents=True,exist_ok=True)
rate=44100;random.seed(2020)
def write(path,data,sr=rate):
    peak=max(abs(v) for v in data) or 1
    # -2dBFS leaves room for simultaneous stereo weapon/creature mix.
    samples=array.array('h',(int(max(-1,min(1,v/peak*.79))*32767) for v in data))
    path.parent.mkdir(parents=True,exist_ok=True)
    with wave.open(str(path),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(sr);f.writeframes(samples.tobytes())
report={}
for cue,duration in [('Swing',.32),('Flesh',.25),('Armour',.42),('Parry',.56),('Arrival',.62),('Departure',.48)]:
    data=[];low=0;prev=0
    for i in range(int(rate*duration)):
        t=i/rate;u=t/duration;n=random.uniform(-1,1);low=.88*low+.12*n;high=n-prev;prev=n
        attack=min(1,t/.002);tail=min(1,(duration-t)/.012)
        if cue=='Swing':v=(low*.9+high*.06)*math.sin(math.pi*u)**2*(.4+.6*u)
        elif cue=='Flesh':v=(low*.7+high*.16)*math.exp(-t*23)+math.sin(2*math.pi*(145*t-110*t*t))*.3*math.exp(-t*29)
        elif cue in ('Armour','Parry'):
            freq=1720 if cue=='Parry' else 820
            v=high*.16*math.exp(-t*80)+sum(math.sin(2*math.pi*freq*k*t)*math.exp(-t*(8+j*4))/(5+j*3) for j,k in enumerate([1,1.417,2.31,3.72]))
        else:
            v=low*.3*math.sin(math.pi*u)**2+sum(math.sin(2*math.pi*f*t)*.10*math.sin(math.pi*u)**2 for f in ([392,587.33,784] if cue=='Arrival' else [784,523.25,392]))
        data.append(v*attack*tail)
    write(OUT/(cue+'.wav'),data);report[cue]={'seconds':duration,'peak_dbfs':-2.05}
source=ROOT/'Assets/QuestDemonMR/Resources/Audio/EnemyV18/CinderBrute'
for path in source.rglob('*.wav'):
    with wave.open(str(path),'rb') as f:
        assert f.getsampwidth()==2
        sr=f.getframerate();channels=f.getnchannels();data=array.array('h',f.readframes(f.getnframes()))
    mono=[sum(data[i:i+channels])/(32768*channels) for i in range(0,len(data),channels)]
    # Broken iron: short, irregular partials beneath the existing vocal core.
    result=[]
    for i,v in enumerate(mono):
        t=i/sr;clang=0
        for onset in (.07,.23,.51):
            age=t-onset
            if 0<age<.18:clang+=sum(math.sin(2*math.pi*f*age)*math.exp(-age*32)*.022 for f in (1180,1697,2713))
        result.append(v+clang)
    write(ROOT/'Assets/QuestDemonMR/Resources/Audio/EnemyV18/ChainPenitent'/path.relative_to(source),result,int(sr*.92))
(ROOT/'Verification/V20/audio.json').write_text(json.dumps(report,indent=2))
print('V20_AUDIO_OK',len(list((source.parent/'ChainPenitent').rglob('*.wav'))),'chain clips')
