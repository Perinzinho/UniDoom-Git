# CampusLibrary — biblioteca universitária da Unimar

## Leitura rápida

- Cena: `Assets/_Project/Scenes/CampusLibrary/CampusLibrary.unity`; interior no prefab `Prefabs/CampusLibrary.prefab` da mesma pasta. A cena referencia o prefab; não há geração do mapa em runtime.
- Este é o interior novo do campus Unimar. `Scenes/Library/Library.unity` é a biblioteca antiga e não pertence a esta revisão.
- Contexto, portais e histórico: [README_CAMPUS_EXTERIOR.md](../../README_CAMPUS_EXTERIOR.md), seções 5, 6 e 7.
- Revisão de 07/11/2026: ampliação ao norte, a partir de `z = 12`, mantendo hall, atendimento, degraus, foto, pesquisa e sanitários. Nova ala em `North wing - university library`, com leitura silenciosa, salas de estudo, cabines e acervo por assunto.
- Envelope aproximado: **56 × 42 m**, antes **56 × 24 m**. Acréscimo de aproximadamente **1.016 m²**, cerca de **76%** sobre os 1.344 m² nominais anteriores; área bruta de planta, sem descontar paredes e móveis. Limite interno norte: `z = 30.15`.
- Paleta e mobiliário de `Global/Materials/CampusInterior/` e `Global/Meshes/CampusInterior/`. Meshes exclusivos novos: `CampusLibrary/Meshes/NorthWing*.asset`.
- Nenhum novo download de modelo 3D. Fontes e hashes da reutilização: `Licenses/AssetSources/CampusLibrary/expansion-sources.json`; os três documentos de créditos registram o uso nesta ala.

## Função, layout e circulação

O edifício continua térreo, com hall elevado a **0,64 m** e quatro degraus de **0,16 m** até o piso do acervo. A ala adicional fica no nível inferior. Uma abertura de **6 × 3,20 m** na antiga parede de fundo conecta os ambientes sem carregamento de cena. A parede mantém seus objetos, GUID e fileID do mesh; geometria e colisões contornam a abertura.

| Setor adicional | Implantação | Conteúdo |
| --- | --- | --- |
| Leitura silenciosa | Oeste, `z ≈ 15–23` | Seis mesas de quatro lugares, documentos e livros nos tampos |
| Salas de estudo 1 e 2 | Noroeste, `z = 24–30.15` | Mesa de quatro lugares e quadro em cada sala; entradas de 1,60 m |
| Estudo individual | Parede norte, faixa central | Doze cabines, cadeiras e notas |
| Acervo geral | Leste | Quinze conjuntos duplos: trinta estantes, com ciências sociais, naturais, tecnologia, artes e literatura |
| Serviço | Junto à passagem central | Carrinho de livros consultados, fora do eixo principal |

Passagens do acervo: **3,26 m entre conjuntos lateralmente** e **1,70 m entre fileiras transversalmente**. Espaço diante das cadeiras das cabines: aproximadamente **1,6 m** junto ao acervo. Há pilares em pontos dessas faixas; a verificação de rotas considera as colisões reais.

A ala acrescenta **8 mesas**, **12 cabines**, **44 cadeiras** e **30 estantes**. Permanecem recepção, espera, seis posições individuais anteriores, nove gabinetes de computador, leitura em grupo, periódicos e dois sanitários com seis cabines.

## Escala em relação ao jogador

Referência: prefab global `Player`, sem mudar sua escala ou o sistema persistente. O `CharacterController` tem aproximadamente **2,121 m** de altura em mundo e raio de **0,237 m**; `Head` fica aproximadamente **1,839 m** acima dos pés. Valores incluem a escala não uniforme já existente no prefab.

| Elemento | Dimensões e decisão |
| --- | --- |
| Cadeiras de leitura | Altura de 0,83 para **0,913 m**, largura aproximada 0,539 m e profundidade 0,594 m. Aumento uniforme de 10%, com apoio no piso e colisão acompanhando a mudança |
| Estantes | **2,15 m** de altura; cada conjunto duplo ocupa aproximadamente 1,24 × 2,65 m. Mantida a dimensão funcional existente |
| Mesas novas | Tampo a **0,80 m**, área 2,80 × 1,35 m, pés até o piso |
| Cabines novas | Tampo a **0,80 m**, largura 1,45 m, profundidade 0,78 m; divisórias de aproximadamente 1,32 m |
| Passagem da ala | **6,00 m** de largura e **3,20 m** de altura livre |
| Salas de estudo | Entrada de **1,60 m**, acima da altura da cápsula |
| Teto | Cota próxima de **4,60 m** |

As correções anteriores de apoio dos tampos, bancadas, gabinetes e corrimãos permanecem. As cadeiras novas usam o mesh compartilhado e a dimensão revisada das anteriores; a ampliação não aplica escala global aos props.

## Materiais e direção visual

Fontes normativas: [prompt mestre](../../.codex/PROMPT_MESTRE.md) e [skill local](../../.codex/SKILL.md). Referência observada: captura oficial [Hotel Lobby, Total Chaos clássico](https://www.moddb.com/mods/total-chaos/images/hotel-lobby), de wadaholic. Orienta contraste, leitura, superfícies usadas e profundidade; nenhum asset do mod foi extraído.

A nova ala mantém granito, reboco institucional, carvalho gasto, aço escuro, mobiliário azul e vigas alaranjadas. Janelas laterais têm montantes e peitoris; as salas de estudo têm base opaca de 0,90 m e parte superior envidraçada, com montantes e travessas. O teto modular tem perfis e luminárias físicas. Algumas estão apagadas; as doze cabines têm luminárias de tarefa com luz quente localizada. Não houve mudança global de névoa, shader ou pós-processamento. Madeira e cadeiras conservam texturas e normais compartilhadas, sem cópias de imagens.

Superfícies novas usam **183 meshes persistentes** por célula espacial e material. Estantes e cadeiras reutilizam sete meshes compartilhados, sem alterar seus bytes. O material exclusivo `Materials/North wing worn oak.mat` varia a cor-base do carvalho para preservar leitura nos tampos; referencia as mesmas texturas e normais. O vidro reutiliza `Global/Materials/CampusNight/Night clear dusty glazing.mat`. Não foram alterados materiais globais.

Luzes adicionais: pontos nas luminárias do teto e spots curtos nas doze luminárias de tarefa, todos sem sombras em tempo real. As primeiras capturas motivaram aumentar a luz de estudo, clarear o material local, envidraçar as divisórias e afastar a mesa da sala 1 do pilar. Medir custo no hardware alvo antes de afirmar desempenho ou ampliar sombras.

## Dependências, conexões e preservação

- Entrada: `CampusExterior → CampusLibrary / FromCampusExterior`; chegada `(0, 1.75, -9.6)`.
- Saída: `CampusLibrary → CampusExterior / FromCampusLibrary`; trigger `(0, 1.9, -11.48)`.
- `GameManager`, HUD, inventário, munição, player persistente e transições continuam compartilhados. A ampliação não cria outro player nem muda o menu.
- Foto `Campus library portrait.png` e arte `Resources/Textures/UI/Transitions/CampusLibrary.png`: preservar as alterações locais preexistentes.
- Prefab e meshes persistentes bastam para jogar. As quatro ferramentas temporárias de Editor e seus `.meta` foram retirados de `Assets` após validar os resultados e confirmar zero consumidores serializados de seus GUIDs e nenhum uso no código do jogo. Permanecem apenas na área descartável `Temp/CampusLibraryRevision/`, ignorada pelo Git; não são necessárias para carregar ou jogar o mapa.

## Validação desta revisão

Executada em **07/11/2026**, Unity **6000.5.7f1**, URP **17.5.0**, Windows. Capturas em **1920 × 1080**, FOV 60°, perfil existente da cena; não constituem benchmark de CPU/GPU.

| Verificação | Resultado e limite |
| --- | --- |
| Compilação da edição pela Unity API | Concluída sem erros de C# nas ferramentas desta revisão |
| Cena e prefab salvos e reabertos | Carregamento na Unity; **0 scripts ausentes**, **0 materiais inválidos**, contagem final **62 estantes / 95 cadeiras** |
| Referências do prefab | **472 GUIDs distintos resolvidos**, incluindo recursos de pacotes; nenhum GUID sem destino |
| Identidade dos objetos | **3.186 / 3.186 fileIDs originais preservados**; novas peças acrescentam IDs |
| Recursos protegidos | SHA-256 idêntico para cena Library antiga, CampusExterior, Player, cena CampusLibrary e as duas imagens locais preexistentes; os **sete meshes compartilhados** também mantiveram seus hashes |
| Navegação após o acabamento | **18 / 18 rotas completas**, da chegada a leitura antiga, pesquisa, circulação do acervo, acessos dos dois sanitários, passagem nova, leitura silenciosa, duas salas, cabines e acervo novo |
| Agente de teste | Altura **2,141 m**, raio **0,27 m**, degrau 0,36 m, voxel 0,09 m; margem sobre a cápsula real. NavMesh temporário, não salvo no jogo |
| Contato das cadeiras | Bounds reais **0,539 × 0,913 × 0,594 m**; diferença entre base visual e piso: **0 m** na amostra inspecionada |
| Play Mode controlado | Player real com cápsula **0,473 × 2,121 × 0,473 m**, olhos a **1,838885 m** dos pés; `CharacterController.Move` atravessou a passagem original → ala nova; **uma instância de PlayerMovement** |
| Captura com o player | Câmera do jogo posicionada na `Head` real em Play Mode para avaliar escala; pose de revisão, não teste de input nem de acompanhamento automático da Cinemachine |
| Comparação visual | Sete vistas na altura dos olhos, mais captura do player; revistas após luz, vidro, contato e afastamento do pilar. Prévia histórica da entrada usada como referência, sem equivalência de benchmark |
| Diff de texto | `git diff --check` passou nos arquivos rastreados alterados nesta tarefa |

**Não executados nesta revisão:** build/executável, benchmark de desempenho, percurso manual completo por input, verificação auditiva, nova ida e volta pelos portais e testes de menu/HUD/inventário. Cena e contratos de transição foram preservados e comparados; os testes históricos de ida/volta não foram contabilizados como novos.

**Console e ambiente:** antes da edição já havia falha de `CampusParkingVehicleSetup`, por ausência de `MeshFilter` em `Street Parking Bays - 12`; essa ferramenta externa à biblioteca não foi corrigida nem usada como validação desta tarefa. O Play Mode aditivo também emitiu aviso de dois AudioListeners; a origem não foi investigada e nenhum componente de áudio foi acrescentado ou alterado nos assets desta revisão. Portanto, o resultado não equivale a afirmar que todo o Console do projeto está sem avisos/erros.

Checklist visual da skill, avaliação interna limitada às capturas: escala 2, materiais 2, desgaste causal 1, fontes de luz 2, sombras/contato 1, profundidade 2, uso/abandono 1, props/colisões 2, legibilidade 2, identidade 2, pós-processamento 2. **19 pontos nos 11 itens avaliados**; desempenho **não verificado**, sem nota atribuída. Não declarar aprovação integral dos 12 critérios nem FPS sem medição.

### Capturas finais

- [Entrada e ligação com a ala nova](../../output/CampusLibrary-Ampliada-Entrada.png)
- [Visão da ala](../../output/CampusLibrary-Ampliada-Ala-Nova.png)
- [Leitura silenciosa e salas envidraçadas](../../output/CampusLibrary-Ampliada-Leitura-Silenciosa.png)
- [Acervo](../../output/CampusLibrary-Ampliada-Acervo.png)
- [Cabines e luminárias de tarefa](../../output/CampusLibrary-Ampliada-Estudo-Individual.png)
- [Sala de estudo 1](../../output/CampusLibrary-Ampliada-Salas-de-Estudo.png)
- [Mobiliário na altura dos olhos](../../output/CampusLibrary-Ampliada-Escala-Mobiliario.png)
- [Câmera na Head do player em Play Mode](../../output/CampusLibrary-Ampliada-Player-Real.png)
