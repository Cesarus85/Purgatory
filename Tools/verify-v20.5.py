"""Run with project root and actual V20.5 export; safe to stage in /private/tmp."""
import hashlib,json,os,re,subprocess,sys,zipfile
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
root=Path(sys.argv[1]);export=Path(sys.argv[2]);out=root/'Verification/V20.5'
assert str(export).startswith('/private/tmp/qdmr-v205-export.')
def sha(p):
    with p.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
expected=json.loads((out/'source-checksums.json').read_text())
def check(item):
    p,h=item
    assert sha(root/p)==h,p
with ThreadPoolExecutor(max_workers=8) as pool:list(pool.map(check,expected.items()))
print('Source snapshot verified',len(expected),flush=True)
settings=root/'ProjectSettings/ProjectSettings.asset'
normalized=settings.read_bytes().replace(b'bundleVersion: 0.20.5',b'bundleVersion: 0.20.4').replace(b'AndroidBundleVersionCode: 65',b'AndroidBundleVersionCode: 64')
assert hashlib.sha256(normalized).hexdigest()=='6fa6040bbb4632ab5f27cee97e1712eba31ec43ffae466f9364c0939139f5011','Unexpected PlayerSettings drift'
baseline=Path('/private/tmp/qdmr-v204-export.bU8vMT')
for rel in ['unityLibrary/src/main/AndroidManifest.xml','launcher/src/main/AndroidManifest.xml']:
    assert (export/rel).read_bytes()==(baseline/rel).read_bytes(),rel
boot='unityLibrary/src/main/assets/bin/Data/boot.config'
clean=lambda b:re.sub(rb'(?m)^build-guid=.*\n?',b'',b)
assert clean((export/boot).read_bytes())==clean((baseline/boot).read_bytes())
apk=root/'Builds/Purgatory-v20.5-katana-reliability.apk'
sdk=Path('/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer')
bin=sdk/'SDK/build-tools/36.0.0';env=dict(os.environ,JAVA_HOME=str(sdk/'OpenJDK'))
badging=subprocess.check_output([str(bin/'aapt'),'dump','badging',str(apk)],text=True)
assert "versionCode='65'" in badging and "versionName='0.20.5'" in badging
assert "native-code: 'arm64-v8a'" in badging and 'application-debuggable' not in badging
assert "package: name='de.stefanmaier.questdemonmr'" in badging
signing=subprocess.check_output([str(bin/'apksigner'),'verify','--verbose','--print-certs',str(apk)],text=True,env=env)
cert='3d0dae3634ea615e63ce60e6126b1ab7e354124c9d7188992385d9869d39ebd2'
assert cert in signing and 'Verified using v2 scheme (APK Signature Scheme v2): true' in signing
with zipfile.ZipFile(apk) as z:
    assert z.testzip() is None
    metadata=z.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    for name in [b'ObserveSeparation',b'_reverseTravel',b'LiftCutPatch',b'ContactStrength']:assert name in metadata
    lib=hashlib.sha256(z.read('lib/arm64-v8a/libil2cpp.so')).hexdigest()
    assert lib!='f79559fcf6f725da9173f2c25e30ec36b7717b185e9dea6580bc109ddaa8cc2d'
    assert z.read('assets/bin/Data/boot.config')==(export/boot).read_bytes()
assert sha(root/'Builds/Purgatory-v20.4-katana-seal-polish.apk')=='de2a1cb6f2e327600876d0fcf0f8d25330e99b58846048bbedd68f2f1cacefbb'
assert sha(Path('/Users/stefanmaier/Downloads/Katana_VR_Asset/Katana_Szene.blend'))=='2d45ed14ae79e199d60b17f289cfb167bc44091ee2315916f802039b7f03ae9e'
log=Path(str(export)+'.unity.log');text=log.read_text()
assert 'QDMR_V205_EXPORT_OK' in text and 'QDMR_STARTUP_SERIALIZATION_OK' in text
assert not re.search(r'error CS\d|Shader error|Exception:|TypeDB: Class',text)
report=dict(apk=str(apk),bytes=apk.stat().st_size,sha256=sha(apk),version='0.20.5',code=65,certificate=cert,libil2cpp=lib,source_verified=True,source_files=len(expected),post_settings=sha(settings),settings_match_v204_plus_version=True,unity_log=str(log),native=json.loads((out/'native-result.json').read_text()),installed=False,headset_tested=False)
(out/'delivery.json').write_text(json.dumps(report,indent=2)+'\n')
(out/'apk-badging.txt').write_text(badging);(out/'apk-signature.txt').write_text(signing)
print(json.dumps(report,indent=2),flush=True)
