from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
source=(ROOT/'Tools/Cylinder/CanteenDoors/install.py').read_text()
source=source.replace('CylinderCanteenDoorsBefore','CylinderMacabreBefore').replace('CanteenEvidence','MacabreEvidence').replace('/CanteenDoors','/MacabreRevision')
exec(compile(source,__file__,'exec'))
