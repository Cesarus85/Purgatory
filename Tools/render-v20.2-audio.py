"""Bounded, deterministic katana Foley; no sustained bells or ambient loops."""
from pathlib import Path
import math, random, wave, array, json, shutil
root=Path(__file__).resolve().parents[1]
out=root/'Assets/QuestDemonMR/Resources/Audio/KatanaV202';out.mkdir(parents=True,exist_ok=True)
report={};rate=44100
for cue,duration in [('Swing',.20),('Parry',.29),('Thrust',.16)]:
    rng=random.Random(20202+len(cue));data=[];low=0;mid=0
    for i in range(int(rate*duration)):
        t=i/rate;u=t/duration;n=rng.uniform(-1,1)
        low+=.035*(n-low);mid+=.22*(n-mid)
        if cue=='Swing':value=(mid-low)*math.sin(math.pi*u)**2
        elif cue=='Parry':
            body=math.sin(2*math.pi*(165*t-110*t*t))*.7*math.exp(-t*32)
            contact=mid*.32*math.exp(-t*100)
            metal=math.sin(2*math.pi*430*t)*.11*math.exp(-t*65)
            extinguish=(mid-low)*.19*math.exp(-t*19)*(1-math.exp(-t*90))
            value=body+contact+metal+extinguish
        else:value=low*.9*math.exp(-t*36)+math.sin(2*math.pi*(120*t-80*t*t))*.23*math.exp(-t*40)+mid*.08*math.exp(-t*55)
        data.append(value*min(1,t/.002)*min(1,(duration-t)/.012))
    peak=max(abs(v) for v in data);gain=.79/peak
    pcm=array.array('h',(int(v*gain*32767) for v in data))
    with wave.open(str(out/(cue+'.wav')),'wb') as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes(pcm.tobytes())
    report[cue]={'duration':duration,'peak_dbfs':20*math.log10(.79),'tail_rms':math.sqrt(sum(v*v for v in data[-2205:])/2205)*gain}
for cue in ['Flesh','Armour','Arrival','Departure']:
    shutil.copyfile(root/('Assets/QuestDemonMR/Resources/Audio/KatanaV20/'+cue+'.wav'),out/(cue+'.wav'))
verify=root/'Verification/V20.2';verify.mkdir(parents=True,exist_ok=True)
(verify/'audio.json').write_text(json.dumps(report,indent=2)+'\n')
print('V202_AUDIO_OK',report)
