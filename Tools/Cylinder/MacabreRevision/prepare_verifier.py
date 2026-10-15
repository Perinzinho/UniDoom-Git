from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
s=(ROOT/'Tools/Cylinder/CanteenDoors/CylinderCanteenVerifier.cs').read_text()
for a,b in [('CylinderCanteenVerifier','CylinderMacabreVerifier'),('CylinderCanteenBuilder.Prefab','CylinderMacabreBuilder.Prefab'),('CanteenEvidence/','MacabreEvidence/'),('CylinderCanteenBuilder.OptimizeGenerated(); Capture();','Capture();'),('CanteenDoorPlay','MacabreDoorPlay'),('CanteenDoorPhase','MacabreDoorPhase'),('CanteenDoorStart','MacabreDoorStart'),('CYLINDER_CANTEEN','CYLINDER_MACABRE')]:
    s=s.replace(a,b)
s=s.replace('        Capture(); Debug.Log("CYLINDER_MACABRE_EDIT_PASS");','        VerifyNewProps(room); Capture(); Debug.Log("CYLINDER_MACABRE_EDIT_PASS");')
if (ROOT/'Tools/Cylinder/MacabreRevision/CylinderMacabreVerifier.cs').exists():
    raise SystemExit('Verifier already extended; refusing to overwrite manual validation additions')
(ROOT/'Tools/Cylinder/MacabreRevision/CylinderMacabreVerifier.cs').write_text(s)
