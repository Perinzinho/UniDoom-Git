"""Describe the approved existing routes; these diagrams never rebuild room geometry."""
from pathlib import Path
import json
import sys

sys.stdout.reconfigure(encoding='utf-8')
ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT/'Source/total-chaos-unity-maps/scripts'))
from validate_layout import main as validate
from preview_layout import render


def save(name, theme, grid, height, door, metadata):
    data = {'name': name, 'theme': theme, 'cellSize': 2, 'wallHeight': height,
            'doorHeight': door, 'ceiling': True, 'autoShell': True,
            'textureMeters': 3, 'seed': 1718, 'preserveExistingGeometry': True,
            'purpose': 'Schematic of approved existing routes and atmosphere cues, not replacement geometry.',
            'grid': [''.join(row) for row in grid], 'existingMap': metadata}
    path = ROOT/'Layouts'/f'{name}.json'
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
    assert validate(str(path)) == 0, name
    render(str(path), str(path.with_suffix('.png')))


def grid(w, h):
    return [[' ']*w for _ in range(h)]


def rect(g, x0, y0, x1, y1, char='.'):
    for y in range(y0, y1+1):
        for x in range(x0, x1+1):
            g[y][x] = char


def put(g, x, y, char):
    assert g[y][x] == '.', (x,y,char)
    g[y][x] = char


def main():
    # 18 x 16 m lecture room; the route returns to its one existing entrance.
    g=grid(11,12);rect(g,1,3,9,10);g[2][1]='D';g[1][1]='P';g[1][2]='X'
    for x,y in [(2,8),(7,5),(7,8),(4,9)]:put(g,x,y,'C')
    for x,y in [(3,4),(8,4),(5,7)]:put(g,x,y,'L')
    put(g,8,3,'I')
    save('SalaDeAula','apartments',g,3.8,2.72,{
        'prefab':'Assets/prefabs/Scenario/ClassRoom.prefab','dimensionsMeters':[18,16,3.8],
        'playerHeightMeters':2.1209743,'deskCount':42,'enemyPlacement':'unchanged; scene spawner is outside this room prefab',
        'note':'evacuation/last lecture','route':'entrance -> flanking aisle -> lecturer wall -> same entrance',
        'safeCue':'warm lecturer desk lamp','darkCue':'rear damaged fluorescent sector'})

    # Covered oval atrium, 57 x 41 m; a solid kiosk preserves the two circulation circuits.
    g=grid(35,27)
    for y in range(3,24):
        for x in range(3,32):
            if ((x-17)/14)**2+((y-13)/10)**2<=1:g[y][x]='.'
    rect(g,15,11,19,15,'#')
    for x,y in [(3,13),(31,13),(17,23)]:g[y][x]='D'
    for x,y in [(2,13),(1,13),(32,13),(33,13),(17,24),(17,25),(17,2),(17,1)]:g[y][x]='.'
    put(g,17,1,'P');put(g,17,25,'X')
    for x,y in [(10,7),(24,8),(8,18),(24,18),(17,10)]:put(g,x,y,'L')
    for x,y in [(25,6),(7,17)]:put(g,x,y,'I')
    save('Cilindro','apartments',g,8.6,3.25,{
        'prefab':'Assets/prefabs/Scenario/Cylinder.prefab','dimensionsMeters':[57,41,8.6],
        'functionalDoors':3,'sealedDoors':43,'doorLeafMeters':[1.85,3.25],
        'existingFloorCircuits':2,'notes':['radio without response','missing notices'],
        'route':'south entry -> outer circuit / kiosk circuit -> three existing accesses',
        'safeCue':'radio and canteen','darkCue':'damaged gallery sector',
        'warnings':'The stock validator counts cycles between door-separated regions, not the two continuous floor circuits. These are validated with the real player in Unity.'})

    # Schematic of the existing arena -> combat gate -> bent corridor -> boss room.
    g=grid(27,45);rect(g,3,2,23,19);rect(g,2,25,21,41)
    g[20][5]='D';rect(g,5,21,5,23);rect(g,5,23,11,23);g[24][11]='D'
    g[42][7]='D';g[43][7]='P';put(g,13,3,'X')
    for x,y in [(7,7),(19,7),(7,15),(19,15),(4,28),(19,29),(4,38),(19,39)]:put(g,x,y,'L')
    for x in [6,18]:
        for y in [28,30,32,34,36,38]:put(g,x,y,'E')
    put(g,13,12,'E')
    for x,y in [(4,17),(20,17),(20,26)]:put(g,x,y,'I')
    for x,y in [(3,28),(20,38),(3,36),(20,30)]:put(g,x,y,'O')
    save('Porao','facility',g,12,4.2,{
        'prefab':'Assets/prefabs/Scenario/Basement.prefab','arenaMeters':[40,34,9.5],
        'bossRoomMeters':[42,36,12],'corridorWidthMeters':3.6,
        'combatGate':'existing wave-controlled gate; not a newly locked key door',
        'arenaSpawnMarkers':12,'bossMarker':'existing; no new enemy/loot scripts',
        'notes':['restraint station record','failed evacuation','last warning'],
        'route':'entry -> arena -> existing combat gate -> L corridor -> boss chamber',
        'safeCue':'restraint station perimeter light','darkCue':'approach corridor / boss center'})


if __name__=='__main__':main()
