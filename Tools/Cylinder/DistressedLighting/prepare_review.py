from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
source=(ROOT/'Tools/Cylinder/AbandonedLighting/CylinderAbandonedLightingReview.cs').read_text()
for a,b in [
    ('CylinderAbandonedLightingReview','CylinderDistressedLightingReview'),
    ('AbandonedLightingEvidence','DistressedLightingEvidence'),
    ('CylinderAbandonedLightPlay','CylinderDistressedLightPlay'),
    ('AbandonedLightingPlay.unity','DistressedLightingPlay.unity'),
    ('CYLINDER_ABANDONED_LIGHTING','CYLINDER_DISTRESSED_LIGHTING'),
    ('flickers.Length==3','flickers.Length==10'),
    ('three active flicker components','ten active flicker components'),
    ('new[]{26f,28f,38f,3f,3.5f}','new[]{26f,28f,38f,3f,3.5f,30f,27f,3.2f,3.4f,32f,3.1f}'),
    ('==5,"Expected five weak lights"','==16,"Expected sixteen weak lights"'),
    ('22 live lights: 14 normal, five weak, three flickering.','22 live lights: 16 weak (10 flickering) and six normal steady. Both weak and flickering outnumber normal.'),
    ('with one existing component enabled and two components added.','with four additional components on existing lights.'),
    ('samples.Count==3','samples.Count==10'),
    ('steady.Count==19','steady.Count==12'),
    ('nineteen steady normal/weak lights','twelve steady normal/weak lights'),
    ('All three lamps varied','All ten lamps varied'),
    ('All nineteen steady','All twelve steady'),
]:source=source.replace(a,b)
source=source.replace('        foreach(var f in flickers)\n','        Check(flickers.All(f=>f.GetComponent<Light>().intensity<40),"All ten flickers must belong to the weak group");\n        Check(lights.Count(l=>l.intensity==65f||l.intensity==7f||l.intensity==6f)==6,"Expected six normal steady lights");\n        foreach(var f in flickers)\n')
(ROOT/'Tools/Cylinder/DistressedLighting/CylinderDistressedLightingReview.cs').write_text(source)
