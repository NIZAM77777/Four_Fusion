"""Refresh the opt-in capture driver in the already-built QA player, using Unity's player compiler response."""
from pathlib import Path
import subprocess
root=Path(__file__).resolve().parents[1]
clone=root/'ValidationWorkspace'
rsp=next((clone/'Library/Bee/artifacts').glob('1900*P*.dag/Assembly-CSharp.rsp')).read_text(encoding='utf-8-sig')
lines=[]
for line in rsp.splitlines():
    if line.startswith('-out:'): line='-out:"'+(root/'.utmp/Player/FourFusion_Data/Managed/Assembly-CSharp.dll').as_posix()+'"'
    if line.startswith('-refout:'): continue
    if 'Assets/Scripts/Utilities/FusionPlayerValidation.cs' in line.replace('\\','/'):
        line='"'+(root/'Assets/Scripts/Utilities/FusionPlayerValidation.cs').as_posix()+'"'
    lines.append(line)
target=root/'.utmp/validation-capture.rsp';target.write_text('\n'.join(lines),encoding='utf-8')
subprocess.run(['dotnet','C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor/Data/DotNetSdkRoslyn/csc.dll','@'+str(target)],cwd=clone,check=True)
