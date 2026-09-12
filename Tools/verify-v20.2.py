"""Package/source verification, without device access."""
import hashlib, json, os, re, subprocess, sys, zipfile
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
root=Path(__file__).resolve().parents[1]
out=root/'Verification/V20.2'
def sha(p):
    with p.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
snapshot=out/'source-checksums.json'
export=Path('/private/tmp/qdmr-v202-export.o5ZIc8')
baseline_export=Path('/private/tmp/qdmr-v201-export.lVLenO')
def audit_generated_settings(expected):
    # Keep the original snapshot untouched. Unity changed this generated file
    # during export; validate its final bytes against the accepted baseline,
    # with ONLY the two authorized version values normalized.
    p=root/'ProjectSettings/ProjectSettings.asset';current=p.read_bytes()
    normalized=current.replace(b'bundleVersion: 0.20.2',b'bundleVersion: 0.20.1').replace(b'AndroidBundleVersionCode: 62',b'AndroidBundleVersionCode: 61')
    baseline=json.loads((root/'Verification/V20.1/source-checksums.json').read_text())['ProjectSettings/ProjectSettings.asset']
    assert hashlib.sha256(normalized).hexdigest()==baseline,'Unexpected final PlayerSettings change'
    boot='unityLibrary/src/main/assets/bin/Data/boot.config'
    clean=lambda b:re.sub(rb'(?m)^build-guid=.*\n?',b'',b)
    assert clean((export/boot).read_bytes())==clean((baseline_export/boot).read_bytes()),'XR startup configuration changed'
    for rel in ['unityLibrary/src/main/AndroidManifest.xml','launcher/src/main/AndroidManifest.xml']:
        assert (export/rel).read_bytes()==(baseline_export/rel).read_bytes(),rel
    properties=(export/'gradle.properties').read_text()
    assert 'unity.versionName=0.20.2\n' in properties and 'unity.versionCode=62\n' in properties
    return dict(pre_export_sha256=expected,post_export_sha256=sha(p),raw_snapshot_changed=True,
        final_settings_match_v201_plus_version=True,xr_boot_matches_v201_except_build_guid=True,
        manifests_byte_identical_to_v201=True,original_snapshot_preserved=True)
def verify_sources():
    expected=json.loads(snapshot.read_text())
    def check(item):
        p,h=item
        if sha(root/p)==h:return None
        if p=='ProjectSettings/ProjectSettings.asset':return audit_generated_settings(h)
        raise AssertionError(p)
    with ThreadPoolExecutor(max_workers=8) as pool:results=list(pool.map(check,expected.items()))
    settings=next((r for r in results if r is not None),None)
    return dict(exact_snapshot_files=len(expected)-(1 if settings else 0),generated_settings_audit=settings)
if '--sources-only' in sys.argv:
    print('SOURCE_AUDIT_OK',json.dumps(verify_sources()));sys.exit(0)
if '--snapshot' in sys.argv:
    files=list((root/'Assets/QuestDemonMR/Scripts').glob('*.cs'))
    files+=list((root/'Assets/QuestDemonMR/Resources/Spatial').glob('*.shader'))
    files+=list((root/'Assets/QuestDemonMR/Resources/Audio/KatanaV202').glob('*.wav'))
    files+=[root/'Assets/QuestDemonMR/Resources/Models/MercyKatanaV20.fbx',root/'Assets/QuestDemonMR/Scenes/Main.unity',root/'ProjectSettings/ProjectSettings.asset']
    with ThreadPoolExecutor(max_workers=8) as pool:
        values=dict(pool.map(lambda p:(str(p.relative_to(root)),sha(p)),files))
    snapshot.write_text(json.dumps(values,indent=2)+'\n')
    print('Snapshot:',len(values));sys.exit(0)
apk=root/'Builds/Purgatory-v20.2-katana-combat-polish.apk'
sdk=Path('/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer')
bin=sdk/'SDK/build-tools/36.0.0';env=dict(os.environ,JAVA_HOME=str(sdk/'OpenJDK'))
badging=subprocess.check_output([str(bin/'aapt'),'dump','badging',str(apk)],text=True)
assert "versionCode='62'" in badging and "versionName='0.20.2'" in badging
assert "native-code: 'arm64-v8a'" in badging and 'application-debuggable' not in badging
assert "package: name='de.stefanmaier.questdemonmr'" in badging
signing=subprocess.check_output([str(bin/'apksigner'),'verify','--verbose','--print-certs',str(apk)],text=True,env=env)
cert='3d0dae3634ea615e63ce60e6126b1ab7e354124c9d7188992385d9869d39ebd2'
assert cert in signing and 'Verified using v2 scheme (APK Signature Scheme v2): true' in signing
with zipfile.ZipFile(apk) as z:
    assert z.testzip() is None
    metadata=z.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    for name in [b'GroundSteering',b'RouteProgressWatch',b'KatanaVisual',b'BladeStrikeKind',b'TryResolveBladeThrust']:assert name in metadata
    lib=hashlib.sha256(z.read('lib/arm64-v8a/libil2cpp.so')).hexdigest()
    assert lib!='e8b76da2257195761c775f127843bc73156a3e5460fa1a47535664593a301d60'
    assert lib!='4d0af9bf2be23bb577b8a57976a0dcbe4bd59137467c44f2532faa8e1b02fac2'
    assert z.read('assets/bin/Data/boot.config')==(export/'unityLibrary/src/main/assets/bin/Data/boot.config').read_bytes()
source_audit=verify_sources()
assert sha(root/'Builds/Purgatory-v20.0-katana.apk')=='87146d3c6f8a586aed0b929e9c04dc82dfd607c34cb589dda6b4fb26b53cdaf7'
assert sha(root/'Builds/Purgatory-v20.1-katana-fixes.apk')=='d6369b0edfe19d908a0102720bac472b6d0c61f6c646bd3b2aa8e9924990e94e'
assert sha(Path('/Users/stefanmaier/Downloads/Katana_VR_Asset/Katana_Szene.blend'))=='2d45ed14ae79e199d60b17f289cfb167bc44091ee2315916f802039b7f03ae9e'
log=max(out.glob('unity-qdmr-v202-export.*.log'),key=lambda p:p.stat().st_mtime);text=log.read_text()
assert 'QDMR_V202_EXPORT_OK' in text and 'QDMR_STARTUP_SERIALIZATION_OK' in text
assert not re.search(r'error CS\d|Shader error|Exception:|TypeDB: Class',text)
report=dict(apk=str(apk),bytes=apk.stat().st_size,sha256=sha(apk),version='0.20.2',code=62,certificate=cert,libil2cpp=lib,source_verified=True,source_audit=source_audit,originals_unchanged=True,unity_log=str(log),native=json.loads((out/'native-result.json').read_text()),installed=False,headset_tested=False)
(out/'delivery.json').write_text(json.dumps(report,indent=2)+'\n')
(out/'apk-badging.txt').write_text(badging);(out/'apk-signature.txt').write_text(signing)
print(json.dumps(report,indent=2))
