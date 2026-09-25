from pathlib import Path
import re, json
changes=[]
for p in Path('Assets/UI').rglob('*.png.meta'):
    text=p.read_text(encoding='utf-8-sig')
    if '/Coins/' not in p.as_posix() and 'BoardHole' not in p.name: continue
    target=512
    pattern=r'(  - serializedVersion: \d+\n    buildTarget: Android\n)(.*?)(?=  - serializedVersion:|  spriteSheet:)'
    match=re.search(pattern,text,re.S)
    if not match: continue
    block=match.group(2)
    block=re.sub(r'maxTextureSize: \d+',f'maxTextureSize: {target}',block)
    block=re.sub(r'textureFormat: -?\d+','textureFormat: 47',block) # ETC2 RGBA8; GLES3 baseline supported
    block=re.sub(r'overridden: \d+','overridden: 1',block)
    changed=text[:match.start(2)]+block+text[match.end(2):]
    if changed!=text:
        p.write_text(changed,encoding='utf-8')
    changes.append(str(p))
for p in Path('Assets/Audio').rglob('*.meta'):
    text=p.read_text(encoding='utf-8-sig')
    if 'AudioImporter:' not in text: continue
    music='background music' in str(p)
    text=re.sub(r'loadType: \d+', 'loadType: '+('2' if music else '0'), text)
    text=re.sub(r'quality: [\d.]+','quality: 0.7',text)
    text=text.replace('loadInBackground: 0','loadInBackground: 1').replace('  3D: 1','  3D: 0')
    if not music: text=text.replace('forceToMono: 0','forceToMono: 1')
    p.write_text(text,encoding='utf-8'); changes.append(str(p))
Path('Documentation/Audit/import-changes.json').write_text(json.dumps(changes,indent=2),encoding='utf-8')
print(f'Updated {len(changes)} import settings')
