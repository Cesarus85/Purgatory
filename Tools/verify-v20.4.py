"""Validate signed V20.4 against its native export and immutable source snapshot."""
import hashlib,json,os,re,subprocess,sys,zipfile
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
root=Path(__file__).resolve().parents[1];out=root/'Verification/V20.4'
export=Path(sys.argv[1]);log=Path(sys.argv[2])
assert str(export).startswith('/private/tmp/qdmr-v204-export.')
def sha(p):
    with p.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
expected=json.loads((out/'source-checksums.json').read_text())
def check(item):
    p,h=item
    assert sha(root/p)==h,p
with ThreadPoolExecutor(max_workers=8) as pool:list(pool.map(check,expected.items()))
settings=root/'ProjectSettings/ProjectSettings.asset';pre=Path(str(export)+'.pre-settings')
assert sha(pre)==expected['ProjectSettings/ProjectSettings.asset']
normalized=settings.read_bytes().replace(b'bundleVersion: 0.20.4',b'bundleVersion: 0.20.3').replace(b'AndroidBundleVersionCode: 64',b'AndroidBundleVersionCode: 63')
assert hashlib.sha256(normalized).hexdigest()=='7a294ace347e7eac79db3facaca0444c4b4fb9967a259782826a46d4e96a8ba2','Unexpected PlayerSettings drift'
baseline=Path('/private/tmp/qdmr-v203-export.cY8Dad')
for rel in ['unityLibrary/src/main/AndroidManifest.xml','launcher/src/main/AndroidManifest.xml']:
    assert (export/rel).read_bytes()==(baseline/rel).read_bytes(),rel
boot='unityLibrary/src/main/assets/bin/Data/boot.config'
clean=lambda b:re.sub(rb'(?m)^build-guid=.*\n?',b'',b)
assert clean((export/boot).read_bytes())==clean((baseline/boot).read_bytes())
apk=root/'Builds/Purgatory-v20.4-katana-seal-polish.apk'
sdk=Path('/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer')
bin=sdk/'SDK/build-tools/36.0.0';env=dict(os.environ,JAVA_HOME=str(sdk/'OpenJDK'))
badging=subprocess.check_output([str(bin/'aapt'),'dump','badging',str(apk)],text=True)
assert "versionCode='64'" in badging and "versionName='0.20.4'" in badging
assert "native-code: 'arm64-v8a'" in badging and 'application-debuggable' not in badging
assert "package: name='de.stefanmaier.questdemonmr'" in badging
signing=subprocess.check_output([str(bin/'apksigner'),'verify','--verbose','--print-certs',str(apk)],text=True,env=env)
cert='3d0dae3634ea615e63ce60e6126b1ab7e354124c9d7188992385d9869d39ebd2'
assert cert in signing and 'Verified using v2 scheme (APK Signature Scheme v2): true' in signing
with zipfile.ZipFile(apk) as z:
    assert z.testzip() is None
    metadata=z.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    for name in [b'ReinforcementWait',b'WaitForSupply',b'LiftCutPatch',b'ContactStrength']:assert name in metadata
    lib=hashlib.sha256(z.read('lib/arm64-v8a/libil2cpp.so')).hexdigest()
    assert lib!='50838e0289e25de9c9e7cdee8ddd4cab6aff2d3769f6f86a863f8d3191f57210'
    assert z.read('assets/bin/Data/boot.config')==(export/boot).read_bytes()
assert sha(root/'Builds/Purgatory-v20.3-katana-contact-fixes.apk')=='59cca7806bd76c7640d2ad7a4429c393470dccad1384613c19ca189f08448996'
assert sha(Path('/Users/stefanmaier/Downloads/Katana_VR_Asset/Katana_Szene.blend'))=='2d45ed14ae79e199d60b17f289cfb167bc44091ee2315916f802039b7f03ae9e'
text=log.read_text()
assert 'QDMR_V204_EXPORT_OK' in text and 'QDMR_STARTUP_SERIALIZATION_OK' in text
assert not re.search(r'error CS\d|Shader error|Exception:|TypeDB: Class',text)
report=dict(apk=str(apk),bytes=apk.stat().st_size,sha256=sha(apk),version='0.20.4',code=64,certificate=cert,libil2cpp=lib,
    source_verified=True,source_files=len(expected),pre_settings=sha(pre),post_settings=sha(settings),settings_match_v203_plus_version=True,
    unity_log=str(log),native=json.loads((out/'native-result.json').read_text()),installed=False,headset_tested=False)
(out/'delivery.json').write_text(json.dumps(report,indent=2)+'\n')
(out/'apk-badging.txt').write_text(badging);(out/'apk-signature.txt').write_text(signing)
print(json.dumps(report,indent=2))
