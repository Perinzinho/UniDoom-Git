# CampusLibrary — biblioteca universitária da Unimar

## Leitura rápida

- Cena: `Assets/_Project/Scenes/CampusLibrary/CampusLibrary.unity`; interior no prefab `Prefabs/CampusLibrary.prefab` da mesma pasta. A cena referencia o prefab; não há geração do mapa em runtime.
- Este é o interior novo do campus Unimar. `Scenes/Library/Library.unity` é a biblioteca antiga e não pertence a esta revisão.
- Fila de trabalho: [tarefas da biblioteca](CampusLibrary-Tarefas.md), uma por vez, priorizando a última pendente e marcando após validação.
- Design de gameplay: [mecânicas gerais](CampusLibrary-Mecanicas.md) e [puzzles caóticos com inimigos](CampusLibrary-Puzzles-Caoticos.md); propostas documentadas, sem implementação.
- Contexto, portais e histórico: [README_CAMPUS_EXTERIOR.md](../../README_CAMPUS_EXTERIOR.md), seções 5, 6 e 7.
- Revisão de 07/11/2026: ampliação ao norte, a partir de `z = 12`, mantendo hall, atendimento, degraus, foto, pesquisa e sanitários. Após restaurar a organização do commit `32028565`, a fila de tarefas revisa escala do mobiliário, modelos, luz, sinalização e separação dos sanitários. Nova ala em `North wing - university library`, com leitura silenciosa, duas salas de estudo, cabines e acervo por assunto.
- Envelope aproximado: **56 × 42 m**, antes **56 × 24 m**. Acréscimo de aproximadamente **1.016 m²**, cerca de **76%** sobre os 1.344 m² nominais anteriores; área bruta de planta, sem descontar paredes e móveis. Limite interno norte: `z = 30.15`.
- Paleta e mobiliário de `Global/Materials/CampusInterior/` e `Global/Meshes/CampusInterior/`. Meshes exclusivos novos: `CampusLibrary/Meshes/NorthWing*.asset`.
- Nenhum novo download de modelo 3D. Fontes e hashes da reutilização: `Licenses/AssetSources/CampusLibrary/expansion-sources.json` e `reused-props-sources.json` da mesma pasta; os três documentos de créditos registram o uso nesta ala.

## Função, layout e circulação

O edifício continua térreo, com hall elevado a **0,64 m** e quatro degraus de **0,16 m** até o piso do acervo. A ala adicional fica no nível inferior. Uma abertura de **6 × 3,20 m** na antiga parede de fundo conecta os ambientes sem carregamento de cena. A parede mantém seus objetos, GUID e fileID do mesh; geometria e colisões contornam a abertura.

| Setor adicional | Implantação | Conteúdo |
| --- | --- | --- |
| Leitura silenciosa | Oeste, `z ≈ 15–23` | Seis mesas de quatro lugares, documentos e livros nos tampos |
| Salas de estudo 1 e 2 | Noroeste, `z = 24–30.15` | Mesa de quatro lugares e quadro em cada sala; entradas de 1,60 m |
| Estudo individual | Parede norte, faixa central | Doze cabines, cadeiras e notas |
| Acervo geral | Leste | Quinze conjuntos duplos: trinta estantes, com ciências sociais, naturais, tecnologia, artes e literatura |
| Serviço | Junto à passagem central | Carrinho de livros consultados, fora do eixo principal |

Após o aumento dos móveis, o acervo norte mantém aproximadamente **3,01 m entre conjuntos lateralmente** e **1,17 m entre fileiras transversalmente**; a coluna adicional a leste tem cerca de **1,61 m** até a vizinha. No acervo antigo, os intervalos nominais são **1,91 m lateralmente** e **0,92 m entre fileiras**. Pilares e cadeiras reduzem trechos locais; a verificação de rotas considera as colisões reais, inclusive diante das cabines individuais.

A ala acrescenta **8 mesas**, **12 cabines**, **44 cadeiras** e **30 estantes**. Permanecem recepção, espera, seis posições individuais anteriores, nove gabinetes de computador, leitura em grupo, periódicos e dois sanitários com seis cabines.

### Separação dos sanitários na fila de tarefas

Dois conjuntos duplos, antes em `(17.42, 0, 3.5)` e `(17.42, 0, 7.6)`, passam para `(26.6, 0, 16.8)` e `(26.6, 0, 21.15)` na lateral do acervo norte. Foram movidas as quatro estantes, suas duas colisões e placas; continuam **62 estantes** ao todo. As coordenadas indicam os centros dos conjuntos em planta.

Um anteparo de reboco em `x = 16`, entre `z = 2.6–12.15`, separa livros e acesso dos banheiros. O vestíbulo comum tem aproximadamente **2,84 m livres**, entrada de **2,00 m** e lintel com **2,60 m livres**. Um pilar existente reduz localmente a faixa, sem ter sido removido. As duas plantas sanitárias, suas seis cabines e instalações permanecem nas posições anteriores. A medida resolve a proximidade com o acervo sem refazer a organização geral restaurada.

Capturas de conferência: `output/CampusLibrary-Sanitarios-{Acesso,Separacao,Acervo}.png`. Não usar as fotografias institucionais como comprovação dessa planta: o anteparo é uma adaptação ao mapa do jogo.

## Escala em relação ao jogador

Referência: prefab global `Player`, sem mudar sua escala ou o sistema persistente. O `CharacterController` tem aproximadamente **2,121 m** de altura em mundo e raio de **0,237 m**; `Head` fica aproximadamente **1,839 m** acima dos pés. Valores incluem a escala não uniforme já existente no prefab.

| Elemento | Dimensões e decisão |
| --- | --- |
| Cadeiras de leitura | **1,0956 m** de altura, aproximadamente 0,6468 m de largura e 0,7128 m de profundidade. Aumento adicional de 20% sobre os 0,913 m restaurados, ancorado nos pés, com colisão acompanhando a mudança |
| Estantes | **2,58 m** de altura; cada conjunto duplo ocupa aproximadamente **1,488 × 3,18 m**. Os livros e as placas acompanham o conjunto |
| Mesas novas | Tampo a **0,96 m**, área **3,36 × 1,62 m**, pés até o piso |
| Cabines novas | Tampo a **0,96 m**, largura **1,74 m**, profundidade **0,936 m**; divisórias de aproximadamente **1,584 m** |
| Pesquisa e atendimento | Oito bancadas de pesquisa aumentadas com computadores e cadeiras, espaçamento entre centros corrigido para 3,24 m. Balcão de atendimento ampliado a partir do piso elevado, sem mover degraus ou corrimãos |
| Sanitários reutilizados | Vasos com 1,05 m de altura; pias com 0,63 m de altura, base em 0,50 m e topo em 1,13 m. Encaixe, fundo e dreno ajustados à bancada |
| Passagem da ala | **6,00 m** de largura e **3,20 m** de altura livre |
| Salas de estudo | Entrada de **1,60 m**, acima da altura da cápsula |
| Teto | Cota próxima de **4,60 m** |

O aumento de 20% é aplicado por conjunto de mobiliário: 17 mesas, 18 posições individuais, oito bancadas de pesquisa, recepção, 31 pares de estantes, catálogo, periódicos e 95 cadeiras. Luminárias e objetos apoiados nesses móveis acompanham a transformação; a traseira das cabines permanece ancorada junto à parede. Os oito computadores da pesquisa receberam maior espaçamento entre centros para evitar sobreposição. Piso, pilares, portas, teto, degraus, corrimãos e player não foram redimensionados. Meshes globais permanecem intactos; os lotes exclusivos do mapa foram ajustados por peças conectadas e as instâncias compartilhadas, por seus transforms.

## Materiais e direção visual

### Iluminação e sinalização — fila de tarefas

A revisão posterior ao retorno para `32028565` ajusta as luzes existentes, sem alterar o perfil de pós-processamento, névoa ou configuração global da URP. Pontos do salão usam intensidade 12, os da ala norte 10, com alcance de 8,5 m no teto; os pontos baixos dos sanitários usam intensidade 8 e alcance 5,5 m. As doze luzes de tarefa passam a intensidade 1,25, alcance 2,2 m e tonalidade quente. São valores do componente Light, sem equivalência declarada em lux.

Duas luminárias existentes acima das salas coletivas passam de Point para Spot voltado ao piso, cone externo 112°, alcance 7 m e sombras suaves no nível baixo de resolução da URP. As demais sombras existentes são preservadas; a quantidade de luzes não aumenta. Quatro vistas foram revistas na Unity, com 0 scripts ausentes e 0 materiais inválidos. Capturas: `output/CampusLibrary-Luz-{Mesas,Sala,Individual,Entrada}.png`; custo de GPU não medido.

A entrada do acervo recebe placa de duas faces com “SEÇÕES DO ACERVO / CONSULTE AS PLACAS NAS ESTANTES”. Os 31 textos de classificação originais mantêm seus objetos e códigos, agora em placas individuais de 1,75 × 0,50 m antes da correção de escala dos conjuntos. Três placas foram afastadas dos pilares que as encobriam. A geometria usa meshes exclusivos persistentes e materiais compartilhados; painéis antigos tiveram somente os triângulos correspondentes retirados dos lotes, preservando assets e identidades. Capturas: `output/CampusLibrary-Sinalizacao-{Entrada,Marcadores}.png`.

### Referências reais pesquisadas — 07/11/2026

Duas fotografias oficiais da Biblioteca Central Zilma Parente de Barros foram inspecionadas antes de retomar os ajustes:

- [Salão de leitura](https://oficial.unimar.br/wp-content/uploads/2020/07/biblioteca-e1643832656844.jpg), vinculado à [página oficial](https://oficial.unimar.br/biblioteca/): mesas claras de quatro lugares, assentos azuis, pilares claros com faixas verdes, vigas laranjas, janelas altas, luzes lineares e piso granulado. A arquitetura existente já compartilha parte dessa identidade.
- [Nichos de estudo](https://portal.unimar.br/site/public/img/noticias/148/biblioteca-site.jpg), publicados no [portal institucional](https://portal.unimar.br/site/noticias/biblioteca-da-unimar-e-revitalizada): módulos escuros com painéis laterais altos, mesa, cadeiras, quadro branco e cabeceira superior.

São referências de aparência, sem medidas confiáveis de planta. Elas não estabelecem a posição dos banheiros nem o controle de acesso ao acervo; nesses pontos vale o contexto do usuário. As fotos foram consultadas temporariamente fora de Assets, sem importação ou redistribuição como texturas. Publicação institucional da Unimar; autoria fotográfica individual e licença de reutilização não identificadas nas páginas.

Propostas de gameplay para esse espaço: [possíveis mecânicas](CampusLibrary-Mecanicas.md). São ideias para decisão futura, sem implementação nesta tarefa documental.

Fontes normativas: [prompt mestre](../../.codex/PROMPT_MESTRE.md) e [skill local](../../.codex/SKILL.md). Referência observada: captura oficial [Hotel Lobby, Total Chaos clássico](https://www.moddb.com/mods/total-chaos/images/hotel-lobby), de wadaholic. Orienta contraste, leitura, superfícies usadas e profundidade; nenhum asset do mod foi extraído.

A nova ala mantém granito, reboco institucional, carvalho gasto, aço escuro, mobiliário azul e vigas alaranjadas. Janelas laterais têm montantes e peitoris; as salas de estudo têm base opaca de 0,90 m e parte superior envidraçada, com montantes e travessas. O teto modular tem perfis e luminárias físicas. Algumas estão apagadas; as doze cabines têm luminárias de tarefa com luz quente localizada. Não houve mudança global de névoa, shader ou pós-processamento. Madeira e cadeiras conservam texturas e normais compartilhadas, sem cópias de imagens.

Superfícies novas usam **183 meshes persistentes** por célula espacial e material. Estantes e cadeiras reutilizam sete meshes compartilhados, sem alterar seus bytes. O material exclusivo `Materials/North wing worn oak.mat` varia a cor-base do carvalho para preservar leitura nos tampos; referencia as mesmas texturas e normais. O vidro reutiliza `Global/Materials/CampusNight/Night clear dusty glazing.mat`. Não foram alterados materiais globais.

Luzes adicionais: pontos nas luminárias do teto e spots curtos nas doze luminárias de tarefa, todos sem sombras em tempo real. As primeiras capturas motivaram aumentar a luz de estudo, clarear o material local, envidraçar as divisórias e afastar a mesa da sala 1 do pilar. Medir custo no hardware alvo antes de afirmar desempenho ou ampliar sombras.

## Dependências, conexões e preservação

### Reutilização de modelos na fila de tarefas

Foram integradas **31 instâncias** de recursos já baixados: seis vasos `Toilet Elongated A`, seis pias `Sink A`, cinco `Metal Trash Can`, dois `Korean Fire Extinguisher 01` e doze `Desk Lamp Arm 01`. Autores, CC0, fontes, conversões e movimentos estão em `Licenses/AssetSources/CampusLibrary/reused-props-sources.json` e nos três MDs de créditos. Não houve download de modelo novo; os móveis e a arquitetura específicos do mapa continuam autorais/reutilizados.

Dezenove meshes, materiais, texturas e o prefab do extintor foram compartilhados em Global pela Unity API; GUIDs e SHA-256 conferidos sem divergência. Os consumidores de LawSchool mantêm as mesmas referências. A luminária usa geometria estática exclusiva da cena, extraída da pose de bind, sem alterar o FBX/importador de origem; uma peça auxiliar incompatível com a extração foi omitida. Bancadas receberam aberturas persistentes para as cubas, com encaixes locais de fundo e dreno. Objetos/IDs anteriores são conservados, com superfícies e colisões substituídas desativadas.

A conferência na Unity reabriu a cena com **0 scripts ausentes e 0 materiais inválidos**, 31 instâncias e revisão visual dos sanitários e luminárias. A tarefa de modelos está concluída na fila. Capturas: `output/CampusLibrary-Modelos-{Sanitarios,Lavatorios,Luminarias}.png`.

### Conexões e arquivos protegidos

- Entrada: `CampusExterior → CampusLibrary / FromCampusExterior`; chegada `(0, 1.75, -9.6)`.
- Saída: `CampusLibrary → CampusExterior / FromCampusLibrary`; trigger `(0, 1.9, -11.48)`.
- `GameManager`, HUD, inventário, munição, player persistente e transições continuam compartilhados. A ampliação não cria outro player nem muda o menu.
- Foto `Campus library portrait.png` e arte `Resources/Textures/UI/Transitions/CampusLibrary.png`: preservar as alterações locais preexistentes.
- Prefab e meshes persistentes bastam para jogar. As quatro ferramentas temporárias de Editor e seus `.meta` foram retirados de `Assets` após validar os resultados e confirmar zero consumidores serializados de seus GUIDs e nenhum uso no código do jogo. Permanecem apenas na área descartável `Temp/CampusLibraryRevision/`, ignorada pelo Git; não são necessárias para carregar ou jogar o mapa.

## Validação da fila de tarefas — 07/11/2026

Executada na Unity **6000.5.7f1**, URP **17.5.0**, após restaurar a organização de `32028565`. As verificações desta seção são novas; a seção seguinte conserva os testes da ampliação anterior.

| Verificação atual | Resultado e limite |
| --- | --- |
| Cena e prefab | Salvos e reabertos na Unity; **0 scripts ausentes**, **0 materiais inválidos**; 95 cadeiras, 62 estantes, duas salas coletivas e 31 modelos reutilizados |
| Escala e colisões | Aumento de 20% por conjunto; 132 meshes exclusivos ajustados por 2.526 peças conectadas, instâncias compartilhadas transformadas e 237 colisores independentes acompanhando o mobiliário |
| Apoio no piso | **95 / 95 cadeiras** verificadas por raycast; diferença máxima de aproximadamente **0,00000006 m**, dentro da precisão numérica |
| Arquitetura durante a escala | Registro de posição, escala e dimensões dos colisores de paredes, pisos, teto, pilares, portas, degraus e conexões idêntico antes/depois do aumento dos móveis; o anteparo sanitário já integrava o registro inicial |
| Navegação | **28 / 28 rotas completas**, tanto após separar os sanitários quanto após aumentar os móveis; inclui as seis cabines, pesquisa, salas, acervo e circulação junto às estantes transferidas |
| Agente de navegação | Altura **2,141 m**, raio **0,27 m**, degrau **0,36 m**, voxel **0,08 m**. NavMesh temporário, sem novo asset ou mudança no runtime |
| Referências | **487 GUIDs externos distintos no prefab** e **512 no conjunto dos 447 arquivos de cena/prefabs/materiais/assets da pasta**, todos resolvidos em Assets ou pacotes; GUIDs internos padrão da Unity excluídos dessas contagens |
| Identidades | **5.821 / 5.821 fileIDs** da versão restaurada preservados; prefab atual com **6.301 IDs** |
| Recursos preservados | SHA-256 idêntico dos oito arquivos protegidos: Library antiga, CampusExterior, Player, cena CampusLibrary, duas imagens preexistentes, prefab LawSchool e prefab CampusBlock4. Os sete meshes compartilhados também mantêm os hashes |
| Assets movidos para Global | **19 / 19** com conteúdo e GUID preservados; caminhos atuais das texturas compartilhadas atualizados no manifesto sanitário de LawSchool |
| Ferramentas temporárias | Oito fontes de Editor e seus `.meta` retirados de Assets após salvar/reabrir os resultados e verificar **0 consumidores por GUID ou código do jogo**; cache descartável em `Temp/CampusLibraryScale/FinishedEditor/`, sem dependência para jogar ou carregar o mapa |
| Compilação final | Scripts recompilados pela Unity após a retirada das ferramentas, com sucesso; não é um build de executável |
| Play Mode controlado | **Um PlayerMovement**, cápsula real **0,4733 × 2,1210 × 0,4733 m**, olhos a **1,838885 m** dos pés. `CharacterController.Move` atravessou a abertura original → ala norte até `z = 14` |
| Revisão visual | Capturas com FOV 60° em **1920 × 1080**, incluindo comparação antes/depois e câmera do jogo posicionada na Head real; não equivale a teste de input ou acompanhamento automático da Cinemachine |

Capturas atuais de escala: `output/CampusLibrary-Escala-Antes-{Mesas,Sala}.png`, `output/CampusLibrary-Escala-Depois-{Mesas,Sala,Individual,Entrada}.png` e `output/CampusLibrary-Escala-Player-Real.png`. As capturas de modelos, sinalização, luz e sanitários acima registram etapas anteriores ao aumento final do mobiliário.

Hardware informado pela Unity: **Intel UHD Graphics 620**, **Intel Core i5-8265U**, aproximadamente **8 GB de RAM**. Não houve benchmark, build/executável, percurso manual completo, verificação auditiva ou nova ida/volta pelos portais. Os contratos dos portais foram preservados por comparação de conteúdo. Problemas históricos de `CampusParkingVehicleSetup` e avisos de áudio do ambiente aditivo não foram tratados como regressões desta biblioteca nem como prova de Console global limpo.

Checklist interno da skill, limitado às capturas e à passagem controlada: escala 2, materiais 2, desgaste causal 1, fontes de luz 2, sombras/contato 2, profundidade 2, uso/abandono 1, props/colisões 2, legibilidade 2, identidade 2 e pós-processamento 2: **20 pontos nos 11 itens avaliados**. Desempenho não avaliado; sem aprovação integral dos 12 critérios ou afirmação de FPS.

## Validação histórica da ampliação — commit 32028565

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
