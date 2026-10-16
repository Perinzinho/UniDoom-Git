# Entrada UniDoom — revisão por referência, 16/10/2026

Refatoração de `EntranceGarden.prefab` e `EntranceHallway.prefab`, Unity 6000.5.7f1 / URP 17.5.0. O usuário escolheu ampliar a área externa e remover os muros altos. A imagem anexada orienta arquitetura e circulação; o prompt mestre e a skill `total-chaos-visual-direction` orientam materiais, abandono e iluminação.

## Resultado

- Praça externa com cerca de 36 × 42 m, portão aberto, percurso central, calçada em mosaico, piso tátil azul, canteiros afunilados, caminhos diagonais, palmeiras e luminárias esféricas.
- Fachada larga com reboco bege, faixas marrons, portal vermelho, cobogós brancos, marquise e placa **UniDoom**, iluminada por luminárias físicas. Não foi usado o nome da instituição real nos elementos criados.
- Portas de vidro mantidas abertas, com vão central de 2,6 m; saguão com atendimento nas laterais e corredor de 7,66 × 28,7 m com piso geométrico, caixilhos, colunas, vasos e fluorescentes. O ponto final permanece alinhado ao acesso existente.
- `SampleScene.unity`: jardim alinhado à entrada do corredor; `SpawnPoint` existente reposicionado para o exterior; antiga instância de porta substituída na entrada desativada; porta ao fundo preserva seus componentes e recebe apenas acabamento de material coerente.
- A decoração anterior e os muros substituídos ficam desativados, mantendo seus objetos e referências. Outros ambientes não foram reconstruídos.

Os grupos `CampusArchitecture` separam exterior, portão, fachada, recepção e corredor. Módulos de palmeira, luminária, cobogó, caixilho, vaso, banco e samambaia são prefabs persistentes. `CampusEntranceAnchor` compensa a escala não uniforme das instâncias e ancora a geometria ao piso original; não cria assets nem geometria em runtime.

Materiais e módulos compartilhados ficam em `Global`; recursos exclusivos ficam no respectivo cenário, em `CampusReference`. O shader existente `UniDoom/TotalChaosInspiredSurface` foi reutilizado. `TCRegionAtmosphere.directionalLightMultiplier` permite à região exterior conservar parte da luz natural da cena; o valor padrão zero mantém o comportamento das regiões antigas.

## Fontes e licenças

- [Palm tree v2, Yughues/Nobiax, OpenGameArt](https://opengameart.org/content/palm-tree-v2): **CC0**. Modelo OBJ e atlas fotográfico original; TGA convertido para PNG sem redimensionamento. Licença original preservada em `Downloads/Palm/readme.txt`; conversões em `palm-conversion.json`.
- [Patterned Cobblestone 02, Poly Haven](https://polyhaven.com/a/patterned_cobblestone_02): **CC0**, mapas PNG 2K de albedo, normal OpenGL e ARM. URLs e hashes em `sources.json`; manifesto oficial em `patterned_cobblestone_02.json`. O desenho de rosetas da calçada é geometria própria com tesselas de 10 cm e esses mapas como detalhe de pedra; não é uma textura extraída da fotografia da faculdade.
- Reutilizados do projeto: reboco `UniversityPaintedPlaster`, concreto, piso institucional, terra, banco, samambaias e vasos do acervo Poly Haven já instalado. Não foram duplicados os arquivos de textura desses recursos. A documentação de procedência anterior permanece em `Tools/EntranceRebuild` e `Tools/Library`.
- Direção de atmosfera: [Total Chaos clássico — Hotel Lobby](https://www.moddb.com/mods/total-chaos/images/hotel-lobby) e referências locais já existentes em `.utmp/TotalChaosReferences`. Nenhum asset do mod foi copiado.

## Evidências

`audit.txt` registra o estado inicial obtido na Unity. `validation.txt` registra encaixe, materiais e passagem física; `play-validation.txt` registra o percurso com o controlador original; `final-validation.txt` verifica abertura dos prefabs e persistência dos assets. As capturas `Exterior-*.png`, `Interior-*.png` e `Campus-Overview.png` são renderizações reais da cena, sem edição de imagem.

`installed-validation.json` registra preservação dos GUIDs, fileIDs e arquivos existentes fora do escopo; `installed.json` lista os arquivos instalados e seus hashes. `Before/` preserva os dois prefabs, seus metas, a cena principal e o script de atmosfera anterior. `before.json` contém hashes de todos os arquivos de Assets, Packages e ProjectSettings antes da revisão.

A cena principal recebeu alterações concorrentes durante a execução. `merge_scene.py` preserva os ajustes de mobiliário, dados do ProBuilder e remoções de instâncias do usuário; aplica apenas os sete blocos necessários à entrada. O bloco compartilhado do corredor mantém integralmente os dados do usuário: a única diferença da entrada nesse bloco seria remover um placeholder nulo de TMP. `scene-merge.json` registra a união; `ConcurrentBeforeInstall/` guarda a cena imediatamente anterior à instalação. A reversão usa essa versão para preservar as alterações concorrentes.

Os testes de Play Mode desativam o `EnemySpawner` preexistente com lista vazia somente na cena de teste não salva. Avisos preexistentes de APIs obsoletas/Cinemachine e a pressão do atlas de sombras da cena completa não constituem medição de desempenho. Não foram executados build novo, bake de lightmaps, profiling de CPU/GPU, medição de FPS, testes em hardware-alvo ou teste manual com teclado/mouse.

Os logs completos desta execução ficam em `Logs/`. A geração também registra a mensagem de Editor sobre o método estático `Reset` já existente em `TCRegionAtmosphere` ao adicionar novos componentes; o reset de runtime foi exercitado pelo teste de Play Mode. Não há alegação de Console inteiro sem mensagens preexistentes.

O percurso automatizado usa deslocamentos curtos do `CharacterController` para testar colisão e apoio; sua duração não representa velocidade de caminhada nem benchmark. Capturas iniciais também foram feitas em Direct3D 12; a revisão final e o teste de Play Mode usam Direct3D 11 por argumento de execução, sem mudar Player Settings. A serialização final preserva todos os fileIDs originais e remove somente entradas novas de componentes temporários nulos do TextMesh Pro (`serialization-repair.json`). Meshes intermediárias já incorporadas aos meshes finais ficam preservadas na cópia isolada, listadas em `generation-intermediates.json`, e não são instaladas em Assets.

Revisão pela checklist da skill: escala, composição, identificação, fontes de luz, persistência e navegação verificadas; materiais fotográficos e desgaste superficial presentes. O paisagismo simplifica os arbustos da referência usando samambaias do acervo, e o desenho do mosaico é uma aproximação geométrica. Desgaste narrativo mais localizado e continuidade visual das áreas preexistentes além do acesso são pontos para refinamento. Desempenho permanece sem pontuação e sem aprovação por falta de profiling no alvo.

## Regeneração e reversão

As fontes C# neste diretório são ferramentas pontuais, fora de Assets. Foram compiladas somente em `.utmp/EntranceRebuild/Assets/_TemporaryTools/Editor`. `prepare.py` sincroniza uma cópia isolada e recusa substituir o baseline existente. `download.py` verifica os downloads; `install_palm.py` converte os atlas.

O fluxo de geração é `CampusBuilder.Build`, `install.py --preserve-ids`, `CampusFinalCheck.Run`, `CampusPlayCheck.Run` e `install.py`; `format_assets.py` remove apenas espaços finais dos prefabs/cena instalados e atualiza seus hashes. Se a cena original mudar durante o trabalho, execute `merge_scene.py` e valide novamente com `CampusFinalCheck.Run` antes de instalar. As execuções Unity usam `-licensingIpc LicenseClient-leo`; o teste de Play Mode sai pelo próprio verificador e não deve usar `-quit`. A integração da cena usa o backup desta revisão para impedir overrides obsoletos entre gerações isoladas. Para uma futura revisão, crie um novo baseline que inclua alterações posteriores do usuário.

Para conferir a reversão sem modificar arquivos: `py Tools/CampusEntrance/rollback.py --check`. Com a Unity fechada, `--apply` restaura os arquivos existentes somente se seus hashes ainda coincidirem com esta instalação. Os assets novos ficam preservados; nada é excluído definitivamente.
