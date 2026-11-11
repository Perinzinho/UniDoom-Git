# CampusLibrary — missão do gerador

Implementação de 09/11/2026 baseada no README fornecido pelo usuário, no projeto existente, Unity 6000.5.7f1 / URP 17.5.0. A biblioteca antiga `Scenes/Library` é independente.

## Avisos, placas e porta de funcionários — 10/11/2026

`Global/Scripts/UI/GameplayNotice.cs` compartilha a apresentação de `DirectorSecretPassage`: texto central a 79% da altura da tela, fonte de 14–24 px conforme a resolução, cor creme e sombra preta. Descoberta, fusíveis, instalação, resultado do puzzle e chaves numeradas usam essa apresentação; os prompts de interação e o aviso da entrada externa trancada usam o mesmo desenho. A mensagem mais recente substitui a anterior, sem fila de contagens vencidas. Avisos têm 4,5 s de leitura, suspendem o tempo durante pausa/modal e são limpos ao morrer ou trocar de cena. O checkpoint continua com seu aviso próprio. Nenhum inventário ou Canvas paralelo foi criado.

A passagem interna do gerador, centrada em **(-22,5; 0; 38,8)**, recebeu porta dupla com esmalte azul e aço já usados na biblioteca. Vão original de **2 × 3,2 m**, com vão do batente de **1,88 × 3,12 m**; folhas de **0,92 × 3,08 m**, abertura de 95° por contato com `OpenDoorWithKey`, sem chave adicional ou reload de setor. Placas físicas nas duas faces dizem **ENTRADA SOMENTE PARA FUNCIONÁRIOS**. Foram desativados os objetos `Safety sign` (“ÁREA TÉCNICA • BARREIRA DE SEGURANÇA”) e `Generator instructions` (instruções antigas), incluindo suas placas filhas. Identificações de acervo, leitura, atendimento, sanitários, manutenção/oficina e os três bilhetes permanecem.

A placa da entrada principal da manutenção, em **(-18,5; 1,95; 33,86)**, agora usa `Textures/Campus Library Sign cutout.png`, derivado da imagem fornecida pelo usuário `Campus Library Sign.jpeg`. O fundo externo foi retirado, mantendo o fundo branco interno, a mão vermelha e o texto **ATENÇÃO / ENTRADA SOMENTE PARA FUNCIONÁRIOS**. O suporte metálico existente foi ajustado para **1,62 × 1,10 m**; a face é um `SpriteRenderer` de **1,5 × 1,0 m**, com material transparente local baseado no shader TotalChaos já usado no projeto. O texto anterior da placa permanece serializado, mas inativo. Fonte e hashes em `Licenses/AssetSources/CampusLibrary/staff-sign-user-provided-2026-11-10.json`. Inspeção visual na Unity/Play Mode pendente.

O Play Mode identificou uma dependência quebrada **já presente na cena**: o override remove `Generator install and ignition switch`, mas o controlador ainda o usava como alvo obrigatório. O prefab agora referencia `Generator interaction target`, independente da peça removida, em **(-24,7; 1,85; 41,64)**, com o mesmo pequeno volume de interação e sem renderer. As remoções e demais overrides da cena foram preservados; o interruptor antigo não voltou a aparecer. Esse ajuste permite inicializar a missão e exibir seus avisos.

Validação desta revisão: compilação C# e Unity **6000.5.7f1 / Direct3D 11**, cena/prefab carregados e **31 verificações** aprovadas em Play Mode na cópia isolada existente. Incluem ausência de scripts faltantes, placas desativadas, abertura das duas folhas, travessia com `CharacterController` real, descoberta, três coletas, substituição de mensagens, pausa/retomada, chave numerada, instalação e abertura modal em uma interação, sete terminais, energia e aviso final ao fechar o modal. Interações acionadas programaticamente; não representam teste manual de teclado/controle. Captura de **1600 × 900** da porta em iluminação de emergência inspecionada: inscrição legível e montagem no vão conferida. A captura direta dos avisos de tela não foi produzida pelo modo batch; sua apresentação foi comparada no código compartilhado e seu comportamento verificado em runtime.

Integração: **8.708 registros/fileIDs anteriores preservados**, 123 registros novos, `.meta` do prefab preservado e referências novas conferidas. A única dependência externa adicional do prefab é o `OpenDoorWithKey` já existente; mesh, materiais e fonte reutilizados. Ferramentas desta tarefa retiradas de Assets após salvar os resultados. Build, ida/volta pelos portais, percurso manual completo e comparação visual dos avisos em múltiplas resoluções não executados. Avisos antigos de APIs obsoletas e a exceção de indexação `UnityEditor.Search.SearchDatabase` da cópia de teste não foram atribuídos a esta alteração.

Revisão de **10/11/2026**, solicitada em [correcoesBiblioteca.md](../correcoesBiblioteca.md): estantes alinhadas/preenchidas, inscrições em placas, fusíveis em sprite, área dos funcionários ampliada, instalação manual, quatro zumbis e três bilhetes. Os comandos respeitam a câmera horizontal do jogador. Validações desta revisão são registradas na fila; os testes de 09/11 abaixo são históricos.

**Modelo final do gerador — 10/11/2026:** o bloco provisório foi substituído pelos quatro `.glb` fornecidos pelo usuário, representando 0, 1, 2 e 3 fusíveis. O importador local combina as peças de cada arquivo em um mesh, preserva as cores de vértice e usa shader URP exclusivo. A variante muda após cada inserção; visual antigo fica oculto, mas seus colliders e o alvo de interação são preservados.

## Cenário persistente

Cena: `Assets/_Project/Scenes/CampusLibrary/CampusLibrary.unity`, instância de `Prefabs/CampusLibrary.prefab`. A ala **Emergency mission - annex and maintenance** está salva nesse prefab, sem geração de layout em runtime. A cena atual possui overrides de posicionamento e remoções, incluindo o antigo interruptor do gerador; eles devem ser preservados ao revisar o prefab.

- Acréscimo ao norte de aproximadamente **790 m²**, cerca de **33%** sobre a planta anterior, até `z ≈ 44,3`. Envelope nominal aproximado **56 × 56,5 m**; área bruta estimada, sem descontar móveis/paredes.
- Passagem de **4 m**, centrada em `x = 18,5`, na parede de `z = 30,3`, altura livre **3,25 m**. Recorte de dois meshes exclusivos e adaptação do collider da parede, com identidades preservadas.
- **12 estantes**, **6 mesas**, **24 cadeiras**, livros, notas e luminárias adicionais. Totais de estantes/cadeiras: **74 / 119**. Tampos a 0,96 m, com circulação entre os grupos.
- **Sala única do gerador — 10/11/2026:** a área restrita continua em aproximadamente `x = -27,05…-14`, `z = 34…44,3` (**13,05 × 10,3 m**), com a entrada original de **2,20 × 3,20 m** em `(-21,5; 0; 34)`. As divisórias do hall, da antiga sala do gerador e da oficina foram desativadas. A bancada e o bilhete 1 foram aproximados do gerador; quatro antigas peças de vão agora formam dois armários metálicos e dois dutos no teto, com um duto vertical até o armário. Luz normal/vermelha, ventilação, suportes e modelo do gerador permanecem. Na parede ao lado da entrada, a placa mostra **ATENÇÃO / ENTRADA SOMENTE PARA FUNCIONÁRIOS**. Os objetos e fileIDs originais foram preservados; Unity e Play Mode não puderam ser executados por falta de licença válida neste ambiente.
- Entrada externa, chegada `FromCampusExterior`, saída, degraus, sanitários, foto, chaves numeradas e mobiliário anterior preservados. Sem segundo andar ou escada de subsolo.
- Materiais, cube mesh persistente, estantes, cadeiras, fontes e sons reutilizados. Sprite de fusível gerado com imagegen integrado; atlas de placas e três bilhetes compostos com Liberation Sans já existente. Sem download de modelo ou pacote. Créditos e hashes em `Licenses/AssetSources/CampusLibrary/corrections-2026-11-10.json`.

## Progressão e controles

O HUD persistente `KeysUI` usa três posições com o mesmo tamanho e espaçamento das chaves dos primeiros mapas, numa fileira abaixo delas. `LibraryMissionController` fornece o sprite existente `Library maintenance fuse.png` e atualiza a quantidade visível a cada mudança de `LibraryMissionProgress`: 0–3 fusíveis carregados. A instalação no gerador remove os ícones, e a volta à biblioteca reconstrói a fileira a partir do estado da sessão; ao sair da cena, ela é ocultada. Não há inventário paralelo nem alteração nos ícones das chaves. A ligação de GUID, fluxo de coleta/instalação e limpeza de cena foram verificados estaticamente; Play Mode e inspeção visual pendentes.

Ao completar a sequência do puzzle, a transição para `GeneratorOn` reproduz uma vez o efeito de partida de três segundos `Audio/GeneratorStartup.mp3` na posição do alvo do gerador, com atenuação 3D de 2 a 14 m. O `AudioSource` ignora a pausa do listener enquanto o painel modal está aberto. O som de porta pesada antes associado a `generatorSound` foi substituído; os cliques e erros do painel continuam no áudio de feedback original. Fonte CC0, autor e hash em `Licenses/AssetSources/CampusLibrary/generator-startup-sound.json`. Referência do clip, GUID, formato MP3 e pontos de chamada foram verificados estaticamente; audição e Play Mode ainda não foram executados.

Girar a visão horizontal e usar **E** ou **X do controle**, alcance de 2,8 m, jogador vivo, gameplay ativo e bloqueio de entradas repetidas. Toda a instalação e abertura do puzzle acontecem no próprio gerador. O quadro físico de sete terminais, o porta-fusíveis de parede, suas placas, indicadores e colisores foram desativados. Fusíveis e bilhetes sobre móveis aceitam direção horizontal em cone de aproximadamente 12°, distância original e linha de visão dos olhos ao objeto: não exigem inclinar a câmera nem permitem atravessar obstáculos. A lista desses alvos é armazenada uma vez na inicialização.

O puzzle existe somente como modal de tela. Com os três itens coletados, **um único E diretamente no gerador** instala os fusíveis restantes e abre o modal industrial com quatro controles na fileira superior e três na inferior. As transições internas de instalação são preservadas para as variantes do modelo, terminando no gerador com três fusíveis visíveis; não há espera entre essas transições. `GameMenus.Screen.Interaction` pausa a simulação, bloqueia movimento, câmera, armas e HUD, libera o cursor e intercepta cliques sobre o fundo. O botão **SAIR**, Escape ou fechamento externo restauram exatamente o estado anterior. Bilhetes continuam usando o leitor de documentos existente; nenhum sistema paralelo de inventário ou progressão foi criado.

1. `NotDiscovered`: três fusíveis inteiramente inativos. Luz normal desligada, vermelhas com pulsação suave de 0,55 Hz e ambiente mínimo estável.
2. Proximidade do gerador: `SearchingFuses`, aviso central inferior no padrão da sala do diretor e ativação de exatamente três IDs **0, 1 e 2**. Reaproximar-se não reinicia nada.
3. Cada coleta registra seu bit exclusivo e desativa somente aquele item; IDs inválidos/repetidos são recusados. Aviso imediato **Fusível coletado (N/3)** durante 4,5 s, independente do foco, substituindo a mensagem anterior. Terceiro item: `FusesCollected`, sem instalação automática, com orientação para retornar.
4. Usar **E no gerador uma vez** para instalar os três fusíveis e abrir o puzzle na mesma interação. `InstalledFuseCount` percorre 0 → 1 → 2 → 3 e notifica cada estado; se a sessão já tiver um ou dois instalados, completa somente os restantes. O estágio passa a `FusesInstalled`, com a variante de três fusíveis visível sob o modal.
5. Clicar nos sete terminais do modal aberto. Sequência inicial **3 → 1 → 5 → 2 → 7 → 4 → 6**. Cada clique válido alterna um único passo visual; erro reinicia somente a tentativa e produz feedback vermelho/sonoro. Barra, LEDs, estados `ON/OFF/TRAVADO` e segmentos físicos derivam do mesmo `SequenceStep`. Fechar e reabrir com E preserva o passo atual.
6. Ao acertar o sétimo terminal, a progressão passa por `PuzzleSolved` e liga imediatamente o gerador em `GeneratorOn`: vermelhas apagadas, luz/ambiente normais restaurados e reforços encerrados. Não existe uma segunda interação de ignição após o puzzle.

Campo `sequence` de `LibraryMissionController`: sete números distintos de 1 a 7. **A ordem não é exibida ao jogador.** Cada botão tem número e função: 1 aquecer, 2 rotação, 3 admissão, 4 sincronia, 5 pressão, 6 carga, 7 excitação. Três registros dão relações entre operações, sem lista numérica pronta; as relações determinam uma única ordem. Alterar a sequência exige revisar as pistas, que não são geradas automaticamente a partir da resposta.

| Bilhete | Local | Conteúdo funcional |
|---|---|---|
| 0 — Anotação do plantão | Quadro junto à entrada restrita, `(-25,9; 1,8; 34,21)` | Admissão inicia; aquecimento vem depois; pressão antes da rotação |
| 1 — Ocorrência da oficina | Bancada técnica do gerador, `(-20,55; 1,005; 41,6)` | Aquecimento antes da pressão; rotação antes da excitação; excitação antes da sincronia |
| 2 — Aviso ao próximo turno | Mesa pública, `(-10,7; 1,025; 37,6)` | Carga encerra; sincronia imediatamente antes |

| ID | Local | Pivô do fusível |
|---|---|---|
| 0 | Mesa da nova área de estudo, esquerda (posição temporária de teste) | `(-11,35; 1,025; 37,3)` |
| 1 | Mesa da nova área de estudo, centro (posição temporária de teste) | `(-11; 1,025; 37,3)` |
| 2 | Mesa da nova área de estudo, direita (posição temporária de teste) | `(-10,65; 1,025; 37,3)` |

Os três fusíveis estão temporariamente reunidos sobre a mesma mesa, com 35 cm entre os pivôs, para agilizar o teste da coleta e instalação. IDs, ativação após descobrir o gerador, indicadores e coleta individual foram preservados.

Destaque luminoso moderado acompanha o coletável, inativo antes da descoberta. Sons existentes de coleta, acionamento mecânico, botão e erro. Sem inventário global novo.

## Persistência e integração futura

`UniDoom.CampusLibrary.LibraryMissionProgress` guarda estágio, máscara de IDs e progresso do circuito durante a partida, seguindo o padrão de `Block4PuzzleProgress`. Saída/reentrada e checkpoint preservam os dados. Não é save em disco; fechar o jogo ou iniciar outra partida limpa o progresso.

- Flag: `LibraryMissionProgress.IsLibraryGeneratorPowered`.
- Evento: `LibraryMissionProgress.OnLibraryGeneratorPowered`, uma vez na transição final. Recarregar a cena concluída não reemite o evento; consumidores tardios consultam a flag.
- `GameManager.EndSession()` e a inicialização do runtime limpam o estado.
- Assinantes devem se desinscrever ao serem destruídos/desativados.

**Nenhuma abertura, animação ou condição de cela foi implementada.** Chaves e sistemas prisionais anteriores não foram alterados.

## Zumbis e sala segura

`CampusLibraryZombie.prefab` é variante do `Global/Prefabs/Enemy/Enemy.prefab`: reutiliza sprite padrão, animações, saúde, dano, morte e loot, acrescentando agente NavMesh e restrição local. Aparências alternativas desabilitadas somente nessa variante mantêm a cápsula normal.

Entrada original dentro da porta, `(0; 1,64; -8,5)`, preservada; três marcadores adicionais próximos de `(-8; 0; 23)`, `(18,5; 0; 32,5)` e `(3; 0; 40)`, ajustados ao NavMesh. Padrão do prefab **4 vivos**, configurável entre 1 e 4; reposição **4 segundos** após morte. Tenta os pontos em rodízio com `EnemySpawnSafety`, mínimo **8 m** sobre a posição efetiva de navegação, câmera/visibilidade e exclusão da área segura. Pontos inseguros aguardam; mortes repetidas não abrem slots extras. Saúde, dano e velocidade originais. Corpos desses reforços removidos após 18 s. Sobreviventes continuam ativos após a energia; novos reforços param.

Proteção da manutenção:

1. `NavMeshModifierVolume` não navegável e NavMesh persistente em `Navigation/Library mission navigation.asset`.
2. Collider físico da entrada ignorado somente pela cápsula do jogador persistente, permitindo ida/volta.
3. `LibraryEnemyRestriction` recusa alvos protegidos no teste existente de visão/impacto e recupera atores colocados indevidamente dentro da sala, inclusive com agente desabilitado. Verifica na física e antes de renderizar; o volume inclui margem diante do batente.

`Enemy.cs` recebeu somente a consulta opcional desse componente. Inimigos sem a restrição mantêm o comportamento anterior.

## Arquivos e Inspector

Scripts em `Assets/_Project/Scenes/CampusLibrary/Scripts/`:

| Script | Responsabilidade |
|---|---|
| `LibraryMissionProgress.cs` | Estados, IDs, sequência, flag e eventos da sessão |
| `LibraryMissionController.cs` | Descoberta, interação no mundo, intenção dos terminais, mensagens, áudio e indicadores físicos |
| `LibraryGeneratorPanel.cs` | Estado de abertura/fechamento, modal industrial, hit-testing por cursor, views dos terminais e eventos `PanelOpened`, `PanelClosed`, `SwitchToggled` e `PuzzleCompleted` |
| `LibraryGeneratorModelVariants.cs` | Carrega as quatro variantes persistentes e mostra o estado de 0–3 fusíveis |
| `LibraryMissionTarget.cs` | Identificação de fusíveis, botões, comando e esquema |
| `LibraryLightingController.cs` | Emergência e restauração de luzes/ambiente |
| `LibraryMaintenanceSafety.cs` | Volume protegido e colisão seletiva |
| `LibraryEnemyRestriction.cs` | Proteção do alvo e recuperação do inimigo |
| `LibraryZombieSpawner.cs` | Limite, entrada segura, morte e reposição |

Alterações compartilhadas: reset em `GameManager.cs`, consulta opcional em `Enemy.cs` e apresentação em `GameplayNotice.cs`, compartilhada com `DirectorSecretPassage`. A missão não usa mais o toast de checkpoint de `DeathScreen.cs` nem seu antigo HUD exclusivo de coleta. Checkpoint conserva seu tamanho/texto próprios.

Além do prefab principal e dois meshes do recorte: variante de zumbi, material vermelho local e NavMesh persistente, com `.meta`. **Não há referências para ligar manualmente:** gerador, descoberta, fusíveis, botões, LEDs, texto, áudio, luzes, entrada e área segura já serializados.

Ferramentas/logs temporários em `Temp/LibraryMission/` e na cópia isolada preexistente `.utmp/DirectorRoomValidation/Assets/Editor/`. Não são dependências do jogo. Nenhum gerador novo integrado aos assets finais.

## Validação desta implementação

**Interação com os novos modelos — 10/11/2026:** 22 casos isolados executando os métodos reais de progressão e interação, com substitutos para apresentação/Unity: bloqueio antes dos três fusíveis, seis ordens de coleta, instalação parcial, abertura com um E, eventos de contagem, reabertura, erro e acionamento imediato da energia uma única vez. Compilação C# isolada dos assemblies de jogo e Editor com referências locais da Unity passou sem erros, com avisos preexistentes. Essa verificação não executa renderização, input real ou carregamento de cena. Unity, Play Mode, build, screenshots e vídeos não executados, conforme pedido do usuário.

Executada na Unity **6000.5.7f1**, na cópia isolada sincronizada com os arquivos atuais, usando player/cena reais. Os itens abaixo são históricos da implementação física anterior ao modal:

- Compilação, abertura de prefab/cena e resolução de scripts, materiais e shaders.
- `CharacterController` pela passagem, corredor da ala e entrada da manutenção. Rotas do NavMesh persistente até a ala e aproximações das mesas.
- Fusíveis inativos, descoberta por proximidade, IDs únicos, coleta, duplicação recusada, reentrada com 1/3 e **recarga real de checkpoint com 2/3**.
- Painel bloqueado sem instalação, instalação explícita, erro sem perda de inventário, sete passos, entradas repetidas recusadas, solução travada e ignição separada.
- Vermelho antes, normal depois, reentrada concluída e evento emitido uma vez.
- Perseguição até entrada aberta da manutenção, jogador sem dano através do limite e recuperação de zumbi colocado à força dentro dela, inclusive sem agente.
- Um vivo, uma vaga por morte, reposição com intervalo, parada após conclusão e spawn junto da chegada recusado.
- Ida/volta por `DoorReloadScreen` entre biblioteca/campus, um jogador e munição preservados. Carregamento de `CampusBlock4` e `LawSchool`; teclado do Bloco 4 presente com estado independente.
- `GameManager.EndSession()` limpa a missão. Capturas da ala, manutenção e painel antes/depois revisadas visualmente.
- Teste complementar com teclado sintético do Input System confirmou **E → raycast → coleta**, com o foco do harness ajustado para execução em background. Input de missão recusado durante pausa. Pista física/contextual acompanha alteração da sequência no Inspector e o toast comporta a pista inteira sem overflow.
- Integração conferida por bytes com a cópia compilada/testada: **6.664 fileIDs anteriores preservados**, **8.319 registros finais**, `.meta` anteriores idênticos, referências locais/novos GUIDs resolvidos e nenhum GUID duplicado no projeto. Cena e assets compartilhados visuais permanecem intactos. `git diff --check` limpo após normalizar espaços finais das novas linhas emitidas pela Unity.
- Reabertura dos arquivos finais na Unity confirmou serialização, textos e referências. Comparadas **731 dependências da cena**, com arquivos e `.meta` iguais entre projeto principal e cópia validada. Dois fingerprints `userData` de silhuetas foram normalizados somente na cópia de teste; recursos/importadores do projeto principal preservados.

Prévias: [ala em emergência](../../output/CampusLibrary-Gerador-Ala-Emergencia.png), [ala com energia](../../output/CampusLibrary-Gerador-Ala-Energia.png), [manutenção em emergência](../../output/CampusLibrary-Gerador-Emergencia.png), [gerador ligado](../../output/CampusLibrary-Gerador-Energia.png) e [pista](../../output/CampusLibrary-Gerador-Pista.png).

O Editor isolado voltou a registrar a exceção **preexistente** de indexação `UnityEditor.Search.SearchDatabase`, sem impedir os testes. Não foi atribuída à missão; os componentes da missão passaram sem erros.

Não houve build, benchmark/Profiler, sessão manual completa ou teste com controle físico. Puzzles completos do Bloco 4 e missão inteira de Direito não foram repetidos: a regressão foi de carregamento/estado/continuidade do jogador. Suas cenas/prefabs não foram alterados.

## Teste manual de ponta a ponta

Começar partida nova e entrar pelo campus. Conferir ausência dos fusíveis. Ir ao fundo leste do acervo norte, cruzar **Acervo complementar** e seguir à esquerda para **Manutenção**. Avançar contra a porta da sala do gerador para abri-la. Descobrir o gerador, coletar os três itens da tabela e voltar. Usar E uma vez no gerador para inserir os três fusíveis e abrir o puzzle. Confirmar cursor livre e câmera/personagem imóveis, clicar um terminal errado, fechar/reabrir e então executar a ordem correta. Conferir também clique fora/terminal travado, Escape, restauração dos controles, alinhamento em outra resolução e persistência ao sair/voltar. Durante a busca, eliminar o reforço e observar a reposição segura; atraí-lo até a manutenção para verificar o bloqueio.

## Painel modal por mouse — 10/11/2026

O painel foi adaptado conforme `Docs/SKILL_painel_interativo_jogo.md`, reutilizando `GameMenus` como gerenciador do modo de input e `LibraryMissionProgress` como regra independente. A ordem, os três fusíveis, os bilhetes, a persistência de sessão e os indicadores físicos foram preservados. O modal é criado uma vez por instância da missão e reusado nas reaberturas, sem acumular listeners. Depois dos três fusíveis instalados, somente o gerador abre o modal; completar a sequência liga o gerador imediatamente. Fechamento por Escape, botão, morte/transição ou destruição do controlador passa pelo mesmo proprietário de interação.

Validação desta revisão: compilação do conjunto de runtime com o Roslyn e as referências geradas pela Unity 6000.5.7f1, sem erros. Avisos existentes de APIs `Find*` obsoletas e um campo não usado em Direito permanecem fora desta mudança. A instância aberta estava executando uma cena e adiou a recompilação automática da Unity após a correção inicial; Play Mode, cliques reais, comparação visual, controle físico, build e teste em resoluções diferentes não foram executados nesta revisão.

Fluxo de ativação revisto em 10/11/2026: o modal passou a abrir exclusivamente pela interação com o gerador depois da instalação dos três fusíveis, e o sétimo acerto liga a energia sem uma segunda interação. Compilação direta do runtime concluída sem erros; somente os avisos preexistentes citados acima. Play Mode não executado.

Inserção direta revista em 10/11/2026: 42 objetos do antigo quadro físico e porta-fusíveis separado foram desativados no prefab, incluindo placas, números, segmentos, textos e colisores de interação. Os três indicadores foram reposicionados na frente do gerador. Compilação direta concluída sem erros e referências estáticas conferidas; Play Mode e comparação visual não executados.

Integração dos modelos revista em 10/11/2026: quatro arquivos em `Resources/Generator`, importador `GlbVertexColorImporter.cs`, shader `GeneratorVertexColor.shader` e seleção por `InstalledFuseCount`. Compilações isoladas de runtime e Editor passaram sem erros; avisos preexistentes de APIs `Find*` e campos não usados permanecem. Importação visual, Play Mode, colisão em jogo e comparação de enquadramento não foram executados.

## Integração com development — 10/11/2026

Branch local atualizada de `7e4bee86` para `455826b0`, preservando a implementação local do gerador. As duas chaves/cadeados de acesso recebidos do remoto continuam exigidos antes da entrada; a missão do gerador ocorre depois, dentro da biblioteca. Nenhuma abertura de cela foi acrescentada.

Não houve conflito de arquivo; os dois documentos sobrepostos foram combinados e a descrição de gerador futuro foi alinhada à missão existente. Os arquivos locais de código/assets foram preservados, com apenas normalização de finais de linha pelo Git. Oito imagens LFS recebidas foram baixadas e conferidas pelo SHA-256 registrado no commit.

Compilação C# isolada dos **123 scripts de runtime** e **14 de Editor** passou sem erros, usando referências/compilador da Unity instalada e saídas somente em `Temp`. Runtime com avisos existentes de API obsoleta e um campo não utilizado; Editor sem avisos. Não foi iniciada Unity, nem repetidos Play Mode, percursos, build ou comparação visual nesta atualização. Os testes de gameplay anteriores desta ficha permanecem históricos.
