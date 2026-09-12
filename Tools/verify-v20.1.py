"""Package/source verification, without device access."""
import hashlib, json, os, re, subprocess, sys, zipfile
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
root=Path(__file__).resolve().parents[1]
out=root/'Verification/V20.1'
def sha(p):
    with p.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
snapshot=out/'source-checksums.json'
def verify_sources():
    expected=json.loads(snapshot.read_text())
    def check(item):
        p,h=item
        assert sha(root/p)==h,p
    with ThreadPoolExecutor(max_workers=8) as pool:list(pool.map(check,expected.items()))
    return len(expected)
if '--sources-only' in sys.argv:
    print('SOURCE_SNAPSHOT_OK',verify_sources());sys.exit(0)
if '--snapshot' in sys.argv:
    files=list((root/'Assets/QuestDemonMR/Scripts').glob('*.cs'))
    files+=list((root/'Assets/QuestDemonMR/Resources/Spatial').glob('*.shader'))
    files+=list((root/'Assets/QuestDemonMR/Resources/Art/KatanaV20').glob('*.png'))
    files+=[root/'Assets/QuestDemonMR/Resources/Models/MercyKatanaV20.fbx',root/'Assets/QuestDemonMR/Scenes/Main.unity',root/'ProjectSettings/ProjectSettings.asset']
    with ThreadPoolExecutor(max_workers=8) as pool:
        values=dict(pool.map(lambda p:(str(p.relative_to(root)),sha(p)),files))
    snapshot.write_text(json.dumps(values,indent=2)+'\n')
    print('Snapshot:',len(values));sys.exit(0)
apk=root/'Builds/Purgatory-v20.1-katana-fixes.apk'
sdk=Path('/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer')
bin=sdk/'SDK/build-tools/36.0.0';env=dict(os.environ,JAVA_HOME=str(sdk/'OpenJDK'))
badging=subprocess.check_output([str(bin/'aapt'),'dump','badging',str(apk)],text=True)
assert "versionCode='61'" in badging and "versionName='0.20.1'" in badging
assert "native-code: 'arm64-v8a'" in badging and 'application-debuggable' not in badging
assert "package: name='de.stefanmaier.questdemonmr'" in badging
signing=subprocess.check_output([str(bin/'apksigner'),'verify','--verbose','--print-certs',str(apk)],text=True,env=env)
cert='3d0dae3634ea615e63ce60e6126b1ab7e354124c9d7188992385d9869d39ebd2'
assert cert in signing and 'Verified using v2 scheme (APK Signature Scheme v2): true' in signing
with zipfile.ZipFile(apk) as z:
    assert z.testzip() is None
    metadata=z.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    for name in [b'GroundSteering',b'RouteProgressWatch',b'KatanaVisual']:assert name in metadata
    lib=hashlib.sha256(z.read('lib/arm64-v8a/libil2cpp.so')).hexdigest()
    assert lib!='e8b76da2257195761c775f127843bc73156a3e5460fa1a47535664593a301d60'
verify_sources()
assert sha(root/'Builds/Purgatory-v20.0-katana.apk')=='87146d3c6f8a586aed0b929e9c04dc82dfd607c34cb589dda6b4fb26b53cdaf7'
assert sha(Path('/Users/stefanmaier/Downloads/Katana_VR_Asset/Katana_Szene.blend'))=='2d45ed14ae79e199d60b17f289cfb167bc44091ee2315916f802039b7f03ae9e'
log=max(out.glob('unity-qdmr-v201-export.*.log'),key=lambda p:p.stat().st_mtime);text=log.read_text()
assert 'QDMR_V201_EXPORT_OK' in text and 'QDMR_STARTUP_SERIALIZATION_OK' in text
assert not re.search(r'error CS\d|Shader error|Exception:|TypeDB: Class',text)
report=dict(apk=str(apk),bytes=apk.stat().st_size,sha256=sha(apk),version='0.20.1',code=61,certificate=cert,libil2cpp=lib,source_verified=True,originals_unchanged=True,unity_log=str(log),native=json.loads((out/'native-result.json').read_text()),installed=False,headset_tested=False)
(out/'delivery.json').write_text(json.dumps(report,indent=2)+'\n')
(out/'apk-badging.txt').write_text(badging);(out/'apk-signature.txt').write_text(signing)
print(json.dumps(report,indent=2))
