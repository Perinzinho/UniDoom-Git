# Sala de ritual do Basement

Alteração restrita ao `BossRoom` de `Assets/prefabs/Scenario/Basement.prefab`,
em Unity 6000.5.7f1 / URP 17.5. Direção visual pela skill
`total-chaos-visual-direction`, usando a referência do
[mod clássico no ModDB](https://www.moddb.com/mods/total-chaos/images/hotel-lobby).

O altar permanece como foco da sala. Piso de calcário gasto, alvenaria antiga,
arcos laterais, tecidos funerários e oferendas de latão reforçam o ritual.
Grupos de velas delimitam a procissão e o círculo sem acrescentar colisores.
As fontes quentes contrastam com a iluminação fria dos corpos suspensos.

Os seis corpos pendurados mantêm suas malhas, posições e materiais. Os assets
originais são preservados; o símbolo usa um material e uma textura novos.
Decoração junto ao altar acompanha a mesma âncora, inclusive seu deslocamento
na cena principal. A geometria do piso e das paredes e os colisores permanecem.

## Assets externos

Todos são gratuitos, sem marca d'água, sob [CC0-1.0](https://polyhaven.com/license):

| Asset | Uso | Resolução |
| --- | --- | --- |
| [Brass Candleholders](https://polyhaven.com/a/brass_candleholders) | Castiçais, velas e chamas | FBX + mapas 1K |
| [Brass Goblets](https://polyhaven.com/a/brass_goblets) | Cálices de oferenda | FBX + mapas 1K |
| [Monastery Stone Floor](https://polyhaven.com/a/monastery_stone_floor) | Piso da sala | PNG 2K |
| [Old Stone Wall](https://polyhaven.com/a/old_stone_wall) | Paredes e arcos | PNG 2K |

`Assets/Art/Basement/Ritual/asset-sources.json` registra URLs oficiais,
licenças, tamanhos, MD5 e SHA-256 de cada download. O download usa a API
pública. Imagens de apresentação da biblioteca não são utilizadas como texturas.
Materiais usam URP Lit, normal GL e máscara convertida: R=metallic, G=AO,
B=roughness, A=smoothness. Albedos e normais com mipmaps, filtro trilinear e
anisotropia 4; máscaras importadas em espaço linear, com mipmaps.

## Símbolo

`Assets/Art/Basement/Ritual/Textures/AltarFineLineSeal.png` foi editado com a
ferramenta integrada `image_gen`, a partir do símbolo existente fornecido pelo
usuário. Fundo transparente, traços centrais afinados e espaços entre os
elementos geométricos. O PNG anterior permanece intacto. Material com recorte
de alpha, emissão moderada e compensação da orientação dos UVs existentes.

Prompt utilizado:

> Use case: precise-object-edit. Edit target: the supplied ornate red occult seal. Asset type: transparent RGBA Unity game altar decal. Preserve the exact overall design, all three triangular outlines, circle, central double-bar cross, bottom small ring, symmetrical spiral flourishes and framing. PRIMARY CHANGE: dramatically thin the over-heavy strokes to roughly ONE THIRD their current width, especially triangle sides, central vertical and double horizontal cross bars and the large circle. Keep the centerlines and proportions of the original shapes. Create clear transparent gaps so all intersecting geometric shapes read independently; reduce tiny busy hatching in crowded regions. Fine clean dark oxblood-red linework, slightly hand-inked but no glow, no shadows, no background, no gradients. The cross bars must be slender outlines/strokes, never thick solid filled blocks. Outer filigree delicately drawn with ample negative space. Square canvas, whole seal centered and uncropped with 5% margin. True transparent background. Preserve its identity; do not invent a different emblem.

## Reprodução e reversão

`prepare.py` preserva um snapshot em `.utmp/BasementRitual/before` e sincroniza
a cópia isolada existente em `.utmp/BasementPreview`. `download.py` baixa os
assets e confere hashes. Copie `BasementRitualBuilder.cs` para `Assets/Editor`
da cópia isolada, junto com os builders anteriores já presentes nela.

Execute `BasementRitualBuilder.Build` e `BasementRitualBuilder.FineTune`, depois `python Tools/Basement/Ritual/install.py`
para preservar os IDs e aplicar apenas os campos visuais autorizados no YAML.
Execute `BasementRitualBuilder.Verify` nessa versão mesclada e
`BasementSurfacePlayVerifier.Run` (sem `-quit`, pois ele encerra o Editor ao
concluir a verificação em Play Mode); então
`python Tools/Basement/Ritual/install.py --install`. O instalador exige logs
completos sem erros, conteúdo validado idêntico e ausência de edições concorrentes.

Restaure apenas o `Basement.prefab` do snapshot para reverter a sala. Os assets
anteriores, a cena e os scripts de gameplay não são gravados pelo instalador.
As capturas `BeforeRitual-*` e `VerifiedRitual-*` usam as mesmas câmeras e a
atmosfera regional do jogo. Validação técnica em `validation.txt`.
O teste em Play Mode verifica câmera, neblina, volumes e lanterna, além da
restauração do ambiente fora do Basement; não simula o combate.

Desempenho final e ritmo do combate precisam ser avaliados em playtest no
hardware alvo; capturas de Editor não medem FPS de uma build.

## Revisão visual

Revisão provisória pela checklist da skill: 21/24. Escala, materiais,
fontes de luz, leitura das sombras, profundidade, contato dos props,
legibilidade, identidade e pós-processamento recebem 2/2. Desgaste causal,
história de uso anterior e desempenho recebem 1/2: a estrutura industrial
anterior continua visível e o custo final ainda precisa de medição em build.
Os conjuntos que ultrapassavam o apoio dos pedestais foram removidos; a luz
fria sobre os corpos recebeu luminárias visíveis. O símbolo novo reduziu a
cobertura de tinta de 27,3% para 17,7%, na mesma resolução de 1254 × 1254.

O instalador preserva os 82 blocos originais de colisão/física. Não há
colisores novos na decoração. Alterações de prefabs aninhados são limitadas
a materiais, visibilidade e parâmetros das luzes. Atmosferas dos corredores
são preservadas. Os registros detalhados da montagem ficam em
`.utmp/BasementRitual`.
