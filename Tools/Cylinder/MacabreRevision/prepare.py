from pathlib import Path
import runpy

ROOT = Path(__file__).resolve().parents[3]
source = (ROOT / 'Tools/Cylinder/CanteenDoors/prepare.py').read_text()
source = source.replace('CylinderCanteenDoorsBefore', 'CylinderMacabreBefore')
source = source.replace('CylinderCanteenGeometry', 'CylinderMacabreGeometry')
source = source.replace('/CanteenDoors', '/MacabreRevision')
source = source.replace('CylinderCanteenGeometry.cs', 'CylinderMacabreGeometry.cs')
exec(compile(source, __file__, 'exec'))
