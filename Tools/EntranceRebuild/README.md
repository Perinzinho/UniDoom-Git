# Reconstrução de Entrance Garden, Entrance Hallway e DirectorRoom

16/10/2026. Unity 6000.5.7f1, URP 17.5.0. Direção baseada na skill enviada e no Total Chaos clássico do GZDoom. A orientação posterior do usuário prevalece no escritório: uma sala arrumada e preservada em contraste com as áreas abandonadas.

Revisão posterior: [mesa e cadeiras da diretoria ampliadas em 30% em relação ao player](FurnitureScale/README.md), com captura atualizada e backup próprio.

- **Jardim:** pavimento fotográfico, alvenaria com musgo, samambaias, árvores detalhadas, troncos caídos nos canteiros, bancos e luminárias industriais. Corredor central livre. Uma variante de samambaia por posição; duas árvores mantêm a profundidade sem repetir a geometria densa quatro vezes.
- **Corredores:** reboco deteriorado, piso gasto, pintura institucional, rodapés e proteção de parede segmentados para preservar os acessos laterais; fluorescentes, luminárias de parede, cadeiras, arquivo, telefone e extintor.
- **Diretoria:** parquet e lambris, mesa e cadeiras alinhadas, documentos organizados, estantes, plantas, busto e luminárias estáveis. Luz quente e ausência de flicker reforçam o contraste com os outros ambientes.

As fotos `InMap-*.png` foram renderizadas na SampleScene real, com as escalas e os overrides existentes. As capturas sem prefixo mostram o prefab isolado; o jardim e o corredor têm dimensões diferentes no mapa. O bloco verde visível ao fundo do corredor pertence ao conteúdo preexistente fora do prefab.

## Assets e materiais

Downloads novos do [Poly Haven](https://polyhaven.com), sob [CC0](https://polyhaven.com/license):

- Modelos: `painted_wooden_bench`, `fern_02`, `dead_tree_trunk_02`, `tree_small_02`, `metal_office_desk`, `mid_century_lounge_chair`, `desk_lamp_arm_01`, `potted_plant_02`, `vintage_telephone_wall_clock`, `industrial_wall_lamp`, `korean_fire_extinguisher_01`.
- Superfícies: `cobblestone_floor_08`, `mossy_brick`, `herringbone_parquet`, `wood_table`.

`sources.json` registra URLs oficiais, autores, licenças, tamanhos e SHA-256 dos downloads. `Manifests/` contém os manifestos oficiais consultados. As texturas são PNG 2K; entradas de 16 bits foram convertidas para 8 bits, mantendo a resolução, antes da compressão de runtime da Unity. Os arquivos usados na instalação e seus hashes finais estão em `installed.json`; as conversões estão em `source-png-transforms.txt`.

Reutilizados sem duplicar os downloads existentes: estantes, enciclopédias, busto, arquivo e cadeiras da Library; folhas de `tree_small_02` já presentes na Library; reboco, teto, azulejos, concreto e terra das coleções existentes. Os materiais novos usam o shader já existente `UniDoom/TotalChaosInspiredSurface`, normal maps OpenGL, máscaras metallic/smoothness/AO empacotadas e recorte de alfa para folhagem. SSAO utiliza o renderer PC já configurado. O conversor e os shaders existentes foram preservados.

Assets exclusivos ficam nos respectivos cenários, em subpastas `EntranceRebuild`. A luminária compartilhada e o script de encaixe ficam em Global. `EntranceDressingLayout` apenas ajusta transforms quando o piso ou a escala da instância muda; não gera meshes ou materiais em runtime. Geometria e materiais estão persistidos.

## Preservação e validação

Os três prefabs mantêm GUIDs, todos os fileIDs originais, transforms, scripts e colliders do shell e elementos de gameplay. A decoração antiga continua preservada com seus renderers, luzes e colliders decorativos desativados. Os meshes novos de revestimento são independentes dos meshes transitórios do ProBuilder. Os dois IDs antigos de transforms stripped que a Unity remapeou ao salvar foram restaurados antes da validação final.

Evidências:

- `baseline.txt`: inspeção anterior dos prefabs e das instâncias no mapa.
- `validation.txt`: abertura, materiais, scripts e contagem estática de geometria.
- `*-dependencies.txt`: dependências dos três prefabs obtidas pela Unity.
- `map-validation.txt`: encaixe e amostragem de passagem central com cápsula na cena principal.
- `play-validation.txt`: encaixe em runtime, shaders, atmosfera de cada região e estabilidade das luzes da diretoria.
- `installed-validation.json`: GUIDs/metas, fileIDs, transforms, preservação de arquivos existentes e hash da SampleScene após a instalação.
- `unity-generation.log` e `unity-validation.log`: execuções reais na cópia isolada.

O Play Mode usa a cena principal com os componentes EnemySpawner desativados apenas na cópia de teste: o conteúdo original possui um spawner sem pontos de spawn. Nenhuma configuração desse sistema foi alterada no projeto. Avisos preexistentes do Cinemachine e de APIs obsoletas continuam separados da mudança.

Não foram executados Development Build, bake de lightmaps, teste em tela cheia/monitores diferentes, medição de FPS/frame time ou profiling em hardware-alvo. A contagem de triângulos inclui meshes de todos os LODs importados e não representa uma medição por frame. As árvores do jardim continuam densas; não há alegação de desempenho aprovado nem ganho de FPS. Escala, composição, materiais, legibilidade e iluminação foram revisados por capturas reais; aprovação de desempenho permanece pendente de medição no alvo.

## Reversão e regeneração

Os backups originais dos três prefabs e seus metas estão em `Before/`, fora de Assets. Para conferir a reversão: `py Tools/EntranceRebuild/rollback.py --check`. Com a Unity fechada, `--apply` restaura os três prefabs somente se não tiverem alterações posteriores; os assets novos são mantidos, sem exclusões.

Os geradores ficam neste diretório, fora de Assets. Foram compilados exclusivamente em `.utmp/EntranceRebuild/Assets/_TemporaryTools/Editor/`. Não execute ferramentas históricas que usam `Assets/Art` ou `Assets/prefabs`. Para refazer esta geração, prepare uma cópia isolada, baixe as fontes verificadas, copie as ferramentas temporárias e o script runtime para seus locais correspondentes na cópia e execute `EntranceRebuilder.Build`; depois execute `preserve_ids.py`, `EntranceFinalValidation.Run` e `install.py`. A instalação exige os marcadores de sucesso, preserva alterações concorrentes e bloqueia sobrescrita de prefabs modificados.
