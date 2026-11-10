# CampusLibrary — biblioteca universitária da Unimar

## Leitura rápida

- Chaves 4/5/6/10/11 com metal envelhecido e número gravado, altura visual de aproximadamente 24 cm e raio de coleta de aproximadamente 1,05 m sobre as mesas. Distribuição e proposta dos puzzles preservadas. Conferência estática, sem Unity/Play Mode. [Contrato dos sprites e coleta](PrisonCellKeys.md).
- Distribuição atual: cinco chaves de cela, **4/5/6/10/11**. Chaves 10 e 11 na cabine individual ao norte e na mesa da leitura silenciosa, respectivamente. [Proposta de puzzles curtos de leitura](CampusLibrary-Puzzles-Leitura.md); os enigmas ainda não estão implementados.
- Cena: `Assets/_Project/Scenes/CampusLibrary/CampusLibrary.unity`; interior no prefab `Prefabs/CampusLibrary.prefab` da mesma pasta. A cena referencia o prefab; não há geração do mapa em runtime.
- Este é o interior novo do campus Unimar. `Scenes/Library/Library.unity` é a biblioteca antiga e não pertence a esta revisão.
- Fila de trabalho: [tarefas da biblioteca](CampusLibrary-Tarefas.md), uma por vez, priorizando a última pendente e marcando após validação.
- Design de gameplay: [mecânicas gerais](CampusLibrary-Mecanicas.md) e [puzzles caóticos com inimigos](CampusLibrary-Puzzles-Caoticos.md); propostas documentadas, sem implementação.
- Contexto, portais e histórico: [README_CAMPUS_EXTERIOR.md](../../README_CAMPUS_EXTERIOR.md), seções 5, 6 e 7.
- Revisão de 08/11/2026: fechamento dos sanitários até o teto do salão e inscrições físicas em atlas, descritos em [Revisão final de paredes e sinalização](#revisão-final-de-paredes-e-sinalização--08112026). As dimensões e a circulação do mobiliário permanecem as da revisão anterior.
- Revisão de 07/11/2026: ampliação ao norte, a partir de `z = 12`, mantendo hall, atendimento, degraus, foto, pesquisa e sanitários. Após restaurar a organização do commit `32028565`, a fila de tarefas revisa escala do mobiliário, modelos, luz, sinalização e separação dos sanitários. Nova ala em `North wing - university library`, com leitura silenciosa, duas salas de estudo, cabines e acervo por assunto.
- Envelope aproximado: **56 × 42 m**, antes **56 × 24 m**. Acréscimo de aproximadamente **1.016 m²**, cerca de **76%** sobre os 1.344 m² nominais anteriores; área bruta de planta, sem descontar paredes e móveis. Limite interno norte: `z = 30.15`.
- Paleta e mobiliário de `Global/Materials/CampusInterior/` e `Global/Meshes/CampusInterior/`. Meshes exclusivos novos: `CampusLibrary/Meshes/NorthWing*.asset`.
- Nenhum novo download de modelo 3D. Fontes e hashes da reutilização: `Licenses/AssetSources/CampusLibrary/expansion-sources.json` e `reused-props-sources.json` da mesma pasta; os três documentos de créditos registram o uso nesta ala.

## Acesso com duas chaves — 09/11/2026

A entrada pelo CampusExterior exige **as duas chaves de acesso à biblioteca**, guardadas no inventário persistente `PlayerCellKeys`. Uma chave sozinha, chaves de cela ou a chave da manutenção não liberam a entrada. As chaves de acesso permanecem no inventário, permitindo voltar à biblioteca na mesma sessão; a saída continua livre e os spawn IDs foram preservados.

| Chave de acesso | Obtenção | Identidade |
| --- | --- | --- |
| Direito | Sorteada por partida pelos pontos já configurados em LawSchool; posição mantida ao retornar ao prédio. Alarme e manutenção preservados | `campus-library-law-school`; latão, etiqueta ocre “BIBLIOTECA / DIREITO” |
| Bloco 4 | Recompensa na sala 423, atrás da porta com senha; exige puzzle resolvido e porta totalmente aberta | `campus-library-block-4`; aço, etiqueta azul “BIBLIOTECA / BLOCO 4” |

A porta externa recebe chapa metálica com **duas fechaduras identificadas**, os dois desenhos de chave e contador **0/2 → 1/2 → 2/2**. Cada indicador responde à sua chave. Uma travessa e um bloqueio sólido permanecem enquanto falta alguma; ambos são liberados com as duas. `CampusLibraryEntranceLock` implementa o guard já aceito por `SceneTransitionDoor`; nenhum sistema paralelo de inventário ou carregamento foi criado.

Conforme a resposta explícita do usuário, as antigas chaves de cela **1/2/3/9**, em Direito, e **7/8/12/13**, no Bloco 4, foram retiradas do gameplay. Os dois pickups principais foram convertidos para acesso; os seis adicionais foram desativados, com objetos/fileIDs preservados. **Não foram transferidas para a biblioteca.** As cinco chaves que já estavam na biblioteca permanecem nesta rodada. O gerador e a mecânica que abrirão as celas serão implementados depois; os oito IDs retirados ainda não têm nova forma de abertura.

Sprites finais: `Global/Textures/Pickups/LibraryAccessLawSchool.png` e `LibraryAccessBlock4.png`, sob `Assets/_Project/`. PNGs gerados com alpha transparente, importados como sprites individuais; imagens numeradas anteriores preservadas. Fonte, prompts e SHA-256: `Licenses/AssetSources/CampusLibrary/library-access-key-sprites.json`. Os três MDs de créditos registram a geração integrada por imagegen. A troca visual não altera os triggers de coleta.

As mudanças foram aplicadas às **cenas jogáveis e aos prefabs independentes** de Direito, Bloco 4 e CampusExterior. A cena independente de LawSchool não depende do prefab para receber a mudança. O interior e o layout da biblioteca foram preservados.

### Verificação do acesso — 09/11/2026

Unity 6000.5.7f1, URP do projeto, Windows/D3D11, modo de edição. Cenas e prefabs de Direito, Bloco 4 e CampusExterior reabertos; componentes e referências do novo acesso conferidos.

| Verificação | Resultado |
| --- | --- |
| Pickups nos blocos | Uma chave de biblioteca em cada bloco; zero pickups de cela ativos nos dois. Manutenção de Direito preservada |
| Sorteio de Direito | 33 pontos preservados; política de escolha estável na mesma sessão testada em inventário isolado; PlayerPrefs temporário restaurado |
| Puzzle do Bloco 4 | Referências ao keypad e à porta preservadas, junto aos checks de solução e abertura total; código 24153 mantido |
| Exigência de duas chaves | Cinco casos executados no componente: nenhuma, somente Direito, somente Bloco 4, ambas, todas as antigas chaves de cela. Somente ambas liberam o guard e a barreira |
| Apresentação real da porta | Quatro combinações de inventário aplicadas ao guard da cena; contador 0/2, 1/2 e 2/2, indicadores, barreira e travessa conferidos. Recortes das fechaduras persistentes e barreira alinhada à frente da chapa |
| Sprites | PNGs 1254 × 1254 com alpha transparente; importação individual, limite 1024, 512 pixels/unidade. Altura de quadro aproximada 0,28 m, sem redimensionar os triggers |
| Progressão das celas | Chaves de biblioteca não concedem chave de cela; cinco pickups já existentes dentro da biblioteca mantidos. Gerador futuro ainda sem implementação |
| Preservação | IDs anteriores dos seis assets-alvo mantidos; interior/cena CampusLibrary, Player, Library antiga e PrisonProgress com conteúdo preservado |

Capturas em 1600 × 900, FOV 50°: `output/Biblioteca-Acesso-Duas-Chaves.png` (0/2) e `output/Biblioteca-Acesso-Estado-{1,2,3}.png` (Direito, Bloco 4, ambas). Inventários de teste foram destruídos e o player anterior restaurado; os estados de teste não foram gravados nas cenas.

Nesta rodada não foram executados Play Mode, coleta real, ida/volta pelos portais, uso do teclado por input, build/executável ou benchmark. Os testes de Play Mode nas revisões anteriores desta ficha continuam históricos, sem serem contabilizados como novos.

`LawSchoolCellKeyPickup` e `Block4TeacherKeyPickup` conservam seus nomes de classe/GUIDs por compatibilidade com os assets; seus papéis atuais são as duas chaves de biblioteca. Inventário e guard: `Global/Scripts/Player/CampusLibraryAccess.cs` e `Global/Scripts/Utils/CampusLibraryEntranceLock.cs`. As ferramentas pontuais de Editor foram retiradas depois da verificação de dependências; os resultados persistentes bastam para jogar.

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

Um anteparo de reboco em `x = 16`, entre `z = 2.6–12.15`, separa livros e acesso dos banheiros. O vestíbulo comum tem aproximadamente **2,84 m livres**, entrada de **2,00 m** e lintel com **2,60 m livres**. Na correção de circulação, o pilar foi incorporado à parede oeste, liberando a faixa de passagem. As duas plantas sanitárias, suas seis cabines e instalações permanecem nas posições anteriores. A medida resolve a proximidade com o acervo sem refazer a organização geral restaurada.

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

## Revisão final de paredes e sinalização — 08/11/2026

### Correções persistentes

As oito peças de parede dos dois sanitários terminavam em **3,20 m**, abaixo do teto do salão a **4,60 m**. As paredes e lintéis agora chegam a 4,60 m, preservando o vão de **2,50 m** sob os lintéis e os tetos internos a 3,20 m. As duas paredes de privacidade foram prolongadas **0,20 m** até os lintéis, eliminando as fendas verticais. Os extremos leste das paredes alcançam a parede externa em **x = 28,15**; uma peça de fechamento une os retornos entre **z = 7,30 e 7,70**. Também foram fechadas as duas frestas de **0,05 m** entre os tetos internos e a parede externa. Os oito colisores de parede e os dois do teto acompanham as superfícies, mais um collider na nova junta. UVs métricas mantêm a densidade do azulejo.

Os cinco meshes existentes `CampusLibrary_{5_2,5_3,6_2,6_3,6_4}_Restroom ceramic.asset` e os dois lotes `CampusLibrary_{6_2,6_3}_Mineral ceiling panels.asset` foram editados pela Unity API, com GUIDs/importadores preservados. A junta acrescentada reutiliza o material compartilhado `Restroom ceramic`. Nada foi alterado no player, nos móveis, no acervo ou nos materiais globais.

As **55 inscrições estáticas** do prefab foram rasterizadas em **Liberation Sans**, já distribuída no projeto sob SIL OFL 1.1, uma fonte sem serifa adequada à sinalização institucional impressa. A fonte anterior dos TMP também era Liberation Sans; a mudança principal é a inscrição passar a integrar uma superfície física iluminada. Cada placa preserva o conteúdo em português, acentos, códigos, orientação e posição, com ajuste de tamanho para caber no suporte. As duas faces da placa de seções continuam legíveis em seus respectivos sentidos.

As faces opacas usam **um atlas RGB 4096 × 4096**, mipmaps, filtragem trilinear e **um material local** com o shader existente `TotalChaosInspiredSurface`, sem emissão, metallic 0 e smoothness 0,20. Letras em tinta clara envelhecida, bordas discretamente gastas e fixadores sugerem impressão sobre esmalte azul; as placas de extintor conservam a base vermelha. A sinalização recebe a iluminação e as sombras do cenário. `SANITÁRIOS`, antes sem suporte, ganhou uma chapa de **1,90 × 0,42 × 0,03 m**.

Dependências novas: `Textures/Library signage atlas.png`, `Materials/Library printed enamel.mat`, 55 meshes `Meshes/Printed sign NN.asset` e os meshes da chapa/junta, todos persistentes com `.meta`. Os componentes TMP, textos e fileIDs originais permanecem no prefab, **desabilitados**, para permitir edição posterior; não há texto de cenário gerado em runtime. Para mudar uma inscrição, editar sua origem preservada e atualizar a região correspondente do atlas/mesh. HUD, menus e textos de outras cenas mantêm seu funcionamento.

Fonte, autoria, licença, conversão e SHA-256: `Licenses/AssetSources/CampusLibrary/printed-signage-sources.json`. Nenhuma fonte ou modelo novo foi baixado. A captura oficial de [Hotel Lobby, Total Chaos clássico](https://www.moddb.com/mods/total-chaos/images/hotel-lobby), de wadaholic, foi consultada temporariamente para orientar integração de superfícies, luz e desgaste; não foi importada como asset.

### Verificação desta revisão

Unity **6000.5.7f1**, URP **17.5.0**, Windows, resolução **1920 × 1080**, FOV **60°**. A biblioteca foi carregada isoladamente para evitar que outra cena aberta aditivamente interferisse na renderização ou na física; o conjunto anterior de cenas do Editor foi restaurado ao fim dos testes.

| Verificação | Resultado |
| --- | --- |
| Compilação, prefab e cena | Recompilação e reabertura na Unity; **0 scripts ausentes e 0 materiais inválidos** |
| Fechamento visual dos sanitários | **12.344 / 12.344 amostras** sobre os triângulos renderizados, incluindo paredes superiores, juntas e tetos; vãos de entrada abaixo de 2,50 m excluídos intencionalmente |
| Demais juntas revisadas | **68 / 68 amostras** nas janelas e divisórias das salas, sem novos reparos necessários |
| Sinalização | **55 / 55 faces persistentes**, **0 TMP ativos** no prefab, materiais/meshes salvos; inspeção de capturas do hall, sanitários, acervo e salas |
| Navegação | **34 / 34 rotas completas**; agente 2,141 m, raio 0,27 m, degrau 0,36 m, voxel 0,08 m; NavMesh temporário, sem asset integrado |
| Circulação e apoio | Passagem central e corredor sanitário sem caixas sólidas invadindo as áreas verificadas; **95 / 95 cadeiras** apoiadas; **62 estantes** conservadas |
| Play Mode físico | **31 trajetos** seguindo os cantos do NavMesh por `CharacterController.Move`, incluindo as seis cabines, pesquisa, ala norte e salas; cápsula real **0,4733 × 2,1210 × 0,4733 m** |
| Colisão das juntas corrigidas | Três tentativas físicas de cruzar a parede em **z = 4,40; 7,50; 9,10**, bloqueadas |
| Portais em Play Mode | **CampusLibrary → CampusExterior → CampusLibrary** por contato real com os triggers; mesmo jogador, **vida 100, carregador 10 e reserva 30** preservados. A automação encerrou o menu inicial/pausa antes do teste; nenhum contrato de transição foi alterado |
| Preservação | **6.333 / 6.333 fileIDs originais** preservados, 6.564 finais; **550 GUIDs externos** do prefab resolvidos; nenhum GUID duplicado nos metadados consultados; importadores anteriores intactos |
| Recursos protegidos | SHA-256 idêntico de **65 arquivos** protegidos, incluindo as cenas CampusLibrary/CampusExterior, prefab do exterior e meshes/materiais compartilhados do mobiliário |

As rotas de Play Mode foram automatizadas, sem uma sessão manual completa por teclado/controle. As amostras geométricas e as capturas cobrem os trechos descritos, sem constituir garantia de ausência absoluta de defeitos. **Não houve build de executável, benchmark com Profiler ou teste auditivo.** O atlas novo tem custo de memória próprio; não foi afirmado ganho de FPS.

Checklist interno da direção visual: escala 2, materiais 2, desgaste causal 1, fontes de luz 2, sombras/contato 2, profundidade 2, uso/abandono 1, props/colisões 2, legibilidade 2, identidade 2 e pós-processamento 2: **20 pontos nos 11 critérios avaliados**. O 12º critério, desempenho, permanece sem nota por falta de medição; não equivale a uma aprovação integral ou benchmark.

As ferramentas pontuais desta revisão ficam em `Temp/LibraryFinalReview/`, fora dos assets finais após conferir as dependências; não são necessárias para carregar ou jogar. A falha preexistente do gerador `CampusParkingVehicleSetup`, externa à biblioteca, continua separada dos resultados desta revisão. Capturas finais: `output/CampusLibrary-Final-after-*.png` e `output/CampusLibrary-Final-Player-Sanitarios.png`.

### Complemento após conferência do usuário — fechamento frontal e cabines

A vista deixada pelo usuário no Play Mode mostrou um trecho que a primeira amostragem não cobria: as duas paredes frontais começavam em **x = 20,65**, deixando aproximadamente **1,73 m** até o encontro com a parede lateral em **x = 18,92**. As paredes visíveis e seus colisores agora abrangem essa faixa em **z = 3,00 e 7,70**, do piso ao teto. Permanecem somente as entradas laterais previstas; não há mais passagem frontal direta do acervo para as cabines.

Conforme a escolha explícita do usuário, as portas continuam **estáticas e entreabertas**, sem mecânica nova de abertura. Os seis painéis frontais esquerdos foram ampliados até os encontros; foram acrescentados seis painéis frontais direitos e as duas divisórias terminais a oeste. Cada folha de **0,85 m** corresponde ao seu vão de **0,85 m** em posição fechada. A pose salva foi ajustada de 55° para **85°**, com puxadores e dobradiças acompanhando a folha, para conservar acesso ao vão; o collider passou da caixa alinhada aos eixos para uma caixa orientada de **0,85 × 2,05 × 0,05 m**, acompanhando a geometria. As folgas de 0,125 m sob os painéis e sua altura de 2,05 m são próprias da divisória sanitária, não trechos de parede incompletos.

Reutilizados os materiais existentes; acrescentados apenas dois meshes persistentes de painéis em `Meshes/Cubicle {west end partition,front right return} mesh.asset`, compartilhados entre as instâncias locais. Os **6.564 fileIDs anteriores** foram preservados; prefab final com **6.606 fileIDs**. As 55 inscrições impressas permanecem iguais às da primeira etapa.

Nova validação na Unity, com a cena isolada: **6.588 / 6.588 amostras de triângulos** nas duas fachadas completas e nos painéis fixos das cabines, excluindo os seis vãos intencionais; **34 / 34 rotas completas**, incluindo as seis cabines, com o mesmo agente de teste. As capturas `output/CampusLibrary-Sanitarios-Final-{VistaApontada,Fachada,Cabines,Cabines2,Entrada}.png` substituem as vistas anteriores para avaliar o fechamento frontal e os encaixes. As capturas da primeira etapa conservam apenas valor histórico nesses trechos.

O Play Mode repetiu os **31 trajetos físicos**, incluindo as seis cabines, e as três tentativas nas juntas laterais, com sucesso. O usuário também conferiu o mapa por conta própria e confirmou a correção do problema; outros refinamentos visuais ficam para revisão futura.

Ferramenta complementar e registros em `Temp/LibraryFinalReview/WallFollowup/`; sem gerador necessário em runtime. As limitações de build, benchmark, áudio e input manual descritas acima continuam válidas.

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

## Integração de development — 09/11/2026

Merge em fix/library preserva a ampliação, a escala atual do mobiliário e as revisões de paredes, sanitários e sinalização. As três chaves recebidas de development foram incorporadas sem substituir o prefab da biblioteca: cela 4 na mesa individual original, cela 5 na mesa em carvalho e cela 6 na bancada de pesquisa original. Posições adaptadas à transformação atual de cada móvel, mantendo a posição relativa sobre o tampo: (6; 1,606; −9,464001), (−23,0839988; 0,96000004; −1,974) e (−26,174; 0,9420001; 10,546), respectivamente. Sprites e pickups mantêm IDs e referências recebidos.

O prefab mantém todos os IDs de ambos os lados, com 6.632 registros. Veículos do exterior foram comparados por hierarquia, componentes e referências: conteúdo equivalente, com diferenças de IDs internos/ordenação; preservada a versão local e seus IDs. Ferramentas e capturas temporárias dos créditos, já retiradas a pedido do usuário, não foram reintroduzidas. Conferência estática de referências e merge; sem abrir/focar Unity, compilação, jogo, Play Mode, build, prints ou vídeos. Testes de gameplay com o usuário.

## Chaves transferidas de Direito — 09/11/2026

Cinco chaves de cela ativas: 2, 3, 4, 5 e 6. As chaves 2/3 foram transferidas de Direito, que deve conter apenas cela 1 e manutenção. Novos pickups reutilizam sprites, material e scripts existentes, nas mesas de estudo individual e carvalho, deslocados 0,55 m dos pickups 4/5. IDs anteriores e layout preservados. Sem abrir Unity ou executar jogo, testes, compilação, build ou capturas.
