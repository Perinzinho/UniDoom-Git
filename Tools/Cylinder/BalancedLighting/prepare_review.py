from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
source=(ROOT/'Tools/Cylinder/AbandonedLighting/CylinderAbandonedLightingReview.cs').read_text()
for a,b in [
    ('CylinderAbandonedLightingReview','CylinderBalancedLightingReview'),
    ('AbandonedLightingEvidence','BalancedLightingEvidence'),
    ('CylinderAbandonedLightPlay','CylinderBalancedLightPlay'),
    ('AbandonedLightingPlay.unity','BalancedLightingPlay.unity'),
    ('CYLINDER_ABANDONED_LIGHTING','CYLINDER_BALANCED_LIGHTING'),
    ('flickers.Length==3','flickers.Length==6'),
    ('three active flicker components','six active flicker components'),
    ('new[]{26f,28f,38f,3f,3.5f}','new[]{26f,28f,38f,3f,3.5f,30f,27f,3.2f,3.4f}'),
    ('==5,"Expected five weak lights"','==11,"Expected eleven weak lights"'),
    ('22 live lights: 14 normal, five weak, three flickering.','22 live lights: 11 normal and 11 weak. Six flicker independently: three normal and three weak.'),
    ('with one existing component enabled and two components added.','with two additional existing components enabled and one component added.'),
    ('samples.Count==3','samples.Count==6'),
    ('steady.Count==19','steady.Count==16'),
    ('nineteen steady normal/weak lights','sixteen steady normal/weak lights'),
    ('All three lamps varied','All six lamps varied'),
    ('All nineteen steady','All sixteen steady'),
]:source=source.replace(a,b)
source=source.replace('        foreach(var f in flickers)\n','        Check(flickers.Count(f=>f.GetComponent<Light>().intensity<40)==3,"Expected three weak flickering lights");\n        Check(flickers.Count(f=>f.GetComponent<Light>().intensity>=60)==3,"Expected three normal flickering lights");\n        foreach(var f in flickers)\n')
(ROOT/'Tools/Cylinder/BalancedLighting/CylinderBalancedLightingReview.cs').write_text(source)
