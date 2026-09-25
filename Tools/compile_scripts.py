from pathlib import Path
import subprocess, re
root=Path(__file__).resolve().parents[1]
rsp=next((root/'Library/Bee/artifacts').glob('*E.dag/Assembly-CSharp.rsp')).read_text(encoding='utf-8-sig')
lines=[]
for line in rsp.splitlines():
    if line.startswith('-out:'): line='-out:".utmp/FusionCompile.dll"'
    if line.startswith('-refout:'): continue
    if line.strip('"').endswith('.cs'): continue
    lines.append(line)
lines.extend('"'+p.relative_to(root).as_posix()+'"' for p in (root/'Assets/Scripts').rglob('*.cs'))
(root/'.utmp/compile.rsp').write_text('\n'.join(lines),encoding='utf-8')
csc=next(Path('C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor/Data').rglob('csc.dll'))
result=subprocess.run(['dotnet',str(csc),'@.utmp/compile.rsp'],cwd=root,capture_output=True,text=True)
print(result.stdout,result.stderr)
raise SystemExit(result.returncode)
