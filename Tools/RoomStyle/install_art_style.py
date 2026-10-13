"""Install the visual comparison and Cylinder's aligned materials, retaining geometry."""
import re
import shutil
from pathlib import Path
from install_room_style import blocks, merge_prefab

ROOT = Path(__file__).resolve().parents[2]
PREVIEW = ROOT / '.utmp/BasementPreview'
BEFORE = ROOT / '.utmp/ArtStyle-before'
PREFAB = Path('Assets/prefabs/Scenario/Cylinder.prefab')
ART = Path('Assets/Art/Cylinder')


def main():
    log = (ROOT / '.utmp/ArtStyle-build.log').read_text(encoding='utf-8-sig')
    assert all(s in log for s in ['ART_STYLE_ALIGNMENT_OK', 'ART_STYLE_COMPARISON_OK',
                                'CYLINDER_DOORS_OK', 'CYLINDER_PLAYER_ROUTES_OK',
                                'CYLINDER_CANTEEN_SUPPORT_OK', 'CYLINDER_ROOF_COVERAGE_OK']), 'Unity audit must pass'
    comparison_log = ROOT / '.utmp/ArtStyle-comparison.log'
    if comparison_log.exists():
        assert 'ART_STYLE_COMPARISON_OK' in comparison_log.read_text(encoding='utf-8-sig'), 'Map comparison must pass'
    before = (BEFORE / 'Cylinder.prefab').read_text(encoding='utf-8-sig')
    assert (ROOT / PREFAB).read_text(encoding='utf-8-sig') == before, 'Concurrent Cylinder edit'
    merged, _ = merge_prefab(before, (PREVIEW / PREFAB).read_text(encoding='utf-8-sig'), classroom=True)
    for folder in ['Materials', 'Textures']:
        for p in (ROOT / ART / folder).iterdir():
            assert p.read_bytes() == (BEFORE / folder / p.name).read_bytes(), f'Concurrent edit: {p}'
    # The only prefab differences must be local light colors/intensities.
    old, new = blocks(before), blocks(merged)
    for fid, (kind, block) in old.items():
        if kind != 108:
            assert new[fid][1] == block
    for p in (PREVIEW / ART / 'Materials').glob('*.mat'):
        shutil.copy2(p, ROOT / ART / 'Materials' / p.name)
    for p in (PREVIEW / ART / 'Textures').glob('*.meta'):
        shutil.copy2(p, ROOT / ART / 'Textures' / p.name)
    (ROOT / PREFAB).write_text(merged, encoding='utf-8', newline='\n')
    scene = ART / 'CylinderPreview.unity'
    clean = re.sub(r'[ \t]+(?=\r?$)', '', (PREVIEW / scene).read_text(encoding='utf-8-sig'), flags=re.M)
    (ROOT / scene).write_text(clean, encoding='utf-8', newline='\n')
    for p in (PREVIEW / 'Preview').glob('Cylinder-*.png'):
        shutil.copy2(p, ROOT / 'Tools/Cylinder' / p.name)
    destination = ROOT / 'Tools/RoomStyle/Comparison'
    destination.mkdir(exist_ok=True)
    for p in (PREVIEW / 'Preview/ArtStyle').iterdir():
        shutil.copy2(p, destination / p.name)
    cards = [('Director', 'Sala do diretor — referência'), ('Basement', 'Porão'),
             ('ClassRoom', 'Sala de aula'), ('Cylinder', 'Cilindro')]
    figures = ''.join(f'<figure><img src="{name}.png" alt="{title}"><figcaption>{title}</figcaption></figure>' for name, title in cards)
    html = ('<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">'
            '<title>Comparação de estilo artístico</title><style>body{margin:24px;background:#181817;color:#e4ddca;font:16px system-ui}'
            'main{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:20px}figure{margin:0}img{width:100%;display:block}'
            'figcaption{padding:8px 0}a{color:#e3b573}@media(max-width:800px){main{grid-template-columns:1fr}}</style>'
            '<h1>Comparação de estilo artístico</h1><p>Mesma iluminação de inspeção e câmera; luzes locais de cada ambiente preservadas.</p>'
            f'<main>{figures}</main><p><a href="Comparison.md">Materiais, texturas e parâmetros verificados</a></p></html>')
    (destination / 'comparison.html').write_text(html, encoding='utf-8')
    print('Installed Cylinder alignment and four-room visual comparison; every geometry block retained.')


if __name__ == '__main__':
    main()
