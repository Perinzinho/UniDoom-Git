"""50/50 base intensity groups with six independent flickering fixtures."""
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
source=(ROOT/'Tools/Cylinder/AbandonedLighting/revision.py').read_text()
source=source.replace('CylinderAbandonedLightingBefore','CylinderBalancedLightingBefore').replace('/AbandonedLighting','/BalancedLighting').replace('AbandonedLightingEvidence','BalancedLightingEvidence')
start=source.index('WEAK=');end=source.index('\ndef sha(',start)
source=source[:start]+'''WEAK={
    '5929003806968244892':26,'8913311444593308694':28,'8400577148571674640':38,
    '6563073049681176813':3,'8034186246090133953':3.5,
    '7399678302198001166':30,'3832286037084169881':30,'6026105909859072030':27,
    '9019497220995269664':3.2,'185342289156574897':3.4,'611749453700029460':3.5,
}
FLICKERS={
    '7165146227382918089':dict(go='3453314751911998067',amount=.28,speed=2.7,chance=.13,new=False),
    '1148559297890235500':dict(go='2027390000922515434',amount=.22,speed=3.4,chance=.11,new=False),
    '9014101600000000003':dict(go='587428174378470980',amount=.30,speed=2.3,chance=.16,new=True),
}
''' +source[end:]
source=source.replace("len(blocks)+2","len(blocks)+1").replace("else:assert identifier=='7406942453112340403'","else:assert identifier in FLICKERS")
source=source.replace('normal_lights=14','normal_lights=11,weak_light_count=11,total_flickering_lights=6,normal_flickering_lights=3,weak_flickering_lights=3').replace('added_flicker_components=2','added_flicker_components=1')
source=source.replace('changed_existing_blocks=len(modified)','changed_existing_blocks=sum(final[i][1]!=blocks[i][1] for i in blocks)')
source=source.replace('Prepared: 14 steady normal lights, 5 weak lights, 3 independent flickers; positions and ambient unchanged.','Prepared: 11 normal and 11 weak lights, with six flickers (three in each group); positions and ambient unchanged.')
exec(compile(source,__file__,'exec'))
