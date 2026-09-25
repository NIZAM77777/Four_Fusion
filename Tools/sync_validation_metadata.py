"""Copy only missing Unity-generated metadata back from the isolated validation project."""
from pathlib import Path
import shutil
root=Path(__file__).resolve().parents[1]
source=root/'ValidationWorkspace/Assets'
count=0
for meta in source.rglob('*.meta'):
    destination=root/'Assets'/meta.relative_to(source)
    asset=Path(str(destination)[:-5])
    if not destination.exists() and asset.exists():
        shutil.copy2(meta,destination); count+=1
print(f'Copied {count} missing metadata files; existing GUIDs preserved')
