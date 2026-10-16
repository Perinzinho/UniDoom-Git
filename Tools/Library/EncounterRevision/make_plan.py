"""Review diagram from the builder layout and Unity's measured route, not a game asset."""
import json
from pathlib import Path

OUT = Path(__file__).resolve().parent
route = json.loads((OUT / 'route.json').read_text())
svg = ['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 920 1010" role="img" aria-label="Planta dos obstáculos e encontros da biblioteca">',
       '<rect width="920" height="1010" fill="#111a21"/>',
       '<g font-family="Segoe UI,Arial,sans-serif" fill="#e8edf0">',
       '<text x="45" y="43" font-size="25">Library · busca da Key02</text>',
       '<text x="45" y="72" font-size="14">Planta esquemática · rota calculada na Unity · 58,7 m até o fundo</text>']


def xy(x, z):
    return 305 + x * 20, 505 - z * 20


def rect(x, z, w, d, color, opacity=1):
    px, py = xy(x - w / 2, z + d / 2)
    svg.append(f'<rect x="{px:.1f}" y="{py:.1f}" width="{w*20:.1f}" height="{d*20:.1f}" fill="{color}" opacity="{opacity}" stroke="#111a21"/>')


rect(0, 0, 24.3, 39.4, '#24323b')
bands = [
    (10, -12.1, ['case','case','table','case','case','table','case','case','cabinet','cabinet'], [1.7,1.7,3.2,1.7,1.7,3.2,1.7,1.7,1.35,1.35]),
    (0, -6.4, ['table','case','case','cabinet','case','case','table','case','case'], [3.2,1.7,1.7,1.9,1.7,1.7,3.2,1.7,1.7]),
    (-10, -12.1, ['cabinet','cabinet','case','table','case','case','table','case','case','case'], [1.35,1.35,1.7,3.2,1.7,1.7,3.2,1.7,1.7,1.7])]
colors = {'case':'#9c704e','table':'#798b9a','cabinet':'#69856c'}
for z, cursor, kinds, widths in bands:
    for kind, width in zip(kinds, widths):
        rect(cursor + width/2, z, width, .8, colors[kind])
        cursor += width - .025
for x,z in [(-2.7,15),(-2.7,5.4),(-2.7,-4.8),(2.8,15),(2.8,5.6),(2.8,-5.1),(2,-14.5),(9.2,15.8),(9.1,4.4),(9.2,-5),(-2.2,-16.8)]:
    rect(x,z,3.17,1.45,'#50616c',.8)
points = ' '.join(f'{xy(p["x"],p["z"])[0]:.1f},{xy(p["x"],p["z"])[1]:.1f}' for p in route['points'])
svg.append(f'<polyline points="{points}" fill="none" stroke="#68dae9" stroke-width="3" stroke-dasharray="7 5"/>')
for label,x,z in [('A1',-8.5,3),('A2',-7.7,-2.5),('B1',8.2,-13),('B2',5.4,-6.8),('R1',7,-6.9),('R2',-7.7,4.8),('R3',7.9,13)]:
    px,py=xy(x,z)
    svg.append(f'<circle cx="{px}" cy="{py}" r="9" fill="#c65d5b" stroke="#fad3b1"/><text x="{px+12}" y="{py+4}" font-size="12">{label}</text>')
for label,x,z,color in [('Entrada',10.1,19.7,'#68dae9'),('Key02',-.85,-16.97,'#f1c261')]:
    px,py=xy(x,z)
    svg.append(f'<circle cx="{px}" cy="{py}" r="7" fill="{color}"/><text x="{px-20}" y="{py-15}" font-size="16" fill="{color}">{label}</text>')
for n,text in enumerate(['ESTANTES E COBERTURAS','Marrom · estantes reposicionadas','Cinza · mesas tombadas','Verde · arquivos','Azul tracejado · percurso testado','','ENCONTROS','A1–A2 · primeiro avanço','B1–B2 · aproximação do arquivo','R1–R3 · retorno após a chave','','Vãos alternados: direita,','esquerda e direita.','Mesas baixas cortam a visão;','estantes fecham atalhos.']):
    svg.append(f'<text x="590" y="{150+n*25}" font-size="{16 if n in (0,6) else 14}">{text}</text>')
svg.extend(['<text x="65" y="950" font-size="13">Esquema: mobiliário secundário, cadeiras e pilares omitidos. Colisões verificadas na Unity.</text>','</g></svg>'])
(OUT / 'encounter-plan.svg').write_text('\n'.join(svg), encoding='utf-8')
