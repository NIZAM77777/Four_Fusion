"""Read-only asset/hierarchy inventory; run before migration and retain the baseline."""
from pathlib import Path
import json, re, collections
from PIL import Image

root = Path(__file__).resolve().parents[1]
assets = root / 'Assets'
report = {'files': [], 'scenes': {}, 'textures': [], 'audio_imports': {}}
for p in sorted(assets.rglob('*')):
    if not p.is_file() or p.suffix == '.meta': continue
    relative = p.relative_to(root).as_posix()
    report['files'].append({'path': relative, 'bytes': p.stat().st_size})
    if p.suffix == '.unity':
        text = p.read_text(encoding='utf-8-sig')
        report['scenes'][relative] = {'objects': re.findall(r'^  m_Name: (.+)$', text, re.M),
            'scripts': re.findall(r'm_Script:.*guid: (\w+)', text),
            'actions': re.findall(r'm_MethodName: (.+)', text)}
    if p.suffix.lower() in ('.png', '.jpg', '.jpeg', '.psd'):
        try:
            with Image.open(p) as im:
                report['textures'].append({'path':relative,'size':im.size,'rgba_bytes':im.width*im.height*4})
        except (OSError, ValueError): pass
    if p.suffix.lower() in ('.mp3', '.wav', '.ogg'):
        report['audio_imports'][relative] = Path(str(p)+'.meta').read_text()
report['extension_counts'] = dict(collections.Counter(Path(x['path']).suffix for x in report['files']))
output = root / 'Documentation' / 'Audit'
output.mkdir(parents=True, exist_ok=True)
(output / 'baseline-inventory.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(f"Inspected {len(report['files'])} assets, {len(report['scenes'])} scenes, {len(report['textures'])} textures")
