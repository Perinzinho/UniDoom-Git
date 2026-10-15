"""Make weak and flickering fixtures outnumber the normal steady fixtures."""
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
source=(ROOT/'Tools/Cylinder/AbandonedLighting/revision.py').read_text()
source=source.replace('CylinderAbandonedLightingBefore','CylinderDistressedLightingBefore').replace('/AbandonedLighting','/DistressedLighting').replace('AbandonedLightingEvidence','DistressedLightingEvidence')
start=source.index('WEAK=');end=source.index('\ndef sha(',start)
source=source[:start]+'''WEAK={
    '5929003806968244892':26,'8913311444593308694':28,'8400577148571674640':38,
    '6563073049681176813':3,'8034186246090133953':3.5,
    '7399678302198001166':30,'3832286037084169881':30,'6026105909859072030':27,
    '9019497220995269664':3.2,'185342289156574897':3.4,'611749453700029460':3.5,
    '5229933207314824466':30,'2930217622340293113':28,
    '7588875596714975938':30,'2564810946271072936':32,'1417289744319041267':3.1,
}
FLICKERS={
    '9014101600000000004':dict(go='896770317247508660',amount=.24,speed=2.6,chance=.12,new=True),
    '9014101600000000005':dict(go='1931771047059410239',amount=.20,speed=3.3,chance=.10,new=True),
    '9014101600000000006':dict(go='5264245613431465960',amount=.26,speed=2.9,chance=.14,new=True),
    '9014101600000000007':dict(go='6850520161462108264',amount=.28,speed=2.5,chance=.15,new=True),
}
''' +source[end:]
source=source.replace('len(blocks)+2','len(blocks)+4').replace("else:assert identifier=='7406942453112340403'",'else:assert identifier in FLICKERS')
source=source.replace('normal_lights=14','normal_lights=6,weak_light_count=16,total_flickering_lights=10,normal_flickering_lights=0,weak_flickering_lights=10').replace('added_flicker_components=2','added_flicker_components=4')
source=source.replace('changed_existing_blocks=len(modified)','changed_existing_blocks=sum(final[i][1]!=blocks[i][1] for i in blocks)')
source=source.replace('Prepared: 14 steady normal lights, 5 weak lights, 3 independent flickers; positions and ambient unchanged.','Prepared: 16 weak lights (10 flickering), six normal steady lights; positions and ambient unchanged.')
exec(compile(source,__file__,'exec'))
