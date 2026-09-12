"""Verify the finished V20 package without touching a connected device."""
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import zipfile
from concurrent.futures import ThreadPoolExecutor

root = Path(__file__).resolve().parents[1]
apk = root / 'Builds/Purgatory-v20.0-katana.apk'
sdk = Path('/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer')
build_tools = sdk / 'SDK/build-tools/36.0.0'
env = dict(os.environ, JAVA_HOME=str(sdk / 'OpenJDK'))
def sha(path):
    with open(path, 'rb') as source:
        return hashlib.file_digest(source, 'sha256').hexdigest()
badging = subprocess.check_output([str(build_tools / 'aapt'), 'dump', 'badging', str(apk)], text=True)
assert "versionCode='60'" in badging and "versionName='0.20.0'" in badging
assert "native-code: 'arm64-v8a'" in badging and 'application-debuggable' not in badging
assert "sdkVersion:'29'" in badging and "targetSdkVersion:'36'" in badging
signing = subprocess.check_output([str(build_tools / 'apksigner'), 'verify', '--verbose', '--print-certs', str(apk)], text=True, env=env)
certificate = '3d0dae3634ea615e63ce60e6126b1ab7e354124c9d7188992385d9869d39ebd2'
assert certificate in signing and 'Verified using v2 scheme (APK Signature Scheme v2): true' in signing
with zipfile.ZipFile(apk) as package:
    assert package.testzip() is None
    library = hashlib.sha256(package.read('lib/arm64-v8a/libil2cpp.so')).hexdigest()
    assert library != '2de3254ffbc8eff8e2a4166cc38df872e27ec52cedee25f73785679cc0d36993'
    metadata = package.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    assert len(metadata) > 100000
    for name in (b'KatanaVisual', b'PerkWeaponController', b'ChainPenitent'):
        assert name in metadata, f'New runtime type missing: {name}'
baseline = {
    root / 'Builds/Purgatory-v19.20-startup-fix.apk': '749ffe6e41fc538b52e520b5b2f554dd395ff25cc73bd33b07f1639985131847',
    root / 'Assets/QuestDemonMR/Resources/Models/EmberfiendAnimatedV12.fbx': '7fa8590ca2645c9fd53485b3bc5b37ff829478761da521424529d31d57dd5366',
    Path('/Users/stefanmaier/Downloads/Katana_VR_Asset/Katana_Szene.blend'): '2d45ed14ae79e199d60b17f289cfb167bc44091ee2315916f802039b7f03ae9e',
}
for path, expected in baseline.items():
    assert sha(path) == expected, f'Original changed: {path}'
logs = sorted((root / 'Verification/V20').glob('unity-qdmr-v200-export.*.log'), key=lambda p:p.stat().st_mtime)
log = logs[-1]
content = log.read_text()
assert 'QDMR_V20_EXPORT_OK' in content and 'QDMR_STARTUP_SERIALIZATION_OK' in content
assert not re.search(r'error CS\d|Shader error|Exception:|TypeDB: Class', content)
print('APK version, signature, ZIP, runtime metadata and original files verified.',flush=True)
entries = [line.split('  ',1) for line in (root/'Verification/V20/final-source-checksums.txt').read_text().splitlines()]
def check_source(entry):
    expected,path = entry
    assert sha(root/path)==expected, f'Post-export source change: {path}'
with ThreadPoolExecutor(max_workers=12) as readers:
    list(readers.map(check_source,entries))
ensemble = root / 'Verification/V20/ensemble-3.log'
assert 'QDMR_V20_ENSEMBLE_CAPTURE_OK' in ensemble.read_text()
report = {
    'apk': str(apk), 'bytes': apk.stat().st_size, 'sha256': sha(apk),
    'version_name': '0.20.0', 'version_code': 60, 'package': re.search(r"package: name='([^']+)'", badging)[1],
    'abi': 'arm64-v8a', 'min_sdk':29, 'target_sdk':36, 'debuggable':False,
    'zip_verified': True, 'certificate_sha256':certificate, 'signature_v2':True,
    'libil2cpp_sha256': library, 'unchanged_originals': {str(p):h for p,h in baseline.items()},
    'unity_log':str(log), 'native_validation':json.loads((root/'Verification/V20/native-result.json').read_text()),
    'startup_serialization_guard':True, 'installed':False, 'headset_tested':False,
    'pre_export_source_snapshot_verified':True, 'post_export_editor_only_ensemble_log':str(ensemble),
    'open_acceptance':['V19-D gameplay and performance','V20-A stereo ensemble','V20-D controller comfort, combat, thermal performance'],
}
(root / 'Verification/V20/delivery.json').write_text(json.dumps(report, indent=2)+'\n')
(root / 'Verification/V20/apk-badging.txt').write_text(badging)
(root / 'Verification/V20/apk-signature.txt').write_text(signing)
print(json.dumps(report, indent=2))
