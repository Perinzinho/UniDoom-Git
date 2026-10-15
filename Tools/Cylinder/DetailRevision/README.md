# Cylinder — detalhes, iluminação e pontos de spawn

Revisão de 15/10/2026 para Unity 6000.5.7f1 / URP 17.5. O átrio, a cantina,
as galerias, as três portas funcionais e as 43 portas decorativas conservam
o traçado anterior. A direção usa a skill enviada pelo usuário e as referências
do [Total Chaos clássico](https://www.moddb.com/mods/total-chaos/images/hotel-lobby).

- 24 cadeiras recebem geometria importada com encostos vazados, braços e desgaste.
  Oito mesas recebem tampo e base arredondados. Os objetos de referência e os
  colisores anteriores permanecem no prefab; as malhas visuais anteriores das
  mesas ficam desabilitadas.
- Três armários elétricos e três barris importados junto às paredes; oito conjuntos
  de ventilação, conduítes, abraçadeiras, parafusos, caixas de passagem e tubulações.
- Oito luminárias fluorescentes novas, duas apagadas. As fontes frias das galerias
  e pendentes contrastam com a cantina âmbar e os indicadores vermelhos das saídas.
  Duas fontes novas projetam sombras; as demais conservam custo de sombras baixo.
- Concreto, metal, madeira e piso usam o shader PBR existente e texturas reais já
  disponíveis no ClassRoom. Normais e máscaras de metallic/AO/smoothness são
  associadas explicitamente. Não foi necessário alterar o shader compartilhado.
- Parâmetros de lanterna, tonemapping e pós-processamento seguem o ClassRoom.
  A luz ambiente mantém sua proporção de cores, com intensidade multiplicada
  por 1,7 para a escala maior do átrio. Névoa 0,009; transição regional de 2 m.
- Seis transforms em `Detailed maintenance and abandonment/Enemy Spawn Points`.
  O trigger da entrada sul usa `CylinderEnemySpawns`, referencia o `Enemy.prefab`
  existente e cria os seis inimigos com intervalo de 1,4 s, uma vez por instância
  do átrio. O encontro começa quando o player entra fisicamente no trigger.

Os resultados persistentes ficam em `Assets/_Project/Scenes/Cylinder/`, em
subpastas `DetailRevision` de materiais, meshes, texturas e profiles. O único
script novo necessário em runtime fica em `Cylinder/Scripts/`.

## Fontes e licença

Powered by [Poly Haven](https://polyhaven.com). Modelos e mapas são gratuitos,
sem marca d'água e licenciados como [CC0-1.0](https://polyhaven.com/license).
Downloads PNG em 1K, normais GL, mipmaps e filtragem trilinear. A máscara Unity
reorganiza o ARM original: R metallic, G AO, B roughness, A 1 − roughness.

| Asset | Autor | Uso |
|---|---|---|
| [Plastic Monobloc Chair 01](https://polyhaven.com/a/plastic_monobloc_chair_01) | Kuutti Siitonen | Cadeiras da cantina |
| [Utility Box 02](https://polyhaven.com/a/utility_box_02) | James Ray Cock | Quadros de serviço |
| [Barrel 03](https://polyhaven.com/a/barrel_03) | Serhii Khromov | Manutenção abandonada |

[asset-sources.json](asset-sources.json) registra autores, URLs oficiais, licença,
tamanhos e SHA-256. O downloader verifica MD5 contra o manifesto oficial.
Concrete Wall 006, Rusty Metal 02, Wood Table Worn, Worn Tile Floor e Rock Face
são reutilizados das referências existentes em `ClassRoom/Surface/texture-sources.json`.
A máscara de concreto já compartilhada está em `Global/Textures/ClassRoom/Surface/`.
Nenhum arquivo original de material ou importador compartilhado é convertido.

## Validação e limites

Resultados verificáveis: [estrutura](structure-validation.json),
[Editor](edit-validation.txt), [Play Mode](play-validation.txt) e
[arquivos instalados](installed.json). As contagens de assets são estáticas;
não representam FPS, tempo de GPU, draw calls ou resultado em dispositivo móvel.

Capturas com as mesmas câmeras: [antes](Before-Atrium.png),
[átrio final](Final-Atrium.png), [cantina](Final-Canteen.png),
[galeria](Final-Gallery.png), [manutenção](Final-Service.png) e
[visão geral](Final-Overview.png). Não há luz de inspeção nem lanterna artificial
nessas capturas. Os arquivos `Final-*-Flashlight.png` acrescentam a lanterna real
criada por `TCRegionAtmosphere`, com os parâmetros usados em gameplay; por exemplo,
[cadeiras e cantina com lanterna](Final-Canteen-Flashlight.png) e
[armário/barril com lanterna](Final-Service-Flashlight.png).
A comparação visual não substitui o teste jogando no mapa completo.

Não foi executado um build Windows, bake de lightmaps ou profiling no hardware-alvo.
Balanceamento, combate completo e navegação da IA existente precisam de playtest.
O script Enemy existente movimenta diretamente em direção ao player; esta revisão
não introduz um sistema de navegação. As novas posições são conferidas por cápsulas
e raycasts para evitar que os inimigos nasçam dentro da arquitetura.

No início do Play Mode, o Editor da cópia isolada registrou uma
`ArgumentOutOfRangeException` interna em `UnityEditor.Search.SearchDatabase`.
O teste continuou, executou os eventos físicos e concluiu com
`CYLINDER_DETAIL_PLAY_PASS`. O registro não é tratado como erro do prefab nem
como evidência de um problema preexistente no projeto principal.

## Reprodução e reversão

Os geradores são ferramentas pontuais fora de Assets. Não executar os geradores
históricos de Cylinder/SkillAtmosphere sem adaptar os caminhos da reorganização.

1. `py Tools/Cylinder/DetailRevision/prepare.py` cria uma cópia isolada e preserva
   os hashes dos assets recebidos; recusa sobrescrever uma preparação existente.
2. `download_assets.py` baixa apenas os arquivos descritos no manifesto.
3. Copiar builder/audit/verifier para `Assets/_TemporaryTools/Editor` da cópia e
   o runtime para `Assets/_Project/Scenes/Cylinder/Scripts`.
4. Executar `CylinderDetailVerifier.BuildWithBaseline` pela Unity em batchmode.
5. Executar `py Tools/Cylinder/DetailRevision/install.py` para preparar o YAML,
   preservando os blocos originais de colisores, MeshFilters, scripts de portas,
   GUIDs e fileIDs. Apenas campos visualmente intencionais são integrados.
6. Executar `CylinderDetailVerifier.VerifyFinal` e `RunPlayMode` na cópia isolada.
   Play Mode requer execução sem `-quit`, pois o runner encerra o Editor ao terminar.
7. `install.py --install` exige os dois resultados PASS e recusa sobrescrever
   arquivos alterados concorrentemente. Não executa commits nem remove arquivos.

Para restaurar o prefab anterior: `py Tools/Cylinder/DetailRevision/install.py --restore-prefab`.
O comando compara seu hash com o resultado instalado e recusa sobrescrever edições
posteriores. As adições ficam disponíveis para regeneração; nenhum asset é apagado.
O backup e o inventário inicial estão em `.utmp/CylinderDetailBefore/`.
