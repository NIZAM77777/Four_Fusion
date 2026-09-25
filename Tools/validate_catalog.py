from pathlib import Path
import json,re
data=json.loads(Path('Assets/Resources/Fusion/Localization.json').read_text(encoding='utf-8'))
tables=data['tables']; expected=set(e['key'] for e in tables[0]['entries'])
assert len(expected)==len(tables[0]['entries'])
assert set(t['code'] for t in tables)=={'en','ar','hi','ml','ta','ur'}
for table in tables:
    keys=[e['key'] for e in table['entries']]
    assert len(keys)==len(set(keys)) and set(keys)==expected, table['code']
    assert all(e['value'].strip() for e in table['entries']), table['code']
    assert table['rtl']==(table['code'] in ('ar','ur'))
    assert Path('Assets/Resources/Fusion/Fonts/'+table['font']+'-Regular.ttf').exists()
used=set()
for path in Path('Assets/Scripts').rglob('*.cs'):
    used.update(re.findall(r'"((?:MENU|MODE|DIFFICULTY|RESULT|BUTTON|AUDIO|PLAYER|TURN|HELP|ERROR|PRIVACY|THEME|VISUAL|OTHER|ADS|UNDO|ABOUT|DROP)_[A-Z_]+)"',path.read_text(encoding='utf-8-sig')))
assert not used-expected, used-expected
print(f'PASS: {len(tables)} complete catalogs, {len(expected)} unique keys each, all referenced UI keys and font files present')
